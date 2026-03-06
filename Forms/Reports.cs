using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FontAwesome.Sharp;
using baranggaysystem1.Controls;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

public partial class Reports : Form
{
    private readonly TableLayoutPanel _root = new();
    private readonly Panel _filterPanel = new();
    private readonly TableLayoutPanel _filterLayout = new();
    private readonly TableLayoutPanel _filterTopRow = new();
    private readonly TableLayoutPanel _filterControlsRow = new();
    private readonly TableLayoutPanel _filterMetaRow = new();
    private readonly FlowLayoutPanel _activeFilters = new();
    private readonly ComboBox _preset = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly ComboBox _purok = new();
    private readonly ComboBox _certStatus = new();
    private readonly ComboBox _blotterStatus = new();
    private readonly Button _apply = new();
    private readonly Button _reset = new();
    private readonly Button _export = new();
    private readonly Button _refresh = new();
    private readonly Label _appliedStamp = new();
    private readonly Label _activeFiltersLabel = new();
    private readonly Label _filterValidation = new();
    private readonly Label _status = new();
    private readonly ToolTip _toolTip = new();
    private readonly ContextMenuStrip _exportMenu = new();

    private readonly FlowLayoutPanel _cards = new();
    private readonly Label _newResidentsValue = new();
    private readonly Label _certRequestsValue = new();
    private readonly Label _certReleasedValue = new();
    private readonly Label _blottersFiledValue = new();
    private readonly Label _pendingCertsValue = new();
    private readonly Label _activeBlottersValue = new();
    private readonly Label _avgApprovalValue = new();
    private readonly Label _avgReleaseValue = new();

    private readonly SplitContainer _contentSplit = new();
    private readonly Panel _trendHost = new();
    private readonly Chart _trendChart = new();
    private readonly Panel _trendStateHost = new();
    private readonly Label _trendStateTitle = new();
    private readonly Label _trendStateMessage = new();
    private readonly Button _trendRetry = new();
    private readonly Label _chartLastUpdated = new();
    private readonly DataGridView _monthlyGrid = new();
    private readonly Label _monthlyEmptyState = new();
    private readonly DataGridView _staffGrid = new();
    private readonly Label _staffEmptyState = new();
    private readonly DataGridView _hotspotGrid = new();
    private readonly Label _hotspotEmptyState = new();
    private readonly TableLayoutPanel _detailShell = new();
    private readonly Panel _detailCompactBar = new();
    private readonly ComboBox _detailNavCompact = new();
    private readonly SplitContainer _contentRightSplit = new();
    private readonly Panel _detailNavCard = new();
    private readonly Label _detailNavTitle = new();
    private readonly ListBox _detailNav = new();
    private readonly Panel _detailContentHost = new();
    private readonly System.Collections.Generic.Dictionary<string, Control> _detailViews = new(StringComparer.Ordinal);
    private readonly SplitContainer _hotspotRoot = new();
    private readonly Panel _hotspotMapPanel = new();
    private readonly Label _hotspotLegend = new();
    private readonly LoadingOverlay _loadingOverlay = new();
    private readonly List<(RectangleF Bounds, HotspotPoint Point)> _hotspotHitTargets = new();
    private IReadOnlyList<HotspotPoint> _hotspotPoints = Array.Empty<HotspotPoint>();
    private string _hotspotTooltipText = string.Empty;

    private bool _isLoading;
    private bool _suspendFilterTracking;
    private bool _hasPendingFilterChanges;
    private bool _suspendDetailSelectionSync;
    private string _appliedFilterSignature = string.Empty;
    private string _currentDetailView = "Monthly";
    private DateTime _lastAppliedAt;
    private ReportsDashboardData? _lastData;
    private DateTime _lastFrom;
    private DateTime _lastTo;
    private Panel? _inlineStateCard;
    private readonly System.Collections.Generic.Dictionary<Label, Label> _cardHints = new();

    private const string DefaultPreset = "Last 6 months";

    public Reports()
    {
        InitializeComponent();

        Text = "Reports";
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;
        MinimumSize = new Size(1024, 680);

        BuildUi();
        HookEvents();
        SetTrendState(
            "Loading trend data",
            "Preparing monthly trend chart for the selected filters.",
            showRetry: false);

        _toolTip.IsBalloon = true;
        ConfigureExportMenu();
        InitializeFilterOptions();

        // Default: last 6 months (inclusive).
        DateTime today = DateTime.Today;
        DateTime start = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        _suspendFilterTracking = true;
        _preset.SelectedItem = DefaultPreset;
        _from.Value = start;
        _to.Value = today;
        _suspendFilterTracking = false;
        _appliedFilterSignature = BuildFilterSignature();

        _avgApprovalValue.Text = "N/A";
        _avgReleaseValue.Text = "N/A";
        UiTheme.StandardizeButtonLayout(this);
        UiTheme.SetTabOrder(
            _preset,
            _from,
            _to,
            _purok,
            _certStatus,
            _blotterStatus,
            _apply,
            _reset,
            _export,
            _refresh,
            _detailNav,
            _detailNavCompact,
            _monthlyGrid,
            _staffGrid);
        UiTheme.EnhanceAccessibility(this);
        Resize += Reports_Resize;
        _cards.Resize += Reports_Resize;
        _contentSplit.Resize += Reports_Resize;
        Shown += (_, __) => ApplyResponsiveLayout();
        UpdateApplyState();
        RebuildActiveFilterSummary();
        ApplyResponsiveLayout();
    }

    private void BuildUi()
    {
        SuspendLayout();

        _root.Dock = DockStyle.Fill;
        _root.Padding = new Padding(16);
        _root.ColumnCount = 1;
        _root.RowCount = 4;
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        BuildFilterPanel();
        BuildCardsPanel();
        BuildContentArea();
        BuildStatusBar();

        _root.Controls.Add(_filterPanel, 0, 0);
        _root.Controls.Add(_cards, 0, 1);
        _root.Controls.Add(_contentSplit, 0, 2);
        _root.Controls.Add(_status, 0, 3);

        Controls.Add(_root);
        ConfigureLoadingOverlay();
        ResumeLayout();
    }

    private void ConfigureLoadingOverlay()
    {
        _loadingOverlay.HideLoading();
        Controls.Add(_loadingOverlay);
        _loadingOverlay.BringToFront();
    }

    private void BuildFilterPanel()
    {
        _filterPanel.Dock = DockStyle.Top;
        _filterPanel.Height = 146;
        _filterPanel.Padding = new Padding(12);
        _filterPanel.AutoScroll = true;
        UiTheme.StyleSectionCard(_filterPanel);

        _filterLayout.Dock = DockStyle.Fill;
        _filterLayout.ColumnCount = 1;
        _filterLayout.RowCount = 3;
        _filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _filterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        _filterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _filterLayout.Padding = Padding.Empty;
        _filterLayout.Margin = Padding.Empty;
        _filterLayout.BackColor = Color.Transparent;

        var title = new Label
        {
            AutoSize = true,
            Text = "Filters",
            Margin = new Padding(0, 4, 0, 0)
        };
        UiTheme.StyleSectionHeader(title, useHeadingFont: true);

        _appliedStamp.AutoSize = true;
        _appliedStamp.Font = UiTheme.SmallFont;
        _appliedStamp.ForeColor = UiTheme.Slate500;
        _appliedStamp.Margin = new Padding(0, 8, 0, 0);
        _appliedStamp.Text = "Data as of -";

        _refresh.Text = "Refresh";
        _refresh.Width = 96;
        _refresh.Height = 32;
        UiTheme.StyleSecondaryButton(_refresh);

        _filterTopRow.Dock = DockStyle.Fill;
        _filterTopRow.ColumnCount = 3;
        _filterTopRow.RowCount = 1;
        _filterTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _filterTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _filterTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _filterTopRow.Margin = Padding.Empty;
        _filterTopRow.Padding = Padding.Empty;
        _filterTopRow.BackColor = Color.Transparent;

        var topRight = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        topRight.Controls.Add(_appliedStamp);
        topRight.Controls.Add(_refresh);

        _filterTopRow.Controls.Add(title, 0, 0);
        _filterTopRow.Controls.Add(topRight, 2, 0);

        _preset.DropDownStyle = ComboBoxStyle.DropDownList;
        _preset.Font = UiTheme.BodyFont;
        _preset.Width = 160;
        _preset.Dock = DockStyle.Fill;
        _preset.Margin = new Padding(0, 6, 8, 4);
        _preset.Items.AddRange(new object[]
        {
            "This month",
            "Last 30 days",
            "Last 6 months",
            "Last 12 months",
            "Year to date",
            "All time",
            "Custom range"
        });

        _from.Format = DateTimePickerFormat.Short;
        _from.Width = 130;
        _from.Dock = DockStyle.Fill;
        _from.Margin = new Padding(0, 6, 8, 4);

        _to.Format = DateTimePickerFormat.Short;
        _to.Width = 130;
        _to.Dock = DockStyle.Fill;
        _to.Margin = new Padding(0, 6, 8, 4);

        _purok.DropDownStyle = ComboBoxStyle.DropDownList;
        _purok.Font = UiTheme.BodyFont;
        _purok.Width = 170;
        _purok.Dock = DockStyle.Fill;
        _purok.Margin = new Padding(0, 6, 8, 4);

        _certStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _certStatus.Font = UiTheme.BodyFont;
        _certStatus.Width = 170;
        _certStatus.Dock = DockStyle.Fill;
        _certStatus.Margin = new Padding(0, 6, 8, 4);

        _blotterStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _blotterStatus.Font = UiTheme.BodyFont;
        _blotterStatus.Width = 150;
        _blotterStatus.Dock = DockStyle.Fill;
        _blotterStatus.Margin = new Padding(0, 6, 8, 4);

        _apply.Text = "Apply";
        _apply.Width = 96;
        _apply.Height = 32;
        _apply.Dock = DockStyle.Fill;
        _apply.Margin = new Padding(0, 4, 8, 4);
        UiTheme.StylePrimaryButton(_apply);

        _reset.Text = "Reset";
        _reset.Width = 92;
        _reset.Height = 32;
        _reset.Dock = DockStyle.Fill;
        _reset.Margin = new Padding(0, 4, 8, 4);
        UiTheme.StyleSecondaryButton(_reset);

        _export.Text = "Export";
        _export.Width = 104;
        _export.Height = 32;
        _export.Dock = DockStyle.Fill;
        _export.Margin = new Padding(0, 4, 0, 4);
        UiTheme.StyleSecondaryButton(_export);

        _filterControlsRow.Dock = DockStyle.Fill;
        _filterControlsRow.ColumnCount = 17;
        _filterControlsRow.RowCount = 1;
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Preset
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F)); // preset value
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // From
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // from value
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // To
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F)); // to value
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Purok
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F)); // purok value
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Cert status
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F)); // cert value
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Blotter status
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F)); // blotter value
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // spacer
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F)); // apply
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F)); // reset
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F)); // export
        _filterControlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8F)); // right pad
        _filterControlsRow.Margin = Padding.Empty;
        _filterControlsRow.Padding = Padding.Empty;
        _filterControlsRow.BackColor = Color.Transparent;

        Label BuildFilterLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                Font = UiTheme.LabelFont,
                ForeColor = UiTheme.Slate700,
                Margin = new Padding(0, 10, 6, 0)
            };
        }

        _filterControlsRow.Controls.Add(BuildFilterLabel("Preset"), 0, 0);
        _filterControlsRow.Controls.Add(_preset, 1, 0);
        _filterControlsRow.Controls.Add(BuildFilterLabel("From"), 2, 0);
        _filterControlsRow.Controls.Add(_from, 3, 0);
        _filterControlsRow.Controls.Add(BuildFilterLabel("To"), 4, 0);
        _filterControlsRow.Controls.Add(_to, 5, 0);
        _filterControlsRow.Controls.Add(BuildFilterLabel("Purok"), 6, 0);
        _filterControlsRow.Controls.Add(_purok, 7, 0);
        _filterControlsRow.Controls.Add(BuildFilterLabel("Cert status"), 8, 0);
        _filterControlsRow.Controls.Add(_certStatus, 9, 0);
        _filterControlsRow.Controls.Add(BuildFilterLabel("Blotter status"), 10, 0);
        _filterControlsRow.Controls.Add(_blotterStatus, 11, 0);
        _filterControlsRow.Controls.Add(_apply, 13, 0);
        _filterControlsRow.Controls.Add(_reset, 14, 0);
        _filterControlsRow.Controls.Add(_export, 15, 0);

        _activeFiltersLabel.AutoSize = true;
        _activeFiltersLabel.Text = "Active filters:";
        _activeFiltersLabel.Font = UiTheme.LabelFont;
        _activeFiltersLabel.ForeColor = UiTheme.Slate700;
        _activeFiltersLabel.Margin = new Padding(0, 7, 6, 0);

        _activeFilters.Dock = DockStyle.Fill;
        _activeFilters.FlowDirection = FlowDirection.LeftToRight;
        _activeFilters.WrapContents = false;
        _activeFilters.AutoScroll = true;
        _activeFilters.Margin = Padding.Empty;
        _activeFilters.Padding = Padding.Empty;

        _filterValidation.AutoSize = true;
        _filterValidation.Font = UiTheme.SmallFont;
        _filterValidation.ForeColor = UiTheme.AccentRed;
        _filterValidation.TextAlign = ContentAlignment.MiddleRight;
        _filterValidation.Margin = new Padding(6, 8, 0, 0);

        _filterMetaRow.Dock = DockStyle.Fill;
        _filterMetaRow.ColumnCount = 3;
        _filterMetaRow.RowCount = 1;
        _filterMetaRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _filterMetaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _filterMetaRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _filterMetaRow.Margin = Padding.Empty;
        _filterMetaRow.Padding = Padding.Empty;
        _filterMetaRow.BackColor = Color.Transparent;
        _filterMetaRow.Controls.Add(_activeFiltersLabel, 0, 0);
        _filterMetaRow.Controls.Add(_activeFilters, 1, 0);
        _filterMetaRow.Controls.Add(_filterValidation, 2, 0);

        _filterLayout.Controls.Add(_filterTopRow, 0, 0);
        _filterLayout.Controls.Add(_filterControlsRow, 0, 1);
        _filterLayout.Controls.Add(_filterMetaRow, 0, 2);

        _filterPanel.Controls.Add(_filterLayout);
    }

    private void BuildCardsPanel()
    {
        _cards.Dock = DockStyle.Top;
        _cards.FlowDirection = FlowDirection.LeftToRight;
        _cards.WrapContents = true;
        _cards.AutoSize = true;
        _cards.Padding = new Padding(0);
        _cards.Margin = new Padding(0, 12, 0, 12);
        _cards.BackColor = Color.Transparent;

        _cards.Controls.Add(CreateStatCard("New Residents", _newResidentsValue, UiTheme.AccentGreen));
        _cards.Controls.Add(CreateStatCard("Cert Requests", _certRequestsValue, UiTheme.AccentBlue));
        _cards.Controls.Add(CreateStatCard("Cert Released", _certReleasedValue, UiTheme.AccentAmber));
        _cards.Controls.Add(CreateStatCard("Blotter Filed", _blottersFiledValue, UiTheme.AccentOrange));
        _cards.Controls.Add(CreateStatCard("Pending Certs", _pendingCertsValue, UiTheme.Slate500));
        _cards.Controls.Add(CreateStatCard("Active Blotters", _activeBlottersValue, UiTheme.AccentRed));
        _cards.Controls.Add(CreateStatCard("Avg Req->Approve", _avgApprovalValue, UiTheme.AccentBlue));
        _cards.Controls.Add(CreateStatCard("Avg Approve->Release", _avgReleaseValue, UiTheme.AccentAmber));
    }

    private void BuildContentArea()
    {
        _contentSplit.Dock = DockStyle.Fill;
        _contentSplit.Orientation = Orientation.Vertical;
        _contentSplit.FixedPanel = FixedPanel.Panel1;
        _contentSplit.Panel1MinSize = 0;
        _contentSplit.Panel2MinSize = 0;
        _contentSplit.SplitterWidth = 6;
        _contentSplit.BackColor = Color.Transparent;

        BuildTrendChart();
        BuildMonthlyGrid();
        BuildStaffGrid();
        BuildHotspotView();
        BuildDetailArea();

        _contentRightSplit.Dock = DockStyle.Fill;
        _contentRightSplit.Orientation = Orientation.Horizontal;
        _contentRightSplit.Panel1MinSize = 0;
        _contentRightSplit.Panel2MinSize = 0;
        _contentRightSplit.SplitterWidth = 6;
        _contentRightSplit.BackColor = Color.Transparent;

        var detailHostCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        UiTheme.StyleGridContainer(detailHostCard);
        detailHostCard.Controls.Add(_detailShell);

        _contentRightSplit.Panel1.Controls.Add(_trendHost);
        _contentRightSplit.Panel2.Controls.Add(detailHostCard);

        _contentSplit.Panel1.Controls.Add(_detailNavCard);
        _contentSplit.Panel2.Controls.Add(_contentRightSplit);
    }

    private void BuildDetailArea()
    {
        _detailShell.Dock = DockStyle.Fill;
        _detailShell.ColumnCount = 1;
        _detailShell.RowCount = 2;
        _detailShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        _detailShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _detailShell.Margin = Padding.Empty;
        _detailShell.Padding = Padding.Empty;
        _detailShell.BackColor = Color.White;

        _detailCompactBar.Dock = DockStyle.Fill;
        _detailCompactBar.Padding = new Padding(0, 0, 0, 8);
        _detailCompactBar.Visible = false;
        _detailCompactBar.BackColor = Color.Transparent;

        var compactLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        compactLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        compactLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var compactLabel = new Label
        {
            AutoSize = true,
            Text = "View",
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate700,
            Margin = new Padding(0, 8, 6, 0)
        };

        _detailNavCompact.DropDownStyle = ComboBoxStyle.DropDownList;
        _detailNavCompact.Dock = DockStyle.Fill;
        _detailNavCompact.Margin = new Padding(0, 4, 0, 0);
        UiTheme.StyleComboBox(_detailNavCompact);
        _detailNavCompact.Items.AddRange(new object[] { "Monthly", "Staff Performance", "Hotspot Map" });
        if (_detailNavCompact.Items.Count > 0)
        {
            _detailNavCompact.SelectedIndex = 0;
        }
        _detailNavCompact.AccessibleName = "Report view selector";

        compactLayout.Controls.Add(compactLabel, 0, 0);
        compactLayout.Controls.Add(_detailNavCompact, 1, 0);
        _detailCompactBar.Controls.Add(compactLayout);

        _detailNavCard.Dock = DockStyle.Fill;
        _detailNavCard.Padding = new Padding(10);
        UiTheme.StyleSectionCard(_detailNavCard, Color.White);

        var navLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        navLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        navLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _detailNavTitle.AutoSize = true;
        _detailNavTitle.Text = "Report Views";
        _detailNavTitle.Margin = new Padding(0, 0, 0, 8);
        UiTheme.StyleSectionHeader(_detailNavTitle);

        _detailNav.Dock = DockStyle.Fill;
        _detailNav.BorderStyle = BorderStyle.None;
        _detailNav.DrawMode = DrawMode.OwnerDrawFixed;
        _detailNav.ItemHeight = 34;
        _detailNav.IntegralHeight = false;
        _detailNav.Font = UiTheme.BodyFont;
        _detailNav.BackColor = Color.White;
        _detailNav.ForeColor = UiTheme.Slate700;
        _detailNav.Items.AddRange(new object[] { "Monthly", "Staff Performance", "Hotspot Map" });
        _detailNav.AccessibleName = "Report views navigation";

        navLayout.Controls.Add(_detailNavTitle, 0, 0);
        navLayout.Controls.Add(_detailNav, 0, 1);
        _detailNavCard.Controls.Add(navLayout);

        _detailContentHost.Dock = DockStyle.Fill;
        _detailContentHost.Margin = Padding.Empty;
        _detailContentHost.Padding = Padding.Empty;
        _detailContentHost.BackColor = Color.Transparent;
        _detailViews.Clear();
        _detailContentHost.Controls.Clear();

        RegisterDetailView("Monthly", BuildGridTabHost(_monthlyGrid, _monthlyEmptyState, "No monthly records for selected filters."));
        RegisterDetailView("Staff Performance", BuildGridTabHost(_staffGrid, _staffEmptyState, "No staff performance records for selected filters."));

        if (helper.Permissions.CanViewHotspotReports)
        {
            RegisterDetailView("Hotspot Map", _hotspotRoot);
        }
        else
        {
            var denied = UiTheme.CreateStateCard(
                "Access restricted",
                "Your account does not have permission to view hotspot analytics.",
                IconChar.Lock,
                UiTheme.AccentOrange);
            denied.Dock = DockStyle.Fill;
            RegisterDetailView("Hotspot Map", denied);
        }

        _detailShell.Controls.Add(_detailCompactBar, 0, 0);
        _detailShell.Controls.Add(_detailContentHost, 0, 1);

        _detailNav.DrawItem += DetailNav_DrawItem;
        _detailNav.SelectedIndexChanged += (_, __) =>
        {
            if (_suspendDetailSelectionSync || _detailNav.SelectedItem is not string selected)
            {
                return;
            }

            SelectDetailView(selected);
        };

        _detailNavCompact.SelectedIndexChanged += (_, __) =>
        {
            if (_suspendDetailSelectionSync || _detailNavCompact.SelectedItem is not string selected)
            {
                return;
            }

            SelectDetailView(selected);
        };

        if (_detailNav.Items.Count > 0)
        {
            _detailNav.SelectedIndex = 0;
        }
        SelectDetailView("Monthly");
    }

    private void RegisterDetailView(string key, Control view)
    {
        if (string.IsNullOrWhiteSpace(key) || view == null)
        {
            return;
        }

        view.Dock = DockStyle.Fill;
        view.Visible = false;
        _detailViews[key] = view;
        _detailContentHost.Controls.Add(view);
    }

    private void SelectDetailView(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !_detailViews.ContainsKey(key))
        {
            return;
        }

        _suspendDetailSelectionSync = true;
        foreach (var kvp in _detailViews)
        {
            kvp.Value.Visible = string.Equals(kvp.Key, key, StringComparison.Ordinal);
            if (kvp.Value.Visible)
            {
                kvp.Value.BringToFront();
            }
        }

        _currentDetailView = key;
        if (_detailNav.SelectedItem?.ToString() != key)
        {
            _detailNav.SelectedItem = key;
        }
        if (_detailNavCompact.SelectedItem?.ToString() != key)
        {
            _detailNavCompact.SelectedItem = key;
        }
        _suspendDetailSelectionSync = false;

        _detailNav.Invalidate();
    }

    private void DetailNav_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _detailNav.Items.Count)
        {
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        bool focused = (e.State & DrawItemState.Focus) == DrawItemState.Focus;
        string text = _detailNav.Items[e.Index]?.ToString() ?? string.Empty;

        Color back = selected
            ? UiTheme.Blend(Color.White, UiTheme.AccentBlue, 16)
            : Color.White;
        Color fore = selected
            ? UiTheme.Slate900
            : UiTheme.Slate700;

        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);

        if (selected)
        {
            using var accentBrush = new SolidBrush(UiTheme.AccentBlue);
            e.Graphics.FillRectangle(accentBrush, e.Bounds.Left, e.Bounds.Top, 4, e.Bounds.Height);
        }

        var textRect = Rectangle.Inflate(e.Bounds, -12, 0);
        if (selected)
        {
            using var selectedFont = new Font(UiTheme.BodyFont, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                selectedFont,
                textRect,
                fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else
        {
            TextRenderer.DrawText(
                e.Graphics,
                text,
                UiTheme.BodyFont,
                textRect,
                fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        if (focused)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, fore, back);
        }
    }

    private void BuildHotspotView()
    {
        _hotspotRoot.Dock = DockStyle.Fill;
        _hotspotRoot.Orientation = Orientation.Vertical;
        _hotspotRoot.Panel1MinSize = 0;
        _hotspotRoot.Panel2MinSize = 0;
        _hotspotRoot.BackColor = Color.White;
        SetSafeSplitterDistance(_hotspotRoot, 520);

        var mapHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 10, 10, 6),
            BackColor = Color.White
        };

        _hotspotMapPanel.Dock = DockStyle.Fill;
        _hotspotMapPanel.BackColor = UiTheme.Blend(Color.White, UiTheme.AccentBlue, 6);
        _hotspotMapPanel.BorderStyle = BorderStyle.FixedSingle;
        _hotspotMapPanel.Paint += HotspotMapPanel_Paint;
        _hotspotMapPanel.MouseMove += HotspotMapPanel_MouseMove;
        _hotspotMapPanel.MouseLeave += (_, __) => _toolTip.Hide(_hotspotMapPanel);

        _hotspotLegend.Dock = DockStyle.Bottom;
        _hotspotLegend.Height = 24;
        _hotspotLegend.Font = UiTheme.SmallFont;
        _hotspotLegend.ForeColor = UiTheme.Slate600;
        _hotspotLegend.Text = "Larger/redder circles indicate higher incident density.";

        mapHost.Controls.Add(_hotspotMapPanel);
        mapHost.Controls.Add(_hotspotLegend);

        _hotspotGrid.Dock = DockStyle.Fill;
        _hotspotGrid.ReadOnly = true;
        _hotspotGrid.AllowUserToAddRows = false;
        _hotspotGrid.AllowUserToDeleteRows = false;
        _hotspotGrid.AllowUserToResizeRows = false;
        _hotspotGrid.MultiSelect = false;
        _hotspotGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _hotspotGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _hotspotGrid.RowHeadersVisible = false;
        _hotspotGrid.AllowUserToOrderColumns = true;
        _hotspotGrid.AllowUserToResizeColumns = true;
        UiTheme.StyleGrid(_hotspotGrid);

        _hotspotRoot.Panel1.Controls.Add(mapHost);
        _hotspotRoot.Panel2.Controls.Add(BuildGridTabHost(_hotspotGrid, _hotspotEmptyState, "No hotspot rows for selected filters."));
    }

    private void BuildTrendChart()
    {
        _trendHost.Dock = DockStyle.Fill;
        _trendHost.Padding = new Padding(12);
        UiTheme.StyleSectionCard(_trendHost);

        var trendShell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.White
        };
        trendShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        trendShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var trendHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.White
        };
        trendHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        trendHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var trendTitle = new Label
        {
            AutoSize = true,
            Text = "Trend Overview",
            Margin = new Padding(0, 6, 0, 0)
        };
        UiTheme.StyleSectionHeader(trendTitle);

        _chartLastUpdated.AutoSize = true;
        _chartLastUpdated.Dock = DockStyle.Right;
        _chartLastUpdated.TextAlign = ContentAlignment.MiddleRight;
        _chartLastUpdated.Font = UiTheme.SmallFont;
        _chartLastUpdated.ForeColor = UiTheme.Slate500;
        _chartLastUpdated.Text = "No data loaded";

        trendHeader.Controls.Add(trendTitle, 0, 0);
        trendHeader.Controls.Add(_chartLastUpdated, 1, 0);

        _trendChart.Dock = DockStyle.Fill;
        _trendChart.MinimumSize = new Size(240, 140);
        _trendChart.BackColor = Color.White;
        _trendChart.BorderlineDashStyle = ChartDashStyle.NotSet;
        _trendChart.BorderlineColor = UiTheme.Slate100;
        _trendChart.BorderlineWidth = 1;

        _trendChart.ChartAreas.Clear();
        _trendChart.Series.Clear();
        _trendChart.Titles.Clear();
        _trendChart.Legends.Clear();

        var area = new ChartArea("Main");
        area.BackColor = Color.White;
        area.AxisX.MajorGrid.Enabled = false;
        area.AxisX.Interval = 1;
        area.AxisX.LabelStyle.Angle = -45;
        area.AxisX.LabelStyle.Font = UiTheme.SmallFont;
        area.AxisX.LineColor = UiTheme.Slate300;
        area.AxisY.MajorGrid.LineColor = UiTheme.Slate100;
        area.AxisY.LabelStyle.Font = UiTheme.SmallFont;
        area.AxisY.LineColor = UiTheme.Slate300;
        _trendChart.ChartAreas.Add(area);

        var legend = new Legend
        {
            Docking = Docking.Top,
            Alignment = StringAlignment.Near,
            BackColor = Color.Transparent,
            Font = UiTheme.SmallFont
        };
        _trendChart.Legends.Add(legend);

        _trendChart.Titles.Add(new Title
        {
            Text = "Residents, Certificates, and Blotter Cases",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate500,
            Alignment = ContentAlignment.TopLeft
        });

        _trendChart.Series.Add(CreateLineSeries("Residents", UiTheme.AccentGreen));
        _trendChart.Series.Add(CreateLineSeries("Certificates", UiTheme.AccentBlue));
        _trendChart.Series.Add(CreateLineSeries("Blotter", UiTheme.AccentOrange));

        var trendBody = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        trendBody.Controls.Add(_trendChart);

        _trendStateHost.Dock = DockStyle.Fill;
        _trendStateHost.BackColor = Color.White;
        _trendStateHost.Visible = false;

        var trendStateCard = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(24, 20, 24, 20),
            BackColor = Color.White
        };
        trendStateCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        trendStateCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        trendStateCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _trendStateTitle.AutoSize = true;
        _trendStateTitle.Font = new Font(UiTheme.BodyFont, FontStyle.Bold);
        _trendStateTitle.ForeColor = UiTheme.Slate700;
        _trendStateTitle.Margin = new Padding(0, 0, 0, 8);

        _trendStateMessage.AutoSize = true;
        _trendStateMessage.Font = UiTheme.LabelFont;
        _trendStateMessage.ForeColor = UiTheme.Slate500;
        _trendStateMessage.MaximumSize = new Size(760, 0);
        _trendStateMessage.Margin = new Padding(0, 0, 0, 12);

        _trendRetry.Text = "Retry";
        _trendRetry.AutoSize = true;
        _trendRetry.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _trendRetry.Padding = new Padding(10, 3, 10, 3);
        _trendRetry.Visible = false;
        UiTheme.StyleSecondaryButton(_trendRetry);
        _trendRetry.Click += async (_, __) => await RefreshReportsAsync();

        trendStateCard.Controls.Add(_trendStateTitle, 0, 0);
        trendStateCard.Controls.Add(_trendStateMessage, 0, 1);
        trendStateCard.Controls.Add(_trendRetry, 0, 2);
        _trendStateHost.Controls.Add(trendStateCard);

        trendBody.Controls.Add(_trendStateHost);

        trendShell.Controls.Add(trendHeader, 0, 0);
        trendShell.Controls.Add(trendBody, 0, 1);
        _trendHost.Controls.Add(trendShell);
    }

    private void BuildMonthlyGrid()
    {
        _monthlyGrid.Dock = DockStyle.Fill;
        _monthlyGrid.ReadOnly = true;
        _monthlyGrid.AllowUserToAddRows = false;
        _monthlyGrid.AllowUserToDeleteRows = false;
        _monthlyGrid.AllowUserToResizeRows = false;
        _monthlyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _monthlyGrid.MultiSelect = false;
        _monthlyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _monthlyGrid.RowHeadersVisible = false;
        _monthlyGrid.AllowUserToOrderColumns = true;
        _monthlyGrid.AllowUserToResizeColumns = true;

        UiTheme.StyleGrid(_monthlyGrid);
    }

    private void BuildStaffGrid()
    {
        _staffGrid.Dock = DockStyle.Fill;
        _staffGrid.ReadOnly = true;
        _staffGrid.AllowUserToAddRows = false;
        _staffGrid.AllowUserToDeleteRows = false;
        _staffGrid.AllowUserToResizeRows = false;
        _staffGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _staffGrid.MultiSelect = false;
        _staffGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _staffGrid.RowHeadersVisible = false;
        _staffGrid.AllowUserToOrderColumns = true;
        _staffGrid.AllowUserToResizeColumns = true;

        UiTheme.StyleGrid(_staffGrid);
    }

    private static Panel BuildGridTabHost(Control grid, Label emptyStateLabel, string emptyText)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            Margin = Padding.Empty,
            BackColor = Color.White
        };

        emptyStateLabel.Dock = DockStyle.Fill;
        emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyStateLabel.Font = UiTheme.LabelFont;
        emptyStateLabel.ForeColor = UiTheme.Slate500;
        emptyStateLabel.Text = emptyText;
        emptyStateLabel.Visible = false;

        host.Controls.Add(grid);
        host.Controls.Add(emptyStateLabel);
        return host;
    }

    private static void UpdateGridEmptyState(DataGridView grid, Label emptyStateLabel)
    {
        emptyStateLabel.Visible = grid.Rows.Count == 0;
        if (emptyStateLabel.Visible)
        {
            emptyStateLabel.BringToFront();
        }
    }

    private void BuildStatusBar()
    {
        _status.Dock = DockStyle.Top;
        _status.AutoSize = true;
        _status.Margin = new Padding(0, 10, 0, 0);
        _status.Font = UiTheme.SmallFont;
        _status.ForeColor = UiTheme.Slate500;
        _status.Text = "Ready";
    }

    private void Reports_Resize(object? sender, EventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (IsDisposed || _cards.ClientSize.Width <= 0 || _contentSplit.Height <= 0)
        {
            return;
        }

        int width = ClientSize.Width;
        _filterPanel.Height = width switch
        {
            < 1120 => 186,
            < 1360 => 164,
            _ => 146
        };

        int cardsPerRow = width switch
        {
            < 860 => 1,
            < 1240 => 2,
            _ => 4
        };
        int spacing = 10;
        int available = Math.Max(240, _cards.ClientSize.Width);
        int cardWidth = Math.Max(190, (available - ((cardsPerRow - 1) * spacing)) / cardsPerRow);
        foreach (Control card in _cards.Controls)
        {
            card.Width = cardWidth;
        }
        if (_inlineStateCard != null)
        {
            _inlineStateCard.Width = Math.Max(220, _cards.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        }

        int desiredNavWidth = width < 1360 ? 220 : 240;
        ApplySplitMinimums(_contentSplit, desiredPanel1Min: 200, desiredPanel2Min: 520);
        SetSafeSplitterDistance(_contentSplit, desiredNavWidth);

        int desiredTrendHeight = width switch
        {
            < 1100 => (int)(_contentRightSplit.Height * 0.44f),
            < 1400 => (int)(_contentRightSplit.Height * 0.47f),
            _ => (int)(_contentRightSplit.Height * 0.5f)
        };
        ApplySplitMinimums(_contentRightSplit, desiredPanel1Min: 210, desiredPanel2Min: 220);
        SetSafeSplitterDistance(_contentRightSplit, desiredTrendHeight);

        bool useCompactDetailNav = width < 1180;
        _detailCompactBar.Visible = useCompactDetailNav;
        _detailShell.RowStyles[0].Height = useCompactDetailNav ? 44F : 0F;

        if (useCompactDetailNav)
        {
            if (!_contentSplit.Panel1Collapsed)
            {
                _contentSplit.Panel1Collapsed = true;
            }

            if (_detailNavCompact.SelectedItem?.ToString() != _currentDetailView)
            {
                _detailNavCompact.SelectedItem = _currentDetailView;
            }
        }
        else
        {
            if (_contentSplit.Panel1Collapsed)
            {
                _contentSplit.Panel1Collapsed = false;
            }

            ApplySplitMinimums(_contentSplit, desiredPanel1Min: 200, desiredPanel2Min: 520);
            SetSafeSplitterDistance(_contentSplit, desiredNavWidth);

            if (_detailNav.SelectedItem?.ToString() != _currentDetailView)
            {
                _detailNav.SelectedItem = _currentDetailView;
            }
        }

        int desiredHotspotWidth = (int)(_hotspotRoot.Width * (width < 1200 ? 0.54f : 0.62f));
        ApplySplitMinimums(_hotspotRoot, desiredPanel1Min: 260, desiredPanel2Min: 220);
        SetSafeSplitterDistance(_hotspotRoot, desiredHotspotWidth);
    }

    private static void ApplySplitMinimums(SplitContainer split, int desiredPanel1Min, int desiredPanel2Min)
    {
        if (split.IsDisposed)
        {
            return;
        }

        int total = split.Orientation == Orientation.Horizontal
            ? split.ClientSize.Height
            : split.ClientSize.Width;
        if (total <= 0)
        {
            return;
        }

        int available = Math.Max(0, total - split.SplitterWidth);
        int panel1Min;
        int panel2Min;
        int requestedTotal = Math.Max(0, desiredPanel1Min) + Math.Max(0, desiredPanel2Min);
        if (requestedTotal <= 0)
        {
            panel1Min = 0;
            panel2Min = 0;
        }
        else if (requestedTotal <= available)
        {
            panel1Min = Math.Clamp(desiredPanel1Min, 0, available);
            panel2Min = Math.Clamp(desiredPanel2Min, 0, available - panel1Min);
        }
        else
        {
            // If the split area is small, scale both panel minimums instead of collapsing one panel to zero.
            float ratio = available / (float)requestedTotal;
            int baseFloor = Math.Min(80, Math.Max(0, available / 2));
            panel1Min = Math.Max(baseFloor, (int)Math.Round(desiredPanel1Min * ratio));
            panel1Min = Math.Clamp(panel1Min, 0, available);
            panel2Min = Math.Max(baseFloor, available - panel1Min);
            panel2Min = Math.Clamp(panel2Min, 0, available - panel1Min);
            panel1Min = Math.Clamp(available - panel2Min, 0, available);
        }

        split.Panel1MinSize = 0;
        split.Panel2MinSize = 0;
        SetSafeSplitterDistance(split, split.SplitterDistance);

        split.Panel1MinSize = panel1Min;
        split.Panel2MinSize = panel2Min;
        SetSafeSplitterDistance(split, split.SplitterDistance);
    }

    private static void SetSafeSplitterDistance(SplitContainer split, int desiredDistance)
    {
        if (split.IsDisposed)
        {
            return;
        }

        int total = split.Orientation == Orientation.Horizontal
            ? split.ClientSize.Height
            : split.ClientSize.Width;
        if (total <= 0)
        {
            return;
        }

        int min = Math.Max(0, split.Panel1MinSize);
        int max = total - Math.Max(0, split.Panel2MinSize) - split.SplitterWidth;
        if (max < min)
        {
            return;
        }

        int value = Math.Clamp(desiredDistance, min, max);
        if (split.SplitterDistance != value)
        {
            split.SplitterDistance = value;
        }
    }

    private static Series CreateLineSeries(string name, Color color)
    {
        return new Series(name)
        {
            ChartType = SeriesChartType.Line,
            BorderWidth = 2,
            Color = color,
            MarkerStyle = MarkerStyle.Circle,
            MarkerSize = 5,
            MarkerColor = color
        };
    }

    private Panel CreateStatCard(string title, Label valueLabel, Color accent)
    {
        var card = new Panel
        {
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(0, 0, 10, 10),
            Width = 190,
            Height = 112
        };
        UiTheme.StyleSectionCard(card, UiTheme.Blend(Color.White, accent, 6));

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = title,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate700
        };

        var hintLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 20,
            Text = "Updated with current filters",
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate500,
            TextAlign = ContentAlignment.BottomLeft
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Font = new Font(UiTheme.HeadingFont.FontFamily, 16F, FontStyle.Bold);
        valueLabel.ForeColor = UiTheme.Blend(UiTheme.Slate900, accent, 30);
        if (string.IsNullOrWhiteSpace(valueLabel.Text))
        {
            valueLabel.Text = "0";
        }

        _cardHints[valueLabel] = hintLabel;
        card.Controls.Add(hintLabel);
        card.Controls.Add(valueLabel);
        card.Controls.Add(titleLabel);
        return card;
    }

    private void HookEvents()
    {
        Load += async (_, __) => await RefreshReportsAsync();
        _apply.Click += async (_, __) =>
        {
            if (_apply.Enabled)
            {
                await RefreshReportsAsync();
            }
        };
        _reset.Click += (_, __) => ResetFilters();
        _refresh.Click += async (_, __) =>
        {
            if (_hasPendingFilterChanges)
            {
                _status.Text = "Apply pending filter changes before refreshing.";
                return;
            }

            await RefreshReportsAsync();
        };

        _preset.SelectedIndexChanged += (_, __) => OnPresetChanged();
        _from.ValueChanged += (_, __) => OnDateRangeChanged();
        _to.ValueChanged += (_, __) => OnDateRangeChanged();
        _purok.SelectedIndexChanged += (_, __) => OnFilterInputChanged();
        _certStatus.SelectedIndexChanged += (_, __) => OnFilterInputChanged();
        _blotterStatus.SelectedIndexChanged += (_, __) => OnFilterInputChanged();

        _export.Click += (_, __) =>
        {
            if (_exportMenu.Items.Count == 0)
            {
                return;
            }

            if (_hasPendingFilterChanges)
            {
                _status.Text = "Export will use the last applied filters.";
            }

            _exportMenu.Show(_export, new Point(0, _export.Height));
        };
    }

    private void ConfigureExportMenu()
    {
        _exportMenu.Items.Clear();

        _exportMenu.Items.Add("Export Excel (.xlsx)", null, async (_, __) => await ExportExcelAsync());
        _exportMenu.Items.Add("Export PDF (.pdf)", null, async (_, __) => await ExportPdfAsync());
    }

    private void InitializeFilterOptions()
    {
        UiTheme.StyleComboBoxes(_preset, _purok, _certStatus, _blotterStatus);

        BindLookupCombo(_certStatus, new System.Collections.Generic.List<LookupItem>
        {
            new LookupItem((int)CertificateStatusFilter.AllNonDraft, "All (non-draft)"),
            new LookupItem((int)CertificateStatusFilter.Pending, "Pending (Submitted+Approved)"),
            new LookupItem((int)CertificateStatusFilter.Submitted, "Submitted"),
            new LookupItem((int)CertificateStatusFilter.Approved, "Approved"),
            new LookupItem((int)CertificateStatusFilter.Released, "Released"),
            new LookupItem((int)CertificateStatusFilter.Cancelled, "Cancelled"),
            new LookupItem((int)CertificateStatusFilter.Rejected, "Rejected")
        });
        _certStatus.SelectedValue = (int)CertificateStatusFilter.AllNonDraft;

        BindLookupCombo(_blotterStatus, new System.Collections.Generic.List<LookupItem>
        {
            new LookupItem((int)BlotterStatusFilter.All, "All"),
            new LookupItem((int)BlotterStatusFilter.Active, "Active (Open/Ongoing)"),
            new LookupItem((int)BlotterStatusFilter.Settled, "Settled"),
            new LookupItem((int)BlotterStatusFilter.Referred, "Referred"),
            new LookupItem((int)BlotterStatusFilter.Closed, "Closed")
        });
        _blotterStatus.SelectedValue = (int)BlotterStatusFilter.All;

        LoadPurokList();

        _toolTip.SetToolTip(_purok, "Filter all report data by the complainant/resident purok.");
        _toolTip.SetToolTip(_certStatus, "Filter the certificate series in trends by current status.");
        _toolTip.SetToolTip(_blotterStatus, "Filter the blotter series in trends by current status.");
        _toolTip.SetToolTip(_apply, "Apply current filters.");
        _toolTip.SetToolTip(_reset, "Reset filters to defaults.");
        _toolTip.SetToolTip(_export, "Export report using applied filters.");
        _toolTip.SetToolTip(_refresh, "Reload data using applied filters.");

        _preset.AccessibleName = "Preset range";
        _from.AccessibleName = "From date";
        _to.AccessibleName = "To date";
        _purok.AccessibleName = "Purok filter";
        _certStatus.AccessibleName = "Certificate status filter";
        _blotterStatus.AccessibleName = "Blotter status filter";
        _apply.AccessibleName = "Apply filters";
        _reset.AccessibleName = "Reset filters";
        _export.AccessibleName = "Export report";
        _refresh.AccessibleName = "Refresh report";
    }

    private void LoadPurokList()
    {
        var items = new System.Collections.Generic.List<LookupItem>
        {
            new LookupItem(0, "All Puroks")
        };

        try
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                "SELECT purok_id, name FROM purok_sitio WHERE barangay_id = @bid ORDER BY name",
                conn);
            cmd.Parameters.AddWithValue("@bid", SchemaDefaults.DefaultBarangayId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.IsDBNull(1) ? $"#{id}" : reader.GetString(1);
                items.Add(new LookupItem(id, name));
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Unable to load purok list for reports filters.", ex);
        }

        BindLookupCombo(_purok, items);
        _purok.SelectedValue = 0;
    }

    private static void BindLookupCombo(ComboBox comboBox, System.Collections.Generic.List<LookupItem> items)
    {
        comboBox.DataSource = null;
        comboBox.DisplayMember = nameof(LookupItem.Name);
        comboBox.ValueMember = nameof(LookupItem.Id);
        comboBox.DataSource = items;
    }

    private int? GetSelectedLookupId(ComboBox comboBox)
    {
        if (comboBox.SelectedValue is int id)
        {
            return id == 0 ? (int?)null : id;
        }

        if (comboBox.SelectedItem is LookupItem item)
        {
            return item.Id == 0 ? (int?)null : item.Id;
        }

        return null;
    }

    private static TEnum GetSelectedEnum<TEnum>(ComboBox comboBox, TEnum fallback) where TEnum : struct, Enum
    {
        int raw = -1;
        if (comboBox.SelectedValue is int id)
        {
            raw = id;
        }
        else if (comboBox.SelectedItem is LookupItem item)
        {
            raw = item.Id;
        }

        if (raw >= 0 && Enum.IsDefined(typeof(TEnum), raw))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), raw);
        }

        return fallback;
    }

    private void ApplyPreset()
    {
        if (_preset.SelectedIndex < 0 || string.Equals(_preset.SelectedItem?.ToString(), "Custom range", StringComparison.Ordinal))
        {
            return;
        }

        DateTime today = DateTime.Today;
        DateTime monthStart = new DateTime(today.Year, today.Month, 1);
        _suspendFilterTracking = true;

        switch (_preset.SelectedItem?.ToString())
        {
            case "This month":
                _from.Value = monthStart;
                _to.Value = today;
                break;
            case "Last 30 days":
                _from.Value = today.AddDays(-29);
                _to.Value = today;
                break;
            case "Last 12 months":
                _from.Value = monthStart.AddMonths(-11);
                _to.Value = today;
                break;
            case "Year to date":
                _from.Value = new DateTime(today.Year, 1, 1);
                _to.Value = today;
                break;
            case "All time":
                // Best-effort: show a wide range without querying storage.
                _from.Value = today.AddYears(-10);
                _to.Value = today;
                break;
            default:
                _from.Value = monthStart.AddMonths(-5);
                _to.Value = today;
                break;
        }

        _suspendFilterTracking = false;
    }

    private void OnPresetChanged()
    {
        if (_suspendFilterTracking)
        {
            return;
        }

        ApplyPreset();
        OnFilterInputChanged();
    }

    private void OnDateRangeChanged()
    {
        if (_suspendFilterTracking)
        {
            return;
        }

        if (!string.Equals(_preset.SelectedItem?.ToString(), "Custom range", StringComparison.Ordinal))
        {
            _suspendFilterTracking = true;
            _preset.SelectedItem = "Custom range";
            _suspendFilterTracking = false;
        }

        OnFilterInputChanged();
    }

    private void OnFilterInputChanged()
    {
        if (_suspendFilterTracking)
        {
            return;
        }

        UpdateApplyState();
        RebuildActiveFilterSummary();
    }

    private void ResetFilters()
    {
        DateTime today = DateTime.Today;
        DateTime start = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

        _suspendFilterTracking = true;
        _preset.SelectedItem = DefaultPreset;
        _from.Value = start;
        _to.Value = today;
        _purok.SelectedValue = 0;
        _certStatus.SelectedValue = (int)CertificateStatusFilter.AllNonDraft;
        _blotterStatus.SelectedValue = (int)BlotterStatusFilter.All;
        _suspendFilterTracking = false;

        OnFilterInputChanged();
    }

    private bool ValidateFilters(out string message)
    {
        if (_from.Value.Date > _to.Value.Date)
        {
            message = "From date cannot be after To date.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void UpdateApplyState()
    {
        bool filtersValid = ValidateFilters(out string message);
        _filterValidation.Text = message;

        _hasPendingFilterChanges = !string.Equals(
            BuildFilterSignature(),
            _appliedFilterSignature,
            StringComparison.Ordinal);

        _apply.Enabled = !_isLoading && filtersValid && _hasPendingFilterChanges;
        _reset.Enabled = !_isLoading && !IsDefaultFilters();
        _refresh.Enabled = !_isLoading;
        _export.Enabled = !_isLoading;

        if (!_isLoading)
        {
            if (!filtersValid)
            {
                _status.Text = message;
            }
            else if (_hasPendingFilterChanges &&
                     (string.IsNullOrWhiteSpace(_status.Text) ||
                      _status.Text.StartsWith("Filters changed", StringComparison.OrdinalIgnoreCase) ||
                      _status.Text.StartsWith("Ready", StringComparison.OrdinalIgnoreCase) ||
                      _status.Text.StartsWith("Updated", StringComparison.OrdinalIgnoreCase) ||
                      _status.Text.StartsWith("Applied", StringComparison.OrdinalIgnoreCase)))
            {
                _status.Text = "Filters changed. Click Apply to refresh the report.";
            }
        }
    }

    private bool IsDefaultFilters()
    {
        DateTime today = DateTime.Today;
        DateTime start = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        bool rangeDefault = _from.Value.Date == start && _to.Value.Date == today;
        bool purokDefault = !GetSelectedLookupId(_purok).HasValue;
        bool certDefault = GetSelectedEnum(_certStatus, CertificateStatusFilter.AllNonDraft) == CertificateStatusFilter.AllNonDraft;
        bool blotterDefault = GetSelectedEnum(_blotterStatus, BlotterStatusFilter.All) == BlotterStatusFilter.All;
        bool presetDefault = string.Equals(_preset.SelectedItem?.ToString(), DefaultPreset, StringComparison.Ordinal) ||
                             (string.Equals(_preset.SelectedItem?.ToString(), "Custom range", StringComparison.Ordinal) && rangeDefault);
        return rangeDefault && purokDefault && certDefault && blotterDefault && presetDefault;
    }

    private string BuildFilterSignature()
    {
        int? purokId = GetSelectedLookupId(_purok);
        int certStatus = (int)GetSelectedEnum(_certStatus, CertificateStatusFilter.AllNonDraft);
        int blotterStatus = (int)GetSelectedEnum(_blotterStatus, BlotterStatusFilter.All);
        string preset = _preset.SelectedItem?.ToString() ?? string.Empty;
        return string.Join("|",
            preset,
            _from.Value.Date.ToString("yyyyMMdd"),
            _to.Value.Date.ToString("yyyyMMdd"),
            purokId?.ToString() ?? "all",
            certStatus.ToString(),
            blotterStatus.ToString());
    }

    private void CaptureAppliedFilterState()
    {
        _appliedFilterSignature = BuildFilterSignature();
        _lastAppliedAt = DateTime.Now;
        _appliedStamp.Text = $"Applied {_lastAppliedAt:MMM dd, yyyy hh:mm tt}";
        _chartLastUpdated.Text = $"Data as of {_lastAppliedAt:MMM dd, yyyy hh:mm tt}";
        UpdateApplyState();
        RebuildActiveFilterSummary();
    }

    private void RebuildActiveFilterSummary()
    {
        _activeFilters.SuspendLayout();
        _activeFilters.Controls.Clear();

        _activeFilters.Controls.Add(CreateFilterChip($"Range: {_from.Value:MMM dd, yyyy} - {_to.Value:MMM dd, yyyy}"));

        string preset = _preset.SelectedItem?.ToString() ?? "Custom range";
        if (!string.Equals(preset, "Custom range", StringComparison.Ordinal))
        {
            _activeFilters.Controls.Add(CreateFilterChip($"Preset: {preset}"));
        }

        int? purokId = GetSelectedLookupId(_purok);
        if (purokId.HasValue)
        {
            _activeFilters.Controls.Add(CreateFilterChip($"Purok: {GetComboDisplayText(_purok, "Selected")}"));
        }

        var certStatus = GetSelectedEnum(_certStatus, CertificateStatusFilter.AllNonDraft);
        if (certStatus != CertificateStatusFilter.AllNonDraft)
        {
            _activeFilters.Controls.Add(CreateFilterChip($"Cert: {GetComboDisplayText(_certStatus, certStatus.ToString())}"));
        }

        var blotterStatus = GetSelectedEnum(_blotterStatus, BlotterStatusFilter.All);
        if (blotterStatus != BlotterStatusFilter.All)
        {
            _activeFilters.Controls.Add(CreateFilterChip($"Blotter: {GetComboDisplayText(_blotterStatus, blotterStatus.ToString())}"));
        }

        if (_hasPendingFilterChanges)
        {
            _activeFilters.Controls.Add(CreateFilterChip("Pending changes", UiTheme.AccentBlue));
        }

        if (_activeFilters.Controls.Count == 0)
        {
            _activeFilters.Controls.Add(CreateFilterChip("All default filters"));
        }

        _activeFilters.ResumeLayout();
    }

    private static string GetComboDisplayText(ComboBox comboBox, string fallback)
    {
        if (comboBox.SelectedItem is LookupItem item)
        {
            return item.Name;
        }

        if (!string.IsNullOrWhiteSpace(comboBox.Text))
        {
            return comboBox.Text;
        }

        return fallback;
    }

    private static Label CreateFilterChip(string text, Color? accent = null)
    {
        Color blendAccent = accent ?? UiTheme.Slate300;
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate700,
            BackColor = UiTheme.Blend(Color.White, blendAccent, 12),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 0)
        };
    }

    private async Task RefreshReportsAsync()
    {
        if (_isLoading)
        {
            return;
        }

        if (!ValidateFilters(out string validationMessage))
        {
            _filterValidation.Text = validationMessage;
            _status.Text = validationMessage;
            UpdateApplyState();
            return;
        }

        DateTime from = _from.Value.Date;
        DateTime to = _to.Value.Date;

        SetLoading(true, $"Loading reports ({from:MMM dd, yyyy} to {to:MMM dd, yyyy})...");
        SetTrendState(
            "Loading trend data",
            "Preparing monthly trend chart for the selected filters.",
            showRetry: false);

        ReportsDashboardData data;
        try
        {
            var filters = new ReportsFilters
            {
                PurokId = GetSelectedLookupId(_purok),
                CertificateStatus = GetSelectedEnum(_certStatus, CertificateStatusFilter.AllNonDraft),
                BlotterStatus = GetSelectedEnum(_blotterStatus, BlotterStatusFilter.All)
            };

            data = await Task.Run(() => ReportsService.LoadDashboard(from, to, filters)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Unable to load reports dashboard.", ex);
            BindSummary(new ReportsSummary());
            BindServiceTimes(new ServiceTimeMetrics());
            BindTrends(Array.Empty<MonthlyTrendRow>());
            BindStaffPerformance(Array.Empty<StaffPerformanceRow>());
            BindHotspots(Array.Empty<HotspotPoint>());
            _lastData = null;
            _lastFrom = from;
            _lastTo = to;
            SetTrendState(
                "Trend data unavailable",
                "The chart could not be loaded right now. You can retry once the connection is available.",
                showRetry: true);
            ShowInlineState(
                "Connection unavailable",
                "Reports could not load due to a connection issue. Retry once the connection is restored.",
                IconChar.Wifi,
                UiTheme.AccentOrange,
                "Retry",
                () => _ = RefreshReportsAsync());
            SetLoading(false, "Unable to load reports. Check your connection and try again.");
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        BindSummary(data.Summary);
        BindServiceTimes(data.ServiceTimes);
        BindTrends(data.Trends);
        BindStaffPerformance(data.StaffPerformance);
        BindHotspots(data.Hotspots);

        if ((data.Trends?.Count ?? 0) == 0 && (data.StaffPerformance?.Count ?? 0) == 0)
        {
            ShowInlineState(
                "No records in this range",
                "No residents, certificates, or blotter data matched the selected filters.",
                IconChar.Inbox,
                UiTheme.Slate500,
                "Reset filters",
                () =>
                {
                    ResetFilters();
                    _ = RefreshReportsAsync();
                });
        }
        else
        {
            ClearInlineState();
        }

        _lastData = data;
        _lastFrom = from;
        _lastTo = to;
        CaptureAppliedFilterState();

        SetLoading(false, $"Updated {DateTime.Now:MMM dd, yyyy hh:mm tt}");
    }

    private void ShowInlineState(
        string title,
        string message,
        IconChar icon,
        Color accent,
        string? primaryActionText = null,
        Action? primaryAction = null)
    {
        ClearInlineState();
        _inlineStateCard = UiTheme.CreateStateCard(
            title,
            message,
            icon,
            accent,
            primaryActionText,
            primaryAction);
        _inlineStateCard.Width = Math.Max(220, _cards.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        _inlineStateCard.Margin = new Padding(0, 0, 0, 10);
        _cards.Controls.Add(_inlineStateCard);
        _cards.Controls.SetChildIndex(_inlineStateCard, 0);
    }

    private void ClearInlineState()
    {
        if (_inlineStateCard == null)
        {
            return;
        }

        if (_cards.Controls.Contains(_inlineStateCard))
        {
            _cards.Controls.Remove(_inlineStateCard);
        }

        _inlineStateCard.Dispose();
        _inlineStateCard = null;
    }

    private void SetTrendState(string title, string message, bool showRetry)
    {
        _trendStateTitle.Text = title;
        _trendStateMessage.Text = message;
        _trendRetry.Visible = showRetry;
        _trendStateHost.Visible = true;
        _trendStateHost.BringToFront();
    }

    private void HideTrendState()
    {
        _trendStateHost.Visible = false;
    }

    private void SetLoading(bool loading, string statusText)
    {
        _isLoading = loading;
        _preset.Enabled = !loading;
        _from.Enabled = !loading;
        _to.Enabled = !loading;
        _purok.Enabled = !loading;
        _certStatus.Enabled = !loading;
        _blotterStatus.Enabled = !loading;
        _status.Text = statusText;
        Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
        if (loading)
        {
            _loadingOverlay.ShowLoading(statusText);
        }
        else
        {
            _loadingOverlay.HideLoading();
        }

        UpdateApplyState();
    }

    private void BindSummary(ReportsSummary summary)
    {
        _newResidentsValue.Text = summary.NewResidents.ToString("N0");
        _certRequestsValue.Text = summary.CertificateRequests.ToString("N0");
        _certReleasedValue.Text = summary.CertificatesReleased.ToString("N0");
        _blottersFiledValue.Text = summary.BlottersFiled.ToString("N0");
        _pendingCertsValue.Text = summary.PendingCertificates.ToString("N0");
        _activeBlottersValue.Text = summary.ActiveBlotters.ToString("N0");

        if (_cardHints.TryGetValue(_newResidentsValue, out Label? residentsHint))
        {
            residentsHint.Text = "Registered in selected range";
        }

        if (_cardHints.TryGetValue(_certRequestsValue, out Label? requestsHint))
        {
            requestsHint.Text = "Requested in selected range";
        }

        if (_cardHints.TryGetValue(_certReleasedValue, out Label? releasedHint))
        {
            releasedHint.Text = "Released in selected range";
        }

        if (_cardHints.TryGetValue(_blottersFiledValue, out Label? blotterHint))
        {
            blotterHint.Text = "Filed in selected range";
        }

        if (_cardHints.TryGetValue(_pendingCertsValue, out Label? pendingHint))
        {
            pendingHint.Text = "Currently pending";
        }

        if (_cardHints.TryGetValue(_activeBlottersValue, out Label? activeHint))
        {
            activeHint.Text = "Currently active";
        }
    }

    private void BindServiceTimes(ServiceTimeMetrics metrics)
    {
        _avgApprovalValue.Text = FormatDuration(metrics.AvgRequestToApprovalSeconds);
        _avgReleaseValue.Text = FormatDuration(metrics.AvgApprovalToReleaseSeconds);

        if (_cardHints.TryGetValue(_avgApprovalValue, out Label? approvalHint))
        {
            approvalHint.Text = metrics.ApprovalSamples > 0
                ? $"Samples: {metrics.ApprovalSamples:N0}"
                : "No approvals in selected range";
        }

        if (_cardHints.TryGetValue(_avgReleaseValue, out Label? releaseHint))
        {
            releaseHint.Text = metrics.ReleaseSamples > 0
                ? $"Samples: {metrics.ReleaseSamples:N0}"
                : "No releases in selected range";
        }

        _toolTip.SetToolTip(
            _avgApprovalValue,
            metrics.ApprovalSamples > 0
                ? $"Average time from request to approval.\r\nSamples: {metrics.ApprovalSamples:N0}"
                : "No approvals in the selected date range.");

        _toolTip.SetToolTip(
            _avgReleaseValue,
            metrics.ReleaseSamples > 0
                ? $"Average time from approval to release.\r\nSamples: {metrics.ReleaseSamples:N0}"
                : "No releases in the selected date range.");
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds <= 0)
        {
            return "N/A";
        }

        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalMinutes < 1)
        {
            return "<1m";
        }

        if (ts.TotalHours < 1)
        {
            return $"{ts.TotalMinutes:0}m";
        }

        if (ts.TotalDays < 1)
        {
            return $"{ts.TotalHours:0.#}h";
        }

        int days = (int)Math.Floor(ts.TotalDays);
        if (days < 10 && ts.Hours > 0)
        {
            return $"{days}d {ts.Hours}h";
        }

        return $"{ts.TotalDays:0.#}d";
    }

    private void BindTrends(System.Collections.Generic.IReadOnlyList<MonthlyTrendRow> trends)
    {
        var rows = trends ?? Array.Empty<MonthlyTrendRow>();
        var table = new DataTable();
        table.Columns.Add("Month", typeof(string));
        table.Columns.Add("Residents", typeof(int));
        table.Columns.Add("Certificates", typeof(int));
        table.Columns.Add("Blotter", typeof(int));

        foreach (MonthlyTrendRow row in rows)
        {
            table.Rows.Add(row.MonthLabel, row.Residents, row.Certificates, row.Blotters);
        }

        _monthlyGrid.DataSource = table;
        UpdateGridEmptyState(_monthlyGrid, _monthlyEmptyState);

        var residents = _trendChart.Series["Residents"];
        var certs = _trendChart.Series["Certificates"];
        var blotter = _trendChart.Series["Blotter"];
        residents.Points.Clear();
        certs.Points.Clear();
        blotter.Points.Clear();

        foreach (MonthlyTrendRow row in rows)
        {
            residents.Points.AddXY(row.MonthLabel, row.Residents);
            certs.Points.AddXY(row.MonthLabel, row.Certificates);
            blotter.Points.AddXY(row.MonthLabel, row.Blotters);
        }

        if (rows.Count == 0)
        {
            SetTrendState(
                "No chart data",
                "No trend data for the current filters. Try widening the date range or resetting filters.",
                showRetry: false);
        }
        else
        {
            HideTrendState();
        }

        // Improve grid display order/widths.
        if (_monthlyGrid.Columns.Count >= 4)
        {
            _monthlyGrid.Columns[0].FillWeight = 140;
            _monthlyGrid.Columns[1].FillWeight = 90;
            _monthlyGrid.Columns[2].FillWeight = 90;
            _monthlyGrid.Columns[3].FillWeight = 90;
        }
    }

    private void BindStaffPerformance(System.Collections.Generic.IReadOnlyList<StaffPerformanceRow> staff)
    {
        var table = new DataTable();
        table.Columns.Add("User", typeof(string));
        table.Columns.Add("Completed", typeof(int));
        table.Columns.Add("Overdue", typeof(int));

        table.Columns.Add("Cert Approvals", typeof(int));
        table.Columns.Add("Approval Overdue", typeof(int));
        table.Columns.Add("Avg Req->Approve", typeof(string));

        table.Columns.Add("Cert Releases", typeof(int));
        table.Columns.Add("Release Overdue", typeof(int));
        table.Columns.Add("Avg Approve->Release", typeof(string));

        table.Columns.Add("Blotter Updates", typeof(int));
        table.Columns.Add("Resolutions", typeof(int));
        table.Columns.Add("Resolution Overdue", typeof(int));
        table.Columns.Add("Avg Resolution", typeof(string));

        table.Columns.Add("_active_sort", typeof(int));

        foreach (StaffPerformanceRow row in staff ?? Array.Empty<StaffPerformanceRow>())
        {
            bool hasAny = row.ApprovalsCompleted > 0 ||
                          row.ReleasesCompleted > 0 ||
                          row.BlotterStatusChanges > 0 ||
                          row.BlotterResolutions > 0;
            if (!row.IsActive && !hasAny)
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(row.DisplayName) ? row.Username : row.DisplayName;
            if (!string.IsNullOrWhiteSpace(row.Username) && !string.Equals(name, row.Username, StringComparison.OrdinalIgnoreCase))
            {
                name = $"{row.Username} ({name})";
            }
            if (!row.IsActive)
            {
                name += " [inactive]";
            }

            int completed = row.ApprovalsCompleted + row.ReleasesCompleted + row.BlotterResolutions;
            int overdue = row.ApprovalsOverdue + row.ReleasesOverdue + row.BlotterResolutionsOverdue;

            table.Rows.Add(
                name,
                completed,
                overdue,
                row.ApprovalsCompleted,
                row.ApprovalsOverdue,
                FormatDuration(row.AvgRequestToApprovalSeconds),
                row.ReleasesCompleted,
                row.ReleasesOverdue,
                FormatDuration(row.AvgApprovalToReleaseSeconds),
                row.BlotterStatusChanges,
                row.BlotterResolutions,
                row.BlotterResolutionsOverdue,
                FormatDuration(row.AvgBlotterResolutionSeconds),
                row.IsActive ? 1 : 0);
        }

        var view = table.DefaultView;
        view.Sort = "_active_sort DESC, Completed DESC, Overdue ASC, User ASC";
        _staffGrid.DataSource = view;
        UpdateGridEmptyState(_staffGrid, _staffEmptyState);

        if (_staffGrid.Columns.Contains("_active_sort"))
        {
            _staffGrid.Columns["_active_sort"].Visible = false;
        }

        if (_staffGrid.Columns.Count > 0)
        {
            void SetWeight(string column, float weight)
            {
                if (_staffGrid.Columns.Contains(column))
                {
                    _staffGrid.Columns[column].FillWeight = weight;
                }
            }

            SetWeight("User", 170);
            SetWeight("Completed", 72);
            SetWeight("Overdue", 72);
            SetWeight("Cert Approvals", 92);
            SetWeight("Approval Overdue", 94);
            SetWeight("Avg Req->Approve", 110);
            SetWeight("Cert Releases", 92);
            SetWeight("Release Overdue", 94);
            SetWeight("Avg Approve->Release", 120);
            SetWeight("Blotter Updates", 98);
            SetWeight("Resolutions", 82);
            SetWeight("Resolution Overdue", 106);
            SetWeight("Avg Resolution", 96);
        }
    }

    private void BindHotspots(IReadOnlyList<HotspotPoint> hotspots)
    {
        _hotspotPoints = hotspots ?? Array.Empty<HotspotPoint>();

        var table = new DataTable();
        table.Columns.Add("Purok", typeof(string));
        table.Columns.Add("Incidents", typeof(int));
        table.Columns.Add("Latitude", typeof(string));
        table.Columns.Add("Longitude", typeof(string));

        foreach (HotspotPoint point in _hotspotPoints)
        {
            table.Rows.Add(
                string.IsNullOrWhiteSpace(point.PurokName) ? $"Purok #{point.PurokId}" : point.PurokName,
                point.IncidentCount,
                point.Latitude.HasValue ? $"{point.Latitude.Value:0.00000} deg" : "-",
                point.Longitude.HasValue ? $"{point.Longitude.Value:0.00000} deg" : "-");
        }

        _hotspotGrid.DataSource = table;
        UpdateGridEmptyState(_hotspotGrid, _hotspotEmptyState);
        if (_hotspotGrid.Columns.Count >= 4)
        {
            _hotspotGrid.Columns[0].FillWeight = 160;
            _hotspotGrid.Columns[1].FillWeight = 70;
            _hotspotGrid.Columns[2].FillWeight = 90;
            _hotspotGrid.Columns[3].FillWeight = 90;
        }

        _hotspotLegend.Text = _hotspotPoints.Count == 0
            ? "No hotspot data for selected filters."
            : "Larger/redder circles indicate higher incident density.";

        _hotspotMapPanel.Invalidate();
    }

    private void HotspotMapPanel_Paint(object? sender, PaintEventArgs e)
    {
        Rectangle bounds = _hotspotMapPanel.ClientRectangle;
        e.Graphics.Clear(UiTheme.Blend(Color.White, UiTheme.AccentBlue, 6));
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        _hotspotHitTargets.Clear();

        if (_hotspotPoints.Count == 0)
        {
            using var emptyBrush = new SolidBrush(UiTheme.Slate500);
            using var emptyFont = new Font(UiTheme.BodyFont, FontStyle.Italic);
            TextRenderer.DrawText(
                e.Graphics,
                "No hotspot data for selected filters.",
                emptyFont,
                bounds,
                UiTheme.Slate500,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        int pad = 22;
        Rectangle plot = Rectangle.Inflate(bounds, -pad, -pad);
        if (plot.Width <= 20 || plot.Height <= 20)
        {
            return;
        }

        using (var gridPen = new Pen(Color.FromArgb(36, UiTheme.Slate600)))
        {
            for (int i = 1; i < 4; i++)
            {
                int x = plot.Left + (plot.Width * i / 4);
                int y = plot.Top + (plot.Height * i / 4);
                e.Graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            }
        }

        int maxCount = 1;
        foreach (HotspotPoint point in _hotspotPoints)
        {
            if (point.IncidentCount > maxCount)
            {
                maxCount = point.IncidentCount;
            }
        }

        var geoPoints = _hotspotPoints.Where(p => p.Latitude.HasValue && p.Longitude.HasValue).ToList();
        bool useGeo = geoPoints.Count >= 2;
        double minLat = 0;
        double maxLat = 0;
        double minLon = 0;
        double maxLon = 0;
        if (useGeo)
        {
            minLat = geoPoints.Min(p => p.Latitude!.Value);
            maxLat = geoPoints.Max(p => p.Latitude!.Value);
            minLon = geoPoints.Min(p => p.Longitude!.Value);
            maxLon = geoPoints.Max(p => p.Longitude!.Value);
            useGeo = (maxLat - minLat) > 0.000001 && (maxLon - minLon) > 0.000001;
        }

        int fallbackCols = (int)Math.Ceiling(Math.Sqrt(_hotspotPoints.Count));
        int fallbackRows = Math.Max(1, (int)Math.Ceiling(_hotspotPoints.Count / (double)fallbackCols));

        for (int i = 0; i < _hotspotPoints.Count; i++)
        {
            HotspotPoint point = _hotspotPoints[i];
            float px;
            float py;

            if (useGeo && point.Latitude.HasValue && point.Longitude.HasValue)
            {
                double lonSpan = maxLon - minLon;
                double latSpan = maxLat - minLat;
                double nx = lonSpan <= 0 ? 0.5 : (point.Longitude.Value - minLon) / lonSpan;
                double ny = latSpan <= 0 ? 0.5 : (maxLat - point.Latitude.Value) / latSpan;
                px = plot.Left + (float)(nx * plot.Width);
                py = plot.Top + (float)(ny * plot.Height);
            }
            else
            {
                int col = i % fallbackCols;
                int row = i / fallbackCols;
                px = plot.Left + ((col + 0.5f) * plot.Width / fallbackCols);
                py = plot.Top + ((row + 0.5f) * plot.Height / fallbackRows);
            }

            float scale = Math.Min(1f, point.IncidentCount / (float)maxCount);
            float radius = 10f + (scale * 26f);
            RectangleF dot = new RectangleF(px - radius, py - radius, radius * 2f, radius * 2f);
            Color fill = InterpolateHotspotColor(scale);

            using (var fillBrush = new SolidBrush(Color.FromArgb(170, fill)))
            using (var borderPen = new Pen(Color.FromArgb(220, fill), 2f))
            {
                e.Graphics.FillEllipse(fillBrush, dot);
                e.Graphics.DrawEllipse(borderPen, dot.X, dot.Y, dot.Width, dot.Height);
            }

            string countText = point.IncidentCount.ToString();
            TextRenderer.DrawText(
                e.Graphics,
                countText,
                UiTheme.SmallFont,
                Rectangle.Round(dot),
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            string label = string.IsNullOrWhiteSpace(point.PurokName) ? $"Purok #{point.PurokId}" : point.PurokName;
            var labelRect = new Rectangle((int)(px + radius + 4), (int)(py - 8), 180, 18);
            TextRenderer.DrawText(e.Graphics, label, UiTheme.SmallFont, labelRect, UiTheme.Slate700);

            _hotspotHitTargets.Add((dot, point));
        }
    }

    private void HotspotMapPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        foreach ((RectangleF bounds, HotspotPoint point) in _hotspotHitTargets)
        {
            if (!bounds.Contains(e.Location))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(point.PurokName) ? $"Purok #{point.PurokId}" : point.PurokName;
            string tooltip = $"{name}\nIncidents: {point.IncidentCount:N0}";
            if (!string.Equals(_hotspotTooltipText, tooltip, StringComparison.Ordinal))
            {
                _hotspotTooltipText = tooltip;
                _toolTip.Show(tooltip, _hotspotMapPanel, e.Location.X + 14, e.Location.Y + 14, 2000);
            }
            return;
        }

        _hotspotTooltipText = string.Empty;
        _toolTip.Hide(_hotspotMapPanel);
    }

    private static Color InterpolateHotspotColor(float scale)
    {
        scale = Math.Clamp(scale, 0f, 1f);
        Color cool = Color.FromArgb(36, 124, 255);
        Color warm = Color.FromArgb(232, 52, 52);
        int r = (int)(cool.R + (warm.R - cool.R) * scale);
        int g = (int)(cool.G + (warm.G - cool.G) * scale);
        int b = (int)(cool.B + (warm.B - cool.B) * scale);
        return Color.FromArgb(r, g, b);
    }

    private async Task<bool> EnsureExportDataAsync()
    {
        if (_lastData != null)
        {
            return true;
        }

        if (_hasPendingFilterChanges)
        {
            return false;
        }

        await RefreshReportsAsync();
        return _lastData != null;
    }

    private async Task ExportExcelAsync()
    {
        if (_isLoading)
        {
            return;
        }

        bool ok = await EnsureExportDataAsync();
        if (!ok || _lastData == null)
        {
            ControllerDialogs.Warning(
                _hasPendingFilterChanges
                    ? "Apply filters first, then export."
                    : "No report data available to export.",
                "Export");
            return;
        }

        string defaultName = $"reports-{_lastFrom:yyyyMMdd}-{_lastTo:yyyyMMdd}.xlsx";
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = SanitizeFileName(defaultName),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        string path = dialog.FileName;
        SetLoading(true, "Exporting Excel...");
        try
        {
            await Task.Run(() => ReportsExportService.ExportDashboardExcel(_lastData, _lastFrom, _lastTo, path));
            ControllerDialogs.Info($"Exported Excel report to:\r\n{path}", "Export");
            SetLoading(false, $"Exported Excel: {DateTime.Now:MMM dd, yyyy hh:mm tt}");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Excel export failed.", ex);
            ControllerDialogs.Error(ex, "Unable to export Excel report.", "Export");
            SetLoading(false, "Export failed.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (_isLoading)
        {
            return;
        }

        bool ok = await EnsureExportDataAsync();
        if (!ok || _lastData == null)
        {
            ControllerDialogs.Warning(
                _hasPendingFilterChanges
                    ? "Apply filters first, then export."
                    : "No report data available to export.",
                "Export");
            return;
        }

        string defaultName = $"reports-{_lastFrom:yyyyMMdd}-{_lastTo:yyyyMMdd}.pdf";
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = SanitizeFileName(defaultName),
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        string path = dialog.FileName;
        SetLoading(true, "Exporting PDF...");
        try
        {
            await Task.Run(() => ReportsExportService.ExportDashboardPdf(_lastData, _lastFrom, _lastTo, path));
            ControllerDialogs.Info($"Exported PDF report to:\r\n{path}", "Export");
            SetLoading(false, $"Exported PDF: {DateTime.Now:MMM dd, yyyy hh:mm tt}");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("PDF export failed.", ex);
            ControllerDialogs.Error(ex, "Unable to export PDF report.", "Export");
            SetLoading(false, "Export failed.");
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "report";
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '-');
        }

        return name.Trim();
    }
}
