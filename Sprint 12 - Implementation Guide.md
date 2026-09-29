# Sprint 12 — Notification preferences

## Purpose

Sprint 11 started emailing and texting families about announcements they
had not read in the portal. Everyone with a portal account and a valid
contact was included, and reminders could arrive at any hour. Sprint 12
gives each family a say:

- **Channel opt-outs** — turn email reminders and/or SMS reminders off.
- **Quiet hours** — a daily window (school time, e.g. 21:00–07:00) when no
  reminder is sent. Reminders due inside the window are held until it ends,
  not dropped.
- **Urgent categories** — announcements in an urgent category (seeded:
  *Emergency*) still go out during quiet hours. Channel opt-outs are always
  respected, even for emergencies.

Parents and students manage this themselves on a new portal page. The
school office can set it on a family's behalf from the parent or student
record, for example for a parent who phones in because they don't use the
portal.

## Acceptance criteria

1. **Portal → Reminder settings** (`/portal/notifications`) lets a parent
   or student switch email and SMS reminders on or off and set quiet hours
   in 30-minute steps. It shows where each reminder would be sent.
2. A channel the user has turned off is never queued. A reminder queued
   *before* the opt-out is skipped at send time with the note
   *"Recipient turned off SMS reminders."* (or email).
3. A reminder that falls due during the recipient's quiet hours stays
   **Pending**, is rescheduled to the end of the window, and does not use
   up a retry attempt. When the window ends, the dispatcher checks read
   status and preferences again before sending.
4. Announcements in an urgent category (Emergency) ignore quiet hours.
5. SuperAdmin and HeadTeacher can view and change any parent's or
   student's preferences from a new **Reminders** tab on **Edit parent**
   and **Edit student**. A parent or student can only see and change their
   own.
6. A user with no saved preferences gets the defaults: both channels on,
   no quiet hours. Nothing changes for existing families until they choose.
7. If the school has switched a channel off in configuration, the switch
   is disabled on the page with an explanation.
8. **Notify now** reports how many messages were not queued because the
   family turned that channel off.

## Data model

| Change | Detail |
| --- | --- |
| New table `NotificationPreferences` | `UserId` (unique, FK → `Users`, cascade), `EmailEnabled`, `SmsEnabled`, `QuietHoursEnabled`, `QuietHoursStart`, `QuietHoursEnd` (`time`), plus the usual audit and soft-delete columns |
| New column `AnnouncementCategories.IsUrgent` | `bit`, default 0. The migration sets it to 1 for the existing `EMERG` row, and the seeder sets it on fresh databases. |

Migration: `20260929104425_NotificationPreferences`, generated with
`dotnet ef migrations add`, plus one hand-written `UPDATE` that marks
Emergency urgent on databases seeded before this sprint.

The boolean columns deliberately have **no** database default. With
EF Core, a `bool` column that has a default of `true` would silently turn
a saved `false` (an opt-out) into `true` on insert, because EF omits values
equal to the CLR default.

## How the rules are applied

Queueing and dispatch share one small class,
`NotificationPreferenceRules`, so they can never disagree.

```text
publish / notify now
   └─ for each recipient × channel
        ├─ opted out?  → not queued (a waiting Pending row becomes Skipped)
        └─ otherwise   → queued as in sprint 11

dispatcher, for each due Pending row
   ├─ announcement deleted / unpublished / expired, account inactive → Skipped
   ├─ recipient opted out of this channel                             → Skipped
   ├─ already read in the portal                                      → Skipped
   ├─ inside quiet hours and category not urgent → stays Pending,
   │     ScheduledFor = end of window, AttemptCount unchanged
   └─ send → Sent / retry / Failed as in sprint 11
```

### Quiet hours arithmetic

Quiet hours are wall-clock times in the school's zone, set by
`Notifications:TimeZone` (default `Africa/Lagos`). `QuietHours.QuietUntil`:

- converts "now" to school time;
- treats the window as `[start, end)`, so a reminder at exactly the end
  time goes out;
- treats a window whose start is later than its end (21:00–07:00) as
  wrapping midnight: at 22:00 it holds until 07:00 the **next** day, and
  at 05:30 until 07:00 the **same** day;
- treats `start == end` as no window. Saving such a window is rejected
  with a validation message.

If the host has no entry for the configured zone id, it falls back to a
fixed UTC+1 zone (West Africa Time has no daylight saving, so this is
exact for Nigeria).

## Code map

### Domain

- `Communications/NotificationPreference.cs` — new entity.
- `Communications/AnnouncementCategory.cs` — new `IsUrgent` flag.

### Application

- `Communications/INotificationPreferenceService.cs` —
  `GetForCurrentUserAsync` / `SaveForCurrentUserAsync` for the portal, and
  `GetForUserAsync` / `SaveForUserAsync` for staff.
- `Communications/Dtos/NotificationPreferenceDtos.cs` —
  `NotificationPreferenceDto` (preferences, the resolved email and phone,
  school-wide channel switches, time zone, whether anything has been
  saved) and `UpdateNotificationPreferenceRequest`.
- `Communications/Dtos/NotificationDtos.cs` —
  `NotificationQueueResult.OptedOut`.
- `Family/Dtos/ParentDtos.cs`, `StudentDtos.cs` — `UserId`, so the edit
  pages know whether there is a portal account to manage.

### Infrastructure

- `Services/NotificationPreferenceService.cs` — load and save preferences.
  Staff methods check the SuperAdmin / HeadTeacher role inside the service,
  not only in the UI.
- `Notifications/NotificationPreferenceRules.cs` — opt-out and quiet-hours
  rules shared by queueing and dispatch.
- `Notifications/QuietHours.cs` — time-zone resolution and window
  arithmetic.
- `Notifications/ContactNormalizer.cs` — `FirstEmail` / `FirstPhone`
  helpers, so the recipient resolver and the settings page use the same
  "parent record, then portal account" fallback.
- `Services/NotificationService.cs` — skips opted-out channels when
  queueing.
- `Notifications/NotificationDispatcher.cs` — opt-out skip and quiet-hours
  hold at send time.
- `Notifications/NotificationOptions.cs` — `TimeZone`.
- `Persistence/ApplicationDbContext.cs`, `DatabaseInitializer.cs` — model
  configuration and the `IsUrgent` seed.

### Web

- `Components/Shared/NotificationPreferencesEditor.razor` — the Radzen
  form (switches, 30-minute time dropdowns, destination hints) used by
  both screens.
- `Pages/Portals/NotificationSettings.razor` — `/portal/notifications`
  (Parent, Student).
- `Pages/Family/EditParent.razor`, `EditStudent.razor` — new
  **Reminders** tab.
- `Layout/NavMenu.razor` — **Reminder settings** under both portal menus.
- `Pages/Communications/Announcements.razor` — the **Notify now** message
  includes the opted-out count.
- `appsettings.json` — `Notifications:TimeZone`.

## How to test end-to-end

With the default **Log** providers, set `UnreadGraceMinutes` to `0` and
`DispatchIntervalSeconds` to `10` in `appsettings.Development.json`.

1. Create parents Ada (email + phone) and Bola (email + phone).
2. Sign in as Ada and open **Parent portal → Reminder settings**. Both
   switches are on, and the page shows her email address and her
   `+234…` number. Turn **SMS** off and save.
3. As the head teacher, publish a **Parents** notice. The log shows three
   reminders, with no SMS for Ada, and **Notify now** reports
   *"1 not sent because the family turned that channel off."*
4. Sign in as Bola and set quiet hours to a window that includes the
   current time. Publish another ordinary notice. Bola's rows stay
   **Pending** with the note *"Held for quiet hours until HH:mm."* and are
   due at the end of the window.
5. Publish an **Emergency** notice. Bola's reminders are **Sent** straight
   away despite quiet hours.
6. As the head teacher, open **Parents → Bola → Reminders**. You see and
   can change her settings. The same tab on a parent without a portal
   account shows an explanation instead.
7. Try saving quiet hours with the same start and end time. You get
   *"Quiet hours must start and end at different times."*

### SQL checks

```sql
SELECT u.UserName, p.EmailEnabled, p.SmsEnabled,
       p.QuietHoursEnabled, p.QuietHoursStart, p.QuietHoursEnd
FROM NotificationPreferences p
JOIN Users u ON u.Id = p.UserId
WHERE p.IsDeleted = 0;

SELECT Code, IsUrgent FROM AnnouncementCategories ORDER BY DisplayOrder;
```

## Known limits and follow-ups

- **Held reminders show as Pending in the log** with a "Held for quiet
  hours" note. There is no separate status, so filtering Pending includes
  them.
- **Urgency is per category.** Marking one ordinary announcement as urgent
  would need a per-announcement override.
- **One preference per person.** A parent with several wards has one
  setting for all of them. Per-ward or per-category muting is a possible
  later refinement.
- **No admin screen for `IsUrgent` yet.** It is seeded (Emergency) and can
  be changed in the database. A categories admin page would expose it.
