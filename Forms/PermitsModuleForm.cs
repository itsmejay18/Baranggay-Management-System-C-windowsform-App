using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using baranggaysystem1.Controls;
using baranggaysystem1.Database;
using baranggaysystem1.helper;
using FontAwesome.Sharp;
using MySql.Data.MySqlClient;

namespace baranggaysystem1;

internal sealed class PermitsModuleForm : Form
{
    private sealed class StatusOption
    {
        public string Value { get; }
        public string Label { get; }

        public StatusOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public override string ToString() => Label;
    }

    private sealed class QueryContext
    {
        public List<string> Conditions { get; } = new List<string>();
        public List<(string Name, object Value)> Parameters { get; } = new List<(string, object)>();
    }

    private sealed class ModulePageResult
    {
        public ModulePageResult(DataTable table, int totalRows, int pageNumber, int totalPages)
        {
            Table = table;
            TotalRows = totalRows;
            PageNumber = pageNumber;
            TotalPages = totalPages;
        }

        public DataTable Table { get; }
        public int TotalRows { get; }
        public int PageNumber { get; }
        public int TotalPages { get; }
    }

    private readonly Action<CertificateAction> _openCertificates;
    private readonly Action<int, int> _openCertificateById;

    private readonly Label _titleLabel = new Label();
    private readonly Label _subtitleLabel = new Label();

    private readonly FlowLayoutPanel _actionPanel = new FlowLayoutPanel();
    private readonly Button _newButton = new Button();
    private readonly Button _approveButton = new Button();
    private readonly Button _releaseButton = new Button();
    private readonly Button _printButton = new Button();
    private readonly Button _exportButton = new Button();
    private readonly Button _openButton = new Button();
    private readonly Button _refreshButton = new Button();

    private readonly TextBox _searchBox = new TextBox();
    private readonly ComboBox _statusFilter = new ComboBox();
    private readonly DateTimePicker _fromDate = new DateTimePicker();
    private readonly DateTimePicker _toDate = new DateTimePicker();

    private readonly DataGridView _grid = new DataGridView();
    private readonly Panel _statePanel = new Panel();

    private readonly Button _prevButton = new Button();
    private readonly Button _nextButton = new Button();
    private readonly Label _pageInfoLabel = new Label();
    private readonly ComboBox _pageSizeCombo = new ComboBox();

    private readonly System.Windows.Forms.Timer _searchDebounce = new System.Windows.Forms.Timer();
    private readonly ToolTip _toolTip = new ToolTip();
    private readonly LoadingOverlay _loadingOverlay = new LoadingOverlay();

    private CancellationTokenSource? _loadCts;
    private int _loadVersion;
    private int _currentPage = 1;
    private int _pageSize = 25;
    private int _totalRows;
    private readonly int _barangayId;

    private bool HasCertificateAccess => Permissions.IsAdmin
        || Permissions.CanRequestCertificates
        || Permissions.CanEditCertificateRequests
        || Permissions.CanApproveCertificates
        || Permissions.CanIssueCertificates
        || Permissions.CanCancelCertificates
        || Permissions.CanExportCertificates;

    public PermitsModuleForm(Action<CertificateAction> openCertificates, Action<int, int> openCertificateById)
    {
        _openCertificates = openCertificates ?? throw new ArgumentNullException(nameof(openCertificates));
        _openCertificateById = openCertificateById ?? throw new ArgumentNullException(nameof(openCertificateById));
        _barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);

        InitializeComponent();
        LoadStatusOptions();
        ApplyRolePermissions();
        RefreshList(resetPage: true);
    }

    private void InitializeComponent()
    {
        Text = "Permits";
        Name = "PermitsModuleForm";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = UiTheme.Slate100;
        Font = UiTheme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16),
            BackColor = UiTheme.Slate100
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = UiTheme.Slate100,
            Height = 70
        };
        _titleLabel.Text = "Permits";
        _titleLabel.Font = UiTheme.HeadingFont;
        _titleLabel.ForeColor = UiTheme.Slate900;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(0, 0);

        _subtitleLabel.Text = "Process business clearance permits, approvals, and releases.";
        _subtitleLabel.Font = UiTheme.LabelFont;
        _subtitleLabel.ForeColor = UiTheme.Slate600;
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Location = new Point(0, 30);

        headerPanel.Controls.Add(_titleLabel);
        headerPanel.Controls.Add(_subtitleLabel);
        root.Controls.Add(headerPanel, 0, 0);

        _actionPanel.Dock = DockStyle.Top;
        _actionPanel.AutoSize = true;
        _actionPanel.FlowDirection = FlowDirection.LeftToRight;
        _actionPanel.WrapContents = true;
        _actionPanel.Margin = new Padding(0, 0, 0, 8);

        _newButton.Text = "New Request";
        _approveButton.Text = "Approve";
        _releaseButton.Text = "Release";
        _printButton.Text = "Print";
        _exportButton.Text = "Export";
        _openButton.Text = "Open Selected";
        _refreshButton.Text = "Refresh";

        UiTheme.StylePrimaryButton(_newButton);
        UiTheme.StyleSecondaryButton(_approveButton);
        UiTheme.StyleSecondaryButton(_releaseButton);
        UiTheme.StyleSecondaryButton(_printButton);
        UiTheme.StyleSecondaryButton(_exportButton);
        UiTheme.StyleSecondaryButton(_openButton);
        UiTheme.StyleSecondaryButton(_refreshButton);

        _actionPanel.Controls.Add(_newButton);
        _actionPanel.Controls.Add(_approveButton);
        _actionPanel.Controls.Add(_releaseButton);
        _actionPanel.Controls.Add(_printButton);
        _actionPanel.Controls.Add(_exportButton);
        _actionPanel.Controls.Add(_openButton);
        _actionPanel.Controls.Add(_refreshButton);
        root.Controls.Add(_actionPanel, 0, 1);

        var filterCard = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = Color.White,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10)
        };
        UiTheme.StyleSectionCard(filterCard, Color.White, enforceBorder: true, padding: new Padding(10));
        root.Controls.Add(filterCard, 0, 2);

        var filterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.White
        };
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterCard.Controls.Add(filterLayout);

        _searchBox.PlaceholderText = "Search by permit no, business name, owner, or OR no...";
        _searchBox.Dock = DockStyle.Fill;
        UiTheme.StyleTextBox(_searchBox);

        _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusFilter.Dock = DockStyle.Fill;
        UiTheme.StyleComboBox(_statusFilter);

        var fromLabel = new Label
        {
            Text = "From",
            AutoSize = true,
            ForeColor = UiTheme.Slate600,
            Font = UiTheme.LabelFont,
            Dock = DockStyle.Fill
        };
        var toLabel = new Label
        {
            Text = "To",
            AutoSize = true,
            ForeColor = UiTheme.Slate600,
            Font = UiTheme.LabelFont,
            Dock = DockStyle.Fill
        };

        ConfigureDatePicker(_fromDate);
        ConfigureDatePicker(_toDate);

        filterLayout.Controls.Add(_searchBox, 0, 0);
        filterLayout.SetRowSpan(_searchBox, 2);
        filterLayout.Controls.Add(_statusFilter, 1, 0);
        filterLayout.SetRowSpan(_statusFilter, 2);
        filterLayout.Controls.Add(fromLabel, 2, 0);
        filterLayout.Controls.Add(toLabel, 3, 0);
        filterLayout.Controls.Add(_fromDate, 2, 1);
        filterLayout.Controls.Add(_toDate, 3, 1);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        UiTheme.StyleGridContainer(gridCard);
        root.Controls.Add(gridCard, 0, 3);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        UiTheme.StyleGrid(_grid);
        gridCard.Controls.Add(_grid);

        _statePanel.Dock = DockStyle.Fill;
        _statePanel.BackColor = Color.Transparent;
        _statePanel.Visible = false;
        gridCard.Controls.Add(_statePanel);

        var pagerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };

        _prevButton.Text = "Prev";
        _nextButton.Text = "Next";
        UiTheme.StyleSecondaryButton(_prevButton);
        UiTheme.StyleSecondaryButton(_nextButton);

        _pageInfoLabel.AutoSize = true;
        _pageInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        _pageInfoLabel.Padding = new Padding(6, 8, 6, 0);

        _pageSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _pageSizeCombo.Width = 86;
        _pageSizeCombo.Items.AddRange(new object[] { "25", "50", "100" });
        _pageSizeCombo.SelectedIndex = 0;
        UiTheme.StyleComboBox(_pageSizeCombo);

        pagerPanel.Controls.Add(new Label
        {
            Text = "Rows",
            AutoSize = true,
            Padding = new Padding(0, 8, 4, 0),
            ForeColor = UiTheme.Slate700
        });
        pagerPanel.Controls.Add(_pageSizeCombo);
        pagerPanel.Controls.Add(_prevButton);
        pagerPanel.Controls.Add(_nextButton);
        pagerPanel.Controls.Add(_pageInfoLabel);
        root.Controls.Add(pagerPanel, 0, 4);

        _loadingOverlay.HideLoading();
        Controls.Add(_loadingOverlay);
        _loadingOverlay.BringToFront();

        _searchDebounce.Interval = 300;
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            RefreshList(resetPage: true);
        };

        _searchBox.TextChanged += (_, _) =>
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };

        _statusFilter.SelectedIndexChanged += (_, _) => RefreshList(resetPage: true);
        _fromDate.ValueChanged += (_, _) => RefreshList(resetPage: true);
        _toDate.ValueChanged += (_, _) => RefreshList(resetPage: true);
        _pageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            if (int.TryParse(Convert.ToString(_pageSizeCombo.SelectedItem), out int parsed) && parsed > 0)
            {
                _pageSize = parsed;
                RefreshList(resetPage: true);
            }
        };

        _prevButton.Click += (_, _) => ChangePage(-1);
        _nextButton.Click += (_, _) => ChangePage(1);
        _refreshButton.Click += (_, _) => RefreshList(resetPage: false);
        _newButton.Click += (_, _) => OpenNewRequest();
        _approveButton.Click += (_, _) => ApproveSelected();
        _releaseButton.Click += (_, _) => ReleaseSelected();
        _printButton.Click += (_, _) => PrintSelected();
        _exportButton.Click += (_, _) => ExportListAsync();
        _openButton.Click += (_, _) => OpenSelected();
        _grid.CellDoubleClick += (_, _) => OpenSelected();
        _grid.SelectionChanged += (_, _) => UpdateActionState();

        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
    }
    private static void ConfigureDatePicker(DateTimePicker picker)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "MMM dd, yyyy";
        picker.ShowCheckBox = true;
        picker.Checked = false;
        picker.Dock = DockStyle.Fill;
        picker.Font = UiTheme.BodyFont;
    }

    private void LoadStatusOptions()
    {
        var options = new List<StatusOption>
        {
            new StatusOption(string.Empty, "All Statuses"),
            new StatusOption("SUBMITTED", "Requested"),
            new StatusOption("APPROVED", "Approved"),
            new StatusOption("RELEASED", "Issued"),
            new StatusOption("CANCELLED", "Cancelled"),
            new StatusOption("REJECTED", "Rejected"),
            new StatusOption("DRAFT", "Draft")
        };

        _statusFilter.DisplayMember = nameof(StatusOption.Label);
        _statusFilter.ValueMember = nameof(StatusOption.Value);
        _statusFilter.DataSource = options;
        _statusFilter.SelectedIndex = 0;
    }

    private void ApplyRolePermissions()
    {
        _newButton.Enabled = Permissions.CanRequestCertificates;
        _approveButton.Enabled = Permissions.CanApproveCertificates;
        _releaseButton.Enabled = Permissions.CanIssueCertificates;
        _exportButton.Enabled = Permissions.CanExportCertificates;

        _openButton.Enabled = HasCertificateAccess;
        _printButton.Enabled = HasCertificateAccess;
        _refreshButton.Enabled = HasCertificateAccess;
        _searchBox.Enabled = HasCertificateAccess;
        _statusFilter.Enabled = HasCertificateAccess;
        _fromDate.Enabled = HasCertificateAccess;
        _toDate.Enabled = HasCertificateAccess;

        if (!Permissions.CanRequestCertificates)
        {
            _toolTip.SetToolTip(_newButton, "No permission: certificates.request");
        }
        if (!Permissions.CanApproveCertificates)
        {
            _toolTip.SetToolTip(_approveButton, "No permission: certificates.approve");
        }
        if (!Permissions.CanIssueCertificates)
        {
            _toolTip.SetToolTip(_releaseButton, "No permission: certificates.issue");
        }
        if (!Permissions.CanExportCertificates)
        {
            _toolTip.SetToolTip(_exportButton, "No permission: certificates.export");
        }

        UpdateActionState();
    }

    private void UpdateActionState()
    {
        bool hasAccess = HasCertificateAccess;
        bool hasSelection = TryGetSelectedCertificate(out _, out _);

        _openButton.Enabled = hasAccess && hasSelection;
        _printButton.Enabled = hasAccess && hasSelection;
        _approveButton.Enabled = hasAccess && Permissions.CanApproveCertificates && hasSelection;
        _releaseButton.Enabled = hasAccess && Permissions.CanIssueCertificates && hasSelection;
        _newButton.Enabled = hasAccess && Permissions.CanRequestCertificates;
        _exportButton.Enabled = hasAccess && Permissions.CanExportCertificates;
        _refreshButton.Enabled = hasAccess;
    }

    private async void RefreshList(bool resetPage)
    {
        if (resetPage)
        {
            _currentPage = 1;
        }

        if (!HasCertificateAccess)
        {
            _grid.DataSource = null;
            _totalRows = 0;
            UpdatePager(1);
            ShowNoPermissionState();
            return;
        }

        int version = ++_loadVersion;
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        CancellationToken token = _loadCts.Token;

        SetLoading(true, "Loading permits...");

        try
        {
            ModulePageResult result = await LoadPageAsync(token);
            if (token.IsCancellationRequested || version != _loadVersion)
            {
                return;
            }

            DataTable display = BuildDisplayTable(result.Table);
            _grid.DataSource = display;
            ConfigureGridColumns();

            _totalRows = result.TotalRows;
            _currentPage = result.PageNumber;
            UpdatePager(result.TotalPages);
            UpdateStatePanel(display.Rows.Count);
        }
        catch (OperationCanceledException)
        {
            // Ignore canceled loads.
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load permits.", "Permits");
            ShowErrorState("Unable to load permits right now.");
        }
        finally
        {
            if (version == _loadVersion)
            {
                SetLoading(false, string.Empty);
                UpdateActionState();
            }
        }
    }
    private async Task<ModulePageResult> LoadPageAsync(CancellationToken cancellationToken)
    {
        QueryContext context = BuildQueryContext();
        string whereClause = context.Conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", context.Conditions)
            : string.Empty;

        string countSql = $@"
SELECT COUNT(*)
FROM document_request dr
LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
LEFT JOIN resident r ON r.resident_id = dr.resident_id
{whereClause}";

        int totalRows = await DatabaseManagerAsync.ExecuteScalarAsync<int>(
            countSql,
            cmd => AddParameters(cmd, context.Parameters),
            cancellationToken);

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)_pageSize));
        int pageNumber = Math.Clamp(_currentPage, 1, totalPages);
        int offset = (pageNumber - 1) * _pageSize;

        var dataParams = new List<(string Name, object Value)>(context.Parameters)
        {
            ("@pageSize", _pageSize),
            ("@offset", offset)
        };

        string dataSql = $@"
SELECT dr.doc_request_id,
       dr.resident_id,
       dr.document_no,
       dr.purpose,
       dr.business_name,
       dr.business_nature,
       dr.status,
       dr.requested_at,
       dr.approved_at,
       dr.released_at,
       dr.fee,
       dr.or_number,
       r.first_name,
       r.middle_name,
       r.last_name,
       r.suffix
FROM document_request dr
LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
LEFT JOIN resident r ON r.resident_id = dr.resident_id
{whereClause}
ORDER BY dr.requested_at DESC, dr.doc_request_id DESC
LIMIT @pageSize OFFSET @offset";

        DataTable table = await DatabaseManagerAsync.LoadTableAsync(
            dataSql,
            cmd => AddParameters(cmd, dataParams),
            cancellationToken);

        return new ModulePageResult(table, totalRows, pageNumber, totalPages);
    }

    private async Task<DataTable> LoadAllAsync(CancellationToken cancellationToken)
    {
        QueryContext context = BuildQueryContext();
        string whereClause = context.Conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", context.Conditions)
            : string.Empty;

        string dataSql = $@"
SELECT dr.doc_request_id,
       dr.resident_id,
       dr.document_no,
       dr.purpose,
       dr.business_name,
       dr.business_nature,
       dr.status,
       dr.requested_at,
       dr.approved_at,
       dr.released_at,
       dr.fee,
       dr.or_number,
       r.first_name,
       r.middle_name,
       r.last_name,
       r.suffix
FROM document_request dr
LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
LEFT JOIN resident r ON r.resident_id = dr.resident_id
{whereClause}
ORDER BY dr.requested_at DESC, dr.doc_request_id DESC";

        return await DatabaseManagerAsync.LoadTableAsync(
            dataSql,
            cmd => AddParameters(cmd, context.Parameters),
            cancellationToken);
    }

    private QueryContext BuildQueryContext()
    {
        var context = new QueryContext();
        context.Conditions.Add("dr.barangay_id = @barangayId");
        context.Parameters.Add(("@barangayId", _barangayId));

        context.Conditions.Add("(dt.code = @docCode OR dt.name = @docName)");
        context.Parameters.Add(("@docCode", "BUS"));
        context.Parameters.Add(("@docName", "Business Clearance"));

        string search = _searchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            context.Conditions.Add(@"
(
    dr.document_no LIKE @search
    OR dr.or_number LIKE @search
    OR dr.business_name LIKE @search
    OR dr.business_nature LIKE @search
    OR dr.purpose LIKE @search
    OR r.first_name LIKE @search
    OR r.middle_name LIKE @search
    OR r.last_name LIKE @search
    OR r.suffix LIKE @search
)");
            context.Parameters.Add(("@search", $"%{search}%"));
        }

        string? status = GetSelectedStatus();
        if (!string.IsNullOrWhiteSpace(status))
        {
            context.Conditions.Add("dr.status = @status");
            context.Parameters.Add(("@status", status));
        }

        if (_fromDate.Checked)
        {
            DateTime from = _fromDate.Value.Date;
            context.Conditions.Add("dr.requested_at >= @fromDate");
            context.Parameters.Add(("@fromDate", from.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        if (_toDate.Checked)
        {
            DateTime to = _toDate.Value.Date.AddDays(1);
            context.Conditions.Add("dr.requested_at < @toDate");
            context.Parameters.Add(("@toDate", to.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        return context;
    }

    private static void AddParameters(MySqlCommand cmd, IEnumerable<(string Name, object Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    private DataTable BuildDisplayTable(DataTable source)
    {
        var table = new DataTable();
        table.Columns.Add("certificate_id", typeof(int));
        table.Columns.Add("resident_id", typeof(int));
        table.Columns.Add("document_no", typeof(string));
        table.Columns.Add("business_name", typeof(string));
        table.Columns.Add("business_nature", typeof(string));
        table.Columns.Add("resident_name", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("requested_at", typeof(string));
        table.Columns.Add("approved_at", typeof(string));
        table.Columns.Add("released_at", typeof(string));
        table.Columns.Add("or_number", typeof(string));
        table.Columns.Add("fee", typeof(string));

        foreach (DataRow row in source.Rows)
        {
            int certificateId = ReadInt(row, "doc_request_id");
            int residentId = ReadInt(row, "resident_id");
            string docNo = SafeString(row, "document_no");
            string residentName = BuildResidentName(row);
            string businessName = SafeString(row, "business_name");
            string businessNature = SafeString(row, "business_nature");
            string statusLabel = FormatStatusLabel(SafeString(row, "status"));
            string requested = FormatDateTime(ReadDateTime(row, "requested_at"));
            string approved = FormatDateTime(ReadDateTime(row, "approved_at"));
            string released = FormatDateTime(ReadDateTime(row, "released_at"));
            string orNumber = SafeString(row, "or_number");
            string fee = FormatFee(ReadDecimal(row, "fee"));

            table.Rows.Add(
                certificateId,
                residentId,
                string.IsNullOrWhiteSpace(docNo) ? "-" : docNo,
                string.IsNullOrWhiteSpace(businessName) ? "-" : businessName,
                string.IsNullOrWhiteSpace(businessNature) ? "-" : businessNature,
                residentName,
                statusLabel,
                requested,
                approved,
                released,
                string.IsNullOrWhiteSpace(orNumber) ? "-" : orNumber,
                fee);
        }

        return table;
    }
    private void ConfigureGridColumns()
    {
        if (_grid.Columns.Contains("certificate_id"))
        {
            _grid.Columns["certificate_id"].Visible = false;
        }
        if (_grid.Columns.Contains("resident_id"))
        {
            _grid.Columns["resident_id"].Visible = false;
        }
        if (_grid.Columns.Contains("document_no"))
        {
            _grid.Columns["document_no"].HeaderText = "Permit No";
            _grid.Columns["document_no"].FillWeight = 85;
        }
        if (_grid.Columns.Contains("business_name"))
        {
            _grid.Columns["business_name"].HeaderText = "Business Name";
            _grid.Columns["business_name"].FillWeight = 170;
        }
        if (_grid.Columns.Contains("business_nature"))
        {
            _grid.Columns["business_nature"].HeaderText = "Nature";
            _grid.Columns["business_nature"].FillWeight = 120;
        }
        if (_grid.Columns.Contains("resident_name"))
        {
            _grid.Columns["resident_name"].HeaderText = "Owner";
            _grid.Columns["resident_name"].FillWeight = 120;
        }
        if (_grid.Columns.Contains("status"))
        {
            _grid.Columns["status"].HeaderText = "Status";
            _grid.Columns["status"].FillWeight = 80;
        }
        if (_grid.Columns.Contains("requested_at"))
        {
            _grid.Columns["requested_at"].HeaderText = "Requested";
            _grid.Columns["requested_at"].FillWeight = 105;
        }
        if (_grid.Columns.Contains("approved_at"))
        {
            _grid.Columns["approved_at"].HeaderText = "Approved";
            _grid.Columns["approved_at"].FillWeight = 105;
        }
        if (_grid.Columns.Contains("released_at"))
        {
            _grid.Columns["released_at"].HeaderText = "Released";
            _grid.Columns["released_at"].FillWeight = 105;
        }
        if (_grid.Columns.Contains("or_number"))
        {
            _grid.Columns["or_number"].HeaderText = "OR No";
            _grid.Columns["or_number"].FillWeight = 90;
        }
        if (_grid.Columns.Contains("fee"))
        {
            _grid.Columns["fee"].HeaderText = "Fee";
            _grid.Columns["fee"].FillWeight = 70;
        }
    }

    private void UpdatePager(int totalPages)
    {
        if (totalPages <= 0)
        {
            totalPages = 1;
            _currentPage = 1;
        }

        _prevButton.Enabled = _currentPage > 1;
        _nextButton.Enabled = _currentPage < totalPages;
        _pageInfoLabel.Text = $"Page {_currentPage} of {totalPages} | {_totalRows} record(s)";
    }

    private void ChangePage(int delta)
    {
        int next = _currentPage + delta;
        if (next <= 0)
        {
            return;
        }

        _currentPage = next;
        RefreshList(resetPage: false);
    }

    private void UpdateStatePanel(int rowCount)
    {
        if (!HasCertificateAccess)
        {
            ShowNoPermissionState();
            return;
        }

        if (rowCount > 0)
        {
            HideState();
            return;
        }

        bool hasFilters = HasActiveFilters();
        string title = hasFilters ? "No permits found" : "No permit requests yet";
        string message = hasFilters
            ? "Try adjusting the search text, status filter, or date range."
            : "Create your first business clearance permit request to get started.";

        Panel card = UiTheme.CreateStateCard(
            title,
            message,
            IconChar.FileSignature,
            UiTheme.Slate500,
            Permissions.CanRequestCertificates ? "New Request" : null,
            Permissions.CanRequestCertificates ? new Action(OpenNewRequest) : null);

        ShowState(card);
    }

    private void ShowNoPermissionState()
    {
        Panel card = UiTheme.CreateStateCard(
            "Access restricted",
            "You do not have permission to view permit records.",
            IconChar.Lock,
            UiTheme.AccentRed);
        ShowState(card);
    }

    private void ShowErrorState(string message)
    {
        Panel card = UiTheme.CreateStateCard(
            "Something went wrong",
            message,
            IconChar.TriangleExclamation,
            UiTheme.AccentRed);
        ShowState(card);
    }

    private void ShowState(Panel card)
    {
        _statePanel.Controls.Clear();
        _statePanel.Visible = true;
        _statePanel.BringToFront();
        card.Dock = DockStyle.Top;
        _statePanel.Controls.Add(card);
    }

    private void HideState()
    {
        _statePanel.Visible = false;
        _statePanel.Controls.Clear();
    }

    private bool HasActiveFilters()
    {
        return !string.IsNullOrWhiteSpace(_searchBox.Text)
               || !string.IsNullOrWhiteSpace(GetSelectedStatus())
               || _fromDate.Checked
               || _toDate.Checked;
    }

    private string? GetSelectedStatus()
    {
        if (_statusFilter.SelectedItem is StatusOption option)
        {
            return string.IsNullOrWhiteSpace(option.Value) ? null : option.Value;
        }

        if (_statusFilter.SelectedValue is string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private void OpenNewRequest()
    {
        if (!Permissions.CanRequestCertificates)
        {
            ControllerDialogs.Warning("You do not have permission to create permit requests.");
            return;
        }

        _openCertificates(CertificateAction.NewRequest);
    }

    private void ApproveSelected()
    {
        if (!Permissions.CanApproveCertificates)
        {
            ControllerDialogs.Warning("You do not have permission to approve permits.");
            return;
        }

        if (!TryGetSelectedCertificate(out int residentId, out int certificateId))
        {
            ControllerDialogs.Warning("Select a permit first.");
            return;
        }

        _openCertificateById(residentId, certificateId);
        _openCertificates(CertificateAction.Approve);
    }

    private void ReleaseSelected()
    {
        if (!Permissions.CanIssueCertificates)
        {
            ControllerDialogs.Warning("You do not have permission to release permits.");
            return;
        }

        if (!TryGetSelectedCertificate(out int residentId, out int certificateId))
        {
            ControllerDialogs.Warning("Select a permit first.");
            return;
        }

        _openCertificateById(residentId, certificateId);
        _openCertificates(CertificateAction.Issue);
    }

    private void PrintSelected()
    {
        if (!TryGetSelectedCertificate(out int residentId, out int certificateId))
        {
            ControllerDialogs.Warning("Select a permit first.");
            return;
        }

        _openCertificateById(residentId, certificateId);
        _openCertificates(CertificateAction.Print);
    }

    private void OpenSelected()
    {
        if (!TryGetSelectedCertificate(out int residentId, out int certificateId))
        {
            ControllerDialogs.Warning("Select a permit first.");
            return;
        }

        _openCertificateById(residentId, certificateId);
    }
    private async void ExportListAsync()
    {
        if (!Permissions.CanExportCertificates)
        {
            ControllerDialogs.Warning("You do not have permission to export permits.");
            return;
        }

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        CancellationToken token = _loadCts.Token;

        SetLoading(true, "Preparing export...");

        try
        {
            DataTable raw = await LoadAllAsync(token);
            if (raw.Rows.Count == 0)
            {
                ControllerDialogs.Warning("Nothing to export.", "Export Permits");
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Export Permits",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"permits_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            DataTable display = BuildDisplayTable(raw);
            var sb = new StringBuilder();
            sb.AppendLine("Permit No,Business Name,Business Nature,Owner,Status,Requested,Approved,Released,OR No,Fee");

            foreach (DataRow row in display.Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    EscapeCsv(Convert.ToString(row["document_no"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["business_name"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["business_nature"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["resident_name"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["status"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["requested_at"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["approved_at"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["released_at"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["or_number"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["fee"]) ?? string.Empty)
                }));
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            ControllerDialogs.Info("Permit export completed.", "Export Permits");
        }
        catch (OperationCanceledException)
        {
            // Ignore canceled export.
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to export permits.", "Export Permits");
        }
        finally
        {
            SetLoading(false, string.Empty);
        }
    }
    private bool TryGetSelectedCertificate(out int residentId, out int certificateId)
    {
        residentId = 0;
        certificateId = 0;

        if (_grid.CurrentRow == null || !_grid.Columns.Contains("certificate_id") || !_grid.Columns.Contains("resident_id"))
        {
            return false;
        }

        object? certValue = _grid.CurrentRow.Cells["certificate_id"]?.Value;
        object? resValue = _grid.CurrentRow.Cells["resident_id"]?.Value;
        if (certValue == null || certValue == DBNull.Value || resValue == null || resValue == DBNull.Value)
        {
            return false;
        }

        certificateId = Convert.ToInt32(certValue);
        residentId = Convert.ToInt32(resValue);
        return certificateId > 0 && residentId > 0;
    }

    private void SetLoading(bool loading, string message)
    {
        if (loading)
        {
            _loadingOverlay.ShowLoading(message);
        }
        else
        {
            _loadingOverlay.HideLoading();
        }
    }

    private static string SafeString(DataRow row, string column)
    {
        return row.Table.Columns.Contains(column)
            ? Convert.ToString(row[column]) ?? string.Empty
            : string.Empty;
    }

    private static int ReadInt(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return 0;
        }

        return int.TryParse(Convert.ToString(row[column]), out int value) ? value : 0;
    }

    private static decimal ReadDecimal(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return 0m;
        }

        return decimal.TryParse(Convert.ToString(row[column]), out decimal value) ? value : 0m;
    }

    private static DateTime? ReadDateTime(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column))
        {
            return null;
        }

        object? value = row[column];
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        if (value is DateTime dateTime)
        {
            return dateTime;
        }

        return DateTime.TryParse(Convert.ToString(value), out DateTime parsed) ? parsed : null;
    }

    private static string BuildResidentName(DataRow row)
    {
        string first = SafeString(row, "first_name").Trim();
        string middle = SafeString(row, "middle_name").Trim();
        string last = SafeString(row, "last_name").Trim();
        string suffix = SafeString(row, "suffix").Trim();

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(first))
        {
            parts.Add(first);
        }
        if (!string.IsNullOrWhiteSpace(middle))
        {
            parts.Add(middle);
        }
        if (!string.IsNullOrWhiteSpace(last))
        {
            parts.Add(last);
        }

        string name = parts.Count == 0 ? string.Empty : string.Join(" ", parts);
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            name = string.IsNullOrWhiteSpace(name) ? suffix : name + " " + suffix;
        }

        return string.IsNullOrWhiteSpace(name) ? "-" : name;
    }

    private static string FormatStatusLabel(string? status)
    {
        string normalized = WorkflowRules.NormalizeCertificateStatus(status);
        return normalized switch
        {
            "SUBMITTED" => "Requested",
            "APPROVED" => "Approved",
            "RELEASED" => "Issued",
            "CANCELLED" => "Cancelled",
            "REJECTED" => "Rejected",
            "DRAFT" => "Draft",
            _ => normalized
        };
    }

    private static string FormatDateTime(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("MMM dd, yyyy hh:mm tt") : "-";
    }

    private static string FormatFee(decimal fee)
    {
        return fee <= 0 ? "0.00" : fee.ToString("N2");
    }

    private static string EscapeCsv(string text)
    {
        string safe = text ?? string.Empty;
        bool mustQuote = safe.Contains(',') || safe.Contains('"') || safe.Contains('\n') || safe.Contains('\r');
        if (safe.Contains('"'))
        {
            safe = safe.Replace("\"", "\"\"");
        }

        return mustQuote ? $"\"{safe}\"" : safe;
    }
}
