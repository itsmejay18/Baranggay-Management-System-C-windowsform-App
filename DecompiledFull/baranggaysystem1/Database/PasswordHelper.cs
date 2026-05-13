using System;
using System.Security.Cryptography;
using System.Text;

namespace baranggaysystem1.Database;

internal static class PasswordHelper
{
	internal enum VerificationResult
	{
		Failed,
		Success,
		SuccessRehashNeeded
	}

	private const string HashPrefix = "v1";

	private const int DefaultIterations = 100000;

	private const int SaltSize = 16;

	private const int KeySize = 32;

	public static string HashPassword(string password)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(16);
		byte[] inArray = Rfc2898DeriveBytes.Pbkdf2(password ?? string.Empty, bytes, 100000, HashAlgorithmName.SHA256, 32);
		return $"{"v1"}.{100000}.{Convert.ToBase64String(bytes)}.{Convert.ToBase64String(inArray)}";
	}

	public static VerificationResult VerifyPassword(string password, string storedHash, out string? upgradedHash)
	{
		upgradedHash = null;
		if (string.IsNullOrWhiteSpace(storedHash))
		{
			return VerificationResult.Failed;
		}
		if (TryVerifyVersioned(password, storedHash, out var needsUpgrade))
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
		string[] array = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 4 || !array[0].Equals("v1", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!int.TryParse(array[1], out var result) || result < 1000)
		{
			return false;
		}
		try
		{
			byte[] salt = Convert.FromBase64String(array[2]);
			byte[] array2 = Convert.FromBase64String(array[3]);
			if (!CryptographicOperations.FixedTimeEquals(Rfc2898DeriveBytes.Pbkdf2(password ?? string.Empty, salt, result, HashAlgorithmName.SHA256, array2.Length), array2))
			{
				return false;
			}
			needsUpgrade = result < 100000;
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
		return ComputeSha256Hex(password ?? string.Empty).Equals(storedHash, StringComparison.OrdinalIgnoreCase);
	}

	private static bool LooksLikeLegacySha256(string value)
	{
		if (value.Length != 64)
		{
			return false;
		}
		foreach (char c in value)
		{
			if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
			{
				return false;
			}
		}
		return true;
	}

	private static string ComputeSha256Hex(string password)
	{
		byte[] array = SHA256.HashData(Encoding.UTF8.GetBytes(password));
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
