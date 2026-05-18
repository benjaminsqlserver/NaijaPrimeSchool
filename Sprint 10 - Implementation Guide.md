# Sprint 10 — Parent-deletion hotfix

## Purpose

Sprint 9 closed the create-side loop: creating a parent at `/parents/new`
now provisions an `ApplicationUser` in the **Parent** role and stamps
`Parent.UserId` so the portal lights up on first sign-in. Sprint 10
closes a smaller but very visible gap on the **delete** side: a parent
whose pupil links had all been unlinked still could not be deleted from
**Parents → Delete**. The office kept seeing *"Cannot delete a parent
who is still linked to a student. Unlink them first."* even after a
quick check in the database showed zero active `StudentParents` rows
pointing at the parent.

This hotfix replaces the unreliable in-memory link-count guard with a
fresh database query and, while we are there, retires the linked
`ApplicationUser` so the deleted parent cannot keep signing in.

## Acceptance criteria

1. After unlinking every pupil from a parent (via **Edit student →
   Parents → Unlink**), pressing **Delete** on the parent in
   **Parents** succeeds.
2. If at least one active `StudentParent` row still points at the
   parent, the delete is refused with an error that names the actual
   remaining count (e.g. *"Cannot delete this parent because 2 active
   student link(s) remain. Unlink them first."*).
3. When the parent does delete, the linked `ApplicationUser` (if
   any) is also soft-deleted. The parent can no longer sign in to the
   portal.
4. No new tables, no migration, no UI change — the fix is entirely
   behind the `IParentService` boundary.

## Root cause

`ParentService.SoftDeleteAsync` originally eager-loaded the parent
together with its `StudentLinks` navigation:

```csharp
var parent = await db.Parents
    .Include(p => p.StudentLinks)
    .FirstOrDefaultAsync(p => p.Id == id, ct);

if (parent.StudentLinks.Count > 0)
    return OperationResult.Failure(
        "Cannot delete a parent who is still linked to a student. Unlink them first.");
```

EF Core's relationship fix-up stitches navigations from **everything
currently tracked** by the `DbContext`. The global query filter
`!sp.IsDeleted` only filters new `SELECT` statements; it does **not**
prune the in-memory graph. In a Blazor Server circuit the
`ApplicationDbContext` is scoped per request, so two requests in the
same circuit see distinct context instances — but a single request that
loads the parent, calls `Unlink` for several links, and then refreshes
the parent (which both **Edit student** and **Parents** can trigger)
keeps the just-soft-deleted `StudentParent` rows tracked in
`EntityState.Modified` with `IsDeleted = true`. The next `Include(p =>
p.StudentLinks)` pulls a SQL result of zero matching rows, but fix-up
adds the still-tracked soft-deleted rows to the navigation. `.Count`
therefore reports the pre-unlink count, and the delete is refused.

The seeded reproduction that surfaced the bug was the natural office
workflow:

1. Open **Family → Edit student** for a pupil with two parents linked.
2. On the **Parents** tab, click **Unlink** on each row.
3. Navigate to **Family → Parents** and click **Delete** on the parent
   that was just unlinked.
4. Observe the misleading *"…still linked to a student"* error.

## The fix

`ParentService.SoftDeleteAsync` now skips the eager-load entirely and
counts active links with a fresh database query that respects the
global soft-delete filter and ignores the change tracker:

```csharp
var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == id, ct);
if (parent is null) return OperationResult.Failure("Parent not found.");

var activeLinks = await db.StudentParents
    .CountAsync(l => l.ParentId == id, ct);

if (activeLinks > 0)
    return OperationResult.Failure(
        $"Cannot delete this parent because {activeLinks} active student link(s) remain. Unlink them first.");

db.Parents.Remove(parent);
await db.SaveChangesAsync(ct);

if (parent.UserId is { } userId)
{
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is not null)
    {
        await userManager.DeleteAsync(user);
    }
}
```

Three things are now true that were not before:

1. The link count is taken from the database, not the in-memory graph,
   so soft-deleted links never inflate it.
2. The error message includes the real count, which makes it obvious
   when something is actually still linked vs. a stale tracker.
3. The linked `ApplicationUser` is retired in the same operation. Both
   `ApplicationDbContext` and `UserManager<ApplicationUser>` are wired
   to the same scoped DbContext, so `UserManager.DeleteAsync` flows
   through `ApplyAuditAndSoftDelete` and ends up as an `IsDeleted =
   true` update on the **Users** table — preserving audit history while
   blocking future sign-ins.

## Scope (what changed)

| Layer | File | Change |
| --- | --- | --- |
| Infrastructure | `Infrastructure/Services/ParentService.cs` | `SoftDeleteAsync` no longer `Include`s `StudentLinks`; it counts active links via `db.StudentParents.CountAsync(...)` and then retires the linked `ApplicationUser` through `UserManager`. |
| Docs | `README.md` | New Sprint 10 section + roadmap entry + generator-path entry. |
| Docs | `Sprint 10 - Implementation Guide.md` | This markdown companion. |
| Docs | `Sprint 10 - Implementation Guide.docx` | Word walk-through built by the generator below. |
| Tooling | `tools/generate_sprint10_guide.py` | New generator (mirrors `generate_sprint9_guide.py`). |

The DTOs, the **Parents** Razor page, and `IParentService` are all
unchanged — the page already handles `OperationResult.Failure` by
piping the message into a Radzen `Notification`, so the new
count-aware error surfaces without a UI change.

## Why not also fix `StudentService.SoftDeleteAsync`?

The same EF Core relationship-fix-up trap exists for
`StudentService.SoftDeleteAsync`, which guards on
`student.Enrolments.Any(e => e.WithdrawnOn == null)` after a similar
`Include(s => s.Enrolments)`. The bug has not been reported there yet
and the predicate happens to be more forgiving (`WithdrawnOn == null`
typically clears on the same save that withdrew the enrolment), but
the symmetric hotfix is a sensible follow-up sprint.

## How to test

1. Sign in as `superadmin@naijaprimeschool.ng / Admin@12345`.
2. Pick a pupil with at least one parent linked.
3. Open **Family → Students → Edit**, switch to the **Parents** tab,
   and click **Unlink** on every link.
4. Without refreshing the page, navigate to **Family → Parents** and
   click **Delete** on the parent that was just unlinked.
5. The delete completes and the parent disappears from the list.
6. Sign out, then attempt to sign in with that parent's username and
   password (the credentials sprint 9 captured on create). The sign-in
   is refused — the linked `ApplicationUser` is now soft-deleted.
7. As a negative test, link a parent to a pupil and try to delete the
   parent without unlinking. The error message reads *"Cannot delete
   this parent because 1 active student link(s) remain. Unlink them
   first."* — note the count.

## Follow-ups

- Apply the same fresh-query pattern to `StudentService.SoftDeleteAsync`
  for symmetry, and audit any other `Include + .Count`/`.Any` guards
  across the service layer.
- Consider wrapping the parent-delete-then-user-delete pair in an
  explicit `IDbContextTransaction` for atomicity. Today both writes
  share the same EF Core context, so a failure between them is
  observable as *"parent gone but user remains"* — easy to spot in the
  Users page but not automatically reversed.
