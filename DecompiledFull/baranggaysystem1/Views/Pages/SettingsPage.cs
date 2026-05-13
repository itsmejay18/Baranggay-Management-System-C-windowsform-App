using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using baranggaysystem1.Database;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Pages;

public class SettingsPage : UserControl, IComponentConnector
{
	private bool _isLoadingConnectionProfile;

	private bool _isBackupOperationRunning;

	internal TextBox txtSystemName;

	internal TextBox txtBarangayName;

	internal TextBox txtMunicipality;

	internal TextBox txtProvince;

	internal TextBox txtRegion;

	internal Button btnSaveBranding;

	internal Button btnResetBranding;

	internal Image logoPreview;

	internal StackPanel logoPreviewFallback;

	internal Button btnUploadLogo;

	internal Button btnRemoveLogo;

	internal TextBox txtOfficeAddress;

	internal TextBox txtContactNumber;

	internal TextBox txtOfficialEmail;

	internal Button btnSaveOfficeSettings;

	internal Button btnResetOfficeSettings;

	internal ComboBox cmbConnectionProfile;

	internal TextBlock lblConnectionProfileDescription;

	internal Grid pnlMySqlConnectionFields;

	internal TextBox txtDbHost;

	internal TextBox txtDbUser;

	internal PasswordBox txtDbPassword;

	internal TextBox txtDbPort;

	internal TextBox txtDbName;

	internal CheckBox chkDbUseSsl;

	internal Border pnlSqliteDetails;

	internal TextBox txtSqlitePath;

	internal Button btnTestConnection;

	internal Button btnSaveConnection;

	internal Button btnResetConnection;

	internal Button btnRunFullBackup;

	internal Button btnRunIncrementalBackup;

	internal Button btnRunDifferentialBackup;

	internal Button btnOpenBackupFolder;

	internal Button btnRestoreBackup;

	internal TextBlock lblBackupSummary;

	internal TextBlock lblBackupMeta;

	internal TextBlock lblBackupDirectory;

	internal TextBlock lblEnvironmentSummary;

	internal TextBlock lblBrandingSummary;

	internal TextBlock lblConnectionSummary;

	internal TextBlock lblConnectionModeSummary;

	internal TextBlock lblLogoSummary;

	private bool _contentLoaded;

	public SettingsPage()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			LoadAll();
		};
	}

	public SettingsPage(string route)
		: this()
	{
	}

	private void LoadAll()
	{
		try
		{
			SystemConfigService.EnsureTable();
			LoadBrandingSettings();
			LoadOfficeSettings();
			LoadDatabaseSettings();
			RefreshLogoPreview();
			RefreshEnvironmentSummary();
			RefreshBackupSummary();
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to load settings: " + ex.Message);
		}
	}

	private void LoadBrandingSettings()
	{
		SystemBrandingSettings systemBrandingSettings = SystemConfigService.LoadBrandingSettings();
		txtSystemName.Text = systemBrandingSettings.SystemName;
		txtBarangayName.Text = systemBrandingSettings.BarangayName;
		txtMunicipality.Text = systemBrandingSettings.Municipality;
		txtProvince.Text = systemBrandingSettings.Province;
		txtRegion.Text = systemBrandingSettings.Region;
	}

	private void LoadOfficeSettings()
	{
		SystemOfficeSettings systemOfficeSettings = SystemConfigService.LoadOfficeSettings();
		txtOfficeAddress.Text = systemOfficeSettings.OfficeAddress;
		txtContactNumber.Text = systemOfficeSettings.ContactNumber;
		txtOfficialEmail.Text = systemOfficeSettings.OfficialEmail;
	}

	private void LoadDatabaseSettings()
	{
		_isLoadingConnectionProfile = true;
		try
		{
			IReadOnlyList<DatabaseConnectionOption> availableOptions = DbConnectionSettingsStore.GetAvailableOptions();
			cmbConnectionProfile.DisplayMemberPath = "DisplayName";
			cmbConnectionProfile.SelectedValuePath = "Key";
			cmbConnectionProfile.ItemsSource = availableOptions;
			cmbConnectionProfile.SelectedValue = DbConnectionSettingsStore.LoadSelectedProfileKeyOrDefault();
			DatabaseConnectionOption databaseConnectionOption = (cmbConnectionProfile.SelectedItem as DatabaseConnectionOption) ?? availableOptions.First();
			cmbConnectionProfile.SelectedItem = databaseConnectionOption;
			ApplyConnectionOption(databaseConnectionOption);
		}
		finally
		{
			_isLoadingConnectionProfile = false;
		}
	}

	private void ApplyConnectionOption(DatabaseConnectionOption option)
	{
		DatabaseConnectionProfile profile = option.Profile;
		bool usesSqlite = option.UsesSqlite;
		txtDbHost.Text = profile.Server;
		txtDbPort.Text = profile.Port.ToString();
		txtDbName.Text = profile.Database;
		txtDbUser.Text = profile.Username;
		txtDbPassword.Password = profile.Password;
		chkDbUseSsl.IsChecked = profile.UseSsl;
		txtSqlitePath.Text = (string.IsNullOrWhiteSpace(option.SqliteFilePath) ? OfflineDatabaseSupport.GetDatabasePath() : option.SqliteFilePath);
		lblConnectionProfileDescription.Text = (usesSqlite ? (option.Description + " The app will use this local file immediately after you save.") : (option.IsEditable ? option.Description : (option.Description + " Select Custom if you want to manually edit connection values.")));
		SetConnectionInputsEnabled(option.IsEditable);
		pnlMySqlConnectionFields.Visibility = (usesSqlite ? Visibility.Collapsed : Visibility.Visible);
		pnlSqliteDetails.Visibility = ((!usesSqlite) ? Visibility.Collapsed : Visibility.Visible);
		btnTestConnection.Content = (usesSqlite ? "Check SQLite File" : "Test Connectivity");
		btnSaveConnection.Content = (usesSqlite ? "Use SQLite" : "Save Connection");
	}

	private void SetConnectionInputsEnabled(bool isEnabled)
	{
		txtDbHost.IsEnabled = isEnabled;
		txtDbPort.IsEnabled = isEnabled;
		txtDbName.IsEnabled = isEnabled;
		txtDbUser.IsEnabled = isEnabled;
		txtDbPassword.IsEnabled = isEnabled;
		chkDbUseSsl.IsEnabled = isEnabled;
	}

	private DatabaseConnectionOption GetSelectedConnectionOption()
	{
		return (cmbConnectionProfile.SelectedItem as DatabaseConnectionOption) ?? DbConnectionSettingsStore.GetSelectedOptionOrDefault();
	}

	private string GetSelectedConnectionProfileKey()
	{
		return GetSelectedConnectionOption().Key;
	}

	private DatabaseConnectionProfile GetCurrentConnectionProfile()
	{
		if (!GetSelectedConnectionOption().UsesSqlite)
		{
			return BuildConnectionProfileFromInputs();
		}
		return DatabaseConnectionProfile.CreateDefault();
	}

	private void RefreshLogoPreview()
	{
		BitmapImage logo = SystemConfigService.GetLogo();
		if (logo != null)
		{
			logoPreview.Source = logo;
			logoPreview.Visibility = Visibility.Visible;
			logoPreviewFallback.Visibility = Visibility.Collapsed;
		}
		else
		{
			logoPreview.Source = null;
			logoPreview.Visibility = Visibility.Collapsed;
			logoPreviewFallback.Visibility = Visibility.Visible;
		}
	}

	private void RefreshEnvironmentSummary()
	{
		SystemBrandingSettings systemBrandingSettings = SystemConfigService.LoadBrandingSettings();
		DatabaseConnectionOption selectedOptionOrDefault = DbConnectionSettingsStore.GetSelectedOptionOrDefault();
		DatabaseConnectionProfile profile = selectedOptionOrDefault.Profile;
		bool flag = SystemConfigService.GetLogo() != null;
		lblEnvironmentSummary.Text = systemBrandingSettings.SystemName + " - WPF Professional Edition";
		lblBrandingSummary.Text = $"{systemBrandingSettings.BarangayName} | {systemBrandingSettings.Municipality}, {systemBrandingSettings.Province}, {systemBrandingSettings.Region}";
		lblConnectionSummary.Text = (selectedOptionOrDefault.UsesSqlite ? ("SQLite file / " + OfflineDatabaseSupport.GetDatabasePath()) : $"{profile.Server}:{profile.Port} / {profile.Database} / SSL {(profile.UseSsl ? "enabled" : "disabled")}");
		lblConnectionModeSummary.Text = "Selected profile: " + selectedOptionOrDefault.DisplayName;
		lblLogoSummary.Text = (flag ? "Custom logo is active in the application shell." : "Default logo is active in the application shell.");
	}

	private void RefreshBackupSummary()
	{
		SetBackupButtonsEnabled(!_isBackupOperationRunning);
		lblBackupDirectory.Text = "Backup folder: " + BackupService.GetBackupDirectory();
		BackupRunInfo backupRunInfo = BackupService.TryGetLatestRun();
		if ((object)backupRunInfo != null && backupRunInfo.State == BackupRunState.Running)
		{
			backupRunInfo = BackupService.MarkInterruptedRunAsFailed(backupRunInfo);
		}
		if (backupRunInfo == null)
		{
			lblBackupSummary.Text = "No recorded backups yet.";
			lblBackupMeta.Text = BackupService.GetCurrentBackupSourceSummary() + Environment.NewLine + "Run a full backup to create your first restore point.";
			return;
		}
		string text = backupRunInfo.State switch
		{
			BackupRunState.Success => "completed successfully", 
			BackupRunState.Failed => "failed", 
			BackupRunState.Running => "is still running", 
			_ => "finished with an unknown state", 
		};
		lblBackupSummary.Text = ToDisplayMode(backupRunInfo.Mode) + " backup " + text + ".";
		string value = (string.IsNullOrWhiteSpace(backupRunInfo.FilePath) ? "No backup file recorded." : (Path.GetFileName(backupRunInfo.FilePath) + " (" + FormatFileSize(backupRunInfo.FileSizeBytes) + ")"));
		string value2 = (backupRunInfo.BaselineStartedAt.HasValue ? $" Baseline: {backupRunInfo.BaselineStartedAt.Value:MMM dd, yyyy hh:mm tt}." : string.Empty);
		string value3 = (string.IsNullOrWhiteSpace(backupRunInfo.TargetDescription) ? string.Empty : (" Target: " + backupRunInfo.TargetDescription + "."));
		lblBackupMeta.Text = ((backupRunInfo.State == BackupRunState.Failed) ? $"Started {backupRunInfo.StartedAt:MMM dd, yyyy hh:mm tt}.{value3} {backupRunInfo.ErrorMessage ?? "Backup failed."}".Trim() : $"Started {backupRunInfo.StartedAt:MMM dd, yyyy hh:mm tt}. File: {value}.{value2}{value3}");
	}

	private void SetBackupButtonsEnabled(bool isEnabled)
	{
		btnRunFullBackup.IsEnabled = isEnabled;
		bool flag = !OfflineDatabaseSupport.IsOffline && !DbConnectionSettingsStore.IsSqliteSelected();
		btnRunIncrementalBackup.IsEnabled = isEnabled && flag;
		btnRunDifferentialBackup.IsEnabled = isEnabled && flag;
		btnOpenBackupFolder.IsEnabled = isEnabled;
		btnRestoreBackup.IsEnabled = isEnabled && flag;
	}

	private async Task RunBackupAsync(BackupMode mode)
	{
		if (_isBackupOperationRunning)
		{
			return;
		}
		try
		{
			_isBackupOperationRunning = true;
			SetBackupButtonsEnabled(isEnabled: false);
			lblBackupSummary.Text = "Running " + ToDisplayMode(mode).ToLowerInvariant() + " backup...";
			lblBackupMeta.Text = BackupService.GetCurrentBackupSourceSummary() + Environment.NewLine + "Please keep this window open until the backup operation finishes.";
			BackupRunInfo backupRunInfo = await Task.Run(() => mode switch
			{
				BackupMode.Incremental => BackupService.RunIncrementalBackupNow(UserSession.UserId), 
				BackupMode.Differential => BackupService.RunDifferentialBackupNow(UserSession.UserId), 
				_ => BackupService.RunFullBackupNow(UserSession.UserId), 
			});
			RefreshBackupSummary();
			if (backupRunInfo.State == BackupRunState.Success)
			{
				DialogService.Instance.ShowInfo(string.IsNullOrWhiteSpace(backupRunInfo.FilePath) ? (ToDisplayMode(backupRunInfo.Mode) + " backup completed successfully.") : $"{ToDisplayMode(backupRunInfo.Mode)} backup completed successfully.\n\nSource:\n{backupRunInfo.TargetDescription ?? BackupService.GetCurrentBackupSourceSummary()}\n\nSaved to:\n{backupRunInfo.FilePath}", "Backup Complete");
			}
			else
			{
				DialogService.Instance.ShowError(backupRunInfo.ErrorMessage ?? "Backup failed. Check the active database source and backup client tools.", "Backup Failed");
			}
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Backup failed: " + ex.Message, "Backup Failed");
		}
		finally
		{
			_isBackupOperationRunning = false;
			SetBackupButtonsEnabled(isEnabled: true);
			RefreshBackupSummary();
		}
	}

	private void BtnSaveBranding_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			SystemConfigService.SaveBrandingSettings(new SystemBrandingSettings
			{
				SystemName = txtSystemName.Text,
				BarangayName = txtBarangayName.Text,
				Municipality = txtMunicipality.Text,
				Province = txtProvince.Text,
				Region = txtRegion.Text
			});
			LoadAll();
			RefreshShellBranding();
			DialogService.Instance.ShowInfo("Branding and identity settings have been saved.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to save branding settings: " + ex.Message);
		}
	}

	private void BtnResetBranding_Click(object sender, RoutedEventArgs e)
	{
		if (!DialogService.Instance.Confirm("Restore branding, identity, and logo to their default values?"))
		{
			return;
		}
		try
		{
			SystemConfigService.ResetBrandingSettings();
			LoadAll();
			RefreshShellBranding();
			DialogService.Instance.ShowInfo("Branding settings were restored to defaults.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to reset branding settings: " + ex.Message);
		}
	}

	private void BtnSaveOfficeSettings_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			SystemConfigService.SaveOfficeSettings(new SystemOfficeSettings
			{
				OfficeAddress = txtOfficeAddress.Text,
				ContactNumber = txtContactNumber.Text,
				OfficialEmail = txtOfficialEmail.Text
			});
			LoadAll();
			DialogService.Instance.ShowInfo("Office and contact settings have been saved.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to save office settings: " + ex.Message);
		}
	}

	private void BtnResetOfficeSettings_Click(object sender, RoutedEventArgs e)
	{
		if (!DialogService.Instance.Confirm("Clear the saved office address, contact number, and official email?"))
		{
			return;
		}
		try
		{
			SystemConfigService.ResetOfficeSettings();
			LoadAll();
			DialogService.Instance.ShowInfo("Office and contact settings have been cleared.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to clear office settings: " + ex.Message);
		}
	}

	private void BtnUploadLogo_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Select a logo image",
			Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*"
		};
		if (openFileDialog.ShowDialog() != true)
		{
			return;
		}
		try
		{
			SystemConfigService.SaveLogoFromFile(openFileDialog.FileName);
			RefreshLogoPreview();
			RefreshEnvironmentSummary();
			RefreshShellBranding();
			DialogService.Instance.ShowInfo("Logo saved successfully.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to upload logo: " + ex.Message);
		}
	}

	private void BtnRemoveLogo_Click(object sender, RoutedEventArgs e)
	{
		if (!DialogService.Instance.Confirm("Remove the custom logo and restore the default application badge?"))
		{
			return;
		}
		try
		{
			SystemConfigService.RemoveLogo();
			RefreshLogoPreview();
			RefreshEnvironmentSummary();
			RefreshShellBranding();
			DialogService.Instance.ShowInfo("Custom logo removed.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to remove logo: " + ex.Message);
		}
	}

	private void CmbConnectionProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isLoadingConnectionProfile)
		{
			ApplyConnectionOption(GetSelectedConnectionOption());
		}
	}

	private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (GetSelectedConnectionOption().UsesSqlite)
			{
				if (!OfflineDatabaseSupport.EnsureInitialised())
				{
					DialogService.Instance.ShowError("The SQLite database file could not be prepared.", "SQLite Check Failed");
					return;
				}
				txtSqlitePath.Text = OfflineDatabaseSupport.GetDatabasePath();
				DialogService.Instance.ShowInfo("SQLite database is ready.\n\nFile:\n" + txtSqlitePath.Text, "SQLite Ready");
				return;
			}
			ConnectionTestResult connectionTestResult = PackageInstallerService.TestConnection(GetCurrentConnectionProfile());
			if (!connectionTestResult.Success)
			{
				DialogService.Instance.ShowError(connectionTestResult.Message, "Connection Test Failed");
			}
			else if (connectionTestResult.DatabaseMissing)
			{
				DialogService.Instance.ShowWarning(connectionTestResult.Message, "Database Not Found");
			}
			else
			{
				DialogService.Instance.ShowInfo(connectionTestResult.Message, "Connection Test Passed");
			}
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Connection test failed: " + ex.Message);
		}
	}

	private void BtnSaveConnection_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string selectedConnectionProfileKey = GetSelectedConnectionProfileKey();
			if (GetSelectedConnectionOption().UsesSqlite)
			{
				if (!OfflineDatabaseSupport.EnsureInitialised())
				{
					DialogService.Instance.ShowError("The SQLite database file could not be prepared.", "Connection Save Blocked");
					return;
				}
				DbConnectionSettingsStore.SaveSelectedProfile(selectedConnectionProfileKey);
				DBConnection.SetRuntimeSqliteSelection(isSelected: true);
				OfflineDatabaseSupport.ActivateOfflineMode();
				txtSqlitePath.Text = OfflineDatabaseSupport.GetDatabasePath();
				LoadAll();
				DialogService.Instance.ShowInfo("SQLite profile saved. New database operations will use the local SQLite file.");
				return;
			}
			DatabaseConnectionProfile currentConnectionProfile = GetCurrentConnectionProfile();
			ConnectionTestResult connectionTestResult = PackageInstallerService.TestConnection(currentConnectionProfile);
			if (!connectionTestResult.Success)
			{
				DialogService.Instance.ShowError(connectionTestResult.Message, "Connection Save Blocked");
			}
			else if (!connectionTestResult.DatabaseMissing || DialogService.Instance.Confirm(connectionTestResult.Message + Environment.NewLine + Environment.NewLine + "Save this profile anyway? The application may need initialization before this database can be used.", "Save Connection Profile"))
			{
				if (DbConnectionSettingsStore.IsCustomProfileKey(selectedConnectionProfileKey))
				{
					DbConnectionSettingsStore.SaveSelectedProfile(selectedConnectionProfileKey, currentConnectionProfile);
				}
				else
				{
					DbConnectionSettingsStore.SaveSelectedProfile(selectedConnectionProfileKey);
				}
				DBConnection.SetRuntimeSqliteSelection(isSelected: false);
				DBConnection.SetRuntimeConnectionString(DbConnectionSettingsStore.BuildConnectionString(currentConnectionProfile));
				OfflineDatabaseSupport.ActivateOnlineMode();
				LoadAll();
				DialogService.Instance.ShowInfo("Connection profile saved. New database operations will use the selected connection.");
			}
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to save database connection: " + ex.Message);
		}
	}

	private void BtnResetConnection_Click(object sender, RoutedEventArgs e)
	{
		if (!DialogService.Instance.Confirm("Reset the selected database connection back to the localhost preset?"))
		{
			return;
		}
		try
		{
			DbConnectionSettingsStore.SaveSelectedProfile("localhost");
			DBConnection.SetRuntimeSqliteSelection(isSelected: false);
			DBConnection.SetRuntimeConnectionString(DbConnectionSettingsStore.BuildConnectionString(DatabaseConnectionProfile.CreateDefault()));
			OfflineDatabaseSupport.ActivateOnlineMode();
			LoadAll();
			DialogService.Instance.ShowInfo("Database connection was reset to the localhost preset.");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to reset the database connection: " + ex.Message);
		}
	}

	private async void BtnRunFullBackup_Click(object sender, RoutedEventArgs e)
	{
		await RunBackupAsync(BackupMode.Full);
	}

	private async void BtnRunIncrementalBackup_Click(object sender, RoutedEventArgs e)
	{
		await RunBackupAsync(BackupMode.Incremental);
	}

	private async void BtnRunDifferentialBackup_Click(object sender, RoutedEventArgs e)
	{
		await RunBackupAsync(BackupMode.Differential);
	}

	private void BtnOpenBackupFolder_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			BackupService.OpenBackupFolder();
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to open the backup folder: " + ex.Message);
		}
	}

	private async void BtnRestoreBackup_Click(object sender, RoutedEventArgs e)
	{
		if (_isBackupOperationRunning)
		{
			return;
		}
		if (OfflineDatabaseSupport.IsOffline || DbConnectionSettingsStore.IsSqliteSelected())
		{
			DialogService.Instance.ShowWarning("SQL and ZIP restore is only available while the app is connected to MySQL. The current profile is using the local SQLite database.", "Restore Unavailable");
			return;
		}
		OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Select a backup file to restore",
			Filter = "Backup Files|*.zip;*.sql|SQL Files|*.sql|ZIP Files|*.zip|All Files|*.*",
			InitialDirectory = BackupService.GetBackupDirectory(),
			CheckFileExists = true
		};
		if (dialog.ShowDialog() != true || !DialogService.Instance.Confirm("Restore the selected backup into the currently selected database profile?\n\nThis applies the SQL content directly to the active database. Create a fresh full backup first if you need a rollback point.", "Restore Backup"))
		{
			return;
		}
		try
		{
			_isBackupOperationRunning = true;
			SetBackupButtonsEnabled(isEnabled: false);
			lblBackupSummary.Text = "Restoring " + Path.GetFileName(dialog.FileName) + "...";
			lblBackupMeta.Text = "Please wait while the SQL backup is being applied to the selected database.";
			string message = await Task.Run(() => BackupService.RestoreBackupFile(dialog.FileName));
			RefreshBackupSummary();
			DialogService.Instance.ShowInfo(message, "Backup Restored");
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError("Failed to restore backup: " + ex.Message, "Backup Restore");
		}
		finally
		{
			_isBackupOperationRunning = false;
			SetBackupButtonsEnabled(isEnabled: true);
			RefreshBackupSummary();
		}
	}

	private DatabaseConnectionProfile BuildConnectionProfileFromInputs()
	{
		string text = txtDbHost.Text.Trim();
		string text2 = txtDbName.Text.Trim();
		string text3 = txtDbUser.Text.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Database host is required.");
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			throw new InvalidOperationException("Schema name is required.");
		}
		if (string.IsNullOrWhiteSpace(text3))
		{
			throw new InvalidOperationException("Authentication user is required.");
		}
		return new DatabaseConnectionProfile
		{
			Server = text,
			Port = ParsePort(txtDbPort.Text),
			Database = text2,
			Username = text3,
			Password = txtDbPassword.Password,
			UseSsl = (chkDbUseSsl.IsChecked == true)
		};
	}

	private static uint ParsePort(string rawPort)
	{
		if (!uint.TryParse(rawPort?.Trim(), out var result) || result == 0 || result > 65535)
		{
			throw new InvalidOperationException("Service port must be a number between 1 and 65535.");
		}
		return result;
	}

	private static string ToDisplayMode(BackupMode mode)
	{
		return mode switch
		{
			BackupMode.Incremental => "Incremental", 
			BackupMode.Differential => "Differential", 
			_ => "Full", 
		};
	}

	private static string FormatFileSize(long? bytes)
	{
		if (!bytes.HasValue || bytes.Value <= 0)
		{
			return "size unavailable";
		}
		string[] array = new string[4] { "B", "KB", "MB", "GB" };
		double num = bytes.Value;
		int num2 = 0;
		while (num >= 1024.0 && num2 < array.Length - 1)
		{
			num /= 1024.0;
			num2++;
		}
		return $"{num:0.##} {array[num2]}";
	}

	private static void RefreshShellBranding()
	{
		if (Application.Current.MainWindow is MainWindow mainWindow)
		{
			mainWindow.RefreshBranding();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/pages/settingspage.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			txtSystemName = (TextBox)target;
			break;
		case 2:
			txtBarangayName = (TextBox)target;
			break;
		case 3:
			txtMunicipality = (TextBox)target;
			break;
		case 4:
			txtProvince = (TextBox)target;
			break;
		case 5:
			txtRegion = (TextBox)target;
			break;
		case 6:
			btnSaveBranding = (Button)target;
			btnSaveBranding.Click += BtnSaveBranding_Click;
			break;
		case 7:
			btnResetBranding = (Button)target;
			btnResetBranding.Click += BtnResetBranding_Click;
			break;
		case 8:
			logoPreview = (Image)target;
			break;
		case 9:
			logoPreviewFallback = (StackPanel)target;
			break;
		case 10:
			btnUploadLogo = (Button)target;
			btnUploadLogo.Click += BtnUploadLogo_Click;
			break;
		case 11:
			btnRemoveLogo = (Button)target;
			btnRemoveLogo.Click += BtnRemoveLogo_Click;
			break;
		case 12:
			txtOfficeAddress = (TextBox)target;
			break;
		case 13:
			txtContactNumber = (TextBox)target;
			break;
		case 14:
			txtOfficialEmail = (TextBox)target;
			break;
		case 15:
			btnSaveOfficeSettings = (Button)target;
			btnSaveOfficeSettings.Click += BtnSaveOfficeSettings_Click;
			break;
		case 16:
			btnResetOfficeSettings = (Button)target;
			btnResetOfficeSettings.Click += BtnResetOfficeSettings_Click;
			break;
		case 17:
			cmbConnectionProfile = (ComboBox)target;
			cmbConnectionProfile.SelectionChanged += CmbConnectionProfile_SelectionChanged;
			break;
		case 18:
			lblConnectionProfileDescription = (TextBlock)target;
			break;
		case 19:
			pnlMySqlConnectionFields = (Grid)target;
			break;
		case 20:
			txtDbHost = (TextBox)target;
			break;
		case 21:
			txtDbUser = (TextBox)target;
			break;
		case 22:
			txtDbPassword = (PasswordBox)target;
			break;
		case 23:
			txtDbPort = (TextBox)target;
			break;
		case 24:
			txtDbName = (TextBox)target;
			break;
		case 25:
			chkDbUseSsl = (CheckBox)target;
			break;
		case 26:
			pnlSqliteDetails = (Border)target;
			break;
		case 27:
			txtSqlitePath = (TextBox)target;
			break;
		case 28:
			btnTestConnection = (Button)target;
			btnTestConnection.Click += BtnTestConnection_Click;
			break;
		case 29:
			btnSaveConnection = (Button)target;
			btnSaveConnection.Click += BtnSaveConnection_Click;
			break;
		case 30:
			btnResetConnection = (Button)target;
			btnResetConnection.Click += BtnResetConnection_Click;
			break;
		case 31:
			btnRunFullBackup = (Button)target;
			btnRunFullBackup.Click += BtnRunFullBackup_Click;
			break;
		case 32:
			btnRunIncrementalBackup = (Button)target;
			btnRunIncrementalBackup.Click += BtnRunIncrementalBackup_Click;
			break;
		case 33:
			btnRunDifferentialBackup = (Button)target;
			btnRunDifferentialBackup.Click += BtnRunDifferentialBackup_Click;
			break;
		case 34:
			btnOpenBackupFolder = (Button)target;
			btnOpenBackupFolder.Click += BtnOpenBackupFolder_Click;
			break;
		case 35:
			btnRestoreBackup = (Button)target;
			btnRestoreBackup.Click += BtnRestoreBackup_Click;
			break;
		case 36:
			lblBackupSummary = (TextBlock)target;
			break;
		case 37:
			lblBackupMeta = (TextBlock)target;
			break;
		case 38:
			lblBackupDirectory = (TextBlock)target;
			break;
		case 39:
			lblEnvironmentSummary = (TextBlock)target;
			break;
		case 40:
			lblBrandingSummary = (TextBlock)target;
			break;
		case 41:
			lblConnectionSummary = (TextBlock)target;
			break;
		case 42:
			lblConnectionModeSummary = (TextBlock)target;
			break;
		case 43:
			lblLogoSummary = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
