using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        await SeedRolesAsync(sp, ct);
        await SeedSuperAdminAsync(sp, logger, ct);
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
