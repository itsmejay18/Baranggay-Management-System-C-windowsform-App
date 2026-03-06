using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal class BlotterForm : Form
{
	private readonly int _complainantId;

	private readonly string? _complainantName;

	private IContainer components = null;

	private FlowLayoutPanel headerPanel;

	private Label lblHeader;

	private Label lblSubHeader;

	private Label lblComplainant;

	private TableLayoutPanel formTable;

	private Label lblRespondent;

	private Label lblIncidentType;

	private Label lblIncidentDate;

	private Label lblDetails;

	private Label lblStatus;

	private Panel respondentPanel;

	private FlowLayoutPanel respondentChoice;

	private RadioButton rbResident;

	private RadioButton rbOther;

	private TableLayoutPanel respondentFields;

	private ComboBox cmbRespondent;

	private TextBox txtRespondentOther;

	private TextBox txtIncidentType;

	private DateTimePicker dtpIncidentDate;

	private TextBox txtIncidentDetails;

	private ComboBox cmbStatus;

	private FlowLayoutPanel buttonPanel;

	private Button btnSave;

	private Button btnCancel;

	public BlotterDto Blotter => new BlotterDto
	{
		ComplainantId = _complainantId,
		RespondentName = GetRespondentName(),
		IncidentType = txtIncidentType.Text.Trim(),
		IncidentDate = dtpIncidentDate.Value.Date,
		IncidentDetails = txtIncidentDetails.Text.Trim(),
		Status = (cmbStatus.SelectedItem?.ToString() ?? "Ongoing"),
		RecordedBy = UserSession.UserId
	};

	public BlotterForm(int complainantId, string? complainantName, IEnumerable<string>? respondentSuggestions = null)
	{
		_complainantId = complainantId;
		_complainantName = complainantName;
		InitializeComponent();
		ApplyTheme();
		if (cmbStatus.Items.Count > 0 && cmbStatus.SelectedIndex < 0)
		{
			cmbStatus.SelectedIndex = 0;
		}
		PopulateComplainant();
		LoadRespondentSuggestions(respondentSuggestions);
		UpdateRespondentMode();
	}

	private void ApplyTheme()
	{
		BackColor = UiTheme.Slate50;
		Font = UiTheme.BodyFont;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		UiTheme.StyleComboBox(cmbRespondent);
		UiTheme.StyleTextBox(txtRespondentOther);
		UiTheme.StyleTextBox(txtIncidentType);
		UiTheme.StyleTextBox(txtIncidentDetails);
		UiTheme.StyleComboBox(cmbStatus);
		dtpIncidentDate.Font = UiTheme.BodyFont;
		UiTheme.StylePrimaryButton(btnSave);
		UiTheme.StyleSecondaryButton(btnCancel);
		lblHeader.Font = UiTheme.HeadingFont;
		lblHeader.ForeColor = UiTheme.Slate900;
		lblSubHeader.Font = UiTheme.LabelFont;
		lblSubHeader.ForeColor = UiTheme.Slate500;
		lblComplainant.Font = UiTheme.LabelFont;
		lblComplainant.ForeColor = UiTheme.Slate500;
		lblRespondent.Font = UiTheme.LabelFont;
		lblRespondent.ForeColor = UiTheme.Slate500;
		lblIncidentType.Font = UiTheme.LabelFont;
		lblIncidentType.ForeColor = UiTheme.Slate500;
		lblIncidentDate.Font = UiTheme.LabelFont;
		lblIncidentDate.ForeColor = UiTheme.Slate500;
		lblDetails.Font = UiTheme.LabelFont;
		lblDetails.ForeColor = UiTheme.Slate500;
		lblStatus.Font = UiTheme.LabelFont;
		lblStatus.ForeColor = UiTheme.Slate500;
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
		if (respondentSuggestions == null)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (string respondentSuggestion in respondentSuggestions)
		{
			if (!string.IsNullOrWhiteSpace(respondentSuggestion) && !list.Contains(respondentSuggestion))
			{
				list.Add(respondentSuggestion);
			}
		}
		if (list.Count > 0)
		{
			ComboBox.ObjectCollection items = cmbRespondent.Items;
			object[] items2 = list.ToArray();
			items.AddRange(items2);
		}
	}

	private void RespondentMode_CheckedChanged(object? sender, EventArgs e)
	{
		UpdateRespondentMode();
	}

	private void ValidateAndClose(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(GetRespondentName()) || string.IsNullOrWhiteSpace(txtIncidentType.Text))
		{
			MessageBox.Show("Respondent and incident type are required.", "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		if (dtpIncidentDate.Value.Date > DateTime.Today)
		{
			MessageBox.Show("Incident date cannot be in the future.", "Invalid date", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void UpdateRespondentMode()
	{
		bool flag = rbResident.Checked;
		cmbRespondent.Enabled = flag;
		txtRespondentOther.Enabled = !flag;
		cmbRespondent.BackColor = Color.White;
		txtRespondentOther.BackColor = Color.White;
		if (flag)
		{
			txtRespondentOther.Text = string.Empty;
		}
	}

	private string GetRespondentName()
	{
		return rbResident.Checked ? cmbRespondent.Text.Trim() : txtRespondentOther.Text.Trim();
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
		this.headerPanel = new System.Windows.Forms.FlowLayoutPanel();
		this.lblHeader = new System.Windows.Forms.Label();
		this.lblSubHeader = new System.Windows.Forms.Label();
		this.lblComplainant = new System.Windows.Forms.Label();
		this.formTable = new System.Windows.Forms.TableLayoutPanel();
		this.lblRespondent = new System.Windows.Forms.Label();
		this.lblIncidentType = new System.Windows.Forms.Label();
		this.lblIncidentDate = new System.Windows.Forms.Label();
		this.lblDetails = new System.Windows.Forms.Label();
		this.lblStatus = new System.Windows.Forms.Label();
		this.respondentPanel = new System.Windows.Forms.Panel();
		this.respondentChoice = new System.Windows.Forms.FlowLayoutPanel();
		this.rbResident = new System.Windows.Forms.RadioButton();
		this.rbOther = new System.Windows.Forms.RadioButton();
		this.respondentFields = new System.Windows.Forms.TableLayoutPanel();
		this.cmbRespondent = new System.Windows.Forms.ComboBox();
		this.txtRespondentOther = new System.Windows.Forms.TextBox();
		this.txtIncidentType = new System.Windows.Forms.TextBox();
		this.dtpIncidentDate = new System.Windows.Forms.DateTimePicker();
		this.txtIncidentDetails = new System.Windows.Forms.TextBox();
		this.cmbStatus = new System.Windows.Forms.ComboBox();
		this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
		this.btnSave = new System.Windows.Forms.Button();
		this.btnCancel = new System.Windows.Forms.Button();
		this.headerPanel.SuspendLayout();
		this.formTable.SuspendLayout();
		this.respondentPanel.SuspendLayout();
		this.respondentChoice.SuspendLayout();
		this.respondentFields.SuspendLayout();
		this.buttonPanel.SuspendLayout();
		base.SuspendLayout();
		this.headerPanel.AutoSize = true;
		this.headerPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.headerPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
		this.headerPanel.Location = new System.Drawing.Point(18, 18);
		this.headerPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
		this.headerPanel.Name = "headerPanel";
		this.headerPanel.Size = new System.Drawing.Size(220, 63);
		this.headerPanel.TabIndex = 0;
		this.headerPanel.WrapContents = false;
		this.headerPanel.Controls.Add(this.lblHeader);
		this.headerPanel.Controls.Add(this.lblSubHeader);
		this.headerPanel.Controls.Add(this.lblComplainant);
		this.lblHeader.AutoSize = true;
		this.lblHeader.Font = new System.Drawing.Font("Century Gothic", 14f, System.Drawing.FontStyle.Bold);
		this.lblHeader.Location = new System.Drawing.Point(0, 0);
		this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.lblHeader.Name = "lblHeader";
		this.lblHeader.Size = new System.Drawing.Size(190, 23);
		this.lblHeader.TabIndex = 0;
		this.lblHeader.Text = "New Blotter Record";
		this.lblSubHeader.AutoSize = true;
		this.lblSubHeader.Location = new System.Drawing.Point(0, 27);
		this.lblSubHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
		this.lblSubHeader.Name = "lblSubHeader";
		this.lblSubHeader.Size = new System.Drawing.Size(258, 15);
		this.lblSubHeader.TabIndex = 1;
		this.lblSubHeader.Text = "Provide incident details and respondent information.";
		this.lblComplainant.AutoSize = true;
		this.lblComplainant.Location = new System.Drawing.Point(0, 44);
		this.lblComplainant.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
		this.lblComplainant.Name = "lblComplainant";
		this.lblComplainant.Size = new System.Drawing.Size(89, 15);
		this.lblComplainant.TabIndex = 2;
		this.lblComplainant.Text = "Complainant:";
		this.formTable.AutoSize = true;
		this.formTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.formTable.ColumnCount = 2;
		this.formTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140f));
		this.formTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260f));
		this.formTable.Controls.Add(this.lblRespondent, 0, 0);
		this.formTable.Controls.Add(this.respondentPanel, 1, 0);
		this.formTable.Controls.Add(this.lblIncidentType, 0, 1);
		this.formTable.Controls.Add(this.txtIncidentType, 1, 1);
		this.formTable.Controls.Add(this.lblIncidentDate, 0, 2);
		this.formTable.Controls.Add(this.dtpIncidentDate, 1, 2);
		this.formTable.Controls.Add(this.lblDetails, 0, 3);
		this.formTable.Controls.Add(this.txtIncidentDetails, 1, 3);
		this.formTable.Controls.Add(this.lblStatus, 0, 4);
		this.formTable.Controls.Add(this.cmbStatus, 1, 4);
		this.formTable.Location = new System.Drawing.Point(18, 91);
		this.formTable.Margin = new System.Windows.Forms.Padding(0);
		this.formTable.Name = "formTable";
		this.formTable.RowCount = 5;
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.formTable.Size = new System.Drawing.Size(400, 303);
		this.formTable.TabIndex = 1;
		this.lblRespondent.AutoSize = true;
		this.lblRespondent.Location = new System.Drawing.Point(0, 8);
		this.lblRespondent.Margin = new System.Windows.Forms.Padding(0, 8, 0, 4);
		this.lblRespondent.Name = "lblRespondent";
		this.lblRespondent.Size = new System.Drawing.Size(72, 15);
		this.lblRespondent.TabIndex = 0;
		this.lblRespondent.Text = "Respondent";
		this.lblIncidentType.AutoSize = true;
		this.lblIncidentType.Location = new System.Drawing.Point(0, 78);
		this.lblIncidentType.Margin = new System.Windows.Forms.Padding(0, 8, 0, 4);
		this.lblIncidentType.Name = "lblIncidentType";
		this.lblIncidentType.Size = new System.Drawing.Size(77, 15);
		this.lblIncidentType.TabIndex = 2;
		this.lblIncidentType.Text = "Incident type";
		this.lblIncidentDate.AutoSize = true;
		this.lblIncidentDate.Location = new System.Drawing.Point(0, 118);
		this.lblIncidentDate.Margin = new System.Windows.Forms.Padding(0, 8, 0, 4);
		this.lblIncidentDate.Name = "lblIncidentDate";
		this.lblIncidentDate.Size = new System.Drawing.Size(80, 15);
		this.lblIncidentDate.TabIndex = 4;
		this.lblIncidentDate.Text = "Incident date";
		this.lblDetails.AutoSize = true;
		this.lblDetails.Location = new System.Drawing.Point(0, 158);
		this.lblDetails.Margin = new System.Windows.Forms.Padding(0, 8, 0, 4);
		this.lblDetails.Name = "lblDetails";
		this.lblDetails.Size = new System.Drawing.Size(43, 15);
		this.lblDetails.TabIndex = 6;
		this.lblDetails.Text = "Details";
		this.lblStatus.AutoSize = true;
		this.lblStatus.Location = new System.Drawing.Point(0, 267);
		this.lblStatus.Margin = new System.Windows.Forms.Padding(0, 8, 0, 4);
		this.lblStatus.Name = "lblStatus";
		this.lblStatus.Size = new System.Drawing.Size(39, 15);
		this.lblStatus.TabIndex = 8;
		this.lblStatus.Text = "Status";
		this.respondentPanel.AutoSize = true;
		this.respondentPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.respondentPanel.Controls.Add(this.respondentFields);
		this.respondentPanel.Controls.Add(this.respondentChoice);
		this.respondentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.respondentPanel.Location = new System.Drawing.Point(140, 0);
		this.respondentPanel.Margin = new System.Windows.Forms.Padding(0);
		this.respondentPanel.Name = "respondentPanel";
		this.respondentPanel.Size = new System.Drawing.Size(260, 70);
		this.respondentPanel.TabIndex = 1;
		this.respondentChoice.AutoSize = true;
		this.respondentChoice.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.respondentChoice.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
		this.respondentChoice.Location = new System.Drawing.Point(0, 0);
		this.respondentChoice.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
		this.respondentChoice.Name = "respondentChoice";
		this.respondentChoice.Size = new System.Drawing.Size(158, 19);
		this.respondentChoice.TabIndex = 0;
		this.respondentChoice.WrapContents = false;
		this.respondentChoice.Controls.Add(this.rbResident);
		this.respondentChoice.Controls.Add(this.rbOther);
		this.rbResident.AutoSize = true;
		this.rbResident.Checked = true;
		this.rbResident.Location = new System.Drawing.Point(0, 0);
		this.rbResident.Margin = new System.Windows.Forms.Padding(0);
		this.rbResident.Name = "rbResident";
		this.rbResident.Size = new System.Drawing.Size(72, 19);
		this.rbResident.TabIndex = 0;
		this.rbResident.TabStop = true;
		this.rbResident.Text = "Resident";
		this.rbResident.UseVisualStyleBackColor = true;
		this.rbResident.CheckedChanged += new System.EventHandler(RespondentMode_CheckedChanged);
		this.rbOther.AutoSize = true;
		this.rbOther.Location = new System.Drawing.Point(84, 0);
		this.rbOther.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
		this.rbOther.Name = "rbOther";
		this.rbOther.Size = new System.Drawing.Size(54, 19);
		this.rbOther.TabIndex = 1;
		this.rbOther.Text = "Other";
		this.rbOther.UseVisualStyleBackColor = true;
		this.rbOther.CheckedChanged += new System.EventHandler(RespondentMode_CheckedChanged);
		this.respondentFields.AutoSize = true;
		this.respondentFields.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.respondentFields.ColumnCount = 2;
		this.respondentFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.respondentFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.respondentFields.Controls.Add(this.cmbRespondent, 0, 0);
		this.respondentFields.Controls.Add(this.txtRespondentOther, 1, 0);
		this.respondentFields.Location = new System.Drawing.Point(0, 25);
		this.respondentFields.Margin = new System.Windows.Forms.Padding(0);
		this.respondentFields.Name = "respondentFields";
		this.respondentFields.RowCount = 1;
		this.respondentFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.respondentFields.Size = new System.Drawing.Size(260, 23);
		this.respondentFields.TabIndex = 1;
		this.cmbRespondent.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
		this.cmbRespondent.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
		this.cmbRespondent.Dock = System.Windows.Forms.DockStyle.Fill;
		this.cmbRespondent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
		this.cmbRespondent.FormattingEnabled = true;
		this.cmbRespondent.IntegralHeight = false;
		this.cmbRespondent.Location = new System.Drawing.Point(0, 0);
		this.cmbRespondent.Margin = new System.Windows.Forms.Padding(0);
		this.cmbRespondent.MaxDropDownItems = 10;
		this.cmbRespondent.DropDownWidth = 260;
		this.cmbRespondent.Name = "cmbRespondent";
		this.cmbRespondent.Size = new System.Drawing.Size(130, 23);
		this.cmbRespondent.TabIndex = 0;
		this.txtRespondentOther.Dock = System.Windows.Forms.DockStyle.Fill;
		this.txtRespondentOther.Location = new System.Drawing.Point(138, 0);
		this.txtRespondentOther.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
		this.txtRespondentOther.Name = "txtRespondentOther";
		this.txtRespondentOther.Size = new System.Drawing.Size(122, 23);
		this.txtRespondentOther.TabIndex = 1;
		this.txtIncidentType.Location = new System.Drawing.Point(140, 74);
		this.txtIncidentType.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
		this.txtIncidentType.Name = "txtIncidentType";
		this.txtIncidentType.Size = new System.Drawing.Size(260, 23);
		this.txtIncidentType.TabIndex = 3;
		this.dtpIncidentDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
		this.dtpIncidentDate.Location = new System.Drawing.Point(140, 114);
		this.dtpIncidentDate.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
		this.dtpIncidentDate.Name = "dtpIncidentDate";
		this.dtpIncidentDate.Size = new System.Drawing.Size(260, 23);
		this.dtpIncidentDate.TabIndex = 5;
		this.txtIncidentDetails.Location = new System.Drawing.Point(140, 154);
		this.txtIncidentDetails.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
		this.txtIncidentDetails.Multiline = true;
		this.txtIncidentDetails.Name = "txtIncidentDetails";
		this.txtIncidentDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
		this.txtIncidentDetails.Size = new System.Drawing.Size(260, 90);
		this.txtIncidentDetails.TabIndex = 7;
		this.cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbStatus.FormattingEnabled = true;
		this.cmbStatus.Items.AddRange(new object[3] { "Ongoing", "Settled", "Referred" });
		this.cmbStatus.Location = new System.Drawing.Point(140, 263);
		this.cmbStatus.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
		this.cmbStatus.Name = "cmbStatus";
		this.cmbStatus.Size = new System.Drawing.Size(260, 23);
		this.cmbStatus.TabIndex = 9;
		this.buttonPanel.AutoSize = true;
		this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		this.buttonPanel.Location = new System.Drawing.Point(18, 408);
		this.buttonPanel.Margin = new System.Windows.Forms.Padding(0);
		this.buttonPanel.Name = "buttonPanel";
		this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
		this.buttonPanel.Size = new System.Drawing.Size(158, 44);
		this.buttonPanel.TabIndex = 2;
		this.buttonPanel.WrapContents = false;
		this.buttonPanel.Controls.Add(this.btnSave);
		this.buttonPanel.Controls.Add(this.btnCancel);
		this.btnSave.AutoSize = true;
		this.btnSave.Location = new System.Drawing.Point(83, 12);
		this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(75, 32);
		this.btnSave.TabIndex = 0;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Click += new System.EventHandler(ValidateAndClose);
		this.btnCancel.AutoSize = true;
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Location = new System.Drawing.Point(0, 12);
		this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 32);
		this.btnCancel.TabIndex = 1;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		base.AcceptButton = this.btnSave;
		base.CancelButton = this.btnCancel;
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(460, 520);
		base.Controls.Add(this.buttonPanel);
		base.Controls.Add(this.formTable);
		base.Controls.Add(this.headerPanel);
		this.MinimumSize = new System.Drawing.Size(440, 500);
		base.Name = "BlotterForm";
		base.Padding = new System.Windows.Forms.Padding(18);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "File Blotter";
		this.headerPanel.ResumeLayout(false);
		this.headerPanel.PerformLayout();
		this.formTable.ResumeLayout(false);
		this.formTable.PerformLayout();
		this.respondentPanel.ResumeLayout(false);
		this.respondentPanel.PerformLayout();
		this.respondentChoice.ResumeLayout(false);
		this.respondentChoice.PerformLayout();
		this.respondentFields.ResumeLayout(false);
		this.respondentFields.PerformLayout();
		this.buttonPanel.ResumeLayout(false);
		this.buttonPanel.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
