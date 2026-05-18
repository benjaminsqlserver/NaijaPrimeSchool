using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Communications;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Domain.Inventory;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Domain.Results;

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
        await SeedResultsLookupsAsync(db, ct);
        await SeedFinanceLookupsAsync(db, ct);
        await SeedInventoryLookupsAsync(db, ct);
        await SeedCommunicationsLookupsAsync(db, ct);
        await SeedRolesAsync(sp, ct);
        await SeedSuperAdminAsync(sp, logger, ct);
    }

    private static async Task SeedInventoryLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.ItemCategories.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code)[] categories =
            [
                ("Books",            "BOOK"),
                ("Uniforms",         "UNIF"),
                ("Stationery",       "STAT"),
                ("Sports Equipment", "SPRT"),
                ("Cleaning Supplies","CLN"),
                ("Food",             "FOOD"),
                ("Furniture",        "FURN"),
                ("ICT Equipment",    "ICT"),
                ("Medical Supplies", "MED"),
                ("Other",            "OTH"),
            ];
            for (var i = 0; i < categories.Length; i++)
            {
                db.ItemCategories.Add(new ItemCategory
                {
                    Name = categories[i].Name,
                    Code = categories[i].Code,
                    DisplayOrder = i + 1,
                });
            }
        }

        if (!await db.UnitsOfMeasure.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code)[] units =
            [
                ("Each",     "EA"),
                ("Piece",    "PC"),
                ("Pack",     "PK"),
                ("Box",      "BOX"),
                ("Carton",   "CTN"),
                ("Set",      "SET"),
                ("Bag",      "BAG"),
                ("Kilogram", "KG"),
                ("Litre",    "L"),
                ("Metre",    "M"),
            ];
            for (var i = 0; i < units.Length; i++)
            {
                db.UnitsOfMeasure.Add(new UnitOfMeasure
                {
                    Name = units[i].Name,
                    Code = units[i].Code,
                    DisplayOrder = i + 1,
                });
            }
        }

        if (!await db.StockMovementTypes.IgnoreQueryFilters().AnyAsync(ct))
        {
            // Direction +1 puts stock in, -1 takes stock out. The service layer
            // multiplies Quantity by Direction to roll up the on-hand figure.
            (string Name, string Code, int Direction)[] types =
            [
                ("Opening Balance",  "OPENING", +1),
                ("Purchase",         "PURCHASE", +1),
                ("Return",           "RETURN",   +1),
                ("Adjustment In",    "ADJ_IN",   +1),
                ("Issue",            "ISSUE",    -1),
                ("Write-off",        "WRITEOFF", -1),
                ("Adjustment Out",   "ADJ_OUT",  -1),
            ];
            for (var i = 0; i < types.Length; i++)
            {
                db.StockMovementTypes.Add(new StockMovementType
                {
                    Name = types[i].Name,
                    Code = types[i].Code,
                    Direction = types[i].Direction,
                    DisplayOrder = i + 1,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCommunicationsLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.AnnouncementCategories.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code)[] categories =
            [
                ("General",   "GEN"),
                ("Academic",  "ACAD"),
                ("Finance",   "FIN"),
                ("Events",    "EVENT"),
                ("Holiday",   "HOL"),
                ("Health",    "HEALTH"),
                ("Emergency", "EMERG"),
            ];
            for (var i = 0; i < categories.Length; i++)
            {
                var c = categories[i];
                db.AnnouncementCategories.Add(new AnnouncementCategory
                {
                    Name = c.Name,
                    Code = c.Code,
                    DisplayOrder = i + 1,
                });
            }
        }

        if (!await db.AnnouncementAudiences.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code, bool RequiresTargetClass)[] audiences =
            [
                ("Everyone",       "ALL",      false),
                ("Parents",        "PARENT",   false),
                ("Students",       "STUDENT",  false),
                ("Specific Class", "CLASS",    true),
            ];
            for (var i = 0; i < audiences.Length; i++)
            {
                var a = audiences[i];
                db.AnnouncementAudiences.Add(new AnnouncementAudience
                {
                    Name = a.Name,
                    Code = a.Code,
                    DisplayOrder = i + 1,
                    RequiresTargetClass = a.RequiresTargetClass,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedFinanceLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.FeeCategories.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code, bool Mandatory)[] categories =
            [
                ("Tuition",            "TUI",  true),
                ("Development Levy",   "DEV",  true),
                ("Examination",        "EXAM", true),
                ("Books",              "BOOK", false),
                ("Uniforms",           "UNIF", false),
                ("Transport",          "TRAN", false),
                ("Meals",              "MEAL", false),
                ("Boarding",           "BRD",  false),
                ("PTA Levy",           "PTA",  true),
                ("Other",              "OTH",  false),
            ];
            for (var i = 0; i < categories.Length; i++)
            {
                var c = categories[i];
                db.FeeCategories.Add(new FeeCategory
                {
                    Name = c.Name,
                    Code = c.Code,
                    DisplayOrder = i + 1,
                    IsMandatoryByDefault = c.Mandatory,
                });
            }
        }

        if (!await db.PaymentMethods.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code, bool RequiresRef)[] methods =
            [
                ("Cash",           "CASH",  false),
                ("Bank Transfer",  "BANK",  true),
                ("POS",            "POS",   true),
                ("Cheque",         "CHQ",   true),
                ("Mobile Money",   "MMNY",  true),
                ("Online Payment", "ONL",   true),
            ];
            for (var i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                db.PaymentMethods.Add(new PaymentMethod
                {
                    Name = m.Name,
                    Code = m.Code,
                    DisplayOrder = i + 1,
                    RequiresReference = m.RequiresRef,
                });
            }
        }

        if (!await db.InvoiceStatuses.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code)[] statuses =
            [
                ("Draft",          "DRAFT"),
                ("Issued",         "ISSUED"),
                ("Partially Paid", "PARTIAL"),
                ("Paid",           "PAID"),
                ("Overdue",        "OVERDUE"),
                ("Cancelled",      "CANCELLED"),
            ];
            for (var i = 0; i < statuses.Length; i++)
            {
                db.InvoiceStatuses.Add(new InvoiceStatus
                {
                    Name = statuses[i].Name,
                    Code = statuses[i].Code,
                    DisplayOrder = i + 1,
                });
            }
        }

        if (!await db.PaymentStatuses.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code)[] statuses =
            [
                ("Pending",   "PENDING"),
                ("Confirmed", "CONFIRMED"),
                ("Bounced",   "BOUNCED"),
                ("Refunded",  "REFUNDED"),
            ];
            for (var i = 0; i < statuses.Length; i++)
            {
                db.PaymentStatuses.Add(new PaymentStatus
                {
                    Name = statuses[i].Name,
                    Code = statuses[i].Code,
                    DisplayOrder = i + 1,
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedResultsLookupsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.AssessmentTypes.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, string Code, int Max, bool IsExam)[] types =
            [
                ("First CA",      "CA1",  20, false),
                ("Second CA",     "CA2",  20, false),
                ("Mid-Term Test", "MID",  20, false),
                ("Assignment",    "ASN",  10, false),
                ("Project",       "PRJ",  10, false),
                ("Examination",   "EXAM", 60, true),
            ];
            for (var i = 0; i < types.Length; i++)
            {
                var t = types[i];
                db.AssessmentTypes.Add(new AssessmentType
                {
                    Name = t.Name,
                    Code = t.Code,
                    DefaultMaxScore = t.Max,
                    IsExam = t.IsExam,
                    DisplayOrder = i + 1,
                });
            }
        }

        if (!await db.GradeBands.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, decimal Lo, decimal Hi, string Desc, string Remark)[] bands =
            [
                ("A", 80m, 100m, "Excellent",  "Outstanding performance — keep it up."),
                ("B", 70m, 79.99m,  "Very Good",  "Very good — aim higher."),
                ("C", 60m, 69.99m,  "Good",       "Good — apply more effort."),
                ("D", 50m, 59.99m,  "Pass",       "Fair — work harder."),
                ("E", 40m, 49.99m,  "Weak Pass",  "Below average — needs improvement."),
                ("F",  0m, 39.99m,  "Fail",       "Poor — extra support required."),
            ];
            for (var i = 0; i < bands.Length; i++)
            {
                var g = bands[i];
                db.GradeBands.Add(new GradeBand
                {
                    Name = g.Name,
                    LowerBound = g.Lo,
                    UpperBound = g.Hi,
                    Description = g.Desc,
                    Remark = g.Remark,
                    DisplayOrder = i + 1,
                });
            }
        }

        if (!await db.AffectiveTraits.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] traits =
            [
                "Punctuality",
                "Attentiveness",
                "Honesty",
                "Neatness",
                "Politeness",
                "Cooperation",
                "Class Participation",
                "Self-control",
            ];
            for (var i = 0; i < traits.Length; i++)
            {
                db.AffectiveTraits.Add(new AffectiveTrait { Name = traits[i], DisplayOrder = i + 1 });
            }
        }

        if (!await db.PsychomotorSkills.IgnoreQueryFilters().AnyAsync(ct))
        {
            string[] skills =
            [
                "Handwriting",
                "Drawing & Painting",
                "Music",
                "Sports & Games",
                "Public Speaking",
                "Crafts",
                "Verbal Fluency",
            ];
            for (var i = 0; i < skills.Length; i++)
            {
                db.PsychomotorSkills.Add(new PsychomotorSkill { Name = skills[i], DisplayOrder = i + 1 });
            }
        }

        if (!await db.TraitRatings.IgnoreQueryFilters().AnyAsync(ct))
        {
            (string Name, int Value)[] ratings =
            [
                ("Excellent", 5),
                ("Very Good", 4),
                ("Good",      3),
                ("Fair",      2),
                ("Poor",      1),
            ];
            for (var i = 0; i < ratings.Length; i++)
            {
                db.TraitRatings.Add(new TraitRating
                {
                    Name = ratings[i].Name,
                    Value = ratings[i].Value,
                    DisplayOrder = i + 1,
                });
            }
        }

        await db.SaveChangesAsync(ct);
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
