using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class HomeLandingPage : UserControl
{
	private readonly DispatcherTimer _clockTimer;





























	public HomeLandingPage()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		InitializeComponent();
		_clockTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.0)
		};
		_clockTimer.Tick += delegate
		{
			UpdateClocks();
		};
		base.Loaded += async delegate
		{
			await HandleLoadedAsync();
		};
		base.Unloaded += delegate
		{
			if (_clockTimer.IsEnabled)
			{
				_clockTimer.Stop();
			}
		};
		ApplyRoleVisibility();
		UpdateSessionCard();
		LoadWorkspaceBranding();
		UpdateClocks();
	}

	private async Task HandleLoadedAsync()
	{
		ApplyRoleVisibility();
		UpdateSessionCard();
		LoadWorkspaceBranding();
		UpdateClocks();
		if (!_clockTimer.IsEnabled)
		{
			_clockTimer.Start();
		}
		await LoadWorkspaceStatsAsync();
	}

	private void UpdateClocks()
	{
		DateTime now = DateTime.Now;
		lblHeroGreeting.Text = ResolveGreeting(now.Hour);
		lblHugeDate.Text = now.ToString("dddd, dd MMMM yyyy");
		lblHeroClock.Text = now.ToString("hh:mm:ss tt");
	}

	private void UpdateSessionCard()
	{
		string text = (string.IsNullOrWhiteSpace(UserSession.Username) ? "Barangay staff" : UserSession.Username.Trim());
		lblWorkspaceSubtitle.Text = text + " coordinating today's resident and service workflows.";
		lblSessionTitle.Text = ResolveSessionTitle(UserSession.Role);
		lblSessionSubtext.Text = text + " | Online";
	}

	private void LoadWorkspaceBranding()
	{
		try
		{
			BitmapImage logo = SystemConfigService.GetLogo();
			bool flag = logo != null;
			workspaceLogoImage.Source = logo;
			workspaceLogoImage.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			((UIElement)(object)workspaceLogoFallback).Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		}
		catch
		{
			workspaceLogoImage.Source = null;
			workspaceLogoImage.Visibility = Visibility.Collapsed;
			((UIElement)(object)workspaceLogoFallback).Visibility = Visibility.Visible;
		}
	}

	private async Task LoadWorkspaceStatsAsync()
	{
		try
		{
			Task<int> residentTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM resident WHERE COALESCE(is_deleted, 0) = 0");
			Task<int> householdTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM household");
			Task<int> pendingTask = DatabaseManagerAsync.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM document_request WHERE UPPER(COALESCE(status, '')) = 'SUBMITTED'");
			Task<decimal> revenueTask = DatabaseManagerAsync.ExecuteScalarAsync<decimal>("SELECT CAST(COALESCE(SUM(amount), 0) AS DECIMAL(12,2))\n                      FROM document_payment\n                      WHERE paid_at >= DATE_FORMAT(CURDATE(), '%Y-%m-01')");
			await Task.WhenAll(residentTask, householdTask, pendingTask, revenueTask);
			int result = residentTask.Result;
			int result2 = householdTask.Result;
			int result3 = pendingTask.Result;
			decimal result4 = revenueTask.Result;
			txtResidentFootnote.Text = $"{result:N0} active resident file(s).";
			txtHouseholdFootnote.Text = $"{result2:N0} household record(s).";
			txtPaymentsFootnote.Text = ((result3 > 0) ? $"{result3:N0} request(s) still in queue." : "Receipts and balances are current.");
			txtPendingCount.Text = ((result3 > 99) ? "99+" : result3.ToString("N0"));
			txtRevenueValue.Text = $"P{result4:N2}";
			txtConnectedFooter.Text = $"{result:N0} residents and {result2:N0} households live.";
			txtTopBadge.Text = ((result3 > 99) ? "99+" : result3.ToString("N0"));
		}
		catch (Exception ex)
		{
			AppLogger.LogError("HomeLandingPage: failed to load workspace metrics.", ex);
			txtTopBadge.Text = "!";
			txtConnectedFooter.Text = "Metrics temporarily unavailable.";
		}
	}

	private static string ResolveGreeting(int hour)
	{
		if (hour < 12)
		{
			return "Good morning";
		}
		if (hour < 18)
		{
			return "Good afternoon";
		}
		return "Good evening";
	}

	private static string ResolveSessionTitle(string? role)
	{
		if (string.Equals(role, "Super Admin", StringComparison.OrdinalIgnoreCase))
		{
			return "System Administrator";
		}
		if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
		{
			return "Barangay Administrator";
		}
		return "Barangay Staff";
	}

	private void Navigate(string route)
	{
		if (Application.Current.MainWindow is MainWindow mainWindow)
		{
			mainWindow.NavigatePage(route);
		}
	}

	private void ApplyRoleVisibility()
	{
		bool flag = string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase) || flag;
		bool allowed = flag2 || Permissions.CanViewHouseholds;
		bool flag3 = flag2 || Permissions.CanRequestCertificates || Permissions.CanEditCertificateRequests || Permissions.CanApproveCertificates || Permissions.CanIssueCertificates || Permissions.CanCancelCertificates || Permissions.CanExportCertificates;
		bool allowed2 = flag2 || Permissions.CanCreateBlotter || Permissions.CanUpdateBlotterStatus;
		bool allowed3 = flag2 || Permissions.CanManageAnnouncements || Permissions.CanManageProjects;
		bool allowed4 = flag3;
		bool allowed5 = flag || Permissions.CanOpenSettings;
		SetTileState(tileHouseholds, allowed);
		SetTileState(tileClearances, flag3);
		SetTileState(tileBlotter, allowed2);
		SetTileState(tileGovernance, allowed3);
		SetTileState(tilePayments, allowed4);
		SetTileState(tileOfficials, flag2);
		SetTileState(tilePermissions, flag);
		SetTileState(tileSettings, allowed5);
		SetTileState(tilePending, flag3);
		SetTileState(tileCollections, flag2);
	}

	private static void SetTileState(ButtonBase button, bool allowed)
	{
		button.IsEnabled = allowed;
		button.Opacity = (allowed ? 1.0 : 0.42);
		ToolTipService.SetToolTip((DependencyObject)(object)button, allowed ? null : "This workspace is unavailable for the current account.");
	}

	private void TileDashboard_Click(object sender, RoutedEventArgs e)
	{
		Navigate("DashboardNotifications");
	}

	private void TileResidents_Click(object sender, RoutedEventArgs e)
	{
		Navigate("ResidentWorkspace");
	}

	private void TileHouseholds_Click(object sender, RoutedEventArgs e)
	{
		Navigate("Households");
	}

	private void TileClearances_Click(object sender, RoutedEventArgs e)
	{
		Navigate("Clearances");
	}

	private void TileGovernance_Click(object sender, RoutedEventArgs e)
	{
		Navigate("GovernanceRegistry");
	}

	private void TileBlotter_Click(object sender, RoutedEventArgs e)
	{
		Navigate("ResidentCases");
	}

	private void TilePayments_Click(object sender, RoutedEventArgs e)
	{
		Navigate("ResidentPayments");
	}

	private void TileOfficials_Click(object sender, RoutedEventArgs e)
	{
		Navigate("Officials");
	}

	private void TileSettings_Click(object sender, RoutedEventArgs e)
	{
		Navigate("Settings");
	}

	private void TilePermissions_Click(object sender, RoutedEventArgs e)
	{
		Navigate("RolePermissions");
	}

	private void TilePending_Click(object sender, RoutedEventArgs e)
	{
		Navigate("Clearances");
	}

	private void TileCollections_Click(object sender, RoutedEventArgs e)
	{
		Navigate("Collections");
	}

	private void TileExit_Click(object sender, RoutedEventArgs e)
	{
		if (DialogService.Instance.Confirm("Are you sure you want to log out and exit the program?"))
		{
			Application.Current.Shutdown();
		}
	}}
