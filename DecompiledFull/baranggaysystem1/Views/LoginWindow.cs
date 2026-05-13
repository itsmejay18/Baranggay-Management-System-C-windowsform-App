using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using FontAwesome.Sharp;
using baranggaysystem1.Database;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views;

public class LoginWindow : Window, IComponentConnector
{
	private sealed class ConnectionStatusSnapshot
	{
		public string StatusText { get; init; } = string.Empty;

		public string ToolTipText { get; init; } = string.Empty;

		public IconChar Icon { get; init; } = (IconChar)61902;

		public Brush StatusBrush { get; init; } = AccentBrush;
	}

	private static readonly Brush AccentBrush = CreateFrozenBrush("#173A6A");

	private static readonly Brush SuccessBrush = CreateFrozenBrush("#166534");

	private static readonly Brush WarningBrush = CreateFrozenBrush("#9A3412");

	private static readonly Brush DangerBrush = CreateFrozenBrush("#B91C1C");

	private readonly LoginViewModel _vm;

	private bool _isLoadingConnectionOptions;

	private bool _isConnectionRefreshRunning;

	private bool _isConnectionRefreshBlockingLogin;

	private bool _isLoginRunning;

	internal Image logoImage;

	internal StackPanel logoFallback;

	internal TextBlock logoFallbackText;

	internal TextBlock brandPrimaryText;

	internal TextBlock brandSecondaryText;

	internal TextBlock brandAddressText;

	internal Image rightLogoImage;

	internal StackPanel rightLogoFallback;

	internal TextBlock rightLogoFallbackText;

	internal TextBlock softwareNameText;

	internal TextBlock softwareSubtitleText;

	internal ComboBox cmbConnectionProfile;

	internal IconBlock connectionStatusIcon;

	internal TextBlock connectionStatusText;

	internal TextBox txtUsername;

	internal PasswordBox txtPassword;

	internal Button btnLogin;

	private bool _contentLoaded;

	public LoginWindow()
	{
		InitializeComponent();
		_vm = (LoginViewModel)base.DataContext;
		_vm.LoginSucceeded += OnLoginSucceeded;
		_vm.RegisterRequested += OnRegisterRequested;
		base.Loaded += async delegate
		{
			BeginFadeIn();
			LoadDynamicBranding();
			InitializeConnectionOptions();
			txtUsername.Focus();
			DatabaseConnectionOption selectedConnectionOption = GetSelectedConnectionOption();
			if (selectedConnectionOption != null)
			{
				await RefreshConnectionStatusAsync(selectedConnectionOption, persistSelection: false, blockLogin: false);
				RefreshConnectionStatusAfterDelayAsync();
			}
		};
	}

	private void LoadDynamicBranding()
	{
		try
		{
			SystemBrandingSettings systemBrandingSettings = SystemConfigService.LoadBrandingSettings();
			SystemOfficeSettings office = SystemConfigService.LoadOfficeSettings();
			string systemName = systemBrandingSettings.SystemName;
			base.Title = systemName + " - Secure Login";
			brandPrimaryText.Text = BuildGovernmentLabel(systemBrandingSettings);
			brandSecondaryText.Text = BuildProfileLine(systemBrandingSettings);
			brandAddressText.Text = BuildOfficeLine(systemBrandingSettings, office);
			softwareNameText.Text = systemName;
			softwareSubtitleText.Text = BuildSoftwareSubtitle(systemBrandingSettings);
			string text = BuildSystemInitials(systemName);
			logoFallbackText.Text = text;
			rightLogoFallbackText.Text = text;
			ApplyLogo(SystemConfigService.GetLogo());
		}
		catch
		{
		}
	}

	private void InitializeConnectionOptions()
	{
		_isLoadingConnectionOptions = true;
		try
		{
			IReadOnlyList<DatabaseConnectionOption> availableOptions = DbConnectionSettingsStore.GetAvailableOptions();
			cmbConnectionProfile.DisplayMemberPath = "DisplayName";
			cmbConnectionProfile.SelectedValuePath = "Key";
			cmbConnectionProfile.ItemsSource = availableOptions;
			cmbConnectionProfile.SelectedValue = DbConnectionSettingsStore.LoadSelectedProfileKeyOrDefault();
			if (cmbConnectionProfile.SelectedItem == null)
			{
				cmbConnectionProfile.SelectedItem = availableOptions.FirstOrDefault();
			}
			ApplyConnectionStatusSnapshot(BuildCheckingSnapshot());
		}
		finally
		{
			_isLoadingConnectionOptions = false;
			UpdateInteractiveStates();
		}
	}

	private async Task RefreshConnectionStatusAfterDelayAsync()
	{
		await Task.Delay(1800);
		if (base.IsLoaded && !_isConnectionRefreshRunning)
		{
			DatabaseConnectionOption selectedConnectionOption = GetSelectedConnectionOption();
			if (selectedConnectionOption != null)
			{
				await RefreshConnectionStatusAsync(selectedConnectionOption, persistSelection: false, blockLogin: false);
			}
		}
	}

	private async Task RefreshConnectionStatusAsync(DatabaseConnectionOption option, bool persistSelection, bool blockLogin)
	{
		if (_isConnectionRefreshRunning)
		{
			return;
		}
		_isConnectionRefreshRunning = true;
		_isConnectionRefreshBlockingLogin = blockLogin;
		ApplyConnectionStatusSnapshot(BuildCheckingSnapshot());
		UpdateInteractiveStates();
		try
		{
			ApplyConnectionStatusSnapshot(await Task.Run(() => EvaluateConnectionStatus(option, persistSelection)));
		}
		catch (Exception ex)
		{
			ApplyConnectionStatusSnapshot(BuildDisconnectedSnapshot(ex.Message));
		}
		finally
		{
			_isConnectionRefreshRunning = false;
			_isConnectionRefreshBlockingLogin = false;
			UpdateInteractiveStates();
		}
	}

	private static ConnectionStatusSnapshot EvaluateConnectionStatus(DatabaseConnectionOption option, bool persistSelection)
	{
		if (persistSelection)
		{
			SaveConnectionOption(option);
		}
		if (option.UsesSqlite)
		{
			if (!OfflineDatabaseSupport.IsAvailable && !OfflineDatabaseSupport.EnsureInitialised())
			{
				DBConnection.SetRuntimeSqliteSelection(isSelected: true);
				return BuildDisconnectedSnapshot("The SQLite database file could not be prepared.");
			}
			DBConnection.SetRuntimeSqliteSelection(isSelected: true);
			OfflineDatabaseSupport.ActivateOfflineMode();
			return BuildSqliteReadySnapshot(option);
		}
		DBConnection.SetRuntimeSqliteSelection(isSelected: false);
		string text = DbConnectionSettingsStore.BuildConnectionString(option.Profile);
		DBConnection.SetRuntimeConnectionString(text);
		if (DBConnection.TryGetWorkingConnectionString(text, out string workingConnectionString, out string _))
		{
			DBConnection.SetRuntimeConnectionString(workingConnectionString);
			OfflineDatabaseSupport.ActivateOnlineMode();
			return BuildReadySnapshot(option);
		}
		if (OfflineDatabaseSupport.IsAvailable || OfflineDatabaseSupport.EnsureInitialised())
		{
			OfflineDatabaseSupport.ActivateOfflineMode();
			return BuildOfflineFallbackSnapshot(option);
		}
		DBConnection.RegisterConnectivityFailure();
		return BuildDisconnectedSnapshot("Live MySQL is unavailable and the local offline database is not ready yet.");
	}

	private DatabaseConnectionOption? GetSelectedConnectionOption()
	{
		return (cmbConnectionProfile.SelectedItem as DatabaseConnectionOption) ?? (cmbConnectionProfile.ItemsSource as IEnumerable<DatabaseConnectionOption>)?.FirstOrDefault() ?? DbConnectionSettingsStore.GetSelectedOptionOrDefault();
	}

	private void UpdateInteractiveStates()
	{
		cmbConnectionProfile.IsEnabled = !_isLoadingConnectionOptions && !_isConnectionRefreshRunning && !_isLoginRunning;
		btnLogin.IsEnabled = !_isLoginRunning && (!_isConnectionRefreshRunning || !_isConnectionRefreshBlockingLogin);
	}

	private void ApplyConnectionStatusSnapshot(ConnectionStatusSnapshot snapshot)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		((IconBlockBase<IconChar>)(object)connectionStatusIcon).Icon = snapshot.Icon;
		((TextBlock)(object)connectionStatusIcon).Foreground = snapshot.StatusBrush;
		connectionStatusText.Text = snapshot.StatusText;
		connectionStatusText.Foreground = snapshot.StatusBrush;
		((FrameworkElement)(object)connectionStatusIcon).ToolTip = snapshot.ToolTipText;
		connectionStatusText.ToolTip = snapshot.ToolTipText;
	}

	private static void SaveConnectionOption(DatabaseConnectionOption option)
	{
		if (DbConnectionSettingsStore.IsCustomProfileKey(option.Key))
		{
			DbConnectionSettingsStore.SaveSelectedProfile(option.Key, option.Profile);
		}
		else
		{
			DbConnectionSettingsStore.SaveSelectedProfile(option.Key);
		}
	}

	private static ConnectionStatusSnapshot BuildCheckingSnapshot()
	{
		return new ConnectionStatusSnapshot
		{
			StatusText = "Checking database...",
			Icon = (IconChar)61902,
			StatusBrush = AccentBrush,
			ToolTipText = "Testing the selected database profile."
		};
	}

	private static ConnectionStatusSnapshot BuildReadySnapshot(DatabaseConnectionOption option)
	{
		bool flag = IsLocalOption(option);
		string toolTipText = $"{option.DisplayName} - {option.Profile.Server}:{option.Profile.Port} / {option.Profile.Database}";
		return new ConnectionStatusSnapshot
		{
			StatusText = (flag ? "Local ready" : "Online ready"),
			Icon = (IconChar)62003,
			StatusBrush = SuccessBrush,
			ToolTipText = toolTipText
		};
	}

	private static ConnectionStatusSnapshot BuildSqliteReadySnapshot(DatabaseConnectionOption option)
	{
		string toolTipText = (string.IsNullOrWhiteSpace(option.SqliteFilePath) ? OfflineDatabaseSupport.GetDatabasePath() : option.SqliteFilePath);
		return new ConnectionStatusSnapshot
		{
			StatusText = "SQLite ready",
			Icon = (IconChar)61888,
			StatusBrush = SuccessBrush,
			ToolTipText = toolTipText
		};
	}

	private static ConnectionStatusSnapshot BuildOfflineFallbackSnapshot(DatabaseConnectionOption option)
	{
		return new ConnectionStatusSnapshot
		{
			StatusText = "Local cache active",
			Icon = (IconChar)61600,
			StatusBrush = WarningBrush,
			ToolTipText = "Selected profile unavailable. Using local offline cache for " + option.DisplayName + "."
		};
	}

	private static ConnectionStatusSnapshot BuildDisconnectedSnapshot(string detail)
	{
		return new ConnectionStatusSnapshot
		{
			StatusText = "Database unavailable",
			Icon = (IconChar)61553,
			StatusBrush = DangerBrush,
			ToolTipText = detail
		};
	}

	private static bool IsLocalOption(DatabaseConnectionOption option)
	{
		if (!option.UsesSqlite && !string.Equals(option.Key, "localhost", StringComparison.OrdinalIgnoreCase))
		{
			return IsLocalServer(option.Profile.Server);
		}
		return true;
	}

	private void ApplyLogo(BitmapImage? logo)
	{
		bool flag = logo != null;
		logoImage.Source = logo;
		logoImage.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		logoFallback.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		rightLogoImage.Source = logo;
		rightLogoImage.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		rightLogoFallback.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
	}

	private static string BuildGovernmentLabel(SystemBrandingSettings branding)
	{
		string text = FirstNonPlaceholder(branding.Municipality, "Municipality", branding.BarangayName, string.Empty, "Your Barangay");
		return "Local Government of " + text;
	}

	private static string BuildProfileLine(SystemBrandingSettings branding)
	{
		List<string> list = new List<string>();
		AddIfMeaningful(list, branding.BarangayName);
		AddIfMeaningful(list, branding.Province, "Province");
		if (list.Count <= 0)
		{
			return "Barangay profile";
		}
		return string.Join(" | ", list);
	}

	private static string BuildOfficeLine(SystemBrandingSettings branding, SystemOfficeSettings office)
	{
		if (!string.IsNullOrWhiteSpace(office.OfficeAddress))
		{
			return office.OfficeAddress.Trim();
		}
		List<string> list = new List<string>();
		AddIfMeaningful(list, branding.Municipality, "Municipality");
		AddIfMeaningful(list, branding.Province, "Province");
		AddIfMeaningful(list, branding.Region, "Region");
		if (list.Count <= 0)
		{
			return "Update office details in System Settings.";
		}
		return string.Join(", ", list);
	}

	private static string BuildSoftwareSubtitle(SystemBrandingSettings branding)
	{
		string text = (string.IsNullOrWhiteSpace(branding.BarangayName) ? "your barangay" : branding.BarangayName.Trim());
		return "Secure access for " + text;
	}

	private static void AddIfMeaningful(ICollection<string> values, string? value, string placeholder = "")
	{
		string text = value?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text) && (string.IsNullOrWhiteSpace(placeholder) || !string.Equals(text, placeholder, StringComparison.OrdinalIgnoreCase)))
		{
			values.Add(text);
		}
	}

	private static string FirstNonPlaceholder(string? primaryValue, string primaryPlaceholder, string? fallbackValue, string fallbackPlaceholder, string finalFallback)
	{
		if (IsMeaningful(primaryValue, primaryPlaceholder))
		{
			return primaryValue.Trim();
		}
		if (IsMeaningful(fallbackValue, fallbackPlaceholder))
		{
			return fallbackValue.Trim();
		}
		return finalFallback;
	}

	private static bool IsMeaningful(string? value, string placeholder)
	{
		string text = value?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return !string.Equals(text, placeholder, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string BuildSystemInitials(string systemName)
	{
		string text = string.Concat(from part in (systemName ?? string.Empty).Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
			where !string.Equals(part, "and", StringComparison.OrdinalIgnoreCase)
			select char.ToUpperInvariant(part[0]));
		if (string.IsNullOrWhiteSpace(text))
		{
			return "BMS";
		}
		if (text.Length > 4)
		{
			return text.Substring(0, 4);
		}
		return text;
	}

	private static Brush CreateFrozenBrush(string hex)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
		((Freezable)solidColorBrush).Freeze();
		return solidColorBrush;
	}

	private static bool IsLocalServer(string? server)
	{
		string a = server?.Trim() ?? string.Empty;
		if (!string.Equals(a, "localhost", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(a, ".", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private void BeginFadeIn()
	{
		base.Opacity = 0.0;
		DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(350.0));
		BeginAnimation(UIElement.OpacityProperty, animation);
	}

	private async void CmbConnectionProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_isLoadingConnectionOptions)
		{
			DatabaseConnectionOption selectedConnectionOption = GetSelectedConnectionOption();
			if (selectedConnectionOption != null)
			{
				await RefreshConnectionStatusAsync(selectedConnectionOption, persistSelection: true, blockLogin: true);
			}
		}
	}

	private async void BtnLogin_Click(object sender, RoutedEventArgs e)
	{
		_isLoginRunning = true;
		UpdateInteractiveStates();
		try
		{
			await _vm.LoginCommand.ExecuteAsync(txtPassword.Password);
		}
		finally
		{
			_isLoginRunning = false;
			UpdateInteractiveStates();
		}
	}

	private void OnLoginSucceeded(bool isAdmin)
	{
		MainWindow mainWindow = new MainWindow();
		Application.Current.MainWindow = mainWindow;
		mainWindow.Show();
		Close();
	}

	private void OnRegisterRequested()
	{
		RegisterWindow reg = new RegisterWindow();
		reg.RegistrationCompleted += delegate
		{
			reg.Close();
			Show();
			Activate();
		};
		reg.BackToLoginRequested += delegate
		{
			reg.Close();
			Show();
			Activate();
		};
		Hide();
		reg.Show();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/loginwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected O, but got Unknown
		switch (connectionId)
		{
		case 1:
			logoImage = (Image)target;
			break;
		case 2:
			logoFallback = (StackPanel)target;
			break;
		case 3:
			logoFallbackText = (TextBlock)target;
			break;
		case 4:
			brandPrimaryText = (TextBlock)target;
			break;
		case 5:
			brandSecondaryText = (TextBlock)target;
			break;
		case 6:
			brandAddressText = (TextBlock)target;
			break;
		case 7:
			rightLogoImage = (Image)target;
			break;
		case 8:
			rightLogoFallback = (StackPanel)target;
			break;
		case 9:
			rightLogoFallbackText = (TextBlock)target;
			break;
		case 10:
			softwareNameText = (TextBlock)target;
			break;
		case 11:
			softwareSubtitleText = (TextBlock)target;
			break;
		case 12:
			cmbConnectionProfile = (ComboBox)target;
			cmbConnectionProfile.SelectionChanged += CmbConnectionProfile_SelectionChanged;
			break;
		case 13:
			connectionStatusIcon = (IconBlock)target;
			break;
		case 14:
			connectionStatusText = (TextBlock)target;
			break;
		case 15:
			txtUsername = (TextBox)target;
			break;
		case 16:
			txtPassword = (PasswordBox)target;
			break;
		case 17:
			btnLogin = (Button)target;
			btnLogin.Click += BtnLogin_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
