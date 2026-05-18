# Naija Prime School

A modern school management system for Nigerian primary schools, built with **.NET 10**, **Blazor Auto**, **Clean Architecture**, **SQL Server**, and **Radzen Blazor Components**.

Ten sprints have shipped. **Sprint 1** delivered the authentication & authorization foundation: user accounts, role-based access control, login/logout, activation/deactivation, and the SuperAdmin user-management screens. **Sprint 2** built the academic domain on top of that foundation: sessions, terms, class arms, subjects, timetable periods, and a click-to-edit weekly timetable grid. **Sprint 3** plugged students and parents into that academic structure: pupil profiles, parent/guardian directory, parent-to-pupil linkage with relationship + primary-contact + pickup flags, and per-session enrolment with a withdrawal lifecycle. **Sprint 4** lands attendance: a daily class register, per-subject session attendance off the timetable, the AttendanceStatus lookup, a submit/reopen lifecycle, and a per-class percentage summary. **Sprint 5** closes the academic loop: a per-(term, class, subject) gradebook of TermAssessments and AssessmentScores, a result computation pipeline that produces SubjectResults with grade bands and class positions, and per-(pupil, term) ReportCards with affective and psychomotor ratings, attendance roll-up, and a publish/unpublish lifecycle. **Sprint 5b** wires up pupil photographs: a dedicated upload pipeline backed by a reusable `StudentAvatar` Razor component, with the photo (or a coloured initials tile fallback) shown next to every pupil row across the Students, Enrolments, daily- and subject-attendance, score-sheet, and report-card pages. **Sprint 6** lays the financial spine: per-(term, class level) `FeeSchedule`s with line items, one-click invoice issuance to every actively-enrolled pupil, multi-allocation payments with auto-allocate, refund flow, and a bursar dashboard summarising invoiced, collected and outstanding amounts. **Sprint 7** turns the storeroom on: a `StoreItem` catalog tracked by `ItemCategory` and `UnitOfMeasure`, a movement-log of `StockMovement` rows (purchases, issuances, openings, write-offs, adjustments) typed by a directional `StockMovementType` lookup, a `Supplier` directory, a low-stock dashboard, and audit-safe reversal that undoes an entry's effect on `QuantityOnHand` when soft-deleted. **Sprint 8** finally turns the system outward to the families: a parent portal listing every linked ward with class, outstanding balance, attendance percentage, and report-card count plus a tabbed ward-detail page; a student portal with today's timetable, attendance summary, results history, and fee ledger; and an announcements pipeline — `AnnouncementCategory`, `AnnouncementAudience` (Everyone / Parents / Students / Specific Class) and per-user `AnnouncementRead` tracking — that lets the head teacher broadcast scoped, expiring, pin-to-top notices into both portals with live unread counts. **Sprint 9** closes the last manual step: creating a parent or a student now provisions a matching `ApplicationUser` in the **Parent** / **Student** role inside the same unit of work and stamps the new user's id onto `Parent.UserId` / `Student.UserId`, so the family can sign in straight away and the parent / student portal loads without an admin follow-up. **Sprint 10** is a focused hotfix on `ParentService.SoftDeleteAsync`: the link-count guard now reads from a fresh `db.StudentParents.CountAsync(...)` query instead of the `Parent.StudentLinks` navigation, so a parent whose links were soft-deleted earlier in the same Blazor circuit can now actually be deleted; the same operation also retires the auto-provisioned `ApplicationUser` so the deleted parent cannot keep signing in.

Implementation walk-throughs for each sprint live at the repo root:

- `Sprint 1 - Implementation Guide.pdf`
- `Sprint 2 - Implementation Guide.pdf`
- `Sprint 3 - Implementation Guide.pdf`
- `Sprint 4 - Implementation Guide.pdf`
- `Sprint 5 - Implementation Guide.pdf`
- `Sprint 5b - Implementation Guide.docx`
- `Sprint 6 - Implementation Guide.docx`
- `Sprint 7 - Implementation Guide.docx`
- `Sprint 8 - Implementation Guide.docx`
- `Sprint 9 - Implementation Guide.docx`
- `Sprint 10 - Implementation Guide.docx`

---

## Sprint 1 — Identity & user management ✅

- **Identity & access**
  - Cookie-based authentication with ASP.NET Core Identity
  - Seven seeded roles: **SuperAdmin**, **HeadTeacher**, **Teacher**, **SchoolBursar**, **SchoolStoreKeeper**, **Parent**, **Student**
  - Role-based authorization policies and role-scoped navigation
  - Lockout on repeated failed logins (5 attempts / 15 minutes)
  - Revalidating authentication state (deactivated users are signed out within the revalidation window)
- **User management** (SuperAdmin)
  - List, search, filter by role / status
  - Add new user with role assignment
  - Edit user details (name, title, gender, DOB, address, email, phone)
  - Activate / deactivate accounts
  - Reassign roles
  - Reset password (admin-initiated)

## Sprint 2 — Academic domain ✅

- **Calendar**
  - Academic **Sessions** (e.g. 2025/2026) with current-session flag
  - **Terms** within each session, typed by a `TermType` lookup (First / Second / Third)
- **Structure**
  - **Classes** (Primary 1A, JSS 2B, …) tied to a session and a `ClassLevel` lookup
  - Optional class teacher pulled from active users in the **Teacher** role
  - **Subjects** with unique short codes (e.g. MTH for Mathematics)
- **Timetables**
  - Configurable **TimetablePeriods** (Period 1, Short Break, Lunch, …)
  - **Weekly timetable grid**: pick a term and class, click any period × weekday cell to assign a subject, teacher, room, and notes
  - One subject per (term, class, weekday, period) slot, enforced by a unique index
- **Pages added** (`/sessions`, `/terms`, `/classes`, `/subjects`, `/timetable-periods`, `/timetable`), all gated to **SuperAdmin** + **HeadTeacher**, with the timetable read view also visible to **Teacher**

## Sprint 3 — Students & parents ✅

- **Pupils**
  - **Students** with admission number, admission date, demographics, photo, blood group, allergies and medical notes
  - Optional `UserId` hook for a future student portal login
  - Active / inactive flag and soft delete (refused while an enrolment is still open)
- **Parents & guardians**
  - **Parents** directory with title, marital status, primary + alternate phones, email, occupation, employer
  - Optional `UserId` hook for a future parent portal login
  - Soft delete refused while linkages still exist
- **Linkage**
  - **StudentParent** join entity carrying `Relationship` (Father, Mother, Guardian, Uncle, …), `IsPrimaryContact`, `CanPickUp` and free-text notes
  - At most one parent appears per pupil (composite unique index)
  - At most one primary contact per pupil (enforced in service)
- **Enrolment**
  - **Enrolment** ties a pupil to a `SchoolClass` with `EnrolledOn`, optional `WithdrawnOn`, `EnrolmentStatus` (Active / Suspended / Withdrawn / Transferred / Graduated)
  - One enrolment per (pupil, class) and at most one open enrolment per (pupil, session) — service-enforced
  - Withdraw flow that stamps `WithdrawnOn`, flips the status, and appends notes
- **Lookup tables (no enums)**
  - `Relationships`, `EnrolmentStatuses`, `BloodGroups`, `MaritalStatuses` — every domain concept that would normally be a C# enum is a first-class table, seeded on startup
- **Pages added** (`/students`, `/students/new`, `/students/{id}`, `/parents`, `/parents/new`, `/parents/{id}`, `/enrolments`), all gated to **SuperAdmin** + **HeadTeacher**. Edit-pupil and edit-parent pages use Radzen tabs to keep profile editing separate from linkage management.

## Sprint 4 — Attendance ✅

- **Daily attendance**
  - **DailyAttendanceRegister** keyed by (Class × Date), with the term auto-resolved from the date
  - Pre-loads every pupil currently enrolled in the class on the date as Present, ready to mark
  - **DailyAttendanceEntry** per pupil with status, optional arrival time (for Late), and remarks
  - One register per (class, day), one entry per (register, pupil) — composite unique indexes
- **Subject attendance**
  - **SubjectAttendanceSession** keyed by (TimetableEntry × Date), tying attendance to a specific lesson on the timetable from sprint 2
  - Refuses dates whose weekday does not match the timetable entry's `WeekDay`
  - **SubjectAttendanceEntry** per pupil with status and remarks
- **Lifecycle**
  - Submit / reopen flow: registers are editable until submitted; reopening restores edit access without losing the original `SubmittedOn` timestamp
  - Submitted registers cannot be soft-deleted until reopened
  - Audit columns capture every change made post-reopen
- **Lookup table (no enums)**
  - `AttendanceStatuses` — Present, Late, Excused, Sick, Absent, Suspended — each with a short code (P, L, E, S, A, SP), display order, and a `CountsAsPresent` flag that drives the summary percentages
- **Summary**
  - Per-class summary view across a term: days counted, days present/late/absent/excused, plus a colour-coded percentage badge (≥ 90% green, ≥ 75% amber, otherwise red)
- **Pages added** (`/attendance/daily`, `/attendance/subject`, `/attendance/summary`), gated to **SuperAdmin** + **HeadTeacher** + **Teacher** (teachers genuinely take registers, so they have write access)

## Sprint 5 — Assessments, results & report cards ✅

- **Gradebook**
  - **TermAssessment** keyed by (Term × SchoolClass × Subject) with `AssessmentType` (CA1, CA2, Mid-Term, Assignment, Project, Examination), max score, multiplier weight, and an assessment date
  - **AssessmentScore** per (assessment, pupil) with absent flag, decimal score, and free-text remarks
  - Score-sheet page pre-loads every actively-enrolled pupil for the class so a class teacher can fill in scores in one pass
  - Publish / unpublish lifecycle on each assessment — published assessments are read-only; unpublishing is needed before edit or delete
- **Result computation**
  - **SubjectResult** per (pupil, term, subject) carrying weighted total, percentage, `GradeBand`, dense-ranked position in class, and a `IsFinalised` flag
  - One-click recompute (idempotent) and Compute & finalise (locks the rows) on the results page
  - Service-level guards: cannot delete a finalised result, cannot recompute over finalised rows without an explicit reopen
- **Report cards**
  - **ReportCard** per (pupil, term) carrying subjects-taken, total score, average percentage, position, attendance roll-up (days present/absent/late, total school days), and the next-term-begins date
  - Affective traits and psychomotor skills sections — each rated on the seeded 5-point `TraitRating` ladder (Excellent, Very Good, Good, Fair, Poor)
  - Class teacher's comment + head teacher's comment as free text
  - Generate / refresh batch action that pulls together every SubjectResult, joins to the sprint-4 attendance summary, and ranks pupils by average percentage
  - Publish / unpublish flow — published cards are read-only and never overwritten by subsequent regenerations
- **Lookup tables (no enums)**
  - `AssessmentTypes` (with `IsExam` flag), `GradeBands` (with `LowerBound`/`UpperBound` and `Remark`), `AffectiveTraits`, `PsychomotorSkills`, `TraitRatings` — every domain concept that would normally be a C# enum is a first-class table, seeded on startup
- **Pages added** (`/assessments`, `/assessments/{id}/scores`, `/results`, `/reports`, `/reports/{id}`) — gradebook pages gated to **SuperAdmin** + **HeadTeacher** + **Teacher**; results and report-card pages gated to **SuperAdmin** + **HeadTeacher**

## Sprint 5b — Student photos ✅

- **Upload pipeline**
  - `IStudentPhotoService` accepts a stream + content type, validates size and format (JPG, PNG, WebP up to 5 MB), writes the file to `wwwroot/uploads/students/{studentId}{ext}`, and updates `Student.PhotoUrl` in one `SaveChanges`
  - Re-upload overwrites the previous file by Id; old extensions are cleaned up so format switches don't orphan a file
  - Cache-busting `?v=<ticks>` query string defeats stale browser caches when a teacher replaces a photo
- **Reusable component**
  - `<StudentAvatar />` Razor component renders a circular photo if `PhotoUrl` is set, otherwise a coloured tile with the pupil's initials
  - Three sizes (`small`, `medium`, `large`) so the same component drives small cell thumbnails and the big report-card header avatar
- **Display surface**
  - Avatars added next to every pupil row on **Students**, **Enrolments**, **Daily attendance**, **Subject attendance**, **Score sheet**, **Report cards list**, and as a large avatar on the **Report card detail** header
  - DTO projections (`StudentDto`, `EnrolmentDto`, daily/subject attendance entry DTOs, `AssessmentScoreDto`, `ReportCardDto`) gained `StudentPhotoUrl` + `StudentFirstName` + `StudentLastName` so a single round-trip carries everything the component needs
- **Upload UI**
  - New **Photo** tab on the **Edit Student** page (`/students/{id}`) with a big preview, a hidden `<InputFile>`, and a Radzen-styled "Choose new photo" / "Remove photo" pair
- **Storage hygiene**
  - `.gitignore` ignores everything under `wwwroot/uploads/students/` except `.gitkeep`, so uploaded photos stay on the local filesystem and don't pollute the repo

## Sprint 6 — Fees, invoices, receipts & bursar workflows ✅

- **Fee schedules**
  - **FeeSchedule** keyed by (Term × ClassLevel) carrying a title, optional notes, and a publish/unpublish lifecycle
  - **FeeScheduleItem** per line item keyed to a `FeeCategory`, with description, amount, mandatory flag, and display order
  - Composite unique index on (TermId, ClassLevelId) — no two competing schedules for the same audience
  - Edit and item changes refused while the schedule is published; unpublishing refused once invoices have been issued from it
- **Invoices**
  - **Invoice** issued per pupil per term with sequential `NPS/INV/<year>/<seq>` numbering
  - **InvoiceLine** per fee category carrying description, amount, per-line discount, and a back-link to the source `FeeScheduleItem`
  - Statuses (`Draft`, `Issued`, `PartiallyPaid`, `Paid`, `Overdue`, `Cancelled`) recomputed automatically after every discount or payment change
  - Bulk issue action that fans a published schedule into one invoice per actively-enrolled pupil in a chosen class, skipping pupils that already have an invoice for that (term, class)
- **Payments & receipts**
  - **Payment** with sequential `NPS/RCP/<year>/<seq>` receipt numbering
  - **PaymentAllocation** distributes a single payment across one or more invoices; composite unique (PaymentId, InvoiceId) index prevents double-allocation
  - Auto-allocate-oldest-first helper on the record-payment page; hand-allocate row-by-row also supported
  - Refund flow releases allocations and flips status to `Refunded`, leaving the receipt history intact
- **Bursar dashboard**
  - Per-term summary of total invoiced, total collected, total outstanding, plus invoice-status counts
  - Collected-by-method and invoiced-by-category breakdowns
- **Lookup tables (no enums)**
  - `FeeCategories` (with `IsMandatoryByDefault` flag), `PaymentMethods` (with `RequiresReference` flag), `InvoiceStatuses`, `PaymentStatuses` — each seeded on startup; services key off `Code` so rows can be renamed without breaking logic
- **Pages added** (`/fees`, `/fees/{id}`, `/invoices`, `/invoices/issue`, `/invoices/{id}`, `/payments`, `/payments/new`, `/payments/{id}`, `/finance`) — all gated to **SuperAdmin** + **HeadTeacher** + **SchoolBursar**. The previously-disabled Finance navigation placeholder is now a fully realised workspace.

## Sprint 7 — Store & inventory management ✅

- **Catalog**
  - **StoreItem** with name, optional SKU, `ItemCategory`, `UnitOfMeasure`, cached `QuantityOnHand`, `ReorderLevel`, `LastUnitCost`, and active flag
  - Filtered unique index on `Sku` (only enforced when an item has one) so most items can leave SKU blank while imported goods can carry a barcode
  - Quantity stored at `decimal(14,3)` so kilograms and litres are tracked with three-decimal precision; money fields stay at `decimal(12,2)`
  - Create-with-opening-balance shortcut on `/store/items/new` — the same `SaveChanges` writes the item *and* an `OPENING` `StockMovement` so day-one stock is a proper audit row, not a magic seed
- **Movement log**
  - **StockMovement** with sequential `NPS/STK/<year>/<seq>` numbering, `MovedOn` date, `Quantity`, optional `UnitCost` and computed `TotalCost`, free-text reference and notes
  - Three optional counter-party slots — `ReceivedFromSupplierId`, `IssuedToStudentId`, `IssuedToSchoolClassId`, `IssuedToUserId` — with service-level **at-most-one-recipient** validation so a row never claims a parcel went to a supplier *and* a student
  - Inbound rows refresh `LastUnitCost` so reorder pricing reflects the most recent purchase
  - Outbound rows refuse to dispatch more than `QuantityOnHand`, refuse to record without a recipient (or an explicit `WRITEOFF`/`ADJ_OUT` reason), and decrement the cached running balance inside the same `SaveChanges`
  - Soft-delete on a movement **reverses** `Direction × Quantity` against the item's on-hand and *then* removes the row — clean undo semantics for the inevitable typo
- **Suppliers**
  - **Supplier** directory with contact name, primary phone, email, address, notes, active flag, plus computed counts of `PurchaseCount` and `TotalPurchased`
  - Soft-delete refused while any purchase rows still point at the supplier — protects historical movement attribution
- **Store dashboard**
  - Items in stock vs items below reorder level, total stock value (Σ on-hand × last unit cost), recent inbound / outbound counts for the trailing 30 days
  - Low-stock list ordered by deficit, plus the last ten movements with linked counter-party
- **Lookup tables (no enums)**
  - `ItemCategories` (10 seeded: BOOK, UNIF, STAT, SPRT, CLN, FOOD, FURN, ICT, MED, OTH), `UnitsOfMeasure` (10 seeded: EA, PC, PK, BOX, CTN, SET, BAG, KG, L, M), `StockMovementTypes` (7 seeded: OPENING +1, PURCHASE +1, RETURN +1, ADJ_IN +1, ISSUE -1, WRITEOFF -1, ADJ_OUT -1) — services key off `Code` and the `Direction` column drives every running-balance calculation, so types can be renamed without breaking logic
- **Pages added** (`/store`, `/store/items`, `/store/items/new`, `/store/items/{id}`, `/store/movements`, `/store/movements/new`, `/store/suppliers`) — all gated to **SuperAdmin** + **HeadTeacher** + **SchoolStoreKeeper**. The previously-disabled Store & Inventory navigation placeholder is now a fully realised workspace.

## Sprint 8 — Parent & student portals + announcements ✅

- **Announcements**
  - **Announcement** entity with title, body, optional expiry date, optional pinned flag, publish/unpublish lifecycle and a soft-delete audit trail
  - Scoped by an **AnnouncementAudience** lookup — Everyone, Parents, Students, or Specific Class (the only audience that requires a target `SchoolClass`)
  - Typed by an **AnnouncementCategory** lookup — General, Academic, Finance, Events, Holiday, Health, Emergency
  - Composer page at `/announcements/new` and edit page at `/announcements/{id}` — the audience dropdown drives a conditional class picker, and the admin grid filters by category / audience / publish status with an *Include expired* toggle
- **Read tracking**
  - **AnnouncementRead** row per (announcement, user) with a composite unique index, so clicking *Mark as read* twice is a no-op
  - `CountUnreadForCurrentUserAsync` powers the live unread badge on both portals and re-uses the same visibility predicates as the feed (audience codes + relevant class ids + non-expired)
- **Parent portal**
  - `/portal/parent` opens on a ward grid — one card per linked pupil showing relationship, primary-contact flag, current class, outstanding balance, attendance percentage and published-report-card count
  - `/portal/parent/wards/{id}` opens a tabbed detail with Overview, Report cards (deep-linking to the sprint-5 PDF), and Fees & invoices (the same `StudentLedger` the bursar uses, scoped to the pupil)
  - Access guard (`CurrentUserCanViewStudentAsync`) checks the `StudentParent` linkage on every load — SuperAdmin and HeadTeacher pass unconditionally so the same pages double as admin previews
- **Student portal**
  - `/portal/student` dashboard with five stat tiles (class, outstanding fees, attendance percentage, published report cards, unread announcements) and a *Today's timetable* grid resolved from `TimetableEntries` for the pupil's current class and term
  - Sub-pages at `/portal/student/profile` (read-only demographics), `/portal/student/results` (published report cards), `/portal/student/attendance` (term tally), and `/portal/student/fees` (invoice + payment ledger)
- **Shared announcements feed**
  - `/portal/announcements` renders the same list for both audiences — the service resolves audience codes from `ICurrentUser.Roles` plus the relevant class ids (a parent gets their wards' classes; a student gets their own current class)
  - Cards highlight unread rows with a coloured left border; *Back* routes to `/portal/parent` or `/portal/student` based on the signed-in role
- **Identity plumbing**
  - `ICurrentUser` gains `Roles` and `IsInRole` so the announcement service can decide audience visibility without reaching into `ClaimsPrincipal`
  - The school office links a login to a `Family.Parent` or `Family.Student` row by setting `UserId` on the entity — `PortalService.ResolveParentIdForCurrentUserAsync` and `ResolveStudentIdForCurrentUserAsync` walk that link on every portal load and render a friendly fallback card when no link exists
- **Lookup tables (no enums)**
  - `AnnouncementCategories` (7 seeded: GEN, ACAD, FIN, EVENT, HOL, HEALTH, EMERG), `AnnouncementAudiences` (4 seeded: ALL, PARENT, STUDENT, CLASS — the last with `RequiresTargetClass = true`) — services key off `Code` and the `RequiresTargetClass` flag drives editor/validator behaviour, so audience labels can be renamed without breaking logic
- **Pages added** — admin: `/announcements`, `/announcements/new`, `/announcements/{id}` (gated to **SuperAdmin** + **HeadTeacher**). Parent portal: `/portal/parent`, `/portal/parent/wards/{id}` (gated to **Parent** + the two admin roles). Student portal: `/portal/student`, `/portal/student/profile`, `/portal/student/results`, `/portal/student/attendance`, `/portal/student/fees` (gated to **Student** + the two admin roles). Shared: `/portal/announcements` (gated to **Parent** + **Student** + the two admin roles). The previously-disabled *My Children* navigation placeholder is replaced with three new role-scoped panels.

## Sprint 9 — Auto-provisioned portal accounts ✅

- **Create-parent provisions a login**
  - `/parents/new` now collects a **Username** and **Initial password** alongside the existing profile fields; `Email` becomes required (Identity needs a unique email)
  - `ParentService.CreateAsync` runs as a single unit of work: validate the request, create the `ApplicationUser`, assign the **Parent** role, then insert the `Parent` row with `UserId = user.Id`
  - Username + email uniqueness are checked up-front so the form returns a clean error instead of letting Identity throw a generic `DuplicateUserName`
  - If role assignment fails after the user is created, the half-built user is deleted to avoid orphaning an account
- **Create-student provisions a login**
  - `/students/new` mirrors the parent flow, capturing **Username**, **Email** and **Initial password** in a new *Portal sign-in* section
  - `StudentService.CreateAsync` keeps its existing guards (admission number uniqueness, DOB-before-admission, optional initial class) running before any Identity work, so a validation failure never leaves a dangling user
  - The `ApplicationUser` is created in the **Student** role; `Student.UserId` is stamped before `SaveChanges`
- **Portals light up on first sign-in**
  - `PortalService.ResolveParentIdForCurrentUserAsync` and `ResolveStudentIdForCurrentUserAsync` already keyed off `Parent.UserId` / `Student.UserId`; once those columns are populated the dashboards, ward detail, today's timetable, fees and results pages all resolve without any portal-layer changes
- **No new tables, no migration**
  - `Parent.UserId` and `Student.UserId` were added in sprint 3; the `Parent` and `Student` roles were seeded in sprint 1's `DatabaseInitializer.SeedRolesAsync`. Sprint 9 wires the two together for new rows only — existing rows with `UserId = NULL` keep the legacy manual-link behaviour.
- **Files touched** — `Application/Family/Dtos/ParentDtos.cs`, `Application/Family/Dtos/StudentDtos.cs`, `Infrastructure/Services/ParentService.cs`, `Infrastructure/Services/StudentService.cs`, `Web/Components/Pages/Family/CreateParent.razor`, `Web/Components/Pages/Family/CreateStudent.razor`. Walk-through: `Sprint 9 - Implementation Guide.docx` (+ markdown companion).

## Sprint 10 — Parent-deletion hotfix ✅

- **The bug**
  - After unlinking every pupil from a parent on the **Edit student → Parents** tab, the office still saw *"Cannot delete a parent who is still linked to a student. Unlink them first."* on **Parents → Delete**, even though `SELECT COUNT(*) FROM StudentParents WHERE ParentId = … AND IsDeleted = 0` reported zero.
- **Root cause**
  - `ParentService.SoftDeleteAsync` loaded the parent with `.Include(p => p.StudentLinks)` and then tested `parent.StudentLinks.Count > 0`. EF Core relationship fix-up populates that navigation with **every** `StudentParent` currently in the change tracker that points at the parent — including the ones the unlink had just soft-deleted (`IsDeleted = true`, `EntityState = Modified`). Global query filters only constrain new SELECTs, not in-memory graph stitching, so in a long-lived Blazor Server circuit the count stayed positive and the guard fired forever.
- **The fix**
  - `ParentService.SoftDeleteAsync` no longer eager-loads the navigation. The guard now runs a fresh `db.StudentParents.CountAsync(l => l.ParentId == id, ct)` query, which honours the `!IsDeleted` filter at the database level and is immune to whatever is sitting in the change tracker. The error message also reports the actual remaining count so the office can see at a glance whether something is genuinely still linked.
- **Bonus cleanup**
  - When the parent does delete, the linked `ApplicationUser` (provisioned in sprint 9) is now retired via `UserManager.DeleteAsync`. Without this the parent could still sign in with their old credentials and hit the *"We can't find your record"* portal fallback indefinitely. `DeleteAsync` flows through the same soft-delete interceptor that everything else uses, so the user row is preserved for auditing.
- **No new tables, no migration, no UI change** — `Parents.razor` already calls `ParentService.SoftDeleteAsync` and surfaces the operation result; the fix is entirely behind the service boundary.
- **Files touched** — `Infrastructure/Services/ParentService.cs`. Walk-through: `Sprint 10 - Implementation Guide.docx` (+ markdown companion).

## Cross-cutting (every sprint)

- **Beautiful, inviting UI**
  - Green + gold Nigerian-themed palette
  - Radzen Blazor components (DataGrid, Dialog, Notification, Layout, Sidebar, Forms)
  - Responsive layout
- **Data integrity**
  - No enums; every lookup (roles, titles, genders, term types, class levels, week days, relationships, enrolment statuses, blood groups, marital statuses, attendance statuses, assessment types, grade bands, affective traits, psychomotor skills, trait ratings, fee categories, payment methods, invoice statuses, payment statuses, item categories, units of measure, stock movement types, announcement categories, announcement audiences, …) is a first-class table
  - Soft delete for all entities, enforced globally via EF Core query filters
  - Auditing (CreatedOn/By, ModifiedOn/By, DeletedOn/By) applied automatically in `SaveChanges`

---

## Tech stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET 10 |
| Web / UI | Blazor Web App (Auto interactivity), Radzen Blazor Components |
| Auth | ASP.NET Core Identity (cookie) with custom `ApplicationUser` / `ApplicationRole` (Guid keys) |
| Data | Entity Framework Core 10, SQL Server (LocalDB by default) |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / Web + Web.Client) |

---

## Architecture

```
NaijaPrimeSchool/
├── src/
│   ├── NaijaPrimeSchool.Domain/                # Entities, base types, interfaces. No dependencies on infra.
│   │   ├── Common/                              # BaseEntity, IAuditable, ISoftDelete
│   │   ├── Identity/                            # ApplicationUser, ApplicationRole, Title, Gender, Roles (sprint 1)
│   │   ├── Academics/                           # Session, Term, SchoolClass, Subject, Timetable* (sprint 2)
│   │   ├── Family/                              # Student, Parent, StudentParent, Enrolment + lookups (sprint 3)
│   │   ├── Attendance/                          # Daily/Subject registers + AttendanceStatus lookup (sprint 4)
│   │   ├── Results/                             # TermAssessment, SubjectResult, ReportCard + lookups (sprint 5)
│   │   ├── Finance/                             # FeeSchedule, Invoice, Payment + lookups (sprint 6)
│   │   ├── Inventory/                           # Supplier, StoreItem, StockMovement + lookups (sprint 7)
│   │   └── Communications/                      # Announcement, AnnouncementRead + lookups (sprint 8)
│   ├── NaijaPrimeSchool.Application/            # DTOs, service contracts, shared abstractions
│   │   ├── Common/                              # ICurrentUser, OperationResult
│   │   ├── Users/                               # IUserService, ILookupService, DTOs (sprint 1)
│   │   ├── Academics/                           # I*Service interfaces and DTOs (sprint 2)
│   │   ├── Family/                              # IStudentService, IParentService, IEnrolmentService, DTOs (sprint 3)
│   │   ├── Attendance/                          # IDailyAttendanceService, ISubjectAttendanceService, DTOs (sprint 4)
│   │   ├── Results/                             # IAssessmentService, IResultService, IReportCardService, DTOs (sprint 5)
│   │   ├── Finance/                             # IFeeScheduleService, IInvoiceService, IPaymentService, DTOs (sprint 6)
│   │   ├── Inventory/                           # ISupplierService, IStoreItemService, IStockMovementService, DTOs (sprint 7)
│   │   ├── Communications/                      # IAnnouncementService, DTOs (sprint 8)
│   │   └── Portals/                             # IPortalService, DTOs (sprint 8)
│   ├── NaijaPrimeSchool.Infrastructure/         # EF Core DbContext, Identity stores, service impls, seed, migrations
│   ├── NaijaPrimeSchool.Web/                    # Blazor server host (auth endpoints, layout, pages, Program.cs)
│   │   └── Components/Pages/
│   │       ├── Users/                           # User management pages (sprint 1)
│   │       ├── Academics/                       # Sessions, Terms, Classes, Subjects, Periods, Timetable (sprint 2)
│   │       ├── Family/                          # Students, Parents, Enrolments (sprint 3)
│   │       ├── Attendance/                      # Daily, Subject and Summary attendance pages (sprint 4)
│   │       ├── Results/                         # Assessments, Score sheet, Results, Report cards (sprint 5)
│   │       ├── Finance/                         # Fee schedules, Invoices, Payments, Bursar dashboard (sprint 6)
│   │       ├── Inventory/                       # Store dashboard, Catalog, Movements, Suppliers (sprint 7)
│   │       ├── Communications/                  # Announcements list + editor (sprint 8)
│   │       └── Portals/                         # Parent + student portals, shared announcements feed (sprint 8)
│   └── NaijaPrimeSchool.Web.Client/             # Blazor WebAssembly client (Auto interactivity)
├── tools/                                       # Scripts (e.g. sprint guide generators)
└── NaijaPrimeSchool.slnx
```

Dependency direction flows inward: `Web` → `Infrastructure` → `Application` → `Domain`. The Domain layer has no outward dependencies other than `Microsoft.Extensions.Identity.Stores` (for `IdentityUser<Guid>`/`IdentityRole<Guid>` base types).

### Soft delete & auditing

- Every entity derives from `BaseEntity` or implements `IAuditable` + `ISoftDelete`.
- `ApplicationDbContext.SaveChanges` intercepts writes, rewrites `Delete` to `Modified`, and stamps `Created*`, `Modified*`, `Deleted*` fields from the current user (`ICurrentUser`).
- Global query filters hide soft-deleted rows by default; use `IgnoreQueryFilters()` for admin/audit queries.

### No enums, tables only

Domain concepts that would typically be enums are stored as tables:

- **Titles** (Mr., Mrs., Dr., Prof., Chief, Alhaji, ...)
- **Genders** (Male, Female)
- **Roles** (seeded via `RoleManager` on startup)

`Roles` is a `static` class of `string` constants — it is **not** an enum — so code can reference role names strongly without coupling to integer values.

---

## Prerequisites

- **.NET 10 SDK** (10.0.201 or newer) — https://dotnet.microsoft.com/download
- **SQL Server** — any of:
  - SQL Server LocalDB (ships with Visual Studio / SQL Server Express) — default
  - SQL Server Developer / Express / Standard
  - Azure SQL
- A modern browser

---

## Getting started

### 1. Clone

```bash
git clone https://github.com/benjaminsqlserver/NaijaPrimeSchool.git
cd NaijaPrimeSchool
```

### 2. Configure the database connection

Open `src/NaijaPrimeSchool.Web/appsettings.json` and set `ConnectionStrings:DefaultConnection`. The default targets SQL Server LocalDB:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=NaijaPrimeSchool;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

For a full SQL Server instance:

```
Server=localhost;Database=NaijaPrimeSchool;User Id=sa;Password=<your-password>;TrustServerCertificate=True
```

### 3. Restore, build, run

```bash
dotnet restore
dotnet build
dotnet run --project src/NaijaPrimeSchool.Web
```

On first run, `DatabaseInitializer` will:

1. Apply EF Core migrations (create the schema).
2. Seed the **Title** and **Gender** lookup tables.
3. Seed the seven application roles.
4. Create the default **SuperAdmin** account.

Navigate to the URL printed by `dotnet run` (typically `https://localhost:7xxx` or `http://localhost:5xxx`).

### 4. Sign in

| Field | Value |
| --- | --- |
| Email | `superadmin@naijaprimeschool.ng` |
| Password | `Admin@12345` |

> **Change this password immediately after your first sign-in** (use *Edit user → Reset password* on the SuperAdmin account).

---

## EF Core migrations

To add a new migration after changing domain entities:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/NaijaPrimeSchool.Infrastructure \
  --startup-project src/NaijaPrimeSchool.Web \
  --output-dir Persistence/Migrations
```

To apply migrations manually (the app also applies them on startup):

```bash
dotnet ef database update \
  --project src/NaijaPrimeSchool.Infrastructure \
  --startup-project src/NaijaPrimeSchool.Web
```

---

## Roles & authorization

| Role | Intended access |
| --- | --- |
| SuperAdmin | Full system access, user and role management |
| HeadTeacher | School-wide academic oversight |
| Teacher | Classroom and subject management |
| SchoolBursar | Fees and school finance |
| SchoolStoreKeeper | Inventory and supplies |
| Parent | Ward profiles, reports, communications |
| Student | Own profile, timetable, results |

User management screens are gated behind the `ManageUsers` policy, which requires the `SuperAdmin` role.

---

## Project map

| Path | Purpose |
| --- | --- |
| `src/NaijaPrimeSchool.Domain/Common/BaseEntity.cs` | Base type with id + audit + soft delete |
| `src/NaijaPrimeSchool.Domain/Identity/ApplicationUser.cs` | `IdentityUser<Guid>` with profile, audit, soft delete |
| `src/NaijaPrimeSchool.Domain/Identity/Roles.cs` | String constants for role names (not an enum) |
| `src/NaijaPrimeSchool.Application/Users/IUserService.cs` | Service contract consumed by the UI |
| `src/NaijaPrimeSchool.Infrastructure/Persistence/ApplicationDbContext.cs` | EF Core + Identity + soft-delete filters + auditing |
| `src/NaijaPrimeSchool.Infrastructure/Persistence/DatabaseInitializer.cs` | Migrate + seed lookups, roles, SuperAdmin |
| `src/NaijaPrimeSchool.Infrastructure/Services/UserService.cs` | User CRUD, role assignment, activation |
| `src/NaijaPrimeSchool.Web/Program.cs` | Host wiring: Identity, Radzen, policies, endpoints, DB init |
| `src/NaijaPrimeSchool.Web/Components/Account/Pages/Login.razor` | Sign-in page (Radzen UI) |
| `src/NaijaPrimeSchool.Web/Components/Pages/Users/` | List / Create / Edit / Roles / Reset password |
| `src/NaijaPrimeSchool.Web/Components/Layout/MainLayout.razor` | Radzen shell with sidebar + header |
| `src/NaijaPrimeSchool.Web/wwwroot/app.css` | Green + gold school theme + timetable grid styles |
| `src/NaijaPrimeSchool.Domain/Academics/` | Session, Term, SchoolClass, Subject, Timetable* (sprint 2) |
| `src/NaijaPrimeSchool.Application/Academics/` | I*Service contracts and DTOs for the academic domain |
| `src/NaijaPrimeSchool.Infrastructure/Services/SessionService.cs` | Sessions CRUD + SetCurrent |
| `src/NaijaPrimeSchool.Infrastructure/Services/TermService.cs` | Terms CRUD + SetCurrent |
| `src/NaijaPrimeSchool.Infrastructure/Services/SchoolClassService.cs` | Class arms CRUD |
| `src/NaijaPrimeSchool.Infrastructure/Services/SubjectService.cs` | Subjects CRUD |
| `src/NaijaPrimeSchool.Infrastructure/Services/TimetableService.cs` | Periods CRUD + entry list / upsert / delete |
| `src/NaijaPrimeSchool.Web/Components/Pages/Academics/` | Sessions, Terms, Classes, Subjects, Periods, Timetable grid |
| `src/NaijaPrimeSchool.Domain/Family/` | Student, Parent, StudentParent, Enrolment + lookups (sprint 3) |
| `src/NaijaPrimeSchool.Application/Family/` | I*Service contracts and DTOs for the family domain |
| `src/NaijaPrimeSchool.Infrastructure/Services/StudentService.cs` | Student CRUD + parent linkage |
| `src/NaijaPrimeSchool.Infrastructure/Services/ParentService.cs` | Parent CRUD |
| `src/NaijaPrimeSchool.Infrastructure/Services/EnrolmentService.cs` | Enrolment CRUD + Withdraw |
| `src/NaijaPrimeSchool.Web/Components/Pages/Family/` | Students, Parents, Enrolments admin pages |
| `src/NaijaPrimeSchool.Domain/Attendance/` | DailyAttendance/SubjectAttendance entities + AttendanceStatus lookup (sprint 4) |
| `src/NaijaPrimeSchool.Application/Attendance/` | I*Service contracts and DTOs for attendance |
| `src/NaijaPrimeSchool.Infrastructure/Services/DailyAttendanceService.cs` | Daily register CRUD + class/student summaries |
| `src/NaijaPrimeSchool.Infrastructure/Services/SubjectAttendanceService.cs` | Per-lesson register CRUD |
| `src/NaijaPrimeSchool.Web/Components/Pages/Attendance/` | Daily, Subject, and Summary attendance pages |
| `src/NaijaPrimeSchool.Domain/Results/` | TermAssessment, AssessmentScore, SubjectResult, ReportCard + lookups (sprint 5) |
| `src/NaijaPrimeSchool.Application/Results/` | I*Service contracts and DTOs for assessments / results / report cards |
| `src/NaijaPrimeSchool.Infrastructure/Services/AssessmentService.cs` | Gradebook CRUD + score-sheet bulk save |
| `src/NaijaPrimeSchool.Infrastructure/Services/ResultService.cs` | Compute weighted percentages, grade bands, positions |
| `src/NaijaPrimeSchool.Infrastructure/Services/ReportCardService.cs` | Generate cards, edit comments + ratings, publish |
| `src/NaijaPrimeSchool.Web/Components/Pages/Results/` | Assessments, score sheet, results, report cards |
| `src/NaijaPrimeSchool.Application/Family/IStudentPhotoService.cs` | Photo upload / remove contract (sprint 5b) |
| `src/NaijaPrimeSchool.Infrastructure/Services/StudentPhotoService.cs` | Saves to `wwwroot/uploads/students`, updates `Student.PhotoUrl` |
| `src/NaijaPrimeSchool.Web/Components/Shared/StudentAvatar.razor` | Reusable circular avatar (photo or initials fallback) |
| `src/NaijaPrimeSchool.Web/wwwroot/uploads/students/` | Per-pupil photo storage (gitignored except `.gitkeep`) |
| `src/NaijaPrimeSchool.Domain/Finance/` | FeeSchedule, FeeScheduleItem, Invoice, InvoiceLine, Payment, PaymentAllocation + lookups (sprint 6) |
| `src/NaijaPrimeSchool.Application/Finance/` | I*Service contracts and DTOs for fees, invoices, payments, ledger |
| `src/NaijaPrimeSchool.Infrastructure/Services/FeeScheduleService.cs` | Schedule + items CRUD, publish lifecycle |
| `src/NaijaPrimeSchool.Infrastructure/Services/InvoiceService.cs` | Issue from schedule, set discounts, cancel, ledger, status recompute |
| `src/NaijaPrimeSchool.Infrastructure/Services/PaymentService.cs` | Record, refund, bursar-dashboard summary |
| `src/NaijaPrimeSchool.Web/Components/Pages/Finance/` | Fee schedules, invoices, payments, bursar dashboard |
| `src/NaijaPrimeSchool.Domain/Inventory/` | Supplier, StoreItem, StockMovement + ItemCategory / UnitOfMeasure / StockMovementType lookups (sprint 7) |
| `src/NaijaPrimeSchool.Application/Inventory/` | I*Service contracts and DTOs for suppliers, store items, stock movements, store dashboard |
| `src/NaijaPrimeSchool.Infrastructure/Services/SupplierService.cs` | Supplier CRUD with purchase-history guard |
| `src/NaijaPrimeSchool.Infrastructure/Services/StoreItemService.cs` | Catalog CRUD + create-with-opening-balance shortcut |
| `src/NaijaPrimeSchool.Infrastructure/Services/StockMovementService.cs` | Record / soft-delete movements, running balance, NPS/STK numbering, dashboard rollups |
| `src/NaijaPrimeSchool.Web/Components/Pages/Inventory/` | Store dashboard, catalog, item detail, movements, record movement, suppliers |
| `src/NaijaPrimeSchool.Domain/Communications/` | Announcement, AnnouncementRead + AnnouncementCategory / AnnouncementAudience lookups (sprint 8) |
| `src/NaijaPrimeSchool.Application/Communications/` | IAnnouncementService contract and DTOs |
| `src/NaijaPrimeSchool.Application/Portals/` | IPortalService contract and dashboard DTOs |
| `src/NaijaPrimeSchool.Infrastructure/Services/AnnouncementService.cs` | Announcement CRUD, publish/unpublish, portal feed + per-user read tracking |
| `src/NaijaPrimeSchool.Infrastructure/Services/PortalService.cs` | Parent + student dashboard façade with `CurrentUserCanViewStudentAsync` access guard |
| `src/NaijaPrimeSchool.Web/Components/Pages/Communications/` | Announcements admin list + editor |
| `src/NaijaPrimeSchool.Web/Components/Pages/Portals/` | Parent dashboard, ward detail, student dashboard + sub-pages, shared announcements feed |
| `tools/generate_sprint2_guide.py` | Generator for `Sprint 2 - Implementation Guide.docx` |
| `tools/generate_sprint3_guide.py` | Generator for `Sprint 3 - Implementation Guide.docx` |
| `tools/generate_sprint4_guide.py` | Generator for `Sprint 4 - Implementation Guide.docx` |
| `tools/generate_sprint5_guide.py` | Generator for `Sprint 5 - Implementation Guide.docx` |
| `tools/generate_sprint5b_guide.py` | Generator for `Sprint 5b - Implementation Guide.docx` |
| `tools/generate_sprint6_guide.py` | Generator for `Sprint 6 - Implementation Guide.docx` |
| `tools/generate_sprint7_guide.py` | Generator for `Sprint 7 - Implementation Guide.docx` |
| `tools/generate_sprint8_guide.py` | Generator for `Sprint 8 - Implementation Guide.docx` |
| `tools/generate_sprint9_guide.py` | Generator for `Sprint 9 - Implementation Guide.docx` |
| `tools/generate_sprint10_guide.py` | Generator for `Sprint 10 - Implementation Guide.docx` |

---

## Roadmap

Delivered:

- ✅ **Sprint 1** — Identity & user management (cookie auth, roles, lockout, SuperAdmin user CRUD)
- ✅ **Sprint 2** — Academic domain (sessions, terms, classes, subjects, periods, timetable grid)
- ✅ **Sprint 3** — Students & parents (pupil profiles, parent directory, linkage, enrolment)
- ✅ **Sprint 4** — Attendance (daily + per-subject registers, submit/reopen lifecycle, summary)
- ✅ **Sprint 5** — Assessments, results & report cards (gradebook, weighted compute, grade bands, term cards)
- ✅ **Sprint 5b** — Student photographs (upload pipeline, reusable avatar component, photos shown across every pupil-facing page)
- ✅ **Sprint 6** — Fees, invoices, receipts & bursar workflows (fee schedules, invoice issuance, multi-allocation payments with refund, bursar dashboard)
- ✅ **Sprint 7** — Store & inventory management for the storekeeper (catalog, supplier directory, movement log with NPS/STK numbering, reversible soft-delete, low-stock dashboard)
- ✅ **Sprint 8** — Parent & student portals + announcements (ward grid + tabbed detail, student dashboard with today's timetable, scoped announcements with per-user read tracking)
- ✅ **Sprint 9** — Auto-provisioned portal accounts (create-parent and create-student now provision the matching ApplicationUser in the Parent / Student role and stamp `Parent.UserId` / `Student.UserId` so the portals light up on first sign-in)
- ✅ **Sprint 10** — Parent-deletion hotfix (`ParentService.SoftDeleteAsync` reads the link count from a fresh DB query instead of the EF Core navigation, so a parent unlinked earlier in the same Blazor circuit can actually be deleted; the linked `ApplicationUser` is retired in the same operation)

Planned for upcoming sprints:

- Notifications (email / SMS push of unread announcements)
- Two-way messaging between school office and families
- Online fee payment (gateway on top of the existing `PaymentService`)
- Audit log viewer


---

## License

Released under the [MIT License](LICENSE) — see the `LICENSE` file at the repo root for the full text. In short: free to use, copy, modify, merge, publish, distribute, sublicense, or sell copies of the software, provided the copyright notice and the permission notice are preserved. The software is provided "as is", without warranty of any kind.
