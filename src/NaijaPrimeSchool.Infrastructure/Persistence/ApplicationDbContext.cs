using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
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

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<TermType> TermTypes => Set<TermType>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<ClassLevel> ClassLevels => Set<ClassLevel>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<WeekDay> WeekDays => Set<WeekDay>();
    public DbSet<TimetablePeriod> TimetablePeriods => Set<TimetablePeriod>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

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

        ConfigureAcademics(builder);
    }

    private static void ConfigureAcademics(ModelBuilder builder)
    {
        ConfigureLookup<TermType>(builder, "TermTypes", extra: b =>
        {
            b.Property(t => t.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
        });

        ConfigureLookup<ClassLevel>(builder, "ClassLevels", extra: b =>
        {
            b.Property(t => t.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
        });

        ConfigureLookup<WeekDay>(builder, "WeekDays", extra: b =>
        {
            b.Property(d => d.Name).HasMaxLength(20).IsRequired();
            b.Property(d => d.ShortName).HasMaxLength(5).IsRequired();
            b.HasIndex(d => d.Name).IsUnique();
        });

        builder.Entity<Session>(b =>
        {
            b.ToTable("Sessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(40).IsRequired();
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);
            b.HasIndex(s => s.Name).IsUnique();
            b.HasIndex(s => s.IsCurrent);
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<Term>(b =>
        {
            b.ToTable("Terms");
            b.HasKey(t => t.Id);
            b.Property(t => t.CreatedBy).HasMaxLength(100);
            b.Property(t => t.ModifiedBy).HasMaxLength(100);
            b.Property(t => t.DeletedBy).HasMaxLength(100);

            b.HasOne(t => t.Session).WithMany(s => s.Terms)
                .HasForeignKey(t => t.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.TermType).WithMany(tt => tt.Terms)
                .HasForeignKey(t => t.TermTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(t => new { t.SessionId, t.TermTypeId }).IsUnique();
            b.HasIndex(t => t.IsCurrent);
            b.HasIndex(t => t.IsDeleted);
            b.HasQueryFilter(t => !t.IsDeleted);
        });

        builder.Entity<SchoolClass>(b =>
        {
            b.ToTable("SchoolClasses");
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(80).IsRequired();
            b.Property(c => c.Description).HasMaxLength(300);
            b.Property(c => c.CreatedBy).HasMaxLength(100);
            b.Property(c => c.ModifiedBy).HasMaxLength(100);
            b.Property(c => c.DeletedBy).HasMaxLength(100);

            b.HasOne(c => c.ClassLevel).WithMany(cl => cl.Classes)
                .HasForeignKey(c => c.ClassLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.Session).WithMany(s => s.Classes)
                .HasForeignKey(c => c.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.ClassTeacher).WithMany()
                .HasForeignKey(c => c.ClassTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(c => new { c.SessionId, c.Name }).IsUnique();
            b.HasIndex(c => c.IsDeleted);
            b.HasQueryFilter(c => !c.IsDeleted);
        });

        builder.Entity<Subject>(b =>
        {
            b.ToTable("Subjects");
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(80).IsRequired();
            b.Property(s => s.Code).HasMaxLength(10).IsRequired();
            b.Property(s => s.Description).HasMaxLength(300);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);
            b.HasIndex(s => s.Code).IsUnique();
            b.HasIndex(s => s.Name).IsUnique();
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<TimetablePeriod>(b =>
        {
            b.ToTable("TimetablePeriods");
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).HasMaxLength(40).IsRequired();
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.ModifiedBy).HasMaxLength(100);
            b.Property(p => p.DeletedBy).HasMaxLength(100);
            b.HasIndex(p => p.DisplayOrder);
            b.HasIndex(p => p.IsDeleted);
            b.HasQueryFilter(p => !p.IsDeleted);
        });

        builder.Entity<TimetableEntry>(b =>
        {
            b.ToTable("TimetableEntries");
            b.HasKey(e => e.Id);
            b.Property(e => e.Room).HasMaxLength(60);
            b.Property(e => e.Notes).HasMaxLength(300);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.ModifiedBy).HasMaxLength(100);
            b.Property(e => e.DeletedBy).HasMaxLength(100);

            b.HasOne(e => e.Term).WithMany(t => t.TimetableEntries)
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.SchoolClass).WithMany(c => c.TimetableEntries)
                .HasForeignKey(e => e.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Subject).WithMany(s => s.TimetableEntries)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.WeekDay).WithMany(d => d.TimetableEntries)
                .HasForeignKey(e => e.WeekDayId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.TimetablePeriod).WithMany(p => p.TimetableEntries)
                .HasForeignKey(e => e.TimetablePeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Teacher).WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // One subject per (term, class, day, period) slot.
            b.HasIndex(e => new { e.TermId, e.SchoolClassId, e.WeekDayId, e.TimetablePeriodId })
                .IsUnique();
            b.HasIndex(e => e.IsDeleted);
            b.HasQueryFilter(e => !e.IsDeleted);
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
