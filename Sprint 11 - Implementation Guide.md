# Sprint 11 — Email / SMS notifications for unread announcements

## Purpose

Sprint 8 gave the head teacher a way to broadcast announcements into the
parent and student portals, with per-user `AnnouncementRead` tracking and
live unread counts. That only works if families actually open the portal.
Sprint 11 closes the loop: when an announcement is published, every parent
and student in its audience who **still has not read it** after a grace
period gets an **email** and/or an **SMS** pointing them back to the portal.

The pipeline is deliberately split in two:

1. **Queue** — publishing an announcement (or pressing **Notify now**)
   writes one `AnnouncementNotification` row per recipient per channel.
2. **Dispatch** — a hosted background worker picks up due rows, re-checks
   that the recipient still has not read the notice, sends through the
   configured gateway, and records the outcome.

Nothing is sent from inside a Blazor request, so a slow SMTP server or SMS
API can never freeze the head teacher's screen.

## Acceptance criteria

1. Publishing an announcement queues an email (and an SMS where a valid
   phone number exists) for every parent / student in its audience who has
   an active portal account.
2. The reminders wait out a configurable grace period (default 120
   minutes). Anyone who reads the notice in the portal inside that window
   is **not** emailed or texted — their row is marked **Skipped**.
3. **Notify now** on the Announcements grid sends immediately to everyone
   still unread, without creating duplicates for people already queued or
   already sent.
4. Unpublishing, deleting or letting an announcement expire before the
   reminder goes out means the reminder is skipped, not sent.
5. Failed sends are retried automatically (default: 3 attempts, 5 / 10 /
   15 minutes apart). After the final attempt the row is **Failed** and
   can be re-queued from the **Notification log** page, one row at a time
   or all at once.
6. The Announcements grid shows sent / pending / failed / skipped counts
   per notice, linking to the filtered log.
7. Development works out of the box: the default **Log** providers write
   messages to the application log instead of sending them.
8. No enums: channels and statuses are lookup tables, seeded on startup.

## Audience → recipients

The recipient rules mirror the portal visibility rules in
`AnnouncementService.ListForCurrentUserAsync`, so an email only ever
points at a notice the recipient can actually see:

| Audience code | Who is notified |
| --- | --- |
| `ALL` | every active parent and every active student with a portal account |
| `PARENT` | every active parent with a portal account |
| `STUDENT` | every active student with a portal account |
| `CLASS` | pupils actively enrolled in the target class (not withdrawn) **plus** every parent linked to one of them |

Contact details are resolved at queue time:

- **Parent email** — `Parent.Email`, falling back to the portal user's email.
- **Parent phone** — `Parent.PrimaryPhone`, then `AlternatePhone`, then the
  portal user's `PhoneNumber`.
- **Student** — the portal user's email and phone.

Phone numbers are normalised to `+234…`: `0803 123 4567` becomes
`+2348031234567`, and numbers that already carry a country code are kept.
Anything that does not look like a phone number (e.g. `"call me"`) is
treated as missing, so no SMS row is created.

## Data model

Three new tables, all using the project's soft-delete and audit columns:

| Table | Purpose |
| --- | --- |
| `NotificationChannels` | lookup: `EMAIL`, `SMS` |
| `NotificationStatuses` | lookup: `PENDING`, `SENT`, `FAILED`, `SKIPPED` |
| `AnnouncementNotifications` | one row per (announcement, user, channel) |

Key columns on `AnnouncementNotifications`:

- `ScheduledFor` — when the dispatcher may send it. Publishing sets this to
  `PublishedOn + UnreadGraceMinutes`; **Notify now** sets it to now.
- `Destination`, `RecipientName` — resolved when the row is queued.
- `Subject`, `Message` — stamped at send time, so the log shows exactly
  what the family received even if the notice is edited later.
- `AttemptCount`, `LastAttemptOn`, `SentOn`, `LastError`,
  `ProviderMessageId` — delivery bookkeeping.

Indexes:

- unique `(AnnouncementId, UserId, NotificationChannelId)` — re-queueing
  re-arms the existing row instead of inserting a duplicate;
- `(NotificationStatusId, ScheduledFor)` — the dispatcher's scan.

The FK to `Users` is `Restrict`, not `Cascade`, so SQL Server never sees
two cascade paths from `Users` (the other goes through
`Announcements.PostedById`). The FK to `Announcements` cascades, which only
matters for hard deletes — the app itself only soft-deletes.

Migration: `20260926040847_AnnouncementNotifications`. It is purely
additive and was generated with `dotnet ef migrations add`.

## Status lifecycle

```text
               queue / notify-now
                      │
                      ▼
   ┌──────────────► PENDING ──── read in portal / unpublished /
   │                  │          deleted / expired / account inactive
   │     send OK      │                      │
   │   ┌──────────────┤                      ▼
   │   ▼              │ send failed       SKIPPED
   │  SENT            │ (attempt < Max)      │
   │                  ├──► reschedule ─┐     │
   │                  │                │     │
   │                  │ attempt = Max  │     │
   │                  ▼                │     │
   │               FAILED ◄────────────┘     │
   │                  │                      │
   └── retry / retry all failed / re-queue ◄─┘
```

- The dispatcher only ever picks up **Pending** rows whose `ScheduledFor`
  has passed.
- Re-queueing a notice (publish again, or **Notify now**) leaves
  **Pending** and **Sent** rows alone and re-arms **Failed** / **Skipped**
  rows, unless the recipient has since read the notice.
- If a recipient has read the notice by the time it is re-queued, any row
  of theirs still waiting out the grace period is marked **Skipped**
  straight away.

## Code map

### Domain — `NaijaPrimeSchool.Domain/Communications`

- `NotificationChannel.cs`, `NotificationStatus.cs` — lookup entities.
- `AnnouncementNotification.cs` — the queue / log row.
- `Announcement.cs` — gains a `Notifications` navigation.

### Application — `NaijaPrimeSchool.Application/Communications`

- `INotificationService.cs` — `QueueForAnnouncementAsync`, `ListAsync`,
  `GetCountsByAnnouncementAsync`, `RetryAsync`, `RetryAllFailedAsync`.
- `IMessageGateways.cs` — `IEmailGateway`, `ISmsGateway` and
  `GatewayResult`. The names avoid clashing with ASP.NET Identity's
  `IEmailSender<TUser>`.
- `Dtos/NotificationDtos.cs` — `AnnouncementNotificationDto`,
  `NotificationFilter`, `NotificationQueueResult`, `NotificationCounts`.
- `Dtos/AnnouncementDtos.cs` — `AnnouncementDto.Notifications` carries the
  per-status counts shown in the grid.

### Infrastructure — `NaijaPrimeSchool.Infrastructure`

- `Services/NotificationService.cs` — recipient resolution, queueing,
  listing, counts and retries.
- `Services/AnnouncementService.cs` — `CreateAsync` (publish immediately)
  and `PublishAsync` call `QueueForAnnouncementAsync(sendNow: false)` when
  `AutoQueueOnPublish` is on. `ListAsync` fills the notification counts.
- `Notifications/NotificationDispatcher.cs` — sends one batch of due rows
  and saves after **each** row, so a crash mid-batch never re-sends a
  delivered message.
- `Notifications/NotificationDispatchWorker.cs` — a `BackgroundService`
  that ticks every `DispatchIntervalSeconds` and drains batches, each in
  its own DI scope (fresh `DbContext`).
- `Notifications/NotificationComposer.cs` — email (plain text + HTML in
  the school's green and gold) and SMS text. SMS messages are squeezed
  into one 160-character segment, keep the portal link intact, and use
  `...` rather than `…` so they stay in the GSM-7 alphabet.
- `Notifications/ContactNormalizer.cs` — email validation and Nigerian
  phone normalisation.
- `Notifications/LogGateways.cs` — development gateways that log instead of
  sending.
- `Notifications/SmtpEmailGateway.cs` — `System.Net.Mail` SMTP.
- `Notifications/TermiiSmsGateway.cs` — [Termii](https://developers.termii.com)
  SMS (typed `HttpClient`).
- `Notifications/NotificationOptions.cs` — the `Notifications` config
  section.
- `DependencyInjection.cs` — `AddNotifications` binds the options, picks
  the gateways from `Provider`, and registers the hosted worker. An unknown
  provider name fails fast at startup.
- `Persistence/ApplicationDbContext.cs`, `DatabaseInitializer.cs` — model
  configuration and lookup seeding.

### Web — `NaijaPrimeSchool.Web`

- `Pages/Communications/Announcements.razor` — new **Email / SMS** column
  (badges link to the filtered log), a **Notify now** button
  (`notifications_active`) on published, unexpired notices, and a
  **Notification log** header button.
- `Pages/Communications/NotificationLog.razor` — `/announcements/notifications`
  (SuperAdmin, HeadTeacher). Stat cards, filters (search, announcement,
  channel, status), an expandable row showing the exact message sent,
  per-row retry and **Retry all failed**. Accepts `?announcementId=`.
- `Layout/NavMenu.razor` — **Communications → Notification log**.
- `appsettings.json` — the `Notifications` section.

## Configuration

```json
"Notifications": {
  "Enabled": true,
  "AutoQueueOnPublish": true,
  "UnreadGraceMinutes": 120,
  "DispatchIntervalSeconds": 60,
  "BatchSize": 50,
  "MaxAttempts": 3,
  "RetryDelayMinutes": 5,
  "SchoolName": "Naija Prime School",
  "PortalBaseUrl": "",
  "Email": {
    "Enabled": true,
    "Provider": "Log",
    "FromAddress": "no-reply@naijaprimeschool.ng",
    "FromName": "Naija Prime School",
    "Smtp": { "Host": "", "Port": 587, "EnableSsl": true, "UserName": "", "Password": "" }
  },
  "Sms": {
    "Enabled": true,
    "Provider": "Log",
    "SenderId": "NaijaPrime",
    "Termii": { "BaseUrl": "https://api.ng.termii.com", "ApiKey": "", "Channel": "generic" }
  }
}
```

| Setting | Meaning |
| --- | --- |
| `Enabled` | Master switch for the dispatcher. When off, rows still queue and stay Pending; turning it back on catches up. |
| `AutoQueueOnPublish` | Queue reminders automatically on publish. When off, only **Notify now** queues. |
| `UnreadGraceMinutes` | How long after publishing before the reminder goes out. `0` sends on the next tick. |
| `PortalBaseUrl` | Public URL of the app, e.g. `https://portal.naijaprimeschool.ng`. Adds a link to `/portal/announcements` in the email and SMS. Leave empty to omit it. |
| `Email.Provider` | `Log` or `Smtp`. |
| `Sms.Provider` | `Log` or `Termii`. |
| `Sms.SenderId` | Alphanumeric sender ID registered with the SMS provider (at most 11 characters). |
| `Sms.Termii.Channel` | `generic`, or `dnd` to reach numbers on the Do-Not-Disturb register (DND needs Termii approval). |

Keep secrets such as the SMTP password and Termii API key out of
`appsettings.json`. Use `dotnet user-secrets` in development and
environment variables in production, for example:

```bash
dotnet user-secrets set "Notifications:Sms:Provider" "Termii" --project src/NaijaPrimeSchool.Web
dotnet user-secrets set "Notifications:Sms:Termii:ApiKey" "<your key>" --project src/NaijaPrimeSchool.Web
```

```powershell
$env:Notifications__Email__Provider = "Smtp"
$env:Notifications__Email__Smtp__Password = "<smtp password>"
```

## Applying the migration

The app migrates on startup (`DatabaseInitializer`), so running it is
enough. To apply the migration by hand:

```bash
dotnet ef database update --project src/NaijaPrimeSchool.Infrastructure --startup-project src/NaijaPrimeSchool.Web
```

The two lookup tables are seeded on the next start.

## How to test end-to-end

Use the default **Log** providers, and set `UnreadGraceMinutes` to `2` and
`DispatchIntervalSeconds` to `10` in `appsettings.Development.json` so you
don't have to wait.

1. Create two parents at **Family → Add Parent**: *Ada* with email
   `ada@example.ng` and phone `0803 123 4567`, and *Bola* with an email
   but no phone.
2. At **Communications → New announcement**, create a notice for the
   **Parents** audience and tick **Publish immediately**.
3. On **Announcements** the new row shows **3 pending** (Ada email, Ada
   SMS, Bola email). Open **Notification log**: the rows are due two
   minutes after publishing.
4. Sign in as Bola in a private window and open **Announcements** in the
   portal, which marks the notice read.
5. Wait for the grace period. The application log shows `[Email:Log]` and
   `[SMS:Log]` entries for Ada only. Bola's row is **Skipped** with the
   note *"Read in the portal before the reminder went out."*
6. Expand Ada's SMS row in the log. The message is at most 160 characters
   and ends with the portal link (if `PortalBaseUrl` is set).
7. Press **Notify now** on the same notice. The toast reports
   *0 queued … already queued or sent … already read*, and no duplicates
   are created.
8. Failure path: set `Sms:Provider` to `Termii` with an empty `ApiKey` and
   publish another notice to Parents. Ada's SMS is retried, then shows
   **Failed** with *"Notifications:Sms:Termii:ApiKey is not configured."*
   Switch back to `Log`, restart, and press **Retry all failed**; the row
   goes to **Sent**.
9. Class audience: enrol a pupil with a portal account in a class, link
   Ada as the pupil's parent, and publish a **Specific Class** notice for
   that class. Only the pupil and Ada are queued.
10. Unpublish path: publish, press **Notify now**, then immediately
    **Unpublish**. On the next tick every row becomes **Skipped** with
    *"Announcement was unpublished before the reminder went out."*

### SQL checks

```sql
SELECT s.Code, c.Code AS Channel, COUNT(*) AS Messages
FROM AnnouncementNotifications n
JOIN NotificationStatuses s ON s.Id = n.NotificationStatusId
JOIN NotificationChannels c ON c.Id = n.NotificationChannelId
WHERE n.IsDeleted = 0
GROUP BY s.Code, c.Code
ORDER BY s.Code, c.Code;
```

## Known limits and follow-ups

- **Single dispatcher.** The worker assumes one app instance is sending.
  Running several web nodes against one database would need a
  row-claiming step (e.g. `UPDATE … OUTPUT` with `READPAST`) so two nodes
  never send the same row.
- **No per-family opt-out yet.** Everyone with a portal account and a
  valid contact is notified. A preferences screen (email on/off, SMS
  on/off, quiet hours) is the natural next step.
- **Students' contacts** come from their portal account. Young pupils
  often share a parent's phone, so the SMS channel mostly reaches parents
  in practice.
- **Contact snapshot.** Contact details are captured at queue time.
  Editing a parent's phone after queueing does not change a pending row;
  re-queueing after a failure picks up the new value.
- **SMS cost.** Every SMS costs money. Use `UnreadGraceMinutes`, or turn
  `Sms.Enabled` off, to control spend.
