using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ✅ Syncfusion license (from configuration/user-secrets)
var syncfusionKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(syncfusionKey))
{
    SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);
}

// ✅ Register Syncfusion Blazor services
builder.Services.AddSyncfusionBlazor();

// (Optional) If you later need HttpClient for calling an API:
// builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
