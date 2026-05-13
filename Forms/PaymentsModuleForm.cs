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

internal sealed class PaymentsModuleForm : Form
{
    private sealed class FilterOption
    {
        public string Value { get; }
        public string Label { get; }

        public FilterOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        public override string ToString() => Label;
    }

    private sealed class DocTypeOption
    {
        public int Id { get; }
        public string Name { get; }

        public DocTypeOption(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString() => Name;
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

    private readonly Action<int, int> _openCertificateById;

    private readonly Label _titleLabel = new Label();
    private readonly Label _subtitleLabel = new Label();

    private readonly FlowLayoutPanel _actionPanel = new FlowLayoutPanel();
    private readonly Button _exportButton = new Button();
    private readonly Button _openButton = new Button();
    private readonly Button _refreshButton = new Button();

    private readonly TextBox _searchBox = new TextBox();
    private readonly ComboBox _typeFilter = new ComboBox();
    private readonly ComboBox _methodFilter = new ComboBox();
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

    private bool HasPaymentAccess => Permissions.IsAdmin
        || Permissions.CanRequestCertificates
        || Permissions.CanEditCertificateRequests
        || Permissions.CanApproveCertificates
        || Permissions.CanIssueCertificates
        || Permissions.CanCancelCertificates
        || Permissions.CanExportCertificates;

    public PaymentsModuleForm(Action<int, int> openCertificateById)
    {
        _openCertificateById = openCertificateById ?? throw new ArgumentNullException(nameof(openCertificateById));
        _barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);

        InitializeComponent();
        LoadDocumentTypes();
        LoadMethodOptions();
        ApplyRolePermissions();
        RefreshList(resetPage: true);
    }

    private void InitializeComponent()
    {
        Text = "Payments";
        Name = "PaymentsModuleForm";
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
        _titleLabel.Text = "Payments";
        _titleLabel.Font = UiTheme.HeadingFont;
        _titleLabel.ForeColor = UiTheme.Slate900;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(0, 0);

        _subtitleLabel.Text = "Track document payments, OR numbers, and receipts.";
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

        _openButton.Text = "Open Certificate";
        _exportButton.Text = "Export";
        _refreshButton.Text = "Refresh";

        UiTheme.StylePrimaryButton(_openButton);
        UiTheme.StyleSecondaryButton(_exportButton);
        UiTheme.StyleSecondaryButton(_refreshButton);

        _actionPanel.Controls.Add(_openButton);
        _actionPanel.Controls.Add(_exportButton);
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
            ColumnCount = 5,
            RowCount = 2,
            BackColor = Color.White
        };
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterCard.Controls.Add(filterLayout);

        _searchBox.PlaceholderText = "Search by OR no, document no, business name, or owner...";
        _searchBox.Dock = DockStyle.Fill;
        UiTheme.StyleTextBox(_searchBox);

        _typeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _typeFilter.Dock = DockStyle.Fill;
        UiTheme.StyleComboBox(_typeFilter);

        _methodFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _methodFilter.Dock = DockStyle.Fill;
        UiTheme.StyleComboBox(_methodFilter);

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
        filterLayout.Controls.Add(_typeFilter, 1, 0);
        filterLayout.SetRowSpan(_typeFilter, 2);
        filterLayout.Controls.Add(_methodFilter, 2, 0);
        filterLayout.SetRowSpan(_methodFilter, 2);
        filterLayout.Controls.Add(fromLabel, 3, 0);
        filterLayout.Controls.Add(toLabel, 4, 0);
        filterLayout.Controls.Add(_fromDate, 3, 1);
        filterLayout.Controls.Add(_toDate, 4, 1);

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

        _typeFilter.SelectedIndexChanged += (_, _) => RefreshList(resetPage: true);
        _methodFilter.SelectedIndexChanged += (_, _) => RefreshList(resetPage: true);
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

    private void LoadMethodOptions()
    {
        var options = new List<FilterOption>
        {
            new FilterOption(string.Empty, "All Methods"),
            new FilterOption("Cash", "Cash"),
            new FilterOption("GCash", "GCash"),
            new FilterOption("Bank", "Bank")
        };

        _methodFilter.DisplayMember = nameof(FilterOption.Label);
        _methodFilter.ValueMember = nameof(FilterOption.Value);
        _methodFilter.DataSource = options;
        _methodFilter.SelectedIndex = 0;
    }

    private void LoadDocumentTypes()
    {
        var options = new List<DocTypeOption> { new DocTypeOption(0, "All Types") };
        try
        {
            DataTable table = DbHelper.LoadTable("SELECT doc_type_id, name FROM document_type ORDER BY name");
            foreach (DataRow row in table.Rows)
            {
                int id = row["doc_type_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["doc_type_id"]);
                string name = Convert.ToString(row["name"]) ?? string.Empty;
                if (id > 0 && !string.IsNullOrWhiteSpace(name))
                {
                    options.Add(new DocTypeOption(id, name.Trim()));
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning("Unable to load document types for payments filter.", ex);
        }

        _typeFilter.DisplayMember = nameof(DocTypeOption.Name);
        _typeFilter.ValueMember = nameof(DocTypeOption.Id);
        _typeFilter.DataSource = options;
        _typeFilter.SelectedIndex = 0;
    }

    private void ApplyRolePermissions()
    {
        bool hasAccess = HasPaymentAccess;
        _openButton.Enabled = hasAccess;
        _exportButton.Enabled = hasAccess && Permissions.CanExportCertificates;
        _refreshButton.Enabled = hasAccess;
        _searchBox.Enabled = hasAccess;
        _typeFilter.Enabled = hasAccess;
        _methodFilter.Enabled = hasAccess;
        _fromDate.Enabled = hasAccess;
        _toDate.Enabled = hasAccess;

        if (!Permissions.CanExportCertificates)
        {
            _toolTip.SetToolTip(_exportButton, "No permission: certificates.export");
        }

        UpdateActionState();
    }

    private void UpdateActionState()
    {
        bool hasAccess = HasPaymentAccess;
        bool hasSelection = TryGetSelectedPayment(out _, out _);

        _openButton.Enabled = hasAccess && hasSelection;
        _exportButton.Enabled = hasAccess && Permissions.CanExportCertificates;
        _refreshButton.Enabled = hasAccess;
    }

    private async void RefreshList(bool resetPage)
    {
        if (resetPage)
        {
            _currentPage = 1;
        }

        if (!HasPaymentAccess)
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

        SetLoading(true, "Loading payments...");

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
            ControllerDialogs.Error(ex, "Unable to load payments.", "Payments");
            ShowErrorState("Unable to load payments right now.");
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
FROM document_payment dp
INNER JOIN document_request dr ON dr.doc_request_id = dp.doc_request_id
LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
LEFT JOIN resident r ON r.resident_id = dr.resident_id
LEFT JOIN user_account u ON u.user_id = dp.received_by_user_id
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
SELECT dp.payment_id,
       dr.doc_request_id,
       dr.resident_id,
       dp.amount,
       dp.or_no,
       dp.payment_method,
       dp.paid_at,
       u.username AS received_by,
       dt.name AS document_type,
       dr.document_no,
       dr.business_name,
       dr.business_nature,
       r.first_name,
       r.middle_name,
       r.last_name,
       r.suffix
FROM document_payment dp
INNER JOIN document_request dr ON dr.doc_request_id = dp.doc_request_id
LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
LEFT JOIN resident r ON r.resident_id = dr.resident_id
LEFT JOIN user_account u ON u.user_id = dp.received_by_user_id
{whereClause}
ORDER BY dp.paid_at DESC, dp.payment_id DESC
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
SELECT dp.payment_id,
       dr.doc_request_id,
       dr.resident_id,
       dp.amount,
       dp.or_no,
       dp.payment_method,
       dp.paid_at,
       u.username AS received_by,
       dt.name AS document_type,
       dr.document_no,
       dr.business_name,
       dr.business_nature,
       r.first_name,
       r.middle_name,
       r.last_name,
       r.suffix
FROM document_payment dp
INNER JOIN document_request dr ON dr.doc_request_id = dp.doc_request_id
LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
LEFT JOIN resident r ON r.resident_id = dr.resident_id
LEFT JOIN user_account u ON u.user_id = dp.received_by_user_id
{whereClause}
ORDER BY dp.paid_at DESC, dp.payment_id DESC";

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

        int? docTypeId = GetSelectedDocTypeId();
        if (docTypeId.HasValue)
        {
            context.Conditions.Add("dr.doc_type_id = @docTypeId");
            context.Parameters.Add(("@docTypeId", docTypeId.Value));
        }

        string? method = GetSelectedMethod();
        if (!string.IsNullOrWhiteSpace(method))
        {
            context.Conditions.Add("dp.payment_method = @paymentMethod");
            context.Parameters.Add(("@paymentMethod", method));
        }

        string search = _searchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            context.Conditions.Add(@"
(
    dp.or_no LIKE @search
    OR dr.document_no LIKE @search
    OR dr.business_name LIKE @search
    OR dr.business_nature LIKE @search
    OR r.first_name LIKE @search
    OR r.middle_name LIKE @search
    OR r.last_name LIKE @search
    OR r.suffix LIKE @search
    OR dt.name LIKE @search
    OR dp.payment_method LIKE @search
    OR u.username LIKE @search
)");
            context.Parameters.Add(("@search", $"%{search}%"));
        }

        if (_fromDate.Checked)
        {
            DateTime from = _fromDate.Value.Date;
            context.Conditions.Add("dp.paid_at >= @fromDate");
            context.Parameters.Add(("@fromDate", from.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        if (_toDate.Checked)
        {
            DateTime to = _toDate.Value.Date.AddDays(1);
            context.Conditions.Add("dp.paid_at < @toDate");
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
        table.Columns.Add("payment_id", typeof(int));
        table.Columns.Add("doc_request_id", typeof(int));
        table.Columns.Add("resident_id", typeof(int));
        table.Columns.Add("or_no", typeof(string));
        table.Columns.Add("amount", typeof(string));
        table.Columns.Add("payment_method", typeof(string));
        table.Columns.Add("paid_at", typeof(string));
        table.Columns.Add("document_type", typeof(string));
        table.Columns.Add("document_no", typeof(string));
        table.Columns.Add("business_name", typeof(string));
        table.Columns.Add("resident_name", typeof(string));
        table.Columns.Add("received_by", typeof(string));

        foreach (DataRow row in source.Rows)
        {
            int paymentId = ReadInt(row, "payment_id");
            int requestId = ReadInt(row, "doc_request_id");
            int residentId = ReadInt(row, "resident_id");
            string orNo = SafeString(row, "or_no");
            string amount = FormatFee(ReadDecimal(row, "amount"));
            string method = SafeString(row, "payment_method");
            string paidAt = FormatDateTime(ReadDateTime(row, "paid_at"));
            string docType = SafeString(row, "document_type");
            string docNo = SafeString(row, "document_no");
            string businessName = SafeString(row, "business_name");
            string residentName = BuildResidentName(row);
            string receivedBy = SafeString(row, "received_by");

            table.Rows.Add(
                paymentId,
                requestId,
                residentId,
                string.IsNullOrWhiteSpace(orNo) ? "-" : orNo,
                amount,
                string.IsNullOrWhiteSpace(method) ? "-" : method,
                paidAt,
                string.IsNullOrWhiteSpace(docType) ? "-" : docType,
                string.IsNullOrWhiteSpace(docNo) ? "-" : docNo,
                string.IsNullOrWhiteSpace(businessName) ? "-" : businessName,
                residentName,
                string.IsNullOrWhiteSpace(receivedBy) ? "-" : receivedBy);
        }

        return table;
    }
    private void ConfigureGridColumns()
    {
        if (_grid.Columns.Contains("payment_id"))
        {
            _grid.Columns["payment_id"].Visible = false;
        }
        if (_grid.Columns.Contains("doc_request_id"))
        {
            _grid.Columns["doc_request_id"].Visible = false;
        }
        if (_grid.Columns.Contains("resident_id"))
        {
            _grid.Columns["resident_id"].Visible = false;
        }
        if (_grid.Columns.Contains("or_no"))
        {
            _grid.Columns["or_no"].HeaderText = "OR No";
            _grid.Columns["or_no"].FillWeight = 90;
        }
        if (_grid.Columns.Contains("amount"))
        {
            _grid.Columns["amount"].HeaderText = "Amount";
            _grid.Columns["amount"].FillWeight = 70;
        }
        if (_grid.Columns.Contains("payment_method"))
        {
            _grid.Columns["payment_method"].HeaderText = "Method";
            _grid.Columns["payment_method"].FillWeight = 80;
        }
        if (_grid.Columns.Contains("paid_at"))
        {
            _grid.Columns["paid_at"].HeaderText = "Paid At";
            _grid.Columns["paid_at"].FillWeight = 110;
        }
        if (_grid.Columns.Contains("document_type"))
        {
            _grid.Columns["document_type"].HeaderText = "Document Type";
            _grid.Columns["document_type"].FillWeight = 130;
        }
        if (_grid.Columns.Contains("document_no"))
        {
            _grid.Columns["document_no"].HeaderText = "Document No";
            _grid.Columns["document_no"].FillWeight = 90;
        }
        if (_grid.Columns.Contains("business_name"))
        {
            _grid.Columns["business_name"].HeaderText = "Business Name";
            _grid.Columns["business_name"].FillWeight = 140;
        }
        if (_grid.Columns.Contains("resident_name"))
        {
            _grid.Columns["resident_name"].HeaderText = "Owner";
            _grid.Columns["resident_name"].FillWeight = 120;
        }
        if (_grid.Columns.Contains("received_by"))
        {
            _grid.Columns["received_by"].HeaderText = "Received By";
            _grid.Columns["received_by"].FillWeight = 110;
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
        if (!HasPaymentAccess)
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
        string title = hasFilters ? "No payments found" : "No payment records yet";
        string message = hasFilters
            ? "Try adjusting the search text, type filter, method filter, or date range."
            : "Payments will appear here once documents are issued and paid.";

        Panel card = UiTheme.CreateStateCard(
            title,
            message,
            IconChar.FileSignature,
            UiTheme.Slate500);

        ShowState(card);
    }

    private void ShowNoPermissionState()
    {
        Panel card = UiTheme.CreateStateCard(
            "Access restricted",
            "You do not have permission to view payment records.",
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
               || GetSelectedDocTypeId().HasValue
               || !string.IsNullOrWhiteSpace(GetSelectedMethod())
               || _fromDate.Checked
               || _toDate.Checked;
    }

    private string? GetSelectedMethod()
    {
        if (_methodFilter.SelectedItem is FilterOption option)
        {
            return string.IsNullOrWhiteSpace(option.Value) ? null : option.Value;
        }

        if (_methodFilter.SelectedValue is string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private int? GetSelectedDocTypeId()
    {
        if (_typeFilter.SelectedItem is DocTypeOption option)
        {
            return option.Id > 0 ? option.Id : (int?)null;
        }

        if (_typeFilter.SelectedValue is int id)
        {
            return id > 0 ? id : (int?)null;
        }

        if (_typeFilter.SelectedValue is string text && int.TryParse(text, out int parsed))
        {
            return parsed > 0 ? parsed : (int?)null;
        }

        return null;
    }

    private void OpenSelected()
    {
        if (!TryGetSelectedPayment(out int residentId, out int requestId))
        {
            ControllerDialogs.Warning("Select a payment first.");
            return;
        }

        _openCertificateById(residentId, requestId);
    }
    private async void ExportListAsync()
    {
        if (!Permissions.CanExportCertificates)
        {
            ControllerDialogs.Warning("You do not have permission to export payments.");
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
                ControllerDialogs.Warning("Nothing to export.", "Export Payments");
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Export Payments",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"payments_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            DataTable display = BuildDisplayTable(raw);
            var sb = new StringBuilder();
            sb.AppendLine("OR No,Amount,Method,Paid At,Received By,Document Type,Document No,Business Name,Owner");

            foreach (DataRow row in display.Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    EscapeCsv(Convert.ToString(row["or_no"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["amount"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["payment_method"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["paid_at"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["received_by"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["document_type"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["document_no"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["business_name"]) ?? string.Empty),
                    EscapeCsv(Convert.ToString(row["resident_name"]) ?? string.Empty)
                }));
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            ControllerDialogs.Info("Payment export completed.", "Export Payments");
        }
        catch (OperationCanceledException)
        {
            // Ignore canceled export.
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to export payments.", "Export Payments");
        }
        finally
        {
            SetLoading(false, string.Empty);
        }
    }
    private bool TryGetSelectedPayment(out int residentId, out int requestId)
    {
        residentId = 0;
        requestId = 0;

        if (_grid.CurrentRow == null || !_grid.Columns.Contains("doc_request_id") || !_grid.Columns.Contains("resident_id"))
        {
            return false;
        }

        object? requestValue = _grid.CurrentRow.Cells["doc_request_id"]?.Value;
        object? resValue = _grid.CurrentRow.Cells["resident_id"]?.Value;
        if (requestValue == null || requestValue == DBNull.Value || resValue == null || resValue == DBNull.Value)
        {
            return false;
        }

        requestId = Convert.ToInt32(requestValue);
        residentId = Convert.ToInt32(resValue);
        return requestId > 0 && residentId > 0;
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
