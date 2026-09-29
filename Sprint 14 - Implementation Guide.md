# Sprint 14 — Email / SMS alerts for message replies

## Purpose

Sprint 13 added private conversations between families and the school
office, but a family only found out about a reply by opening the portal.
Sprint 14 closes that gap. When the office writes to a family, the parent
or student gets an email and/or SMS, unless they have already read the
reply in the portal by the time the alert is due.

It reuses the Sprint 11 pipeline (queue → background dispatcher → Log /
SMTP / Termii gateways → notification log) and respects every Sprint 12
preference.

## Acceptance criteria

1. When a SuperAdmin or HeadTeacher replies in a conversation, or starts a
   new conversation with a family member, an email and an SMS alert are
   queued for that family member. There is one per channel they can
   receive.
2. Alerts wait `Notifications:Messages:GraceMinutes` (default 10). If the
   family member opens the conversation in the portal before then (the
   Sprint 13 view refreshes every 15 seconds, so someone already on the
   page catches it), the alert is **Skipped** with the note *"Read in the
   portal before the alert went out."*
3. Several office replies in quick succession produce **one** alert per
   channel, not one per reply. A reply that arrives after the previous
   alert went out queues a new one, and earlier alerts stay in the log.
4. A family's own messages never trigger alerts.
5. **Email** quotes the office messages the family hasn't read yet (up to
   the last five) and links straight to the conversation. **SMS** never
   includes the message text, because family phones are often shared. It
   names the conversation subject and links to it, and fits in one
   160-character segment.
6. Sprint 12 preferences apply: an opted-out channel isn't queued, and an
   alert due during quiet hours is held until the window ends. Message
   alerts are never urgent.
7. An alert for a conversation that has since been deleted is skipped.
8. **Communications → Notification log** shows announcement reminders and
   message alerts together, with a **Type** filter and an *About* column
   that links a message alert to its conversation. Each office
   conversation has an **Alert log** button that opens the log filtered to
   that conversation.
9. `Notifications:Messages:Enabled = false` switches message alerts off
   without affecting announcement reminders.

## Data model

Message alerts are rows in the existing notifications table, so the
dispatcher, retry logic, log page and gateways all work unchanged.

| Change | Detail |
| --- | --- |
| `AnnouncementNotifications.AnnouncementId` | now **nullable** |
| `AnnouncementNotifications.MessageThreadId` | new nullable FK → `MessageThreads` (cascade) |
| Check constraint `CK_AnnouncementNotifications_OneSource` | exactly one of `AnnouncementId` / `MessageThreadId` is set |
| Unique index on `(AnnouncementId, UserId, NotificationChannelId)` | now **filtered** to `AnnouncementId IS NOT NULL`, so message rows aren't caught by it |
| Index on `(MessageThreadId, UserId, NotificationChannelId)` | non-unique: one row per alert, so the history is kept |

Migration: `MessageAlerts`. Existing announcement rows are untouched and
already satisfy the check constraint. Its `Down` deletes message-alert
rows first, because `AnnouncementId` becomes required again.

The entity keeps its Sprint 11 name, `AnnouncementNotification`, and the
table keeps `AnnouncementNotifications`. That avoids renaming a table that
holds live data. The class comment explains that it now covers both kinds.

## How it works

```text
office replies / starts a conversation (MessagingService)
   └─ NotificationService.QueueMessageAlertAsync(threadId)
        for each enabled channel:
          opted out                         → not queued (OptedOut)
          no usable email / phone           → not queued (MissingContact)
          a Pending alert already waiting   → not queued (AlreadyQueued)
          otherwise                         → Pending, ScheduledFor = now + GraceMinutes

dispatcher, for each due message alert
   ├─ conversation deleted / account inactive / channel off / opted out → Skipped
   ├─ no office message newer than FamilyLastReadOn                   → Skipped (read)
   ├─ inside quiet hours                                               → held until the window ends
   └─ compose from the unread office messages at send time → Sent / retry / Failed
```

The email is composed **at send time** from the office messages the
family still hasn't read. An alert queued by the first of three quick
replies therefore quotes all three.

Contacts come from `RecipientContacts.ForUserAsync`, which the Sprint 12
preferences page now uses too. The page and the alert always agree on
where a message goes: a parent's own email / phone first, then the portal
account.

## Configuration

```json
"Notifications": {
  "Messages": {
    "Enabled": true,
    "GraceMinutes": 10
  }
}
```

The email and SMS providers, `PortalBaseUrl`, `TimeZone` and retry
settings are shared with announcement reminders (see the Sprint 11 and 12
guides). Set `PortalBaseUrl` so alerts link straight to the conversation
(`/portal/messages/{id}`).

## Code map

- `Domain/Communications/AnnouncementNotification.cs` — nullable
  `AnnouncementId`, new `MessageThreadId`.
- `Application/Communications/INotificationService.cs` —
  `QueueMessageAlertAsync`.
- `Application/Communications/Dtos/NotificationDtos.cs` —
  `AnnouncementNotificationDto.Kind` / `Title` / `MessageThreadId`,
  `NotificationFilter.Kind` / `MessageThreadId`, `NotificationKinds`.
- `Infrastructure/Services/NotificationService.cs` — queueing, and
  listing both kinds.
- `Infrastructure/Services/MessagingService.cs` — queues an alert after
  an office reply or an office-started conversation.
- `Infrastructure/Notifications/NotificationDispatcher.cs` — separate
  skip checks and composition for announcement and message rows.
- `Infrastructure/Notifications/NotificationComposer.cs` —
  `ComposeMessageEmail`, `ComposeMessageSms`.
- `Infrastructure/Notifications/RecipientContacts.cs` — shared single-user
  contact resolution.
- `Infrastructure/Notifications/NotificationOptions.cs` —
  `MessageAlertOptions`.
- `Infrastructure/Persistence/ApplicationDbContext.cs` — FK, filtered
  unique index, check constraint.
- `Web/.../Communications/NotificationLog.razor` — Type filter, *About*
  column, `?messageThreadId=`.
- `Web/.../Shared/MessageThreadView.razor` — **Alert log** button (staff).
- `Web/.../Shared/NotificationPreferencesEditor.razor`,
  `Portals/NotificationSettings.razor` — wording now covers message
  alerts.

## How to test end-to-end

Use the default **Log** providers, and set `Messages:GraceMinutes` to `1`
and `DispatchIntervalSeconds` to `10`.

1. Create parent Ada with an email address and the phone `0803 123 4567`.
2. As Ada, start a conversation with the school. No alert is queued: the
   log is empty for it.
3. As the head teacher, reply twice within a minute. Open **Alert log**
   from the conversation: there are two **Pending** rows (email and SMS),
   not four.
4. Wait a minute without opening the conversation as Ada. The application
   log shows `[Email:Log]` quoting both replies and `[SMS:Log]` naming the
   subject only. Both rows are **Sent**.
5. Reply again as the head teacher, then open the conversation as Ada
   within a minute. The new alerts become **Skipped** — *Read in the portal
   before the alert went out.*
6. As Ada, turn SMS off under **Reminder settings** and get another reply
   from the office. Only an email is queued.
7. Filter the Notification log by **Type → Message alerts**. Announcement
   reminders disappear, and each row links to its conversation.

## Known limits and follow-ups

- **Families only.** The office isn't alerted when a family writes. It
  sees the unread dot and *Awaiting reply* in the Inbox. A staff digest
  email is a possible later addition.
- **Replies to the email aren't read.** The email says so and points to
  the portal. Inbound email parsing is out of scope.
- **One grace period for everyone.** It isn't a per-family setting.
