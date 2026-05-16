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
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Domain.Inventory;
using NaijaPrimeSchool.Domain.Results;

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

    public DbSet<AssessmentType> AssessmentTypes => Set<AssessmentType>();
    public DbSet<GradeBand> GradeBands => Set<GradeBand>();
    public DbSet<AffectiveTrait> AffectiveTraits => Set<AffectiveTrait>();
    public DbSet<PsychomotorSkill> PsychomotorSkills => Set<PsychomotorSkill>();
    public DbSet<TraitRating> TraitRatings => Set<TraitRating>();
    public DbSet<TermAssessment> TermAssessments => Set<TermAssessment>();
    public DbSet<AssessmentScore> AssessmentScores => Set<AssessmentScore>();
    public DbSet<SubjectResult> SubjectResults => Set<SubjectResult>();
    public DbSet<ReportCard> ReportCards => Set<ReportCard>();
    public DbSet<AffectiveRating> AffectiveRatings => Set<AffectiveRating>();
    public DbSet<PsychomotorRating> PsychomotorRatings => Set<PsychomotorRating>();

    public DbSet<FeeCategory> FeeCategories => Set<FeeCategory>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<InvoiceStatus> InvoiceStatuses => Set<InvoiceStatus>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();
    public DbSet<FeeScheduleItem> FeeScheduleItems => Set<FeeScheduleItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<StockMovementType> StockMovementTypes => Set<StockMovementType>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StoreItem> StoreItems => Set<StoreItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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
        ConfigureResults(builder);
        ConfigureFinance(builder);
        ConfigureInventory(builder);
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

    private static void ConfigureResults(ModelBuilder builder)
    {
        ConfigureLookup<AssessmentType>(builder, "AssessmentTypes", extra: b =>
        {
            b.Property(t => t.Name).HasMaxLength(50).IsRequired();
            b.Property(t => t.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
            b.HasIndex(t => t.Code).IsUnique();
        });

        ConfigureLookup<GradeBand>(builder, "GradeBands", extra: b =>
        {
            b.Property(g => g.Name).HasMaxLength(20).IsRequired();
            b.Property(g => g.Description).HasMaxLength(80).IsRequired();
            b.Property(g => g.Remark).HasMaxLength(120);
            b.Property(g => g.LowerBound).HasPrecision(5, 2);
            b.Property(g => g.UpperBound).HasPrecision(5, 2);
            b.HasIndex(g => g.Name).IsUnique();
        });

        ConfigureLookup<AffectiveTrait>(builder, "AffectiveTraits", extra: b =>
        {
            b.Property(t => t.Name).HasMaxLength(60).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
        });

        ConfigureLookup<PsychomotorSkill>(builder, "PsychomotorSkills", extra: b =>
        {
            b.Property(s => s.Name).HasMaxLength(60).IsRequired();
            b.HasIndex(s => s.Name).IsUnique();
        });

        ConfigureLookup<TraitRating>(builder, "TraitRatings", extra: b =>
        {
            b.Property(r => r.Name).HasMaxLength(40).IsRequired();
            b.HasIndex(r => r.Name).IsUnique();
            b.HasIndex(r => r.Value).IsUnique();
        });

        builder.Entity<TermAssessment>(b =>
        {
            b.ToTable("TermAssessments");
            b.HasKey(a => a.Id);
            b.Property(a => a.Title).HasMaxLength(120).IsRequired();
            b.Property(a => a.Notes).HasMaxLength(500);
            b.Property(a => a.Weight).HasPrecision(5, 2);
            b.Property(a => a.CreatedBy).HasMaxLength(100);
            b.Property(a => a.ModifiedBy).HasMaxLength(100);
            b.Property(a => a.DeletedBy).HasMaxLength(100);

            b.HasOne(a => a.Term).WithMany(t => t.TermAssessments)
                .HasForeignKey(a => a.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.SchoolClass).WithMany(c => c.TermAssessments)
                .HasForeignKey(a => a.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Subject).WithMany(s => s.TermAssessments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.AssessmentType).WithMany(t => t.TermAssessments)
                .HasForeignKey(a => a.AssessmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(a => new { a.TermId, a.SchoolClassId, a.SubjectId });
            b.HasIndex(a => a.IsDeleted);
            b.HasQueryFilter(a => !a.IsDeleted);
        });

        builder.Entity<AssessmentScore>(b =>
        {
            b.ToTable("AssessmentScores");
            b.HasKey(s => s.Id);
            b.Property(s => s.Score).HasPrecision(7, 2);
            b.Property(s => s.Remarks).HasMaxLength(300);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);

            b.HasOne(s => s.TermAssessment).WithMany(a => a.Scores)
                .HasForeignKey(s => s.TermAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Student).WithMany(st => st.AssessmentScores)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // One score per (assessment, student).
            b.HasIndex(s => new { s.TermAssessmentId, s.StudentId }).IsUnique();
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<SubjectResult>(b =>
        {
            b.ToTable("SubjectResults");
            b.HasKey(r => r.Id);
            b.Property(r => r.TotalScore).HasPrecision(7, 2);
            b.Property(r => r.Percentage).HasPrecision(5, 2);
            b.Property(r => r.TeacherComment).HasMaxLength(500);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.ModifiedBy).HasMaxLength(100);
            b.Property(r => r.DeletedBy).HasMaxLength(100);

            b.HasOne(r => r.Student).WithMany(s => s.SubjectResults)
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.Term).WithMany(t => t.SubjectResults)
                .HasForeignKey(r => r.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.Subject).WithMany(s => s.SubjectResults)
                .HasForeignKey(r => r.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.SchoolClass).WithMany(c => c.SubjectResults)
                .HasForeignKey(r => r.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.GradeBand).WithMany(g => g.SubjectResults)
                .HasForeignKey(r => r.GradeBandId)
                .OnDelete(DeleteBehavior.SetNull);

            // One result per (student, term, subject).
            b.HasIndex(r => new { r.StudentId, r.TermId, r.SubjectId }).IsUnique();
            b.HasIndex(r => new { r.SchoolClassId, r.TermId, r.SubjectId });
            b.HasIndex(r => r.IsDeleted);
            b.HasQueryFilter(r => !r.IsDeleted);
        });

        builder.Entity<ReportCard>(b =>
        {
            b.ToTable("ReportCards");
            b.HasKey(c => c.Id);
            b.Property(c => c.TotalScore).HasPrecision(7, 2);
            b.Property(c => c.AveragePercentage).HasPrecision(5, 2);
            b.Property(c => c.ClassTeacherComment).HasMaxLength(1000);
            b.Property(c => c.HeadTeacherComment).HasMaxLength(1000);
            b.Property(c => c.CreatedBy).HasMaxLength(100);
            b.Property(c => c.ModifiedBy).HasMaxLength(100);
            b.Property(c => c.DeletedBy).HasMaxLength(100);

            b.HasOne(c => c.Student).WithMany(s => s.ReportCards)
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.Term).WithMany(t => t.ReportCards)
                .HasForeignKey(c => c.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.SchoolClass).WithMany(sc => sc.ReportCards)
                .HasForeignKey(c => c.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // One report card per (student, term).
            b.HasIndex(c => new { c.StudentId, c.TermId }).IsUnique();
            b.HasIndex(c => new { c.SchoolClassId, c.TermId });
            b.HasIndex(c => c.IsDeleted);
            b.HasQueryFilter(c => !c.IsDeleted);
        });

        builder.Entity<AffectiveRating>(b =>
        {
            b.ToTable("AffectiveRatings");
            b.HasKey(r => r.Id);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.ModifiedBy).HasMaxLength(100);
            b.Property(r => r.DeletedBy).HasMaxLength(100);

            b.HasOne(r => r.ReportCard).WithMany(c => c.AffectiveRatings)
                .HasForeignKey(r => r.ReportCardId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.AffectiveTrait).WithMany(t => t.Ratings)
                .HasForeignKey(r => r.AffectiveTraitId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.TraitRating).WithMany(tr => tr.AffectiveRatings)
                .HasForeignKey(r => r.TraitRatingId)
                .OnDelete(DeleteBehavior.Restrict);

            // One rating per (report card, trait).
            b.HasIndex(r => new { r.ReportCardId, r.AffectiveTraitId }).IsUnique();
            b.HasIndex(r => r.IsDeleted);
            b.HasQueryFilter(r => !r.IsDeleted);
        });

        builder.Entity<PsychomotorRating>(b =>
        {
            b.ToTable("PsychomotorRatings");
            b.HasKey(r => r.Id);
            b.Property(r => r.CreatedBy).HasMaxLength(100);
            b.Property(r => r.ModifiedBy).HasMaxLength(100);
            b.Property(r => r.DeletedBy).HasMaxLength(100);

            b.HasOne(r => r.ReportCard).WithMany(c => c.PsychomotorRatings)
                .HasForeignKey(r => r.ReportCardId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.PsychomotorSkill).WithMany(s => s.Ratings)
                .HasForeignKey(r => r.PsychomotorSkillId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.TraitRating).WithMany(tr => tr.PsychomotorRatings)
                .HasForeignKey(r => r.TraitRatingId)
                .OnDelete(DeleteBehavior.Restrict);

            // One rating per (report card, skill).
            b.HasIndex(r => new { r.ReportCardId, r.PsychomotorSkillId }).IsUnique();
            b.HasIndex(r => r.IsDeleted);
            b.HasQueryFilter(r => !r.IsDeleted);
        });
    }

    private static void ConfigureFinance(ModelBuilder builder)
    {
        ConfigureLookup<FeeCategory>(builder, "FeeCategories", extra: b =>
        {
            b.Property(c => c.Name).HasMaxLength(60).IsRequired();
            b.Property(c => c.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(c => c.Name).IsUnique();
            b.HasIndex(c => c.Code).IsUnique();
        });

        ConfigureLookup<PaymentMethod>(builder, "PaymentMethods", extra: b =>
        {
            b.Property(m => m.Name).HasMaxLength(60).IsRequired();
            b.Property(m => m.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(m => m.Name).IsUnique();
            b.HasIndex(m => m.Code).IsUnique();
        });

        ConfigureLookup<InvoiceStatus>(builder, "InvoiceStatuses", extra: b =>
        {
            b.Property(s => s.Name).HasMaxLength(40).IsRequired();
            b.Property(s => s.Code).HasMaxLength(20).IsRequired();
            b.HasIndex(s => s.Name).IsUnique();
            b.HasIndex(s => s.Code).IsUnique();
        });

        ConfigureLookup<PaymentStatus>(builder, "PaymentStatuses", extra: b =>
        {
            b.Property(s => s.Name).HasMaxLength(40).IsRequired();
            b.Property(s => s.Code).HasMaxLength(20).IsRequired();
            b.HasIndex(s => s.Name).IsUnique();
            b.HasIndex(s => s.Code).IsUnique();
        });

        builder.Entity<FeeSchedule>(b =>
        {
            b.ToTable("FeeSchedules");
            b.HasKey(s => s.Id);
            b.Property(s => s.Title).HasMaxLength(120).IsRequired();
            b.Property(s => s.Notes).HasMaxLength(500);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);

            b.HasOne(s => s.Term).WithMany(t => t.FeeSchedules)
                .HasForeignKey(s => s.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.ClassLevel).WithMany(cl => cl.FeeSchedules)
                .HasForeignKey(s => s.ClassLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(s => new { s.TermId, s.ClassLevelId }).IsUnique();
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<FeeScheduleItem>(b =>
        {
            b.ToTable("FeeScheduleItems");
            b.HasKey(i => i.Id);
            b.Property(i => i.Description).HasMaxLength(160).IsRequired();
            b.Property(i => i.Amount).HasPrecision(12, 2);
            b.Property(i => i.CreatedBy).HasMaxLength(100);
            b.Property(i => i.ModifiedBy).HasMaxLength(100);
            b.Property(i => i.DeletedBy).HasMaxLength(100);

            b.HasOne(i => i.FeeSchedule).WithMany(s => s.Items)
                .HasForeignKey(i => i.FeeScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.FeeCategory).WithMany(c => c.ScheduleItems)
                .HasForeignKey(i => i.FeeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(i => new { i.FeeScheduleId, i.DisplayOrder });
            b.HasIndex(i => i.IsDeleted);
            b.HasQueryFilter(i => !i.IsDeleted);
        });

        builder.Entity<Invoice>(b =>
        {
            b.ToTable("Invoices");
            b.HasKey(i => i.Id);
            b.Property(i => i.InvoiceNumber).HasMaxLength(40).IsRequired();
            b.Property(i => i.Notes).HasMaxLength(500);
            b.Property(i => i.Subtotal).HasPrecision(12, 2);
            b.Property(i => i.DiscountTotal).HasPrecision(12, 2);
            b.Property(i => i.AmountDue).HasPrecision(12, 2);
            b.Property(i => i.AmountPaid).HasPrecision(12, 2);
            b.Property(i => i.CreatedBy).HasMaxLength(100);
            b.Property(i => i.ModifiedBy).HasMaxLength(100);
            b.Property(i => i.DeletedBy).HasMaxLength(100);
            b.Ignore(i => i.Balance);

            b.HasOne(i => i.Student).WithMany(s => s.Invoices)
                .HasForeignKey(i => i.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.Term).WithMany(t => t.Invoices)
                .HasForeignKey(i => i.TermId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.SchoolClass).WithMany(c => c.Invoices)
                .HasForeignKey(i => i.SchoolClassId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.InvoiceStatus).WithMany(s => s.Invoices)
                .HasForeignKey(i => i.InvoiceStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(i => i.InvoiceNumber).IsUnique();
            b.HasIndex(i => new { i.StudentId, i.TermId });
            b.HasIndex(i => i.IsDeleted);
            b.HasQueryFilter(i => !i.IsDeleted);
        });

        builder.Entity<InvoiceLine>(b =>
        {
            b.ToTable("InvoiceLines");
            b.HasKey(l => l.Id);
            b.Property(l => l.Description).HasMaxLength(160).IsRequired();
            b.Property(l => l.Amount).HasPrecision(12, 2);
            b.Property(l => l.Discount).HasPrecision(12, 2);
            b.Property(l => l.CreatedBy).HasMaxLength(100);
            b.Property(l => l.ModifiedBy).HasMaxLength(100);
            b.Property(l => l.DeletedBy).HasMaxLength(100);
            b.Ignore(l => l.LineTotal);

            b.HasOne(l => l.Invoice).WithMany(i => i.Lines)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(l => l.FeeCategory).WithMany(c => c.InvoiceLines)
                .HasForeignKey(l => l.FeeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(l => l.FeeScheduleItem).WithMany()
                .HasForeignKey(l => l.FeeScheduleItemId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(l => l.IsDeleted);
            b.HasQueryFilter(l => !l.IsDeleted);
        });

        builder.Entity<Payment>(b =>
        {
            b.ToTable("Payments");
            b.HasKey(p => p.Id);
            b.Property(p => p.ReceiptNumber).HasMaxLength(40).IsRequired();
            b.Property(p => p.Amount).HasPrecision(12, 2);
            b.Property(p => p.Reference).HasMaxLength(120);
            b.Property(p => p.Notes).HasMaxLength(300);
            b.Property(p => p.CreatedBy).HasMaxLength(100);
            b.Property(p => p.ModifiedBy).HasMaxLength(100);
            b.Property(p => p.DeletedBy).HasMaxLength(100);

            b.HasOne(p => p.Student).WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.PaymentMethod).WithMany(m => m.Payments)
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.PaymentStatus).WithMany(s => s.Payments)
                .HasForeignKey(p => p.PaymentStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.CollectedBy).WithMany()
                .HasForeignKey(p => p.CollectedById)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(p => p.ReceiptNumber).IsUnique();
            b.HasIndex(p => p.PaidOn);
            b.HasIndex(p => p.IsDeleted);
            b.HasQueryFilter(p => !p.IsDeleted);
        });

        builder.Entity<PaymentAllocation>(b =>
        {
            b.ToTable("PaymentAllocations");
            b.HasKey(a => a.Id);
            b.Property(a => a.AmountApplied).HasPrecision(12, 2);
            b.Property(a => a.CreatedBy).HasMaxLength(100);
            b.Property(a => a.ModifiedBy).HasMaxLength(100);
            b.Property(a => a.DeletedBy).HasMaxLength(100);

            b.HasOne(a => a.Payment).WithMany(p => p.Allocations)
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Invoice).WithMany(i => i.Allocations)
                .HasForeignKey(a => a.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // One allocation row per (Payment, Invoice) pair.
            b.HasIndex(a => new { a.PaymentId, a.InvoiceId }).IsUnique();
            b.HasIndex(a => a.IsDeleted);
            b.HasQueryFilter(a => !a.IsDeleted);
        });
    }

    private static void ConfigureInventory(ModelBuilder builder)
    {
        ConfigureLookup<ItemCategory>(builder, "ItemCategories", extra: b =>
        {
            b.Property(c => c.Name).HasMaxLength(60).IsRequired();
            b.Property(c => c.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(c => c.Name).IsUnique();
            b.HasIndex(c => c.Code).IsUnique();
        });

        ConfigureLookup<UnitOfMeasure>(builder, "UnitsOfMeasure", extra: b =>
        {
            b.Property(u => u.Name).HasMaxLength(40).IsRequired();
            b.Property(u => u.Code).HasMaxLength(10).IsRequired();
            b.HasIndex(u => u.Name).IsUnique();
            b.HasIndex(u => u.Code).IsUnique();
        });

        ConfigureLookup<StockMovementType>(builder, "StockMovementTypes", extra: b =>
        {
            b.Property(t => t.Name).HasMaxLength(40).IsRequired();
            b.Property(t => t.Code).HasMaxLength(20).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
            b.HasIndex(t => t.Code).IsUnique();
        });

        builder.Entity<Supplier>(b =>
        {
            b.ToTable("Suppliers");
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(120).IsRequired();
            b.Property(s => s.ContactName).HasMaxLength(120);
            b.Property(s => s.Phone).HasMaxLength(30);
            b.Property(s => s.Email).HasMaxLength(256);
            b.Property(s => s.Address).HasMaxLength(300);
            b.Property(s => s.Notes).HasMaxLength(500);
            b.Property(s => s.CreatedBy).HasMaxLength(100);
            b.Property(s => s.ModifiedBy).HasMaxLength(100);
            b.Property(s => s.DeletedBy).HasMaxLength(100);

            b.HasIndex(s => s.Name);
            b.HasIndex(s => s.IsDeleted);
            b.HasQueryFilter(s => !s.IsDeleted);
        });

        builder.Entity<StoreItem>(b =>
        {
            b.ToTable("StoreItems");
            b.HasKey(i => i.Id);
            b.Property(i => i.Name).HasMaxLength(120).IsRequired();
            b.Property(i => i.Sku).HasMaxLength(60);
            b.Property(i => i.Description).HasMaxLength(500);
            b.Property(i => i.QuantityOnHand).HasPrecision(14, 3);
            b.Property(i => i.ReorderLevel).HasPrecision(14, 3);
            b.Property(i => i.LastUnitCost).HasPrecision(12, 2);
            b.Property(i => i.CreatedBy).HasMaxLength(100);
            b.Property(i => i.ModifiedBy).HasMaxLength(100);
            b.Property(i => i.DeletedBy).HasMaxLength(100);

            b.HasOne(i => i.ItemCategory).WithMany(c => c.Items)
                .HasForeignKey(i => i.ItemCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.UnitOfMeasure).WithMany(u => u.Items)
                .HasForeignKey(i => i.UnitOfMeasureId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(i => i.Name);
            b.HasIndex(i => i.Sku).IsUnique()
                .HasFilter("[Sku] IS NOT NULL");
            b.HasIndex(i => i.IsDeleted);
            b.HasQueryFilter(i => !i.IsDeleted);
        });

        builder.Entity<StockMovement>(b =>
        {
            b.ToTable("StockMovements");
            b.HasKey(m => m.Id);
            b.Property(m => m.MovementNumber).HasMaxLength(40).IsRequired();
            b.Property(m => m.Quantity).HasPrecision(14, 3);
            b.Property(m => m.UnitCost).HasPrecision(12, 2);
            b.Property(m => m.TotalCost).HasPrecision(14, 2);
            b.Property(m => m.Reference).HasMaxLength(120);
            b.Property(m => m.Notes).HasMaxLength(300);
            b.Property(m => m.CreatedBy).HasMaxLength(100);
            b.Property(m => m.ModifiedBy).HasMaxLength(100);
            b.Property(m => m.DeletedBy).HasMaxLength(100);

            b.HasOne(m => m.StoreItem).WithMany(i => i.Movements)
                .HasForeignKey(m => m.StoreItemId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.StockMovementType).WithMany(t => t.Movements)
                .HasForeignKey(m => m.StockMovementTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.ReceivedFromSupplier).WithMany(s => s.Purchases)
                .HasForeignKey(m => m.ReceivedFromSupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(m => m.IssuedToStudent).WithMany(s => s.StockIssuances)
                .HasForeignKey(m => m.IssuedToStudentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(m => m.IssuedToSchoolClass).WithMany(c => c.StockIssuances)
                .HasForeignKey(m => m.IssuedToSchoolClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Both FKs land on Users. SQL Server refuses more than one
            // cascade/SetNull path from a single child table to the same
            // parent, so these are Restrict. Users are soft-deleted in this
            // app, so the FK rule never actually fires.
            b.HasOne(m => m.IssuedToUser).WithMany()
                .HasForeignKey(m => m.IssuedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.PerformedBy).WithMany()
                .HasForeignKey(m => m.PerformedById)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(m => m.MovementNumber).IsUnique();
            b.HasIndex(m => new { m.StoreItemId, m.MovedOn });
            b.HasIndex(m => m.MovedOn);
            b.HasIndex(m => m.IsDeleted);
            b.HasQueryFilter(m => !m.IsDeleted);
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
