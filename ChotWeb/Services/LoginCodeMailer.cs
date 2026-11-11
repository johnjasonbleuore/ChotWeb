using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;

namespace ChotWeb.Services
{
    /// <summary>
    /// Emails sign-in codes. Set ONE of these up with environment variables (never commit the keys):
    ///
    ///   Brevo (works on Render's free plan, which blocks SMTP):
    ///     Email__BrevoApiKey = xkeysib-...
    ///     Email__From        = the sender address you verified in Brevo
    ///
    ///   SMTP (paid hosting or your own PC), e.g. Gmail with an app password:
    ///     Email__Smtp__Host = smtp.gmail.com   Email__Smtp__Port = 587
    ///     Email__Smtp__Username / Email__Smtp__Password
    ///     Email__From = the address to send from
    ///
    /// With neither set, the code is written to the server log instead (Render: service > Logs).
    /// </summary>
    public class LoginCodeMailer(IConfiguration configuration, IHttpClientFactory httpClients, ILogger<LoginCodeMailer> logger)
    {
        private const string Subject = "Your sign-in code";

        public string? BrevoApiKey => Clean(configuration["Email:BrevoApiKey"]);
        public string? SmtpHost => Clean(configuration["Email:Smtp:Host"]);
        public string? From => Clean(configuration["Email:From"]);
        public string FromName => Clean(configuration["Email:FromName"]) ?? "Chot Umandal site";

        public bool IsConfigured => From is not null && (BrevoApiKey is not null || SmtpHost is not null);

        public async Task SendAsync(string to, string code, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                logger.LogWarning("Email sending isn't set up, so here is the sign-in code for {Email}: {Code}", to, code);
                return;
            }

            string text =
                $"Your sign-in code is {code}\n\n" +
                $"Enter it on the sign-in page within {LoginCodeStore.Lifetime.TotalMinutes:0} minutes. It works once.\n\n" +
                "If you didn't try to sign in, you can ignore this email.";
            string html =
                "<div style=\"font-family:Arial,sans-serif;max-width:420px;margin:auto;padding:24px;color:#2b211c\">" +
                "<p style=\"margin:0 0 12px\">Your sign-in code is</p>" +
                $"<p style=\"font-size:32px;font-weight:bold;letter-spacing:6px;margin:0 0 16px\">{code}</p>" +
                $"<p style=\"margin:0 0 12px;color:#6b5d55\">Enter it on the sign-in page within {LoginCodeStore.Lifetime.TotalMinutes:0} minutes. It works once.</p>" +
                "<p style=\"margin:0;color:#6b5d55;font-size:13px\">If you didn't try to sign in, you can ignore this email.</p></div>";

            if (BrevoApiKey is not null)
            {
                await SendWithBrevoAsync(to, text, html, cancellationToken);
            }
            else
            {
                await SendWithSmtpAsync(to, text, html, cancellationToken);
            }

            logger.LogInformation("Sign-in code emailed to {Email}.", to);
        }

        private async Task SendWithBrevoAsync(string to, string text, string html, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = JsonContent.Create(new
                {
                    sender = new { name = FromName, email = From },
                    to = new[] { new { email = to } },
                    subject = Subject,
                    textContent = text,
                    htmlContent = html,
                }),
            };
            request.Headers.Add("api-key", BrevoApiKey);
            request.Headers.Add("accept", "application/json");

            using HttpResponseMessage response = await httpClients.CreateClient(nameof(LoginCodeMailer)).SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Brevo refused the email ({(int)response.StatusCode}): {body}");
            }
        }

        private async Task SendWithSmtpAsync(string to, string text, string html, CancellationToken cancellationToken)
        {
            using MailMessage message = new(new MailAddress(From!, FromName), new MailAddress(to))
            {
                Subject = Subject,
                Body = text,
            };
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));

            using SmtpClient client = new(SmtpHost, configuration.GetValue("Email:Smtp:Port", 587))
            {
                EnableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true),
                Credentials = new NetworkCredential(configuration["Email:Smtp:Username"], configuration["Email:Smtp:Password"]),
                Timeout = 20_000,
            };
            await client.SendMailAsync(message, cancellationToken);
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
