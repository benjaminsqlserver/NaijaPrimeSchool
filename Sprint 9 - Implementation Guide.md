# Sprint 9 — Auto-provisioned portal accounts

## Purpose

Sprint 8 shipped the parent and student portals, but each pupil and each
parent record still had to be linked to a login by hand: a `SuperAdmin`
had to create the user under **Users → New user**, remember which
`ApplicationUser.Id` belonged to whom, and then come back and stamp the
matching `Family.Parent.UserId` or `Family.Student.UserId`. Until that
link existed, the portals greeted the family with a friendly *"We can't
find your record"* card and refused to load.

Sprint 9 closes that loop. Creating a parent or a student now creates an
`ApplicationUser` in the correct role (`Parent` or `Student`) in the
**same** unit of work and stamps the new user's id onto the
`UserId` foreign key. The next time the family signs in with those
credentials, the portal loads their wards / their own profile straight
away — no admin follow-up required.

## Acceptance criteria

1. Creating a parent at `/parents/new` also creates an `ApplicationUser`
   in the **Parent** role, and `Parent.UserId` points at that user.
2. Creating a student at `/students/new` also creates an
   `ApplicationUser` in the **Student** role, and `Student.UserId`
   points at that user.
3. When that parent signs in, `/portal/parent` resolves their `ParentId`
   from `Parent.UserId` and renders their wards.
4. When that student signs in, `/portal/student` resolves their
   `StudentId` from `Student.UserId` and renders their dashboard.
5. The existing portal access guard (`CurrentUserCanViewStudentAsync`)
   already trusts the `UserId` link, so no portal-layer changes are
   required — the fix is upstream in the create-parent and
   create-student services.

## Scope (what changed)

| Layer | File | Change |
| --- | --- | --- |
| Application | `Application/Family/Dtos/ParentDtos.cs` | `CreateParentRequest` gains required `UserName` + `Password`; `Email` is now required. |
| Application | `Application/Family/Dtos/StudentDtos.cs` | `CreateStudentRequest` gains required `UserName`, `Email`, `Password`. |
| Infrastructure | `Infrastructure/Services/ParentService.cs` | Constructor takes `UserManager<ApplicationUser>` and `ICurrentUser`; `CreateAsync` provisions the user, assigns the Parent role, and stamps `Parent.UserId`. |
| Infrastructure | `Infrastructure/Services/StudentService.cs` | Same shape as ParentService — provisions a user in the Student role and stamps `Student.UserId`. |
| Web | `Web/Components/Pages/Family/CreateParent.razor` | New *Portal sign-in* section with Username + Password fields; payload includes them. |
| Web | `Web/Components/Pages/Family/CreateStudent.razor` | Same — Username + Email + Password captured on the New student form. |

The portals (`Portals/ParentDashboard.razor`,
`Portals/StudentDashboard.razor`, `Portals/WardDetail.razor`,
`Portals/Student*.razor`) and the underlying `PortalService` were
**not** touched. They already key off `Parent.UserId` /
`Student.UserId`, so once those columns are populated they begin to
resolve correctly.

## Design notes

- **One DbContext, one save**: the new user is created via
  `UserManager.CreateAsync` (which uses the same `ApplicationDbContext`
  registered for Identity), then the role assignment runs, then the
  Parent / Student row is added and `SaveChangesAsync` flushes the
  remaining work. If role assignment fails, the just-created user is
  deleted before returning so we don't orphan a half-built account.
- **Username + Email uniqueness** is checked up-front via
  `UserManager.FindByNameAsync` / `FindByEmailAsync` to give the form
  a clean error message instead of letting Identity throw a generic
  `DuplicateUserName` later.
- **Password policy** matches the one configured in
  `DependencyInjection.cs` for Identity — at least 8 characters with an
  uppercase letter, a lowercase letter, a digit, and a non-alphanumeric
  character. The Razor forms surface that requirement inline.
- **Email becomes required on Parent** because we need a unique address
  for the underlying Identity user (Identity is configured with
  `RequireUniqueEmail = true`). The student form already needed an
  email for the same reason; it is now a first-class field on
  `CreateStudentRequest`.
- **Date of birth on the student user** is normalised through
  `DateOnly.ToDateTime(TimeOnly.MinValue)` because `Student.DateOfBirth`
  is `DateOnly` while `ApplicationUser.DateOfBirth` is `DateTime?`.
- **Role names** are taken from `Domain/Identity/Roles.cs` constants —
  `Roles.Parent` and `Roles.Student` — so there is no string-typo risk.

## Migration / data impact

No EF Core migration is needed:

- `Parent.UserId` and `Student.UserId` already exist (added in
  sprint 3 in anticipation of the portals).
- The seven roles (including `Parent` and `Student`) are seeded on
  startup by `DatabaseInitializer.SeedRolesAsync`.

Existing parent and student rows that were created **before** sprint 9
still have `UserId = NULL`. The SuperAdmin / HeadTeacher can either:

- Edit the corresponding user from `/users` and manually associate it
  (current flow), or
- Re-create the parent / student so a fresh user is provisioned. (Soft
  delete protects history: the old parent / student row is hidden but
  not removed.)

## How to test

1. Sign in as `superadmin@naijaprimeschool.ng`.
2. Navigate to **Family → New parent** (`/parents/new`). Fill in the
   profile, scroll to **Portal sign-in**, and pick a username +
   password.
3. Save. The notification banner says *"Parent '…' created."* and
   redirects to the parent profile.
4. Sign out and sign back in as the new parent's credentials.
5. The sidebar shows the **Parent portal** panel. Click *My wards*. If
   the parent has been linked to one or more pupils via the
   `StudentParent` join table (Family → Students → pick a pupil →
   Linked parents tab), each ward renders as a card; otherwise the
   page prompts the office to link them.
6. Repeat with **Family → New student** and the student's
   credentials — `/portal/student` opens directly on the dashboard.

## Files touched

```text
src/NaijaPrimeSchool.Application/Family/Dtos/ParentDtos.cs
src/NaijaPrimeSchool.Application/Family/Dtos/StudentDtos.cs
src/NaijaPrimeSchool.Infrastructure/Services/ParentService.cs
src/NaijaPrimeSchool.Infrastructure/Services/StudentService.cs
src/NaijaPrimeSchool.Web/Components/Pages/Family/CreateParent.razor
src/NaijaPrimeSchool.Web/Components/Pages/Family/CreateStudent.razor
README.md
Sprint 9 - Implementation Guide.md   (this file)
```

## Verification

- `dotnet build NaijaPrimeSchool.slnx` — succeeds with 0 warnings, 0
  errors.
- New parent flow — `Parent.UserId` is populated; the user has the
  **Parent** role assigned in `AspNetUserRoles`; the parent can sign in
  and load `/portal/parent`.
- New student flow — `Student.UserId` is populated; the user has the
  **Student** role assigned; the student can sign in and load
  `/portal/student`.
