using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class HouseholdDetailsForm : Form
{
    private readonly int _householdId;
    private readonly int _barangayId;
    private readonly HouseholdRepository _householdRepository;
    private readonly ResidentHouseholdService _residentHouseholdService;

    private readonly Label _titleLabel = new Label();
    private readonly Label _subtitleLabel = new Label();
    private readonly Button _editButton = new Button();
    private readonly Button _refreshButton = new Button();
    private readonly Button _closeButton = new Button();
    private readonly TabControl _tabs = new TabControl();

    private readonly Label _addressValue = new Label();
    private readonly Label _purokValue = new Label();
    private readonly Label _coordsValue = new Label();
    private readonly Label _updatedValue = new Label();
    private readonly Label _totalValue = new Label();
    private readonly Label _seniorValue = new Label();
    private readonly Label _pwdValue = new Label();
    private readonly Label _fourPsValue = new Label();
    private readonly Label _voterValue = new Label();

    private readonly Button _addExistingButton = new Button();
    private readonly Button _registerButton = new Button();
    private readonly DataGridView _membersGrid = new DataGridView();
    private readonly Label _membersHint = new Label();

    private readonly DataGridView _historyGrid = new DataGridView();
    private readonly Label _attachmentsHint = new Label();

    private HouseholdDetailsDto? _details;

    public bool HasChanges { get; private set; }

    public HouseholdDetailsForm(int householdId)
        : this(new HouseholdRepository(), new ResidentHouseholdService(), householdId)
    {
    }

    internal HouseholdDetailsForm(
        HouseholdRepository householdRepository,
        ResidentHouseholdService residentHouseholdService,
        int householdId)
    {
        _householdRepository = householdRepository ?? throw new ArgumentNullException(nameof(householdRepository));
        _residentHouseholdService = residentHouseholdService ?? throw new ArgumentNullException(nameof(residentHouseholdService));
        _householdId = householdId;
        _barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);

        InitializeComponent();
        RefreshAll();
    }

    private void InitializeComponent()
    {
        Text = "Household Details";
        Name = "HouseholdDetailsForm";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        MinimumSize = new Size(980, 620);
        Size = new Size(1100, 720);
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = UiTheme.Slate50
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(root);

        var headerCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(14)
        };
        UiTheme.StyleSectionCard(headerCard, Color.White, enforceBorder: true, padding: new Padding(14));
        root.Controls.Add(headerCard, 0, 0);

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.White
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerCard.Controls.Add(headerLayout);

        _titleLabel.AutoSize = true;
        _titleLabel.Font = UiTheme.HeadingFont;
        _titleLabel.ForeColor = UiTheme.Slate900;
        _titleLabel.Text = "Household";

        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Font = UiTheme.LabelFont;
        _subtitleLabel.ForeColor = UiTheme.Slate600;
        _subtitleLabel.Text = "Loading details...";

        var headerActions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        _editButton.Text = "Edit Household";
        _refreshButton.Text = "Refresh";
        _closeButton.Text = "Close";
        _editButton.Click += EditButton_Click;
        _refreshButton.Click += (_, _) => RefreshAll();
        _closeButton.Click += (_, _) => Close();
        UiTheme.StyleSecondaryButton(_editButton);
        UiTheme.StyleSecondaryButton(_refreshButton);
        UiTheme.StylePrimaryButton(_closeButton);
        headerActions.Controls.Add(_closeButton);
        headerActions.Controls.Add(_refreshButton);
        headerActions.Controls.Add(_editButton);

        headerLayout.Controls.Add(_titleLabel, 0, 0);
        headerLayout.Controls.Add(headerActions, 1, 0);
        headerLayout.Controls.Add(_subtitleLabel, 0, 1);
        headerLayout.SetColumnSpan(_subtitleLabel, 2);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = UiTheme.BodyFont;
        root.Controls.Add(_tabs, 0, 1);

        BuildOverviewTab();
        BuildMembersTab();
        BuildTransferHistoryTab();
        BuildAttachmentsTab();

        UiTheme.SetTabOrder(_editButton, _refreshButton, _closeButton, _tabs);
        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
    }

    private void BuildOverviewTab()
    {
        var overview = new TabPage("Overview")
        {
            BackColor = UiTheme.Slate50
        };
        _tabs.TabPages.Add(overview);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = UiTheme.Slate50
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        overview.Controls.Add(root);

        var addressCard = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = Color.White,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 10)
        };
        UiTheme.StyleSectionCard(addressCard, Color.White, enforceBorder: true, padding: new Padding(14));
        root.Controls.Add(addressCard, 0, 0);

        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 4; i++)
        {
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        addressCard.Controls.Add(info);

        AddReadOnlyRow(info, 0, "Full Address", _addressValue);
        AddReadOnlyRow(info, 1, "Purok/Sitio", _purokValue);
        AddReadOnlyRow(info, 2, "Coordinates", _coordsValue);
        AddReadOnlyRow(info, 3, "Updated At", _updatedValue);

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(cards, 0, 1);

        cards.Controls.Add(CreateStatCard("Total Members", _totalValue));
        cards.Controls.Add(CreateStatCard("Seniors", _seniorValue));
        cards.Controls.Add(CreateStatCard("PWD", _pwdValue));
        cards.Controls.Add(CreateStatCard("4Ps", _fourPsValue));
        cards.Controls.Add(CreateStatCard("Voters", _voterValue));
    }

    private void BuildMembersTab()
    {
        var members = new TabPage("Members")
        {
            BackColor = UiTheme.Slate50
        };
        _tabs.TabPages.Add(members);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = UiTheme.Slate50
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        members.Controls.Add(root);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true
        };
        _addExistingButton.Text = "Add Existing Resident";
        _registerButton.Text = "Register New Resident";
        _addExistingButton.Click += AddExistingButton_Click;
        _registerButton.Click += RegisterButton_Click;
        UiTheme.StyleSecondaryButton(_addExistingButton);
        UiTheme.StylePrimaryButton(_registerButton);
        actions.Controls.Add(_addExistingButton);
        actions.Controls.Add(_registerButton);
        root.Controls.Add(actions, 0, 0);

        _membersHint.AutoSize = true;
        _membersHint.Font = UiTheme.LabelFont;
        _membersHint.ForeColor = UiTheme.Slate600;
        _membersHint.Text = "Actions: View Resident, Transfer Out, Remove from Household.";
        _membersHint.Margin = new Padding(0, 2, 0, 8);
        root.Controls.Add(_membersHint, 0, 1);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = Color.White
        };
        UiTheme.StyleGridContainer(gridCard);
        root.Controls.Add(gridCard, 0, 2);

        _membersGrid.Dock = DockStyle.Fill;
        _membersGrid.ReadOnly = true;
        _membersGrid.AllowUserToAddRows = false;
        _membersGrid.AllowUserToDeleteRows = false;
        _membersGrid.AllowUserToResizeRows = false;
        _membersGrid.MultiSelect = false;
        _membersGrid.RowHeadersVisible = false;
        _membersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _membersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _membersGrid.CellContentClick += MembersGrid_CellContentClick;
        UiTheme.StyleGrid(_membersGrid);
        gridCard.Controls.Add(_membersGrid);
    }

    private void BuildTransferHistoryTab()
    {
        var history = new TabPage("Transfer History")
        {
            BackColor = UiTheme.Slate50
        };
        _tabs.TabPages.Add(history);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = UiTheme.Slate50
        };
        history.Controls.Add(card);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = Color.White
        };
        UiTheme.StyleGridContainer(gridCard);
        card.Controls.Add(gridCard);

        _historyGrid.Dock = DockStyle.Fill;
        _historyGrid.ReadOnly = true;
        _historyGrid.AllowUserToAddRows = false;
        _historyGrid.AllowUserToDeleteRows = false;
        _historyGrid.AllowUserToResizeRows = false;
        _historyGrid.MultiSelect = false;
        _historyGrid.RowHeadersVisible = false;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        UiTheme.StyleGrid(_historyGrid);
        gridCard.Controls.Add(_historyGrid);
    }

    private void BuildAttachmentsTab()
    {
        var attachments = new TabPage("Attachments")
        {
            BackColor = UiTheme.Slate50
        };
        _tabs.TabPages.Add(attachments);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = UiTheme.Slate50
        };
        attachments.Controls.Add(card);

        _attachmentsHint.Dock = DockStyle.Top;
        _attachmentsHint.AutoSize = true;
        _attachmentsHint.MaximumSize = new Size(860, 0);
        _attachmentsHint.Font = UiTheme.LabelFont;
        _attachmentsHint.ForeColor = UiTheme.Slate600;
        _attachmentsHint.Text = "Attachments for household entities are optional and not enabled in the current attachment schema.";
        card.Controls.Add(_attachmentsHint);
    }

    private void RefreshAll()
    {
        try
        {
            _details = _householdRepository.GetDetails(_householdId, _barangayId);
            if (_details == null)
            {
                ControllerDialogs.Warning("Household record not found.", "Household");
                Close();
                return;
            }

            _titleLabel.Text = string.IsNullOrWhiteSpace(_details.FullAddress)
                ? $"Household #{_details.HouseholdId}"
                : _details.FullAddress;
            _subtitleLabel.Text = $"Purok: {_details.PurokName} | Active cases: {_details.ActiveCaseCount}";

            _addressValue.Text = string.IsNullOrWhiteSpace(_details.FullAddress) ? "-" : _details.FullAddress;
            _purokValue.Text = string.IsNullOrWhiteSpace(_details.PurokName) ? "-" : _details.PurokName;
            _coordsValue.Text = _details.Latitude.HasValue && _details.Longitude.HasValue
                ? $"{_details.Latitude:0.######}, {_details.Longitude:0.######}"
                : "-";
            _updatedValue.Text = _details.UpdatedAt.HasValue ? _details.UpdatedAt.Value.ToString("MMM dd, yyyy hh:mm tt") : "-";
            _totalValue.Text = _details.MemberCount.ToString();
            _seniorValue.Text = _details.SeniorCount.ToString();
            _pwdValue.Text = _details.PwdCount.ToString();
            _fourPsValue.Text = _details.FourPsCount.ToString();
            _voterValue.Text = _details.VoterCount.ToString();

            LoadMembersGrid();
            LoadTransferHistoryGrid();

            _editButton.Enabled = Permissions.CanEditHouseholds;
            _addExistingButton.Enabled = Permissions.CanTransferHouseholds;
            _registerButton.Enabled = Permissions.CanCreateResidents;
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load household details.", "Household");
        }
    }

    private void LoadMembersGrid()
    {
        var rows = _householdRepository.GetMembers(_householdId, _barangayId);
        var table = new DataTable();
        table.Columns.Add("resident_id", typeof(int));
        table.Columns.Add("photo", typeof(string));
        table.Columns.Add("full_name", typeof(string));
        table.Columns.Add("age", typeof(string));
        table.Columns.Add("sex", typeof(string));
        table.Columns.Add("civil_status", typeof(string));
        table.Columns.Add("contact_no", typeof(string));
        table.Columns.Add("status", typeof(string));

        foreach (HouseholdMemberRecord row in rows)
        {
            table.Rows.Add(
                row.ResidentId,
                row.HasPhoto ? "Yes" : "No",
                row.FullName,
                row.Age.HasValue ? row.Age.Value.ToString() : "-",
                row.Sex,
                row.CivilStatus,
                row.ContactNo,
                row.Status);
        }

        _membersGrid.DataSource = table;
        if (_membersGrid.Columns.Contains("resident_id"))
        {
            _membersGrid.Columns["resident_id"].Visible = false;
        }
        if (_membersGrid.Columns.Contains("full_name"))
        {
            _membersGrid.Columns["full_name"].HeaderText = "Full Name";
            _membersGrid.Columns["full_name"].FillWeight = 220;
        }
        if (_membersGrid.Columns.Contains("civil_status"))
        {
            _membersGrid.Columns["civil_status"].HeaderText = "Civil Status";
            _membersGrid.Columns["civil_status"].FillWeight = 90;
        }
        if (_membersGrid.Columns.Contains("contact_no"))
        {
            _membersGrid.Columns["contact_no"].HeaderText = "Contact";
            _membersGrid.Columns["contact_no"].FillWeight = 110;
        }

        EnsureMembersActionColumns();
    }

    private void EnsureMembersActionColumns()
    {
        AddMembersActionColumn("action_view", "View");
        AddMembersActionColumn("action_transfer", "Transfer");
        AddMembersActionColumn("action_remove", "Remove");
    }

    private void AddMembersActionColumn(string name, string text)
    {
        if (_membersGrid.Columns.Contains(name))
        {
            return;
        }

        var buttonColumn = new DataGridViewButtonColumn
        {
            Name = name,
            HeaderText = text,
            Text = text,
            UseColumnTextForButtonValue = true,
            FillWeight = 74,
            MinimumWidth = 72
        };
        _membersGrid.Columns.Add(buttonColumn);
    }

    private void LoadTransferHistoryGrid()
    {
        IReadOnlyList<HouseholdTransferHistoryItem> rows = _householdRepository.GetTransferHistory(_householdId, _barangayId);
        var table = new DataTable();
        table.Columns.Add("date", typeof(string));
        table.Columns.Add("resident_name", typeof(string));
        table.Columns.Add("old_address", typeof(string));
        table.Columns.Add("new_address", typeof(string));
        table.Columns.Add("reason", typeof(string));
        table.Columns.Add("transferred_by", typeof(string));

        foreach (HouseholdTransferHistoryItem row in rows)
        {
            table.Rows.Add(
                row.TransferredAt.HasValue ? row.TransferredAt.Value.ToString("MMM dd, yyyy hh:mm tt") : "-",
                row.ResidentName,
                row.OldAddress,
                row.NewAddress,
                row.Reason,
                row.TransferredBy);
        }

        _historyGrid.DataSource = table;
        if (_historyGrid.Columns.Contains("resident_name"))
        {
            _historyGrid.Columns["resident_name"].HeaderText = "Resident";
        }
        if (_historyGrid.Columns.Contains("old_address"))
        {
            _historyGrid.Columns["old_address"].HeaderText = "Old Address";
            _historyGrid.Columns["old_address"].FillWeight = 140;
        }
        if (_historyGrid.Columns.Contains("new_address"))
        {
            _historyGrid.Columns["new_address"].HeaderText = "New Address";
            _historyGrid.Columns["new_address"].FillWeight = 140;
        }
        if (_historyGrid.Columns.Contains("transferred_by"))
        {
            _historyGrid.Columns["transferred_by"].HeaderText = "Transferred By";
            _historyGrid.Columns["transferred_by"].FillWeight = 95;
        }
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanEditHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to edit households.");
            return;
        }

        using var editForm = new HouseholdEditForm(_householdId);
        if (editForm.ShowDialog(this) == DialogResult.OK)
        {
            HasChanges = true;
            RefreshAll();
        }
    }

    private void AddExistingButton_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanTransferHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to add existing residents.");
            return;
        }

        int? residentId = ShowResidentPickerDialog();
        if (!residentId.HasValue)
        {
            return;
        }

        try
        {
            _residentHouseholdService.AddExistingResidentToHousehold(
                residentId.Value,
                _householdId,
                _barangayId,
                "Added as existing resident.");
            HasChanges = true;
            RefreshAll();
            ControllerDialogs.Info("Resident added to household.", "Household");
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to add resident to household.", "Household");
        }
    }

    private void RegisterButton_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanCreateResidents)
        {
            ControllerDialogs.Warning("You do not have permission to register residents.");
            return;
        }

        var preset = new ResidentDto
        {
            BarangayId = _barangayId,
            PurokId = _details?.PurokId,
            HouseholdId = _householdId,
            Status = "Active",
            Gender = "M",
            CivilStatus = "Single",
            DateOfBirth = DateTime.Today.AddYears(-18)
        };

        using var residentForm = new ResidentForm("Register Resident", preset);
        if (residentForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _residentHouseholdService.RegisterResident(residentForm.Resident);
            HasChanges = true;
            RefreshAll();
            ControllerDialogs.Info("Resident registered and assigned to household.", "Household");
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to register resident.", "Household");
        }
    }

    private void MembersGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        int residentId = GetSelectedResidentId(e.RowIndex);
        if (residentId <= 0)
        {
            return;
        }

        string action = _membersGrid.Columns[e.ColumnIndex].Name;
        if (action == "action_view")
        {
            using var details = new ResidentDetailsModal(residentId, readOnly: true, initialTabIndex: 0);
            details.ShowDialog(this);
            return;
        }

        if (action == "action_transfer")
        {
            if (!Permissions.CanTransferHouseholds)
            {
                ControllerDialogs.Warning("You do not have permission to transfer residents.");
                return;
            }

            string residentName = Convert.ToString(_membersGrid.Rows[e.RowIndex].Cells["full_name"]?.Value) ?? $"Resident #{residentId}";
            using var transfer = new TransferResidentDialog(residentId, residentName, _householdId);
            if (transfer.ShowDialog(this) == DialogResult.OK)
            {
                HasChanges = true;
                RefreshAll();
                ControllerDialogs.Info("Resident transferred successfully.", "Household");
            }
            return;
        }

        if (action == "action_remove")
        {
            if (!Permissions.CanTransferHouseholds)
            {
                ControllerDialogs.Warning("You do not have permission to remove household members.");
                return;
            }

            string residentName = Convert.ToString(_membersGrid.Rows[e.RowIndex].Cells["full_name"]?.Value) ?? $"Resident #{residentId}";
            DialogResult confirm = ControllerDialogs.Confirm(
                $"Remove {residentName} from this household?",
                "Confirm Remove");
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _residentHouseholdService.RemoveResidentFromHousehold(
                    residentId,
                    _barangayId,
                    "Removed from household member list.");
                HasChanges = true;
                RefreshAll();
                ControllerDialogs.Info("Resident removed from household.", "Household");
            }
            catch (Exception ex)
            {
                ControllerDialogs.Error(ex, "Unable to remove resident.", "Household");
            }
        }
    }

    private int GetSelectedResidentId(int rowIndex)
    {
        if (!_membersGrid.Columns.Contains("resident_id") || rowIndex < 0 || rowIndex >= _membersGrid.Rows.Count)
        {
            return 0;
        }

        object? value = _membersGrid.Rows[rowIndex].Cells["resident_id"]?.Value;
        return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }

    private int? ShowResidentPickerDialog()
    {
        using var dialog = new Form
        {
            Text = "Select Existing Resident",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Width = 820,
            Height = 540,
            BackColor = UiTheme.Slate50,
            Font = UiTheme.BodyFont
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        dialog.Controls.Add(root);

        var searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "Search resident name or contact..."
        };
        UiTheme.StyleTextBox(searchBox);
        root.Controls.Add(searchBox, 0, 0);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = Color.White
        };
        UiTheme.StyleGridContainer(gridCard);
        root.Controls.Add(gridCard, 0, 1);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UiTheme.StyleGrid(grid);
        gridCard.Controls.Add(grid);

        var addButton = new Button { Text = "Add Selected" };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        UiTheme.StylePrimaryButton(addButton);
        UiTheme.StyleSecondaryButton(cancelButton);
        addButton.Enabled = false;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true
        };
        actions.Controls.Add(addButton);
        actions.Controls.Add(cancelButton);
        root.Controls.Add(actions, 0, 2);

        int? selectedResidentId = null;

        void LoadRows(string search)
        {
            IReadOnlyList<ResidentPickerItem> rows = _householdRepository.GetResidentsForHouseholdPicker(_barangayId, _householdId, search);
            var table = new DataTable();
            table.Columns.Add("resident_id", typeof(int));
            table.Columns.Add("full_name", typeof(string));
            table.Columns.Add("contact_no", typeof(string));
            table.Columns.Add("current_address", typeof(string));

            foreach (ResidentPickerItem row in rows)
            {
                table.Rows.Add(row.ResidentId, row.FullName, row.ContactNo, row.CurrentAddress);
            }

            grid.DataSource = table;
            if (grid.Columns.Contains("resident_id"))
            {
                grid.Columns["resident_id"].Visible = false;
            }
            if (grid.Columns.Contains("full_name"))
            {
                grid.Columns["full_name"].HeaderText = "Full Name";
                grid.Columns["full_name"].FillWeight = 160;
            }
            if (grid.Columns.Contains("contact_no"))
            {
                grid.Columns["contact_no"].HeaderText = "Contact";
                grid.Columns["contact_no"].FillWeight = 90;
            }
            if (grid.Columns.Contains("current_address"))
            {
                grid.Columns["current_address"].HeaderText = "Current Address";
                grid.Columns["current_address"].FillWeight = 200;
            }

            addButton.Enabled = grid.Rows.Count > 0;
            if (grid.Rows.Count > 0)
            {
                grid.Rows[0].Selected = true;
            }
        }

        searchBox.TextChanged += (_, _) => LoadRows(searchBox.Text);
        grid.SelectionChanged += (_, _) =>
        {
            if (!grid.Columns.Contains("resident_id") || grid.CurrentRow == null)
            {
                selectedResidentId = null;
                return;
            }

            object? value = grid.CurrentRow.Cells["resident_id"]?.Value;
            selectedResidentId = value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
        };

        addButton.Click += (_, _) =>
        {
            if (!selectedResidentId.HasValue || selectedResidentId.Value <= 0)
            {
                return;
            }

            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        LoadRows(string.Empty);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            return selectedResidentId;
        }

        return null;
    }

    private static void AddReadOnlyRow(TableLayoutPanel table, int row, string labelText, Label valueLabel)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate700,
            Margin = new Padding(0, 5, 8, 0)
        };

        valueLabel.AutoSize = true;
        valueLabel.Font = UiTheme.BodyFont;
        valueLabel.ForeColor = UiTheme.Slate900;
        valueLabel.Margin = new Padding(0, 5, 0, 0);
        valueLabel.Text = "-";

        table.Controls.Add(label, 0, row);
        table.Controls.Add(valueLabel, 1, row);
    }

    private static Panel CreateStatCard(string title, Label valueLabel)
    {
        var card = new Panel
        {
            Width = 165,
            Height = 78,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 8, 8),
            BackColor = Color.White
        };
        UiTheme.StyleSectionCard(card, Color.White, enforceBorder: true, padding: new Padding(10));

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 24,
            ForeColor = UiTheme.Slate600,
            Font = UiTheme.LabelFont
        };

        valueLabel.Text = "0";
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.ForeColor = UiTheme.Slate900;
        valueLabel.Font = new Font(UiTheme.HeadingFont.FontFamily, 15F, FontStyle.Bold);

        card.Controls.Add(valueLabel);
        card.Controls.Add(titleLabel);
        return card;
    }
}
