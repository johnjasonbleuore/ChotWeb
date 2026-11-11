using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ChotWeb.Services
{
    /// <summary>
    /// The usernames and passwords allowed into /admin, from the "Admin:Users" setting.
    /// Never put them in appsettings.json (it's on GitHub). Instead:
    ///
    ///   Render:  Environment variable  Admin__Users = chot:FirstPassword; manager:SecondPassword
    ///   Your PC: dotnet user-secrets set "Admin:Users" "chot:FirstPassword; manager:SecondPassword"
    ///
    /// Each entry is username:password, separated by ";" or new lines. Usernames ignore upper/lower case;
    /// passwords don't. A password may contain ":" but not ";".
    /// Removing a user, or changing their password, signs them out on their next click.
    /// </summary>
    public class AdminUsers(IConfiguration configuration)
    {
        public const string StampClaim = "pw_stamp";
        private static readonly char[] EntrySeparators = [';', '\n', '\r'];

        public IReadOnlyDictionary<string, string> Users
        {
            get
            {
                Dictionary<string, string> users = new(StringComparer.OrdinalIgnoreCase);
                IConfigurationSection section = configuration.GetSection("Admin:Users");

                if (section.Value is not null)
                {
                    // "chot:pass1; manager:pass2"
                    foreach (string entry in section.Value.Split(EntrySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        int colon = entry.IndexOf(':');
                        if (colon > 0)
                        {
                            Add(users, entry[..colon], entry[(colon + 1)..]);
                        }
                    }
                }
                else
                {
                    // JSON form: "Users": [ { "Username": "chot", "Password": "..." } ]
                    foreach (IConfigurationSection child in section.GetChildren())
                    {
                        Add(users, child["Username"], child["Password"]);
                    }
                }

                return users;
            }
        }

        /// <summary>Returns the username as written in the settings if the password matches, otherwise null.</summary>
        public string? Verify(string? username, string? password)
        {
            string name = (username ?? "").Trim();
            byte[] given = Digest(password ?? "");
            string? match = null;

            // Compare against every user in constant time so response time doesn't reveal valid usernames.
            foreach (KeyValuePair<string, string> user in Users)
            {
                bool nameMatches = string.Equals(user.Key, name, StringComparison.OrdinalIgnoreCase);
                bool passwordMatches = CryptographicOperations.FixedTimeEquals(Digest(user.Value), given);
                if (nameMatches && passwordMatches)
                {
                    match = user.Key;
                }
            }

            return match;
        }

        public ClaimsPrincipal CreatePrincipal(string username)
        {
            ClaimsIdentity identity = new(
                [new Claim(ClaimTypes.Name, username), new Claim(StampClaim, Stamp(username, Users[username]))],
                CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>Signs out anyone whose user was removed or whose password was changed.</summary>
        public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            AdminUsers admins = context.HttpContext.RequestServices.GetRequiredService<AdminUsers>();
            string? name = context.Principal?.Identity?.Name;
            string? stamp = context.Principal?.FindFirst(StampClaim)?.Value;

            bool stillValid = name is not null
                && admins.Users.TryGetValue(name, out string? password)
                && stamp == Stamp(name, password);

            if (!stillValid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        private static void Add(Dictionary<string, string> users, string? username, string? password)
        {
            string name = (username ?? "").Trim();
            if (name.Length > 0 && !string.IsNullOrEmpty(password))
            {
                users[name] = password.Trim();
            }
        }

        private static byte[] Digest(string value)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(value));
        }

        // A short fingerprint of the current password, stored in the (encrypted) sign-in cookie.
        private static string Stamp(string username, string password)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("chotweb-stamp\n" + username.ToLowerInvariant() + "\n" + password));
            return Convert.ToBase64String(hash, 0, 12);
        }
    }
}
