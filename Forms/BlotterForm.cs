using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal partial class BlotterForm : Form
{
    private const string SelectRespondentPlaceholder = "-- Select resident respondent --";
    private static readonly Size FilingClientSize = new(970, 720);
    private static readonly Size FilingMinimumSize = new(988, 770);
    private static readonly Size ReviewClientSize = new(970, 1040);
    private static readonly Size ReviewMinimumSize = new(988, 1080);

    private readonly int _complainantId;
    private readonly string? _complainantName;
    private readonly BlotterFormController _controller;
    private readonly AiBlotterService _aiService;
    private int? _blotterIdForAnalysis;
    private string _originalStatus = "Ongoing";
    private bool _wasUpdated;
    private readonly PrintDocument _printDocument = new PrintDocument();
    private PrintPreviewDialog? _printPreviewDialog;
    private System.Windows.Forms.Timer? _timelineSearchDebounceTimer;
    private Button? _btnScheduleMediation;
    private SplitContainer? _reviewSplitMain;
    private Button? _btnToggleAiPanel;
    private Button? _btnCloseReview;
    private FlowLayoutPanel? _flpCaseActions;
    private Label? _lblCaseMeta;
    private Label? _lblCaseTitle;
    private Label? _lblRespondentSummary;
    private Label? _lblQuickStatusValue;
    private Label? _lblReviewFormHint;
    private Label? _lblReviewValidation;
    private Label? _lblQuickCaseIdValue;
    private Label? _lblQuickRecordedByValue;
    private Label? _lblQuickLastUpdatedValue;
    private DataGridView? _timelineGrid;
    private TextBox? _txtTimelineSearch;
    private DataTable? _timelineDisplayTable;
    private DataGridView? _witnessGrid;
    private ListView? _attachmentsList;
    private ErrorProvider? _reviewErrorProvider;
    private ToolTip? _reviewToolTip;
    private bool _reviewValidationEventsWired;
    private bool _reviewLayoutBuilt;

    internal int? AnalysisBlotterId => _blotterIdForAnalysis;
    internal AiBlotterService AiService => _aiService;
    internal bool WasUpdated => _wasUpdated;

    public BlotterDto Blotter => new BlotterDto
    {
        ComplainantId = _complainantId,
        RespondentResidentId = GetRespondentResidentId(),
        RespondentName = GetRespondentName(),
        IncidentType = txtIncidentType.Text.Trim(),
        IncidentDate = dtpIncidentDate.Value.Date,
        IncidentTime = dtpIncidentTime.Value.TimeOfDay,
        IncidentLocation = txtIncidentLocation.Text.Trim(),
        Witnesses = txtWitnesses.Text.Trim(),
        ActionTaken = txtActionTaken.Text.Trim(),
        ResolutionDetails = txtResolution.Text.Trim(),
        IncidentDetails = txtIncidentDetails.Text.Trim(),
        Status = cmbStatus.SelectedItem?.ToString() ?? "Ongoing",
        RecordedBy = UserSession.UserId
    };

    public BlotterForm(
        int complainantId,
        string? complainantName,
        IEnumerable<string>? respondentSuggestions = null,
        int? blotterIdForAnalysis = null,
        AiBlotterService? aiService = null)
    {
        _complainantId = complainantId;
        _complainantName = complainantName;
        _blotterIdForAnalysis = blotterIdForAnalysis;
        _aiService = aiService ?? new AiBlotterService();

        InitializeComponent();
        _controller = new BlotterFormController(this);

        ApplyTheme();
        InitializeMediationActions();
        ConfigurePrint();
        dtpIncidentTime.Value = DateTime.Now;
        if (cmbStatus.Items.Count > 0 && cmbStatus.SelectedIndex < 0)
        {
            cmbStatus.SelectedIndex = 0;
        }

        if (!_blotterIdForAnalysis.HasValue)
        {
            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("Ongoing");
            cmbStatus.SelectedIndex = 0;
        }
        else
        {
            if (!cmbStatus.Items.Contains("Closed"))
            {
                cmbStatus.Items.Add("Closed");
            }
        }

        PopulateComplainant();
        LoadRespondentSuggestions(respondentSuggestions);
        UpdateRespondentMode();
        InitializeAiState();
        ApplyReviewModeLayout();
        UpdateStatusBadge();
        UpdateResolutionVisibility();
        ReloadTimeline();

        cmbStatus.SelectedIndexChanged -= StatusSelectionChanged;
        cmbStatus.SelectedIndexChanged += StatusSelectionChanged;
    }

    private void EnsureReviewLayoutBuilt()
    {
        if (_reviewLayoutBuilt)
        {
            return;
        }

        _reviewLayoutBuilt = true;
        SuspendLayout();
        try
        {
            Controls.Clear();
            Padding = new Padding(12);

            _reviewSplitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                // Apply min sizes and splitter distance only after the control has a measured width.
                Panel1MinSize = 0,
                Panel2MinSize = 0,
                SplitterWidth = 6
            };
            _reviewSplitMain.Resize += (_, _) => UpdateReviewSplitDistance();

            leftPanel.Controls.Clear();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Padding = Padding.Empty;
            leftPanel.BackColor = UiTheme.Slate50;
            leftPanel.AutoScroll = false;

            rightPanel.Controls.Clear();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Padding = Padding.Empty;
            rightPanel.BackColor = UiTheme.Slate50;
            rightPanel.AutoScroll = false;

            TableLayoutPanel leftRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                ColumnCount = 1,
                RowCount = 2
            };
            leftRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
            leftRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel pnlCaseHeader = BuildReviewCaseHeader();
            TabControl tcCase = BuildReviewTabControl();

            leftRoot.Controls.Add(pnlCaseHeader, 0, 0);
            leftRoot.Controls.Add(tcCase, 0, 1);
            leftPanel.Controls.Add(leftRoot);

            BuildReviewAiPanel();

            _reviewSplitMain.Panel1.Controls.Add(leftPanel);
            _reviewSplitMain.Panel2.Controls.Add(rightPanel);
            Controls.Add(_reviewSplitMain);
        }
        finally
        {
            ResumeLayout(performLayout: true);
        }

        BuildWitnessGridFromText();
        UpdateReviewMeta();
        UpdateReviewSplitDistance();
        UpdateAiPanelToggleText();
        WireReviewValidationEvents();
        ValidateReviewInputs(showMessages: false, out _, out _);
        UiTheme.EnhanceAccessibility(this);
    }

    private void UpdateReviewSplitDistance()
    {
        if (_reviewSplitMain == null || _reviewSplitMain.IsDisposed)
        {
            return;
        }

        if (_reviewSplitMain.Panel2Collapsed)
        {
            return;
        }

        int totalWidth = _reviewSplitMain.ClientSize.Width;
        if (totalWidth <= 0)
        {
            return;
        }

        int splitterWidth = Math.Max(4, _reviewSplitMain.SplitterWidth);
        int available = Math.Max(0, totalWidth - splitterWidth);

        const int desiredLeftMin = 760;
        const int desiredRightMin = 420;
        int minLeft = desiredLeftMin;
        int minRight = desiredRightMin;
        if (available < (desiredLeftMin + desiredRightMin))
        {
            // Keep the UI usable on narrower displays instead of throwing split-container exceptions.
            minLeft = Math.Max(280, available / 2);
            minRight = Math.Max(220, available - minLeft);
            if ((minLeft + minRight) > available)
            {
                minLeft = Math.Max(0, available - minRight);
            }
            if ((minLeft + minRight) > available)
            {
                minRight = Math.Max(0, available - minLeft);
            }
        }

        try
        {
            _reviewSplitMain.Panel1MinSize = minLeft;
            _reviewSplitMain.Panel2MinSize = minRight;
        }
        catch
        {
            _reviewSplitMain.Panel1MinSize = 0;
            _reviewSplitMain.Panel2MinSize = 0;
        }

        int minDistance = Math.Max(0, _reviewSplitMain.Panel1MinSize);
        int maxDistance = Math.Max(minDistance, available - Math.Max(0, _reviewSplitMain.Panel2MinSize));
        int preferred = available >= 1400 ? 980 : (int)Math.Round(available * 0.62);
        int target = Math.Clamp(preferred, minDistance, maxDistance);
        if (_reviewSplitMain.Panel1MinSize == 0 && _reviewSplitMain.Panel2MinSize == 0)
        {
            target = Math.Clamp(Math.Min(980, Math.Max(240, available / 2)), 0, Math.Max(0, available));
        }

        try
        {
            _reviewSplitMain.SplitterDistance = target;
        }
        catch
        {
            // Ignore transient layout exceptions while the form is resizing.
        }
    }

    private Panel BuildReviewCaseHeader()
    {
        Panel panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        UiTheme.StyleSectionCard(panel, backColor: Color.White, enforceBorder: true, padding: new Padding(12));

        TableLayoutPanel headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 2,
            RowCount = 1
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        TableLayoutPanel leftInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 1,
            RowCount = 3
        };
        leftInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        leftInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        leftInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        leftInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));

        _lblCaseTitle = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = UiTheme.Slate900,
            Text = "Case: -"
        };

        _lblCaseMeta = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Slate700,
            Text = "Blotter Case # - | Filed: -"
        };

        _lblRespondentSummary = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate600,
            Text = "Respondent: - | Last updated: -"
        };

        leftInfo.Controls.Add(_lblCaseTitle, 0, 0);
        leftInfo.Controls.Add(_lblCaseMeta, 0, 1);
        leftInfo.Controls.Add(_lblRespondentSummary, 0, 2);

        TableLayoutPanel rightActions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 1,
            RowCount = 2
        };
        rightActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rightActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        rightActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        TableLayoutPanel statusRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 3,
            RowCount = 1
        };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148F));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        statusRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Label lblStatusHeader = new Label
        {
            AutoSize = true,
            Text = "Status",
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0),
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate700
        };

        cmbStatus.Dock = DockStyle.Fill;
        cmbStatus.Margin = new Padding(0, 0, 8, 0);
        cmbStatus.MinimumSize = new Size(120, 0);
        cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;

        lblStatusBadge.AutoSize = true;
        lblStatusBadge.Padding = new Padding(10, 2, 10, 2);
        lblStatusBadge.Margin = new Padding(0, 4, 0, 0);
        lblStatusBadge.MinimumSize = new Size(0, 22);
        lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;

        statusRow.Controls.Add(lblStatusHeader, 0, 0);
        statusRow.Controls.Add(cmbStatus, 1, 0);
        statusRow.Controls.Add(lblStatusBadge, 2, 0);

        _btnToggleAiPanel = new Button
        {
            AutoSize = false,
            Size = new Size(108, 32),
            Margin = Padding.Empty,
            Text = "AI Panel ▾"
        };
        UiTheme.StyleSecondaryButton(_btnToggleAiPanel);
        _btnToggleAiPanel.Click -= ToggleAiPanel_Click;
        _btnToggleAiPanel.Click += ToggleAiPanel_Click;

        _flpCaseActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 0),
            Padding = Padding.Empty
        };

        ConfigureHeaderActionButton(btnUpdateStatus, "Update Status", 130, UpdateStatus_Click);
        ConfigureHeaderActionButton(btnPrint, "Print", 90, PrintBlotter_Click);

        _btnCloseReview = new Button
        {
            Text = "Close",
            Width = 96,
            Height = 32,
            AutoSize = false,
            Margin = new Padding(8, 0, 0, 0)
        };
        UiTheme.StyleSecondaryButton(_btnCloseReview);
        _btnCloseReview.Click += (_, _) => Close();

        _flpCaseActions.Controls.Add(_btnToggleAiPanel);
        _flpCaseActions.Controls.Add(btnUpdateStatus);
        _flpCaseActions.Controls.Add(btnPrint);
        _flpCaseActions.Controls.Add(_btnCloseReview);

        rightActions.Controls.Add(statusRow, 0, 0);
        rightActions.Controls.Add(_flpCaseActions, 0, 1);

        headerLayout.Controls.Add(leftInfo, 0, 0);
        headerLayout.Controls.Add(rightActions, 1, 0);
        panel.Controls.Add(headerLayout);

        _reviewToolTip ??= new ToolTip();
        _reviewToolTip.SetToolTip(_btnToggleAiPanel, "Collapse or expand AI assistant panel.");
        _reviewToolTip.SetToolTip(btnUpdateStatus, "Save the selected status update.");
        _reviewToolTip.SetToolTip(btnPrint, "Open print preview for this blotter case.");

        UiTheme.SetTabOrder(cmbStatus, _btnToggleAiPanel, btnUpdateStatus, btnPrint, _btnCloseReview);

        return panel;
    }

    private static void ConfigureHeaderActionButton(Button button, string text, int width, EventHandler clickHandler)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 32;
        button.AutoSize = false;
        button.AutoEllipsis = false;
        button.Margin = new Padding(8, 0, 0, 0);
        UiTheme.StyleSecondaryButton(button);
        button.Click -= clickHandler;
        button.Click += clickHandler;
    }

    private TabControl BuildReviewTabControl()
    {
        TabControl tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Point(12, 6)
        };

        TabPage overviewTab = new TabPage("Overview") { BackColor = Color.White };
        TabPage timelineTab = new TabPage("Timeline") { BackColor = Color.White };
        TabPage witnessesTab = new TabPage("Witnesses") { BackColor = Color.White };
        TabPage attachmentsTab = new TabPage("Attachments") { BackColor = Color.White };
        tabControl.AccessibleName = "Blotter case tabs";
        overviewTab.AccessibleDescription = "Overview tab for case details and actions.";
        timelineTab.AccessibleDescription = "Timeline tab for case history.";
        witnessesTab.AccessibleDescription = "Witnesses tab.";
        attachmentsTab.AccessibleDescription = "Attachments tab.";

        BuildOverviewTab(overviewTab);
        BuildTimelineTab(timelineTab);
        BuildWitnessesTab(witnessesTab);
        BuildAttachmentsTab(attachmentsTab);

        tabControl.TabPages.Add(overviewTab);
        tabControl.TabPages.Add(timelineTab);
        tabControl.TabPages.Add(witnessesTab);
        tabControl.TabPages.Add(attachmentsTab);
        return tabControl;
    }

    private void BuildOverviewTab(TabPage tab)
    {
        tab.Padding = Padding.Empty;
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        TableLayoutPanel leftColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 1,
            RowCount = 2
        };
        leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        GroupBox grpIncident = new GroupBox
        {
            Text = "Incident",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Font = UiTheme.LabelFont,
            Margin = new Padding(0, 0, 8, 8)
        };

        TableLayoutPanel incidentGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 4
        };
        incidentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
        incidentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        incidentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
        incidentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        incidentGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        incidentGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        incidentGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        incidentGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));

        Label lblType = new Label { Text = "Incident type *", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
        Label lblDate = new Label { Text = "Incident date *", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
        Label lblTime = new Label { Text = "Incident time", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
        Label lblLocation = new Label { Text = "Location", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };

        txtIncidentType.Dock = DockStyle.Fill;
        txtIncidentType.Margin = new Padding(0, 0, 12, 0);
        txtIncidentType.PlaceholderText = "e.g., Theft";
        txtIncidentType.MaxLength = 100;
        txtIncidentType.AccessibleName = "Incident type";

        dtpIncidentDate.Dock = DockStyle.Fill;
        dtpIncidentDate.Margin = new Padding(0, 0, 12, 0);
        dtpIncidentDate.Format = DateTimePickerFormat.Short;
        dtpIncidentDate.AccessibleName = "Incident date";

        dtpIncidentTime.Dock = DockStyle.Fill;
        dtpIncidentTime.Margin = new Padding(0, 0, 12, 0);
        dtpIncidentTime.Format = DateTimePickerFormat.Time;
        dtpIncidentTime.ShowUpDown = true;
        dtpIncidentTime.AccessibleName = "Incident time";

        txtIncidentLocation.Dock = DockStyle.Fill;
        txtIncidentLocation.Margin = new Padding(0, 0, 12, 0);
        txtIncidentLocation.PlaceholderText = "Landmark or purok/sitio (optional)";
        txtIncidentLocation.MaxLength = 120;
        txtIncidentLocation.AccessibleName = "Incident location";

        Label lblTimeHelper = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Use local date/time.",
            ForeColor = UiTheme.Slate500,
            Font = UiTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        Label lblLocationHelper = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Location is optional (max 120 characters).",
            ForeColor = UiTheme.Slate500,
            Font = UiTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        incidentGrid.Controls.Add(lblType, 0, 0);
        incidentGrid.Controls.Add(txtIncidentType, 1, 0);
        incidentGrid.Controls.Add(lblDate, 2, 0);
        incidentGrid.Controls.Add(dtpIncidentDate, 3, 0);
        incidentGrid.Controls.Add(lblTime, 0, 1);
        incidentGrid.Controls.Add(dtpIncidentTime, 1, 1);
        incidentGrid.Controls.Add(lblLocation, 2, 1);
        incidentGrid.Controls.Add(txtIncidentLocation, 3, 1);
        incidentGrid.Controls.Add(lblTimeHelper, 1, 2);
        incidentGrid.Controls.Add(lblLocationHelper, 3, 2);
        incidentGrid.SetColumnSpan(lblLocationHelper, 1);
        grpIncident.Controls.Add(incidentGrid);

        GroupBox grpDetails = new GroupBox
        {
            Text = "Details",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Font = UiTheme.LabelFont,
            Margin = new Padding(0, 0, 8, 0)
        };
        TableLayoutPanel detailsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        txtIncidentDetails.Dock = DockStyle.Fill;
        txtIncidentDetails.Multiline = true;
        txtIncidentDetails.ScrollBars = ScrollBars.Vertical;
        txtIncidentDetails.PlaceholderText = "Describe what happened...";
        txtIncidentDetails.AccessibleName = "Case details";
        Label lblDetailsHelper = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Provide key facts in clear, concise statements.",
            ForeColor = UiTheme.Slate500,
            Font = UiTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        detailsLayout.Controls.Add(txtIncidentDetails, 0, 0);
        detailsLayout.Controls.Add(lblDetailsHelper, 0, 1);
        grpDetails.Controls.Add(detailsLayout);

        leftColumn.Controls.Add(grpIncident, 0, 0);
        leftColumn.Controls.Add(grpDetails, 0, 1);

        TableLayoutPanel rightColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 1,
            RowCount = 2
        };
        rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        GroupBox grpQuick = new GroupBox
        {
            Text = "Quick info (read-only)",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Font = UiTheme.LabelFont,
            Margin = new Padding(0, 0, 0, 8)
        };

        TableLayoutPanel quickGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4
        };
        quickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
        quickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        quickGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        quickGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        quickGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        quickGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

        _lblQuickCaseIdValue = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Slate700 };
        _lblQuickRecordedByValue = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Slate700 };
        _lblQuickLastUpdatedValue = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Slate700 };
        _lblQuickStatusValue = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Slate700 };

        quickGrid.Controls.Add(new Label { Text = "Case ID", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        quickGrid.Controls.Add(_lblQuickCaseIdValue, 1, 0);
        quickGrid.Controls.Add(new Label { Text = "Recorded by", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        quickGrid.Controls.Add(_lblQuickRecordedByValue, 1, 1);
        quickGrid.Controls.Add(new Label { Text = "Last updated", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        quickGrid.Controls.Add(_lblQuickLastUpdatedValue, 1, 2);
        quickGrid.Controls.Add(new Label { Text = "Status", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        quickGrid.Controls.Add(_lblQuickStatusValue, 1, 3);
        grpQuick.Controls.Add(quickGrid);

        GroupBox grpActions = new GroupBox
        {
            Text = "Case actions",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Font = UiTheme.LabelFont,
            Margin = Padding.Empty
        };
        TableLayoutPanel actionGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6
        };
        actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _lblReviewFormHint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Fields marked * are required.",
            ForeColor = UiTheme.Slate500,
            Font = UiTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleLeft
        };

        lblActionTaken.Text = "Action taken";
        lblActionTaken.Dock = DockStyle.Fill;
        lblActionTaken.TextAlign = ContentAlignment.MiddleLeft;
        lblResolution.Text = "Resolution / notes";
        lblResolution.Dock = DockStyle.Fill;
        lblResolution.TextAlign = ContentAlignment.MiddleLeft;

        txtActionTaken.Dock = DockStyle.Fill;
        txtActionTaken.Multiline = true;
        txtActionTaken.ScrollBars = ScrollBars.Vertical;
        txtActionTaken.PlaceholderText = "Document actions taken by officers or mediators.";
        txtActionTaken.AccessibleName = "Action taken";
        txtActionTaken.Margin = Padding.Empty;

        txtResolution.Dock = DockStyle.Fill;
        txtResolution.Multiline = true;
        txtResolution.ScrollBars = ScrollBars.Vertical;
        txtResolution.PlaceholderText = "Add notes required for Settled, Referred, or Closed status.";
        txtResolution.AccessibleName = "Resolution notes";
        txtResolution.Margin = Padding.Empty;

        _lblReviewValidation = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.AccentRed,
            Font = UiTheme.SmallFont,
            TextAlign = ContentAlignment.TopLeft
        };

        actionGrid.Controls.Add(_lblReviewFormHint, 0, 0);
        actionGrid.Controls.Add(lblActionTaken, 0, 1);
        actionGrid.Controls.Add(txtActionTaken, 0, 2);
        actionGrid.Controls.Add(lblResolution, 0, 3);
        actionGrid.Controls.Add(txtResolution, 0, 4);
        actionGrid.Controls.Add(_lblReviewValidation, 0, 5);
        grpActions.Controls.Add(actionGrid);

        rightColumn.Controls.Add(grpQuick, 0, 0);
        rightColumn.Controls.Add(grpActions, 0, 1);

        root.Controls.Add(leftColumn, 0, 0);
        root.Controls.Add(rightColumn, 1, 0);
        tab.Controls.Add(root);

        UiTheme.SetTabOrder(
            txtIncidentType,
            dtpIncidentDate,
            dtpIncidentTime,
            txtIncidentLocation,
            txtIncidentDetails,
            txtActionTaken,
            txtResolution);
    }

    private void BuildTimelineTab(TabPage tab)
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        TableLayoutPanel actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 4,
            RowCount = 1
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        Button btnAddTimeline = new Button { Text = "Add Entry", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 6) };
        Button btnEditTimeline = new Button { Text = "Edit", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 6) };
        Button btnRemoveTimeline = new Button { Text = "Remove", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 6) };
        UiTheme.StyleSecondaryButtons(btnAddTimeline, btnEditTimeline, btnRemoveTimeline);
        btnAddTimeline.Enabled = false;
        btnEditTimeline.Enabled = false;
        btnRemoveTimeline.Enabled = false;

        _txtTimelineSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 6),
            PlaceholderText = "Search timeline..."
        };
        UiTheme.StyleTextBoxes(_txtTimelineSearch);

        if (_timelineSearchDebounceTimer == null)
        {
            _timelineSearchDebounceTimer = new System.Windows.Forms.Timer
            {
                Interval = 320
            };
            _timelineSearchDebounceTimer.Tick += TimelineSearchDebounceTimer_Tick;
        }

        _txtTimelineSearch.TextChanged += TimelineSearch_TextChanged;

        actions.Controls.Add(btnAddTimeline, 0, 0);
        actions.Controls.Add(btnEditTimeline, 1, 0);
        actions.Controls.Add(btnRemoveTimeline, 2, 0);
        actions.Controls.Add(_txtTimelineSearch, 3, 0);

        _timelineGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeight = 34,
            RowTemplate = { Height = 30 },
            BackgroundColor = Color.White
        };
        _timelineGrid.Columns.Clear();
        _timelineGrid.Columns.Add("When", "When");
        _timelineGrid.Columns.Add("Event", "Event");
        _timelineGrid.Columns.Add("By", "By");
        _timelineGrid.Columns.Add("Notes", "Notes");
        _timelineGrid.Columns["When"]!.FillWeight = 24;
        _timelineGrid.Columns["Event"]!.FillWeight = 36;
        _timelineGrid.Columns["By"]!.FillWeight = 16;
        _timelineGrid.Columns["Notes"]!.FillWeight = 24;
        _timelineGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        _timelineGrid.CellDoubleClick -= TimelineGrid_CellDoubleClick;
        _timelineGrid.CellDoubleClick += TimelineGrid_CellDoubleClick;
        UiTheme.StyleGrid(_timelineGrid);

        root.Controls.Add(actions, 0, 0);
        root.Controls.Add(_timelineGrid, 0, 1);
        tab.Controls.Add(root);
    }

    private void BuildWitnessesTab(TabPage tab)
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        FlowLayoutPanel actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 6, 0, 6)
        };
        Button btnAddWitness = new Button { Text = "Add Witness", Width = 110, Height = 32, AutoSize = false };
        Button btnEditWitness = new Button { Text = "Edit", Width = 90, Height = 32, AutoSize = false, Margin = new Padding(8, 0, 0, 0) };
        Button btnRemoveWitness = new Button { Text = "Remove", Width = 90, Height = 32, AutoSize = false, Margin = new Padding(8, 0, 0, 0) };
        UiTheme.StyleSecondaryButtons(btnAddWitness, btnEditWitness, btnRemoveWitness);
        btnAddWitness.Enabled = false;
        btnEditWitness.Enabled = false;
        btnRemoveWitness.Enabled = false;
        actions.Controls.Add(btnAddWitness);
        actions.Controls.Add(btnEditWitness);
        actions.Controls.Add(btnRemoveWitness);

        _witnessGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeight = 34,
            RowTemplate = { Height = 30 },
            BackgroundColor = Color.White
        };
        _witnessGrid.Columns.Add("Name", "Name");
        _witnessGrid.Columns.Add("Contact", "Contact");
        _witnessGrid.Columns.Add("Statement", "Statement/Notes");
        _witnessGrid.Columns["Name"]!.FillWeight = 30;
        _witnessGrid.Columns["Contact"]!.FillWeight = 20;
        _witnessGrid.Columns["Statement"]!.FillWeight = 50;
        UiTheme.StyleGrid(_witnessGrid);

        txtWitnesses.Visible = false;
        root.Controls.Add(actions, 0, 0);
        root.Controls.Add(_witnessGrid, 0, 1);
        tab.Controls.Add(root);
    }

    private void BuildAttachmentsTab(TabPage tab)
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        FlowLayoutPanel actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 6, 0, 6)
        };
        Button btnAddAttachment = new Button { Text = "Add Attachment", Width = 140, Height = 32, AutoSize = false };
        Button btnOpenAttachment = new Button { Text = "Open", Width = 90, Height = 32, AutoSize = false, Margin = new Padding(8, 0, 0, 0) };
        Button btnRemoveAttachment = new Button { Text = "Remove", Width = 110, Height = 32, AutoSize = false, Margin = new Padding(8, 0, 0, 0) };
        UiTheme.StyleSecondaryButtons(btnAddAttachment, btnOpenAttachment, btnRemoveAttachment);
        btnAddAttachment.Enabled = false;
        btnOpenAttachment.Enabled = false;
        btnRemoveAttachment.Enabled = false;
        actions.Controls.Add(btnAddAttachment);
        actions.Controls.Add(btnOpenAttachment);
        actions.Controls.Add(btnRemoveAttachment);

        _attachmentsList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false
        };
        _attachmentsList.Columns.Add("File Name", 320);
        _attachmentsList.Columns.Add("Type", 100);
        _attachmentsList.Columns.Add("Added", 170);
        _attachmentsList.Columns.Add("By", 120);
        _attachmentsList.Items.Add(new ListViewItem(new[] { "No attachments yet.", "-", "-", "-" }));

        root.Controls.Add(actions, 0, 0);
        root.Controls.Add(_attachmentsList, 0, 1);
        tab.Controls.Add(root);
    }

    private void BuildReviewAiPanel()
    {
        rightPanel.Controls.Clear();
        rightPanel.Padding = new Padding(12, 0, 0, 0);

        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        GroupBox header = new GroupBox
        {
            Text = "AI Blotter Case Assistant",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Font = UiTheme.LabelFont
        };
        TableLayoutPanel headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        TableLayoutPanel headerInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        headerInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        headerInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        Label lblModel = new Label
        {
            Text = $"Model: {_aiService.ModelName}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Slate700
        };
        lblAiMeta.Dock = DockStyle.Fill;
        lblAiMeta.Margin = Padding.Empty;
        lblAiMeta.TextAlign = ContentAlignment.MiddleLeft;
        headerInfo.Controls.Add(lblModel, 0, 0);
        headerInfo.Controls.Add(lblAiMeta, 0, 1);

        btnRunAiAnalysis.Width = 120;
        btnRunAiAnalysis.Height = 32;
        btnRunAiAnalysis.AutoSize = false;
        btnRunAiAnalysis.Text = "Run AI";

        headerLayout.Controls.Add(headerInfo, 0, 0);
        headerLayout.Controls.Add(btnRunAiAnalysis, 1, 0);
        header.Controls.Add(headerLayout);

        TableLayoutPanel content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        TableLayoutPanel left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));

        GroupBox grpSummary = new GroupBox { Text = "Summary", Dock = DockStyle.Fill, Padding = new Padding(10), Font = UiTheme.LabelFont };
        txtAiSummary.Dock = DockStyle.Fill;
        txtAiSummary.Multiline = true;
        txtAiSummary.ReadOnly = true;
        txtAiSummary.ScrollBars = ScrollBars.Vertical;
        grpSummary.Controls.Add(txtAiSummary);

        GroupBox grpKeyPoints = new GroupBox { Text = "Key Points", Dock = DockStyle.Fill, Padding = new Padding(10), Font = UiTheme.LabelFont };
        lstAiKeyPoints.Dock = DockStyle.Fill;
        grpKeyPoints.Controls.Add(lstAiKeyPoints);

        GroupBox grpNextAction = new GroupBox { Text = "Recommended Action", Dock = DockStyle.Fill, Padding = new Padding(10), Font = UiTheme.LabelFont };
        txtAiNextAction.Dock = DockStyle.Fill;
        txtAiNextAction.Multiline = true;
        txtAiNextAction.ReadOnly = true;
        txtAiNextAction.ScrollBars = ScrollBars.Vertical;
        grpNextAction.Controls.Add(txtAiNextAction);

        left.Controls.Add(grpSummary, 0, 0);
        left.Controls.Add(grpKeyPoints, 0, 1);
        left.Controls.Add(grpNextAction, 0, 2);

        TableLayoutPanel right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

        GroupBox grpRisk = new GroupBox { Text = "Risk", Dock = DockStyle.Fill, Padding = new Padding(10), Font = UiTheme.LabelFont };
        TableLayoutPanel risk = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5
        };
        risk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        risk.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        risk.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        risk.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        risk.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        risk.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        risk.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        lblAiCategory.Text = "Category";
        lblAiConfidence.Text = "Confidence";
        lblAiRiskLevel.Text = "Risk Level";
        lblAiRiskScore.Text = "Risk Score";
        lblAiCategory.Dock = DockStyle.Fill;
        lblAiConfidence.Dock = DockStyle.Fill;
        lblAiRiskLevel.Dock = DockStyle.Fill;
        lblAiRiskScore.Dock = DockStyle.Fill;
        lblAiCategory.TextAlign = ContentAlignment.MiddleLeft;
        lblAiConfidence.TextAlign = ContentAlignment.MiddleLeft;
        lblAiRiskLevel.TextAlign = ContentAlignment.MiddleLeft;
        lblAiRiskScore.TextAlign = ContentAlignment.MiddleLeft;
        lblAiCategoryValue.Dock = DockStyle.Fill;
        lblAiConfidenceValue.Dock = DockStyle.Fill;
        lblAiRiskLevelValue.Dock = DockStyle.Fill;

        FlowLayoutPanel score = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        progressRiskScore.Width = 120;
        progressRiskScore.Height = 14;
        lblAiRiskScoreValue.Margin = new Padding(8, 0, 0, 0);
        score.Controls.Add(progressRiskScore);
        score.Controls.Add(lblAiRiskScoreValue);

        lstAiRiskReasons.Dock = DockStyle.Fill;
        Panel riskReasonsHost = new Panel { Dock = DockStyle.Fill, Padding = Padding.Empty, Margin = Padding.Empty };
        riskReasonsHost.Controls.Add(lstAiRiskReasons);

        risk.Controls.Add(lblAiCategory, 0, 0);
        risk.Controls.Add(lblAiCategoryValue, 1, 0);
        risk.Controls.Add(lblAiConfidence, 0, 1);
        risk.Controls.Add(lblAiConfidenceValue, 1, 1);
        risk.Controls.Add(lblAiRiskLevel, 0, 2);
        risk.Controls.Add(lblAiRiskLevelValue, 1, 2);
        risk.Controls.Add(lblAiRiskScore, 0, 3);
        risk.Controls.Add(score, 1, 3);
        risk.Controls.Add(riskReasonsHost, 0, 4);
        risk.SetColumnSpan(riskReasonsHost, 2);
        grpRisk.Controls.Add(risk);

        GroupBox grpEntities = new GroupBox { Text = "Entities", Dock = DockStyle.Fill, Padding = new Padding(10), Font = UiTheme.LabelFont };
        TableLayoutPanel entities = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        entities.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        entities.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        entities.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        entities.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        entities.Controls.Add(BuildEntityGroup("People", lstAiPeople), 0, 0);
        entities.Controls.Add(BuildEntityGroup("Places", lstAiPlaces), 1, 0);
        entities.Controls.Add(BuildEntityGroup("Dates/Times", lstAiDatesTimes), 0, 1);
        entities.Controls.Add(BuildEntityGroup("Items", lstAiItems), 1, 1);
        grpEntities.Controls.Add(entities);

        right.Controls.Add(grpRisk, 0, 0);
        right.Controls.Add(grpEntities, 0, 1);

        content.Controls.Add(left, 0, 0);
        content.Controls.Add(right, 1, 0);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(content, 0, 1);
        rightPanel.Controls.Add(root);
    }

    private static GroupBox BuildEntityGroup(string title, ListBox source)
    {
        GroupBox group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(6),
            Font = UiTheme.SmallFont
        };
        source.Dock = DockStyle.Fill;
        group.Controls.Add(source);
        return group;
    }

    private void ToggleAiPanel_Click(object? sender, EventArgs e)
    {
        if (_reviewSplitMain == null)
        {
            return;
        }

        _reviewSplitMain.Panel2Collapsed = !_reviewSplitMain.Panel2Collapsed;
        if (!_reviewSplitMain.Panel2Collapsed)
        {
            UpdateReviewSplitDistance();
        }
        UpdateAiPanelToggleText();
    }

    private void UpdateAiPanelToggleText()
    {
        if (_btnToggleAiPanel == null)
        {
            return;
        }

        bool collapsed = _reviewSplitMain != null && _reviewSplitMain.Panel2Collapsed;
        _btnToggleAiPanel.Text = collapsed ? "AI Panel ▸" : "AI Panel ▾";
    }

    private void WireReviewValidationEvents()
    {
        if (_reviewValidationEventsWired)
        {
            return;
        }

        _reviewValidationEventsWired = true;
        txtIncidentType.TextChanged += ReviewInput_ValueChanged;
        dtpIncidentDate.ValueChanged += ReviewInput_ValueChanged;
        txtIncidentLocation.TextChanged += ReviewInput_ValueChanged;
        txtIncidentDetails.TextChanged += ReviewInput_ValueChanged;
        txtActionTaken.TextChanged += ReviewInput_ValueChanged;
        txtResolution.TextChanged += ReviewInput_ValueChanged;
    }

    private void ReviewInput_ValueChanged(object? sender, EventArgs e)
    {
        UpdateReviewMeta();
        ValidateReviewInputs(showMessages: false, out _, out _);
    }

    private void EnsureReviewValidationUi()
    {
        if (_reviewErrorProvider == null)
        {
            _reviewErrorProvider = new ErrorProvider
            {
                BlinkStyle = ErrorBlinkStyle.NeverBlink,
                ContainerControl = this
            };
        }
    }

    internal bool ValidateReviewInputs(bool showMessages, out string message, out string title)
    {
        EnsureReviewValidationUi();
        message = string.Empty;
        title = "Validation";

        _reviewErrorProvider?.SetError(txtIncidentType, string.Empty);
        _reviewErrorProvider?.SetError(dtpIncidentDate, string.Empty);
        _reviewErrorProvider?.SetError(txtIncidentLocation, string.Empty);
        _reviewErrorProvider?.SetError(txtResolution, string.Empty);
        if (!txtIncidentType.ReadOnly)
        {
            txtIncidentType.BackColor = Color.White;
        }
        if (!txtIncidentLocation.ReadOnly)
        {
            txtIncidentLocation.BackColor = Color.White;
        }
        if (!txtResolution.ReadOnly)
        {
            txtResolution.BackColor = Color.White;
        }

        string respondent = GetRespondentName();
        string incidentType = txtIncidentType.Text.Trim();
        DateTime incidentDate = dtpIncidentDate.Value.Date;
        string location = txtIncidentLocation.Text.Trim();
        string status = GetCurrentStatus();
        bool needsResolution = !status.Equals("Ongoing", StringComparison.OrdinalIgnoreCase)
                               && !status.Equals("Open", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(respondent))
        {
            message = "Respondent is required.";
            title = "Missing data";
        }
        else if (string.IsNullOrWhiteSpace(incidentType))
        {
            _reviewErrorProvider?.SetError(txtIncidentType, "Incident type is required.");
            if (!txtIncidentType.ReadOnly)
            {
                txtIncidentType.BackColor = Color.FromArgb(255, 244, 244);
            }
            message = "Incident type is required.";
            title = "Missing data";
        }
        else if (incidentDate > DateTime.Today)
        {
            _reviewErrorProvider?.SetError(dtpIncidentDate, "Incident date cannot be in the future.");
            message = "Incident date cannot be in the future.";
            title = "Invalid date";
        }
        else if (location.Length > 120)
        {
            _reviewErrorProvider?.SetError(txtIncidentLocation, "Location should be 120 characters or less.");
            if (!txtIncidentLocation.ReadOnly)
            {
                txtIncidentLocation.BackColor = Color.FromArgb(255, 244, 244);
            }
            message = "Location should be 120 characters or less.";
            title = "Invalid input";
        }
        else if (needsResolution && string.IsNullOrWhiteSpace(txtResolution.Text))
        {
            _reviewErrorProvider?.SetError(txtResolution, "Resolution / notes are required for this status.");
            if (!txtResolution.ReadOnly)
            {
                txtResolution.BackColor = Color.FromArgb(255, 244, 244);
            }
            message = "Resolution / notes are required when status is not Ongoing.";
            title = "Missing data";
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            if (_lblReviewValidation != null)
            {
                _lblReviewValidation.ForeColor = UiTheme.AccentRed;
                _lblReviewValidation.Text = message;
            }

            if (showMessages)
            {
                return false;
            }

            return false;
        }

        if (!txtIncidentType.ReadOnly)
        {
            txtIncidentType.BackColor = Color.White;
        }
        if (!txtIncidentLocation.ReadOnly)
        {
            txtIncidentLocation.BackColor = Color.White;
        }
        if (!txtResolution.ReadOnly)
        {
            txtResolution.BackColor = Color.White;
        }

        if (_lblReviewValidation != null)
        {
            if (needsResolution && string.IsNullOrWhiteSpace(txtIncidentDetails.Text))
            {
                _lblReviewValidation.ForeColor = UiTheme.AccentOrange;
                _lblReviewValidation.Text = "Details are empty while status is not Ongoing. Consider adding context.";
            }
            else
            {
                _lblReviewValidation.ForeColor = UiTheme.Slate500;
                _lblReviewValidation.Text = string.Empty;
            }
        }

        return true;
    }

    private void BuildWitnessGridFromText()
    {
        if (_witnessGrid == null)
        {
            return;
        }

        _witnessGrid.Rows.Clear();
        string raw = txtWitnesses.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            _witnessGrid.Rows.Add("No witnesses listed.", "-", "-");
            return;
        }

        string[] parts = raw
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();

        if (parts.Length == 0)
        {
            _witnessGrid.Rows.Add("No witnesses listed.", "-", "-");
            return;
        }

        foreach (string part in parts)
        {
            _witnessGrid.Rows.Add(part, "-", "-");
        }
    }

    private void UpdateReviewMeta()
    {
        if (_lblCaseMeta == null || _lblCaseTitle == null)
        {
            return;
        }

        string incident = string.IsNullOrWhiteSpace(txtIncidentType.Text) ? "-" : txtIncidentType.Text.Trim();
        string incidentTitle = ToTitleCaseWords(incident);
        string respondent = string.IsNullOrWhiteSpace(GetRespondentName()) ? "-" : GetRespondentName();
        string filed = dtpIncidentDate.Value.ToString("MMM dd, yyyy");
        string caseIdText = _blotterIdForAnalysis?.ToString() ?? "-";
        string currentStatus = GetCurrentStatus();

        _lblCaseTitle.Text = $"Case: {incidentTitle}";
        _lblCaseMeta.Text = $"Blotter Case #{caseIdText} • Filed: {filed} • Status: {currentStatus}";

        if (_lblRespondentSummary != null)
        {
            _lblRespondentSummary.Text = $"Respondent: {respondent} • Last updated: {DateTime.Now:MMM dd, yyyy hh:mm tt}";
        }

        if (_lblQuickCaseIdValue != null)
        {
            _lblQuickCaseIdValue.Text = caseIdText;
        }
        if (_lblQuickRecordedByValue != null)
        {
            _lblQuickRecordedByValue.Text = string.IsNullOrWhiteSpace(UserSession.Username) ? "-" : UserSession.Username;
        }
        if (_lblQuickLastUpdatedValue != null)
        {
            _lblQuickLastUpdatedValue.Text = DateTime.Now.ToString("MMM dd, yyyy hh:mm tt");
        }
        if (_lblQuickStatusValue != null)
        {
            _lblQuickStatusValue.Text = currentStatus;
        }
    }

    private static string ToTitleCaseWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "-")
        {
            return "-";
        }

        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text.Trim().ToLowerInvariant());
    }

    internal void SetBlotterIdForAnalysis(int blotterId)
    {
        _blotterIdForAnalysis = blotterId;
        ApplyReviewModeLayout();
        UpdateReviewMeta();
        btnRunAiAnalysis.Enabled = true;
        btnRunAiAnalysis.Text = "Run AI";
        lblAiMeta.Text = $"Last AI run: - | Model: {_aiService.ModelName}";
    }

    internal void LoadExistingBlotterForReview(
        string respondentName,
        string incidentType,
        DateTime incidentDate,
        TimeSpan? incidentTime,
        string incidentLocation,
        string witnesses,
        string actionTaken,
        string incidentDetails,
        string resolutionDetails,
        string status)
    {
        Text = "Review Blotter";
        lblHeader.Text = "Review Blotter Record";
        lblSubHeader.Text = "Review incident details and run AI analysis when needed.";

        rbResident.Enabled = false;
        rbOther.Enabled = false;
        rbOther.Checked = true;
        txtRespondentOther.Text = respondentName;
        cmbRespondent.Enabled = false;
        ApplyReadOnlyTextField(txtRespondentOther, readOnly: true);

        txtIncidentType.Text = incidentType;
        ApplyReadOnlyTextField(txtIncidentType, readOnly: true);

        dtpIncidentDate.Value = incidentDate == DateTime.MinValue ? DateTime.Today : incidentDate.Date;
        dtpIncidentDate.Enabled = false;

        DateTime timeBase = DateTime.Today;
        if (incidentTime.HasValue)
        {
            timeBase = DateTime.Today.Add(incidentTime.Value);
        }
        dtpIncidentTime.Value = timeBase;
        dtpIncidentTime.Enabled = false;

        txtIncidentLocation.Text = incidentLocation ?? string.Empty;
        ApplyReadOnlyTextField(txtIncidentLocation, readOnly: true);

        txtWitnesses.Text = witnesses ?? string.Empty;
        ApplyReadOnlyTextField(txtWitnesses, readOnly: true);

        txtActionTaken.Text = actionTaken ?? string.Empty;
        ApplyReadOnlyTextField(txtActionTaken, readOnly: false);

        txtIncidentDetails.Text = incidentDetails;
        ApplyReadOnlyTextField(txtIncidentDetails, readOnly: true);

        txtResolution.Text = resolutionDetails ?? string.Empty;
        ApplyReadOnlyTextField(txtResolution, readOnly: false);

        if (cmbStatus.Items.Count > 0)
        {
            int statusIndex = cmbStatus.FindStringExact(status ?? string.Empty);
            cmbStatus.SelectedIndex = statusIndex >= 0 ? statusIndex : 0;
        }
        cmbStatus.Enabled = true;
        _originalStatus = cmbStatus.SelectedItem?.ToString() ?? "Ongoing";
        UpdateStatusBadge();
        UpdateResolutionVisibility();
        UpdateStatusButtonState();
        BuildWitnessGridFromText();
        UpdateReviewMeta();
        ValidateReviewInputs(showMessages: false, out _, out _);

        btnSave.Visible = false;
        btnCancel.Text = "Close";
        AcceptButton = null;
    }

    internal void SetAiBusy(bool busy)
    {
        btnRunAiAnalysis.Enabled = !busy && _blotterIdForAnalysis.HasValue;
        btnRunAiAnalysis.Text = busy ? "Running..." : "Run AI";
        UseWaitCursor = busy;
    }

    internal void PopulateAiAnalysis(AiBlotterAnalysis analysis)
    {
        txtAiSummary.Text = analysis.Summary;
        lblAiCategoryValue.Text = analysis.SuggestedCategory;
        lblAiConfidenceValue.Text = analysis.CategoryConfidence.ToString("P0");
        lblAiRiskLevelValue.Text = analysis.RiskLevel;
        lblAiRiskScoreValue.Text = analysis.RiskScore.ToString();
        progressRiskScore.Value = Math.Clamp(analysis.RiskScore, 0, 100);
        txtAiNextAction.Text = analysis.RecommendedNextAction;

        PopulateListBox(lstAiKeyPoints, analysis.KeyPoints, "No key points.");
        PopulateListBox(lstAiRiskReasons, analysis.RiskReasons, "No explicit risk reasons.");
        PopulateListBox(lstAiPeople, analysis.Entities.People, "No people extracted.");
        PopulateListBox(lstAiPlaces, analysis.Entities.Places, "No places extracted.");
        PopulateListBox(lstAiDatesTimes, analysis.Entities.DatesTimes, "No dates/times extracted.");
        PopulateListBox(lstAiItems, analysis.Entities.Items, "No items extracted.");

        lblAiMeta.Text = $"Last AI run: {analysis.ProcessedAt:yyyy-MM-dd HH:mm:ss} | Model: {analysis.Model}";
    }

    private static void PopulateListBox(ListBox listBox, IReadOnlyCollection<string> values, string fallback)
    {
        listBox.BeginUpdate();
        try
        {
            listBox.Items.Clear();
            if (values.Count == 0)
            {
                listBox.Items.Add(fallback);
                return;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    listBox.Items.Add(value.Trim());
                }
            }

            if (listBox.Items.Count == 0)
            {
                listBox.Items.Add(fallback);
            }
        }
        finally
        {
            listBox.EndUpdate();
        }
    }

    private void InitializeAiState()
    {
        progressRiskScore.Minimum = 0;
        progressRiskScore.Maximum = 100;
        progressRiskScore.Value = 0;

        PopulateListBox(lstAiKeyPoints, Array.Empty<string>(), "No key points.");
        PopulateListBox(lstAiRiskReasons, Array.Empty<string>(), "No risk reasons.");
        PopulateListBox(lstAiPeople, Array.Empty<string>(), "No people extracted.");
        PopulateListBox(lstAiPlaces, Array.Empty<string>(), "No places extracted.");
        PopulateListBox(lstAiDatesTimes, Array.Empty<string>(), "No dates/times extracted.");
        PopulateListBox(lstAiItems, Array.Empty<string>(), "No items extracted.");

        txtAiSummary.Text = "No summary yet.";
        txtAiNextAction.Text = "No recommendation yet.";
        lblAiCategoryValue.Text = "-";
        lblAiConfidenceValue.Text = "-";
        lblAiRiskLevelValue.Text = "-";
        lblAiRiskScoreValue.Text = "0";

        if (_blotterIdForAnalysis.HasValue)
        {
            btnRunAiAnalysis.Enabled = true;
            btnRunAiAnalysis.Text = "Run AI";
            lblAiMeta.Text = $"Last AI run: - | Model: {_aiService.ModelName}";
        }
        else
        {
            btnRunAiAnalysis.Enabled = false;
            btnRunAiAnalysis.Text = "Run AI";
            lblAiMeta.Text = "Last AI run: Save or select a blotter record first.";
        }
    }

    private void ApplyReviewModeLayout()
    {
        bool reviewMode = _blotterIdForAnalysis.HasValue;
        if (reviewMode)
        {
            EnsureReviewLayoutBuilt();
        }

        if (_btnScheduleMediation != null && _flpCaseActions != null && !_flpCaseActions.Controls.Contains(_btnScheduleMediation))
        {
            _btnScheduleMediation.Parent?.Controls.Remove(_btnScheduleMediation);
            _btnScheduleMediation.Margin = new Padding(0, 0, 0, 0);
            _flpCaseActions.Controls.Add(_btnScheduleMediation);
            int targetIndex = (_btnToggleAiPanel != null && _flpCaseActions.Controls.Contains(_btnToggleAiPanel)) ? 1 : 0;
            _flpCaseActions.Controls.SetChildIndex(_btnScheduleMediation, targetIndex);
        }

        UpdateMediationActionState();
        btnUpdateStatus.Visible = reviewMode;
        btnPrint.Visible = reviewMode;
        if (_btnCloseReview != null)
        {
            _btnCloseReview.Visible = reviewMode;
        }

        btnSave.Visible = !reviewMode;
        btnCancel.Visible = !reviewMode;
        btnCancel.Text = reviewMode ? "Close" : "Cancel";
        buttonPanel.Visible = !reviewMode;
        AcceptButton = reviewMode ? null : btnSave;
        CancelButton = reviewMode ? _btnCloseReview ?? btnCancel : btnCancel;

        if (reviewMode)
        {
            if (_reviewSplitMain != null)
            {
                _reviewSplitMain.Panel2Collapsed = false;
            }

            UpdateAiPanelToggleText();
            UpdateStatusButtonState();
            BuildWitnessGridFromText();
            UpdateReviewMeta();
        }
        else
        {
            btnUpdateStatus.Enabled = false;
        }

        if (reviewMode)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            WindowState = FormWindowState.Maximized;
            ClientSize = ReviewClientSize;
            MinimumSize = ReviewMinimumSize;
            UpdateReviewSplitDistance();
            return;
        }

        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = FilingClientSize;
        MinimumSize = FilingMinimumSize;
    }

    internal void ReloadTimeline()
    {
        if (!_blotterIdForAnalysis.HasValue || _timelineGrid == null)
        {
            return;
        }

        try
        {
            DataTable raw = CaseTimelineService.LoadTimeline(_blotterIdForAnalysis.Value, limit: 120);
            DataTable display = new DataTable();
            display.Columns.Add("When", typeof(string));
            display.Columns.Add("Event", typeof(string));
            display.Columns.Add("By", typeof(string));
            display.Columns.Add("Notes", typeof(string));

            foreach (DataRow row in raw.Rows)
            {
                DateTime createdAt = DateTime.MinValue;
                if (row["created_at"] != DBNull.Value && DateTime.TryParse(row["created_at"]?.ToString(), out DateTime parsed))
                {
                    createdAt = parsed;
                }

                string title = row["event_title"]?.ToString() ?? "Update";
                string fromStatus = row["from_status"]?.ToString() ?? string.Empty;
                string toStatus = row["to_status"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(fromStatus) && !string.IsNullOrWhiteSpace(toStatus) &&
                    !string.Equals(fromStatus, toStatus, StringComparison.OrdinalIgnoreCase))
                {
                    title += $" ({fromStatus} -> {toStatus})";
                }

                string by = row["created_by"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(by))
                {
                    by = "-";
                }

                string whenText = createdAt == DateTime.MinValue ? "-" : createdAt.ToString("MMM dd, yyyy hh:mm tt");
                string notes = row["event_details"]?.ToString() ?? string.Empty;
                display.Rows.Add(whenText, title, by, string.IsNullOrWhiteSpace(notes) ? "-" : notes.Trim());
            }

            if (display.Rows.Count == 0)
            {
                display.Rows.Add("-", "No timeline entries yet.", "-", "-");
            }

            _timelineDisplayTable = display;
            ApplyTimelineSearchFilter();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Unable to load blotter timeline.", ex);
        }
    }

    private void TimelineSearch_TextChanged(object? sender, EventArgs e)
    {
        if (_timelineSearchDebounceTimer == null)
        {
            ApplyTimelineSearchFilter();
            return;
        }

        _timelineSearchDebounceTimer.Stop();
        _timelineSearchDebounceTimer.Start();
    }

    private void TimelineSearchDebounceTimer_Tick(object? sender, EventArgs e)
    {
        if (_timelineSearchDebounceTimer == null)
        {
            return;
        }

        _timelineSearchDebounceTimer.Stop();
        ApplyTimelineSearchFilter();
    }

    private void ApplyTimelineSearchFilter()
    {
        if (_timelineGrid == null || _timelineDisplayTable == null)
        {
            return;
        }

        string term = _txtTimelineSearch?.Text?.Trim() ?? string.Empty;
        DataView view = _timelineDisplayTable.DefaultView;
        if (string.IsNullOrWhiteSpace(term))
        {
            view.RowFilter = string.Empty;
        }
        else
        {
            string escaped = term.Replace("'", "''");
            view.RowFilter = $"[Event] LIKE '%{escaped}%' OR [By] LIKE '%{escaped}%' OR [Notes] LIKE '%{escaped}%' OR [When] LIKE '%{escaped}%'";
        }

        _timelineGrid.DataSource = view;
    }

    private void TimelineGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_timelineGrid == null || e.RowIndex < 0 || e.RowIndex >= _timelineGrid.Rows.Count)
        {
            return;
        }

        DataGridViewRow row = _timelineGrid.Rows[e.RowIndex];
        string title = row.Cells["Event"]?.Value?.ToString() ?? "Timeline entry";
        string details = row.Cells["Notes"]?.Value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(details) || details == "-")
        {
            details = "No details.";
        }

        ControllerDialogs.Info(details, title);
    }

    internal (DateTime ScheduleAt, string Venue)? PromptMediationSchedule()
    {
        DateTime defaultSchedule = DateTime.Now.AddDays(1);
        defaultSchedule = new DateTime(defaultSchedule.Year, defaultSchedule.Month, defaultSchedule.Day, 9, 0, 0);

        using var dialog = new Form
        {
            Text = "Schedule Mediation",
            Width = 520,
            Height = 240,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Font = UiTheme.BodyFont
        };

        var lblSchedule = new Label
        {
            Left = 16,
            Top = 18,
            Width = 460,
            Height = 20,
            Text = "Schedule date/time:"
        };

        var dtp = new DateTimePicker
        {
            Left = 16,
            Top = 44,
            Width = 260,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "MMM dd, yyyy hh:mm tt",
            ShowUpDown = true,
            Value = defaultSchedule
        };

        var lblVenue = new Label
        {
            Left = 16,
            Top = 82,
            Width = 460,
            Height = 20,
            Text = "Venue (optional):"
        };

        var txtVenue = new TextBox
        {
            Left = 16,
            Top = 108,
            Width = 460
        };
        UiTheme.StyleTextBox(txtVenue);

        var ok = new Button
        {
            Left = 312,
            Top = 150,
            Width = 80,
            Text = "OK",
            DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Left = 396,
            Top = 150,
            Width = 80,
            Text = "Cancel",
            DialogResult = DialogResult.Cancel
        };
        UiTheme.StylePrimaryButton(ok);
        UiTheme.StyleSecondaryButton(cancel);

        dialog.Controls.Add(lblSchedule);
        dialog.Controls.Add(dtp);
        dialog.Controls.Add(lblVenue);
        dialog.Controls.Add(txtVenue);
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);

        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        var result = dialog.ShowDialog(this);
        if (result != DialogResult.OK)
        {
            return null;
        }

        DateTime scheduleAt = dtp.Value;
        if (scheduleAt < DateTime.Now.AddMinutes(-1))
        {
            ControllerDialogs.Warning("Schedule date/time cannot be in the past.", "Schedule Mediation");
            return null;
        }

        return (scheduleAt, txtVenue.Text.Trim());
    }

    internal async Task ScheduleMediationAsync(int blotterId, DateTime scheduleAt, string venue)
    {
        if (blotterId <= 0)
        {
            throw new InvalidOperationException("Blotter case selection is required.");
        }

        using MySqlConnection conn = DBConnection.GetConnection();
        await conn.OpenAsync().ConfigureAwait(true);
        using MySqlTransaction tx = conn.BeginTransaction();

        using var insert = new MySqlCommand(
            @"INSERT INTO case_hearing
                (case_id, schedule_at, venue, status, created_by_user_id)
              VALUES
                (@id, @at, @venue, 'SCHEDULED', @by)",
            conn,
            tx);
        insert.Parameters.AddWithValue("@id", blotterId);
        insert.Parameters.AddWithValue("@at", scheduleAt);
        insert.Parameters.AddWithValue("@venue", string.IsNullOrWhiteSpace(venue) ? (object)DBNull.Value : venue);
        insert.Parameters.AddWithValue("@by", UserSession.UserId);
        await insert.ExecuteNonQueryAsync().ConfigureAwait(true);
        int hearingId = (int)insert.LastInsertedId;

        string timelineDetails = $"Scheduled at: {scheduleAt:yyyy-MM-dd hh:mm tt}";
        if (!string.IsNullOrWhiteSpace(venue))
        {
            timelineDetails += $"\nVenue: {venue}";
        }

        CaseTimelineService.LogTransactional(
            conn,
            tx,
            blotterId,
            "MEDIATION_SCHEDULED",
            "Mediation scheduled",
            timelineDetails,
            null,
            null,
            UserSession.UserId);

        AuditTrailService.LogTransactional(
            conn,
            tx,
            "Blotter",
            "case_hearing",
            hearingId,
            "SCHEDULE",
            null,
            new
            {
                HearingId = hearingId,
                CaseId = blotterId,
                ScheduleAt = scheduleAt,
                Venue = string.IsNullOrWhiteSpace(venue) ? null : venue,
                Status = "SCHEDULED"
            },
            "Mediation scheduled.");

        tx.Commit();
    }

    private void ApplyTheme()
    {
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        UiTheme.StyleComboBoxes(cmbRespondent, cmbStatus);
        UiTheme.StyleTextBoxes(txtRespondentOther, txtIncidentType, txtIncidentLocation, txtIncidentDetails, txtWitnesses, txtActionTaken, txtResolution, txtAiSummary, txtAiNextAction);

        dtpIncidentDate.Font = UiTheme.BodyFont;
        dtpIncidentTime.Font = UiTheme.BodyFont;

        UiTheme.StylePrimaryButtons(btnSave);
        UiTheme.StyleSecondaryButtons(btnCancel);
        UiTheme.StyleSecondaryButtons(btnRunAiAnalysis);
        UiTheme.StyleSecondaryButtons(btnUpdateStatus, btnPrint);

        lblHeader.Font = UiTheme.HeadingFont;
        lblHeader.ForeColor = UiTheme.Slate900;

        UiTheme.ApplyLabelFont(
            UiTheme.LabelFont,
            lblSubHeader,
            lblComplainant,
            lblRespondent,
            lblIncidentType,
            lblIncidentDate,
            lblIncidentTime,
            lblIncidentLocation,
            lblDetails,
            lblWitnesses,
            lblActionTaken,
            lblStatus,
            lblResolution,
            lblAiSummaryTitle,
            lblAiCategory,
            lblAiConfidence,
            lblAiRiskLevel,
            lblAiKeyPointsTitle,
            lblAiRiskReasonsTitle,
            lblAiEntitiesTitle,
            lblAiPeople,
            lblAiPlaces,
            lblAiDatesTimes,
            lblAiItems,
            lblAiNextActionTitle,
            lblAiMeta);

        lblSubHeader.ForeColor = UiTheme.Slate500;
        lblComplainant.ForeColor = UiTheme.Slate500;
        lblRespondent.ForeColor = UiTheme.Slate500;
        lblIncidentType.ForeColor = UiTheme.Slate500;
        lblIncidentDate.ForeColor = UiTheme.Slate500;
        lblIncidentTime.ForeColor = UiTheme.Slate500;
        lblIncidentLocation.ForeColor = UiTheme.Slate500;
        lblDetails.ForeColor = UiTheme.Slate500;
        lblWitnesses.ForeColor = UiTheme.Slate500;
        lblActionTaken.ForeColor = UiTheme.Slate500;
        lblStatus.ForeColor = UiTheme.Slate500;
        lblResolution.ForeColor = UiTheme.Slate500;

        lblSectionIncident.Font = new Font(UiTheme.BodyFont, FontStyle.Bold);
        lblSectionIncident.ForeColor = UiTheme.Slate700;
        lblSectionHandling.Font = new Font(UiTheme.BodyFont, FontStyle.Bold);
        lblSectionHandling.ForeColor = UiTheme.Slate700;

        lblStatusBadge.Font = UiTheme.SmallFont;
        lblStatusBadge.ForeColor = Color.White;

        grpAiAnalysis.ForeColor = UiTheme.Slate900;
        grpAiAnalysis.Font = UiTheme.LabelFont;

        foreach (ListBox listBox in new[] { lstAiKeyPoints, lstAiRiskReasons, lstAiPeople, lstAiPlaces, lstAiDatesTimes, lstAiItems })
        {
            listBox.Font = UiTheme.SmallFont;
            listBox.BackColor = Color.White;
            listBox.ForeColor = UiTheme.Slate900;
            listBox.BorderStyle = BorderStyle.FixedSingle;
        }

        lblAiCategoryValue.Font = UiTheme.BodyFont;
        lblAiConfidenceValue.Font = UiTheme.BodyFont;
        lblAiRiskLevelValue.Font = UiTheme.BodyFont;
        lblAiRiskScoreValue.Font = UiTheme.BodyFont;
        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
    }

    private void PopulateComplainant()
    {
        if (!string.IsNullOrWhiteSpace(_complainantName))
        {
            lblComplainant.Text = "Complainant: " + _complainantName;
            lblComplainant.Visible = true;
        }
        else
        {
            lblComplainant.Text = string.Empty;
            lblComplainant.Visible = false;
        }
    }

    private void LoadRespondentSuggestions(IEnumerable<string>? respondentSuggestions)
    {
        HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
        List<RespondentOption> items = new();

        if (respondentSuggestions != null)
        {
            foreach (string suggestion in respondentSuggestions)
            {
                string name = (suggestion ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(_complainantName) &&
                    name.Equals(_complainantName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (unique.Add(name))
                {
                    items.Add(new RespondentOption(null, name));
                }
            }
        }

        foreach (RespondentOption residentOption in LoadRespondentResidentsFromDatabase())
        {
            if (unique.Add(residentOption.DisplayName))
            {
                items.Add(residentOption);
            }
        }

        items = items
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        cmbRespondent.BeginUpdate();
        try
        {
            cmbRespondent.Items.Clear();
            cmbRespondent.Items.Add(SelectRespondentPlaceholder);
            if (items.Count > 0)
            {
                cmbRespondent.Items.AddRange(items.Cast<object>().ToArray());
            }
            cmbRespondent.SelectedIndex = 0;
        }
        finally
        {
            cmbRespondent.EndUpdate();
        }
    }

    private void RespondentMode_CheckedChanged(object? sender, EventArgs e)
    {
        _controller.HandleRespondentModeChanged();
    }

    private void ValidateAndClose(object? sender, EventArgs e)
    {
        _controller.HandleSave();
    }

    private async void RunAiAnalysis_Click(object? sender, EventArgs e)
    {
        await _controller.HandleRunAiAnalysisAsync();
    }

    private async void UpdateStatus_Click(object? sender, EventArgs e)
    {
        await _controller.HandleUpdateStatusAsync();
    }

    private void PrintBlotter_Click(object? sender, EventArgs e)
    {
        _controller.HandlePrint();
    }

    private static void ApplyReadOnlyTextField(TextBox textBox, bool readOnly)
    {
        textBox.ReadOnly = readOnly;
        if (readOnly)
        {
            textBox.BackColor = Color.FromArgb(245, 247, 250);
            textBox.ForeColor = UiTheme.Slate700;
        }
        else
        {
            textBox.BackColor = Color.White;
            textBox.ForeColor = UiTheme.Slate900;
        }
    }

    private void UpdateRespondentMode()
    {
        bool residentSelected = rbResident.Checked;
        cmbRespondent.Enabled = residentSelected;
        cmbRespondent.Visible = residentSelected;
        txtRespondentOther.Enabled = !residentSelected;
        txtRespondentOther.Visible = !residentSelected;

        cmbRespondent.BackColor = Color.White;
        txtRespondentOther.BackColor = Color.White;

        if (residentSelected)
        {
            txtRespondentOther.Text = string.Empty;
            if (cmbRespondent.Items.Count > 0 && cmbRespondent.SelectedIndex < 0)
            {
                cmbRespondent.SelectedIndex = 0;
            }
        }
        else
        {
            cmbRespondent.SelectedIndex = cmbRespondent.Items.Count > 0 ? 0 : -1;
        }
    }

    private void StatusSelectionChanged(object? sender, EventArgs e)
    {
        UpdateStatusButtonState();
        UpdateMediationActionState();
        UpdateStatusBadge();
        UpdateResolutionVisibility();
        ValidateReviewInputs(showMessages: false, out _, out _);
    }

    private void UpdateStatusButtonState()
    {
        if (!_blotterIdForAnalysis.HasValue)
        {
            btnUpdateStatus.Enabled = false;
            return;
        }

        string current = cmbStatus.SelectedItem?.ToString() ?? _originalStatus;
        btnUpdateStatus.Enabled = !string.Equals(current, _originalStatus, StringComparison.OrdinalIgnoreCase);
    }

    private void InitializeMediationActions()
    {
        if (_btnScheduleMediation != null)
        {
            return;
        }

        _btnScheduleMediation = new Button
        {
            Text = "Schedule Mediation",
            AutoSize = false,
            Width = 160,
            Height = 32,
            Visible = false
        };
        _btnScheduleMediation.AutoEllipsis = false;
        _btnScheduleMediation.Margin = new Padding(0, 0, 8, 0);
        _btnScheduleMediation.Click += ScheduleMediation_Click;
        UiTheme.StyleSecondaryButtons(_btnScheduleMediation);

        if (_flpCaseActions != null)
        {
            _flpCaseActions.Controls.Add(_btnScheduleMediation);
            int targetIndex = (_btnToggleAiPanel != null && _flpCaseActions.Controls.Contains(_btnToggleAiPanel)) ? 1 : 0;
            _flpCaseActions.Controls.SetChildIndex(_btnScheduleMediation, targetIndex);
        }
        else
        {
            buttonPanel.Controls.Add(_btnScheduleMediation);
        }
        UpdateMediationActionState();
    }

    private void UpdateMediationActionState()
    {
        if (_btnScheduleMediation == null)
        {
            return;
        }

        bool reviewMode = _blotterIdForAnalysis.HasValue;
        _btnScheduleMediation.Visible = reviewMode;

        bool enabled = reviewMode
                       && Permissions.CanUpdateBlotterStatus
                       && GetCurrentStatus().Equals("Ongoing", StringComparison.OrdinalIgnoreCase);
        _btnScheduleMediation.Enabled = enabled;
    }

    private async void ScheduleMediation_Click(object? sender, EventArgs e)
    {
        await _controller.HandleScheduleMediationAsync();
    }

    private void UpdateResolutionVisibility()
    {
        string current = GetCurrentStatus();
        bool needsResolution = !current.Equals("Ongoing", StringComparison.OrdinalIgnoreCase);
        lblResolution.Visible = needsResolution;
        txtResolution.Visible = needsResolution;

        if (current.Equals("Closed", StringComparison.OrdinalIgnoreCase))
        {
            lblResolution.Text = "Closure notes";
        }
        else if (current.Equals("Referred", StringComparison.OrdinalIgnoreCase))
        {
            lblResolution.Text = "Resolution / Notes";
        }
        else
        {
            lblResolution.Text = "Resolution";
        }
    }

    private void UpdateStatusBadge()
    {
        string current = GetCurrentStatus();
        lblStatusBadge.Text = current;

        Color badgeColor = UiTheme.AccentBlue;
        if (current.Equals("Settled", StringComparison.OrdinalIgnoreCase))
        {
            badgeColor = UiTheme.AccentGreen;
        }
        else if (current.Equals("Referred", StringComparison.OrdinalIgnoreCase))
        {
            badgeColor = UiTheme.AccentOrange;
        }
        else if (current.Equals("Closed", StringComparison.OrdinalIgnoreCase))
        {
            badgeColor = UiTheme.Slate500;
        }

        lblStatusBadge.BackColor = badgeColor;
        UpdateReviewMeta();
    }


    internal string GetCurrentStatus()
    {
        return cmbStatus.SelectedItem?.ToString() ?? _originalStatus;
    }

    internal string GetOriginalStatus()
    {
        return _originalStatus;
    }

    internal bool IsStatusChanged()
    {
        string current = GetCurrentStatus();
        return !string.Equals(current, _originalStatus, StringComparison.OrdinalIgnoreCase);
    }

    internal void MarkStatusUpdated(string newStatus)
    {
        _originalStatus = newStatus;
        _wasUpdated = true;
        UpdateStatusButtonState();
        UpdateMediationActionState();
    }

    internal async Task UpdateStatusAsync(int blotterId, string newStatus, string? referralDestination)
    {
        if (!helper.Permissions.CanUpdateBlotterStatus)
        {
            throw new InvalidOperationException("You do not have permission to update blotter status.");
        }

        string fromStatus = WorkflowRules.NormalizeBlotterStatus(_originalStatus);
        string toStatus = WorkflowRules.NormalizeBlotterStatus(newStatus);
        string actionTaken = txtActionTaken.Text.Trim();
        string resolutionDetails = txtResolution.Text.Trim();
        string referralDestinationValue = (referralDestination ?? string.Empty).Trim();
        if (toStatus.Equals("REFERRED", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(referralDestinationValue))
        {
            throw new InvalidOperationException("Referral destination is required for Referred status.");
        }
        if (!WorkflowRules.TryValidateBlotterTransition(fromStatus, toStatus, out var transitionMessage))
        {
            throw new InvalidOperationException(transitionMessage);
        }

        bool fromOngoingFamily = fromStatus.Equals("ONGOING", StringComparison.OrdinalIgnoreCase);
        string whereBySource = fromOngoingFamily
            ? "(UPPER(status) = 'ONGOING' OR UPPER(status) = 'OPEN')"
            : "UPPER(status) = @from";

        using MySqlConnection conn = DBConnection.GetConnection();
        await conn.OpenAsync().ConfigureAwait(true);
        using MySqlTransaction tx = conn.BeginTransaction();
        object beforeSnapshot = new
        {
            Status = fromStatus
        };
        try
        {
            using MySqlCommand cmd = new($@"UPDATE case_record
SET status = @status,
    action_taken = @action,
    resolution_details = @resolution,
    referral_destination = CASE WHEN @status = 'REFERRED' THEN @ref_destination ELSE referral_destination END,
    closure_notes = CASE WHEN @status = 'CLOSED' THEN @closure_notes ELSE closure_notes END,
    closed_at = CASE WHEN @status = 'CLOSED' THEN COALESCE(closed_at, NOW()) ELSE closed_at END,
    closed_by_user_id = CASE WHEN @status = 'CLOSED' THEN COALESCE(closed_by_user_id, @by) ELSE closed_by_user_id END,
    updated_at = NOW(),
    handled_by_user_id = @by
WHERE case_id = @id AND {whereBySource}", conn, tx);
            cmd.Parameters.AddWithValue("@status", toStatus);
            cmd.Parameters.AddWithValue("@action", actionTaken);
            cmd.Parameters.AddWithValue("@resolution", resolutionDetails);
            cmd.Parameters.AddWithValue("@ref_destination", toStatus.Equals("REFERRED", StringComparison.OrdinalIgnoreCase) ? referralDestinationValue : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@closure_notes", toStatus.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) ? resolutionDetails : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@by", UserSession.UserId);
            cmd.Parameters.AddWithValue("@id", blotterId);
            if (!fromOngoingFamily)
            {
                cmd.Parameters.AddWithValue("@from", fromStatus);
            }

            int rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);
            if (rows == 0)
            {
                throw new InvalidOperationException("Unable to update status. The blotter status may have changed.");
            }

             AuditTrailService.LogTransactional(
                 conn,
                 tx,
                 "Blotter",
                 "case_record",
                blotterId,
                "STATUS_UPDATE",
                beforeSnapshot,
                 new
                 {
                     Status = toStatus,
                     ActionTaken = actionTaken,
                     ResolutionDetails = resolutionDetails,
                     ReferralDestination = referralDestinationValue,
                     ClosureNotes = toStatus.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) ? resolutionDetails : string.Empty
                  },
                  $"Status changed from {fromStatus} to {toStatus}.");

            string timelineDetails = string.Empty;
            if (!string.IsNullOrWhiteSpace(actionTaken))
            {
                timelineDetails += "Action taken: " + actionTaken.Trim();
            }
            if (!string.IsNullOrWhiteSpace(resolutionDetails))
            {
                if (timelineDetails.Length > 0) timelineDetails += "\n";
                string label = toStatus.Equals("CLOSED", StringComparison.OrdinalIgnoreCase) ? "Closure notes: " : "Resolution: ";
                timelineDetails += label + resolutionDetails.Trim();
            }
            if (toStatus.Equals("REFERRED", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(referralDestinationValue))
            {
                if (timelineDetails.Length > 0) timelineDetails += "\n";
                timelineDetails += "Referral destination: " + referralDestinationValue;
            }

            CaseTimelineService.LogTransactional(
                conn,
                tx,
                blotterId,
                "STATUS_CHANGE",
                $"Status updated: {fromStatus} -> {toStatus}",
                string.IsNullOrWhiteSpace(timelineDetails) ? null : timelineDetails,
                fromStatus,
                toStatus,
                UserSession.UserId);
            tx.Commit();
         }
         catch (MySqlException ex) when (ex.Number == 1054)
         {
            using MySqlCommand cmd = new($"UPDATE case_record SET status = @status WHERE case_id = @id AND {whereBySource}", conn, tx);
            cmd.Parameters.AddWithValue("@status", toStatus);
            cmd.Parameters.AddWithValue("@id", blotterId);
            if (!fromOngoingFamily)
            {
                cmd.Parameters.AddWithValue("@from", fromStatus);
            }

            int rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);
            if (rows == 0)
            {
                throw new InvalidOperationException("Unable to update status. The blotter status may have changed.");
            }

             AuditTrailService.LogTransactional(
                 conn,
                 tx,
                 "Blotter",
                 "case_record",
                blotterId,
                "STATUS_UPDATE",
                beforeSnapshot,
                new
                {
                    Status = toStatus
                 },
                 $"Status changed from {fromStatus} to {toStatus}. Legacy schema: action/resolution columns unavailable.");

            CaseTimelineService.LogTransactional(
                conn,
                tx,
                blotterId,
                "STATUS_CHANGE",
                $"Status updated: {fromStatus} -> {toStatus}",
                "Legacy schema: action/resolution columns unavailable.",
                fromStatus,
                toStatus,
                UserSession.UserId);
            tx.Commit();
         }
     }

    private void ConfigurePrint()
    {
        _printDocument.PrintPage -= PrintDocument_PrintPage;
        _printDocument.PrintPage += PrintDocument_PrintPage;
        _printPreviewDialog = new PrintPreviewDialog
        {
            Document = _printDocument,
            Width = 900,
            Height = 700
        };
    }

    internal void ShowPrintPreview()
    {
        _printPreviewDialog?.ShowDialog(this);
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        string title = "Barangay Blotter Report";
        string complainant = string.IsNullOrWhiteSpace(_complainantName) ? "N/A" : _complainantName;
        string respondent = GetRespondentName();
        string incidentType = txtIncidentType.Text.Trim();
        string incidentDate = dtpIncidentDate.Value.ToString("yyyy-MM-dd");
        string incidentTime = dtpIncidentTime.Value.ToString("hh:mm tt");
        string location = txtIncidentLocation.Text.Trim();
        string witnesses = txtWitnesses.Text.Trim();
        string actionTaken = txtActionTaken.Text.Trim();
        string resolution = txtResolution.Text.Trim();
        string status = cmbStatus.SelectedItem?.ToString() ?? "Ongoing";
        string details = txtIncidentDetails.Text.Trim();

        float left = e.MarginBounds.Left;
        float top = e.MarginBounds.Top;
        float lineHeight = e.Graphics.MeasureString("A", Font).Height + 6;

        using var titleFont = new Font(Font.FontFamily, 14, FontStyle.Bold);
        using var labelFont = new Font(Font.FontFamily, 10, FontStyle.Bold);
        using var valueFont = new Font(Font.FontFamily, 10, FontStyle.Regular);

        e.Graphics.DrawString(title, titleFont, Brushes.Black, left, top);
        top += lineHeight * 2;

        DrawLine("Complainant:", complainant, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Respondent:", string.IsNullOrWhiteSpace(respondent) ? "N/A" : respondent, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Incident type:", incidentType, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Incident date:", incidentDate, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Incident time:", incidentTime, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Location:", string.IsNullOrWhiteSpace(location) ? "N/A" : location, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Witnesses:", string.IsNullOrWhiteSpace(witnesses) ? "N/A" : witnesses, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Status:", status, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Action taken:", string.IsNullOrWhiteSpace(actionTaken) ? "N/A" : actionTaken, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);
        DrawLine("Resolution:", string.IsNullOrWhiteSpace(resolution) ? "N/A" : resolution, ref top, left, labelFont, valueFont, e.Graphics, lineHeight);

        top += lineHeight;
        e.Graphics.DrawString("Details:", labelFont, Brushes.Black, left, top);
        top += lineHeight;

        var detailsRect = new RectangleF(left, top, e.MarginBounds.Width, e.MarginBounds.Height - top);
        e.Graphics.DrawString(details, valueFont, Brushes.Black, detailsRect);
    }

    private static void DrawLine(
        string label,
        string value,
        ref float top,
        float left,
        Font labelFont,
        Font valueFont,
        Graphics graphics,
        float lineHeight)
    {
        graphics.DrawString(label, labelFont, Brushes.Black, left, top);
        graphics.DrawString(value, valueFont, Brushes.Black, left + 140, top);
        top += lineHeight;
    }

    private string GetRespondentName()
    {
        if (rbResident.Checked)
        {
            if (cmbRespondent.SelectedItem is RespondentOption option)
            {
                return option.DisplayName;
            }

            string selected = cmbRespondent.SelectedItem?.ToString()?.Trim() ?? string.Empty;
            if (selected.Equals(SelectRespondentPlaceholder, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return selected;
        }

        return txtRespondentOther.Text.Trim();
    }

    private int? GetRespondentResidentId()
    {
        if (!rbResident.Checked)
        {
            return null;
        }

        if (cmbRespondent.SelectedItem is RespondentOption option)
        {
            return option.ResidentId;
        }

        return null;
    }

    private IEnumerable<RespondentOption> LoadRespondentResidentsFromDatabase()
    {
        List<RespondentOption> items = new();

        try
        {
            using MySqlConnection conn = DBConnection.GetConnection();
            conn.Open();

            const string sql = @"SELECT resident_id, first_name, middle_name, last_name
                                 FROM resident
                                 WHERE resident_id <> @complainant_id
                                 ORDER BY last_name, first_name";

            using MySqlCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@complainant_id", _complainantId);
            using MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int residentId = reader["resident_id"] != DBNull.Value
                    ? Convert.ToInt32(reader["resident_id"])
                    : 0;
                string first = reader["first_name"]?.ToString() ?? string.Empty;
                string middle = reader["middle_name"]?.ToString() ?? string.Empty;
                string last = reader["last_name"]?.ToString() ?? string.Empty;
                string full = string.Join(" ", new[] { first, middle, last }.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();

                if (string.IsNullOrWhiteSpace(full))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(_complainantName) &&
                    full.Equals(_complainantName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                items.Add(new RespondentOption(residentId > 0 ? residentId : null, full));
            }
        }
        catch
        {
            
        }

        return items;
    }

    private sealed class RespondentOption
    {
        public RespondentOption(int? residentId, string displayName)
        {
            ResidentId = residentId;
            DisplayName = displayName;
        }

        public int? ResidentId { get; }

        public string DisplayName { get; }

        public override string ToString() => DisplayName;
    }
}
