using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FontAwesome.Sharp;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.Views.Dialogs;
using baranggaysystem1.Views.Pages;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views;

public class MainWindow : Window, IComponentConnector
{
	private const double ExpandedSidebarWidth = 286.0;

	private const double CollapsedSidebarWidth = 78.0;

	private readonly DispatcherTimer _clockTimer = new DispatcherTimer();

	private bool _isSidebarCollapsed;

	private bool _isSynchronizingNavigation;

	private string _currentRoute = string.Empty;

	internal ColumnDefinition SidebarColumn;

	internal Border SidebarRoot;

	internal Image sidebarBrandLogo;

	internal IconBlock sidebarBrandIcon;

	internal TextBlock sidebarBarangayName;

	internal TextBlock sidebarUserInitial;

	internal TextBlock sidebarUsername;

	internal TextBlock sidebarRole;

	internal RadioButton navHome;

	internal RadioButton navDashboard;

	internal RadioButton navGovernanceRegistry;

	internal TextBlock navGroupCommunity;

	internal RadioButton navResidents;

	internal RadioButton navSoloParents;

	internal RadioButton navYouth;

	internal RadioButton navIndigent;

	internal RadioButton navHouseholds;

	internal RadioButton navDeceased;

	internal RadioButton navCategories;

	internal TextBlock navGroupServices;

	internal RadioButton navClearances;

	internal RadioButton navPermits;

	internal TextBlock navGroupBlotter;

	internal RadioButton navBlotter;

	internal TextBlock navGroupFinance;

	internal RadioButton navPayments;

	internal RadioButton navAyuda;

	internal RadioButton navCollections;

	internal TextBlock navGroupAdmin;

	internal RadioButton navOfficials;

	internal RadioButton navStaff;

	internal RadioButton navRoles;

	internal RadioButton navLogs;

	internal RadioButton navNotificationOutbox;

	internal RadioButton navSettings;

	internal RowDefinition TopBarRow;

	internal RowDefinition StatusBarRow;

	internal Border TopBarContainer;

	internal Button btnToggleSidebar;

	internal TextBlock breadcrumbRootLabel;

	internal TextBlock breadcrumbSeparator;

	internal TextBlock breadcrumbLabel;

	internal Button btnSearch;

	internal Button btnEllie;

	internal Button btnNotify;

	internal TextBlock topBarInitial;

	internal ContentControl pageHost;

	internal Border BottomStatusBar;

	internal Border dbStatusDot;

	internal TextBlock dbStatusLabel;

	internal TextBlock barangayLabel;

	internal TextBlock statusClock;

	internal Popup notificationPopup;

	internal StackPanel notificationList;

	private bool _contentLoaded;

	public MainWindow()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		InitializeComponent();
		NavigationService.Instance.Initialize(pageHost);
		string text = UserSession.Username ?? "User";
		string text2 = UserSession.Role ?? "Staff";
		string text3 = ((text.Length > 0) ? text[0].ToString().ToUpperInvariant() : "U");
		sidebarUsername.Text = text;
		sidebarRole.Text = text2;
		sidebarUserInitial.Text = text3;
		topBarInitial.Text = text3;
		RefreshBranding();
		ApplyRoleVisibility();
		_clockTimer.Interval = TimeSpan.FromSeconds(1.0);
		_clockTimer.Tick += delegate
		{
			UpdateClock();
		};
		_clockTimer.Start();
		UpdateClock();
		base.Loaded += delegate
		{
			base.Opacity = 0.0;
			BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(280.0)));
			NavigatePage("Home");
		};
	}

	private void ApplyRoleVisibility()
	{
		bool flag = string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(UserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase) || flag;
		bool flag3 = flag || Permissions.CanOpenSettings;
		bool visible = flag2 || flag3;
		bool visible2 = flag2 || Permissions.CanManageAnnouncements || Permissions.CanManageProjects;
		SetNav(navGovernanceRegistry, visible2);
		SetNav(navHouseholds, flag2 || Permissions.CanViewHouseholds);
		SetNav(navCategories, flag2);
		SetNav(navBlotter, flag2 || Permissions.CanCreateBlotter);
		bool visible3 = flag2;
		SetNav(navPayments, visible3);
		SetNav(navAyuda, visible3);
		SetNav(navCollections, visible3);
		SetNav(navGroupFinance, visible3);
		SetNav(navOfficials, flag2);
		SetNav(navStaff, flag);
		SetNav(navRoles, flag);
		SetNav(navLogs, visible);
		SetNav(navNotificationOutbox, flag2);
		SetNav(navSettings, flag3);
		SetNav(navGroupBlotter, navBlotter.Visibility == Visibility.Visible);
		SetNav(navGroupAdmin, navOfficials.Visibility == Visibility.Visible || navStaff.Visibility == Visibility.Visible || navRoles.Visibility == Visibility.Visible || navLogs.Visibility == Visibility.Visible || navNotificationOutbox.Visibility == Visibility.Visible || navSettings.Visibility == Visibility.Visible);
	}

	private static void SetNav(UIElement element, bool visible)
	{
		element.Visibility = ((!visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateClock()
	{
		statusClock.Text = DateTime.Now.ToString("ddd, MMM dd yyyy   hh:mm:ss tt");
	}

	public void RefreshBranding()
	{
		string systemName = SystemConfigService.GetSystemName();
		string barangayName = SystemConfigService.GetBarangayName();
		base.Title = systemName;
		barangayLabel.Text = barangayName;
		sidebarBarangayName.Text = FormatSidebarBrandName(barangayName);
		BitmapImage logo = SystemConfigService.GetLogo();
		if (logo != null)
		{
			sidebarBrandLogo.Source = logo;
			sidebarBrandLogo.Visibility = Visibility.Visible;
			((UIElement)(object)sidebarBrandIcon).Visibility = Visibility.Collapsed;
		}
		else
		{
			sidebarBrandLogo.Source = null;
			sidebarBrandLogo.Visibility = Visibility.Collapsed;
			((UIElement)(object)sidebarBrandIcon).Visibility = Visibility.Visible;
		}
	}

	private void NavItem_Checked(object sender, RoutedEventArgs e)
	{
		if (!_isSynchronizingNavigation && sender is RadioButton radioButton)
		{
			string route = (radioButton.Tag as string) ?? "Home";
			NavigatePage(route);
		}
	}

	public void NavigatePage(string route)
	{
		route = NormalizeRoute(route);
		if (string.Equals(_currentRoute, route, StringComparison.Ordinal) && pageHost.Content != null)
		{
			UpdateShellForRoute(route);
			UpdateBreadcrumb(route);
			SyncNavigationSelection(route);
			return;
		}
		UIElement uIElement = route switch
		{
			"Home" => new HomeLandingPage(), 
			"DashboardNotifications" => new DashboardPage(showReminderEntry: true), 
			"Dashboard" => new DashboardPage(), 
			"GovernanceRegistry" => new GovernanceRegistryPage(), 
			"ResidentWorkspace" => new ResidentModulePage(), 
			"ResidentSoloParents" => new ResidentModulePage(route), 
			"ResidentYouth" => new ResidentModulePage(route), 
			"ResidentIndigent" => new ResidentModulePage(route), 
			"Households" => new HouseholdsPage(), 
			"ResidentCategories" => new TagsCategoriesPage(), 
			"DeceasedRegistry" => new DeceasedRegistryPage(), 
			"Clearances" => new ClearancesPage(), 
			"Permits" => new PermitsPage(), 
			"ResidentCases" => new BlotterPage(), 
			"ResidentPayments" => new PaymentsPage(), 
			"Ayuda" => new AyudaPage(), 
			"Collections" => new CollectionsPage(), 
			"Reports" => new ReportsPage(), 
			"Officials" => new OfficialsPage(), 
			"StaffUsers" => new StaffManagementPage(), 
			"RolePermissions" => new RolePermissionsPage(), 
			"SystemLogs" => new SystemLogsPage(), 
			"NotificationOutbox" => new NotificationOutboxPage(), 
			"Settings" => new SettingsPage(), 
			_ => new HomeLandingPage(), 
		};
		_currentRoute = route;
		UpdateShellForRoute(route);
		UpdateBreadcrumb(route);
		SyncNavigationSelection(route);
		if (uIElement is FrameworkElement frameworkElement)
		{
			frameworkElement.Opacity = 0.0;
			frameworkElement.RenderTransform = new TranslateTransform(0.0, 14.0);
		}
		pageHost.Content = uIElement;
		if (uIElement is FrameworkElement frameworkElement2)
		{
			DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200.0));
			DoubleAnimation animation2 = new DoubleAnimation(14.0, 0.0, TimeSpan.FromMilliseconds(200.0))
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			frameworkElement2.BeginAnimation(UIElement.OpacityProperty, animation);
			((TranslateTransform)frameworkElement2.RenderTransform).BeginAnimation(TranslateTransform.YProperty, animation2);
		}
	}

	private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
	{
		SetSidebarState(!_isSidebarCollapsed);
	}

	private void SetSidebarState(bool collapse)
	{
		_isSidebarCollapsed = collapse;
		if (!string.Equals(_currentRoute, "Home", StringComparison.OrdinalIgnoreCase))
		{
			SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
			SidebarColumn.Width = new GridLength(collapse ? 78.0 : 286.0);
		}
	}

	private void BtnSearch_Click(object sender, RoutedEventArgs e)
	{
		new GlobalSearchWindow().ShowDialog();
	}

	private void BtnEllie_Click(object sender, RoutedEventArgs e)
	{
		new EllieAssistantWindow().ShowDialog();
	}

	private void BtnNotify_Click(object sender, RoutedEventArgs e)
	{
		notificationPopup.IsOpen = !notificationPopup.IsOpen;
	}

	private void BtnNotifyViewAll_Click(object sender, RoutedEventArgs e)
	{
		notificationPopup.IsOpen = false;
	}

	private static string NormalizeRoute(string? route)
	{
		if (!string.IsNullOrWhiteSpace(route))
		{
			return route;
		}
		return "Home";
	}

	private static string FormatSidebarBrandName(string barangayName)
	{
		if (string.IsNullOrWhiteSpace(barangayName))
		{
			return "BARANGAY";
		}
		string text = barangayName.Trim();
		if (text.StartsWith("barangay ", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text;
			int length = "barangay ".Length;
			text = text2.Substring(length, text2.Length - length);
		}
		return text.ToUpperInvariant();
	}

	private void UpdateShellForRoute(string route)
	{
		bool num = string.Equals(route, "Home", StringComparison.OrdinalIgnoreCase);
		bool flag = string.Equals(route, "DashboardNotifications", StringComparison.OrdinalIgnoreCase);
		bool flag2 = num || flag;
		SidebarRoot.Visibility = (flag2 ? Visibility.Collapsed : Visibility.Visible);
		btnToggleSidebar.Visibility = (flag2 ? Visibility.Collapsed : Visibility.Visible);
		TopBarContainer.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		BottomStatusBar.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, null);
		SidebarColumn.Width = new GridLength(flag2 ? 0.0 : (_isSidebarCollapsed ? 78.0 : 286.0));
		TopBarRow.Height = (flag ? new GridLength(0.0) : new GridLength(72.0));
		StatusBarRow.Height = (flag ? new GridLength(0.0) : new GridLength(28.0));
	}

	private void UpdateBreadcrumb(string route)
	{
		bool flag = string.Equals(route, "Home", StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(route, "DashboardNotifications", StringComparison.OrdinalIgnoreCase);
		breadcrumbLabel.Text = RouteToTitle(route);
		breadcrumbRootLabel.Visibility = ((flag || flag2) ? Visibility.Collapsed : Visibility.Visible);
		breadcrumbSeparator.Visibility = ((flag || flag2) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void SyncNavigationSelection(string route)
	{
		RadioButton radioButton = route switch
		{
			"Home" => navHome, 
			"DashboardNotifications" => navDashboard, 
			"Dashboard" => navDashboard, 
			"GovernanceRegistry" => navGovernanceRegistry, 
			"ResidentWorkspace" => navResidents, 
			"ResidentSoloParents" => navSoloParents, 
			"ResidentYouth" => navYouth, 
			"ResidentIndigent" => navIndigent, 
			"Households" => navHouseholds, 
			"ResidentCategories" => navCategories, 
			"DeceasedRegistry" => navDeceased, 
			"Clearances" => navClearances, 
			"Permits" => navPermits, 
			"ResidentCases" => navBlotter, 
			"ResidentPayments" => navPayments, 
			"Ayuda" => navAyuda, 
			"Collections" => navCollections, 
			"Officials" => navOfficials, 
			"StaffUsers" => navStaff, 
			"RolePermissions" => navRoles, 
			"SystemLogs" => navLogs, 
			"NotificationOutbox" => navNotificationOutbox, 
			"Settings" => navSettings, 
			_ => null, 
		};
		if (radioButton == null || radioButton.IsChecked == true)
		{
			return;
		}
		_isSynchronizingNavigation = true;
		try
		{
			radioButton.IsChecked = true;
		}
		finally
		{
			_isSynchronizingNavigation = false;
		}
	}

	private static string RouteToTitle(string route)
	{
		return route switch
		{
			"Home" => "Home", 
			"DashboardNotifications" => "Dashboard Notifications", 
			"Dashboard" => "Dashboard", 
			"GovernanceRegistry" => "Announcements & Projects", 
			"ResidentWorkspace" => "Resident Records", 
			"ResidentSoloParents" => "Solo Parent Registry", 
			"ResidentYouth" => "Youth Registry", 
			"ResidentIndigent" => "Indigent Registry", 
			"Households" => "Households", 
			"ResidentCategories" => "Tags & Categories", 
			"DeceasedRegistry" => "Deceased Registry", 
			"Clearances" => "Clearances Queue", 
			"Permits" => "Permits Queue", 
			"ResidentCases" => "Blotter Cases", 
			"ResidentPayments" => "Payments", 
			"Ayuda" => "Ayuda Assistance", 
			"Collections" => "Finance Operations", 
			"Reports" => "Module Reports", 
			"Officials" => "Barangay Officials", 
			"StaffUsers" => "Staff & Users", 
			"RolePermissions" => "Roles & Permissions", 
			"SystemLogs" => "System Logs", 
			"NotificationOutbox" => "Notification Outbox", 
			"Settings" => "System Settings", 
			_ => "Home", 
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Expected O, but got Unknown
		switch (connectionId)
		{
		case 1:
			SidebarColumn = (ColumnDefinition)target;
			break;
		case 2:
			SidebarRoot = (Border)target;
			break;
		case 3:
			sidebarBrandLogo = (Image)target;
			break;
		case 4:
			sidebarBrandIcon = (IconBlock)target;
			break;
		case 5:
			sidebarBarangayName = (TextBlock)target;
			break;
		case 6:
			sidebarUserInitial = (TextBlock)target;
			break;
		case 7:
			sidebarUsername = (TextBlock)target;
			break;
		case 8:
			sidebarRole = (TextBlock)target;
			break;
		case 9:
			navHome = (RadioButton)target;
			navHome.Checked += NavItem_Checked;
			break;
		case 10:
			navDashboard = (RadioButton)target;
			navDashboard.Checked += NavItem_Checked;
			break;
		case 11:
			navGovernanceRegistry = (RadioButton)target;
			navGovernanceRegistry.Checked += NavItem_Checked;
			break;
		case 12:
			navGroupCommunity = (TextBlock)target;
			break;
		case 13:
			navResidents = (RadioButton)target;
			navResidents.Checked += NavItem_Checked;
			break;
		case 14:
			navSoloParents = (RadioButton)target;
			navSoloParents.Checked += NavItem_Checked;
			break;
		case 15:
			navYouth = (RadioButton)target;
			navYouth.Checked += NavItem_Checked;
			break;
		case 16:
			navIndigent = (RadioButton)target;
			navIndigent.Checked += NavItem_Checked;
			break;
		case 17:
			navHouseholds = (RadioButton)target;
			navHouseholds.Checked += NavItem_Checked;
			break;
		case 18:
			navDeceased = (RadioButton)target;
			navDeceased.Checked += NavItem_Checked;
			break;
		case 19:
			navCategories = (RadioButton)target;
			navCategories.Checked += NavItem_Checked;
			break;
		case 20:
			navGroupServices = (TextBlock)target;
			break;
		case 21:
			navClearances = (RadioButton)target;
			navClearances.Checked += NavItem_Checked;
			break;
		case 22:
			navPermits = (RadioButton)target;
			navPermits.Checked += NavItem_Checked;
			break;
		case 23:
			navGroupBlotter = (TextBlock)target;
			break;
		case 24:
			navBlotter = (RadioButton)target;
			navBlotter.Checked += NavItem_Checked;
			break;
		case 25:
			navGroupFinance = (TextBlock)target;
			break;
		case 26:
			navPayments = (RadioButton)target;
			navPayments.Checked += NavItem_Checked;
			break;
		case 27:
			navAyuda = (RadioButton)target;
			navAyuda.Checked += NavItem_Checked;
			break;
		case 28:
			navCollections = (RadioButton)target;
			navCollections.Checked += NavItem_Checked;
			break;
		case 29:
			navGroupAdmin = (TextBlock)target;
			break;
		case 30:
			navOfficials = (RadioButton)target;
			navOfficials.Checked += NavItem_Checked;
			break;
		case 31:
			navStaff = (RadioButton)target;
			navStaff.Checked += NavItem_Checked;
			break;
		case 32:
			navRoles = (RadioButton)target;
			navRoles.Checked += NavItem_Checked;
			break;
		case 33:
			navLogs = (RadioButton)target;
			navLogs.Checked += NavItem_Checked;
			break;
		case 34:
			navNotificationOutbox = (RadioButton)target;
			navNotificationOutbox.Checked += NavItem_Checked;
			break;
		case 35:
			navSettings = (RadioButton)target;
			navSettings.Checked += NavItem_Checked;
			break;
		case 36:
			TopBarRow = (RowDefinition)target;
			break;
		case 37:
			StatusBarRow = (RowDefinition)target;
			break;
		case 38:
			TopBarContainer = (Border)target;
			break;
		case 39:
			btnToggleSidebar = (Button)target;
			btnToggleSidebar.Click += BtnToggleSidebar_Click;
			break;
		case 40:
			breadcrumbRootLabel = (TextBlock)target;
			break;
		case 41:
			breadcrumbSeparator = (TextBlock)target;
			break;
		case 42:
			breadcrumbLabel = (TextBlock)target;
			break;
		case 43:
			btnSearch = (Button)target;
			btnSearch.Click += BtnSearch_Click;
			break;
		case 44:
			btnEllie = (Button)target;
			btnEllie.Click += BtnEllie_Click;
			break;
		case 45:
			btnNotify = (Button)target;
			btnNotify.Click += BtnNotify_Click;
			break;
		case 46:
			topBarInitial = (TextBlock)target;
			break;
		case 47:
			pageHost = (ContentControl)target;
			break;
		case 48:
			BottomStatusBar = (Border)target;
			break;
		case 49:
			dbStatusDot = (Border)target;
			break;
		case 50:
			dbStatusLabel = (TextBlock)target;
			break;
		case 51:
			barangayLabel = (TextBlock)target;
			break;
		case 52:
			statusClock = (TextBlock)target;
			break;
		case 53:
			notificationPopup = (Popup)target;
			break;
		case 54:
			((Button)target).Click += BtnNotifyViewAll_Click;
			break;
		case 55:
			notificationList = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
