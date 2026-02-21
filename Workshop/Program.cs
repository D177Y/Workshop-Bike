
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Syncfusion.Blazor;
using Syncfusion.Licensing;
using Workshop.Data;
using Workshop.Components;
using Workshop.Models;
using Workshop.Services;

var builder = WebApplication.CreateBuilder(args);
// Allow local hosted environments (for example IIS with workshop.local) to read
// the same secrets used by `dotnet run`, even when ASPNETCORE_ENVIRONMENT is not Development.
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Syncfusion
var syncfusionKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(syncfusionKey))
{
    SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);
}
builder.Services.AddSyncfusionBlazor();

// Persist Data Protection keys so antiforgery tokens survive app restarts (IIS)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("Workshop");

// Data + app services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<WorkshopReadService>();
builder.Services.AddScoped<StoreCommandService>();
builder.Services.AddScoped<MechanicCommandService>();
builder.Services.AddScoped<BookingCommandService>();
builder.Services.AddScoped<BookingStatusCommandService>();
builder.Services.AddScoped<TimeOffCommandService>();
builder.Services.AddScoped<CustomerProfileWriteService>();
builder.Services.AddScoped<CustomerBookingProfileService>();
builder.Services.AddScoped<TimetasticWebhookPayloadParser>();
builder.Services.AddScoped<WorkshopData>();
builder.Services.AddScoped<CustomerProfileService>();
builder.Services.AddScoped<FinancialYearService>();
builder.Services.AddScoped<IntegrationSettingsService>();
builder.Services.AddScoped<TimetasticWebhookService>();
builder.Services.AddScoped<DashboardAnalyticsService>();
builder.Services.AddScoped<QuoteWorkflowService>();
builder.Services.AddScoped<EmailRetryQueueService>();
builder.Services.AddScoped<JobCatalogService>();
builder.Services.AddScoped<SuggestedCatalogService>();
builder.Services.AddScoped<TenantSetupService>();
builder.Services.AddScoped<SchedulingService>();
builder.Services.AddScoped<StoreSchedulerBookingProjectionService>();
builder.Services.AddScoped<StoreSchedulerAppointmentService>();
builder.Services.AddScoped<UserAccessService>();
builder.Services.AddHostedService<EmailRetryBackgroundService>();
builder.Services.AddDbContextFactory<WorkshopDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("WorkshopDb");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Connection string 'WorkshopDb' is not configured.");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

var requireConfirmedEmail = builder.Configuration.GetValue("Auth:RequireConfirmedEmail", true);
builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedEmail = requireConfirmedEmail;
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<WorkshopDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});
builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpClient<PostmarkEmailSender>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Local environments may define process-level proxy vars that blackhole outbound HTTP.
        UseProxy = false
    });
builder.Services.AddHttpClient<TimetasticSyncService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseProxy = false
    });
builder.Services.AddScoped<IEmailSender, PostmarkEmailSender>();
builder.Services.AddScoped<IdentityBootstrapper>();
builder.Services.AddScoped<BillingService>();

// Add services to the container.
var razorComponents = builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
#if ENABLE_WASM
razorComponents.AddInteractiveWebAssemblyComponents();
#endif

if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<CircuitOptions>(options =>
    {
        options.DetailedErrors = true;
    });
}

var app = builder.Build();

var stripeKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeKey))
{
    StripeConfiguration.ApiKey = stripeKey;
}

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().EnsureInitializedAsync();
    await scope.ServiceProvider.GetRequiredService<IdentityBootstrapper>().EnsureRolesAsync();
}

if (app.Environment.IsDevelopment())
{
#if ENABLE_WASM
    app.UseWebAssemblyDebugging();
#endif
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost("/stripe/webhook", async (HttpRequest request, IDbContextFactory<WorkshopDbContext> dbFactory, IConfiguration config, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("StripeWebhook");
    var json = await new StreamReader(request.Body).ReadToEndAsync();
    var secret = config["Stripe:WebhookSecret"];

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], secret);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Stripe webhook signature validation failed.");
        return Results.BadRequest();
    }

    if (stripeEvent.Type == Events.CheckoutSessionCompleted)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session is not null)
        {
            var subscriptionId = session.SubscriptionId;
            var customerId = session.CustomerId;
            var tenantIdRaw = session.Metadata?["tenant_id"];
            var planRaw = session.Metadata?["plan"];
            var mechanicLimitRaw = session.Metadata?["mechanic_limit"];

            if (int.TryParse(tenantIdRaw, out var tenantId))
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
                if (tenant is not null)
                {
                    tenant.StripeCustomerId = customerId ?? "";
                    tenant.StripeSubscriptionId = subscriptionId ?? "";
                    tenant.IsActive = true;
                    if (Enum.TryParse<Workshop.Models.PlanTier>(planRaw, true, out var plan))
                        tenant.Plan = plan;
                    if (int.TryParse(mechanicLimitRaw, out var limit))
                        tenant.MaxMechanics = limit;
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    return Results.Ok();
});

app.MapPost("/integrations/timetastic/webhook", async (
    HttpRequest request,
    TimetasticWebhookService webhookService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("TimetasticWebhook");
    var body = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
    var secretHeader = request.Headers["Timetastic-Secret"].ToString();
    var result = await webhookService.ProcessAsync(secretHeader, body, cancellationToken);

    if (result.StatusCode >= 500)
    {
        logger.LogError("Timetastic webhook failed. Status {StatusCode}. Message: {Message}", result.StatusCode, result.Message);
    }
    else
    {
        logger.LogInformation("Timetastic webhook handled. Status {StatusCode}. Message: {Message}", result.StatusCode, result.Message);
    }

    return result.StatusCode switch
    {
        200 => Results.Text(result.Message, "text/plain", statusCode: 200),
        400 => Results.Text(result.Message, "text/plain", statusCode: 400),
        401 => Results.Text(result.Message, "text/plain", statusCode: 401),
        _ => Results.Text(result.Message, "text/plain", statusCode: result.StatusCode)
    };
})
.AllowAnonymous()
.DisableAntiforgery();

app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<AppUser> signInManager) =>
{
    string? email = null;
    string? password = null;
    string? returnUrl = null;

    if (httpContext.Request.HasFormContentType)
    {
        var form = await httpContext.Request.ReadFormAsync();
        email = form["email"].ToString();
        password = form["password"].ToString();
        returnUrl = form["returnUrl"].ToString();
    }
    else
    {
        var payload = await httpContext.Request.ReadFromJsonAsync<LoginRequest>();
        if (payload is not null)
        {
            email = payload.Email;
            password = payload.Password;
            returnUrl = payload.ReturnUrl;
        }
    }

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return Results.BadRequest();

    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) || returnUrl.StartsWith("//", StringComparison.Ordinal))
        returnUrl = "/app";

    var loginIdentifier = email.Trim();
    var loginUser = await signInManager.UserManager.FindByNameAsync(loginIdentifier);
    if (loginUser is null)
        loginUser = await signInManager.UserManager.FindByEmailAsync(loginIdentifier);

    if (loginUser is null)
        return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=invalid");

    var result = await signInManager.PasswordSignInAsync(loginUser.UserName!, password, true, lockoutOnFailure: false);
    if (result.Succeeded)
    {
        if (string.Equals(returnUrl, "/app", StringComparison.OrdinalIgnoreCase)
            && await signInManager.UserManager.IsInRoleAsync(loginUser, "SuperAdmin"))
        {
            return Results.LocalRedirect("/super/settings/default-categories");
        }

        return Results.LocalRedirect(returnUrl);
    }
    if (result.IsNotAllowed)
        return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=notallowed");

    return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=invalid");
}).DisableAntiforgery();

app.MapGet("/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
}).RequireAuthorization();

var razorApp = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
#if ENABLE_WASM
razorApp.AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Workshop.Client._Imports).Assembly);
#endif

app.Run();

internal sealed record LoginRequest(string Email, string Password, string? ReturnUrl);
