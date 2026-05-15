using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

public static class SystemConfigService
{
	public const string KeySystemName = "system_name";

	public const string KeyBarangayName = "barangay_name";

	public const string KeyMunicipality = "municipality";

	public const string KeyProvince = "province";

	public const string KeyRegion = "region";

	public const string KeyLogoBase64 = "logo_base64";

	public const string KeyOfficeAddress = "office_address";

	public const string KeyContactNumber = "contact_number";

	public const string KeyOfficialEmail = "official_email";

	private const int MaxLogoBytes = 2097152;

	private static volatile bool _tableEnsured;

	public static void EnsureTable()
	{
		if (_tableEnsured) return;
		try
		{
			DbHelper.ExecuteNonQuery("\n                    CREATE TABLE IF NOT EXISTS system_config (\n                        config_key   VARCHAR(80) NOT NULL PRIMARY KEY,\n                        config_value TEXT       NULL,\n                        updated_at   DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP\n                    );");
			_tableEnsured = true;
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("SystemConfigService.EnsureTable failed. Will use defaults.", ex);
		}
	}

	public static string Get(string key, string defaultValue = "")
	{
		EnsureTable();
		try
		{
			string text = DbHelper.ExecuteScalar<string>("SELECT config_value FROM system_config WHERE config_key = @k LIMIT 1", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@k", (object)key);
			});
			return string.IsNullOrEmpty(text) ? defaultValue : text;
		}
		catch
		{
			return defaultValue;
		}
	}

	public static string GetSystemName()
	{
		return Get("system_name", "Barangay Management System");
	}

	public static string GetBarangayName()
	{
		return Get("barangay_name", "Barangay San Jose");
	}

	public static SystemBrandingSettings LoadBrandingSettings()
	{
		EnsureTable();
		try
		{
			// Batch-load all branding keys in a single query for performance
			var settings = new SystemBrandingSettings
			{
				SystemName = "Barangay Management System",
				BarangayName = "Barangay San Jose",
				Municipality = "Municipality",
				Province = "Province",
				Region = "Region"
			};
			var table = DbHelper.LoadTable(
				"SELECT config_key, config_value FROM system_config WHERE config_key IN ('system_name','barangay_name','municipality','province','region')");
			foreach (System.Data.DataRow row in table.Rows)
			{
				string key = row["config_key"]?.ToString() ?? "";
				string val = row["config_value"]?.ToString() ?? "";
				if (string.IsNullOrEmpty(val)) continue;
				switch (key)
				{
					case "system_name": settings.SystemName = val; break;
					case "barangay_name": settings.BarangayName = val; break;
					case "municipality": settings.Municipality = val; break;
					case "province": settings.Province = val; break;
					case "region": settings.Region = val; break;
				}
			}
			return settings;
		}
		catch
		{
			return new SystemBrandingSettings
			{
				SystemName = "Barangay Management System",
				BarangayName = "Barangay San Jose",
				Municipality = "Municipality",
				Province = "Province",
				Region = "Region"
			};
		}
	}

	public static void SaveBrandingSettings(SystemBrandingSettings settings)
	{
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		Set("system_name", NormalizeRequired(settings.SystemName, "System name"));
		Set("barangay_name", NormalizeRequired(settings.BarangayName, "Official barangay name"));
		Set("municipality", NormalizeRequired(settings.Municipality, "City / Municipality"));
		Set("province", NormalizeRequired(settings.Province, "Province"));
		Set("region", NormalizeRequired(settings.Region, "Region"));
	}

	public static void ResetBrandingSettings()
	{
		SaveBrandingSettings(SystemBrandingSettings.CreateDefault());
		RemoveLogo();
	}

	public static SystemOfficeSettings LoadOfficeSettings()
	{
		EnsureTable();
		try
		{
			// Batch-load all office keys in a single query for performance
			var settings = new SystemOfficeSettings
			{
				OfficeAddress = "",
				ContactNumber = "",
				OfficialEmail = ""
			};
			var table = DbHelper.LoadTable(
				"SELECT config_key, config_value FROM system_config WHERE config_key IN ('office_address','contact_number','official_email')");
			foreach (System.Data.DataRow row in table.Rows)
			{
				string key = row["config_key"]?.ToString() ?? "";
				string val = row["config_value"]?.ToString() ?? "";
				if (string.IsNullOrEmpty(val)) continue;
				switch (key)
				{
					case "office_address": settings.OfficeAddress = val; break;
					case "contact_number": settings.ContactNumber = val; break;
					case "official_email": settings.OfficialEmail = val; break;
				}
			}
			return settings;
		}
		catch
		{
			return SystemOfficeSettings.CreateDefault();
		}
	}

	public static void SaveOfficeSettings(SystemOfficeSettings settings)
	{
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		Set("office_address", NormalizeOptional(settings.OfficeAddress));
		Set("contact_number", NormalizeOptional(settings.ContactNumber));
		Set("official_email", NormalizeOptional(settings.OfficialEmail));
	}

	public static void ResetOfficeSettings()
	{
		SaveOfficeSettings(SystemOfficeSettings.CreateDefault());
	}

	public static void Set(string key, string? value)
	{
		EnsureTable();
		try
		{
			DbHelper.ExecuteNonQuery("REPLACE INTO system_config (config_key, config_value, updated_at)\n                      VALUES (@k, @v, CURRENT_TIMESTAMP)", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@k", (object)key);
				cmd.Parameters.AddWithValue("@v", ToDatabaseValue(value));
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("SystemConfigService.Set(" + key + ") failed.", ex);
			throw;
		}
	}

	public static void Delete(string key)
	{
		EnsureTable();
		try
		{
			DbHelper.ExecuteNonQuery("DELETE FROM system_config WHERE config_key = @k", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@k", (object)key);
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("SystemConfigService.Delete(" + key + ") failed.", ex);
			throw;
		}
	}

	public static BitmapImage? GetLogo()
	{
		try
		{
			string text = Get("logo_base64");
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return CreateBitmapImage(LogoImageSanitizer.NormalizeLogoImage(Convert.FromBase64String(text)));
		}
		catch
		{
			return null;
		}
	}

	public static void SaveLogoFromFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			throw new FileNotFoundException("The selected logo file could not be found.", filePath);
		}
		byte[] array = LogoImageSanitizer.NormalizeLogoImage(File.ReadAllBytes(filePath));
		if (array.Length > 2097152)
		{
			throw new InvalidOperationException("Logo image is too large. Please use an image under 2 MB.");
		}
		string value = Convert.ToBase64String(array);
		Set("logo_base64", value);
	}

	public static void RemoveLogo()
	{
		Delete("logo_base64");
	}

	private static string NormalizeRequired(string? value, string fieldLabel)
	{
		string obj = value?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(obj))
		{
			throw new InvalidOperationException(fieldLabel + " is required.");
		}
		return obj;
	}

	private static string NormalizeOptional(string? value)
	{
		return value?.Trim() ?? string.Empty;
	}

	private static object ToDatabaseValue(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static BitmapImage CreateBitmapImage(byte[] bytes)
	{
		BitmapImage bitmapImage = new BitmapImage();
		using MemoryStream streamSource = new MemoryStream(bytes);
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = streamSource;
		bitmapImage.EndInit();
		((Freezable)bitmapImage).Freeze();
		return bitmapImage;
	}
}
