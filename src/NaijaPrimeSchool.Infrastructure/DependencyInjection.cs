using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Attendance;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Inventory;
using NaijaPrimeSchool.Application.Portals;
using NaijaPrimeSchool.Application.Results;
using NaijaPrimeSchool.Application.Users;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Notifications;
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
        services.AddScoped<IStudentPhotoService, StudentPhotoService>();

        services.AddScoped<IDailyAttendanceService, DailyAttendanceService>();
        services.AddScoped<ISubjectAttendanceService, SubjectAttendanceService>();

        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IResultService, ResultService>();
        services.AddScoped<IReportCardService, ReportCardService>();

        services.AddScoped<IFeeScheduleService, FeeScheduleService>();
        // InvoiceService is registered twice: as IInvoiceService for the UI,
        // and as the concrete InvoiceService so PaymentService can call
        // its internal RecomputeInvoiceTotalsAsync helper directly.
        services.AddScoped<InvoiceService>();
        services.AddScoped<IInvoiceService>(sp => sp.GetRequiredService<InvoiceService>());
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IStoreItemService, StoreItemService>();
        services.AddScoped<IStockMovementService, StockMovementService>();

        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IPortalService, PortalService>();

        AddNotifications(services, configuration);

        return services;
    }

    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NotificationOptions.SectionName);
        services.Configure<NotificationOptions>(section);
        var options = section.Get<NotificationOptions>() ?? new NotificationOptions();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<NotificationDispatcher>();
        services.AddHostedService<NotificationDispatchWorker>();

        switch (options.Email.Provider.ToLowerInvariant())
        {
            case "log": services.AddScoped<IEmailGateway, LogEmailGateway>(); break;
            case "smtp": services.AddScoped<IEmailGateway, SmtpEmailGateway>(); break;
            default:
                throw new InvalidOperationException(
                    $"Unknown Notifications:Email:Provider '{options.Email.Provider}'. Use 'Log' or 'Smtp'.");
        }

        switch (options.Sms.Provider.ToLowerInvariant())
        {
            case "log": services.AddScoped<ISmsGateway, LogSmsGateway>(); break;
            case "termii": services.AddHttpClient<ISmsGateway, TermiiSmsGateway>(); break;
            default:
                throw new InvalidOperationException(
                    $"Unknown Notifications:Sms:Provider '{options.Sms.Provider}'. Use 'Log' or 'Termii'.");
        }
    }
}
