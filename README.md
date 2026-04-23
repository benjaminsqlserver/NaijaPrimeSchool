# Naija Prime School

A modern school management system for Nigerian primary schools, built with **.NET 10**, **Blazor Auto**, **Clean Architecture**, **SQL Server**, and **Radzen Blazor Components**.

This first sprint delivers the authentication & authorization foundation: user accounts, role-based access control, login/logout, activation/deactivation, and the user management screens that the rest of the system will build on.

---

## Features (this sprint)

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
- **Beautiful, inviting UI**
  - Green + gold Nigerian-themed palette
  - Radzen Blazor components (DataGrid, Dialog, Notification, Layout, Sidebar, Forms)
  - Responsive layout
- **Data integrity**
  - No enums; every lookup (roles, titles, genders) is a first-class table
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
│   ├── NaijaPrimeSchool.Domain/          # Entities, base types, interfaces. No dependencies on infra.
│   ├── NaijaPrimeSchool.Application/     # DTOs, service contracts, shared abstractions (ICurrentUser, OperationResult)
│   ├── NaijaPrimeSchool.Infrastructure/  # EF Core DbContext, SQL Server, Identity, service implementations, seed & migrations
│   ├── NaijaPrimeSchool.Web/             # Blazor server host (auth endpoints, layout, pages, Program.cs)
│   └── NaijaPrimeSchool.Web.Client/      # Blazor WebAssembly client (Auto interactivity)
└── NaijaPrimeSchool.sln
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
| `src/NaijaPrimeSchool.Web/wwwroot/app.css` | Green + gold school theme |

---

## Roadmap

Planned for upcoming sprints:

- Academic domain: sessions, terms, classes, subjects, timetables
- Students & parents (enrolment, linkage, guardians)
- Attendance (daily + per-subject)
- Assessments, exams, result computation, report cards
- Fees, invoices, receipts, bursar workflows
- Store & inventory management for the storekeeper
- Parent and student portals
- Notifications (email / SMS)
- Audit log viewer

---

## Contributing

1. Fork the repo and create a feature branch.
2. Follow the existing Clean Architecture boundaries — no Razor components in `Application` or `Domain`, no EF Core in `Application`.
3. Run `dotnet build` and ensure there are no warnings.
4. Open a pull request describing the change and linking any relevant issues.

---

## License

This project is provided as-is for educational and evaluation purposes. Add the license of your choice before publishing.
