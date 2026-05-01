using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
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

    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<EnrolmentStatus> EnrolmentStatuses => Set<EnrolmentStatus>();
    public DbSet<BloodGroup> BloodGroups => Set<BloodGroup>();
    public DbSet<MaritalStatus> MaritalStatuses => Set<MaritalStatus>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<StudentParent> StudentParents => Set<StudentParent>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    public DbSet<AttendanceStatus> AttendanceStatuses => Set<AttendanceStatus>();
    public DbSet<DailyAttendanceRegister> DailyAttendanceRegisters => Set<DailyAttendanceRegister>();
    public DbSet<DailyAttendanceEntry> DailyAttendanceEntries => Set<DailyAttendanceEntry>();
    public DbSet<SubjectAttendanceSession> SubjectAttendanceSessions => Set<SubjectAttendanceSession>();
    public DbSet<SubjectAttendanceEntry> SubjectAttendanceEntries => Set<SubjectAttendanceEntry>();

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
        ConfigureFamily(builder);
        ConfigureAttendance(builder);
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

    private static void ConfigureFamily(ModelBuilder builder)
    {
        ConfigureLookup<Relationship>(builder, "Relationships", extra: b =>
        {
            b.Property(r => r.Name).HasMaxLength(40).IsRequired();
            b.HasIndex(r => r.Name).IsUnique();
        });

        ConfigureLookup<EnrolmentStatus>(builder, "EnrolmentStatuses", extra: b =>
        {
            b.Property(s => s.Name).HasMaxLength(40).IsRequired();
            b.HasIndex(s => s.Name).IsUnique();
        });

        ConfigureLookup<BloodGroup>(builder, "BloodGroups", extra: b =>
        {
            b.Property(g => g.Name).HasMaxLength(10).IsRequired();
            b.HasIndex(g => g.Name).IsUnique();
        });

        ConfigureLookup<MaritalStatus>(builder, "MaritalStatuses", extra: b =>
        {
            b.Property(m => m.Name).HasMaxLength(40).IsRequired();
            b.HasIndex(m => m.Name).IsUnique();
        });

        builder.Entity<Student>(b =>
        {
            b.ToTable("Students");
            b.HasKey(s => s.Id);
            b.Property(s => s.AdmissionNumber).HasMaxLength(30).IsRequired();
            b.Property(s => s.FirstName).HasMaxLength(80).IsRequired();
            b.Property(s => s.LastName).HasMaxLength(80).IsRequired();
            b.Property(s => s.MiddleName).HasMaxLength(80);
            b.Property(s => s.StateOfOrigin).HasMaxLength(80);
            b.Property(s => s.ResidentialAddress).HasMaxLength(300);
            b.Property(s => s.PhotoUrl).HasMaxLength(500);
            b.Property(s => s.Allergies).HasMaxLength(500);
            b.Property(s => s.MedicalNotes).HasMaxLength(1000);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);
            b.Ignore(s => s.FullName);

            b.HasOne(s => s.Gender).WithMany()
                .HasForeignKey(s => s.GenderId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(s => s.BloodGroup).WithMany(g => g.Students)
                .HasForeignKey(s => s.BloodGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(s => s.User).WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(s => s.AdmissionNumber).IsUnique();
            b.HasIndex(s => s.UserId).IsUnique()
                .HasFilter("[UserId] IS NOT NULL");
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<Parent>(b =>
        {
            b.ToTable("Parents");
            b.HasKey(p => p.Id);
            b.Property(p => p.FirstName).HasMaxLength(80).IsRequired();
            b.Property(p => p.LastName).HasMaxLength(80).IsRequired();
            b.Property(p => p.MiddleName).HasMaxLength(80);
            b.Property(p => p.PrimaryPhone).HasMaxLength(30);
            b.Property(p => p.AlternatePhone).HasMaxLength(30);
            b.Property(p => p.Email).HasMaxLength(256);
            b.Property(p => p.ResidentialAddress).HasMaxLength(300);
            b.Property(p => p.Occupation).HasMaxLength(120);
            b.Property(p => p.Employer).HasMaxLength(120);
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.ModifiedBy).HasMaxLength(100);
            b.Property(p => p.DeletedBy).HasMaxLength(100);
            b.Ignore(p => p.FullName);

            b.HasOne(p => p.Title).WithMany()
                .HasForeignKey(p => p.TitleId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(p => p.Gender).WithMany()
                .HasForeignKey(p => p.GenderId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(p => p.MaritalStatus).WithMany(m => m.Parents)
                .HasForeignKey(p => p.MaritalStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(p => p.User).WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(p => p.UserId).IsUnique()
                .HasFilter("[UserId] IS NOT NULL");
            b.HasIndex(p => p.IsDeleted);
            b.HasQueryFilter(p => !p.IsDeleted);
        });

        builder.Entity<StudentParent>(b =>
        {
            b.ToTable("StudentParents");
            b.HasKey(sp => sp.Id);
            b.Property(sp => sp.Notes).HasMaxLength(300);
            b.Property(sp => sp.CreatedBy).HasMaxLength(100);
            b.Property(sp => sp.ModifiedBy).HasMaxLength(100);
            b.Property(sp => sp.DeletedBy).HasMaxLength(100);

            b.HasOne(sp => sp.Student).WithMany(s => s.ParentLinks)
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(sp => sp.Parent).WithMany(p => p.StudentLinks)
                .HasForeignKey(sp => sp.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(sp => sp.Relationship).WithMany(r => r.StudentLinks)
                .HasForeignKey(sp => sp.RelationshipId)
                .OnDelete(DeleteBehavior.Restrict);

            // A given parent should appear at most once per student.
            b.HasIndex(sp => new { sp.StudentId, sp.ParentId }).IsUnique();
            b.HasIndex(sp => sp.IsDeleted);
            b.HasQueryFilter(sp => !sp.IsDeleted);
        });

        builder.Entity<Enrolment>(b =>
        {
            b.ToTable("Enrolments");
            b.HasKey(e => e.Id);
            b.Property(e => e.Notes).HasMaxLength(500);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.ModifiedBy).HasMaxLength(100);
            b.Property(e => e.DeletedBy).HasMaxLength(100);

            b.HasOne(e => e.Student).WithMany(s => s.Enrolments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.SchoolClass).WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.EnrolmentStatus).WithMany(s => s.Enrolments)
                .HasForeignKey(e => e.EnrolmentStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // A student appears at most once in a given class.
            b.HasIndex(e => new { e.StudentId, e.SchoolClassId }).IsUnique();
            b.HasIndex(e => e.IsDeleted);
            b.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private static void ConfigureAttendance(ModelBuilder builder)
    {
        ConfigureLookup<AttendanceStatus>(builder, "AttendanceStatuses", extra: b =>
        {
            b.Property(s => s.Name).HasMaxLength(40).IsRequired();
            b.Property(s => s.Code).HasMaxLength(5).IsRequired();
            b.HasIndex(s => s.Name).IsUnique();
            b.HasIndex(s => s.Code).IsUnique();
        });

        builder.Entity<DailyAttendanceRegister>(b =>
        {
            b.ToTable("DailyAttendanceRegisters");
            b.HasKey(r => r.Id);
            b.Property(r => r.Notes).HasMaxLength(500);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.ModifiedBy).HasMaxLength(100);
            b.Property(r => r.DeletedBy).HasMaxLength(100);

            b.HasOne(r => r.SchoolClass).WithMany(c => c.DailyAttendanceRegisters)
                .HasForeignKey(r => r.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.Term).WithMany(t => t.DailyAttendanceRegisters)
                .HasForeignKey(r => r.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.TakenBy).WithMany()
                .HasForeignKey(r => r.TakenById)
                .OnDelete(DeleteBehavior.SetNull);

            // One register per (class, date).
            b.HasIndex(r => new { r.SchoolClassId, r.Date }).IsUnique();
            b.HasIndex(r => r.Date);
            b.HasIndex(r => r.IsDeleted);
            b.HasQueryFilter(r => !r.IsDeleted);
        });

        builder.Entity<DailyAttendanceEntry>(b =>
        {
            b.ToTable("DailyAttendanceEntries");
            b.HasKey(e => e.Id);
            b.Property(e => e.Remarks).HasMaxLength(300);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.ModifiedBy).HasMaxLength(100);
            b.Property(e => e.DeletedBy).HasMaxLength(100);

            b.HasOne(e => e.Register).WithMany(r => r.Entries)
                .HasForeignKey(e => e.RegisterId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Student).WithMany(s => s.DailyAttendanceEntries)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.AttendanceStatus).WithMany(s => s.DailyEntries)
                .HasForeignKey(e => e.AttendanceStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // One entry per (register, student).
            b.HasIndex(e => new { e.RegisterId, e.StudentId }).IsUnique();
            b.HasIndex(e => e.IsDeleted);
            b.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<SubjectAttendanceSession>(b =>
        {
            b.ToTable("SubjectAttendanceSessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.Notes).HasMaxLength(500);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);

            b.HasOne(s => s.TimetableEntry).WithMany(e => e.AttendanceSessions)
                .HasForeignKey(s => s.TimetableEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.TakenBy).WithMany()
                .HasForeignKey(s => s.TakenById)
                .OnDelete(DeleteBehavior.SetNull);

            // One session per (timetable entry, date).
            b.HasIndex(s => new { s.TimetableEntryId, s.Date }).IsUnique();
            b.HasIndex(s => s.Date);
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<SubjectAttendanceEntry>(b =>
        {
            b.ToTable("SubjectAttendanceEntries");
            b.HasKey(e => e.Id);
            b.Property(e => e.Remarks).HasMaxLength(300);
            b.Property(e => e.CreatedBy).HasMaxLength(100);
            b.Property(e => e.ModifiedBy).HasMaxLength(100);
            b.Property(e => e.DeletedBy).HasMaxLength(100);

            b.HasOne(e => e.Session).WithMany(s => s.Entries)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Student).WithMany(s => s.SubjectAttendanceEntries)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.AttendanceStatus).WithMany(s => s.SubjectEntries)
                .HasForeignKey(e => e.AttendanceStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // One entry per (session, student).
            b.HasIndex(e => new { e.SessionId, e.StudentId }).IsUnique();
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
