# Sprint 13 — Two-way messaging

## Purpose

Announcements (Sprint 8) and their email / SMS reminders (Sprints 11–12)
only flow one way: school to families. Parents still had to phone or visit
the office to ask a question. Sprint 13 adds private, two-way conversations
between a family and the school office, inside the portal.

- A **parent or student** can message the school from the portal,
  optionally about a particular pupil. A parent can only choose their own
  linked wards.
- The **school office** (SuperAdmin and HeadTeacher) works from a shared
  **Inbox**. Anyone in the office can read and reply, see which
  conversations are waiting for a reply, mark them resolved, and start a
  conversation with any parent or student who has a portal account.
- Each side sees which conversations have new messages, and an open
  conversation picks up the other side's reply within 15 seconds without a
  page reload.

## Acceptance criteria

1. **Parent portal → Messages** and **Student portal → Messages** list the
   user's own conversations, newest first, with a "New reply" badge where
   the office has written since they last looked.
2. **Message the school** starts a conversation with a subject, a message
   of up to 4,000 characters, and an optional pupil. For a parent the pupil
   list is limited to their linked wards; for a student it is themselves.
3. **Communications → Inbox** (`/messages`) lists every conversation for
   the office. It shows the family member and whether they're a parent or
   a student, the pupil it's about, the subject, a one-line preview, and
   the message count. Conversations with new messages from the family
   are marked with a dot, and open conversations whose last message came
   from the family show **Awaiting reply**. It has filters for
   Open / Resolved / All, unread only, and a search across subject, names
   and message text.
4. The office can start a conversation with any parent or student who has
   an active portal account. A **Send message** button on *Edit parent* /
   *Edit student* opens the form with that person already selected.
5. Opening a conversation marks it read for the viewer's side only.
6. The office can **Mark resolved** and **Reopen** a conversation. A new
   message from either side reopens a resolved conversation.
7. A family member can never see, reply to or close another family's
   conversation, even with a guessed URL. Teachers, bursars and
   storekeepers don't take part: they see no conversations and cannot
   send. All of these checks run in the service.
8. Message text is shown as plain text, with line breaks kept. No HTML is
   rendered.

## Data model

| Table | Purpose |
| --- | --- |
| `MessageThreadStatuses` | lookup: `OPEN`, `CLOSED` (displayed as *Open* / *Resolved*) |
| `MessageThreads` | one conversation: `Subject`, `FamilyUserId` (parent or student user), optional `StudentId`, status, `StartedByStaff`, `LastMessageOn`, `LastMessageFromStaff`, `FamilyLastReadOn`, `StaffLastReadOn`, `ClosedOn` |
| `ThreadMessages` | `MessageThreadId`, `SenderUserId`, `IsFromStaff`, `Body` (≤ 4,000), `SentOn` |

Migration: `20260929203856_Messaging`. It only adds tables, and the
`OPEN` / `CLOSED` statuses are seeded on startup.

Design notes:

- **Read tracking uses one timestamp per side** rather than a row per
  reader. A side has unread messages when the *other* side posted after
  that side's `…LastReadOn`. The office inbox is shared, so one staff
  member opening a conversation marks it read for the whole office. For a
  shared inbox that is the behaviour you want: nobody answers the same
  question twice.
- `LastMessageOn` / `LastMessageFromStaff` are stored on the thread so the
  inbox can sort and show **Awaiting reply** without scanning messages.
- Foreign keys to `Users` and `Students` are `Restrict`. Those rows are
  only ever soft-deleted, and SQL Server would reject the extra cascade
  path `Users → Students → MessageThreads`. Messages cascade from their
  thread.

## Who can do what

| Action | Parent / Student | SuperAdmin / HeadTeacher | Other staff |
| --- | --- | --- | --- |
| List conversations | own only | all | none |
| Open a conversation | own only | any | no |
| Start a conversation | to the office, as themselves | to any active parent / student | no |
| Tag a pupil | own wards / self | a pupil linked to the recipient | — |
| Reply | own only | any | no |
| Mark resolved / reopen | no (a reply reopens) | yes | no |

`MessagingService.VisibleThreads()` is the single place that decides which
threads a caller may touch. Every read and write goes through it, so a
guessed thread id from another family returns *not found* rather than
*forbidden*, and doesn't reveal that the thread exists.

## Live updates

`MessageThreadView` polls the thread every 15 seconds with a
`PeriodicTimer` and re-renders only when the message count or status has
changed. In Blazor Server the circuit's `DbContext` is shared by the whole
page, and EF Core refuses two operations at once. The poll and the user's
own actions (send, resolve) therefore go through a `SemaphoreSlim`, so
they queue instead of colliding. The timer is cancelled and disposed with
the component.

The reply box updates on `oninput` rather than Radzen's default `change`
(blur). Otherwise the Send button would stay disabled and the character
counter wouldn't move while the user types.

## Code map

### Domain — `NaijaPrimeSchool.Domain/Messaging`

- `MessageThread.cs`, `ThreadMessage.cs`, `MessageThreadStatus.cs`

### Application — `NaijaPrimeSchool.Application/Messaging`

- `IMessagingService.cs` — `ListThreadsAsync`, `GetThreadAsync` (which
  marks the thread read), `StartThreadAsync`, `ReplyAsync`,
  `SetClosedAsync`, `CountUnreadThreadsAsync`, `GetRecipientsAsync`,
  `GetStudentOptionsAsync`.
- `Dtos/MessagingDtos.cs` — thread summary and detail, message,
  `InboxFilter`, `StartThreadRequest`, recipient and pupil options.

### Infrastructure

- `Services/MessagingService.cs` — all rules above.
- `Persistence/ApplicationDbContext.cs` — `ConfigureMessaging`.
- `Persistence/DatabaseInitializer.cs` — `SeedMessagingLookupsAsync`.
- `DependencyInjection.cs` — registration.

### Web

- `Components/Shared/MessageThreadView.razor` — chat bubbles, reply box,
  resolve / reopen, 15-second refresh.
- `Components/Shared/NewMessageForm.razor` — recipient (staff), pupil,
  subject, message.
- `Pages/Messaging/Inbox.razor` (`/messages`), `InboxThread.razor`
  (`/messages/{id}`), `NewThread.razor` (`/messages/new?to={userId}`).
- `Pages/Portals/PortalMessages.razor` (`/portal/messages`),
  `PortalMessageThread.razor`, `PortalNewMessage.razor`.
- `Pages/Family/EditParent.razor`, `EditStudent.razor` — **Send message**
  button.
- `Layout/NavMenu.razor` — **Communications → Inbox** and **Messages** in
  both portals.
- `wwwroot/app.css` — chat bubble and unread-dot styles.

## How to test end-to-end

1. Create parents Ada and Bola, and a pupil Tunde, each with a portal
   account. Link Ada to Tunde.
2. Sign in as Ada. Go to **Parent portal → Messages → Message the school**,
   choose *Tunde*, write a subject and a message over two lines, and send.
   The conversation opens with your message on the right.
3. Sign in as the head teacher. **Communications → Inbox** shows Ada's
   conversation with a blue dot and **Awaiting reply**. Open it and the dot
   is gone.
4. Leave Ada's conversation open in her window. Reply as the head teacher.
   Within 15 seconds the reply appears on Ada's page without a reload.
5. As the head teacher, press **Mark resolved**. The conversation leaves
   the *Open* filter and appears under *Resolved*. Reply as Ada and it's
   open again, with a new-message dot in the inbox.
6. As Bola, paste the URL of Ada's conversation. You get *Conversation not
   found*.
7. From **Parents → Bola**, press **Send message**. The form opens with
   Bola selected. Choose *Tunde* as the pupil and send. You get *That pupil
   is not linked to this family.*

### SQL checks

```sql
SELECT t.Subject, s.Code, t.LastMessageFromStaff, t.LastMessageOn,
       t.FamilyLastReadOn, t.StaffLastReadOn,
       (SELECT COUNT(*) FROM ThreadMessages m WHERE m.MessageThreadId = t.Id AND m.IsDeleted = 0) AS Messages
FROM MessageThreads t
JOIN MessageThreadStatuses s ON s.Id = t.MessageThreadStatusId
WHERE t.IsDeleted = 0
ORDER BY t.LastMessageOn DESC;
```

## Known limits and follow-ups

- **No email / SMS alert for new messages yet.** Families find out through
  the portal badge. The Sprint 11 notification pipeline is built around
  announcements. Generalising it so a new office reply also triggers a
  reminder (respecting Sprint 12 preferences) is the natural next step.
- **Office only.** Teachers don't take part. Letting class teachers
  message the parents of their own class would need class-scoped access
  rules.
- **No attachments.** Text only.
- **Polling, not push.** A 15-second poll is simple and cheap at school
  scale. SignalR push would make replies instant.
- **Shared read marker.** One office member reading a conversation marks
  it read for everyone in the office. Per-staff read state would need a
  read table.
