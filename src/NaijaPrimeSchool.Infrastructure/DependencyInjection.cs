using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Users;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;
using NaijaPrimeSchool.Infrastructure.Services;

namespace NaijaPrimeSchool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILookupService, LookupService>();

        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITermService, TermService>();
        services.AddScoped<ISchoolClassService, SchoolClassService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ITimetableService, TimetableService>();

        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<IEnrolmentService, EnrolmentService>();

        return services;
    }
}
