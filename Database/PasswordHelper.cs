using System;
using System.Security.Cryptography;
using System.Text;

namespace baranggaysystem1.Database
{
    internal static class PasswordHelper
    {
        private const string HashPrefix = "v1";
        private const int DefaultIterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        internal enum VerificationResult
        {
            Failed = 0,
            Success = 1,
            SuccessRehashNeeded = 2
        }

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password ?? string.Empty,
                salt,
                DefaultIterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{HashPrefix}.{DefaultIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static VerificationResult VerifyPassword(string password, string storedHash, out string? upgradedHash)
        {
            upgradedHash = null;

            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return VerificationResult.Failed;
            }

            if (TryVerifyVersioned(password, storedHash, out bool needsUpgrade))
            {
                if (needsUpgrade)
                {
                    upgradedHash = HashPassword(password);
                    return VerificationResult.SuccessRehashNeeded;
                }

                return VerificationResult.Success;
            }

            if (TryVerifyLegacySha256(password, storedHash))
            {
                upgradedHash = HashPassword(password);
                return VerificationResult.SuccessRehashNeeded;
            }

            return VerificationResult.Failed;
        }

        private static bool TryVerifyVersioned(string password, string storedHash, out bool needsUpgrade)
        {
            needsUpgrade = false;

            string[] parts = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || !parts[0].Equals(HashPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out int iterations) || iterations < 1_000)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);

                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password ?? string.Empty,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expected.Length);

                bool matches = CryptographicOperations.FixedTimeEquals(actual, expected);
                if (!matches)
                {
                    return false;
                }

                needsUpgrade = iterations < DefaultIterations;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryVerifyLegacySha256(string password, string storedHash)
        {
            if (!LooksLikeLegacySha256(storedHash))
            {
                return false;
            }

            string hashed = ComputeSha256Hex(password ?? string.Empty);
            return hashed.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeLegacySha256(string value)
        {
            if (value.Length != 64)
            {
                return false;
            }

            foreach (char c in value)
            {
                bool isHex = (c >= '0' && c <= '9')
                             || (c >= 'a' && c <= 'f')
                             || (c >= 'A' && c <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ComputeSha256Hex(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
