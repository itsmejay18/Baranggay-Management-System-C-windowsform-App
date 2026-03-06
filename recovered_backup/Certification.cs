using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public class Certification : Form
{
	private readonly CertificateDialogMode _mode;

	private readonly string _residentName;

	private CertificateEntry _entry;

	private IContainer components = null;

	private Panel headerPanel;

	private Panel formPanel;

	private TableLayoutPanel formTable;

	private FlowLayoutPanel footerPanel;

	private Label _title;

	private Label _note;

	private ComboBox _type;

	private TextBox _purpose;

	private NumericUpDown _fee;

	private TextBox _orNumber;

	private DateTimePicker _issuedDate;

	private TextBox _businessName;

	private TextBox _businessNature;

	private TextBox _remarks;

	private Button _save;

	private Button _cancel;

	private Label _lblBusinessName;

	private Label _lblBusinessNature;

	private Panel _issueChecklistPanel;

	private FlowLayoutPanel _checklistStack;

	private Label _issueChecklistTitle;

	private Label _issueReqPurpose;

	private Label _issueReqOr;

	private Label _issueReqDate;

	private Label _issueReqBusiness;

	private Label lblType;

	private Label lblPurpose;

	private Label lblFee;

	private Label lblOr;

	private Label lblIssued;

	private Label lblRemarks;

	public CertificateEntry Entry => _entry;

	public Certification()
	{
		InitializeComponent();
		_mode = CertificateDialogMode.Request;
		_residentName = string.Empty;
		_entry = new CertificateEntry();
		ApplyTheme();
		ConfigureChecklistLabel(_issueReqPurpose, "Purpose");
		ConfigureChecklistLabel(_issueReqOr, "OR number");
		ConfigureChecklistLabel(_issueReqDate, "Issued date");
		ConfigureChecklistLabel(_issueReqBusiness, "Business details");
		PopulateEntry();
		ApplyMode();
	}

	public Certification(CertificateDialogMode mode, string residentName, CertificateEntry? existing = null)
	{
		InitializeComponent();
		_mode = mode;
		_residentName = residentName;
		_entry = existing ?? new CertificateEntry();
		ApplyTheme();
		ConfigureChecklistLabel(_issueReqPurpose, "Purpose");
		ConfigureChecklistLabel(_issueReqOr, "OR number");
		ConfigureChecklistLabel(_issueReqDate, "Issued date");
		ConfigureChecklistLabel(_issueReqBusiness, "Business details");
		PopulateEntry();
		ApplyMode();
	}

	private void ApplyTheme()
	{
		Text = "Certificate";
		BackColor = Color.White;
		Font = UiTheme.BodyFont;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		_title.Font = UiTheme.HeadingFont;
		_title.ForeColor = UiTheme.Slate900;
		_note.Font = UiTheme.LabelFont;
		_note.ForeColor = UiTheme.Slate500;
		UiTheme.StyleComboBox(_type);
		UiTheme.StyleTextBox(_purpose);
		UiTheme.StyleTextBox(_orNumber);
		UiTheme.StyleTextBox(_businessName);
		UiTheme.StyleTextBox(_businessNature);
		UiTheme.StyleTextBox(_remarks);
		_fee.Font = UiTheme.BodyFont;
		_fee.BorderStyle = BorderStyle.FixedSingle;
		_fee.TextAlign = HorizontalAlignment.Right;
		_issuedDate.Font = UiTheme.BodyFont;
		UiTheme.StylePrimaryButton(_save);
		UiTheme.StyleSecondaryButton(_cancel);
		_issueChecklistTitle.Font = UiTheme.LabelFont;
		_issueChecklistTitle.ForeColor = UiTheme.Slate500;
	}

	private void Type_SelectedIndexChanged(object? sender, EventArgs e)
	{
		UpdateBusinessFields();
		UpdateIssueChecklist();
	}

	private void IssueField_Changed(object? sender, EventArgs e)
	{
		UpdateIssueChecklist();
	}

	private void PopulateEntry()
	{
		if (!string.IsNullOrWhiteSpace(_entry.Type) && _type.Items.Contains(_entry.Type))
		{
			_type.SelectedItem = _entry.Type;
		}
		else if (_type.Items.Count > 0)
		{
			_type.SelectedIndex = 0;
		}
		_purpose.Text = _entry.Purpose ?? string.Empty;
		_fee.Value = _entry.Fee;
		_orNumber.Text = _entry.OrNumber ?? string.Empty;
		_issuedDate.Value = _entry.IssuedDate ?? DateTime.Today;
		_businessName.Text = _entry.BusinessName ?? string.Empty;
		_businessNature.Text = _entry.BusinessNature ?? string.Empty;
		_remarks.Text = _entry.Remarks ?? string.Empty;
		UpdateBusinessFields();
	}

	private void ApplyMode()
	{
		bool flag = _mode == CertificateDialogMode.Issue;
		bool flag2 = _mode == CertificateDialogMode.EditRequest;
		_save.Text = (flag ? "Issue Certificate" : (flag2 ? "Save Changes" : "Save Request"));
		_issuedDate.Enabled = flag;
		_orNumber.ReadOnly = !flag;
		_fee.Enabled = flag || _mode == CertificateDialogMode.Request || flag2;
		_remarks.ReadOnly = false;
		_type.Enabled = !flag;
		_purpose.ReadOnly = flag;
		_businessName.ReadOnly = flag;
		_businessNature.ReadOnly = flag;
		if (!flag)
		{
			_issuedDate.Enabled = false;
		}
		_note.Text = ((flag && !string.IsNullOrWhiteSpace(_residentName)) ? ("Resident: " + _residentName + " • Provide OR number and issued date.") : (string.IsNullOrWhiteSpace(_residentName) ? string.Empty : ("Resident: " + _residentName)));
		_issueChecklistPanel.Visible = flag;
		UpdateIssueChecklist();
	}

	private void UpdateBusinessFields()
	{
		string text = _type.SelectedItem?.ToString() ?? string.Empty;
		bool visible = text.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0;
		_lblBusinessName.Visible = visible;
		_lblBusinessNature.Visible = visible;
		_businessName.Visible = visible;
		_businessNature.Visible = visible;
		UpdateIssueChecklist();
	}

	private void ConfigureChecklistLabel(Label label, string text)
	{
		label.Text = "[ ] " + text;
		label.Font = UiTheme.LabelFont;
		label.ForeColor = UiTheme.Slate500;
		label.AutoSize = true;
		label.Margin = new Padding(0, 2, 0, 2);
	}

	private void UpdateIssueChecklist()
	{
		if (_mode != CertificateDialogMode.Issue)
		{
			_save.Enabled = true;
			return;
		}
		bool flag = !string.IsNullOrWhiteSpace(_purpose.Text);
		bool flag2 = !string.IsNullOrWhiteSpace(_orNumber.Text);
		bool flag3 = _issuedDate.Value.Date <= DateTime.Today;
		string text = _type.SelectedItem?.ToString() ?? string.Empty;
		bool flag4 = text.IndexOf("Business", StringComparison.OrdinalIgnoreCase) < 0 || (!string.IsNullOrWhiteSpace(_businessName.Text) && !string.IsNullOrWhiteSpace(_businessNature.Text));
		SetChecklistState(_issueReqPurpose, flag, "Purpose");
		SetChecklistState(_issueReqOr, flag2, "OR number");
		SetChecklistState(_issueReqDate, flag3, "Issued date");
		SetChecklistState(_issueReqBusiness, flag4, "Business details");
		_save.Enabled = flag && flag2 && flag3 && flag4;
	}

	private void SetChecklistState(Label label, bool ok, string text)
	{
		label.Text = (ok ? "[x]" : "[ ]") + " " + text;
		label.ForeColor = (ok ? Color.FromArgb(0, 100, 40) : UiTheme.Slate500);
	}

	private void Save_Click(object? sender, EventArgs e)
	{
		string text = _type.SelectedItem?.ToString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			MessageBox.Show("Certificate type is required.");
			return;
		}
		if (string.IsNullOrWhiteSpace(_purpose.Text))
		{
			MessageBox.Show("Purpose is required.");
			return;
		}
		if (text.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0 && (string.IsNullOrWhiteSpace(_businessName.Text) || string.IsNullOrWhiteSpace(_businessNature.Text)))
		{
			MessageBox.Show("Business name and nature are required for business clearance.");
			return;
		}
		if (_mode == CertificateDialogMode.Issue && string.IsNullOrWhiteSpace(_orNumber.Text))
		{
			MessageBox.Show("OR number is required.");
			return;
		}
		if (_mode == CertificateDialogMode.Issue && _issuedDate.Value.Date > DateTime.Today)
		{
			MessageBox.Show("Issued date cannot be in the future.");
			return;
		}
		_entry.Type = text;
		_entry.Purpose = _purpose.Text.Trim();
		_entry.Fee = _fee.Value;
		_entry.OrNumber = _orNumber.Text.Trim();
		_entry.IssuedDate = ((_mode == CertificateDialogMode.Issue) ? new DateTime?(_issuedDate.Value.Date) : ((DateTime?)null));
		_entry.BusinessName = _businessName.Text.Trim();
		_entry.BusinessNature = _businessNature.Text.Trim();
		_entry.Remarks = _remarks.Text.Trim();
		base.DialogResult = DialogResult.OK;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.headerPanel = new System.Windows.Forms.Panel();
		this._title = new System.Windows.Forms.Label();
		this._note = new System.Windows.Forms.Label();
		this.formPanel = new System.Windows.Forms.Panel();
		this.formTable = new System.Windows.Forms.TableLayoutPanel();
		this.lblType = new System.Windows.Forms.Label();
		this.lblPurpose = new System.Windows.Forms.Label();
		this.lblFee = new System.Windows.Forms.Label();
		this.lblOr = new System.Windows.Forms.Label();
		this.lblIssued = new System.Windows.Forms.Label();
		this._lblBusinessName = new System.Windows.Forms.Label();
		this._lblBusinessNature = new System.Windows.Forms.Label();
		this.lblRemarks = new System.Windows.Forms.Label();
		this._type = new System.Windows.Forms.ComboBox();
		this._purpose = new System.Windows.Forms.TextBox();
		this._fee = new System.Windows.Forms.NumericUpDown();
		this._orNumber = new System.Windows.Forms.TextBox();
		this._issuedDate = new System.Windows.Forms.DateTimePicker();
		this._businessName = new System.Windows.Forms.TextBox();
		this._businessNature = new System.Windows.Forms.TextBox();
		this._remarks = new System.Windows.Forms.TextBox();
		this._issueChecklistPanel = new System.Windows.Forms.Panel();
		this._checklistStack = new System.Windows.Forms.FlowLayoutPanel();
		this._issueChecklistTitle = new System.Windows.Forms.Label();
		this._issueReqPurpose = new System.Windows.Forms.Label();
		this._issueReqOr = new System.Windows.Forms.Label();
		this._issueReqDate = new System.Windows.Forms.Label();
		this._issueReqBusiness = new System.Windows.Forms.Label();
		this.footerPanel = new System.Windows.Forms.FlowLayoutPanel();
		this._save = new System.Windows.Forms.Button();
		this._cancel = new System.Windows.Forms.Button();
		this.headerPanel.SuspendLayout();
		this.formPanel.SuspendLayout();
		this.formTable.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this._fee).BeginInit();
		this._issueChecklistPanel.SuspendLayout();
		this._checklistStack.SuspendLayout();
		this.footerPanel.SuspendLayout();
		base.SuspendLayout();
		this.headerPanel.Controls.Add(this._title);
		this.headerPanel.Controls.Add(this._note);
		this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.headerPanel.Location = new System.Drawing.Point(0, 0);
		this.headerPanel.Name = "headerPanel";
		this.headerPanel.Padding = new System.Windows.Forms.Padding(24, 18, 24, 0);
		this.headerPanel.Size = new System.Drawing.Size(640, 70);
		this.headerPanel.TabIndex = 0;
		this._title.AutoSize = true;
		this._title.Location = new System.Drawing.Point(24, 18);
		this._title.Name = "_title";
		this._title.Size = new System.Drawing.Size(115, 15);
		this._title.TabIndex = 0;
		this._title.Text = "New Certificate Request";
		this._note.AutoSize = true;
		this._note.Location = new System.Drawing.Point(24, 50);
		this._note.Name = "_note";
		this._note.Size = new System.Drawing.Size(0, 15);
		this._note.TabIndex = 1;
		this.formPanel.AutoScroll = true;
		this.formPanel.Controls.Add(this._issueChecklistPanel);
		this.formPanel.Controls.Add(this.formTable);
		this.formPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.formPanel.Location = new System.Drawing.Point(0, 70);
		this.formPanel.Name = "formPanel";
		this.formPanel.Padding = new System.Windows.Forms.Padding(24, 8, 24, 8);
		this.formPanel.Size = new System.Drawing.Size(640, 430);
		this.formPanel.TabIndex = 1;
		this.formTable.AutoSize = true;
		this.formTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.formTable.ColumnCount = 2;
		this.formTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160f));
		this.formTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.formTable.Controls.Add(this.lblType, 0, 0);
		this.formTable.Controls.Add(this._type, 1, 0);
		this.formTable.Controls.Add(this.lblPurpose, 0, 1);
		this.formTable.Controls.Add(this._purpose, 1, 1);
		this.formTable.Controls.Add(this.lblFee, 0, 2);
		this.formTable.Controls.Add(this._fee, 1, 2);
		this.formTable.Controls.Add(this.lblOr, 0, 3);
		this.formTable.Controls.Add(this._orNumber, 1, 3);
		this.formTable.Controls.Add(this.lblIssued, 0, 4);
		this.formTable.Controls.Add(this._issuedDate, 1, 4);
		this.formTable.Controls.Add(this._lblBusinessName, 0, 5);
		this.formTable.Controls.Add(this._businessName, 1, 5);
		this.formTable.Controls.Add(this._lblBusinessNature, 0, 6);
		this.formTable.Controls.Add(this._businessNature, 1, 6);
		this.formTable.Controls.Add(this.lblRemarks, 0, 7);
		this.formTable.Controls.Add(this._remarks, 1, 7);
		this.formTable.Dock = System.Windows.Forms.DockStyle.Top;
		this.formTable.Location = new System.Drawing.Point(24, 8);
		this.formTable.Margin = new System.Windows.Forms.Padding(0);
		this.formTable.Name = "formTable";
		this.formTable.RowCount = 8;
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.Size = new System.Drawing.Size(592, 366);
		this.formTable.TabIndex = 0;
		this.lblType.AutoSize = true;
		this.lblType.Location = new System.Drawing.Point(0, 10);
		this.lblType.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this.lblType.Name = "lblType";
		this.lblType.Size = new System.Drawing.Size(31, 15);
		this.lblType.TabIndex = 0;
		this.lblType.Text = "Type";
		this.lblPurpose.AutoSize = true;
		this.lblPurpose.Location = new System.Drawing.Point(0, 54);
		this.lblPurpose.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this.lblPurpose.Name = "lblPurpose";
		this.lblPurpose.Size = new System.Drawing.Size(50, 15);
		this.lblPurpose.TabIndex = 2;
		this.lblPurpose.Text = "Purpose";
		this.lblFee.AutoSize = true;
		this.lblFee.Location = new System.Drawing.Point(0, 134);
		this.lblFee.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this.lblFee.Name = "lblFee";
		this.lblFee.Size = new System.Drawing.Size(27, 15);
		this.lblFee.TabIndex = 4;
		this.lblFee.Text = "Fee";
		this.lblOr.AutoSize = true;
		this.lblOr.Location = new System.Drawing.Point(0, 178);
		this.lblOr.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this.lblOr.Name = "lblOr";
		this.lblOr.Size = new System.Drawing.Size(63, 15);
		this.lblOr.TabIndex = 6;
		this.lblOr.Text = "OR number";
		this.lblIssued.AutoSize = true;
		this.lblIssued.Location = new System.Drawing.Point(0, 222);
		this.lblIssued.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this.lblIssued.Name = "lblIssued";
		this.lblIssued.Size = new System.Drawing.Size(66, 15);
		this.lblIssued.TabIndex = 8;
		this.lblIssued.Text = "Issued date";
		this._lblBusinessName.AutoSize = true;
		this._lblBusinessName.Location = new System.Drawing.Point(0, 266);
		this._lblBusinessName.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this._lblBusinessName.Name = "_lblBusinessName";
		this._lblBusinessName.Size = new System.Drawing.Size(86, 15);
		this._lblBusinessName.TabIndex = 10;
		this._lblBusinessName.Text = "Business name";
		this._lblBusinessNature.AutoSize = true;
		this._lblBusinessNature.Location = new System.Drawing.Point(0, 310);
		this._lblBusinessNature.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this._lblBusinessNature.Name = "_lblBusinessNature";
		this._lblBusinessNature.Size = new System.Drawing.Size(90, 15);
		this._lblBusinessNature.TabIndex = 12;
		this._lblBusinessNature.Text = "Business nature";
		this.lblRemarks.AutoSize = true;
		this.lblRemarks.Location = new System.Drawing.Point(0, 354);
		this.lblRemarks.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);
		this.lblRemarks.Name = "lblRemarks";
		this.lblRemarks.Size = new System.Drawing.Size(52, 15);
		this.lblRemarks.TabIndex = 14;
		this.lblRemarks.Text = "Remarks";
		this._type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._type.FormattingEnabled = true;
		this._type.Items.AddRange(new object[4] { "Barangay Clearance", "Certificate of Residency", "Indigency", "Business Clearance" });
		this._type.Location = new System.Drawing.Point(160, 6);
		this._type.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._type.Name = "_type";
		this._type.Size = new System.Drawing.Size(432, 23);
		this._type.TabIndex = 1;
		this._type.SelectedIndexChanged += new System.EventHandler(Type_SelectedIndexChanged);
		this._purpose.Location = new System.Drawing.Point(160, 48);
		this._purpose.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._purpose.Multiline = true;
		this._purpose.Name = "_purpose";
		this._purpose.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
		this._purpose.Size = new System.Drawing.Size(432, 70);
		this._purpose.TabIndex = 3;
		this._purpose.TextChanged += new System.EventHandler(IssueField_Changed);
		this._fee.DecimalPlaces = 2;
		this._fee.Increment = new decimal(new int[4] { 50, 0, 0, 0 });
		this._fee.Location = new System.Drawing.Point(160, 128);
		this._fee.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._fee.Maximum = new decimal(new int[4] { 1000000, 0, 0, 0 });
		this._fee.Name = "_fee";
		this._fee.Size = new System.Drawing.Size(150, 23);
		this._fee.TabIndex = 5;
		this._fee.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this._orNumber.Location = new System.Drawing.Point(160, 172);
		this._orNumber.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._orNumber.Name = "_orNumber";
		this._orNumber.Size = new System.Drawing.Size(432, 23);
		this._orNumber.TabIndex = 7;
		this._orNumber.TextChanged += new System.EventHandler(IssueField_Changed);
		this._issuedDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
		this._issuedDate.Location = new System.Drawing.Point(160, 216);
		this._issuedDate.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._issuedDate.Name = "_issuedDate";
		this._issuedDate.Size = new System.Drawing.Size(150, 23);
		this._issuedDate.TabIndex = 9;
		this._issuedDate.ValueChanged += new System.EventHandler(IssueField_Changed);
		this._businessName.Location = new System.Drawing.Point(160, 260);
		this._businessName.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._businessName.Name = "_businessName";
		this._businessName.Size = new System.Drawing.Size(432, 23);
		this._businessName.TabIndex = 11;
		this._businessName.TextChanged += new System.EventHandler(IssueField_Changed);
		this._businessNature.Location = new System.Drawing.Point(160, 304);
		this._businessNature.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._businessNature.Name = "_businessNature";
		this._businessNature.Size = new System.Drawing.Size(432, 23);
		this._businessNature.TabIndex = 13;
		this._businessNature.TextChanged += new System.EventHandler(IssueField_Changed);
		this._remarks.Location = new System.Drawing.Point(160, 348);
		this._remarks.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
		this._remarks.Multiline = true;
		this._remarks.Name = "_remarks";
		this._remarks.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
		this._remarks.Size = new System.Drawing.Size(432, 70);
		this._remarks.TabIndex = 15;
		this._issueChecklistPanel.AutoSize = true;
		this._issueChecklistPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this._issueChecklistPanel.Controls.Add(this._checklistStack);
		this._issueChecklistPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this._issueChecklistPanel.Location = new System.Drawing.Point(24, 374);
		this._issueChecklistPanel.Margin = new System.Windows.Forms.Padding(0);
		this._issueChecklistPanel.Name = "_issueChecklistPanel";
		this._issueChecklistPanel.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
		this._issueChecklistPanel.Size = new System.Drawing.Size(592, 102);
		this._issueChecklistPanel.TabIndex = 1;
		this._checklistStack.AutoSize = true;
		this._checklistStack.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this._checklistStack.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
		this._checklistStack.Location = new System.Drawing.Point(0, 12);
		this._checklistStack.Margin = new System.Windows.Forms.Padding(0);
		this._checklistStack.Name = "_checklistStack";
		this._checklistStack.Size = new System.Drawing.Size(112, 70);
		this._checklistStack.TabIndex = 0;
		this._checklistStack.WrapContents = false;
		this._checklistStack.Controls.Add(this._issueChecklistTitle);
		this._checklistStack.Controls.Add(this._issueReqPurpose);
		this._checklistStack.Controls.Add(this._issueReqOr);
		this._checklistStack.Controls.Add(this._issueReqDate);
		this._checklistStack.Controls.Add(this._issueReqBusiness);
		this._issueChecklistTitle.AutoSize = true;
		this._issueChecklistTitle.Location = new System.Drawing.Point(0, 0);
		this._issueChecklistTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
		this._issueChecklistTitle.Name = "_issueChecklistTitle";
		this._issueChecklistTitle.Size = new System.Drawing.Size(81, 15);
		this._issueChecklistTitle.TabIndex = 0;
		this._issueChecklistTitle.Text = "Issue checklist";
		this._issueReqPurpose.AutoSize = true;
		this._issueReqPurpose.Location = new System.Drawing.Point(0, 21);
		this._issueReqPurpose.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
		this._issueReqPurpose.Name = "_issueReqPurpose";
		this._issueReqPurpose.Size = new System.Drawing.Size(64, 15);
		this._issueReqPurpose.TabIndex = 1;
		this._issueReqPurpose.Text = "[ ] Purpose";
		this._issueReqOr.AutoSize = true;
		this._issueReqOr.Location = new System.Drawing.Point(0, 40);
		this._issueReqOr.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
		this._issueReqOr.Name = "_issueReqOr";
		this._issueReqOr.Size = new System.Drawing.Size(80, 15);
		this._issueReqOr.TabIndex = 2;
		this._issueReqOr.Text = "[ ] OR number";
		this._issueReqDate.AutoSize = true;
		this._issueReqDate.Location = new System.Drawing.Point(0, 59);
		this._issueReqDate.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
		this._issueReqDate.Name = "_issueReqDate";
		this._issueReqDate.Size = new System.Drawing.Size(79, 15);
		this._issueReqDate.TabIndex = 3;
		this._issueReqDate.Text = "[ ] Issued date";
		this._issueReqBusiness.AutoSize = true;
		this._issueReqBusiness.Location = new System.Drawing.Point(0, 78);
		this._issueReqBusiness.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
		this._issueReqBusiness.Name = "_issueReqBusiness";
		this._issueReqBusiness.Size = new System.Drawing.Size(112, 15);
		this._issueReqBusiness.TabIndex = 4;
		this._issueReqBusiness.Text = "[ ] Business details";
		this.footerPanel.AutoSize = true;
		this.footerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.footerPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		this.footerPanel.Location = new System.Drawing.Point(0, 500);
		this.footerPanel.Name = "footerPanel";
		this.footerPanel.Padding = new System.Windows.Forms.Padding(24, 8, 24, 16);
		this.footerPanel.Size = new System.Drawing.Size(640, 56);
		this.footerPanel.TabIndex = 2;
		this.footerPanel.WrapContents = false;
		this.footerPanel.Controls.Add(this._save);
		this.footerPanel.Controls.Add(this._cancel);
		this._save.AutoSize = true;
		this._save.Location = new System.Drawing.Point(541, 8);
		this._save.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
		this._save.Name = "_save";
		this._save.Size = new System.Drawing.Size(75, 32);
		this._save.TabIndex = 0;
		this._save.Text = "Save";
		this._save.UseVisualStyleBackColor = true;
		this._save.Click += new System.EventHandler(Save_Click);
		this._cancel.AutoSize = true;
		this._cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this._cancel.Location = new System.Drawing.Point(454, 8);
		this._cancel.Margin = new System.Windows.Forms.Padding(0);
		this._cancel.Name = "_cancel";
		this._cancel.Size = new System.Drawing.Size(75, 32);
		this._cancel.TabIndex = 1;
		this._cancel.Text = "Cancel";
		this._cancel.UseVisualStyleBackColor = true;
		base.AcceptButton = this._save;
		base.CancelButton = this._cancel;
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(640, 556);
		base.Controls.Add(this.formPanel);
		base.Controls.Add(this.footerPanel);
		base.Controls.Add(this.headerPanel);
		base.Name = "Certification";
		this.Text = "Certificate";
		this.headerPanel.ResumeLayout(false);
		this.headerPanel.PerformLayout();
		this.formPanel.ResumeLayout(false);
		this.formPanel.PerformLayout();
		this.formTable.ResumeLayout(false);
		this.formTable.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this._fee).EndInit();
		this._issueChecklistPanel.ResumeLayout(false);
		this._issueChecklistPanel.PerformLayout();
		this._checklistStack.ResumeLayout(false);
		this._checklistStack.PerformLayout();
		this.footerPanel.ResumeLayout(false);
		this.footerPanel.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
