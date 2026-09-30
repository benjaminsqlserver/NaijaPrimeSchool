# Sprint 16 — Audit log viewer

## Purpose

Since Sprint 1, every table has carried `CreatedOn/By`, `ModifiedOn/By`
and `DeletedOn/By`. Those columns only remember the **latest** change,
though: once a fee, phone number or result is edited twice, the first
edit, and what the value used to be, is gone. That isn't enough to answer
questions like *who changed this invoice's discount, and from what?*

Sprint 16 adds a real, append-only audit trail and a screen to read it:

- Every insert, update, soft delete, hard delete and restore made through
  EF Core writes an `AuditEntry` with who, when, which record, and each
  changed field's **old and new value**.
- The entry is written in the **same `SaveChanges`**, and so the same
  database transaction, as the change it describes. A change can't happen
  without its audit row, and a failed save leaves none behind.
- **Administration → Audit log** lets SuperAdmin and HeadTeacher filter,
  search and page through the trail, see the changes behind each entry, and
  open the full history of any single record.

The trail starts when this sprint is deployed. There is no way to
recover the history of changes made before it.

## Acceptance criteria

1. Creating, editing, deleting (soft or hard) and restoring any record
   through the app writes an audit entry. The entry records:
   - the action: *Created*, *Updated*, *Deleted* or *Restored*;
   - the user's id and name, or *System* for background work such as the
     notification dispatcher or a payment webhook;
   - the record type and id;
   - a readable label such as the invoice number, full name, title or
     subject;
   - the changed fields.
2. An update lists **only** the fields whose value actually changed, with
   before and after. A save that changes nothing writes nothing.
3. A soft delete is logged as *Deleted*, not as "IsDeleted changed from
   false to true", and an un-delete as *Restored*. A hard delete (for
   example, removing a role from a user) keeps the values the row held.
4. Secrets are never stored: `PasswordHash` and `SecurityStamp` appear
   only as `(hidden)`, so a password change is visible as a change, and
   Identity tokens aren't logged at all.
5. Noise is left out:
   - the audit columns themselves (the entry already records who and when);
   - Identity bookkeeping (`ConcurrencyStamp`, normalised names,
     `AccessFailedCount`);
   - read markers in message threads;
   - notification queue rows and announcement reads, which have their own
     log.
6. If the save fails, no audit entry is written, and none is carried into
   the next save on the same Blazor circuit.
7. The log is readable only by SuperAdmin and HeadTeacher, and the service
   enforces this. Audit rows can't be edited or deleted through the app.
8. The viewer:
   - filters by date range (inclusive days), user, record type, action,
     and a free-text search over labels, ids and changed values;
   - pages on the server, 50 rows at a time;
   - expands each row into a *Field / Before / After* table;
   - has a **History** button showing every change to that one record,
     which is also deep-linkable as
     `/admin/audit-log?entityType=Invoice&entityId=<id>`.
9. Seeding a fresh database writes no audit entries.

## Data model

| Table | Purpose |
| --- | --- |
| `AuditActions` | lookup: `CREATE`, `UPDATE`, `DELETE`, `RESTORE` (displayed as Created / Updated / Deleted / Restored) |
| `AuditEntries` | `OccurredOn`, `UserId` (nullable, **no FK**, so the trail survives and never constrains accounts), `UserName`, `AuditActionId`, `EntityType`, `EntityId` (composite keys joined with `,`), `EntityLabel`, `Changes` (JSON array of `{ Field, Old, New }`) |

Indexes: `OccurredOn`; `(EntityType, EntityId, OccurredOn)` for record
history; `(UserName, OccurredOn)` for "what did this person do".

`AuditEntry` is deliberately **not** a `BaseEntity`. It has no soft
delete and no audit columns, and is never audited itself. The trail is
append-only.

Migration: `AuditLog` — adds tables only.

## How capture works

`ApplicationDbContext.SaveChanges` / `SaveChangesAsync`:

```text
1. ApplyAuditAndSoftDelete()      (existing) stamp Created/Modified, turn Remove() into a soft delete
2. if anything changed:
     AuditTrail.Capture(ChangeTracker.Entries())
       per Added / Modified / Deleted row, skipping excluded types:
         action  = Added → CREATE, Deleted → DELETE,
                   IsDeleted false→true → DELETE, true→false → RESTORE, else UPDATE
         changes = CREATE: non-null new values
                   UPDATE: fields that are modified AND whose value differs
                   hard DELETE: the values the row held
                   (ignored fields dropped, secrets masked as "(hidden)")
         skip UPDATEs with no remaining changes
     AuditEntries.AddRange(...)
3. base.SaveChanges()             one transaction for the change and its audit rows
   on failure: detach the audit rows just added, then rethrow
```

Values are stored as invariant text: dates in ISO format, and decimals
with a `.` decimal point. Foreign keys are stored as ids. The label is
taken from the first of `FullName`, `InvoiceNumber`, `ReceiptNumber`,
`Reference`, `AdmissionNumber`, `Title`, `Subject`, `Name`, `UserName`,
`Code` or `Email` that the entity has.

The `AuditAction` ids are looked up once per `DbContext`. While a fresh
database is being seeded those rows don't exist yet, so seeding is
silently left out of the trail.

Attribution comes from the same `ICurrentUser` that fills
`ModifiedBy`. Browser testing confirmed that edits made on interactive
Blazor pages are recorded against the signed-in user.

## Code map

- `Domain/Auditing/AuditEntry.cs`, `AuditAction.cs`
- `Application/Auditing/IAuditLogService.cs`, `Dtos/AuditDtos.cs`
- `Infrastructure/Persistence/AuditTrail.cs` — what gets recorded:
  exclusions, ignored and masked fields, labels, value formatting.
- `Infrastructure/Persistence/ApplicationDbContext.cs` — the
  `SaveChanges` hook, `ConfigureAuditing`, and detaching audit rows on
  failure.
- `Infrastructure/Persistence/DatabaseInitializer.cs` —
  `SeedAuditLookupsAsync`, which runs first.
- `Infrastructure/Services/AuditLogService.cs` — filtered, paged reads;
  SuperAdmin / HeadTeacher only.
- `Web/.../Pages/Admin/AuditLog.razor` (`/admin/audit-log`) and
  `wwwroot/app.css` (the before/after table).
- `Web/.../Layout/NavMenu.razor` — **Administration → Audit log**.

## How to test end-to-end

1. Sign in as SuperAdmin. Open **Parents**, edit a parent's phone number
   and save.
2. Open **Administration → Audit log**. The newest row is *Updated* by
   you, labelled with the parent's name, *1 field(s)*. Expand it to see
   *Primary Phone: old → new*.
3. Press **History** on it. You see the parent's full timeline, starting
   from *Created*.
4. Delete the parent (after unlinking them from pupils). A *Deleted* row
   appears with no field noise.
5. Reset a user's password. The user account shows *Updated* with
   `Password Hash (hidden) → (hidden)`, and no hash anywhere.
6. Filter by **Action → Deleted**, by **User**, or search for a value that
   was changed (e.g. the old phone number).
7. Sign in as a teacher or bursar. **Administration** isn't in the menu,
   and `/admin/audit-log` is refused.

### SQL checks

```sql
SELECT TOP 20 e.OccurredOn, e.UserName, a.Code, e.EntityType, e.EntityLabel, e.Changes
FROM AuditEntries e JOIN AuditActions a ON a.Id = e.AuditActionId
ORDER BY e.OccurredOn DESC;

-- nothing sensitive ever stored
SELECT COUNT(*) FROM AuditEntries WHERE Changes LIKE '%AQAAAA%';   -- 0
```

## Known limits and follow-ups

- **Bulk SQL updates aren't captured.** A few services use
  `ExecuteUpdateAsync`, which bypasses `SaveChanges`. They clear
  "is current" flags on other sessions and terms, and "primary contact"
  on a pupil's other parent links, when a new one is chosen. The record
  the user actually edited is logged; those side-effect flag resets
  aren't.
- **Foreign keys show as ids.** For example, a changed `InvoiceStatusId`
  shows two GUIDs. Resolving lookups to names in the viewer is a possible
  refinement.
- **No retention policy yet.** The table grows forever. Archiving or
  purging entries older than N years (with care: audit data may be needed
  for disputes) would be an admin task for later.
- **Direct database edits** made outside the app aren't captured. SQL
  Server Temporal Tables or Change Data Capture would cover that if it
  ever matters.
