using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure;
using NaijaPrimeSchool.Infrastructure.Payments;
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

// The payment simulator lets anyone "pay" without money moving; it must
// never be reachable outside development.
var onlinePayments = builder.Configuration.GetSection(OnlinePaymentOptions.SectionName).Get<OnlinePaymentOptions>() ?? new();
if (onlinePayments.Enabled
    && string.Equals(onlinePayments.Provider, "Simulator", StringComparison.OrdinalIgnoreCase)
    && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "OnlinePayments:Provider 'Simulator' is only allowed in the Development environment. Use 'Paystack'.");
}

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

// Paystack calls this server-to-server after a charge. The body is verified
// with the HMAC signature; the payment itself is re-verified with Paystack
// before anything is recorded.
app.MapPost("/api/payments/paystack/webhook", async (HttpRequest request, IOnlinePaymentService onlinePaymentService, CancellationToken ct) =>
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var ok = await onlinePaymentService.HandleWebhookAsync(body, request.Headers["x-paystack-signature"], ct);
        return ok ? Results.Ok() : Results.Unauthorized();
    })
    .AllowAnonymous()
    .DisableAntiforgery()
    // A machine endpoint: return the real status code rather than letting
    // UseStatusCodePagesWithReExecute re-run a 401 as the HTML not-found page.
    .WithMetadata(new Microsoft.AspNetCore.Mvc.SkipStatusCodePagesAttribute());

using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
}

app.Run();
