using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class TransferResidentDialog : Form
{
    private readonly int _residentId;
    private readonly string _residentName;
    private readonly int? _currentHouseholdId;
    private readonly int _barangayId;

    private readonly HouseholdRepository _householdRepository;
    private readonly ResidentHouseholdService _residentHouseholdService;

    private readonly Label _titleLabel = new Label();
    private readonly Label _subtitleLabel = new Label();
    private readonly Label _residentLabel = new Label();
    private readonly ComboBox _purokCombo = new ComboBox();
    private readonly ComboBox _householdCombo = new ComboBox();
    private readonly TextBox _reasonBox = new TextBox();
    private readonly Button _transferButton = new Button();
    private readonly Button _cancelButton = new Button();
    private readonly ToolTip _toolTip = new ToolTip();

    private bool _loading;

    public bool TransferCompleted { get; private set; }

    public TransferResidentDialog(int residentId, string residentName, int? currentHouseholdId = null)
        : this(new HouseholdRepository(), new ResidentHouseholdService(), residentId, residentName, currentHouseholdId)
    {
    }

    internal TransferResidentDialog(
        HouseholdRepository householdRepository,
        ResidentHouseholdService residentHouseholdService,
        int residentId,
        string residentName,
        int? currentHouseholdId = null)
    {
        _householdRepository = householdRepository ?? throw new ArgumentNullException(nameof(householdRepository));
        _residentHouseholdService = residentHouseholdService ?? throw new ArgumentNullException(nameof(residentHouseholdService));
        _residentId = residentId;
        _residentName = string.IsNullOrWhiteSpace(residentName) ? $"Resident #{residentId}" : residentName.Trim();
        _currentHouseholdId = currentHouseholdId;
        _barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);

        InitializeComponent();
        LoadPurokOptions();
    }

    private void InitializeComponent()
    {
        Text = "Transfer Resident";
        Name = "TransferResidentDialog";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;
        ClientSize = new Size(560, 330);
        MinimumSize = new Size(560, 330);

        _titleLabel.Text = "Transfer Household Member";
        _titleLabel.Font = UiTheme.HeadingFont;
        _titleLabel.ForeColor = UiTheme.Slate900;
        _titleLabel.AutoSize = true;

        _subtitleLabel.Text = "Move resident to another household and purok.";
        _subtitleLabel.Font = UiTheme.LabelFont;
        _subtitleLabel.ForeColor = UiTheme.Slate600;
        _subtitleLabel.AutoSize = true;

        _residentLabel.Text = _residentName;
        _residentLabel.Font = new Font(UiTheme.BodyFont, FontStyle.Bold);
        _residentLabel.ForeColor = UiTheme.Slate900;
        _residentLabel.AutoSize = true;

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

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.White
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        card.Controls.Add(content);

        var residentCaption = CreateCaptionLabel("Resident");
        var purokCaption = CreateCaptionLabel("Target Purok/Sitio");
        var householdCaption = CreateCaptionLabel("Target Household");
        var reasonCaption = CreateCaptionLabel("Reason");

        _purokCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _householdCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        UiTheme.StyleComboBoxes(_purokCombo, _householdCombo);

        _reasonBox.Multiline = true;
        _reasonBox.ScrollBars = ScrollBars.Vertical;
        _reasonBox.MaxLength = 255;
        _reasonBox.Text = "Transferred by household update.";
        UiTheme.StyleTextBox(_reasonBox);

        _purokCombo.SelectedIndexChanged += PurokCombo_SelectedIndexChanged;

        content.Controls.Add(_titleLabel, 0, 0);
        content.SetColumnSpan(_titleLabel, 2);
        content.Controls.Add(_subtitleLabel, 0, 1);
        content.SetColumnSpan(_subtitleLabel, 2);

        content.Controls.Add(residentCaption, 0, 2);
        content.Controls.Add(_residentLabel, 1, 2);
        content.Controls.Add(purokCaption, 0, 3);
        content.Controls.Add(_purokCombo, 1, 3);
        content.Controls.Add(householdCaption, 0, 4);

        var householdPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
            BackColor = Color.White
        };
        householdPanel.Controls.Add(_householdCombo);
        _householdCombo.Dock = DockStyle.Top;
        _householdCombo.Height = 30;
        content.Controls.Add(householdPanel, 1, 4);

        content.Controls.Add(reasonCaption, 0, 5);
        content.Controls.Add(_reasonBox, 1, 5);

        _transferButton.Text = "Transfer";
        _cancelButton.Text = "Cancel";
        _transferButton.Click += TransferButton_Click;
        _cancelButton.Click += (_, _) => Close();
        UiTheme.StylePrimaryButton(_transferButton);
        UiTheme.StyleSecondaryButton(_cancelButton);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(0),
            Margin = new Padding(0, 10, 0, 0)
        };
        actions.Controls.Add(_transferButton);
        actions.Controls.Add(_cancelButton);
        root.Controls.Add(actions, 0, 1);

        _toolTip.SetToolTip(_transferButton, Permissions.CanTransferHouseholds
            ? "Transfer selected resident to target household."
            : "You do not have permission to transfer household members.");

        if (!Permissions.CanTransferHouseholds)
        {
            _transferButton.Enabled = false;
        }

        UiTheme.SetTabOrder(_purokCombo, _householdCombo, _reasonBox, _transferButton, _cancelButton);
        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
    }

    private void LoadPurokOptions()
    {
        _loading = true;
        try
        {
            IReadOnlyList<LookupItem> puroks = _householdRepository.GetPurokOptions(_barangayId);
            _purokCombo.DataSource = null;
            _purokCombo.DisplayMember = nameof(LookupItem.Name);
            _purokCombo.ValueMember = nameof(LookupItem.Id);
            _purokCombo.DataSource = new List<LookupItem>(puroks);
            if (_purokCombo.Items.Count > 0)
            {
                _purokCombo.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load purok list.", "Transfer Resident");
        }
        finally
        {
            _loading = false;
            LoadHouseholdOptions();
        }
    }

    private void PurokCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loading)
        {
            return;
        }

        LoadHouseholdOptions();
    }

    private void LoadHouseholdOptions()
    {
        try
        {
            int? purokId = GetSelectedLookupId(_purokCombo);
            IReadOnlyList<LookupItem> households = _householdRepository.GetHouseholdsForPurok(_barangayId, purokId, _currentHouseholdId);

            _householdCombo.DataSource = null;
            _householdCombo.DisplayMember = nameof(LookupItem.Name);
            _householdCombo.ValueMember = nameof(LookupItem.Id);
            _householdCombo.DataSource = new List<LookupItem>(households);
            _householdCombo.SelectedIndex = _householdCombo.Items.Count > 0 ? 0 : -1;
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to load households.", "Transfer Resident");
        }
    }

    private void TransferButton_Click(object? sender, EventArgs e)
    {
        if (!Permissions.CanTransferHouseholds)
        {
            ControllerDialogs.Warning("You do not have permission to transfer household members.");
            return;
        }

        int? targetHouseholdId = GetSelectedLookupId(_householdCombo);
        if (!targetHouseholdId.HasValue || targetHouseholdId.Value <= 0)
        {
            ControllerDialogs.Warning("Please select a target household.", "Transfer Resident");
            return;
        }

        string reason = (_reasonBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            ControllerDialogs.Warning("Please provide a transfer reason.", "Transfer Resident");
            return;
        }

        DialogResult confirm = ControllerDialogs.Confirm(
            $"Transfer {_residentName} to the selected household?",
            "Confirm Transfer");
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _residentHouseholdService.TransferResident(_residentId, _currentHouseholdId ?? 0, targetHouseholdId.Value, reason);
            TransferCompleted = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to transfer resident.", "Transfer Resident");
        }
    }

    private static Label CreateCaptionLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = UiTheme.Slate700,
            Font = UiTheme.LabelFont,
            Margin = new Padding(0, 8, 8, 0)
        };
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
