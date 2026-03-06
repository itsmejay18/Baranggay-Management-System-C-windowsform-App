using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public partial class AdminDashboard
{
    private static readonly string[] RibbonPrimaryTabs =
    {
        "Dashboard",
        "Community",
        "Services",
        "Cases",
        "Finance",
        "Reports",
        "Administration"
    };

    private Panel? _ribbonHost;
    private Panel? _ribbonHeader;
    private Panel? _ribbonTabsContainer;
    private FlowLayoutPanel? _ribbonTabs;
    private FlowLayoutPanel? _ribbonHeaderRight;
    private Panel? _ribbonActiveIndicator;
    private Panel? _ribbonContentHost;
    private readonly Dictionary<string, Control> _ribbonPages = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ContextMenuStrip> _ribbonTabMenus = new Dictionary<string, ContextMenuStrip>(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _ribbonIndicatorAnimationTimer = new System.Windows.Forms.Timer();
    private Rectangle _ribbonIndicatorTargetBounds = Rectangle.Empty;
    private float _ribbonIndicatorCurrentX;
    private float _ribbonIndicatorCurrentWidth;
    private bool _ribbonIndicatorAnimatorInitialized;
    private Button? _activeRibbonTab;

    private void InitializeRibbonNavigation()
    {
        if (_ribbonHost != null)
        {
            return;
        }

        panelTop.Controls.Clear();
        panelTop.Padding = new Padding(12, 10, 12, 8);
        panelTop.Height = 124;
        panelSidebar.Visible = false;
        panelSidebar.Width = 0;

        _ribbonHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        _ribbonHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.Transparent
        };

        _ribbonTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            AutoScroll = true,
            Padding = new Padding(4, 2, 4, 2),
            BackColor = Color.White
        };
        _ribbonTabs.Layout += (_, __) => UpdateRibbonActiveIndicator();
        _ribbonTabs.Resize += (_, __) => UpdateRibbonActiveIndicator();

        _ribbonTabsContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.White
        };

        _ribbonActiveIndicator = new Panel
        {
            Height = 3,
            Width = 0,
            BackColor = UiTheme.AccentBlue,
            Visible = false
        };
        _ribbonTabsContainer.Controls.Add(_ribbonTabs);
        _ribbonTabsContainer.Controls.Add(_ribbonActiveIndicator);
        EnsureRibbonIndicatorAnimator();

        _ribbonContentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Slate50,
            Padding = new Padding(6, 8, 6, 6)
        };

        panelTop.Controls.Add(_ribbonHost);
        _ribbonHost.Controls.Add(_ribbonTabsContainer);
        _ribbonHost.Controls.Add(_ribbonHeader);

        BuildRibbonHeader();
        BuildRibbonTabs();
        SetRibbonTab("Dashboard");
    }

    private void BuildRibbonHeader()
    {
        if (_ribbonHeader == null)
        {
            return;
        }

        _ribbonHeader.Controls.Clear();

        var headerLeft = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        var titleStack = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _pageTitleLabel.Margin = new Padding(0, 2, 0, 0);
        _pageSubtitleLabel.Visible = false;

        // Ribbon header shows title only; subtitle is rendered in the content area.
        int headerHeight = Math.Max(48, _pageTitleLabel.PreferredHeight + 16);
        _ribbonHeader.Height = headerHeight;
        int rightTopOffset = Math.Max(4, (headerHeight - 32) / 2);

        titleStack.Controls.Add(_pageTitleLabel);
        headerLeft.Controls.Add(titleStack);

        _ribbonHeaderRight = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _signedInPanel.Dock = DockStyle.None;
        _signedInPanel.Margin = new Padding(0, rightTopOffset + 1, 12, 0);
        _signedInPanel.Padding = new Padding(0);

        panel2.Dock = DockStyle.None;
        panel2.Margin = new Padding(0, rightTopOffset, 0, 0);
        panel2.Padding = new Padding(0);
        panel2.Width = 70;
        panel2.Height = 34;
        panel2.Controls.Clear();

        var iconRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _ellieButton.Dock = DockStyle.None;
        _ellieButton.Size = new Size(32, 32);
        _ellieButton.Margin = new Padding(0, 0, 6, 0);
        _ellieButton.IconSize = 18;

        BtnNotification.Dock = DockStyle.None;
        BtnNotification.Size = new Size(32, 32);
        BtnNotification.Margin = new Padding(0);
        BtnNotification.IconSize = 18;

        iconRow.Controls.Add(_ellieButton);
        iconRow.Controls.Add(BtnNotification);
        panel2.Controls.Add(iconRow);

        _ribbonHeaderRight.Controls.Add(_signedInPanel);
        _ribbonHeaderRight.Controls.Add(panel2);

        _ribbonHeader.Controls.Add(_ribbonHeaderRight);
        _ribbonHeader.Controls.Add(headerLeft);
    }

    private void BuildRibbonTabs()
    {
        if (_ribbonTabs == null)
        {
            return;
        }

        BuildRibbonTabMenus();
        _ribbonTabs.Controls.Clear();
        foreach (var tabName in RibbonPrimaryTabs)
        {
            var tabButton = CreateRibbonTab(tabName, _ribbonTabMenus.ContainsKey(tabName));
            _ribbonTabs.Controls.Add(tabButton);
        }
    }

    private void BuildRibbonPages()
    {
        if (_ribbonContentHost == null)
        {
            return;
        }

        _ribbonPages.Clear();
        _ribbonContentHost.Controls.Clear();

        AddRibbonPage("File", BuildFilePage());
        AddRibbonPage("Home", BuildHomePage());
        AddRibbonPage("Residents", BuildResidentsPage());
        AddRibbonPage("Certificates", BuildCertificatesPage());
        AddRibbonPage("Blotter", BuildBlotterPage());
        AddRibbonPage("Reports", BuildReportsPage());
        AddRibbonPage("Settings", BuildSettingsPage());
        AddRibbonPage("View", BuildViewPage());
        AddRibbonPage("Help", BuildHelpPage());
    }

    private void AddRibbonPage(string name, Control page)
    {
        if (_ribbonContentHost == null)
        {
            return;
        }

        page.Dock = DockStyle.Fill;
        page.Visible = false;
        _ribbonPages[name] = page;
        _ribbonContentHost.Controls.Add(page);
    }

    private void BuildRibbonTabMenus()
    {
        DisposeRibbonTabMenus();

        _ribbonTabMenus["Community"] = CreateRibbonMenu(
            ("Residents", () => NavigateToRoute("Residents"), true),
            ("Households", () => NavigateToRoute("Households"), helper.Permissions.CanViewHouseholds));

        _ribbonTabMenus["Services"] = CreateRibbonMenu(
            ("Certificates", () => NavigateToRoute("Certificates"), true),
            ("Clearances", () => NavigateToRoute("Clearances"), true),
            ("Permits", () => NavigateToRoute("Permits"), true));

        _ribbonTabMenus["Finance"] = CreateRibbonMenu(
            ("Payments", () => NavigateToRoute("Payments"), true),
            ("Collections", () => NavigateToRoute("Collections"), true));

        _ribbonTabMenus["Administration"] = CreateRibbonMenu(
            ("Officials", () => NavigateToRoute("Officials"), true),
            ("Staff / Users", () => NavigateToRoute("StaffUsers"), true),
            ("Settings", () => NavigateToRoute("Settings"), helper.Permissions.CanOpenSettings));
    }

    private ContextMenuStrip CreateRibbonMenu(params (string Text, Action Action, bool Enabled)[] items)
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = UiTheme.LabelFont
        };

        foreach (var item in items)
        {
            var menuItem = new ToolStripMenuItem(item.Text)
            {
                Enabled = item.Enabled
            };
            menuItem.Click += (_, __) => item.Action();
            menu.Items.Add(menuItem);
        }

        return menu;
    }

    private void DisposeRibbonTabMenus()
    {
        foreach (var menu in _ribbonTabMenus.Values)
        {
            try
            {
                menu.Dispose();
            }
            catch
            {
                // Best effort cleanup.
            }
        }

        _ribbonTabMenus.Clear();
    }

    private Button CreateRibbonTab(string text, bool hasMenu)
    {
        var tabFont = new Font(UiTheme.BodyFont.FontFamily, 9.5f, FontStyle.Bold);
        string displayText = hasMenu ? $"{text}  v" : text;
        int width = Math.Max(86, TextRenderer.MeasureText(displayText, tabFont).Width + 24);

        var button = new Button
        {
            Text = displayText,
            AutoSize = false,
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UiTheme.Slate700,
            Font = tabFont,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(0, 4, 4, 4),
            Cursor = Cursors.Hand,
            Tag = text
        };
        if (hasMenu)
        {
            button.AccessibleDescription = $"{text} menu";
        }

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = UiTheme.Slate50;
        button.FlatAppearance.MouseDownBackColor = UiTheme.Slate100;
        button.Click += (_, __) =>
        {
            if (!hasMenu)
            {
                SetRibbonTab(text);
                return;
            }

            ShowRibbonGroupMenu(text, button);
        };
        button.KeyDown += (_, e) =>
        {
            if (!hasMenu)
            {
                return;
            }

            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ShowRibbonGroupMenu(text, button);
                e.Handled = true;
            }
        };
        return button;
    }

    private void ShowRibbonGroupMenu(string groupName, Control anchor)
    {
        if (!_ribbonTabMenus.TryGetValue(groupName, out var menu))
        {
            SetRibbonTab(groupName);
            return;
        }

        ActivateRibbonPrimary(groupName);
        menu.Show(anchor, new Point(0, anchor.Height));
    }

    private Control ResolveRibbonMenuAnchor()
    {
        if (_activeRibbonTab != null)
        {
            return _activeRibbonTab;
        }

        if (_ribbonTabs != null)
        {
            return _ribbonTabs;
        }

        return panelTop;
    }

    private void EnsureRibbonIndicatorAnimator()
    {
        if (_ribbonIndicatorAnimatorInitialized)
        {
            return;
        }

        _ribbonIndicatorAnimationTimer.Interval = 15;
        _ribbonIndicatorAnimationTimer.Tick += RibbonIndicatorAnimationTimer_Tick;
        Disposed += (_, __) =>
        {
            try
            {
                _ribbonIndicatorAnimationTimer.Stop();
                _ribbonIndicatorAnimationTimer.Dispose();
            }
            catch
            {
                // Best effort cleanup.
            }
        };

        _ribbonIndicatorAnimatorInitialized = true;
    }

    private bool TryGetRibbonIndicatorTargetBounds(out Rectangle targetBounds)
    {
        targetBounds = Rectangle.Empty;
        if (_ribbonTabsContainer == null || _ribbonActiveIndicator == null || _activeRibbonTab == null || _activeRibbonTab.Parent == null)
        {
            return false;
        }

        var tabOrigin = _ribbonTabsContainer.PointToClient(_activeRibbonTab.Parent.PointToScreen(_activeRibbonTab.Location));
        targetBounds = new Rectangle(
            tabOrigin.X + 8,
            _ribbonTabsContainer.Height - _ribbonActiveIndicator.Height,
            Math.Max(24, _activeRibbonTab.Width - 16),
            _ribbonActiveIndicator.Height);
        return true;
    }

    private void RibbonIndicatorAnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_ribbonActiveIndicator == null)
        {
            _ribbonIndicatorAnimationTimer.Stop();
            return;
        }

        const float easing = 0.32f;
        _ribbonIndicatorCurrentX += (_ribbonIndicatorTargetBounds.X - _ribbonIndicatorCurrentX) * easing;
        _ribbonIndicatorCurrentWidth += (_ribbonIndicatorTargetBounds.Width - _ribbonIndicatorCurrentWidth) * easing;

        int x = (int)Math.Round(_ribbonIndicatorCurrentX);
        int width = Math.Max(24, (int)Math.Round(_ribbonIndicatorCurrentWidth));
        _ribbonActiveIndicator.Bounds = new Rectangle(
            x,
            _ribbonIndicatorTargetBounds.Y,
            width,
            _ribbonIndicatorTargetBounds.Height);
        _ribbonActiveIndicator.Visible = true;
        _ribbonActiveIndicator.BringToFront();

        if (Math.Abs(_ribbonIndicatorTargetBounds.X - _ribbonIndicatorCurrentX) < 0.75f &&
            Math.Abs(_ribbonIndicatorTargetBounds.Width - _ribbonIndicatorCurrentWidth) < 0.75f)
        {
            _ribbonIndicatorAnimationTimer.Stop();
            _ribbonIndicatorCurrentX = _ribbonIndicatorTargetBounds.X;
            _ribbonIndicatorCurrentWidth = _ribbonIndicatorTargetBounds.Width;
            _ribbonActiveIndicator.Bounds = _ribbonIndicatorTargetBounds;
        }
    }

    private void ActivateRibbonPrimary(string name)
    {
        if (_ribbonTabs == null)
        {
            return;
        }

        if (_activeRibbonTab != null)
        {
            _activeRibbonTab.BackColor = Color.White;
            _activeRibbonTab.ForeColor = UiTheme.Slate700;
        }

        foreach (Control child in _ribbonTabs.Controls)
        {
            if (child is not Button tabButton)
            {
                continue;
            }

            var tabName = Convert.ToString(tabButton.Tag) ?? tabButton.Text;
            if (!string.Equals(tabName, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _activeRibbonTab = tabButton;
            _activeRibbonTab.BackColor = Color.White;
            _activeRibbonTab.ForeColor = UiTheme.Slate900;
            break;
        }

        UpdateRibbonActiveIndicator();
    }

    private static string ResolvePrimaryTabForRoute(string route)
    {
        return route switch
        {
            "Residents" => "Community",
            "Households" => "Community",
            "Certificates" => "Services",
            "Clearances" => "Services",
            "Permits" => "Services",
            "Cases" => "Cases",
            "Payments" => "Finance",
            "Collections" => "Finance",
            "Reports" => "Reports",
            "Officials" => "Administration",
            "StaffUsers" => "Administration",
            "Settings" => "Administration",
            "Dashboard" => "Dashboard",
            _ => "Dashboard"
        };
    }

    private void NavigateToRoute(string route)
    {
        ActivateRibbonPrimary(ResolvePrimaryTabForRoute(route));

        switch (route)
        {
            case "Dashboard":
                ShowDashboard();
                return;
            case "Residents":
                OpenResidents(ResidentsView.Profile);
                return;
            case "Households":
                OpenHouseholdsModule();
                return;
            case "Certificates":
                OpenResidents(ResidentsView.Certificates);
                return;
            case "Clearances":
                OpenClearancesModule();
                return;
            case "Permits":
                OpenPermitsModule();
                return;
            case "Cases":
                OpenResidents(ResidentsView.Blotter);
                return;
            case "Payments":
                OpenPaymentsModule();
                return;
            case "Collections":
                OpenCollectionsModule();
                return;
            case "Reports":
                OpenReports();
                return;
            case "Officials":
                OpenOfficialsModule();
                return;
            case "StaffUsers":
                OpenStaffUsersModule();
                return;
            case "Settings":
                SidebarSettings_Click(null, EventArgs.Empty);
                return;
            default:
                ShowDashboard();
                return;
        }
    }

    private static string ResolvePrimaryTabForResidentsView(ResidentsView view)
    {
        return view switch
        {
            ResidentsView.Certificates => "Services",
            ResidentsView.Blotter => "Cases",
            _ => "Community"
        };
    }

    private void SyncRibbonPrimaryFromResidentsView(ResidentsView view)
    {
        ActivateRibbonPrimary(ResolvePrimaryTabForResidentsView(view));
    }

    private void SyncRibbonPrimary(string primaryTabName)
    {
        ActivateRibbonPrimary(primaryTabName);
    }

    private void SetRibbonTab(string name)
    {
        ActivateRibbonPrimary(name);
        NavigateFromRibbonTab(name);
    }

    private void UpdateRibbonActiveIndicator()
    {
        if (_ribbonActiveIndicator == null)
        {
            return;
        }

        if (!TryGetRibbonIndicatorTargetBounds(out var targetBounds))
        {
            _ribbonActiveIndicator.Visible = false;
            _ribbonIndicatorAnimationTimer.Stop();
            return;
        }

        if (!_ribbonActiveIndicator.Visible || _ribbonActiveIndicator.Width <= 0)
        {
            _ribbonIndicatorAnimationTimer.Stop();
            _ribbonIndicatorTargetBounds = targetBounds;
            _ribbonIndicatorCurrentX = targetBounds.X;
            _ribbonIndicatorCurrentWidth = targetBounds.Width;
            _ribbonActiveIndicator.Bounds = targetBounds;
            _ribbonActiveIndicator.Visible = true;
            _ribbonActiveIndicator.BringToFront();
            return;
        }

        _ribbonIndicatorTargetBounds = targetBounds;
        if (Math.Abs(_ribbonActiveIndicator.Left - targetBounds.X) <= 1 &&
            Math.Abs(_ribbonActiveIndicator.Width - targetBounds.Width) <= 1)
        {
            _ribbonIndicatorAnimationTimer.Stop();
            _ribbonIndicatorCurrentX = targetBounds.X;
            _ribbonIndicatorCurrentWidth = targetBounds.Width;
            _ribbonActiveIndicator.Bounds = targetBounds;
            return;
        }

        _ribbonIndicatorCurrentX = _ribbonActiveIndicator.Left;
        _ribbonIndicatorCurrentWidth = _ribbonActiveIndicator.Width;
        if (!_ribbonIndicatorAnimationTimer.Enabled)
        {
            _ribbonIndicatorAnimationTimer.Start();
        }
    }

    private void NavigateFromRibbonTab(string name)
    {
        switch (name)
        {
            case "Home":
            case "Dashboard":
                NavigateToRoute("Dashboard");
                break;
            case "Residents":
                NavigateToRoute("Residents");
                break;
            case "Certificates":
                NavigateToRoute("Certificates");
                break;
            case "Community":
                ShowRibbonGroupMenu("Community", ResolveRibbonMenuAnchor());
                break;
            case "Services":
                ShowRibbonGroupMenu("Services", ResolveRibbonMenuAnchor());
                break;
            case "Cases":
            case "Blotter":
                NavigateToRoute("Cases");
                break;
            case "Finance":
                ShowRibbonGroupMenu("Finance", ResolveRibbonMenuAnchor());
                break;
            case "Reports":
                NavigateToRoute("Reports");
                break;
            case "Administration":
            case "Settings":
                ShowRibbonGroupMenu("Administration", ResolveRibbonMenuAnchor());
                break;
        }
    }

    private Control BuildFilePage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Session",
            CreateRibbonCommand("Dashboard", (_, __) => ShowDashboard(), primary: true),
            CreateRibbonCommand("Exit", (_, __) => Close())));
        return page;
    }

    private Control BuildHomePage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Dashboard",
            CreateRibbonCommand("Dashboard", (_, __) => ShowDashboard(), primary: true),
            CreateRibbonCommand("Refresh", (_, __) => _controller.LoadDashboardStats())));
        page.Controls.Add(CreateRibbonGroup("Residents",
            CreateRibbonCommand("Profile", (_, __) => OpenResidents(ResidentsView.Profile)),
            CreateRibbonCommand("History", (_, __) => OpenResidents(ResidentsView.History))));
        page.Controls.Add(CreateRibbonGroup("Documents",
            CreateRibbonCommand("Certificates", (_, __) => OpenResidents(ResidentsView.Certificates))));
        page.Controls.Add(CreateRibbonGroup("Blotter",
            CreateRibbonCommand("Cases", (_, __) => OpenResidents(ResidentsView.Blotter))));
        page.Controls.Add(CreateRibbonGroup("Reports",
            CreateRibbonCommand("Reports", (_, __) => SidebarReports_Click(null, EventArgs.Empty))));
        return page;
    }

    private Control BuildResidentsPage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Resident Views",
            CreateRibbonCommand("Profile", (_, __) => OpenResidents(ResidentsView.Profile), primary: true),
            CreateRibbonCommand("History", (_, __) => OpenResidents(ResidentsView.History)),
            CreateRibbonCommand("Blotter", (_, __) => OpenResidents(ResidentsView.Blotter))));
        page.Controls.Add(CreateRibbonGroup("Management",
            CreateRibbonCommand("Certificates", (_, __) => OpenResidents(ResidentsView.Certificates)),
            CreateRibbonCommand("Users", (_, __) => OpenUsersList())));
        return page;
    }

    private Control BuildCertificatesPage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Requests",
            CreateRibbonCommand("New", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.NewRequest), primary: true),
            CreateRibbonCommand("Edit", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.EditRequest))));
        page.Controls.Add(CreateRibbonGroup("Processing",
            CreateRibbonCommand("Approve", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.Approve), primary: true),
            CreateRibbonCommand("Issue", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.Issue))));
        page.Controls.Add(CreateRibbonGroup("Output",
            CreateRibbonCommand("Print", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.Print), primary: true),
            CreateRibbonCommand("Export", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.Export))));
        page.Controls.Add(CreateRibbonGroup("Maintenance",
            CreateRibbonCommand("Cancel", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.Cancel)),
            CreateRibbonCommand("Refresh", (_, __) => OpenResidents(ResidentsView.Certificates, CertificateAction.Refresh))));
        page.Controls.Add(CreateRibbonGroup("History",
            CreateRibbonCommand("Certificate History", (_, __) => OpenResidents(ResidentsView.History)),
            CreateRibbonCommand("Dashboard", (_, __) => ShowDashboard())));
        return page;
    }

    private Control BuildBlotterPage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Blotter",
            CreateRibbonCommand("Cases", (_, __) => OpenResidents(ResidentsView.Blotter), primary: true),
            CreateRibbonCommand("History", (_, __) => OpenResidents(ResidentsView.History))));
        return page;
    }

    private Control BuildReportsPage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Reports",
            CreateRibbonCommand("Open Reports", (_, __) => SidebarReports_Click(null, EventArgs.Empty), primary: true),
            CreateRibbonCommand("Dashboard", (_, __) => ShowDashboard())));
        return page;
    }

    private Control BuildSettingsPage()
    {
        var page = CreateRibbonPageContainer();
        var settingsButton = CreateRibbonCommand("Settings", (_, __) => SidebarSettings_Click(null, EventArgs.Empty), primary: true);
        settingsButton.Enabled = helper.Permissions.CanOpenSettings;

        var usersButton = CreateRibbonCommand("Users", (_, __) => OpenUsersList());
        usersButton.Enabled = helper.Permissions.CanManageUsers;

        page.Controls.Add(CreateRibbonGroup("System", settingsButton, usersButton));
        return page;
    }

    private Control BuildViewPage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Layout",
            CreateRibbonCommand("Toggle Sidebar", (_, __) => ToggleSidebar(), primary: true),
            CreateRibbonCommand("Dashboard", (_, __) => ShowDashboard())));
        return page;
    }

    private Control BuildHelpPage()
    {
        var page = CreateRibbonPageContainer();
        page.Controls.Add(CreateRibbonGroup("Help",
            CreateRibbonCommand("Ellie Assistant", (_, __) => _controller.HandleOpenEllieAssistant(), primary: true),
            CreateRibbonCommand("Shortcuts", (_, __) => ShowKeyboardShortcutsHelp()),
            CreateRibbonCommand("About", (_, __) => ControllerDialogs.Info("Barangay System\nVersion 1.0", "About"))));
        return page;
    }

    private static FlowLayoutPanel CreateRibbonPageContainer()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = UiTheme.Slate50
        };
    }

    private static Panel CreateRibbonGroup(string title, params Control[] commands)
    {
        int commandsWidth = 0;
        for (int i = 0; i < commands.Length; i++)
        {
            commandsWidth += commands[i].Width;
            if (i < commands.Length - 1)
            {
                commandsWidth += 8;
            }
        }

        var group = new Panel
        {
            Width = Math.Max(190, commandsWidth + 10),
            Height = 82,
            Margin = new Padding(0, 0, 12, 0),
            BackColor = Color.Transparent
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var commandPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        foreach (var command in commands)
        {
            commandPanel.Controls.Add(command);
        }

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Bottom,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate500,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            AutoEllipsis = true,
            Height = 18
        };

        layout.Controls.Add(commandPanel, 0, 0);
        layout.Controls.Add(label, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private static Button CreateRibbonCommand(string text, EventHandler onClick, bool primary = false)
    {
        int width = Math.Max(92, TextRenderer.MeasureText(text, UiTheme.ButtonFont).Width + 28);
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Size = new Size(width, 40),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        if (primary)
        {
            UiTheme.StylePrimaryButton(button);
            button.Height = 40;
        }
        else
        {
            UiTheme.StyleSecondaryButton(button);
            button.Height = 38;
        }
        button.Click += onClick;
        return button;
    }

    private void ToggleSidebar()
    {
        panelSidebar.Visible = !panelSidebar.Visible;
        panelSidebar.Width = panelSidebar.Visible ? 220 : 0;
    }

    private void OpenUsersList()
    {
        if (!helper.Permissions.CanManageUsers)
        {
            ControllerDialogs.Warning("Only Admin users can manage user accounts.");
            return;
        }

        OpenUsersListModule();
    }
}
