
using Syncfusion.Blazor;
using Syncfusion.Licensing;
using Workshop.Components;
using Workshop.Services;

var builder = WebApplication.CreateBuilder(args);

// Syncfusion
SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCfExyWmFZfVhgd19GZVZTQ2Y/P1ZhSXxVdkRhUX5bcHFXQWFVUUF9XEA=");
builder.Services.AddSyncfusionBlazor();

// App services (in memory v1)
builder.Services.AddSingleton<WorkshopData>();
builder.Services.AddSingleton<JobCatalogService>();
builder.Services.AddSingleton<SchedulingService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
    .AddAdditionalAssemblies(typeof(Workshop.Client._Imports).Assembly);

app.Run();
