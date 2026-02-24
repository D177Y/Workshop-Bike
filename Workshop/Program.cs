using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
builder.Services.AddScoped<SuperAdminDefaultsService>();
builder.Services.AddScoped<SuperAdminDashboardService>();
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
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
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
builder.Services.AddScoped<StripeSubscriptionSyncService>();
builder.Services.AddScoped<TrialLifecycleService>();
builder.Services.AddSingleton<OperationalAlertService>();
builder.Services.AddHostedService<TrialDataPurgeBackgroundService>();
builder.Services.AddHostedService<StripeSubscriptionReconciliationBackgroundService>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapPost("/stripe/webhook", async (
    HttpRequest request,
    IConfiguration config,
    StripeSubscriptionSyncService subscriptionSync,
    ILoggerFactory loggerFactory) =>
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

    try
    {
        await subscriptionSync.HandleWebhookAsync(stripeEvent);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Stripe webhook processing failed for event type {EventType}.", stripeEvent.Type);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    return Results.Ok();
}).AllowAnonymous().DisableAntiforgery();

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

app.MapPost("/auth/login", async (
    HttpContext httpContext,
    SignInManager<AppUser> signInManager,
    TrialLifecycleService trialLifecycle,
    IDbContextFactory<WorkshopDbContext> dbFactory,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("AuthLogin");
    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

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
    logger.LogInformation("Login attempt for {LoginIdentifier} from {RemoteIp}.", loginIdentifier, remoteIp);

    var loginUser = await signInManager.UserManager.FindByNameAsync(loginIdentifier);
    if (loginUser is null)
        loginUser = await signInManager.UserManager.FindByEmailAsync(loginIdentifier);

    if (loginUser is null)
    {
        logger.LogWarning("Login failed for {LoginIdentifier} from {RemoteIp}: user not found.", loginIdentifier, remoteIp);
        return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=invalid");
    }

    var result = await signInManager.PasswordSignInAsync(loginUser.UserName!, password, true, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        logger.LogInformation("Login succeeded for user {UserId} from {RemoteIp}.", loginUser.Id, remoteIp);

        if (string.Equals(returnUrl, "/app", StringComparison.OrdinalIgnoreCase)
            && await signInManager.UserManager.IsInRoleAsync(loginUser, "SuperAdmin"))
        {
            return Results.LocalRedirect("/super/dashboard");
        }

        if (loginUser.TenantId > 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == loginUser.TenantId);
            if (tenant is not null
                && tenant.HasActivatedSubscription
                && !StripeBillingPolicy.HasBillableAccess(tenant))
            {
                logger.LogInformation("Login for tenant {TenantId} redirected to paid-only pricing due to inactive subscription.", tenant.Id);
                return Results.LocalRedirect("/pricing?paid=1&reason=subscription-inactive");
            }
        }

        var trialStatus = await trialLifecycle.GetForTenantAsync(loginUser.TenantId);
        if (trialStatus.RequiresAppGate
            && !returnUrl.StartsWith("/trial-access", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/billing/start", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/logout", StringComparison.OrdinalIgnoreCase))
        {
            return Results.LocalRedirect("/trial-access");
        }

        return Results.LocalRedirect(returnUrl);
    }
    if (result.IsLockedOut)
    {
        logger.LogWarning("Login blocked (locked out) for user {UserId} from {RemoteIp}.", loginUser.Id, remoteIp);
        return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=locked");
    }
    if (result.IsNotAllowed)
    {
        logger.LogWarning("Login blocked (not allowed) for user {UserId} from {RemoteIp}.", loginUser.Id, remoteIp);
        return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=notallowed");
    }

    logger.LogWarning("Login failed for user {UserId} from {RemoteIp}: invalid credentials.", loginUser.Id, remoteIp);
    return Results.LocalRedirect($"/login?ReturnUrl={Uri.EscapeDataString(returnUrl)}&error=invalid");
}).RequireRateLimiting("login").DisableAntiforgery();

app.MapGet("/billing/start", async (
    HttpContext httpContext,
    UserManager<AppUser> userManager,
    BillingService billingService) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
        return Results.Challenge();

    var userIdRaw = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdRaw, out var userId))
        return Results.LocalRedirect("/login");

    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null || string.IsNullOrWhiteSpace(user.Email))
        return Results.LocalRedirect("/login");

    var planRaw = httpContext.Request.Query["plan"].ToString();
    var plan = PlanCatalog.TryParseKey(planRaw, out var parsedPlan)
        ? parsedPlan
        : Workshop.Models.PlanTier.Standard;

    var annualRaw = httpContext.Request.Query["annual"].ToString();
    var annualBilling = string.Equals(annualRaw, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(annualRaw, "true", StringComparison.OrdinalIgnoreCase);

    var priceId = billingService.GetPriceId(plan, annualBilling);
    if (string.IsNullOrWhiteSpace(priceId))
        return Results.LocalRedirect("/pricing?paid=1&reason=price-missing");

    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}/";
    var session = billingService.CreateCheckoutSession(user.TenantId, user.Email, plan, baseUrl, annualBilling);
    return Results.Redirect(session.Url);
}).RequireAuthorization();

app.MapGet("/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
}).RequireAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponse
});

var razorApp = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
#if ENABLE_WASM
razorApp.AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Workshop.Client._Imports).Assembly);
#endif

app.Run();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.ToDictionary(
            pair => pair.Key,
            pair => new
            {
                status = pair.Value.Status.ToString(),
                description = pair.Value.Description,
                durationMs = pair.Value.Duration.TotalMilliseconds
            })
    });
    return context.Response.WriteAsync(payload);
}

internal sealed record LoginRequest(string Email, string Password, string? ReturnUrl);
