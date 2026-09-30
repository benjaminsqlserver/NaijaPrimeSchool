using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NaijaPrimeSchool.Domain.Auditing;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Communications;
using NaijaPrimeSchool.Domain.Messaging;

namespace NaijaPrimeSchool.Infrastructure.Persistence;

// Turns the pending changes in a DbContext into AuditEntry rows. Called by
// ApplicationDbContext.SaveChanges after soft-delete conversion, so a
// soft delete shows up as IsDeleted false -> true and is logged as DELETE.
internal static class AuditTrail
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string Restore = "RESTORE";

    private sealed record FieldChange(string Field, string? Old, string? New);

    // Rows not worth (or not safe) to log: the trail itself, high-volume
    // bookkeeping that has its own log, and Identity tokens (secrets).
    private static readonly HashSet<Type> ExcludedTypes =
    [
        typeof(AuditEntry),
        typeof(AnnouncementNotification),
        typeof(AnnouncementRead),
        typeof(IdentityUserToken<Guid>),
    ];

    // Columns that are noise on every row: the audit columns (the trail
    // records who / when itself), IsDeleted (shown as the action) and
    // Identity bookkeeping.
    private static readonly HashSet<string> IgnoredFields =
    [
        nameof(IAuditable.CreatedOn), nameof(IAuditable.CreatedBy),
        nameof(IAuditable.ModifiedOn), nameof(IAuditable.ModifiedBy),
        nameof(ISoftDelete.IsDeleted), nameof(ISoftDelete.DeletedOn), nameof(ISoftDelete.DeletedBy),
        "ConcurrencyStamp", "NormalizedEmail", "NormalizedUserName", "AccessFailedCount",
    ];

    // Per-type noise: read markers move every time someone opens a thread.
    private static readonly Dictionary<Type, HashSet<string>> IgnoredFieldsByType = new()
    {
        [typeof(MessageThread)] = [nameof(MessageThread.FamilyLastReadOn), nameof(MessageThread.StaffLastReadOn)],
    };

    // Secrets: record that they changed, never their value.
    private static readonly HashSet<string> MaskedFields = ["PasswordHash", "SecurityStamp"];
    private const string Masked = "(hidden)";

    // First of these present on the entity labels the row in the log.
    private static readonly string[] LabelProperties =
    [
        "FullName", "InvoiceNumber", "ReceiptNumber", "Reference", "AdmissionNumber",
        "Title", "Subject", "Name", "UserName", "Code", "Email",
    ];

    public static List<AuditEntry> Capture(
        IEnumerable<EntityEntry> entries, IReadOnlyDictionary<string, Guid> actionIds,
        DateTimeOffset now, Guid? userId, string userName)
    {
        var result = new List<AuditEntry>();
        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
            var type = entry.Metadata.ClrType;
            if (ExcludedTypes.Contains(type)) continue;

            var action = ActionFor(entry);
            var changes = ChangesFor(entry, action, type);
            if (action == Update && changes.Count == 0) continue;
            if (!actionIds.TryGetValue(action, out var actionId)) continue;

            result.Add(new AuditEntry
            {
                OccurredOn = now,
                UserId = userId,
                UserName = Clip(userName, 100)!,
                AuditActionId = actionId,
                EntityType = Clip(type.Name, 100)!,
                EntityId = Clip(KeyOf(entry), 100)!,
                EntityLabel = Clip(LabelOf(entry.Entity), 200),
                Changes = JsonSerializer.Serialize(changes),
            });
        }
        return result;
    }

    private static string ActionFor(EntityEntry entry)
    {
        if (entry.State == EntityState.Added) return Create;
        if (entry.State == EntityState.Deleted) return Delete;

        if (entry.Entity is ISoftDelete && entry.Property(nameof(ISoftDelete.IsDeleted)) is { IsModified: true } deleted)
        {
            var was = (bool)(deleted.OriginalValue ?? false);
            var now = (bool)(deleted.CurrentValue ?? false);
            if (!was && now) return Delete;
            if (was && !now) return Restore;
        }
        return Update;
    }

    private static List<FieldChange> ChangesFor(EntityEntry entry, string action, Type type)
    {
        IgnoredFieldsByType.TryGetValue(type, out var typeIgnored);
        var changes = new List<FieldChange>();

        foreach (var p in entry.Properties)
        {
            var name = p.Metadata.Name;
            if (IgnoredFields.Contains(name) || typeIgnored?.Contains(name) == true) continue;
            // Shadow keys / discriminators and the primary key itself (it is
            // already in EntityId) add nothing.
            if (p.Metadata.IsShadowProperty() || p.Metadata.IsPrimaryKey()) continue;

            var masked = MaskedFields.Contains(name);
            switch (action)
            {
                case Create:
                    if (p.CurrentValue is not null)
                        changes.Add(new FieldChange(name, null, masked ? Masked : Format(p.CurrentValue)));
                    break;

                case Delete when entry.State == EntityState.Deleted:
                    // Hard delete (e.g. a role removed from a user): keep what the row held.
                    if (p.OriginalValue is not null)
                        changes.Add(new FieldChange(name, masked ? Masked : Format(p.OriginalValue), null));
                    break;

                default:
                    if (!p.IsModified || Equals(p.OriginalValue, p.CurrentValue)) continue;
                    changes.Add(masked
                        ? new FieldChange(name, Masked, Masked)
                        : new FieldChange(name, Format(p.OriginalValue), Format(p.CurrentValue)));
                    break;
            }
        }
        return changes;
    }

    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return string.Empty;
        return string.Join(",", key.Properties.Select(p =>
            Format(entry.State == EntityState.Deleted ? entry.Property(p.Name).OriginalValue : entry.Property(p.Name).CurrentValue)));
    }

    private static string? LabelOf(object entity)
    {
        var type = entity.GetType();
        foreach (var name in LabelProperties)
        {
            if (type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { PropertyType: var t } prop
                && t == typeof(string)
                && prop.GetValue(entity) is string { Length: > 0 } value)
            {
                return value;
            }
        }
        return null;
    }

    private static string? Format(object? value)
    {
        var text = value switch
        {
            null => null,
            DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
        return Clip(text, 1000);
    }

    private static string? Clip(string? s, int max) => s is null || s.Length <= max ? s : s[..(max - 1)] + "…";
}
