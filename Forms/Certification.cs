using System;

using System.ComponentModel;

using System.Drawing;

using System.Windows.Forms;



namespace baranggaysystem1;



public partial class Certification : Form

{

	private readonly CertificateDialogMode _mode;



	private readonly string _residentName;

	private CertificateEntry _entry;
	private readonly CertificationController _controller;


	public CertificateEntry Entry => _entry;



	public Certification()
	{
		InitializeComponent();
		_controller = new CertificationController(this);
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
		_controller = new CertificationController(this);
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

		UiTheme.ApplyLabelFont(UiTheme.LabelFont, _note, _issueChecklistTitle);

		_title.ForeColor = UiTheme.Slate900;

		_note.ForeColor = UiTheme.Slate500;

		UiTheme.StyleComboBoxes(_type);

		UiTheme.StyleComboBoxes(_paymentMethod);

		UiTheme.StyleTextBoxes(_purpose, _orNumber, _businessName, _businessNature, _remarks);

		_fee.Font = UiTheme.BodyFont;

		_fee.BorderStyle = BorderStyle.FixedSingle;

		_fee.TextAlign = HorizontalAlignment.Right;

		_issuedDate.Font = UiTheme.BodyFont;

		UiTheme.StylePrimaryButtons(_save);

		UiTheme.StyleSecondaryButtons(_cancel);

		_issueChecklistTitle.ForeColor = UiTheme.Slate500;
		UiTheme.StandardizeButtonLayout(this);

	}



	private void Type_SelectedIndexChanged(object? sender, EventArgs e)
	{
		_controller.HandleTypeChanged();
	}


	private void IssueField_Changed(object? sender, EventArgs e)
	{
		_controller.HandleIssueFieldChanged();
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

		if (!string.IsNullOrWhiteSpace(_entry.PaymentMethod) && _paymentMethod.Items.Contains(_entry.PaymentMethod))
		{
			_paymentMethod.SelectedItem = _entry.PaymentMethod;
		}
		else if (_paymentMethod.Items.Count > 0)
		{
			_paymentMethod.SelectedIndex = 0;
		}

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
		_paymentMethod.Enabled = flag;

		_fee.Enabled = flag || _mode == CertificateDialogMode.Request || flag2;

		_remarks.ReadOnly = false;

		_type.Enabled = !flag;

		_purpose.ReadOnly = flag;

		_businessName.ReadOnly = flag;

		_businessNature.ReadOnly = flag;

		if (!flag)

		{

			_issuedDate.Enabled = false;
			_paymentMethod.SelectedIndex = _paymentMethod.Items.Count > 0 ? 0 : -1;

		}

		_note.Text = ((flag && !string.IsNullOrWhiteSpace(_residentName)) ? ("Resident: " + _residentName + " � Provide OR number and issued date.") : (string.IsNullOrWhiteSpace(_residentName) ? string.Empty : ("Resident: " + _residentName)));

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

		bool hasOr = !string.IsNullOrWhiteSpace(_orNumber.Text);
		decimal fee = _fee.Value;
		bool needsPayment = fee > 0m || hasOr;
		bool orOk = !needsPayment || hasOr;
		bool methodOk = !needsPayment || _paymentMethod.SelectedIndex >= 0;

		bool flag3 = _issuedDate.Value.Date <= DateTime.Today;

		string text = _type.SelectedItem?.ToString() ?? string.Empty;

		bool flag4 = text.IndexOf("Business", StringComparison.OrdinalIgnoreCase) < 0 || (!string.IsNullOrWhiteSpace(_businessName.Text) && !string.IsNullOrWhiteSpace(_businessNature.Text));

		SetChecklistState(_issueReqPurpose, flag, "Purpose");

		SetChecklistState(_issueReqOr, orOk && methodOk, "OR number");

		SetChecklistState(_issueReqDate, flag3, "Issued date");

		SetChecklistState(_issueReqBusiness, flag4, "Business details");

		_save.Enabled = flag && orOk && methodOk && flag3 && flag4;

	}



	private void SetChecklistState(Label label, bool ok, string text)

	{

		label.Text = (ok ? "[x]" : "[ ]") + " " + text;

		label.ForeColor = (ok ? Color.FromArgb(0, 100, 40) : UiTheme.Slate500);

	}



	private void Save_Click(object? sender, EventArgs e)
	{
		_controller.HandleSave();
	}


	



	

}





