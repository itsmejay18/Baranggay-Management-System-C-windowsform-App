using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class HouseholdModuleForm : Form
{
    private readonly HouseholdRepository _householdRepository;

    private readonly Label _titleLabel = new Label();
    private readonly Label _subtitleLabel = new Label();
    private readonly Button _newButton = new Button();
    private readonly Button _exportButton = new Button();
    private readonly Button _refreshButton = new Button();

    private readonly TextBox _searchBox = new TextBox();
    private readonly ComboBox _purokFilter = new ComboBox();
    private readonly CheckBox _withSeniorsFilter = new CheckBox();
    private readonly CheckBox _withPwdFilter = new CheckBox();
    private readonly CheckBox _with4PsFilter = new CheckBox();
    private readonly CheckBox _emptyFilter = new CheckBox();
    private readonly CheckBox _activeCasesFilter = new CheckBox();

    private readonly DataGridView _grid = new DataGridView();
    private readonly Panel _statePanel = new Panel();
    private readonly Label _stateLabel = new Label();
    private readonly Button _stateCreateButton = new Button();

    private readonly Button _prevButton = new Button();
    private readonly Button _nextButton = new Button();
    private readonly Label _pageInfoLabel = new Label();
    private readonly ComboBox _pageSizeCombo = new ComboBox();

    private readonly System.Windows.Forms.Timer _searchDebounce = new System.Windows.Forms.Timer();
    private readonly ToolTip _toolTip = new ToolTip();

    private int _currentPage = 1;
    private int _pageSize = 25;
    private int _totalRows;
    private readonly int _barangayId;

    public HouseholdModuleForm()
        : this(new HouseholdRepository())
    {
    }

    internal HouseholdModuleForm(HouseholdRepository householdRepository)
    {
        _householdRepository = householdRepository ?? throw new ArgumentNullException(nameof(householdRepository));
        _barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);

        InitializeComponent();
        LoadPurokOptions();
        RefreshList(resetPage: true);
    }

    private void InitializeComponent()
    {
        Text = "Households";
        Name = "HouseholdModuleForm";
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
        _titleLabel.Text = "Households";
        _titleLabel.Font = UiTheme.HeadingFont;
        _titleLabel.ForeColor = UiTheme.Slate900;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(0, 0);

        _subtitleLabel.Text = "Manage household records, addresses, and member assignments.";
        _subtitleLabel.Font = UiTheme.LabelFont;
        _subtitleLabel.ForeColor = UiTheme.Slate600;
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Location = new Point(0, 30);

        headerPanel.Controls.Add(_titleLabel);
        headerPanel.Controls.Add(_subtitleLabel);
        root.Controls.Add(headerPanel, 0, 0);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };

        _newButton.Text = "New Household";
        _exportButton.Text = "Export";
        _refreshButton.Text = "Refresh";
        _newButton.Click += NewButton_Click;
        _exportButton.Click += ExportButton_Click;
        _refreshButton.Click += (_, _) => RefreshList(resetPage: false);
        UiTheme.StylePrimaryButton(_newButton);
        UiTheme.StyleSecondaryButton(_exportButton);
        UiTheme.StyleSecondaryButton(_refreshButton);
        actionPanel.Controls.Add(_newButton);
        actionPanel.Controls.Add(_exportButton);
        actionPanel.Controls.Add(_refreshButton);
        root.Controls.Add(actionPanel, 0, 1);

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
            ColumnCount = 7,
            RowCount = 2,
            BackColor = Color.White
        };
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        filterCard.Controls.Add(filterLayout);

        _searchBox.PlaceholderText = "Search by house no, street, subdivision, or resident name...";
        _searchBox.Dock = DockStyle.Fill;
        UiTheme.StyleTextBox(_searchBox);

        _purokFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _purokFilter.Dock = DockStyle.Fill;
        UiTheme.StyleComboBox(_purokFilter);

        ConfigureFilterCheck(_withSeniorsFilter, "With Seniors");
        ConfigureFilterCheck(_withPwdFilter, "With PWD");
        ConfigureFilterCheck(_with4PsFilter, "With 4Ps");
        ConfigureFilterCheck(_emptyFilter, "Empty Household");
        ConfigureFilterCheck(_activeCasesFilter, "Has Active Cases");

        filterLayout.Controls.Add(_searchBox, 0, 0);
        filterLayout.SetRowSpan(_searchBox, 2);
        filterLayout.Controls.Add(_purokFilter, 1, 0);
        filterLayout.SetRowSpan(_purokFilter, 2);
        filterLayout.Controls.Add(_withSeniorsFilter, 2, 0);
        filterLayout.Controls.Add(_withPwdFilter, 3, 0);
        filterLayout.Controls.Add(_with4PsFilter, 4, 0);
        filterLayout.Controls.Add(_emptyFilter, 5, 0);
        filterLayout.Controls.Add(_activeCasesFilter, 6, 0);

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
        _grid.CellContentClick += Grid_CellContentClick;
        UiTheme.StyleGrid(_grid);
        gridCard.Controls.Add(_grid);

        _statePanel.Dock = DockStyle.Fill;
        _statePanel.BackColor = Color.FromArgb(248, 249, 252);
        _statePanel.Visible = false;
        gridCard.Controls.Add(_statePanel);

        var stateLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        stateLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        stateLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stateLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        _statePanel.Controls.Add(stateLayout);

        var stateContent = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Anchor = AnchorStyles.None
        };
        _stateLabel.AutoSize = true;
        _stateLabel.Font = UiTheme.BodyFont;
        _stateLabel.ForeColor = UiTheme.Slate600;
        _stateLabel.MaximumSize = new Size(620, 0);
        _stateLabel.TextAlign = ContentAlignment.MiddleCenter;

        _stateCreateButton.Text = "Create Household";
        UiTheme.StylePrimaryButton(_stateCreateButton);
        _stateCreateButton.AutoSize = true;
        _stateCreateButton.Click += NewButton_Click;

        stateContent.Controls.Add(_stateLabel);
        stateContent.Controls.Add(_stateCreateButton);
        stateLayout.Controls.Add(stateContent, 0, 1);

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
        _prevButton.Click += (_, _) => ChangePage(-1);
        _nextButton.Click += (_, _) => ChangePage(1);
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

        pagerPanel.Controls.Add(new Label { Text = "Rows", AutoSize = true, Padding = new Padding(0, 8, 4, 0), ForeColor = UiTheme.Slate700 });
        pagerPanel.Controls.Add(_pageSizeCombo);
        pagerPanel.Controls.Add(_prevButton);
        pagerPanel.Controls.Add(_nextButton);
        pagerPanel.Controls.Add(_pageInfoLabel);
        root.Controls.Add(pagerPanel, 0, 4);

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

        _purokFilter.SelectedIndexChanged += (_, _) => RefreshList(resetPage: true);
        _withSeniorsFilter.CheckedChanged += (_, _) => RefreshList(resetPage: true);
        _withPwdFilter.CheckedChanged += (_, _) => RefreshList(resetPage: true);
        _with4PsFilter.CheckedChanged += (_, _) => RefreshList(resetPage: true);
        _emptyFilter.CheckedChanged += (_, _) => RefreshList(resetPage: true);
        _activeCasesFilter.CheckedChanged += (_, _) => RefreshList(resetPage: true);
        _pageSizeCombo.SelectedIndexChanged += (_, _) =>
        {
            if (int.TryParse(Convert.ToString(_pageSizeCombo.SelectedItem), out int parsed) && parsed > 0)
            {
                _pageSize = parsed;
                RefreshList(resetPage: true);
            }
        };

        if (!Permissions.CanViewHouseholds)
        {
            _toolTip.SetToolTip(_newButton, "No permission: household.create");
            _toolTip.SetToolTip(_exportButton, "No permission: household.view");
            _toolTip.SetToolTip(_refreshButton, "No permission: household.view");
        }

        _newButton.Enabled = Permissions.CanCreateHouseholds;
        _exportButton.Enabled = Permissions.CanViewHouseholds;
        _refreshButton.Enabled = Permissions.CanViewHouseholds;

        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
    }

    private static void ConfigureFilterCheck(CheckBox checkBox, string text)
    {
        checkBox.Text = text;
        checkBox.AutoSize = true;
        checkBox.ForeColor = UiTheme.Slate700;
        checkBox.Font = UiTheme.LabelFont;
        checkBox.Margin = new Padding(0, 2, 8, 2);
    }

    private void LoadPurokOptions()
    {
        try
        {
            var all = new List<LookupItem> { new LookupItem(0, "All Purok/Sitio") };
            all.AddRange(_householdRepository.GetPurokOptions(_barangayId));

            _purokFilter.DataSource = null;
            _purokFilter.DisplayMember = nameof(LookupItem.Name);
            _purokFilter.ValueMember = nameof(LookupItem.Id);
            _purokFilter.DataSource = all;
            _purokFilter.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load purok filters.", "Households");
        }
    }

    private void RefreshList(bool resetPage)
    {
        if (resetPage)
        {
            _currentPage = 1;
        }

        if (!Permissions.CanViewHouseholds)
        {
            _grid.DataSource = null;
            _statePanel.Visible = true;
            _stateLabel.Text = "You do not have permission to view households.\nRequired permission: household.view";
            _stateCreateButton.Visible = false;
            _pageInfoLabel.Text = "";
            return;
        }

        try
        {
            var result = _householdRepository.Search(new HouseholdListFilters
            {
                BarangayId = _barangayId,
                SearchText = _searchBox.Text,
                PurokId = GetSelectedPurokId(),
                WithSeniors = _withSeniorsFilter.Checked,
                WithPwd = _withPwdFilter.Checked,
                With4Ps = _with4PsFilter.Checked,
                EmptyHouseholdOnly = _emptyFilter.Checked,
                HasActiveCasesOnly = _activeCasesFilter.Checked,
                PageNumber = _currentPage,
                PageSize = _pageSize
            });

            _totalRows = result.TotalRows;
            _currentPage = result.PageNumber;

            BindGrid(result.Items);
            UpdatePager(result.TotalPages);
            UpdateStatePanel(result.Items.Count);
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load households.", "Households");
            _statePanel.Visible = true;
            _stateLabel.Text = "Unable to load households right now.";
            _stateCreateButton.Visible = false;
        }
    }

    private void BindGrid(IReadOnlyList<HouseholdListItem> rows)
    {
        var table = new DataTable();
        table.Columns.Add("household_id", typeof(int));
        table.Columns.Add("house_no", typeof(string));
        table.Columns.Add("street", typeof(string));
        table.Columns.Add("subdivision", typeof(string));
        table.Columns.Add("purok", typeof(string));
        table.Columns.Add("members", typeof(int));
        table.Columns.Add("updated_at", typeof(string));

        foreach (HouseholdListItem row in rows)
        {
            table.Rows.Add(
                row.HouseholdId,
                row.HouseNo,
                row.Street,
                row.Subdivision,
                row.PurokName,
                row.MemberCount,
                row.UpdatedAt.HasValue ? row.UpdatedAt.Value.ToString("MMM dd, yyyy hh:mm tt") : "-");
        }

        _grid.DataSource = table;

        if (_grid.Columns.Contains("household_id"))
        {
            _grid.Columns["household_id"].Visible = false;
        }
        if (_grid.Columns.Contains("house_no"))
        {
            _grid.Columns["house_no"].HeaderText = "House No";
            _grid.Columns["house_no"].FillWeight = 95;
        }
        if (_grid.Columns.Contains("street"))
        {
            _grid.Columns["street"].HeaderText = "Street";
            _grid.Columns["street"].FillWeight = 160;
        }
        if (_grid.Columns.Contains("subdivision"))
        {
            _grid.Columns["subdivision"].HeaderText = "Subdivision";
            _grid.Columns["subdivision"].FillWeight = 130;
        }
        if (_grid.Columns.Contains("purok"))
        {
            _grid.Columns["purok"].HeaderText = "Purok";
            _grid.Columns["purok"].FillWeight = 105;
        }
        if (_grid.Columns.Contains("members"))
        {
            _grid.Columns["members"].HeaderText = "Members";
            _grid.Columns["members"].FillWeight = 75;
        }
        if (_grid.Columns.Contains("updated_at"))
        {
            _grid.Columns["updated_at"].HeaderText = "Updated At";
            _grid.Columns["updated_at"].FillWeight = 130;
        }

        EnsureActionColumn("action_view", "View", 72);
        EnsureActionColumn("action_edit", "Edit", 72);
        EnsureActionColumn("action_delete", "Delete", 74);
    }

    private void EnsureActionColumn(string name, string text, int width)
    {
        if (_grid.Columns.Contains(name))
        {
            return;
        }

        var buttonColumn = new DataGridViewButtonColumn
        {
            Name = name,
            HeaderText = text,
            Text = text,
            UseColumnTextForButtonValue = true,
            FillWeight = width,
            MinimumWidth = width
        };
        _grid.Columns.Add(buttonColumn);
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

    private void UpdateStatePanel(int rowCount)
    {
        bool hasSearch = !string.IsNullOrWhiteSpace(_searchBox.Text)
            || GetSelectedPurokId().HasValue
            || _withSeniorsFilter.Checked
            || _withPwdFilter.Checked
            || _with4PsFilter.Checked
            || _emptyFilter.Checked
            || _activeCasesFilter.Checked;

        bool show = rowCount <= 0;
        _statePanel.Visible = show;
        if (!show)
        {
            return;
        }

        _stateLabel.Text = hasSearch
            ? "No households matched your search and filters."
            : "No household records found yet.";

        _stateCreateButton.Visible = Permissions.CanCreateHouseholds;
    }

    private int? GetSelectedPurokId()
    {
        if (_purokFilter.SelectedValue is int id)
        {
            return id <= 0 ? (int?)null : id;
        }

        if (_purokFilter.SelectedItem is LookupItem lookup)
        {
            return lookup.Id <= 0 ? (int?)null : lookup.Id;
        }

        return null;
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

    private void NewButton_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanCreateHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to create households.");
            return;
        }

        using var editForm = new HouseholdEditForm();
        if (editForm.ShowDialog(this) == DialogResult.OK)
        {
            RefreshList(resetPage: true);
        }
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanViewHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to export households.");
            return;
        }

        try
        {
            var rows = new List<HouseholdListItem>();
            int page = 1;
            int totalPages = 1;

            while (page <= totalPages)
            {
                HouseholdPageResult result = _householdRepository.Search(new HouseholdListFilters
                {
                    BarangayId = _barangayId,
                    SearchText = _searchBox.Text,
                    PurokId = GetSelectedPurokId(),
                    WithSeniors = _withSeniorsFilter.Checked,
                    WithPwd = _withPwdFilter.Checked,
                    With4Ps = _with4PsFilter.Checked,
                    EmptyHouseholdOnly = _emptyFilter.Checked,
                    HasActiveCasesOnly = _activeCasesFilter.Checked,
                    PageNumber = page,
                    PageSize = 100
                });

                rows.AddRange(result.Items);
                totalPages = result.TotalPages <= 0 ? 1 : result.TotalPages;
                page++;
            }

            if (rows.Count == 0)
            {
                ControllerDialogs.Warning("Nothing to export.", "Export Households");
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Export Households",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"households_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("House No,Street,Subdivision,Purok,Members,Seniors,PWD,4Ps,Voters,Active Cases,Updated At");
            foreach (HouseholdListItem row in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    EscapeCsv(row.HouseNo),
                    EscapeCsv(row.Street),
                    EscapeCsv(row.Subdivision),
                    EscapeCsv(row.PurokName),
                    row.MemberCount.ToString(),
                    row.SeniorCount.ToString(),
                    row.PwdCount.ToString(),
                    row.FourPsCount.ToString(),
                    row.VoterCount.ToString(),
                    row.ActiveCaseCount.ToString(),
                    EscapeCsv(row.UpdatedAt.HasValue ? row.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty)
                }));
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            ControllerDialogs.Info("Household export completed.", "Export Households");
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to export households.", "Export Households");
        }
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (!_grid.Columns.Contains("household_id"))
        {
            return;
        }

        object? idValue = _grid.Rows[e.RowIndex].Cells["household_id"]?.Value;
        if (idValue == null || idValue == DBNull.Value)
        {
            return;
        }

        int householdId = Convert.ToInt32(idValue);
        string action = _grid.Columns[e.ColumnIndex].Name;

        if (action == "action_view")
        {
            OpenHouseholdDetails(householdId);
            return;
        }

        if (action == "action_edit")
        {
            if (!Permissions.CanEditHouseholds)
            {
                ControllerDialogs.Warning("You do not have permission to edit households.");
                return;
            }

            using var editForm = new HouseholdEditForm(householdId);
            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                RefreshList(resetPage: false);
            }

            return;
        }

        if (action == "action_delete")
        {
            if (!Permissions.CanDeleteHouseholds)
            {
                ControllerDialogs.Warning("You do not have permission to delete households.");
                return;
            }

            DialogResult confirm = ControllerDialogs.Confirm(
                "Delete this household? This action is only allowed when no members are assigned.",
                "Delete Household");
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                if (_householdRepository.TryDelete(householdId, _barangayId, out string message))
                {
                    RefreshList(resetPage: false);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    ControllerDialogs.Warning(message, "Delete Household");
                }
            }
            catch (Exception ex)
            {
                ControllerDialogs.Error(ex, "Unable to delete household.", "Delete Household");
            }
        }
    }

    private void OpenHouseholdDetails(int householdId)
    {
        using var detailsForm = new HouseholdDetailsForm(householdId);
        detailsForm.ShowDialog(this);
        if (detailsForm.HasChanges)
        {
            RefreshList(resetPage: false);
        }
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
