using System;
using System.CodeDom.Compiler;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public sealed partial class LoginViewModel : ViewModelBase
{
	private string _username = string.Empty;

	private string _errorMessage = string.Empty;

	private bool _hasError;

	public string Username
	{
		get
		{
			return _username;
		}
		set
		{
			SetProperty<string>(ref _username, value, "Username");
		}
	}

	public string ErrorMessage
	{
		get
		{
			return _errorMessage;
		}
		set
		{
			SetProperty<string>(ref _errorMessage, value, "ErrorMessage");
			HasError = !string.IsNullOrWhiteSpace(value);
		}
	}

	public bool HasError
	{
		get
		{
			return _hasError;
		}
		set
		{
			SetProperty<bool>(ref _hasError, value, "HasError");
		}
	}

	public event Action<bool>? LoginSucceeded;

	public event Action? RegisterRequested;

	[RelayCommand]
	public async Task LoginAsync(string password)
	{
		if (string.IsNullOrWhiteSpace(Username))
		{
			ErrorMessage = "Please enter your username.";
			return;
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			ErrorMessage = "Please enter your password.";
			return;
		}
		ErrorMessage = string.Empty;
		SetBusy(busy: true, "Signing in…");
		try
		{
			bool obj = await Task.Run(() => PerformLogin(Username, password));
			this.LoginSucceeded?.Invoke(obj);
		}
		catch (Exception ex)
		{
			ErrorMessage = "Login failed. Please try again.";
			AppLogger.LogError("Login exception.", ex);
		}
		finally
		{
			SetBusy(busy: false);
		}
	}

	[RelayCommand]
	public void Register()
	{
		this.RegisterRequested?.Invoke();
	}

	private static bool PerformLogin(string username, string password)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		if (OfflineDatabaseSupport.IsOffline || DbConnectionSettingsStore.IsSqliteSelected())
		{
			DBConnection.SetRuntimeSqliteSelection(isSelected: true);
			OfflineDatabaseSupport.ActivateOfflineMode();
			if (OfflineDatabaseSupport.TryAuthenticateOffline(username, password, out int userId, out int barangayId, out string role))
			{
				SetSession(userId, barangayId, role, username);
				return IsAdminRole(role);
			}
			throw new UnauthorizedAccessException("Invalid username or password.");
		}
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			MySqlCommand val = new MySqlCommand("SELECT ua.user_id,\n                                          ua.barangay_id,\n                                          COALESCE(r.name, 'Staff') AS role,\n                                          ua.password_hash\n                     FROM user_account ua\n                     LEFT JOIN user_role ur ON ur.user_id = ua.user_id\n                     LEFT JOIN role r ON r.role_id = ur.role_id\n                     WHERE ua.username=@username\n                     AND ua.is_active=1\n                     ORDER BY\n                        (r.name = 'Super Admin') DESC,\n                        (r.name = 'Admin') DESC\n                     LIMIT 1", connection);
			try
			{
				val.Parameters.AddWithValue("@username", (object)username);
				MySqlDataReader val2 = val.ExecuteReader();
				try
				{
					if (!((DbDataReader)(object)val2).Read())
					{
						throw new UnauthorizedAccessException("Invalid username or password.");
					}
					int userId2 = Convert.ToInt32(((DbDataReader)(object)val2)["user_id"]);
					int barangayId2 = ((((DbDataReader)(object)val2)["barangay_id"] == DBNull.Value) ? 1 : Convert.ToInt32(((DbDataReader)(object)val2)["barangay_id"]));
					string role2 = Convert.ToString(((DbDataReader)(object)val2)["role"]) ?? string.Empty;
					string storedHash = Convert.ToString(((DbDataReader)(object)val2)["password_hash"]) ?? string.Empty;
					((DbDataReader)(object)val2).Close();
					string upgradedHash;
					switch (PasswordHelper.VerifyPassword(password, storedHash, out upgradedHash))
					{
					case PasswordHelper.VerificationResult.Failed:
						throw new UnauthorizedAccessException("Invalid username or password.");
					case PasswordHelper.VerificationResult.SuccessRehashNeeded:
						if (!string.IsNullOrWhiteSpace(upgradedHash))
						{
							TryUpgradeHash(connection, userId2, upgradedHash);
						}
						break;
					}
					SetSession(userId2, barangayId2, role2, username);
					return IsAdminRole(role2);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)connection)?.Dispose();
		}
	}

	private static void SetSession(int userId, int barangayId, string role, string username)
	{
		UserSession.UserId = userId;
		UserSession.BarangayId = barangayId;
		UserSession.Role = role;
		UserSession.Username = username;
		Permissions.Refresh();
	}

	private static bool IsAdminRole(string role)
	{
		if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static void TryUpgradeHash(MySqlConnection conn, int userId, string hash)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		try
		{
			MySqlCommand val = new MySqlCommand("UPDATE user_account SET password_hash=@h, updated_at=NOW() WHERE user_id=@id", conn);
			try
			{
				val.Parameters.AddWithValue("@h", (object)hash);
				val.Parameters.AddWithValue("@id", (object)userId);
				((DbCommand)(object)val).ExecuteNonQuery();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
		}
	}
}
