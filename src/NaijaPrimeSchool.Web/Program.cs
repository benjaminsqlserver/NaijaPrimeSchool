using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure;
using NaijaPrimeSchool.Infrastructure.Persistence;
using NaijaPrimeSchool.Web.Components;
using NaijaPrimeSchool.Web.Components.Account;
using NaijaPrimeSchool.Web.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManageUsers", p => p.RequireRole(Roles.SuperAdmin));
});

builder.Services.AddRadzenComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(NaijaPrimeSchool.Web.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

// Run migrations/seeding in the background *after* the app has started listening,
// so Kestrel binds the port immediately and platform health checks (Fly, Azure, etc.)
// don't see the instance as down while we wait on the database. Retries with backoff
// so a slow-to-start or momentarily unreachable database doesn't crash the process.
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

        const int maxAttempts = 8;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 5));
                logger.LogWarning(ex,
                    "Database initialization failed (attempt {Attempt}/{Max}). Retrying in {Delay}...",
                    attempt, maxAttempts, delay);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "Database initialization failed after {Max} attempts. The app will keep running, " +
                    "but data access will fail until connectivity to the database is restored.", maxAttempts);
            }
        }
    });
});

app.Run();
