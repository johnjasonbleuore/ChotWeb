using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ChotWeb.Services
{
    /// <summary>
    /// One-time 6-digit sign-in codes, kept in memory only (a restart simply cancels pending codes).
    /// Each code lasts 10 minutes, allows 5 guesses, and a new one can be requested once a minute.
    /// Only a keyed hash of each code is stored.
    /// </summary>
    public class LoginCodeStore(TimeProvider clock)
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(60);
        public const int MaxAttempts = 5;

        private sealed class Entry
        {
            public required byte[] Hash { get; init; }
            public required DateTimeOffset IssuedAt { get; init; }
            public int Attempts { get; set; }
        }

        private readonly ConcurrentDictionary<string, Entry> _codes = new(StringComparer.Ordinal);
        private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

        /// <summary>Creates a new code for the email, or returns false if one was sent less than a minute ago.</summary>
        public bool TryIssue(string email, out string code, out TimeSpan retryAfter)
        {
            DateTimeOffset now = clock.GetUtcNow();
            RemoveExpired(now);

            if (_codes.TryGetValue(email, out Entry? existing) && now - existing.IssuedAt < ResendDelay)
            {
                code = "";
                retryAfter = ResendDelay - (now - existing.IssuedAt);
                return false;
            }

            code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _codes[email] = new Entry { Hash = Hash(email, code), IssuedAt = now };
            retryAfter = TimeSpan.Zero;
            return true;
        }

        /// <summary>Cancels a code that could not be emailed, so the person can ask again straight away.</summary>
        public void Cancel(string email)
        {
            _codes.TryRemove(email, out _);
        }

        public bool Verify(string email, string? code)
        {
            string digits = new((code ?? "").Where(char.IsAsciiDigit).ToArray());
            if (!_codes.TryGetValue(email, out Entry? entry))
            {
                return false;
            }

            lock (entry)
            {
                bool expired = clock.GetUtcNow() - entry.IssuedAt > Lifetime;
                if (expired || entry.Attempts >= MaxAttempts)
                {
                    _codes.TryRemove(KeyValuePair.Create(email, entry));
                    return false;
                }

                entry.Attempts++;
                if (digits.Length == 6 && CryptographicOperations.FixedTimeEquals(entry.Hash, Hash(email, digits)))
                {
                    _codes.TryRemove(KeyValuePair.Create(email, entry)); // each code works once
                    return true;
                }

                if (entry.Attempts >= MaxAttempts)
                {
                    _codes.TryRemove(KeyValuePair.Create(email, entry));
                }

                return false;
            }
        }

        private byte[] Hash(string email, string code)
        {
            return HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(email + "\n" + code));
        }

        private void RemoveExpired(DateTimeOffset now)
        {
            foreach (KeyValuePair<string, Entry> pair in _codes)
            {
                if (now - pair.Value.IssuedAt > Lifetime)
                {
                    _codes.TryRemove(pair);
                }
            }
        }
    }
}
