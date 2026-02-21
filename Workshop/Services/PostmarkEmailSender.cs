using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Workshop.Services;

public sealed class PostmarkEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public PostmarkEmailSender(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody)
    {
        var serverToken = _config["Postmark:ServerToken"];
        var fromEmail = _config["Postmark:FromEmail"];
        var messageStream = _config["Postmark:MessageStream"] ?? "outbound";

        if (IsMissingOrPlaceholder(serverToken) || IsMissingOrPlaceholder(fromEmail))
            throw new InvalidOperationException("Postmark is not configured. Set Postmark:ServerToken and Postmark:FromEmail.");

        var payload = new
        {
            From = fromEmail,
            To = toEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            MessageStream = messageStream
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Postmark-Server-Token", serverToken);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            // Log full response for diagnostics; caller handles user-facing message.
            throw new HttpRequestException(
                $"Postmark response {(int)response.StatusCode} {response.ReasonPhrase}: {body} " +
                $"(resolved FromEmail='{fromEmail}', MessageStream='{messageStream}')");
        }
    }

    private static bool IsMissingOrPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return value.Contains("__SET_IN_USER_SECRETS__", StringComparison.OrdinalIgnoreCase);
    }
}
