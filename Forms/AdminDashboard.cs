using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using FontAwesome.Sharp;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

public partial class AdminDashboard : Form
{
    private enum DashboardResponsiveMode
    {
        Unknown = 0, 
        Wide,
        Medium,
        Narrow
    }

    private enum OfficialPresence
    {
        Online,
        Offline,
        Away
    }

    private static bool RespectDesignerLayout = false;
    private static readonly Color CardResidentsAccent = Color.FromArgb(34, 139, 34);
    private static readonly Color CardActiveAccent = Color.FromArgb(22, 163, 74);
    private static readonly Color CardHouseholdsAccent = Color.FromArgb(25, 118, 210);
    private static readonly Color CardCertAccent = Color.FromArgb(245, 158, 11);
    private static readonly Color CardBlotterAccent = Color.FromArgb(220, 38, 38);
    private static readonly Color CardLightBorder = Color.FromArgb(219, 227, 235);
    private static readonly Color StatusOnline = Color.FromArgb(34, 197, 94);
    private static readonly Color StatusOffline = Color.FromArgb(148, 163, 184);
    private static readonly Color StatusAway = Color.FromArgb(234, 179, 8);
    private const int StaffCardPhotoSize = 72;
    private const int StaffCardWidth = 340;
    private const int StaffCardHeight = 150;

    private Label _lblBusinessName = new Label();
    private Label _lblBusinessNature = new Label();
    private AdminDashboardController _controller;
    private int[] _certTrend = Array.Empty<int>();
    private int[] _blotterTrend = Array.Empty<int>();
    private int[] _residentTrend = Array.Empty<int>();
    private OfficialCard[] _officialCards = Array.Empty<OfficialCard>();
    private int? _prevTotalResidents;
    private int? _prevActiveResidents;
    private int? _prevHouseholds;
    private int? _prevPendingCertificates;
    private int? _prevOngoingBlotter;
    private DynamicSidebarController? _sidebarController;
    private IconButton? _sidebarToggleButton;
    private readonly ContextMenuStrip _ellieMenu = new ContextMenuStrip();
    private readonly ToolStripMenuItem _ellieOpenItem = new ToolStripMenuItem("Ellie Assistant");
    private readonly IconButton _globalSearchButton = new IconButton();
    private string _selectedActionTarget = string.Empty;
    private DataTable? _actionCenterTable;
    private readonly System.Windows.Forms.Timer _notificationTimer = new System.Windows.Forms.Timer();
    private readonly System.Windows.Forms.Timer _schedulerTimer = new System.Windows.Forms.Timer();
    private DateTime _lastActionCenterRefreshAt = DateTime.MinValue;
    private DateTime _lastNotificationsRefreshAt = DateTime.MinValue;
    private DateTime _lastNotificationAutomationAt = DateTime.MinValue;
    private DateTime _lastDailyChecksRunDate = DateTime.MinValue;
    private bool _notificationOpen;
    private int _notificationTargetHeight;
    private readonly Panel _contentSubtitlePanel = new Panel();
    private readonly Label _contentSubtitleLabel = new Label();
    private readonly Panel _contentBodyHostPanel = new Panel();
    private readonly Panel _workspaceHostPanel = new Panel();
    private readonly Panel _dashboardHostPanel = new Panel();
    private Form? _activeWorkspaceForm;
    private Residents? _residentsWorkspaceForm;
    private readonly Panel _quickActionsPanel = new Panel();
    private readonly FlowLayoutPanel _quickActionsFlow = new FlowLayoutPanel();
    private readonly IconButton _quickAddResidentButton = new IconButton();
    private readonly IconButton _quickAddCertificateButton = new IconButton();
    private readonly IconButton _quickAddBlotterButton = new IconButton();
    private readonly IconButton _quickAddAnnouncementButton = new IconButton();
    private readonly IconButton _quickRefreshButton = new IconButton();

    private readonly FlowLayoutPanel _backupStatusPanel = new FlowLayoutPanel();
    private readonly Label _schemaVersionLabel = new Label();
    private readonly Panel _backupStatusDot = new Panel();
    private readonly Label _backupStatusLabel = new Label();
    private readonly ComboBox _backupModeCombo = new ComboBox();
    private readonly IconButton _backupNowButton = new IconButton();
    private readonly IconButton _backupOpenFolderButton = new IconButton();
    private readonly ToolTip _backupToolTip = new ToolTip();
    private readonly ToolTip _officialStatusToolTip = new ToolTip();
    private readonly Label _connectivityStatusLabel = new Label();
    private readonly HashSet<Panel> _lightBorderPanels = new HashSet<Panel>();
    private readonly FlowLayoutPanel _staffCardsFlow = new FlowLayoutPanel();
    private DateTime _lastBackupStatusRefreshAt = DateTime.MinValue;
    private DashboardResponsiveMode _dashboardResponsiveMode = DashboardResponsiveMode.Unknown;

    public AdminDashboard()
    {
        InitializeComponent();
        DisableLegacyResidentSurface();
        InitializeWorkspaceLayout();
        _sidebarToggleButton = FindSidebarToggleButton();
        if (_sidebarToggleButton == null)
        {
            _sidebarToggleButton = CreateSidebarToggleButton();
        }
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        _controller = new AdminDashboardController(this);
        _officialCards = new[]
        {
            new OfficialCard(officialCard1, officialPhoto1, officialName1, officialRole1, officialStatus1, officialUpdate1),
            new OfficialCard(officialCard2, officialPhoto2, officialName2, officialRole2, officialStatus2, officialUpdate2),
            new OfficialCard(officialCard3, officialPhoto3, officialName3, officialRole3, officialStatus3, officialUpdate3),
        };
        ApplyDashboardTheme();
        InitializeRibbonNavigation();
        ConfigureEllieAssistant();
        ConfigureNotifications();
        ConfigureScheduler();
        ConfigureGlobalSearch();
        ConfigureBackupMonitor();
        WireSidebar();
        ApplyRolePermissions();
        ConfigureDynamicSidebar();
        WireOfficials();
        ShowDashboard();
        WireConnectivityStatus();
    }

    private void DisableLegacyResidentSurface()
    {
        if (contentPanel == null)
        {
            return;
        }

        // Keep the old designer block out of the runtime tree.
        if (contentPanel.Parent != null)
        {
            contentPanel.Parent.Controls.Remove(contentPanel);
        }

        contentPanel.Visible = false;
        contentPanel.Enabled = false;
    }

    private void ApplyDashboardTheme()
    {
        bool allowContentLayout = !RespectDesignerLayout;

        BackColor = UiTheme.Slate100;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Text = "Barangay System - Admin";
        MinimumSize = new Size(1100, 650);

        panelSidebar.BackColor = UiTheme.Slate900;
        panelSidebar.Dock = DockStyle.Left;
        panelSidebar.Width = 220;
        panelSidebar.Padding = new Padding(12, 20, 12, 12);

        panelTop.BackColor = Color.White;
        panelTop.Dock = DockStyle.Top;
        panelTop.Height = 72;

        panel1.BackColor = UiTheme.Slate100;
        panel1.Dock = DockStyle.Fill;
        _contentBodyHostPanel.BackColor = UiTheme.Slate100;
        _contentSubtitlePanel.BackColor = UiTheme.Slate100;
        _dashboardHostPanel.BackColor = UiTheme.Slate100;
        _workspaceHostPanel.BackColor = UiTheme.Slate100;

        UiTheme.AttachGradient(panelTop, Color.White, UiTheme.Slate50, 90f);

        _pageTitleLabel.Font = UiTheme.HeadingFont;
        _pageTitleLabel.ForeColor = UiTheme.Slate900;
        _pageSubtitleLabel.Visible = false;
        _contentSubtitleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _contentSubtitleLabel.ForeColor = UiTheme.Slate600;

        UpdateSignedInLabel();
        _signedInLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        _signedInLabel.ForeColor = UiTheme.Slate500;
        _ellieButton.FlatAppearance.BorderSize = 0;
        _ellieButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(236, 245, 255);
        _ellieButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(246, 250, 255);
        _ellieButton.BackColor = Color.Transparent;
        _ellieButton.IconColor = UiTheme.Slate600;
        _ellieButton.Cursor = Cursors.Hand;
        BtnNotification.FlatAppearance.BorderSize = 0;
        BtnNotification.FlatAppearance.MouseDownBackColor = Color.FromArgb(236, 245, 255);
        BtnNotification.FlatAppearance.MouseOverBackColor = Color.FromArgb(246, 250, 255);
        BtnNotification.BackColor = Color.Transparent;
        BtnNotification.IconColor = UiTheme.Slate600;
        BtnNotification.Cursor = Cursors.Hand;
        if (!RespectDesignerLayout && _sidebarToggleButton != null)
        {
            _pageTitleLabel.Left = _sidebarToggleButton.Right + 10;
        }

        dashboardPanel.BackColor = UiTheme.Slate100;
        dashboardLowerPanel.BackColor = UiTheme.Slate100;
        dashboardTrendsPanel.BackColor = UiTheme.Slate100;
        dashboardCards.BackColor = UiTheme.Slate100;

        StyleStatCard(statResidentsCard, _statResidentsValue, _statResidentsLabel, CardResidentsAccent, allowContentLayout);
        StyleStatCard(statActiveCard, _statActiveValue, _statActiveLabel, CardActiveAccent, allowContentLayout);
        StyleStatCard(statHouseholdsCard, _statHouseholdsValue, _statHouseholdsLabel, CardHouseholdsAccent, allowContentLayout);
        StyleStatCard(statCertsCard, _statCertsValue, _statCertsLabel, CardCertAccent, allowContentLayout);
        StyleStatCard(statBlotterCard, _statBlotterValue, _statBlotterLabel, CardBlotterAccent, allowContentLayout);

        StyleTrendPanel(certTrendPanel, certTrendTitle, CardCertAccent);
        StyleTrendPanel(blotterTrendPanel, blotterTrendTitle, CardBlotterAccent);
        StyleTrendPanel(residentsTrendPanel, residentsTrendTitle, CardResidentsAccent);
        ConfigureStatIcons();

        StyleTrendLabels(
            new[] { certReqLabel, certAppLabel, certIssLabel, certCanLabel, blotterOngoingLabel, blotterSettledLabel, blotterReferredLabel,
                monthLabel1, monthLabel2, monthLabel3, monthLabel4, monthLabel5, monthLabel6 },
            new[] { certReqValue, certAppValue, certIssValue, certCanValue, blotterOngoingValue, blotterSettledValue, blotterReferredValue,
                monthValue1, monthValue2, monthValue3, monthValue4, monthValue5, monthValue6 });

        StyleTrendBars(new[]
        {
            certReqBar, certAppBar, certIssBar, certCanBar,
            blotterOngoingBar, blotterSettledBar, blotterReferredBar,
            monthBar1, monthBar2, monthBar3, monthBar4, monthBar5, monthBar6
        }, allowContentLayout);
        ApplyTrendSectionColors();

        certReqBar.Visible = false;
        certAppBar.Visible = false;
        certIssBar.Visible = false;
        certCanBar.Visible = false;
        blotterOngoingBar.Visible = false;
        blotterSettledBar.Visible = false;
        blotterReferredBar.Visible = false;
        monthBar1.Visible = false;
        monthBar2.Visible = false;
        monthBar3.Visible = false;
        monthBar4.Visible = false;
        monthBar5.Visible = false;
        monthBar6.Visible = false;

        WireSparkline(certSparkline, () => _certTrend, CardCertAccent);
        WireSparkline(blotterSparkline, () => _blotterTrend, CardBlotterAccent);
        WireSparkline(residentsSparkline, () => _residentTrend, CardResidentsAccent);

        StyleOfficialsPanel(allowContentLayout);
        StyleFeaturePanels(allowContentLayout);
        UiTheme.StandardizeButtonLayout(this);
        ApplyDashboardButtonSizingOverrides();
        UiTheme.SetTabOrder(
            btnUsers,
            iconButton4,
            iconButton2,
            _sidebarReports,
            _sidebarSettings,
            announcementsNew,
            announcementsRefresh,
            projectsNew,
            projectsRefresh,
            notificationViewAll);
        UiTheme.EnhanceAccessibility(this);
        ApplyDashboardResponsiveLayout();
    }

    private void ApplyDashboardButtonSizingOverrides()
    {
        Button[] compactButtons =
        {
            announcementsNew,
            announcementsRefresh,
            projectsNew,
            projectsRefresh
        };
        foreach (Button button in compactButtons)
        {
            button.MinimumSize = new Size(0, 0);
            button.MaximumSize = Size.Empty;
            button.AutoSize = false;
            button.Width = 100;
            button.Height = 30;
        }

        foreach (OfficialCard card in _officialCards)
        {
            card.UpdateButton.MinimumSize = new Size(100, 30);
            card.UpdateButton.MaximumSize = new Size(120, 32);
            card.UpdateButton.Width = 100;
            card.UpdateButton.Height = 30;

            OfficialPresence status = card.Status.BackColor == StatusOffline
                ? OfficialPresence.Offline
                : card.Status.BackColor == StatusAway
                    ? OfficialPresence.Away
                    : OfficialPresence.Online;
            StyleStatusButton(card.Status, status);
        }
    }

    private void StyleFeaturePanels(bool allowLayout)
    {
        StyleFeaturePanel(announcementsPanel, announcementsHeader);
        StyleFeaturePanel(projectsPanel, projectsHeader);
        StyleFeaturePanel(actionCenterPanel, actionCenterHeader);

        announcementsNew.Text = "Create";
        announcementsRefresh.Text = "Reload";
        projectsNew.Text = "Create";
        projectsRefresh.Text = "Reload";
        announcementsNew.Width = 100;
        announcementsRefresh.Width = 100;
        projectsNew.Width = 100;
        projectsRefresh.Width = 100;
        announcementsNew.Height = 30;
        announcementsRefresh.Height = 30;
        projectsNew.Height = 30;
        projectsRefresh.Height = 30;

        UiTheme.StyleSecondaryButton(announcementsNew);
        UiTheme.StyleSecondaryButton(announcementsRefresh);
        UiTheme.StyleSecondaryButton(projectsNew);
        UiTheme.StyleSecondaryButton(projectsRefresh);
        announcementsGrid.Visible = false;
        projectsGrid.Visible = false;
        StyleActionCalendar();
        ConfigureFeatureCards(announcementsCards, allowLayout);
        ConfigureFeatureCards(projectsCards, allowLayout);
    }

    private void StyleFeaturePanel(Panel panel, Label header)
    {
        UiTheme.StyleSectionCard(panel, Color.White, enforceBorder: false);
        ApplyLightBorder(panel);
        UiTheme.StyleSectionHeader(header);
        header.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
        panel.Padding = new Padding(12, 10, 12, 12);
    }

    private void ConfigureFeatureCards(FlowLayoutPanel cards, bool allowLayout)
    {
        if (allowLayout)
        {
            cards.FlowDirection = FlowDirection.TopDown;
            cards.WrapContents = false;
            cards.AutoScroll = true;
            cards.Padding = new Padding(0, 4, 0, 0);
        }
        cards.Resize -= FeatureCards_Resize;
        cards.Resize += FeatureCards_Resize;
    }

    private void StyleActionCalendar()
    {
        if (actionCenterCalendar == null)
        {
            return;
        }

        actionCenterPanel.BackColor = UiTheme.Slate50;
        actionCenterCalendar.Font = new Font(UiTheme.BodyFont.FontFamily, 11.5F, FontStyle.Regular);
        actionCenterCalendar.BackColor = Color.White;
        actionCenterCalendar.AccentColor = UiTheme.AccentBlue;
        actionCenterCalendar.HeaderBackColor = Blend(Color.White, UiTheme.AccentBlue, 6);
        actionCenterCalendar.MutedTextColor = UiTheme.Slate50;
        actionCenterCalendar.ApplyTheme();
        actionCenterPanel.Padding = new Padding(12, 10, 12, 10);
        actionCenterLayout.Padding = new Padding(0, 6, 0, 0);
        if (actionCenterLayout.RowStyles.Count >= 2)
        {
            actionCenterLayout.RowStyles[1].SizeType = SizeType.Absolute;
            actionCenterLayout.RowStyles[1].Height = 72F;
        }

        if (actionCenterCalendarHost != null)
        {
            actionCenterCalendarHost.BackColor = Color.White;
            actionCenterCalendarHost.Padding = new Padding(1);
            ApplyLightBorder(actionCenterCalendarHost);
        }
        if (actionCenterInfoPanel != null)
        {
            actionCenterInfoPanel.BackColor = Blend(Color.White, UiTheme.AccentBlue, 6);
            actionCenterInfoPanel.Padding = new Padding(10, 6, 10, 6);
        }
        if (actionCenterInfoTitle != null)
        {
            actionCenterInfoTitle.Font = new Font(UiTheme.LabelFont, FontStyle.Bold);
            actionCenterInfoTitle.ForeColor = UiTheme.Slate900;
            actionCenterInfoTitle.Dock = DockStyle.Top;
            actionCenterInfoTitle.AutoSize = false;
            actionCenterInfoTitle.Height = 20;
        }
        if (actionCenterInfoText != null)
        {
            actionCenterInfoText.Font = UiTheme.LabelFont;
            actionCenterInfoText.ForeColor = UiTheme.Slate700;
            actionCenterInfoText.AutoSize = false;
            actionCenterInfoText.AutoEllipsis = false;
            actionCenterInfoText.Dock = DockStyle.Fill;
            actionCenterInfoText.TextAlign = ContentAlignment.TopLeft;
        }
        if (actionCenterInfoPanel != null)
        {
            actionCenterInfoPanel.Resize -= ActionCenterInfoPanel_Resize;
            actionCenterInfoPanel.Resize += ActionCenterInfoPanel_Resize;
            UpdateActionCenterInfoLayout();
        }

        actionCenterCalendar.SelectedDateChanged -= ActionCenterCalendar_DateSelected;
        actionCenterCalendar.SelectedDateChanged += ActionCenterCalendar_DateSelected;
        UpdateActionCalendarInfo(actionCenterCalendar.SelectedDate);

        actionCenterLayout.Resize -= ActionCenterLayout_Resize;
        actionCenterLayout.Resize += ActionCenterLayout_Resize;
    }

    private void ActionCenterCalendar_DateSelected(object? sender, EventArgs e)
    {
        UpdateActionCalendarInfo(actionCenterCalendar.SelectedDate);
    }

    private void UpdateActionCalendarInfo(DateTime date)
    {
        if (actionCenterInfoText == null)
        {
            return;
        }

        string dateText = date.ToString("ddd, MMM dd, yyyy");
        int alertCount = _actionCenterTable?.Rows.Count ?? 0;
        if (alertCount <= 0)
        {
            actionCenterInfoText.Text = $"Selected: {dateText}\r\nNo urgent items. Next: review pending certificates and blotter cases.";
        }
        else
        {
            actionCenterInfoText.Text = $"Selected: {dateText}\r\n{alertCount} urgent item{(alertCount == 1 ? "" : "s")}.";
        }

        UpdateActionCenterInfoLayout();
    }

    private void ActionCenterInfoPanel_Resize(object? sender, EventArgs e)
    {
        UpdateActionCenterInfoLayout();
    }

    private void UpdateActionCenterInfoLayout()
    {
        if (actionCenterInfoPanel == null || actionCenterInfoText == null)
        {
            return;
        }
        actionCenterInfoText.BringToFront();
    }
    private void ActionCenterLayout_Resize(object? sender, EventArgs e)
    {
        // Fill layout; no manual centering needed.
    }

    private void ApplyDashboardResponsiveLayout()
    {
        if (IsDisposed || panel1 == null || panel1.ClientSize.Width <= 0)
        {
            return;
        }

        int contentWidth = panel1.ClientSize.Width;
        var nextMode = contentWidth >= 1500
            ? DashboardResponsiveMode.Wide
            : contentWidth >= 1180
                ? DashboardResponsiveMode.Medium
                : DashboardResponsiveMode.Narrow;

        _dashboardResponsiveMode = nextMode;

        dashboardPanel.SuspendLayout();
        dashboardLowerPanel.SuspendLayout();
        dashboardCards.SuspendLayout();
        dashboardLowerTable.SuspendLayout();
        dashboardFeaturesTable.SuspendLayout();

        try
        {
            ConfigureDashboardCardsLayout(nextMode);
            ConfigureDashboardLowerLayout(nextMode);
            ConfigureDashboardFeaturesLayout(nextMode);
        }
        finally
        {
            dashboardFeaturesTable.ResumeLayout();
            dashboardLowerTable.ResumeLayout();
            dashboardCards.ResumeLayout();
            dashboardLowerPanel.ResumeLayout();
            dashboardPanel.ResumeLayout();
        }
    }

    private void ConfigureDashboardCardsLayout(DashboardResponsiveMode mode)
    {
        var cards = new Control[]
        {
            statResidentsCard,
            statActiveCard,
            statHouseholdsCard,
            statCertsCard,
            statBlotterCard
        };

        int columns = mode switch
        {
            DashboardResponsiveMode.Wide => 5,
            DashboardResponsiveMode.Medium => 3,
            _ => 2
        };
        int rows = (int)Math.Ceiling(cards.Length / (double)columns);

        dashboardPanel.Padding = mode switch
        {
            DashboardResponsiveMode.Wide => new Padding(12, 12, 12, 12),
            DashboardResponsiveMode.Medium => new Padding(12, 12, 12, 12),
            _ => new Padding(12, 10, 12, 10)
        };
        dashboardPanel.Height = mode switch
        {
            DashboardResponsiveMode.Wide => 150,
            DashboardResponsiveMode.Medium => 258,
            _ => 360
        };
        dashboardCards.AutoScroll = mode != DashboardResponsiveMode.Wide;

        RebuildTableLayout(dashboardCards, columns, rows);
        dashboardCards.Controls.Clear();

        for (int i = 0; i < cards.Length; i++)
        {
            int column = i % columns;
            int row = i / columns;
            int right = column < columns - 1 ? 8 : 0;
            int bottom = row < rows - 1 ? 8 : 0;
            cards[i].Margin = new Padding(0, 0, right, bottom);
            cards[i].MinimumSize = new Size(mode == DashboardResponsiveMode.Narrow ? 220 : 0, 116);
            dashboardCards.Controls.Add(cards[i], column, row);
        }
    }

    private void ConfigureDashboardLowerLayout(DashboardResponsiveMode mode)
    {
        dashboardLowerPanel.Padding = mode switch
        {
            DashboardResponsiveMode.Wide => new Padding(12, 8, 12, 12),
            DashboardResponsiveMode.Medium => new Padding(12, 8, 12, 12),
            _ => new Padding(12, 8, 12, 12)
        };
        dashboardLowerPanel.AutoScroll = true;
        dashboardLowerTable.Dock = DockStyle.Top;
        dashboardFeaturesPanel.Dock = DockStyle.Top;

        dashboardLowerTable.Controls.Clear();
        dashboardLowerTable.ColumnStyles.Clear();
        dashboardLowerTable.RowStyles.Clear();

        if (mode == DashboardResponsiveMode.Wide)
        {
            dashboardLowerTable.ColumnCount = 2;
            dashboardLowerTable.RowCount = 1;
            dashboardLowerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            dashboardLowerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            dashboardLowerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            dashboardLowerTable.Height = 289;
            dashboardFeaturesPanel.Height = 368;

            officialsPanel.Margin = new Padding(0, 0, 8, 0);
            dashboardTrendsPanel.Margin = new Padding(3, 4, 3, 4);
            dashboardLowerTable.Controls.Add(officialsPanel, 0, 0);
            dashboardLowerTable.Controls.Add(dashboardTrendsPanel, 1, 0);
            ConfigureTrendPanelsLayout(DashboardResponsiveMode.Wide);
            EnsureDashboardLowerDockOrder();
            return;
        }

        dashboardLowerTable.ColumnCount = 1;
        dashboardLowerTable.RowCount = 2;
        dashboardLowerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        dashboardLowerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        dashboardLowerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        dashboardLowerTable.Height = mode == DashboardResponsiveMode.Medium ? 522 : 612;
        dashboardFeaturesPanel.Height = mode == DashboardResponsiveMode.Medium ? 444 : 598;

        officialsPanel.Margin = new Padding(0, 0, 0, 12);
        dashboardTrendsPanel.Margin = new Padding(0);
        dashboardLowerTable.Controls.Add(officialsPanel, 0, 0);
        dashboardLowerTable.Controls.Add(dashboardTrendsPanel, 0, 1);

        ConfigureTrendPanelsLayout(mode);
        EnsureDashboardLowerDockOrder();
    }

    private void EnsureDashboardLowerDockOrder()
    {
        dashboardLowerPanel.Controls.Remove(dashboardFeaturesPanel);
        dashboardLowerPanel.Controls.Remove(dashboardLowerTable);
        dashboardLowerPanel.Controls.Add(dashboardFeaturesPanel);
        dashboardLowerPanel.Controls.Add(dashboardLowerTable);
    }

    private void ConfigureTrendPanelsLayout(DashboardResponsiveMode mode)
    {
        bool isNarrow = mode == DashboardResponsiveMode.Narrow;
        int columns = isNarrow ? 1 : 3;
        int rows = isNarrow ? 3 : 1;
        var panels = new Control[] { certTrendPanel, blotterTrendPanel, residentsTrendPanel };

        RebuildTableLayout(trendsTable, columns, rows);
        trendsTable.Controls.Clear();

        for (int i = 0; i < panels.Length; i++)
        {
            int column = isNarrow ? 0 : i;
            int row = isNarrow ? i : 0;
            int right = !isNarrow && column < columns - 1 ? 8 : 0;
            int bottom = isNarrow && row < rows - 1 ? 8 : 0;
            panels[i].Margin = new Padding(0, 0, right, bottom);
            panels[i].MinimumSize = new Size(0, 182);
            trendsTable.Controls.Add(panels[i], column, row);
        }
    }

    private void ConfigureDashboardFeaturesLayout(DashboardResponsiveMode mode)
    {
        dashboardFeaturesTable.Controls.Clear();
        dashboardFeaturesTable.ColumnStyles.Clear();
        dashboardFeaturesTable.RowStyles.Clear();
        dashboardFeaturesTable.SetColumnSpan(actionCenterPanel, 1);

        if (mode == DashboardResponsiveMode.Wide)
        {
            dashboardFeaturesTable.ColumnCount = 3;
            dashboardFeaturesTable.RowCount = 1;
            dashboardFeaturesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));
            dashboardFeaturesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));
            dashboardFeaturesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
            dashboardFeaturesTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            announcementsPanel.Margin = new Padding(0, 0, 8, 0);
            projectsPanel.Margin = new Padding(8, 0, 8, 0);
            actionCenterPanel.Margin = new Padding(8, 0, 0, 0);
            dashboardFeaturesTable.Controls.Add(announcementsPanel, 0, 0);
            dashboardFeaturesTable.Controls.Add(projectsPanel, 1, 0);
            dashboardFeaturesTable.Controls.Add(actionCenterPanel, 2, 0);
            return;
        }

        if (mode == DashboardResponsiveMode.Medium)
        {
            dashboardFeaturesTable.ColumnCount = 2;
            dashboardFeaturesTable.RowCount = 2;
            dashboardFeaturesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            dashboardFeaturesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            dashboardFeaturesTable.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            dashboardFeaturesTable.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

            announcementsPanel.Margin = new Padding(0, 0, 8, 8);
            projectsPanel.Margin = new Padding(8, 0, 0, 8);
            actionCenterPanel.Margin = new Padding(0, 8, 0, 0);

            dashboardFeaturesTable.Controls.Add(announcementsPanel, 0, 0);
            dashboardFeaturesTable.Controls.Add(projectsPanel, 1, 0);
            dashboardFeaturesTable.Controls.Add(actionCenterPanel, 0, 1);
            dashboardFeaturesTable.SetColumnSpan(actionCenterPanel, 2);
            return;
        }

        dashboardFeaturesTable.ColumnCount = 1;
        dashboardFeaturesTable.RowCount = 3;
        dashboardFeaturesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        dashboardFeaturesTable.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        dashboardFeaturesTable.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        dashboardFeaturesTable.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

        announcementsPanel.Margin = new Padding(0, 0, 0, 8);
        projectsPanel.Margin = new Padding(0, 0, 0, 8);
        actionCenterPanel.Margin = new Padding(0);

        dashboardFeaturesTable.Controls.Add(announcementsPanel, 0, 0);
        dashboardFeaturesTable.Controls.Add(projectsPanel, 0, 1);
        dashboardFeaturesTable.Controls.Add(actionCenterPanel, 0, 2);
    }

    private static void RebuildTableLayout(TableLayoutPanel table, int columns, int rows)
    {
        table.ColumnCount = Math.Max(1, columns);
        table.RowCount = Math.Max(1, rows);
        table.ColumnStyles.Clear();
        table.RowStyles.Clear();

        float columnPercent = 100f / table.ColumnCount;
        for (int i = 0; i < table.ColumnCount; i++)
        {
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, columnPercent));
        }

        float rowPercent = 100f / table.RowCount;
        for (int i = 0; i < table.RowCount; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
        }
    }

    internal void SetAnnouncements(DataTable table)
    {
        bool canCreateAnnouncements = Permissions.CanManageAnnouncements;
        RenderCards(
            announcementsCards,
            table,
            BuildAnnouncementCard,
            "No announcements yet",
            canCreateAnnouncements
                ? "Post your first advisory so residents and staff can immediately see updates here."
                : "No published advisories are available right now. Refresh this panel to check for updates.",
            canCreateAnnouncements ? "Create announcement" : "Refresh",
            canCreateAnnouncements ? () => announcementsNew.PerformClick() : () => announcementsRefresh.PerformClick(),
            canCreateAnnouncements ? "Refresh" : null,
            canCreateAnnouncements ? () => announcementsRefresh.PerformClick() : null,
            IconChar.Message);
    }

    internal void SetProjects(DataTable table)
    {
        bool canCreateProjects = Permissions.CanManageProjects;
        RenderCards(
            projectsCards,
            table,
            BuildProjectCard,
            "No projects yet",
            canCreateProjects
                ? "Track barangay initiatives here. Start by creating the first project with a lead and schedule."
                : "No projects are listed yet. Refresh this panel to check if new projects were added.",
            canCreateProjects ? "Create project" : "Refresh",
            canCreateProjects ? () => projectsNew.PerformClick() : () => projectsRefresh.PerformClick(),
            canCreateProjects ? "Refresh" : null,
            canCreateProjects ? () => projectsRefresh.PerformClick() : null,
            IconChar.FolderOpen);
    }

    internal void SetNotifications(DataTable? table)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetNotifications(table)));
            return;
        }

        int count = table?.Rows.Count ?? 0;
        notificationTitle.Text = count > 0 ? $"Notifications ({count})" : "Notifications";
        notificationViewAll.Enabled = count > 0;

        notificationList.SuspendLayout();
        notificationList.Controls.Clear();

        if (count <= 0)
        {
            RenderNotificationEmptyState();
            notificationList.ResumeLayout();
            return;
        }

        if (table == null)
        {
            RenderNotificationEmptyState();
            notificationList.ResumeLayout();
            return;
        }

        foreach (DataRow row in table.Rows)
        {
            notificationList.Controls.Add(BuildNotificationCard(row));
        }

        UpdateNotificationCardWidths();
        notificationList.ResumeLayout();
    }

    internal void ShowAnnouncementsLoading()
    {
        RenderLoadingCards(announcementsCards, "Loading announcements...");
    }

    internal void ShowProjectsLoading()
    {
        RenderLoadingCards(projectsCards, "Loading projects...");
    }

    internal void SetActionCenter(DataTable table)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetActionCenter(table)));
            return;
        }

        _actionCenterTable = table?.Copy();
        _selectedActionTarget = ResolveActionTargetFromTable(_actionCenterTable);
        actionCenterHeader.Text = "Action Calendar";

        int alertCount = _actionCenterTable?.Rows.Count ?? 0;
        if (alertCount > 0)
        {
            actionCenterHeader.Text = $"Action Calendar  ({alertCount})";
        }
    }

    internal bool TryGetSelectedActionTarget(out string targetView)
    {
        targetView = _selectedActionTarget;
        if (string.IsNullOrWhiteSpace(targetView))
        {
            targetView = ResolveActionTargetFromTable(_actionCenterTable);
            _selectedActionTarget = targetView;
        }

        return !string.IsNullOrWhiteSpace(targetView);
    }

    private static string ResolveActionTargetFromTable(DataTable? table)
    {
        if (table == null || table.Rows.Count == 0 || !table.Columns.Contains("target_view"))
        {
            return string.Empty;
        }

        foreach (DataRow row in table.Rows)
        {
            var target = Convert.ToString(row["target_view"]) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(target))
            {
                return target;
            }
        }

        return string.Empty;
    }

    private void WireSidebar()
    {
        StyleSidebarButton(btnUsers, IconChar.BarChart, "Dashboard");
        StyleSidebarButton(iconButton3, IconChar.ClockRotateLeft, "History");
        StyleSidebarButton(iconButton5, IconChar.User, "Profile");
        StyleSidebarButton(iconButton2, IconChar.FileAlt, "Blotter");
        StyleSidebarButton(iconButton4, IconChar.FileSignature, "Certificates");
        StyleSidebarButton(_sidebarReports, IconChar.BarChart, "Reports");
        StyleSidebarButton(_sidebarSettings, IconChar.Gear, "Settings");

        iconButton3.Click += SidebarHistory_Click;
        iconButton5.Click += SidebarProfile_Click;
        iconButton2.Click += SidebarBlotter_Click;
        iconButton4.Click += SidebarCertificates_Click;
        _sidebarReports.Click += SidebarReports_Click;
        _sidebarSettings.Click += SidebarSettings_Click;
    }

    private void ApplyRolePermissions()
    {
        bool canManageUsers = Permissions.CanManageUsers;
        bool canOpenSettings = Permissions.CanOpenSettings;
        bool canManageAnnouncements = Permissions.CanManageAnnouncements;
        bool canManageProjects = Permissions.CanManageProjects;
        bool canRequestCertificates = Permissions.CanRequestCertificates;
        bool canCreateBlotter = Permissions.CanCreateBlotter;
        bool canCreateResidents = Permissions.CanCreateResidents;

        _sidebarSettings.Enabled = canOpenSettings;
        announcementsNew.Enabled = canManageAnnouncements;
        projectsNew.Enabled = canManageProjects;
        _quickAddAnnouncementButton.Enabled = canManageAnnouncements;
        _quickAddCertificateButton.Enabled = canRequestCertificates;
        _quickAddBlotterButton.Enabled = canCreateBlotter;
        _quickAddResidentButton.Enabled = canCreateResidents;

        foreach (var card in _officialCards)
        {
            card.UpdateButton.Enabled = canManageUsers;
        }

        officialsViewAll.Enabled = canManageUsers;
    }

    private void ConfigureEllieAssistant()
    {
        _ellieOpenItem.Click -= EllieOpenItem_Click;
        _ellieOpenItem.Click += EllieOpenItem_Click;
        _ellieMenu.Items.Clear();
        _ellieMenu.Items.Add(_ellieOpenItem);

        _ellieButton.Click -= EllieButton_Click;
        _ellieButton.Click += EllieButton_Click;
    }

    private void ConfigureGlobalSearch()
    {
        _globalSearchButton.Name = "btnGlobalSearch";
        _globalSearchButton.Text = string.Empty;
        _globalSearchButton.BackColor = Color.Transparent;
        _globalSearchButton.Dock = DockStyle.None;
        _globalSearchButton.FlatStyle = FlatStyle.Flat;
        _globalSearchButton.FlatAppearance.BorderSize = 0;
        _globalSearchButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(236, 245, 255);
        _globalSearchButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(246, 250, 255);
        _globalSearchButton.IconChar = IconChar.MagnifyingGlass;
        _globalSearchButton.IconFont = IconFont.Auto;
        _globalSearchButton.IconSize = 22;
        _globalSearchButton.IconColor = UiTheme.Slate600;
        _globalSearchButton.Cursor = Cursors.Hand;
        _globalSearchButton.Size = new Size(34, 34);
        _globalSearchButton.TabStop = false;

        int topOffset = Math.Max(4, ((_ribbonHeader?.Height ?? panelTop.Height) - _globalSearchButton.Height) / 2);

        if (_ribbonHeaderRight != null)
        {
            _globalSearchButton.Margin = new Padding(0, topOffset, 10, 0);

            if (_globalSearchButton.Parent != _ribbonHeaderRight)
            {
                _globalSearchButton.Parent?.Controls.Remove(_globalSearchButton);
                _ribbonHeaderRight.Controls.Add(_globalSearchButton);
            }

            int panel2Index = _ribbonHeaderRight.Controls.IndexOf(panel2);
            if (panel2Index >= 0)
            {
                _ribbonHeaderRight.Controls.SetChildIndex(_globalSearchButton, panel2Index);
            }
        }
        else if (panel2 != null)
        {
            _globalSearchButton.Dock = DockStyle.Right;
            _globalSearchButton.Width = 73;
            _globalSearchButton.Margin = new Padding(0);

            if (!panel2.Controls.Contains(_globalSearchButton))
            {
                panel2.Controls.Add(_globalSearchButton);
                // Keep it left of the other dock-right icons.
                _globalSearchButton.SendToBack();
            }
        }

        _globalSearchButton.Click -= GlobalSearchButton_Click;
        _globalSearchButton.Click += GlobalSearchButton_Click;
    }

    private void ConfigureBackupMonitor()
    {
        if (panelTop == null)
        {
            return;
        }

        _backupStatusPanel.Name = "backupStatusPanel";
        _backupStatusPanel.AutoSize = true;
        _backupStatusPanel.BackColor = Color.Transparent;
        _backupStatusPanel.FlowDirection = FlowDirection.LeftToRight;
        _backupStatusPanel.WrapContents = false;

        _schemaVersionLabel.AutoSize = true;
        _schemaVersionLabel.Font = UiTheme.SmallFont;
        _schemaVersionLabel.ForeColor = UiTheme.Slate500;
        _schemaVersionLabel.Text = "Version v--";
        _schemaVersionLabel.Margin = new Padding(0, 3, 12, 0);

        _backupStatusDot.Size = new Size(10, 10);
        _backupStatusDot.BackColor = UiTheme.Slate300;
        _backupStatusDot.Margin = new Padding(0, 6, 8, 0);

        _backupStatusLabel.AutoSize = true;
        _backupStatusLabel.Font = UiTheme.SmallFont;
        _backupStatusLabel.ForeColor = UiTheme.Slate600;
        _backupStatusLabel.Text = "Backup: --";
        _backupStatusLabel.Margin = new Padding(0, 3, 0, 0);

        _backupModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _backupModeCombo.FlatStyle = FlatStyle.Flat;
        _backupModeCombo.Font = UiTheme.SmallFont;
        _backupModeCombo.Width = 118;
        _backupModeCombo.Margin = new Padding(8, 2, 0, 0);
        _backupModeCombo.BackColor = Color.White;
        _backupModeCombo.ForeColor = UiTheme.Slate700;
        if (_backupModeCombo.Items.Count == 0)
        {
            _backupModeCombo.Items.AddRange(new object[]
            {
                "Full",
                "Incremental",
                "Differential"
            });
        }
        if (_backupModeCombo.SelectedIndex < 0)
        {
            _backupModeCombo.SelectedIndex = 0;
        }

        ConfigureTopIconButton(_backupNowButton, IconChar.Database, 18);
        ConfigureTopIconButton(_backupOpenFolderButton, IconChar.FolderOpen, 18);

        _backupNowButton.Text = "Backup now";
        _backupNowButton.AutoSize = true;
        _backupNowButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _backupNowButton.Padding = new Padding(8, 0, 10, 0);
        _backupNowButton.TextImageRelation = TextImageRelation.ImageBeforeText;
        _backupNowButton.ImageAlign = ContentAlignment.MiddleLeft;
        _backupNowButton.TextAlign = ContentAlignment.MiddleCenter;
        _backupNowButton.BackColor = Color.White;
        _backupNowButton.ForeColor = UiTheme.Slate700;
        _backupNowButton.FlatAppearance.BorderSize = 1;
        _backupNowButton.FlatAppearance.BorderColor = Color.FromArgb(215, 223, 233);
        _backupNowButton.TabStop = true;

        _backupNowButton.Margin = new Padding(8, 0, 0, 0);
        _backupOpenFolderButton.Margin = new Padding(2, 0, 0, 0);

        _backupToolTip.SetToolTip(_backupStatusLabel, "Shows the latest backup status.");
        _backupToolTip.SetToolTip(_schemaVersionLabel, "Version information");
        _backupToolTip.SetToolTip(_backupModeCombo, "Choose backup mode: Full, Incremental, or Differential.");
        _backupToolTip.SetToolTip(_backupNowButton, "Backup now");
        _backupToolTip.SetToolTip(_backupOpenFolderButton, "Open backups folder");

        _backupNowButton.Click -= BackupNowButton_Click;
        _backupNowButton.Click += BackupNowButton_Click;
        _backupOpenFolderButton.Click -= BackupOpenFolderButton_Click;
        _backupOpenFolderButton.Click += BackupOpenFolderButton_Click;

        if (_backupStatusPanel.Controls.Count == 0)
        {
            _backupStatusPanel.Controls.Add(_schemaVersionLabel);
            _backupStatusPanel.Controls.Add(_backupStatusDot);
            _backupStatusPanel.Controls.Add(_backupStatusLabel);
            _backupStatusPanel.Controls.Add(_backupModeCombo);
            _backupStatusPanel.Controls.Add(_backupNowButton);
            _backupStatusPanel.Controls.Add(_backupOpenFolderButton);
        }

        if (_ribbonHeaderRight != null)
        {
            int topOffset = Math.Max(4, ((_ribbonHeader?.Height ?? 52) - 34) / 2);
            _backupStatusPanel.Dock = DockStyle.None;
            _backupStatusPanel.Margin = new Padding(0, topOffset, 10, 0);
            _backupStatusPanel.Padding = new Padding(0);

            if (_backupStatusPanel.Parent != _ribbonHeaderRight)
            {
                _backupStatusPanel.Parent?.Controls.Remove(_backupStatusPanel);
                _ribbonHeaderRight.Controls.Add(_backupStatusPanel);
            }

            int panel2Index = _ribbonHeaderRight.Controls.IndexOf(panel2);
            if (panel2Index >= 0)
            {
                _ribbonHeaderRight.Controls.SetChildIndex(_backupStatusPanel, panel2Index);
            }
        }
        else
        {
            _backupStatusPanel.Dock = DockStyle.Right;
            _backupStatusPanel.Margin = new Padding(0);
            _backupStatusPanel.Padding = new Padding(0, 26, 10, 0);

            if (_backupStatusPanel.Parent != panelTop)
            {
                _backupStatusPanel.Parent?.Controls.Remove(_backupStatusPanel);
                panelTop.Controls.Add(_backupStatusPanel);
            }
        }
    }
 
     private static void ConfigureTopIconButton(IconButton button, IconChar icon, int iconSize)
     {
         button.Text = string.Empty;
         button.BackColor = Color.Transparent;
         button.FlatStyle = FlatStyle.Flat;
         button.FlatAppearance.BorderSize = 0;
         button.FlatAppearance.MouseDownBackColor = Color.FromArgb(236, 245, 255);
         button.FlatAppearance.MouseOverBackColor = Color.FromArgb(246, 250, 255);
         button.IconChar = icon;
         button.IconFont = IconFont.Auto;
         button.IconSize = iconSize;
         button.IconColor = UiTheme.Slate600;
         button.Cursor = Cursors.Hand;
         button.Size = new Size(34, 34);
         button.TabStop = false;
     }
 
     private void BackupNowButton_Click(object? sender, EventArgs e)
     {
         _controller.RunBackupNow(GetSelectedBackupMode());
     }

    internal BackupMode GetSelectedBackupMode()
    {
        return _backupModeCombo.SelectedItem?.ToString() switch
        {
            "Incremental" => BackupMode.Incremental,
            "Differential" => BackupMode.Differential,
            _ => BackupMode.Full
        };
    }
 
     private void BackupOpenFolderButton_Click(object? sender, EventArgs e)
     {
         try
         {
             BackupService.OpenBackupFolder();
         }
         catch (Exception ex)
         {
             ControllerDialogs.Warning("Unable to open backups folder.", ex: ex);
         }
     }

    private void GlobalSearchButton_Click(object? sender, EventArgs e)
    {
        ShowGlobalSearchDialog();
    }

    private void ShowGlobalSearchDialog()
    {
        using var form = new GlobalSearchForm();
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (form.SelectedResult == null)
        {
            return;
        }

        OpenGlobalSearchResult(form.SelectedResult);
    }

    private void OpenGlobalSearchResult(GlobalSearchResult result)
    {
        switch (result.EntityType)
        {
            case GlobalSearchEntityType.Resident:
            {
                var residents = EnsureResidentsWorkspace(ResidentsView.Profile);
                residents.NavigateToResident(result.Id, ResidentsView.Profile);
                break;
            }
            case GlobalSearchEntityType.Certificate:
            {
                if (!result.ResidentId.HasValue)
                {
                    ControllerDialogs.Warning("Unable to open certificate (missing resident reference).");
                    return;
                }

                var residents = EnsureResidentsWorkspace(ResidentsView.Certificates);
                residents.NavigateToResident(result.ResidentId.Value, ResidentsView.Certificates, certificateId: result.Id);
                break;
            }
            case GlobalSearchEntityType.Blotter:
            {
                if (!result.ResidentId.HasValue)
                {
                    ControllerDialogs.Warning("Unable to open blotter (missing resident reference).");
                    return;
                }

                var residents = EnsureResidentsWorkspace(ResidentsView.Blotter);
                residents.NavigateToResident(result.ResidentId.Value, ResidentsView.Blotter, blotterId: result.Id);
                break;
            }
            case GlobalSearchEntityType.User:
            {
                if (!Permissions.CanManageUsers)
                {
                    ControllerDialogs.Warning("Only Admin users can manage user accounts.");
                    return;
                }

                using var userForm = new UpdateUserForm(result.Id);
                userForm.ShowDialog(this);
                _controller.LoadDashboardStats();
                break;
            }
        }
    }

    private Residents EnsureResidentsWorkspace(ResidentsView view, CertificateAction certificateAction = CertificateAction.None)
    {
        Residents OpenWorkspace(Residents workspace)
        {
            ShowWorkspaceForm(workspace, "Residents", ResidentsSubtitle(view));
            workspace.ShowView(view);
            if (certificateAction != CertificateAction.None)
            {
                workspace.ExecuteCertificateAction(certificateAction);
            }

            SetHeader("Residents", ResidentsSubtitle(view));
            return workspace;
        }

        if (_residentsWorkspaceForm == null || _residentsWorkspaceForm.IsDisposed)
        {
            _residentsWorkspaceForm = new Residents();
            _residentsWorkspaceForm.ConfigureForEmbeddedNavigation();
        }

        try
        {
            return OpenWorkspace(_residentsWorkspaceForm);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Unable to open Residents workspace.", ex);
            try
            {
                if (_residentsWorkspaceForm != null && !_residentsWorkspaceForm.IsDisposed)
                {
                    _residentsWorkspaceForm.Dispose();
                }
            }
            catch
            {
                // Best effort reset only.
            }

            _residentsWorkspaceForm = new Residents();
            _residentsWorkspaceForm.ConfigureForEmbeddedNavigation();

            try
            {
                return OpenWorkspace(_residentsWorkspaceForm);
            }
            catch (Exception retryEx)
            {
                AppLogger.LogError("Retry failed while opening Residents workspace.", retryEx);
                ControllerDialogs.Error(retryEx, "Unable to open Residents.");
                throw;
            }
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.K:
                ShowGlobalSearchDialog();
                return true;
            case Keys.Control | Keys.D1:
                ShowDashboard();
                return true;
            case Keys.Control | Keys.D2:
                OpenResidents(ResidentsView.Profile);
                return true;
            case Keys.Control | Keys.D3:
                OpenResidents(ResidentsView.Certificates);
                return true;
            case Keys.Control | Keys.D4:
                OpenResidents(ResidentsView.Blotter);
                return true;
            case Keys.Control | Keys.D5:
                OpenReports();
                return true;
            case Keys.Control | Keys.D6:
                SidebarSettings_Click(null, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.Shift | Keys.R:
                TryOpenResidentShortcut();
                return true;
            case Keys.Control | Keys.Shift | Keys.C:
                TryOpenCertificateShortcut();
                return true;
            case Keys.Control | Keys.Shift | Keys.B:
                TryOpenBlotterShortcut();
                return true;
            case Keys.F5:
                _controller.LoadDashboardStats();
                return true;
            case Keys.Control | Keys.OemQuestion:
            case Keys.Control | Keys.Shift | Keys.OemQuestion:
                ShowKeyboardShortcutsHelp();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        CloseActiveWorkspaceForm();
        if (_residentsWorkspaceForm != null)
        {
            _residentsWorkspaceForm.FormClosed -= HostedWorkspaceForm_Closed;
            if (!_residentsWorkspaceForm.IsDisposed)
            {
                _residentsWorkspaceForm.Dispose();
            }

            _residentsWorkspaceForm = null;
        }

        base.OnFormClosed(e);
    }

    private void TryOpenResidentShortcut()
    {
        if (!Permissions.CanCreateResidents)
        {
            ControllerDialogs.Warning("You do not have permission to add residents.");
            return;
        }

        OpenResidents(ResidentsView.Profile);
    }

    private void TryOpenCertificateShortcut()
    {
        if (!Permissions.CanRequestCertificates)
        {
            ControllerDialogs.Warning("You do not have permission to create certificate requests.");
            return;
        }

        OpenResidents(ResidentsView.Certificates, CertificateAction.NewRequest);
    }

    private void TryOpenBlotterShortcut()
    {
        if (!Permissions.CanCreateBlotter)
        {
            ControllerDialogs.Warning("You do not have permission to create blotter records.");
            return;
        }

        OpenResidents(ResidentsView.Blotter);
    }

    private void ShowKeyboardShortcutsHelp()
    {
        ControllerDialogs.Info(
            "Keyboard Shortcuts\n\n" +
            "Ctrl+K  - Global Search\n" +
            "Ctrl+1  - Dashboard\n" +
            "Ctrl+2  - Residents (Profile)\n" +
            "Ctrl+3  - Certificates\n" +
            "Ctrl+4  - Blotter\n" +
            "Ctrl+5  - Reports\n" +
            "Ctrl+6  - Settings\n" +
            "Ctrl+Shift+R - New Resident\n" +
            "Ctrl+Shift+C - New Certificate Request\n" +
            "Ctrl+Shift+B - New Blotter\n" +
            "F5 - Refresh Dashboard Data\n" +
            "Ctrl+/ - Show this help",
            "Keyboard Shortcuts");
    }

    private void ConfigureNotifications()
    {
        _notificationTargetHeight = notificationPanel.Height;
        notificationPanel.Height = 0;
        notificationPanel.Visible = false;
        notificationPanel.BringToFront();
        notificationPanel.BackColor = Color.White;
        notificationHeaderPanel.BackColor = Color.White;
        notificationTitle.Font = new Font(UiTheme.LabelFont, FontStyle.Bold);
        notificationTitle.ForeColor = UiTheme.Slate900;
        notificationEmptyLabel.Font = UiTheme.LabelFont;
        notificationEmptyLabel.ForeColor = UiTheme.Slate600;
        UiTheme.StyleSecondaryButton(notificationViewAll);
        notificationViewAll.Text = "Mark all read";
        notificationViewAll.Width = 110;
        notificationList.Resize -= NotificationList_Resize;
        notificationList.Resize += NotificationList_Resize;

        _notificationTimer.Interval = 16;
        _notificationTimer.Tick -= NotificationTimer_Tick;
        _notificationTimer.Tick += NotificationTimer_Tick;

        Resize -= AdminDashboard_Resize;
        Resize += AdminDashboard_Resize;
        PositionNotificationPanel();
    }

    private void ConfigureScheduler()
    {
        // Lightweight recurring refresh to keep reminders/notifications up-to-date without manual reloads.
        _schedulerTimer.Interval = 30_000; // 30s tick; actual work is rate-limited below.
        _schedulerTimer.Tick -= SchedulerTimer_Tick;
        _schedulerTimer.Tick += SchedulerTimer_Tick;
        _schedulerTimer.Start();

        FormClosed -= AdminDashboard_FormClosed;
        FormClosed += AdminDashboard_FormClosed;
    }

    private void SchedulerTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            DateTime now = DateTime.Now;

            if (now - _lastNotificationsRefreshAt >= TimeSpan.FromMinutes(2))
            {
                _lastNotificationsRefreshAt = now;
                _controller.RefreshNotifications();
            }

            if (now - _lastNotificationAutomationAt >= TimeSpan.FromMinutes(2))
            {
                _lastNotificationAutomationAt = now;
                _controller.RunNotificationAutomation(includeReminderQueue: false);
            }

            if (now - _lastActionCenterRefreshAt >= TimeSpan.FromMinutes(5))
            {
                _lastActionCenterRefreshAt = now;
                _controller.RefreshActionCenter();
            }

            if (now - _lastBackupStatusRefreshAt >= TimeSpan.FromMinutes(10))
            {
                _lastBackupStatusRefreshAt = now;
                _controller.RefreshBackupStatus();
            }

            // Daily check window (first tick after 7:00 AM) to ensure reminders refresh at least once per day.
            if (_lastDailyChecksRunDate.Date != now.Date && now.TimeOfDay >= new TimeSpan(7, 0, 0))
            {
                _lastDailyChecksRunDate = now.Date;
                _controller.RefreshActionCenter();
                _controller.RefreshNotifications();
                _controller.RunNotificationAutomation(includeReminderQueue: true);
                _controller.TryRunScheduledBackup();
            }
        }
        catch
        {
            // Never crash the UI due to background refresh failures.
        }
    }

    private void AdminDashboard_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _schedulerTimer.Stop();
    }

    private void EllieButton_Click(object? sender, EventArgs e)
    {
        _ellieMenu.Show(_ellieButton, new Point(0, _ellieButton.Height));
    }

    private void EllieOpenItem_Click(object? sender, EventArgs e)
    {
        _controller.HandleOpenEllieAssistant();
    }

    private void ConfigureDynamicSidebar()
    {
        _sidebarController?.Dispose();
        _sidebarController = null;

        // Sidebar navigation is fully disabled. Use top/ribbon navigation only.
        panelSidebar.Visible = false;
        panelSidebar.Width = 0;
        panelSidebar.Padding = Padding.Empty;

        foreach (var button in GetSidebarButtons())
        {
            button.Visible = false;
            button.Enabled = false;
        }

        if (_sidebarToggleButton != null)
        {
            _sidebarToggleButton.Visible = false;
            _sidebarToggleButton.Enabled = false;
        }
    }

    private IconButton? FindSidebarToggleButton()
    {
        var matches = Controls.Find("sidebarToggleButton", true);
        return matches.FirstOrDefault() as IconButton;
    }

    private IconButton? CreateSidebarToggleButton()
    {
        if (panelTop == null)
        {
            return null;
        }

        var button = new IconButton
        {
            Name = "sidebarToggleButton",
            Size = new Size(36, 36),
            Location = new Point(12, 18),
            IconChar = IconChar.Bars,
            IconFont = IconFont.Auto,
            IconSize = 18,
            Text = string.Empty,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Cursor = Cursors.Hand,
            TabStop = false,
            Visible = false
        };
        button.FlatAppearance.BorderSize = 0;
        panelTop.Controls.Add(button);
        button.BringToFront();
        return button;
    }

    private IconButton[] GetSidebarButtons()
    {
        return new[] { btnUsers, iconButton3, iconButton5, iconButton2, iconButton4, _sidebarReports, _sidebarSettings };
    }

    private void InitializeWorkspaceLayout()
    {
        _contentSubtitlePanel.Name = "contentSubtitlePanel";
        _contentSubtitlePanel.Dock = DockStyle.Top;
        _contentSubtitlePanel.Height = 28;
        _contentSubtitlePanel.Padding = new Padding(32, 2, 32, 0);

        _contentSubtitleLabel.Name = "contentSubtitleLabel";
        _contentSubtitleLabel.Dock = DockStyle.Fill;
        _contentSubtitleLabel.AutoEllipsis = true;
        _contentSubtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _contentSubtitlePanel.Controls.Add(_contentSubtitleLabel);

        _contentBodyHostPanel.Name = "contentBodyHostPanel";
        _contentBodyHostPanel.Dock = DockStyle.Fill;

        _dashboardHostPanel.Name = "dashboardHostPanel";
        _dashboardHostPanel.Dock = DockStyle.Fill;
        _dashboardHostPanel.BackColor = panel1.BackColor;

        _workspaceHostPanel.Name = "workspaceHostPanel";
        _workspaceHostPanel.Dock = DockStyle.Fill;
        _workspaceHostPanel.BackColor = panel1.BackColor;
        _workspaceHostPanel.Visible = false;

        panel1.SuspendLayout();
        panel1.Controls.Remove(dashboardLowerPanel);
        panel1.Controls.Remove(dashboardPanel);

        _dashboardHostPanel.Controls.Add(dashboardLowerPanel);
        _dashboardHostPanel.Controls.Add(dashboardPanel);
        _contentBodyHostPanel.Controls.Add(_workspaceHostPanel);
        _contentBodyHostPanel.Controls.Add(_dashboardHostPanel);

        panel1.Controls.Clear();
        panel1.Controls.Add(_contentBodyHostPanel);
        panel1.Controls.Add(_contentSubtitlePanel);
        _contentSubtitlePanel.BringToFront();
        panel1.ResumeLayout();
    }

    private static void ConfigureHostedForm(Form form)
    {
        form.TopLevel = false;
        form.TopMost = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.ControlBox = false;
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.WindowState = FormWindowState.Normal;
        form.Dock = DockStyle.Fill;
    }

    private void ShowWorkspaceForm(Form form, string title, string subtitle)
    {
        bool sameWorkspace = ReferenceEquals(_activeWorkspaceForm, form);
        if (!sameWorkspace)
        {
            CloseActiveWorkspaceForm();
        }

        ConfigureHostedForm(form);

        _activeWorkspaceForm = form;
        _activeWorkspaceForm.FormClosed -= HostedWorkspaceForm_Closed;
        _activeWorkspaceForm.FormClosed += HostedWorkspaceForm_Closed;

        _workspaceHostPanel.SuspendLayout();
        try
        {
            ClearAndDisposeChildControls(_workspaceHostPanel, control => ReferenceEquals(control, _activeWorkspaceForm));
            if (_activeWorkspaceForm.Parent != null && !ReferenceEquals(_activeWorkspaceForm.Parent, _workspaceHostPanel))
            {
                _activeWorkspaceForm.Parent.Controls.Remove(_activeWorkspaceForm);
            }
            _workspaceHostPanel.Controls.Add(_activeWorkspaceForm);
            _activeWorkspaceForm.Dock = DockStyle.Fill;
        }
        finally
        {
            _workspaceHostPanel.ResumeLayout(performLayout: true);
        }

        _dashboardHostPanel.Visible = false;
        _workspaceHostPanel.Visible = true;
        SetHeader(title, subtitle);
        if (!_activeWorkspaceForm.Visible)
        {
            _activeWorkspaceForm.Show();
        }
        else
        {
            _activeWorkspaceForm.BringToFront();
        }
    }

    private void CloseActiveWorkspaceForm()
    {
        if (_activeWorkspaceForm == null)
        {
            return;
        }

        var form = _activeWorkspaceForm;
        _activeWorkspaceForm = null;
        form.FormClosed -= HostedWorkspaceForm_Closed;

        if (_residentsWorkspaceForm != null && ReferenceEquals(form, _residentsWorkspaceForm))
        {
            form.Hide();
            _workspaceHostPanel.SuspendLayout();
            try
            {
                _workspaceHostPanel.Controls.Remove(form);
            }
            finally
            {
                _workspaceHostPanel.ResumeLayout(performLayout: true);
            }
            return;
        }

        if (!form.IsDisposed)
        {
            form.Close();
            if (!form.IsDisposed)
            {
                form.Dispose();
            }
        }
    }

    private void HostedWorkspaceForm_Closed(object? sender, FormClosedEventArgs e)
    {
        if (_residentsWorkspaceForm != null && ReferenceEquals(sender, _residentsWorkspaceForm))
        {
            _residentsWorkspaceForm = null;
        }

        if (!ReferenceEquals(sender, _activeWorkspaceForm))
        {
            return;
        }

        if (_activeWorkspaceForm != null)
        {
            _activeWorkspaceForm.FormClosed -= HostedWorkspaceForm_Closed;
        }
        _activeWorkspaceForm = null;
        ClearAndDisposeChildControls(_workspaceHostPanel);
        ShowDashboard();
    }

    private static void ClearAndDisposeChildControls(Control host, Func<Control, bool>? preserve = null)
    {
        var children = host.Controls.Cast<Control>().ToArray();
        foreach (var child in children)
        {
            host.Controls.Remove(child);
            if (preserve != null && preserve(child))
            {
                continue;
            }

            try
            {
                child.Dispose();
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    private void ShowDashboard()
    {
        SyncRibbonPrimary("Dashboard");
        CloseActiveWorkspaceForm();
        _workspaceHostPanel.Visible = false;
        _dashboardHostPanel.Visible = true;
        SetHeader("Admin Dashboard", "Manage residents and users");
        dashboardPanel.Visible = true;
        dashboardLowerPanel.Visible = true;
        ApplyDashboardResponsiveLayout();
        _controller.LoadDashboardStats();
    }

    private void OpenResidents(ResidentsView view, CertificateAction certificateAction = CertificateAction.None)
    {
        SyncRibbonPrimaryFromResidentsView(view);
        EnsureResidentsWorkspace(view, certificateAction);
    }

    internal void OpenResidentsFromDashboard(ResidentsView view)
    {
        OpenResidents(view);
    }

    internal void OpenUsersListModule()
    {
        SyncRibbonPrimary("Administration");
        ShowWorkspaceForm(new UsersListForm(), "Staff and Admin", "Manage user accounts");
    }

    private void OpenReports()
    {
        SyncRibbonPrimary("Reports");
        ShowWorkspaceForm(new Reports(), "Reports", "View reports and analytics");
    }

    private void OpenHouseholdsModule()
    {
        SyncRibbonPrimary("Community");
        if (!helper.Permissions.CanViewHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to view households.");
            return;
        }

        ShowWorkspaceForm(
            new HouseholdModuleForm(),
            "Households",
            "Household records");
    }

    private void OpenClearancesModule()
    {
        SyncRibbonPrimary("Services");
        ShowWorkspaceForm(
            new ModulePlaceholderForm(
                "Clearances",
                "Barangay clearance module",
                "This module is ready for future implementation."),
            "Clearances",
            "Clearance processing");
    }

    private void OpenPermitsModule()
    {
        SyncRibbonPrimary("Services");
        ShowWorkspaceForm(
            new ModulePlaceholderForm(
                "Permits",
                "Permit processing module",
                "This module is ready for future implementation."),
            "Permits",
            "Permit processing");
    }

    private void OpenPaymentsModule()
    {
        SyncRibbonPrimary("Finance");
        ShowWorkspaceForm(
            new ModulePlaceholderForm(
                "Payments",
                "Payments module",
                "This module is ready for future implementation."),
            "Payments",
            "Payment transactions");
    }

    private void OpenCollectionsModule()
    {
        SyncRibbonPrimary("Finance");
        ShowWorkspaceForm(
            new ModulePlaceholderForm(
                "Collections",
                "Collections module",
                "This module is ready for future implementation."),
            "Collections",
            "Collection tracking");
    }

    private void OpenOfficialsModule()
    {
        SyncRibbonPrimary("Administration");
        ShowWorkspaceForm(
            new ModulePlaceholderForm(
                "Officials",
                "Barangay officials module",
                "This module is ready for future implementation."),
            "Officials",
            "Officials management");
    }

    private void OpenStaffUsersModule()
    {
        SyncRibbonPrimary("Administration");
        ShowWorkspaceForm(
            new ModulePlaceholderForm(
                "Staff / Users",
                "Staff and user accounts module",
                "This module is ready for future implementation."),
            "Staff / Users",
            "Staff and user accounts");
    }

    private static string ResidentsSubtitle(ResidentsView view)
    {
        return view switch
        {
            ResidentsView.Profile => "Profile records",
            ResidentsView.History => "Resident history",
            ResidentsView.Certificates => "Certificate processing",
            ResidentsView.Blotter => "Blotter case tracking",
            _ => "Resident management"
        };
    }

    private void FeatureCards_Resize(object? sender, EventArgs e)
    {
        if (sender is not FlowLayoutPanel host)
        {
            return;
        }

        ResizeCardWidths(host);
    }

    private static void ResizeCardWidths(FlowLayoutPanel host)
    {
        int width = Math.Max(220, host.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        foreach (Control control in host.Controls)
        {
            control.Width = width;
        }
    }

    private void RenderCards(
        FlowLayoutPanel host,
        DataTable table,
        Func<DataRow, Panel> cardBuilder,
        string emptyTitle,
        string emptyMessage,
        string? emptyPrimaryActionText = null,
        Action? emptyPrimaryAction = null,
        string? emptySecondaryActionText = null,
        Action? emptySecondaryAction = null,
        IconChar emptyIcon = IconChar.Message)
    {
        host.SuspendLayout();
        host.Controls.Clear();

        if (table.Rows.Count == 0)
        {
            var emptyCard = BuildEmptyStateCard(
                emptyTitle,
                emptyMessage,
                emptyPrimaryActionText,
                emptyPrimaryAction,
                emptySecondaryActionText,
                emptySecondaryAction,
                emptyIcon);
            host.Controls.Add(emptyCard);
            ResizeCardWidths(host);
            host.ResumeLayout();
            return;
        }

        foreach (DataRow row in table.Rows)
        {
            host.Controls.Add(cardBuilder(row));
        }

        ResizeCardWidths(host);
        host.ResumeLayout();
    }

    private void RenderLoadingCards(FlowLayoutPanel host, string loadingText)
    {
        host.SuspendLayout();
        host.Controls.Clear();

        for (int i = 0; i < 3; i++)
        {
            host.Controls.Add(BuildLoadingCard(i == 0 ? loadingText : "Loading..."));
        }

        ResizeCardWidths(host);
        host.ResumeLayout();
    }

    private Panel BuildLoadingCard(string loadingText)
    {
        var card = CreateCardShell(UiTheme.Slate500);
        card.Height = 92;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = loadingText,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate600
        }, 0, 0);

        var progress = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Height = 10,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 24,
            Margin = new Padding(0, 6, 0, 0)
        };
        layout.Controls.Add(progress, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private void RenderNotificationEmptyState()
    {
        bool canCreateAnnouncements = Permissions.CanManageAnnouncements;
        var emptyCard = BuildEmptyStateCard(
            "No new notifications",
            canCreateAnnouncements
                ? "You're all caught up. Next action: post a new announcement to notify users."
                : "You're all caught up. Next action: click Refresh to check for new announcements.",
            "Refresh",
            () => _controller.RefreshNotifications(),
            canCreateAnnouncements ? "Create announcement" : null,
            canCreateAnnouncements ? () => announcementsNew.PerformClick() : null,
            IconChar.Message);

        emptyCard.Margin = new Padding(0);
        notificationList.Controls.Add(emptyCard);
        UpdateNotificationCardWidths();
    }

    private void UpdateNotificationCardWidths()
    {
        int width = Math.Max(220, notificationList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        foreach (Control control in notificationList.Controls)
        {
            control.Width = width;
        }
    }

    private void NotificationList_Resize(object? sender, EventArgs e)
    {
        UpdateNotificationCardWidths();
    }

    private Panel BuildNotificationCard(DataRow row)
    {
        int announcementId = ReadInt(row, "announcement_id");
        string title = ReadCell(row, "title");
        string priority = ReadCell(row, "priority");
        string published = ReadCell(row, "published");
        Color accent = PriorityColor(priority);

        var card = CreateCardShell(accent);
        card.Height = 88;
        card.Padding = new Padding(10, 8, 10, 8);

        var headerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 26,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.Controls.Add(new Label
        {
            Text = title,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(UiTheme.BodyFont, FontStyle.Bold),
            ForeColor = UiTheme.Slate900
        }, 0, 0);
        headerRow.Controls.Add(CreateChip(priority, accent), 1, 0);

        var footerRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0)
        };
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerRow.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Published " + published,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate600,
            Margin = new Padding(0, 4, 0, 0)
        }, 0, 0);
        footerRow.Controls.Add(
            CreateCardActionButton("View", () => ShowAnnouncementDetails(row)),
            1,
            0);

        card.Controls.Add(footerRow);
        card.Controls.Add(headerRow);

        if (announcementId > 0)
        {
            var menu = new ContextMenuStrip();
            var archiveItem = new ToolStripMenuItem("Archive");
            archiveItem.Click += (_, __) => _controller.HandleAnnouncementArchive(announcementId);
            menu.Items.Add(archiveItem);

            AttachContextMenuRecursive(card, menu);
        }

        return card;
    }

    private Panel BuildAnnouncementCard(DataRow row)
    {
        int announcementId = ReadInt(row, "announcement_id");
        string title = ReadCell(row, "title");
        string body = ReadCell(row, "body");
        string priority = ReadCell(row, "priority");
        string status = ReadCell(row, "status");
        string published = ReadCell(row, "published");
        string userState = ReadCell(row, "user_state");
        Color accent = PriorityColor(priority);
        bool isNew = string.Equals(userState, "NEW", StringComparison.OrdinalIgnoreCase);

        var card = CreateCardShell(accent);
        card.Height = 116;

        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 28,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.Controls.Add(new Label
        {
            Text = title,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(UiTheme.BodyFont, FontStyle.Bold),
            ForeColor = UiTheme.Slate900
        }, 0, 0);
        var chips = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        if (isNew)
        {
            chips.Controls.Add(CreateChip("New", UiTheme.AccentBlue));
        }
        chips.Controls.Add(CreateChip(priority, accent));
        topRow.Controls.Add(chips, 1, 0);

        var summaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate600,
            Padding = new Padding(0, 8, 0, 0),
            Text = BuildSnippet(body, "No message body.")
        };

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var metaRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        metaRow.Controls.Add(CreateChip(status, StatusColor(status)));
        metaRow.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Published " + published,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate600,
            Margin = new Padding(10, 4, 0, 0)
        });
        footer.Controls.Add(metaRow, 0, 0);
        footer.Controls.Add(
            CreateCardActionButton("View", () => ShowAnnouncementDetails(row)),
            1,
            0);

        card.Controls.Add(summaryLabel);
        card.Controls.Add(footer);
        card.Controls.Add(topRow);

        if (announcementId > 0)
        {
            var menu = new ContextMenuStrip();
            var archiveItem = new ToolStripMenuItem("Archive");
            archiveItem.Click += (_, __) => _controller.HandleAnnouncementArchive(announcementId);
            menu.Items.Add(archiveItem);
            AttachContextMenuRecursive(card, menu);
        }

        return card;
    }

    private Panel BuildProjectCard(DataRow row)
    {
        string name = ReadCell(row, "name");
        string status = ReadCell(row, "status");
        decimal budget = ReadDecimal(row, "budget");
        string startDate = ReadCell(row, "start_date");
        string endDate = ReadCell(row, "end_date");
        string lead = ReadCell(row, "lead");
        string remarks = ReadCell(row, "remarks");
        Color accent = ProjectStatusColor(status);

        var card = CreateCardShell(accent);
        card.Height = 118;

        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 28,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.Controls.Add(new Label
        {
            Text = name,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(UiTheme.BodyFont, FontStyle.Bold),
            ForeColor = UiTheme.Slate900
        }, 0, 0);
        topRow.Controls.Add(CreateChip(status, accent), 1, 0);

        string detailsText = $"Budget: {budget:N2}   Start: {startDate}   End: {endDate}";
        if (!string.IsNullOrWhiteSpace(lead))
        {
            detailsText += $"   Lead: {lead}";
        }

        var details = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate600,
            Padding = new Padding(0, 8, 0, 0),
            Text = detailsText
        };

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.Controls.Add(new Label
        {
            AutoSize = true,
            Text = string.IsNullOrWhiteSpace(remarks) ? "No remarks." : BuildSnippet(remarks, "No remarks."),
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate500,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        footer.Controls.Add(
            CreateCardActionButton("View", () => ShowProjectDetails(row)),
            1,
            0);

        card.Controls.Add(details);
        card.Controls.Add(footer);
        card.Controls.Add(topRow);
        return card;
    }

    private Panel BuildEmptyStateCard(
        string title,
        string message,
        string? primaryActionText = null,
        Action? primaryAction = null,
        string? secondaryActionText = null,
        Action? secondaryAction = null,
        IconChar icon = IconChar.Message)
    {
        return UiTheme.CreateStateCard(
            title,
            message,
            icon,
            UiTheme.Slate500,
            primaryActionText,
            primaryAction,
            secondaryActionText,
            secondaryAction);
    }

    private Button CreateCardActionButton(string text, Action clickAction)
    {
        var button = new Button
        {
            Text = text,
            Width = 78,
            Height = 26,
            Margin = new Padding(8, 2, 0, 0),
            FlatStyle = FlatStyle.Flat
        };
        UiTheme.StyleSecondaryButton(button);
        button.Click += (_, __) => clickAction();
        return button;
    }

    private static void AttachContextMenuRecursive(Control root, ContextMenuStrip menu)
    {
        root.ContextMenuStrip = menu;
        foreach (Control child in root.Controls)
        {
            AttachContextMenuRecursive(child, menu);
        }
    }

    private static string BuildSnippet(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= 95)
        {
            return normalized;
        }

        return normalized.Substring(0, 92).TrimEnd() + "...";
    }

    private void ShowAnnouncementDetails(DataRow row)
    {
        int announcementId = ReadInt(row, "announcement_id");
        string title = ReadCell(row, "title");
        string body = ReadCell(row, "body");
        string priority = ReadCell(row, "priority");
        string status = ReadCell(row, "status");
        string published = ReadCell(row, "published");

        string message =
            $"Title: {title}\n" +
            $"Priority: {priority}\n" +
             $"Status: {status}\n" +
             $"Published: {published}\n\n" +
             (string.IsNullOrWhiteSpace(body) ? "No message body." : body);
        ControllerDialogs.Info(message, "Announcement");

        if (announcementId > 0)
        {
            _controller.HandleAnnouncementViewed(announcementId);
        }
    }

    private void ShowProjectDetails(DataRow row)
    {
        string name = ReadCell(row, "name");
        string status = ReadCell(row, "status");
        string lead = ReadCell(row, "lead");
        string remarks = ReadCell(row, "remarks");
        string startDate = ReadCell(row, "start_date");
        string endDate = ReadCell(row, "end_date");
        decimal budget = ReadDecimal(row, "budget");

        string message =
            $"Project: {name}\n" +
            $"Status: {status}\n" +
            $"Lead: {(string.IsNullOrWhiteSpace(lead) ? "-" : lead)}\n" +
            $"Budget: {budget:N2}\n" +
            $"Start: {startDate}\n" +
            $"End: {endDate}\n\n" +
            $"Remarks:\n{(string.IsNullOrWhiteSpace(remarks) ? "No remarks." : remarks)}";
        ControllerDialogs.Info(message, "Project Details");
    }

    private static string ReadCell(DataRow row, string columnName)
    {
        return row.Table.Columns.Contains(columnName)
            ? Convert.ToString(row[columnName]) ?? string.Empty
            : string.Empty;
    }

    private static int ReadInt(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
        {
            return 0;
        }

        return int.TryParse(Convert.ToString(row[columnName]), out var value) ? value : 0;
    }

    private static decimal ReadDecimal(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
        {
            return 0m;
        }

        return decimal.TryParse(Convert.ToString(row[columnName]), out var value) ? value : 0m;
    }

    private Panel CreateCardShell(Color accent)
    {
        var panel = new Panel
        {
            BackColor = Blend(Color.White, accent, 8),
            BorderStyle = BorderStyle.None,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(12, 10, 12, 10)
        };
        ApplyLightBorder(panel);
        return panel;
    }

    private Label CreateChip(string text, Color accent)
    {
        return new Label
        {
            AutoSize = true,
            BackColor = Blend(Color.White, accent, 25),
            ForeColor = Blend(UiTheme.Slate900, accent, 55),
            Font = UiTheme.SmallFont,
            Text = " " + text + " ",
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(6, 3, 6, 3),
            Margin = new Padding(0)
        };
    }

    private static Color PriorityColor(string priority)
    {
        return priority.ToLowerInvariant() switch
        {
            "high" => Color.FromArgb(220, 38, 38),
            "normal" => Color.FromArgb(245, 158, 11),
            "low" => Color.FromArgb(22, 163, 74),
            _ => UiTheme.Slate500
        };
    }

    private static Color StatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "published" => Color.FromArgb(22, 163, 74),
            "draft" => Color.FromArgb(245, 158, 11),
            "archived" => UiTheme.Slate500,
            _ => UiTheme.Slate500
        };
    }

    private static Color ProjectStatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "ongoing" => Color.FromArgb(25, 118, 210),
            "planned" => Color.FromArgb(245, 158, 11),
            "on hold" => Color.FromArgb(220, 38, 38),
            "completed" => Color.FromArgb(22, 163, 74),
            _ => UiTheme.Slate500
        };
    }

    private void SetHeader(string title, string subtitle)
    {
        _ = subtitle;
        _pageTitleLabel.Text = title;
        _contentSubtitleLabel.Text = string.Empty;
        _contentSubtitlePanel.Visible = false;
    }

    private void InitializeQuickActionsBar()
    {
        _quickActionsPanel.BackColor = Color.White;
        _quickActionsPanel.BorderStyle = BorderStyle.FixedSingle;
        _quickActionsPanel.Dock = DockStyle.Top;
        _quickActionsPanel.Height = 56;
        _quickActionsPanel.Padding = new Padding(10, 8, 10, 8);

        _quickActionsFlow.Dock = DockStyle.Fill;
        _quickActionsFlow.FlowDirection = FlowDirection.LeftToRight;
        _quickActionsFlow.WrapContents = false;
        _quickActionsFlow.AutoScroll = true;
        _quickActionsFlow.Padding = new Padding(0);
        _quickActionsFlow.Margin = new Padding(0);

        ConfigureQuickActionButton(_quickAddResidentButton, "+ Resident", IconChar.UserPlus, QuickAddResident_Click);
        ConfigureQuickActionButton(_quickAddCertificateButton, "+ Certificate", IconChar.FileCirclePlus, QuickAddCertificate_Click);
        ConfigureQuickActionButton(_quickAddBlotterButton, "+ Blotter", IconChar.Gavel, QuickAddBlotter_Click);
        ConfigureQuickActionButton(_quickRefreshButton, "Refresh", IconChar.RotateRight, QuickRefresh_Click, primary: false);
        _quickAddAnnouncementButton.Visible = false;

        if (_quickActionsFlow.Controls.Count == 0)
        {
            _quickActionsFlow.Controls.Add(_quickAddResidentButton);
            _quickActionsFlow.Controls.Add(_quickAddCertificateButton);
            _quickActionsFlow.Controls.Add(_quickAddBlotterButton);
            _quickActionsFlow.Controls.Add(_quickRefreshButton);
        }

        if (!_quickActionsPanel.Controls.Contains(_quickActionsFlow))
        {
            _quickActionsPanel.Controls.Add(_quickActionsFlow);
        }

        if (!dashboardPanel.Controls.Contains(_quickActionsPanel))
        {
            dashboardPanel.Controls.Add(_quickActionsPanel);
        }

        dashboardPanel.Height = Math.Max(dashboardPanel.Height, 236);
        _quickActionsPanel.BringToFront();
        ApplyRolePermissions();
    }

    private void ConfigureQuickActionButton(
        IconButton button,
        string text,
        IconChar icon,
        EventHandler handler,
        bool primary = true)
    {
        button.Text = text;
        button.IconChar = icon;
        button.IconColor = primary ? Color.White : UiTheme.Slate700;
        button.IconSize = 16;
        button.IconFont = IconFont.Auto;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.AutoSize = false;
        button.Size = new Size(152, 34);
        button.Margin = new Padding(0, 0, 8, 0);

        if (primary)
        {
            UiTheme.StylePrimaryButton(button);
        }
        else
        {
            UiTheme.StyleSecondaryButton(button);
        }

        button.Click -= handler;
        button.Click += handler;
    }

    private void QuickAddResident_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.Profile);
    }

    private void QuickAddCertificate_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.Certificates, CertificateAction.NewRequest);
    }

    private void QuickAddBlotter_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.Blotter);
    }

    private void QuickAddAnnouncement_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanManageAnnouncements)
        {
            ControllerDialogs.Warning("You do not have permission to manage announcements.");
            return;
        }

        announcementsNew.PerformClick();
    }

    private void QuickRefresh_Click(object? sender, EventArgs e)
    {
        _controller.LoadDashboardStats();
    }

    private void SidebarHistory_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.History);
    }

    private void SidebarProfile_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.Profile);
    }

    private void SidebarBlotter_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.Blotter);
    }

    private void SidebarCertificates_Click(object? sender, EventArgs e)
    {
        OpenResidents(ResidentsView.Certificates);
    }

    private void SidebarReports_Click(object? sender, EventArgs e)
    {
        OpenReports();
    }

    private void SidebarSettings_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanOpenSettings)
        {
            ControllerDialogs.Warning("Only Admin users can open settings.");
            return;
        }

        SyncRibbonPrimary("Administration");
        using var form = new SidebarSettingsForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ConfigureDynamicSidebar();
        }
    }

    private void iconButton1_Click(object sender, EventArgs e)
    {
        ShowDashboard();
    }

    private void StyleSidebarButton(IconButton button, IconChar icon, string text)
    {
        button.Dock = DockStyle.Top;
        button.Height = 46;
        button.Font = SystemFonts.MessageBoxFont;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
        button.IconChar = icon;
        button.IconColor = Color.White;
        button.IconFont = IconFont.Auto;
        button.IconSize = 20;
        button.Text = text;
        button.Tag = text;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.ForeColor = Color.White;
    }

    private void UpdateSignedInLabel()
    {
        var signedIn = string.IsNullOrWhiteSpace(UserSession.Username)
            ? "Signed in"
            : "Signed in as " + UserSession.Username;
        _signedInLabel.Text = signedIn;
    }

    private void StyleStatCard(Panel card, Label valueLabel, Label captionLabel, Color accent, bool allowLayout)
    {
        UiTheme.StyleSectionCard(card, Color.White, enforceBorder: false);
        ApplyLightBorder(card);
        if (allowLayout)
        {
            card.Padding = new Padding(16);
            card.MinimumSize = new Size(card.MinimumSize.Width, 116);
        }

        valueLabel.Font = new Font("Segoe UI", 22F, FontStyle.Bold, GraphicsUnit.Point);
        valueLabel.ForeColor = accent;
        if (allowLayout)
        {
            valueLabel.Dock = DockStyle.Top;
            valueLabel.Height = 48;
            valueLabel.Margin = new Padding(0, 20, 0, 2);
        }
        valueLabel.TextAlign = ContentAlignment.TopLeft;
        captionLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        captionLabel.ForeColor = UiTheme.Slate600;
        if (allowLayout)
        {
            captionLabel.Dock = DockStyle.Bottom;
            captionLabel.Height = 22;
        }
        captionLabel.TextAlign = ContentAlignment.BottomLeft;
    }

    private void ConfigureStatIcons()
    {
        ConfigureStatIconControl(_statResidentsIcon, IconChar.User, CardResidentsAccent);
        ConfigureStatIconControl(_statActiveIcon, IconChar.User, CardActiveAccent);
        ConfigureStatIconControl(_statHouseholdsIcon, IconChar.BarChart, CardHouseholdsAccent);
        ConfigureStatIconControl(_statCertsIcon, IconChar.FileSignature, CardCertAccent);
        ConfigureStatIconControl(_statBlotterIcon, IconChar.FileAlt, CardBlotterAccent);
    }

    private static void ConfigureStatIconControl(IconPictureBox icon, IconChar iconChar, Color color)
    {
        icon.IconChar = iconChar;
        icon.IconColor = color;
        icon.IconSize = 22;
        icon.BackColor = Color.Transparent;
        icon.TabStop = false;
        icon.SizeMode = PictureBoxSizeMode.CenterImage;
    }

    private void ApplyLightBorder(Panel panel)
    {
        if (panel == null)
        {
            return;
        }

        panel.BorderStyle = BorderStyle.None;
        if (_lightBorderPanels.Add(panel))
        {
            panel.Paint += LightBorderPanel_Paint;
            panel.Resize += (_, __) => panel.Invalidate();
        }
        panel.Invalidate();
    }

    private void LightBorderPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel || panel.ClientRectangle.Width <= 1 || panel.ClientRectangle.Height <= 1)
        {
            return;
        }

        Rectangle border = new Rectangle(0, 0, panel.ClientRectangle.Width - 1, panel.ClientRectangle.Height - 1);
        using Pen pen = new Pen(CardLightBorder, 1f);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.DrawRectangle(pen, border);
    }

    private static Color Blend(Color from, Color to, int toPercent)
    {
        int percent = Math.Clamp(toPercent, 0, 100);
        int inv = 100 - percent;
        int r = (from.R * inv + to.R * percent) / 100;
        int g = (from.G * inv + to.G * percent) / 100;
        int b = (from.B * inv + to.B * percent) / 100;
        return Color.FromArgb(r, g, b);
    }

    private void StyleTrendPanel(Panel panel, Label title, Color accent)
    {
        UiTheme.StyleSectionCard(panel, Color.White, enforceBorder: false);
        ApplyLightBorder(panel);
        UiTheme.StyleSectionHeader(title, accent);
        title.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
    }

    private void StyleTrendLabels(Label[] labels, Label[] values)
    {
        foreach (var label in labels)
        {
            label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label.ForeColor = UiTheme.Slate700;
            label.AutoSize = true;
        }

        foreach (var value in values)
        {
            value.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            value.ForeColor = UiTheme.Slate900;
            value.AutoSize = true;
        }
    }

    private void StyleTrendBars(ProgressBar[] bars, bool allowLayout)
    {
        foreach (var bar in bars)
        {
            if (allowLayout)
            {
                bar.Dock = DockStyle.Fill;
                bar.Height = 10;
                bar.Margin = new Padding(3, 6, 6, 6);
            }
        }
    }

    private void ApplyTrendSectionColors()
    {
        ApplyTrendGroupColors(
            CardCertAccent,
            new[] { certReqLabel, certAppLabel, certIssLabel, certCanLabel },
            new[] { certReqValue, certAppValue, certIssValue, certCanValue },
            new[] { certReqBar, certAppBar, certIssBar, certCanBar },
            certSparkline);

        ApplyTrendGroupColors(
            CardBlotterAccent,
            new[] { blotterOngoingLabel, blotterSettledLabel, blotterReferredLabel },
            new[] { blotterOngoingValue, blotterSettledValue, blotterReferredValue },
            new[] { blotterOngoingBar, blotterSettledBar, blotterReferredBar },
            blotterSparkline);

        ApplyTrendGroupColors(
            CardResidentsAccent,
            new[] { monthLabel1, monthLabel2, monthLabel3, monthLabel4, monthLabel5, monthLabel6 },
            new[] { monthValue1, monthValue2, monthValue3, monthValue4, monthValue5, monthValue6 },
            new[] { monthBar1, monthBar2, monthBar3, monthBar4, monthBar5, monthBar6 },
            residentsSparkline);
    }

    private void ApplyTrendGroupColors(
        Color accent,
        Label[] labels,
        Label[] values,
        ProgressBar[] bars,
        PictureBox sparkline)
    {
        foreach (var label in labels)
        {
            label.ForeColor = Blend(UiTheme.Slate700, accent, 35);
        }

        foreach (var value in values)
        {
            value.ForeColor = Blend(UiTheme.Slate900, accent, 70);
            value.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        }

        foreach (var bar in bars)
        {
            bar.ForeColor = accent;
        }

        sparkline.BackColor = Blend(Color.White, accent, 8);
    }

    private void StyleOfficialsPanel(bool allowLayout)
    {
        UiTheme.StyleSectionCard(officialsPanel, Color.White, enforceBorder: false);
        ApplyLightBorder(officialsPanel);
        UiTheme.StyleSectionHeader(officialsHeader);
        officialsHeader.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
        if (allowLayout)
        {
            officialsPanel.Padding = new Padding(12, 10, 12, 10);
            EnsureStaffCardsFlow();
            _staffCardsFlow.Padding = new Padding(0, 8, 0, 0);
            _staffCardsFlow.AutoScroll = false;
            _staffCardsFlow.WrapContents = false;
            _staffCardsFlow.FlowDirection = FlowDirection.LeftToRight;
            _staffCardsFlow.Margin = Padding.Empty;
        }

        foreach (var card in _officialCards)
        {
            ApplyLightBorder(card.Container);
            card.Container.Padding = new Padding(12);
            card.Container.MinimumSize = new Size(220, StaffCardHeight);
            card.Container.MaximumSize = new Size(420, StaffCardHeight);
            card.Container.Size = new Size(StaffCardWidth, StaffCardHeight);
            card.Container.Height = StaffCardHeight;
            card.Container.Margin = new Padding(0, 0, 12, 12);

            BuildOfficialCardLayout(card);

            card.Photo.BackColor = UiTheme.Slate50;
            card.Photo.SizeMode = PictureBoxSizeMode.Zoom;
            card.Photo.MinimumSize = new Size(StaffCardPhotoSize, StaffCardPhotoSize);
            card.Photo.MaximumSize = new Size(StaffCardPhotoSize, StaffCardPhotoSize);
            card.Photo.Size = new Size(StaffCardPhotoSize, StaffCardPhotoSize);
            card.Name.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            card.Name.ForeColor = UiTheme.Slate900;
            card.Name.TextAlign = ContentAlignment.MiddleLeft;
            card.Role.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            card.Role.ForeColor = UiTheme.Slate600;
            card.Detail.Font = new Font("Segoe UI", 8.8F, FontStyle.Regular, GraphicsUnit.Point);
            card.Detail.ForeColor = UiTheme.Slate500;
            card.Detail.TextAlign = ContentAlignment.TopLeft;
            StyleStatusButton(card.Status, OfficialPresence.Online);
            UiTheme.StyleSecondaryButton(card.UpdateButton);
            var compactUpdateFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            card.UpdateButton.Font = compactUpdateFont;
            card.UpdateButton.Padding = new Padding(6, 0, 6, 0);
            card.UpdateButton.AutoEllipsis = false;
            card.UpdateButton.TextAlign = ContentAlignment.MiddleCenter;
            int updateButtonWidth = 100;
            card.UpdateButton.AutoSize = false;
            card.UpdateButton.Width = updateButtonWidth;
            card.UpdateButton.Height = 30;
            card.UpdateButton.Margin = new Padding(0);
            card.UpdateButton.MinimumSize = new Size(updateButtonWidth, 30);
        }

        UiTheme.StyleGhostButton(officialsViewAll);
        officialsViewAll.ForeColor = Color.FromArgb(37, 99, 235);
        officialsViewAll.Font = new Font("Segoe UI", 9F, FontStyle.Underline, GraphicsUnit.Point);
        officialsViewAll.TextAlign = ContentAlignment.MiddleLeft;
        officialsViewAll.MinimumSize = new Size(120, 28);
        officialsViewAll.Padding = new Padding(0);
        UpdateStaffCardSizes();
    }

    private void BuildOfficialCardLayout(OfficialCard card)
    {
        if (card.Container.Tag is string tag && tag == "official-layout")
        {
            return;
        }

        card.Container.SuspendLayout();
        try
        {
            card.Container.Controls.Clear();

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                ColumnCount = 2,
                RowCount = 2
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StaffCardPhotoSize + 12F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            TableLayoutPanel photoHost = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                ColumnCount = 1,
                RowCount = 2
            };
            photoHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            photoHost.RowStyles.Add(new RowStyle(SizeType.Absolute, StaffCardPhotoSize));
            photoHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            card.Photo.Dock = DockStyle.None;
            card.Photo.Margin = new Padding(0);
            card.Photo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            photoHost.Controls.Add(card.Photo, 0, 0);

            TableLayoutPanel details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 0),
                Padding = Padding.Empty,
                ColumnCount = 1,
                RowCount = 4
            };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            card.Name.Dock = DockStyle.Fill;
            card.Name.Margin = new Padding(0);

            FlowLayoutPanel roleRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            card.Status.Margin = new Padding(0, 5, 6, 0);
            card.Role.Margin = new Padding(0, 2, 0, 0);
            card.Role.AutoEllipsis = true;
            roleRow.Controls.Add(card.Status);
            roleRow.Controls.Add(card.Role);

            card.Detail.Dock = DockStyle.Fill;
            card.Detail.Margin = new Padding(0, 0, 0, 0);
            card.Detail.AutoEllipsis = true;

            details.Controls.Add(card.Name, 0, 0);
            details.Controls.Add(roleRow, 0, 1);
            details.Controls.Add(card.Detail, 0, 2);

            FlowLayoutPanel actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            card.UpdateButton.Margin = Padding.Empty;
            actionRow.Controls.Add(card.UpdateButton);

            root.Controls.Add(photoHost, 0, 0);
            root.Controls.Add(details, 1, 0);
            root.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty }, 0, 1);
            root.Controls.Add(actionRow, 1, 1);

            card.Container.Controls.Add(root);
            card.Container.Tag = "official-layout";
        }
        finally
        {
            card.Container.ResumeLayout(performLayout: true);
        }
    }

    private void EnsureStaffCardsFlow()
    {
        if (_staffCardsFlow.Name.Length == 0)
        {
            _staffCardsFlow.Name = "flpStaffCards";
            _staffCardsFlow.Dock = DockStyle.Fill;
            _staffCardsFlow.Padding = new Padding(0, 8, 0, 0);
            _staffCardsFlow.Margin = Padding.Empty;
            _staffCardsFlow.AutoScroll = false;
            _staffCardsFlow.WrapContents = false;
            _staffCardsFlow.FlowDirection = FlowDirection.LeftToRight;
            _staffCardsFlow.Resize -= StaffCardsFlow_Resize;
            _staffCardsFlow.Resize += StaffCardsFlow_Resize;
        }

        if (_staffCardsFlow.Parent != officialsPanel)
        {
            int previousIndex = officialsPanel.Controls.IndexOf(officialsFlow);
            officialsPanel.Controls.Remove(officialsFlow);
            officialsFlow.Visible = false;
            officialsPanel.Controls.Add(_staffCardsFlow);
            if (previousIndex >= 0)
            {
                officialsPanel.Controls.SetChildIndex(_staffCardsFlow, previousIndex);
            }
        }

        if (_staffCardsFlow.Controls.Count != _officialCards.Length)
        {
            _staffCardsFlow.SuspendLayout();
            try
            {
                _staffCardsFlow.Controls.Clear();
                foreach (var card in _officialCards)
                {
                    _staffCardsFlow.Controls.Add(card.Container);
                }
            }
            finally
            {
                _staffCardsFlow.ResumeLayout(performLayout: true);
            }
        }
    }

    private void StaffCardsFlow_Resize(object? sender, EventArgs e)
    {
        UpdateStaffCardSizes();
    }

    private void UpdateStaffCardSizes()
    {
        if (_staffCardsFlow == null || _staffCardsFlow.IsDisposed)
        {
            return;
        }

        int count = _officialCards.Length;
        if (count <= 0)
        {
            return;
        }

        int available = Math.Max(1, _staffCardsFlow.ClientSize.Width - _staffCardsFlow.Padding.Horizontal);
        int spacing = 12;
        int width = (available - (spacing * (count - 1))) / count;
        width = Math.Max(220, width);
        width = Math.Min(360, width);

        for (int i = 0; i < count; i++)
        {
            var card = _officialCards[i];
            card.Container.MinimumSize = new Size(220, StaffCardHeight);
            card.Container.MaximumSize = new Size(420, StaffCardHeight);
            card.Container.Size = new Size(width, StaffCardHeight);
            bool lastCard = i == count - 1;
            card.Container.Margin = new Padding(0, 0, lastCard ? 0 : spacing, 0);
        }
    }

    private void WireOfficials()
    {
        for (int i = 0; i < _officialCards.Length; i++)
        {
            int index = i;
            _officialCards[i].UpdateButton.Click += (_, __) =>
            {
                var card = _officialCards[index];
                if (card.UserId <= 0) return;
                _controller.HandleUpdateOfficial(card.UserId);
            };
        }

        officialsViewAll.Click += (_, __) => _controller.HandleViewAllStaff();
    }

    private void WireConnectivityStatus()
    {
        // Initialize connectivity status label
        _connectivityStatusLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
        _connectivityStatusLabel.AutoSize = true;
        _connectivityStatusLabel.Margin = new Padding(10, 0, 10, 0);
        _connectivityStatusLabel.Dock = DockStyle.Right;
        UpdateConnectivityStatus();

        // Add to panel top (right side, before notification button)
        if (!panelTop.Controls.Contains(_connectivityStatusLabel))
        {
            panelTop.Controls.Add(_connectivityStatusLabel);
        }

        // Subscribe to database mode changes
        DatabaseManager.ModeChanged += (sender, args) => UpdateConnectivityStatus();
    }

    private void UpdateConnectivityStatus()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateConnectivityStatus));
            return;
        }

        if (DatabaseManager.IsOnline)
        {
            _connectivityStatusLabel.Text = "● Online";
            _connectivityStatusLabel.ForeColor = Color.FromArgb(34, 197, 94); // green
        }
        else
        {
            _connectivityStatusLabel.Text = "● Offline";
            _connectivityStatusLabel.ForeColor = Color.FromArgb(148, 163, 184); // slate
        }
    }

    private void StyleStatusButton(Button button, OfficialPresence status)
    {
        button.Text = string.Empty;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = ControlPaint.Dark(GetPresenceColor(status));
        button.Enabled = true;
        button.BackColor = GetPresenceColor(status);
        button.Size = new Size(10, 10);
        button.MinimumSize = Size.Empty;
        button.MaximumSize = new Size(12, 12);
        button.AutoSize = false;
        button.TabStop = false;
        button.Cursor = Cursors.Default;
        button.UseVisualStyleBackColor = false;

        _officialStatusToolTip.SetToolTip(button, status switch
        {
            OfficialPresence.Online => "Online",
            OfficialPresence.Offline => "Offline",
            _ => "Away"
        });

        void ApplyCircle()
        {
            var diameter = Math.Min(button.Width, button.Height);
            var rect = new Rectangle(0, 0, diameter, diameter);
            using var path = new GraphicsPath();
            path.AddEllipse(rect);
            button.Region = new Region(path);
        }

        ApplyCircle();
        button.Resize -= StatusButton_Resize;
        button.Resize += StatusButton_Resize;
    }

    private static Color GetPresenceColor(OfficialPresence status)
    {
        return status switch
        {
            OfficialPresence.Online => StatusOnline,
            OfficialPresence.Offline => StatusOffline,
            _ => StatusAway
        };
    }

    private void StatusButton_Resize(object? sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var diameter = Math.Min(button.Width, button.Height);
        var rect = new Rectangle(0, 0, diameter, diameter);
        using var path = new GraphicsPath();
        path.AddEllipse(rect);
        button.Region = new Region(path);
    }

    private void WireSparkline(PictureBox pictureBox, Func<int[]> seriesProvider, Color lineColor)
    {
        pictureBox.BackColor = Blend(Color.White, lineColor, 8);
        pictureBox.BorderStyle = BorderStyle.FixedSingle;
        pictureBox.Paint += (_, e) =>
        {
            var data = seriesProvider();
            DrawSparkline(e.Graphics, pictureBox.ClientRectangle, data, lineColor);
        };
    }

    private void DrawSparkline(Graphics g, Rectangle bounds, int[] data, Color lineColor)
    {
        g.Clear(Blend(Color.White, lineColor, 8));
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (data.Length < 2)
        {
            return;
        }

        int max = 1;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] > max) max = data[i];
        }

        int left = bounds.Left + 6;
        int right = bounds.Right - 6;
        int top = bounds.Top + 8;
        int bottom = bounds.Bottom - 8;
        int width = Math.Max(1, right - left);
        int height = Math.Max(1, bottom - top);

        using var linePen = new Pen(lineColor, 2f);
        using var fillBrush = new SolidBrush(Color.FromArgb(40, lineColor));

        var points = new PointF[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            float x = left + (width * i / (float)(data.Length - 1));
            float y = bottom - (height * data[i] / (float)max);
            points[i] = new PointF(x, y);
        }

        var fillPoints = new PointF[points.Length + 2];
        fillPoints[0] = new PointF(points[0].X, bottom);
        Array.Copy(points, 0, fillPoints, 1, points.Length);
        fillPoints[^1] = new PointF(points[^1].X, bottom);

        g.FillPolygon(fillBrush, fillPoints);
        g.DrawLines(linePen, points);
    }

    // Stub handlers for designer-wired buttons inside the old contentPanel.
    private void add_Click(object sender, EventArgs e) { }
    private void button1_Click(object sender, EventArgs e) { }
    private void button2_Click(object sender, EventArgs e) { }
    private void button3_Click(object sender, EventArgs e) { }
    private void AdminDashboard_Load(object sender, EventArgs e)
    {
        // Stats are already loaded when ShowDashboard() is called during construction.
    }

    private void AdminDashboard_Resize(object? sender, EventArgs e)
    {
        ApplyDashboardResponsiveLayout();
        PositionNotificationPanel();
    }

    private void PositionNotificationPanel()
    {
        int margin = 12;
        int width = notificationPanel.Width;
        int x = Math.Max(margin, ClientSize.Width - width - margin);
        int y = panelTop.Bottom + margin;
        notificationPanel.Location = new Point(x, y);
        notificationPanel.BringToFront();
    }

    private void ToggleNotifications()
    {
        if (_notificationTimer.Enabled)
        {
            return;
        }

        _notificationOpen = !_notificationOpen;
        if (_notificationOpen)
        {
            notificationPanel.Visible = true;
            notificationPanel.BringToFront();
            _controller.RefreshNotifications();
        }
        _notificationTimer.Start();
    }

    private void NotificationTimer_Tick(object? sender, EventArgs e)
    {
        const int step = 32;
        if (_notificationOpen)
        {
            notificationPanel.Height = Math.Min(_notificationTargetHeight, notificationPanel.Height + step);
            if (notificationPanel.Height >= _notificationTargetHeight)
            {
                _notificationTimer.Stop();
            }
        }
        else
        {
            notificationPanel.Height = Math.Max(0, notificationPanel.Height - step);
            if (notificationPanel.Height <= 0)
            {
                _notificationTimer.Stop();
                notificationPanel.Visible = false;
            }
        }
    }

    internal void SetDashboardStats(int totalResidents, int activeResidents, int households, int pendingCertificates, int ongoingBlotter)
    {
        _statResidentsValue.Text = totalResidents.ToString();
        _statActiveValue.Text = activeResidents.ToString();
        _statHouseholdsValue.Text = households.ToString();
        _statCertsValue.Text = pendingCertificates.ToString();
        _statBlotterValue.Text = ongoingBlotter.ToString();
        UpdateDashboardStatIcons(totalResidents, activeResidents, households, pendingCertificates, ongoingBlotter);
    }

    internal void SetBackupStatus(BackupRunInfo? info)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetBackupStatus(info)));
            return;
        }

        if (info == null)
        {
            _backupStatusDot.BackColor = UiTheme.Slate300;
            _backupStatusLabel.Text = "Backup: --";
            _backupToolTip.SetToolTip(_backupStatusLabel, "No backups recorded yet.");
            _backupNowButton.Enabled = true;
            _backupModeCombo.Enabled = true;
            return;
        }

        DateTime when = info.EndedAt ?? info.StartedAt;
        string whenText = when == DateTime.MinValue ? string.Empty : when.ToString("MMM d h:mm tt");
        string modeText = info.Mode switch
        {
            BackupMode.Incremental => "Incremental",
            BackupMode.Differential => "Differential",
            _ => "Full"
        };

        switch (info.State)
        {
            case BackupRunState.Running:
                _backupStatusDot.BackColor = UiTheme.AccentAmber;
                _backupStatusLabel.Text = $"Backup: {modeText} running...";
                _backupNowButton.Enabled = false;
                _backupModeCombo.Enabled = false;
                break;
            case BackupRunState.Success:
                _backupStatusDot.BackColor = UiTheme.AccentGreen;
                _backupStatusLabel.Text = string.IsNullOrWhiteSpace(whenText)
                    ? $"Backup: {modeText} OK"
                    : $"Backup: {modeText} OK ({whenText})";
                _backupNowButton.Enabled = true;
                _backupModeCombo.Enabled = true;
                break;
            case BackupRunState.Failed:
                _backupStatusDot.BackColor = UiTheme.AccentRed;
                _backupStatusLabel.Text = string.IsNullOrWhiteSpace(whenText)
                    ? $"Backup: {modeText} FAILED"
                    : $"Backup: {modeText} FAILED ({whenText})";
                _backupNowButton.Enabled = true;
                _backupModeCombo.Enabled = true;
                break;
            default:
                _backupStatusDot.BackColor = UiTheme.Slate300;
                _backupStatusLabel.Text = string.IsNullOrWhiteSpace(whenText)
                    ? "Backup: --"
                    : $"Backup: -- ({whenText})";
                _backupNowButton.Enabled = true;
                _backupModeCombo.Enabled = true;
                break;
        }

        if (info.State == BackupRunState.Success && !string.IsNullOrWhiteSpace(info.FilePath))
        {
            _backupToolTip.SetToolTip(_backupStatusLabel, "Last backup file:\n" + info.FilePath);
        }
        else if (info.State == BackupRunState.Failed && !string.IsNullOrWhiteSpace(info.ErrorMessage))
        {
            _backupToolTip.SetToolTip(_backupStatusLabel, "Backup failed:\n" + info.ErrorMessage);
        }
        else
        {
            _backupToolTip.SetToolTip(_backupStatusLabel, "Shows the latest backup status.");
        }
    }
 
    internal void SetSchemaVersion(string? version)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetSchemaVersion(version)));
            return;
        }

        string? normalized = string.IsNullOrWhiteSpace(version) ? null : version.Trim();
        string display = "--";
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            display = normalized;
            if (normalized.Length >= 8 && normalized.Take(8).All(char.IsDigit))
            {
                display = normalized.Substring(0, 8);
            }
        }

        _schemaVersionLabel.Text = "Version v" + display;
        _backupToolTip.SetToolTip(_schemaVersionLabel,
            string.IsNullOrWhiteSpace(normalized) ? "Version: unknown" : "Version: " + normalized);
    }

    private void UpdateDashboardStatIcons(int totalResidents, int activeResidents, int households, int pendingCertificates, int ongoingBlotter)
    {
        UpdateCountIcon(_statResidentsIcon, totalResidents, ref _prevTotalResidents, CardResidentsAccent, false, Color.Empty);
        UpdateCountIcon(_statActiveIcon, activeResidents, ref _prevActiveResidents, CardActiveAccent, false, Color.Empty);
        UpdateCountIcon(_statHouseholdsIcon, households, ref _prevHouseholds, CardHouseholdsAccent, false, Color.Empty);
        UpdateCountIcon(_statCertsIcon, pendingCertificates, ref _prevPendingCertificates, Color.FromArgb(70, 70, 70), true, Color.FromArgb(245, 158, 11));
        UpdateCountIcon(_statBlotterIcon, ongoingBlotter, ref _prevOngoingBlotter, Color.FromArgb(70, 70, 70), true, Color.FromArgb(220, 38, 38));
    }

    private static void UpdateCountIcon(
        IconPictureBox icon,
        int value,
        ref int? previousValue,
        Color normalColor,
        bool warningWhenPositive,
        Color warningColor)
    {
        bool increased = previousValue.HasValue && value > previousValue.Value;
        bool decreased = previousValue.HasValue && value < previousValue.Value;

        int size = 28;
        if (increased) size = 32;
        else if (decreased) size = 24;

        Color color;
        if (warningWhenPositive)
        {
            color = value > 0 ? warningColor : normalColor;
        }
        else
        {
            color = value > 0 ? normalColor : UiTheme.Slate500;
        }

        icon.IconColor = color;
        icon.IconSize = size;
        icon.ForeColor = color;
        previousValue = value;
    }

    internal void SetDashboardTrendStats(
        int certRequested, int certApproved, int certIssued, int certCancelled,
        int blotterOngoing, int blotterSettled, int blotterReferred,
        string[] monthLabels, int[] monthCounts)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetDashboardTrendStats(
                certRequested, certApproved, certIssued, certCancelled,
                blotterOngoing, blotterSettled, blotterReferred,
                monthLabels, monthCounts)));
            return;
        }

        _certTrend = new[] { certRequested, certApproved, certIssued, certCancelled };
        _blotterTrend = new[] { blotterOngoing, blotterSettled, blotterReferred };
        _residentTrend = monthCounts;

        int certTotal = Math.Max(1, certRequested + certApproved + certIssued + certCancelled);
        certReqValue.Text = certRequested.ToString();
        certAppValue.Text = certApproved.ToString();
        certIssValue.Text = certIssued.ToString();
        certCanValue.Text = certCancelled.ToString();
        certReqBar.Maximum = certTotal;
        certAppBar.Maximum = certTotal;
        certIssBar.Maximum = certTotal;
        certCanBar.Maximum = certTotal;
        certReqBar.Value = certRequested;
        certAppBar.Value = certApproved;
        certIssBar.Value = certIssued;
        certCanBar.Value = certCancelled;

        int blotterTotal = Math.Max(1, blotterOngoing + blotterSettled + blotterReferred);
        blotterOngoingValue.Text = blotterOngoing.ToString();
        blotterSettledValue.Text = blotterSettled.ToString();
        blotterReferredValue.Text = blotterReferred.ToString();
        blotterOngoingBar.Maximum = blotterTotal;
        blotterSettledBar.Maximum = blotterTotal;
        blotterReferredBar.Maximum = blotterTotal;
        blotterOngoingBar.Value = blotterOngoing;
        blotterSettledBar.Value = blotterSettled;
        blotterReferredBar.Value = blotterReferred;

        int maxMonth = 1;
        foreach (var count in monthCounts)
        {
            if (count > maxMonth) maxMonth = count;
        }

        var monthBars = new[] { monthBar1, monthBar2, monthBar3, monthBar4, monthBar5, monthBar6 };
        var monthLabelControls = new[] { monthLabel1, monthLabel2, monthLabel3, monthLabel4, monthLabel5, monthLabel6 };
        var monthValueControls = new[] { monthValue1, monthValue2, monthValue3, monthValue4, monthValue5, monthValue6 };

        for (int i = 0; i < monthBars.Length && i < monthCounts.Length && i < monthLabels.Length; i++)
        {
            monthLabelControls[i].Text = monthLabels[i];
            monthValueControls[i].Text = monthCounts[i].ToString();
            monthBars[i].Maximum = maxMonth;
            monthBars[i].Value = Math.Min(monthCounts[i], maxMonth);
        }

        certSparkline.Invalidate();
        blotterSparkline.Invalidate();
        residentsSparkline.Invalidate();
    }

    internal void SetOfficials(OfficialInfo[] officials)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetOfficials(officials)));
            return;
        }

        int visibleStaff = Math.Min(_officialCards.Length, officials.Length);
        officialsViewAll.Text = $"View all staff ({visibleStaff})";

        for (int i = 0; i < _officialCards.Length; i++)
        {
            var card = _officialCards[i];
            if (i >= officials.Length)
            {
                card.Name.Text = "Vacant";
                card.Role.Text = "";
                card.Detail.Text = "No assigned account";
                SetOfficialPhoto(card.Photo, null);
                card.Status.Text = "";
                StyleStatusButton(card.Status, OfficialPresence.Away);
                card.UpdateButton.Enabled = false;
                card.UserId = 0;
                continue;
            }

            var official = officials[i];
            card.Name.Text = official.Name;
            card.Role.Text = official.Role;
            card.Detail.Text = string.IsNullOrWhiteSpace(official.LastLoginText)
                ? "Last login: --"
                : $"Last login: {official.LastLoginText}";
            SetOfficialPhoto(card.Photo, official.PhotoPath);
            StyleStatusButton(card.Status, official.IsActive ? OfficialPresence.Online : OfficialPresence.Offline);
            card.UpdateButton.Enabled = true;
            card.UserId = official.UserId;
        }

        UpdateStaffCardSizes();
    }

    private void SetOfficialPhoto(PictureBox pictureBox, string? path)
    {
        var old = pictureBox.Image;
        var targetSize = pictureBox.ClientSize;
        if (targetSize.Width <= 0 || targetSize.Height <= 0)
        {
            targetSize = new Size(StaffCardPhotoSize, StaffCardPhotoSize);
        }

        using var source = LoadImageSafe(path);
        pictureBox.Image = source != null
            ? CreateCoverImage(source, targetSize)
            : AvatarHelper.CreateDefaultAvatar(targetSize);
        old?.Dispose();
    }

    private static Image? LoadImageSafe(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var img = Image.FromStream(stream);
        return (Image)img.Clone();
    }

    private static Image CreateCoverImage(Image source, Size targetSize)
    {
        int targetW = Math.Max(1, targetSize.Width);
        int targetH = Math.Max(1, targetSize.Height);
        var bmp = new Bitmap(targetW, targetH);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        float scale = Math.Max((float)targetW / source.Width, (float)targetH / source.Height);
        int drawW = (int)Math.Ceiling(source.Width * scale);
        int drawH = (int)Math.Ceiling(source.Height * scale);
        int x = (targetW - drawW) / 2;
        int y = (targetH - drawH) / 2;

        g.DrawImage(source, x, y, drawW, drawH);
        return bmp;
    }

    internal sealed class OfficialInfo
    {
        public int UserId { get; }
        public string Name { get; }
        public string Role { get; }
        public string? PhotoPath { get; }
        public bool IsActive { get; }
        public string? LastLoginText { get; }

        public OfficialInfo(int userId, string name, string role, string? photoPath, bool isActive, string? lastLoginText = null)
        {
            UserId = userId;
            Name = name;
            Role = role;
            PhotoPath = photoPath;
            IsActive = isActive;
            LastLoginText = lastLoginText;
        }
    }

    private sealed class OfficialCard
    {
        public Panel Container { get; }
        public PictureBox Photo { get; }
        public Label Name { get; }
        public Label Role { get; }
        public Label Detail { get; }
        public Button Status { get; }
        public Button UpdateButton { get; }
        public int UserId { get; set; }

        public OfficialCard(Panel container, PictureBox photo, Label name, Label role, Button status, Button updateButton)
        {
            Container = container;
            Photo = photo;
            Name = name;
            Role = role;
            Detail = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.TopLeft,
                Text = string.Empty
            };
            Status = status;
            UpdateButton = updateButton;
            UserId = 0;
        }
    }

    private void BtnNotification_Click(object sender, EventArgs e)
    {
        ToggleNotifications();
    }
}
