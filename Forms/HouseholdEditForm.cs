using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class HouseholdEditForm : Form
{
    private readonly HouseholdRepository _householdRepository;
    private readonly int? _householdId;
    private readonly int _barangayId;

    private readonly ComboBox _purokCombo = new ComboBox();
    private readonly TextBox _houseNoBox = new TextBox();
    private readonly TextBox _streetBox = new TextBox();
    private readonly TextBox _subdivisionBox = new TextBox();
    private readonly TextBox _addressNoteBox = new TextBox();
    private readonly TextBox _latitudeBox = new TextBox();
    private readonly TextBox _longitudeBox = new TextBox();
    private readonly Button _saveButton = new Button();
    private readonly Button _cancelButton = new Button();

    public int SavedHouseholdId { get; private set; }

    public HouseholdEditForm(int? householdId = null)
        : this(new HouseholdRepository(), householdId)
    {
    }

    internal HouseholdEditForm(HouseholdRepository householdRepository, int? householdId = null)
    {
        _householdRepository = householdRepository ?? throw new ArgumentNullException(nameof(householdRepository));
        _householdId = householdId;
        _barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);

        InitializeComponent();
        LoadPurokOptions();
        if (_householdId.HasValue)
        {
            LoadRecordForEdit(_householdId.Value);
        }
    }

    private void InitializeComponent()
    {
        Text = _householdId.HasValue ? "Edit Household" : "New Household";
        Name = "HouseholdEditForm";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;
        ClientSize = new Size(640, 470);
        MinimumSize = new Size(640, 470);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16),
            BackColor = UiTheme.Slate50
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        UiTheme.StyleSectionCard(card, Color.White, enforceBorder: true, padding: new Padding(16));
        root.Controls.Add(card, 0, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            BackColor = Color.White
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 8; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        }
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        card.Controls.Add(grid);

        var header = new Label
        {
            Text = _householdId.HasValue ? "Update household information." : "Create a new household record.",
            AutoSize = true,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate600,
            Margin = new Padding(0, 0, 0, 8)
        };
        grid.Controls.Add(header, 0, 0);
        grid.SetColumnSpan(header, 2);

        AddField(grid, 1, "Purok/Sitio *", _purokCombo);
        AddField(grid, 2, "House No", _houseNoBox);
        AddField(grid, 3, "Street", _streetBox);
        AddField(grid, 4, "Subdivision", _subdivisionBox);
        AddField(grid, 5, "Address Note", _addressNoteBox);
        AddField(grid, 6, "Latitude", _latitudeBox);
        AddField(grid, 7, "Longitude", _longitudeBox);

        _purokCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        UiTheme.StyleComboBox(_purokCombo);
        UiTheme.StyleTextBoxes(_houseNoBox, _streetBox, _subdivisionBox, _addressNoteBox, _latitudeBox, _longitudeBox);

        _saveButton.Text = "Save";
        _cancelButton.Text = "Cancel";
        _saveButton.Click += SaveButton_Click;
        _cancelButton.Click += (_, _) => Close();
        UiTheme.StylePrimaryButton(_saveButton);
        UiTheme.StyleSecondaryButton(_cancelButton);

        if (_householdId.HasValue && !Permissions.CanEditHouseholds)
        {
            _saveButton.Enabled = false;
            _saveButton.Text = "No Permission";
        }
        else if (!_householdId.HasValue && !Permissions.CanCreateHouseholds)
        {
            _saveButton.Enabled = false;
            _saveButton.Text = "No Permission";
        }

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        actions.Controls.Add(_saveButton);
        actions.Controls.Add(_cancelButton);
        root.Controls.Add(actions, 0, 1);

        UiTheme.SetTabOrder(_purokCombo, _houseNoBox, _streetBox, _subdivisionBox, _addressNoteBox, _latitudeBox, _longitudeBox, _saveButton, _cancelButton);
        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
    }

    private static void AddField(TableLayoutPanel host, int row, string labelText, Control editor)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate700,
            Margin = new Padding(0, 8, 6, 0)
        };
        editor.Dock = DockStyle.Fill;
        editor.Margin = new Padding(0, 4, 0, 4);

        host.Controls.Add(label, 0, row);
        host.Controls.Add(editor, 1, row);
    }

    private void LoadPurokOptions()
    {
        try
        {
            var options = _householdRepository.GetPurokOptions(_barangayId);
            _purokCombo.DataSource = null;
            _purokCombo.DisplayMember = nameof(LookupItem.Name);
            _purokCombo.ValueMember = nameof(LookupItem.Id);
            _purokCombo.DataSource = options;
            _purokCombo.SelectedIndex = _purokCombo.Items.Count > 0 ? 0 : -1;
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load purok list.", "Household");
        }
    }

    private void LoadRecordForEdit(int householdId)
    {
        try
        {
            HouseholdEditRecord? record = _householdRepository.GetForEdit(householdId, _barangayId);
            if (record == null)
            {
                ControllerDialogs.Warning("Household record not found.", "Household");
                Close();
                return;
            }

            _purokCombo.SelectedValue = record.PurokId;
            _houseNoBox.Text = record.HouseNo;
            _streetBox.Text = record.Street;
            _subdivisionBox.Text = record.Subdivision;
            _addressNoteBox.Text = record.AddressNote;
            _latitudeBox.Text = record.Latitude.HasValue ? record.Latitude.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
            _longitudeBox.Text = record.Longitude.HasValue ? record.Longitude.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load household record.", "Household");
            Close();
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_householdId.HasValue && !Permissions.CanEditHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to edit households.");
            return;
        }

        if (!_householdId.HasValue && !Permissions.CanCreateHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to create households.");
            return;
        }

        int? purokId = GetSelectedLookupId(_purokCombo);
        if (!purokId.HasValue || purokId.Value <= 0)
        {
            ControllerDialogs.Warning("Purok/Sitio is required.", "Validation");
            return;
        }

        decimal? latitude = ParseDecimal(_latitudeBox.Text, "Latitude", -90m, 90m);
        if (latitude == decimal.MinValue)
        {
            return;
        }

        decimal? longitude = ParseDecimal(_longitudeBox.Text, "Longitude", -180m, 180m);
        if (longitude == decimal.MinValue)
        {
            return;
        }

        if (_householdRepository.ExistsDuplicateAddress(
                _barangayId,
                purokId.Value,
                _houseNoBox.Text,
                _streetBox.Text,
                _householdId))
        {
            ControllerDialogs.Warning(
                "A household with the same Purok, House No, and Street already exists.",
                "Duplicate Household");
            return;
        }

        var request = new HouseholdSaveRequest
        {
            BarangayId = _barangayId,
            PurokId = purokId.Value,
            HouseNo = _houseNoBox.Text.Trim(),
            Street = _streetBox.Text.Trim(),
            Subdivision = _subdivisionBox.Text.Trim(),
            AddressNote = _addressNoteBox.Text.Trim(),
            Latitude = latitude,
            Longitude = longitude
        };

        try
        {
            if (_householdId.HasValue)
            {
                _householdRepository.Update(_householdId.Value, request);
                SavedHouseholdId = _householdId.Value;
            }
            else
            {
                SavedHouseholdId = _householdRepository.Create(request);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to save household record.", "Household");
        }
    }

    private static decimal? ParseDecimal(string? raw, string label, decimal min, decimal max)
    {
        string text = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) &&
            !decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            ControllerDialogs.Warning($"{label} must be a valid number.", "Validation");
            return decimal.MinValue;
        }

        if (value < min || value > max)
        {
            ControllerDialogs.Warning($"{label} must be between {min} and {max}.", "Validation");
            return decimal.MinValue;
        }

        return value;
    }

    private static int? GetSelectedLookupId(ComboBox combo)
    {
        if (combo.SelectedValue is int id)
        {
            return id <= 0 ? (int?)null : id;
        }

        if (combo.SelectedItem is LookupItem lookup)
        {
            return lookup.Id <= 0 ? (int?)null : lookup.Id;
        }

        return null;
    }
}
