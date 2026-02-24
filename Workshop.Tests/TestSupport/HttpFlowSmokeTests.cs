using System.Net;
using System.Net.Http.Headers;

namespace Workshop.Tests.TestSupport;

public static class HttpFlowSmokeTests
{
    public static string DefaultBaseUrl =>
        (Environment.GetEnvironmentVariable("WORKSHOP_HTTP_SMOKE_BASE_URL") ?? "http://workshop.local").TrimEnd('/');

    public static bool IsReachable(string baseUrl)
    {
        try
        {
            using var client = CreateClient(baseUrl, allowAutoRedirect: true);
            var response = client.GetAsync("/").GetAwaiter().GetResult();
            return (int)response.StatusCode >= 200 && (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    public static void PricingPaidMode_Loads()
    {
        using var client = CreateClient(DefaultBaseUrl, allowAutoRedirect: true);
        var response = client.GetAsync("/pricing?paid=1").GetAwaiter().GetResult();
        Ensure(response.StatusCode == HttpStatusCode.OK, $"Expected HTTP 200 from /pricing?paid=1, got {(int)response.StatusCode}.");
    }

    public static void PricingCheckout_AnonymousRedirectsToLogin()
    {
        using var client = CreateClient(DefaultBaseUrl, allowAutoRedirect: false);
        var response = client.GetAsync("/billing/start?plan=standard&annual=0").GetAwaiter().GetResult();
        Ensure(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect from /billing/start for anonymous user, got {(int)response.StatusCode}.");

        var location = response.Headers.Location?.ToString() ?? "";
        Ensure(location.Contains("/login", StringComparison.OrdinalIgnoreCase), $"Expected billing/start redirect to login, got '{location}'.");
    }

    public static void SignupPage_LoadsWithPackageSelector()
    {
        using var client = CreateClient(DefaultBaseUrl, allowAutoRedirect: true);
        var response = client.GetAsync("/signup").GetAwaiter().GetResult();
        Ensure(response.StatusCode == HttpStatusCode.OK, $"Expected HTTP 200 from /signup, got {(int)response.StatusCode}.");
    }

    public static void TrialAccess_AnonymousRedirectsToLogin()
    {
        using var client = CreateClient(DefaultBaseUrl, allowAutoRedirect: false);
        var response = client.GetAsync("/trial-access").GetAwaiter().GetResult();
        Ensure(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect from /trial-access for anonymous user, got {(int)response.StatusCode}.");

        var location = response.Headers.Location?.ToString() ?? "";
        Ensure(location.Contains("/login", StringComparison.OrdinalIgnoreCase), $"Expected redirect to login, got '{location}'.");
    }

    public static void Login_InvalidCredentials_RedirectsWithError()
    {
        using var client = CreateClient(DefaultBaseUrl, allowAutoRedirect: false);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "nobody@example.com",
            ["password"] = "bad-password",
            ["returnUrl"] = "/app"
        });
        form.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = client.PostAsync("/auth/login", form).GetAwaiter().GetResult();
        Ensure(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect from /auth/login invalid credentials, got {(int)response.StatusCode}.");

        var location = response.Headers.Location?.ToString() ?? "";
        Ensure(location.Contains("error=invalid", StringComparison.OrdinalIgnoreCase),
            $"Expected invalid login redirect, got '{location}'.");
    }

    private static HttpClient CreateClient(string baseUrl, bool allowAutoRedirect)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = allowAutoRedirect,
            UseProxy = false
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
