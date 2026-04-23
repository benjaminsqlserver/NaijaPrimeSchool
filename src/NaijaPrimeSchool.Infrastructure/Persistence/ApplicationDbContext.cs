using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUser currentUser)
    : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid,
        Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>,
        ApplicationUserRole,
        Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>,
        Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>,
        Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>(options)
{
    public DbSet<Gender> Genders => Set<Gender>();
    public DbSet<Title> Titles => Set<Title>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users");
            b.Property(u => u.FirstName).HasMaxLength(80).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(80).IsRequired();
            b.Property(u => u.MiddleName).HasMaxLength(80);
            b.Property(u => u.Address).HasMaxLength(300);
            b.Property(u => u.ProfilePhotoUrl).HasMaxLength(500);
            b.Property(u => u.DeactivationReason).HasMaxLength(300);
            b.Property(u => u.CreatedBy).HasMaxLength(100);
            b.Property(u => u.ModifiedBy).HasMaxLength(100);
            b.Property(u => u.DeletedBy).HasMaxLength(100);
            b.Ignore(u => u.FullName);

            b.HasOne(u => u.Title)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TitleId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(u => u.Gender)
                .WithMany(g => g.Users)
                .HasForeignKey(u => u.GenderId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(u => u.IsDeleted);
            b.HasQueryFilter(u => !u.IsDeleted);
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("Roles");
            b.Property(r => r.Description).HasMaxLength(300);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.ModifiedBy).HasMaxLength(100);
            b.Property(r => r.DeletedBy).HasMaxLength(100);
            b.HasQueryFilter(r => !r.IsDeleted);
        });

        builder.Entity<ApplicationUserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.Property(ur => ur.AssignedBy).HasMaxLength(100);

            b.HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));

        ConfigureLookup<Gender>(builder, "Genders", extra: b =>
        {
            b.Property(g => g.Name).HasMaxLength(50).IsRequired();
            b.Property(g => g.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(g => g.Code).IsUnique();
        });

        ConfigureLookup<Title>(builder, "Titles", extra: b =>
        {
            b.Property(t => t.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
        });
    }

    private static void ConfigureLookup<TEntity>(
        ModelBuilder builder,
        string tableName,
        Action<EntityTypeBuilder<TEntity>>? extra = null) where TEntity : BaseEntity
    {
        builder.Entity<TEntity>(b =>
        {
            b.ToTable(tableName);
            b.HasKey(e => e.Id);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.ModifiedBy).HasMaxLength(100);
            b.Property(e => e.DeletedBy).HasMaxLength(100);
            b.HasQueryFilter(BuildIsNotDeletedExpression<TEntity>());
            extra?.Invoke(b);
        });
    }

    private static Expression<Func<TEntity, bool>> BuildIsNotDeletedExpression<TEntity>()
        where TEntity : ISoftDelete
    {
        return e => !e.IsDeleted;
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditAndSoftDelete()
    {
        var now = DateTimeOffset.UtcNow;
        var userName = currentUser.UserName ?? "system";

        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedOn = auditable.CreatedOn == default ? now : auditable.CreatedOn;
                    auditable.CreatedBy ??= userName;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.ModifiedOn = now;
                    auditable.ModifiedBy = userName;
                }
            }

            if (entry.Entity is ISoftDelete softDelete && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDelete.IsDeleted = true;
                softDelete.DeletedOn = now;
                softDelete.DeletedBy = userName;
            }
        }
    }
}
