using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ✅ Syncfusion license (use your 7-day key for now)
SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCfExyWmFZfVhgd19GZVZTQ2Y/P1ZhSXxVdkRhUX5bcHFXQWFVUUF9XEA=");

// ✅ Register Syncfusion Blazor services
builder.Services.AddSyncfusionBlazor();

// (Optional) If you later need HttpClient for calling an API:
// builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
