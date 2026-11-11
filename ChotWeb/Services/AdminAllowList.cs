using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ChotWeb.Services
{
    /// <summary>
    /// The email addresses allowed to sign in to /admin, from the "Admin:AllowedEmails" setting.
    /// On Render, set the environment variable Admin__AllowedEmails to a comma-separated list,
    /// e.g. "chot@gmail.com, manager@gmail.com". Removing an email signs that person out on their next click.
    /// </summary>
    public class AdminAllowList(IConfiguration configuration)
    {
        private static readonly char[] Separators = [',', ';', ' ', '\n', '\r', '\t'];

        public IReadOnlySet<string> Emails
        {
            get
            {
                IConfigurationSection section = configuration.GetSection("Admin:AllowedEmails");
                IEnumerable<string?> raw = section.Value is not null
                    ? section.Value.Split(Separators)                              // "a@x.com, b@y.com"
                    : section.GetChildren().Select(child => child.Value);         // JSON array in appsettings

                return raw.Select(Normalize).Where(email => email.Contains('@')).ToHashSet(StringComparer.Ordinal);
            }
        }

        public bool IsAllowed(string? email)
        {
            return !string.IsNullOrEmpty(email) && Emails.Contains(Normalize(email));
        }

        public static string Normalize(string? email)
        {
            return (email ?? "").Trim().ToLowerInvariant();
        }

        public static ClaimsPrincipal CreatePrincipal(string email)
        {
            ClaimsIdentity identity = new(
                [new Claim(ClaimTypes.Name, email), new Claim(ClaimTypes.Email, email)],
                CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>Signs out anyone whose email has been taken off the list.</summary>
        public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            AdminAllowList allowList = context.HttpContext.RequestServices.GetRequiredService<AdminAllowList>();
            if (!allowList.IsAllowed(context.Principal?.FindFirst(ClaimTypes.Email)?.Value))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
