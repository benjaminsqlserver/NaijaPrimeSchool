using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public const string DefaultSuperAdminEmail = "superadmin@naijaprimeschool.ng";
    public const string DefaultSuperAdminPassword = "Admin@12345";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        var db = sp.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(ct);

        await SeedLookupsAsync(db, ct);
        await SeedAcademicLookupsAsync(db, ct);
        await SeedFamilyLookupsAsync(db, ct);
        await SeedAttendanceLookupsAsync(db, ct);
        await SeedRolesAsync(sp, ct);
        await SeedSuperAdminAsync(sp, logger, ct);
    }

    private static async Task SeedAttendanceLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.AttendanceStatuses.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code, bool CountsAsPresent)[] statuses =
            [
                ("Present",   "P",  true),
                ("Late",      "L",  true),
                ("Excused",   "E",  false),
                ("Sick",      "S",  false),
                ("Absent",    "A",  false),
                ("Suspended", "SP", false),
            ];
            for (var i = 0; i < statuses.Length; i++)
            {
                db.AttendanceStatuses.Add(new AttendanceStatus
                {
                    Name = statuses[i].Name,
                    Code = statuses[i].Code,
                    DisplayOrder = i + 1,
                    CountsAsPresent = statuses[i].CountsAsPresent,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedFamilyLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.Relationships.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] relationships =
            [
                "Father",
                "Mother",
                "Stepfather",
                "Stepmother",
                "Grandfather",
                "Grandmother",
                "Uncle",
                "Aunt",
                "Guardian",
                "Other",
            ];
            for (var i = 0; i < relationships.Length; i++)
            {
                db.Relationships.Add(new Relationship { Name = relationships[i], DisplayOrder = i + 1 });
            }
        }

        if (!await db.EnrolmentStatuses.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] statuses =
            [
                "Active",
                "Suspended",
                "Withdrawn",
                "Transferred",
                "Graduated",
            ];
            for (var i = 0; i < statuses.Length; i++)
            {
                db.EnrolmentStatuses.Add(new EnrolmentStatus { Name = statuses[i], DisplayOrder = i + 1 });
            }
        }

        if (!await db.BloodGroups.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] groups = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-", "Unknown"];
            for (var i = 0; i < groups.Length; i++)
            {
                db.BloodGroups.Add(new BloodGroup { Name = groups[i], DisplayOrder = i + 1 });
            }
        }

        if (!await db.MaritalStatuses.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] statuses = ["Single", "Married", "Divorced", "Widowed", "Separated"];
            for (var i = 0; i < statuses.Length; i++)
            {
                db.MaritalStatuses.Add(new MaritalStatus { Name = statuses[i], DisplayOrder = i + 1 });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedAcademicLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.TermTypes.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.TermTypes.AddRange(
                new TermType { Name = "First Term", DisplayOrder = 1 },
                new TermType { Name = "Second Term", DisplayOrder = 2 },
                new TermType { Name = "Third Term", DisplayOrder = 3 });
        }

        if (!await db.ClassLevels.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] levels =
            [
                "Creche",
                "Pre-Nursery",
                "Nursery 1",
                "Nursery 2",
                "KG 1",
                "KG 2",
                "Primary 1",
                "Primary 2",
                "Primary 3",
                "Primary 4",
                "Primary 5",
                "Primary 6",
            ];
            for (var i = 0; i < levels.Length; i++)
            {
                db.ClassLevels.Add(new ClassLevel { Name = levels[i], DisplayOrder = i + 1 });
            }
        }

        if (!await db.WeekDays.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.WeekDays.AddRange(
                new WeekDay { Name = "Monday",    ShortName = "Mon", DisplayOrder = 1 },
                new WeekDay { Name = "Tuesday",   ShortName = "Tue", DisplayOrder = 2 },
                new WeekDay { Name = "Wednesday", ShortName = "Wed", DisplayOrder = 3 },
                new WeekDay { Name = "Thursday",  ShortName = "Thu", DisplayOrder = 4 },
                new WeekDay { Name = "Friday",    ShortName = "Fri", DisplayOrder = 5 });
        }

        if (!await db.TimetablePeriods.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, int Hour, int Minute, int DurationMinutes, int Order, bool IsBreak)[] periods =
            [
                ("Period 1",      8, 0,  40, 1, false),
                ("Period 2",      8, 40, 40, 2, false),
                ("Period 3",      9, 20, 40, 3, false),
                ("Short Break",  10, 0,  20, 4, true),
                ("Period 4",     10, 20, 40, 5, false),
                ("Period 5",     11, 0,  40, 6, false),
                ("Lunch",        11, 40, 40, 7, true),
                ("Period 6",     12, 20, 40, 8, false),
                ("Period 7",     13, 0,  40, 9, false),
            ];
            foreach (var p in periods)
            {
                var start = new TimeOnly(p.Hour, p.Minute);
                db.TimetablePeriods.Add(new TimetablePeriod
                {
                    Name = p.Name,
                    StartTime = start,
                    EndTime = start.AddMinutes(p.DurationMinutes),
                    DisplayOrder = p.Order,
                    IsBreak = p.IsBreak,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.Genders.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.Genders.AddRange(
                new Gender { Name = "Male", Code = "M", DisplayOrder = 1 },
                new Gender { Name = "Female", Code = "F", DisplayOrder = 2 });
        }

        if (!await db.Titles.IgnoreQueryFilters().AnyAsync(ct))
        {
            db.Titles.AddRange(
                new Title { Name = "Mr.", DisplayOrder = 1 },
                new Title { Name = "Mrs.", DisplayOrder = 2 },
                new Title { Name = "Miss", DisplayOrder = 3 },
                new Title { Name = "Ms.", DisplayOrder = 4 },
                new Title { Name = "Dr.", DisplayOrder = 5 },
                new Title { Name = "Prof.", DisplayOrder = 6 },
                new Title { Name = "Chief", DisplayOrder = 7 },
                new Title { Name = "Alhaji", DisplayOrder = 8 },
                new Title { Name = "Alhaja", DisplayOrder = 9 });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAsync(IServiceProvider sp, CancellationToken ct)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        var descriptions = new Dictionary<string, string>
        {
            [Roles.SuperAdmin] = "System super administrator with full access to all features.",
            [Roles.HeadTeacher] = "Head teacher overseeing the entire school.",
            [Roles.Teacher] = "Teaching staff responsible for classes and subjects.",
            [Roles.SchoolBursar] = "Financial officer managing school finances and fees.",
            [Roles.SchoolStoreKeeper] = "Keeper of school stores, inventory and supplies.",
            [Roles.Parent] = "Parent or guardian of a pupil.",
            [Roles.Student] = "Pupil enrolled in the school.",
        };

        foreach (var name in Roles.All)
        {
            var existing = await roleManager.FindByNameAsync(name);
            if (existing is null)
            {
                var role = new ApplicationRole
                {
                    Name = name,
                    NormalizedName = name.ToUpperInvariant(),
                    Description = descriptions[name],
                    IsSystemRole = true,
                    CreatedBy = "system",
                };
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedSuperAdminAsync(IServiceProvider sp, ILogger logger, CancellationToken ct)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(DefaultSuperAdminEmail);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            UserName = "superadmin",
            Email = DefaultSuperAdminEmail,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            CreatedBy = "system",
        };

        var create = await userManager.CreateAsync(admin, DefaultSuperAdminPassword);
        if (!create.Succeeded)
        {
            logger.LogError("Failed to seed SuperAdmin: {Errors}",
                string.Join(", ", create.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
        logger.LogInformation("Seeded default SuperAdmin ({Email}).", DefaultSuperAdminEmail);
    }
}
