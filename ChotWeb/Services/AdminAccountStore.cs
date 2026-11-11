using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ChotWeb.Services
{
    public record AdminAccount(string Username, string PasswordHash, string Salt, int Iterations, string Version);

    /// <summary>
    /// The single admin account, stored in App_Data/admin.json with a salted PBKDF2 password hash.
    /// First run: no account exists, so a one-time setup code is generated (printed in the console
    /// and saved to App_Data/setup-code.txt) and /admin/setup asks for it.
    /// Forgot the password? Delete App_Data/admin.json and restart the site to set it up again.
    /// </summary>
    public class AdminAccountStore
    {
        public const string VersionClaim = "acct_ver";

        private const int Iterations = 210_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly StoragePaths _paths;
        private readonly Lock _lock = new();
        private AdminAccount? _account;

        public AdminAccountStore(StoragePaths paths)
        {
            _paths = paths;
            if (File.Exists(paths.AccountFile))
            {
                _account = JsonSerializer.Deserialize<AdminAccount>(File.ReadAllText(paths.AccountFile), SiteContentStore.JsonOptions);
            }
        }

        public bool HasAccount => _account is not null;
        public string? Username => _account?.Username;
        public string? Version => _account?.Version;

        public void Create(string username, string password)
        {
            lock (_lock)
            {
                if (_account is not null)
                {
                    throw new InvalidOperationException("An admin account already exists.");
                }

                Save(NewAccount(username, password));
                if (File.Exists(_paths.SetupCodeFile))
                {
                    File.Delete(_paths.SetupCodeFile);
                }
            }
        }

        public bool Verify(string? username, string? password)
        {
            AdminAccount? account = _account;
            if (account is null || username is null || password is null)
            {
                return false;
            }

            byte[] expected = Convert.FromBase64String(account.PasswordHash);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(account.Salt), account.Iterations, Algorithm, expected.Length);
            bool userOk = string.Equals(username.Trim(), account.Username, StringComparison.OrdinalIgnoreCase);
            return CryptographicOperations.FixedTimeEquals(expected, actual) && userOk;
        }

        /// <summary>Changes the username and (optionally) the password. Signs out every other session.</summary>
        public void Update(string newUsername, string? newPassword)
        {
            lock (_lock)
            {
                AdminAccount current = _account ?? throw new InvalidOperationException("No admin account.");
                AdminAccount updated = string.IsNullOrEmpty(newPassword)
                    ? current with { Username = newUsername.Trim(), Version = Guid.NewGuid().ToString("N") }
                    : NewAccount(newUsername, newPassword);
                Save(updated);
            }
        }

        public string GetOrCreateSetupCode()
        {
            lock (_lock)
            {
                if (File.Exists(_paths.SetupCodeFile))
                {
                    return File.ReadAllText(_paths.SetupCodeFile).Trim();
                }

                string raw = RandomNumberGenerator.GetString(CodeAlphabet, 8);
                string code = raw[..4] + "-" + raw[4..];
                AtomicFile.WriteAllText(_paths.SetupCodeFile, code);
                return code;
            }
        }

        public bool CheckSetupCode(string? code)
        {
            if (HasAccount || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            static byte[] Normalize(string s)
            {
                return Encoding.UTF8.GetBytes(s.Replace("-", "").Replace(" ", "").ToUpperInvariant());
            }

            return CryptographicOperations.FixedTimeEquals(Normalize(code), Normalize(GetOrCreateSetupCode()));
        }

        public ClaimsPrincipal CreatePrincipal()
        {
            AdminAccount account = _account ?? throw new InvalidOperationException("No admin account.");
            ClaimsIdentity identity = new(
                [new Claim(ClaimTypes.Name, account.Username), new Claim(VersionClaim, account.Version)],
                CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>Cookie check on every request: sessions from before a password change are rejected.</summary>
        public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            AdminAccountStore store = context.HttpContext.RequestServices.GetRequiredService<AdminAccountStore>();
            string? version = context.Principal?.FindFirst(VersionClaim)?.Value;
            if (store.Version is null || version != store.Version)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        private static AdminAccount NewAccount(string username, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, 32);
            return new AdminAccount(username.Trim(), Convert.ToBase64String(hash), Convert.ToBase64String(salt), Iterations, Guid.NewGuid().ToString("N"));
        }

        private void Save(AdminAccount account)
        {
            AtomicFile.WriteAllText(_paths.AccountFile, JsonSerializer.Serialize(account, SiteContentStore.JsonOptions));
            _account = account;
        }
    }
}
