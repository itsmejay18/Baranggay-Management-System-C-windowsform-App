using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public sealed partial class RegisterViewModel : ViewModelBase
{
	private readonly RolePermissionService _roleService = new RolePermissionService();

	private string _username = string.Empty;

	private string _errorMessage = string.Empty;

	private bool _hasError;

	private string? _photoPath;

	private string _selectedRole = string.Empty;

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

	public string? PhotoPath
	{
		get
		{
			return _photoPath;
		}
		set
		{
			SetProperty<string>(ref _photoPath, value, "PhotoPath");
		}
	}

	public string SelectedRole
	{
		get
		{
			return _selectedRole;
		}
		set
		{
			SetProperty<string>(ref _selectedRole, value, "SelectedRole");
		}
	}

	public ObservableCollection<string> Roles { get; } = new ObservableCollection<string>();

	public event Action? BackToLoginRequested;

	public event Action? RegistrationCompleted;

	public async Task LoadAsync()
	{
		try
		{
			IReadOnlyList<string> obj = await _roleService.GetRoleNameOptionsAsync();
			Roles.Clear();
			foreach (string item in obj)
			{
				Roles.Add(item);
			}
			if (string.IsNullOrWhiteSpace(SelectedRole))
			{
				SelectedRole = ((Roles.Count > 0) ? Roles[0] : "Staff");
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("RegisterViewModel: could not load roles.", ex);
		}
	}

	[RelayCommand]
	public void UploadPhoto()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Select staff photo",
			Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
			Multiselect = false
		};
		if (openFileDialog.ShowDialog() == true)
		{
			PhotoPath = openFileDialog.FileName;
		}
	}

	[RelayCommand]
	public void RemovePhoto()
	{
		PhotoPath = null;
	}

	[RelayCommand]
	public void BackToLogin()
	{
		this.BackToLoginRequested?.Invoke();
	}

	[RelayCommand]
	public async Task RegisterAsync(string password)
	{
		ErrorMessage = string.Empty;
		ValidationResult validationResult = ValidationService.ValidateRegistration(Username, password, SelectedRole);
		if (!validationResult.IsValid)
		{
			ErrorMessage = (string.IsNullOrWhiteSpace(validationResult.Title) ? validationResult.Message : (validationResult.Title + ": " + validationResult.Message));
			return;
		}
		SetBusy(busy: true, "Creating account…");
		try
		{
			await Task.Run(delegate
			{
				CreateUser(Username, password, SelectedRole, PhotoPath);
			});
			this.RegistrationCompleted?.Invoke();
		}
		catch (Exception ex)
		{
			ErrorMessage = "Registration failed. Please try again.";
			AppLogger.LogError("RegisterViewModel.RegisterAsync failed.", ex);
		}
		finally
		{
			SetBusy(busy: false);
		}
	}

	private static void CreateUser(string username, string password, string role, string? photoPath)
	{
		string hash = PasswordHelper.HashPassword(password);
		if (OfflineDatabaseSupport.IsOffline || DbConnectionSettingsStore.IsSqliteSelected())
		{
			DBConnection.SetRuntimeSqliteSelection(isSelected: true);
			OfflineDatabaseSupport.ActivateOfflineMode();
			CreateUserOffline(username, hash, role, photoPath);
		}
		else
		{
			CreateUserOnline(username, hash, role, photoPath);
		}
	}

	private static void CreateUserOnline(string username, string hash, string role, string? photoPath)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		MySqlConnection connection = DBConnection.GetConnection();
		try
		{
			((DbConnection)(object)connection).Open();
			SchemaBootstrap.EnsureCoreDefaults(connection);
			MySqlTransaction val = connection.BeginTransaction();
			try
			{
				int num = EnsureRole(connection, role, val);
				MySqlCommand val2 = new MySqlCommand("INSERT INTO user_account\n                (barangay_id, username, password_hash, full_name, is_active, photo_url, created_at, updated_at)\n                VALUES (@bid, @usr, @pwd, @fn, 1, @photo, NOW(), NOW())", connection, val);
				try
				{
					val2.Parameters.AddWithValue("@bid", (object)1);
					val2.Parameters.AddWithValue("@usr", (object)username);
					val2.Parameters.AddWithValue("@pwd", (object)hash);
					val2.Parameters.AddWithValue("@fn", (object)username);
					val2.Parameters.AddWithValue("@photo", (object)(string.IsNullOrWhiteSpace(photoPath) ? ((IConvertible)DBNull.Value) : ((IConvertible)photoPath)));
					((DbCommand)(object)val2).ExecuteNonQuery();
					int num2 = (int)val2.LastInsertedId;
					MySqlCommand val3 = new MySqlCommand("INSERT INTO user_role (user_id, role_id) VALUES (@u, @r)", connection, val);
					try
					{
						val3.Parameters.AddWithValue("@u", (object)num2);
						val3.Parameters.AddWithValue("@r", (object)num);
						((DbCommand)(object)val3).ExecuteNonQuery();
						object afterState = ReadAuditSnap(connection, num2, val);
						AuditTrailService.LogTransactional(connection, val, "Users", "user_account", num2, "CREATE", null, afterState, "User account created with role '" + role + "'.");
						((DbTransaction)(object)val).Commit();
					}
					finally
					{
						((IDisposable)val3)?.Dispose();
					}
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

	private static void CreateUserOffline(string username, string hash, string role, string? photoPath)
	{
		if (!OfflineDatabaseSupport.EnsureInitialised())
		{
			throw new InvalidOperationException("The SQLite database file is not ready yet.");
		}
		SqliteConnection connection = OfflineDatabaseSupport.GetConnection();
		try
		{
			SqliteTransaction val = connection.BeginTransaction();
			try
			{
				long num = EnsureRoleOffline(connection, role, val);
				long nextOfflineId = GetNextOfflineId(connection, val, "user_account", "user_id");
				SqliteCommand val2 = connection.CreateCommand();
				try
				{
					val2.Transaction = val;
					((DbCommand)(object)val2).CommandText = "\n                    INSERT INTO user_account\n                        (user_id, barangay_id, username, password_hash, full_name, is_active, photo_url, created_at, updated_at, sync_status)\n                    VALUES\n                        ($userId, $barangayId, $username, $passwordHash, $fullName, 1, $photoPath, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, 'dirty');";
					val2.Parameters.AddWithValue("$userId", (object)nextOfflineId);
					val2.Parameters.AddWithValue("$barangayId", (object)1);
					val2.Parameters.AddWithValue("$username", (object)username);
					val2.Parameters.AddWithValue("$passwordHash", (object)hash);
					val2.Parameters.AddWithValue("$fullName", (object)username);
					val2.Parameters.AddWithValue("$photoPath", (object)(string.IsNullOrWhiteSpace(photoPath) ? ((IConvertible)DBNull.Value) : ((IConvertible)photoPath)));
					((DbCommand)(object)val2).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
				long nextOfflineId2 = GetNextOfflineId(connection, val, "user_role", "user_role_id");
				SqliteCommand val3 = connection.CreateCommand();
				try
				{
					val3.Transaction = val;
					((DbCommand)(object)val3).CommandText = "\n                    INSERT INTO user_role (user_role_id, user_id, role_id, sync_status)\n                    VALUES ($userRoleId, $userId, $roleId, 'dirty');";
					val3.Parameters.AddWithValue("$userRoleId", (object)nextOfflineId2);
					val3.Parameters.AddWithValue("$userId", (object)nextOfflineId);
					val3.Parameters.AddWithValue("$roleId", (object)num);
					((DbCommand)(object)val3).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
				((DbTransaction)(object)val).Commit();
				AuditTrailService.Log("Users", "user_account", nextOfflineId, "CREATE", null, new
				{
					UserId = nextOfflineId,
					Username = username,
					FullName = username,
					IsActive = true,
					PhotoUrl = (photoPath ?? string.Empty),
					Role = role
				}, "User account created with role '" + role + "'.");
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

	private static int EnsureRole(MySqlConnection conn, string roleName, MySqlTransaction? tx)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		if (string.IsNullOrWhiteSpace(roleName))
		{
			roleName = "Staff";
		}
		MySqlCommand val = new MySqlCommand("SELECT role_id FROM role WHERE name=@n LIMIT 1", conn);
		try
		{
			val.Transaction = tx;
			val.Parameters.AddWithValue("@n", (object)roleName);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj != null && obj != DBNull.Value)
			{
				return Convert.ToInt32(obj);
			}
			MySqlCommand val2 = new MySqlCommand("INSERT INTO role (name) VALUES (@n)", conn);
			try
			{
				val2.Transaction = tx;
				val2.Parameters.AddWithValue("@n", (object)roleName);
				((DbCommand)(object)val2).ExecuteNonQuery();
				return (int)val2.LastInsertedId;
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

	private static long EnsureRoleOffline(SqliteConnection conn, string roleName, SqliteTransaction tx)
	{
		roleName = (string.IsNullOrWhiteSpace(roleName) ? "Staff" : roleName.Trim());
		SqliteCommand val = conn.CreateCommand();
		try
		{
			val.Transaction = tx;
			((DbCommand)(object)val).CommandText = "SELECT role_id FROM role WHERE name = $name LIMIT 1;";
			val.Parameters.AddWithValue("$name", (object)roleName);
			object obj = ((DbCommand)(object)val).ExecuteScalar();
			if (obj != null && obj != DBNull.Value)
			{
				return Convert.ToInt64(obj);
			}
			long nextOfflineId = GetNextOfflineId(conn, tx, "role", "role_id");
			SqliteCommand val2 = conn.CreateCommand();
			try
			{
				val2.Transaction = tx;
				((DbCommand)(object)val2).CommandText = "\n                INSERT INTO role (role_id, name, description, sync_status)\n                VALUES ($roleId, $name, $description, 'dirty');";
				val2.Parameters.AddWithValue("$roleId", (object)nextOfflineId);
				val2.Parameters.AddWithValue("$name", (object)roleName);
				val2.Parameters.AddWithValue("$description", (object)(roleName + " account."));
				((DbCommand)(object)val2).ExecuteNonQuery();
				return nextOfflineId;
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

	private static long GetNextOfflineId(SqliteConnection conn, SqliteTransaction tx, string tableName, string columnName)
	{
		SqliteCommand val = conn.CreateCommand();
		try
		{
			val.Transaction = tx;
			((DbCommand)(object)val).CommandText = $"SELECT IFNULL(MAX({columnName}), 0) + 1 FROM {tableName};";
			return Convert.ToInt64(((DbCommand)(object)val).ExecuteScalar() ?? ((object)1L));
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static object? ReadAuditSnap(MySqlConnection conn, int userId, MySqlTransaction? tx)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		MySqlCommand val = new MySqlCommand("SELECT ua.user_id, ua.username, ua.full_name, ua.is_active,\n                         ua.photo_url, COALESCE(r.name, 'Staff') AS role_name\n                  FROM user_account ua\n                  LEFT JOIN user_role ur ON ur.user_id = ua.user_id\n                  LEFT JOIN role r ON r.role_id = ur.role_id\n                  WHERE ua.user_id=@id LIMIT 1", conn);
		try
		{
			val.Transaction = tx;
			val.Parameters.AddWithValue("@id", (object)userId);
			MySqlDataReader val2 = val.ExecuteReader();
			try
			{
				if (!((DbDataReader)(object)val2).Read())
				{
					return null;
				}
				return new
				{
					UserId = Convert.ToInt32(((DbDataReader)(object)val2)["user_id"]),
					Username = (Convert.ToString(((DbDataReader)(object)val2)["username"]) ?? string.Empty),
					FullName = (Convert.ToString(((DbDataReader)(object)val2)["full_name"]) ?? string.Empty),
					IsActive = (((DbDataReader)(object)val2)["is_active"] != DBNull.Value && Convert.ToInt32(((DbDataReader)(object)val2)["is_active"]) == 1),
					PhotoUrl = (Convert.ToString(((DbDataReader)(object)val2)["photo_url"]) ?? string.Empty),
					Role = (Convert.ToString(((DbDataReader)(object)val2)["role_name"]) ?? "Staff")
				};
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
}
