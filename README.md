# Naija Prime School

A modern school management system for Nigerian primary schools, built with **.NET 10**, **Blazor Auto**, **Clean Architecture**, **SQL Server**, and **Radzen Blazor Components**.

Four sprints have shipped. **Sprint 1** delivered the authentication & authorization foundation: user accounts, role-based access control, login/logout, activation/deactivation, and the SuperAdmin user-management screens. **Sprint 2** built the academic domain on top of that foundation: sessions, terms, class arms, subjects, timetable periods, and a click-to-edit weekly timetable grid. **Sprint 3** plugged students and parents into that academic structure: pupil profiles, parent/guardian directory, parent-to-pupil linkage with relationship + primary-contact + pickup flags, and per-session enrolment with a withdrawal lifecycle. **Sprint 4** lands attendance: a daily class register, per-subject session attendance off the timetable, the AttendanceStatus lookup, a submit/reopen lifecycle, and a per-class percentage summary.

Implementation walk-throughs for each sprint live at the repo root:

- `Sprint 1 - Implementation Guide.pdf`
- `Sprint 2 - Implementation Guide.pdf`
- `Sprint 3 - Implementation Guide.pdf`
- `Sprint 4 - Implementation Guide.pdf` 

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

## Cross-cutting (every sprint)

- **Beautiful, inviting UI**
  - Green + gold Nigerian-themed palette
  - Radzen Blazor components (DataGrid, Dialog, Notification, Layout, Sidebar, Forms)
  - Responsive layout
- **Data integrity**
  - No enums; every lookup (roles, titles, genders, term types, class levels, week days, relationships, enrolment statuses, blood groups, marital statuses, attendance statuses, …) is a first-class table
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
│   │   └── Attendance/                          # Daily/Subject registers + AttendanceStatus lookup (sprint 4)
│   ├── NaijaPrimeSchool.Application/            # DTOs, service contracts, shared abstractions
│   │   ├── Common/                              # ICurrentUser, OperationResult
│   │   ├── Users/                               # IUserService, ILookupService, DTOs (sprint 1)
│   │   ├── Academics/                           # I*Service interfaces and DTOs (sprint 2)
│   │   ├── Family/                              # IStudentService, IParentService, IEnrolmentService, DTOs (sprint 3)
│   │   └── Attendance/                          # IDailyAttendanceService, ISubjectAttendanceService, DTOs (sprint 4)
│   ├── NaijaPrimeSchool.Infrastructure/         # EF Core DbContext, Identity stores, service impls, seed, migrations
│   ├── NaijaPrimeSchool.Web/                    # Blazor server host (auth endpoints, layout, pages, Program.cs)
│   │   └── Components/Pages/
│   │       ├── Users/                           # User management pages (sprint 1)
│   │       ├── Academics/                       # Sessions, Terms, Classes, Subjects, Periods, Timetable (sprint 2)
│   │       ├── Family/                          # Students, Parents, Enrolments (sprint 3)
│   │       └── Attendance/                      # Daily, Subject and Summary attendance pages (sprint 4)
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
| `tools/generate_sprint2_guide.py` | Generator for `Sprint 2 - Implementation Guide.docx` |
| `tools/generate_sprint3_guide.py` | Generator for `Sprint 3 - Implementation Guide.docx` |
| `tools/generate_sprint4_guide.py` | Generator for `Sprint 4 - Implementation Guide.docx` |

---

## Roadmap

Delivered:

- ✅ **Sprint 1** — Identity & user management (cookie auth, roles, lockout, SuperAdmin user CRUD)
- ✅ **Sprint 2** — Academic domain (sessions, terms, classes, subjects, periods, timetable grid)
- ✅ **Sprint 3** — Students & parents (pupil profiles, parent directory, linkage, enrolment)
- ✅ **Sprint 4** — Attendance (daily + per-subject registers, submit/reopen lifecycle, summary)

Planned for upcoming sprints:

- Sprint 5 — Assessments, exams, result computation, report cards
- Sprint 6 — Fees, invoices, receipts, bursar workflows
- Sprint 7 — Store & inventory management for the storekeeper
- Sprint 8 — Parent and student portals
- Notifications (email / SMS)
- Audit log viewer


---

## License

This project is provided as-is for educational and evaluation purposes. Add the license of your choice before publishing.
