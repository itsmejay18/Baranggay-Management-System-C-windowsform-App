using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace baranggaysystem1;

internal class ResidentForm : Form
{
	private readonly int? _residentId;

	private byte[]? _photoBytes;

	private IContainer components = null;

	private Panel cardPanel;

	private TableLayoutPanel cardLayout;

	private Label lblHeader;

	private Label lblSubHeader;

	private TableLayoutPanel bodyLayout;

	private TableLayoutPanel fieldsTable;

	private Label lblFirstName;

	private Label lblMiddleName;

	private Label lblLastName;

	private Label lblGender;

	private Label lblBirthDate;

	private Label lblCivilStatus;

	private Label lblContact;

	private Label lblStatus;

	private TextBox txtFirstName;

	private TextBox txtMiddleName;

	private TextBox txtLastName;

	private ComboBox cmbGender;

	private DateTimePicker dtpBirthDate;

	private ComboBox cmbCivilStatus;

	private TextBox txtContact;

	private ComboBox cmbStatus;

	private FlowLayoutPanel photoPanel;

	private Label lblPhotoCaption;

	private PictureBox picPhoto;

	private FlowLayoutPanel photoButtonRow;

	private Button btnPhotoUpload;

	private Button btnPhotoRemove;

	private FlowLayoutPanel buttonPanel;

	private Button btnSave;

	private Button btnCancel;

	public ResidentDto Resident => new ResidentDto
	{
		Id = _residentId,
		FirstName = txtFirstName.Text.Trim(),
		MiddleName = txtMiddleName.Text.Trim(),
		LastName = txtLastName.Text.Trim(),
		Gender = (cmbGender.SelectedItem?.ToString() ?? cmbGender.Text.Trim()),
		DateOfBirth = dtpBirthDate.Value.Date,
		CivilStatus = (cmbCivilStatus.SelectedItem?.ToString() ?? cmbCivilStatus.Text.Trim()),
		ContactNo = txtContact.Text.Trim(),
		Status = (cmbStatus.SelectedItem?.ToString() ?? cmbStatus.Text.Trim()),
		PhotoBytes = _photoBytes
	};

	public ResidentForm(string title, ResidentDto? existing = null)
	{
		_residentId = existing?.Id;
		InitializeComponent();
		Text = title;
		ApplyTheme();
		if (existing != null)
		{
			Populate(existing);
		}
		UpdatePhotoPreview();
	}

	private void ApplyTheme()
	{
		BackColor = UiTheme.Slate50;
		Font = UiTheme.BodyFont;
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		UiTheme.StyleTextBox(txtFirstName);
		UiTheme.StyleTextBox(txtMiddleName);
		UiTheme.StyleTextBox(txtLastName);
		UiTheme.StyleComboBox(cmbGender);
		UiTheme.StyleComboBox(cmbCivilStatus);
		UiTheme.StyleTextBox(txtContact);
		UiTheme.StyleComboBox(cmbStatus);
		UiTheme.StyleSecondaryButton(btnPhotoUpload);
		UiTheme.StyleDangerButton(btnPhotoRemove);
		UiTheme.StylePrimaryButton(btnSave);
		UiTheme.StyleSecondaryButton(btnCancel);
		lblHeader.Font = UiTheme.HeadingFont;
		lblHeader.ForeColor = UiTheme.Slate900;
		lblSubHeader.Font = UiTheme.LabelFont;
		lblSubHeader.ForeColor = UiTheme.Slate500;
		lblFirstName.Font = UiTheme.LabelFont;
		lblFirstName.ForeColor = UiTheme.Slate700;
		lblMiddleName.Font = UiTheme.LabelFont;
		lblMiddleName.ForeColor = UiTheme.Slate700;
		lblLastName.Font = UiTheme.LabelFont;
		lblLastName.ForeColor = UiTheme.Slate700;
		lblGender.Font = UiTheme.LabelFont;
		lblGender.ForeColor = UiTheme.Slate700;
		lblBirthDate.Font = UiTheme.LabelFont;
		lblBirthDate.ForeColor = UiTheme.Slate700;
		lblCivilStatus.Font = UiTheme.LabelFont;
		lblCivilStatus.ForeColor = UiTheme.Slate700;
		lblContact.Font = UiTheme.LabelFont;
		lblContact.ForeColor = UiTheme.Slate700;
		lblStatus.Font = UiTheme.LabelFont;
		lblStatus.ForeColor = UiTheme.Slate700;
		lblPhotoCaption.Font = UiTheme.LabelFont;
		lblPhotoCaption.ForeColor = UiTheme.Slate500;
	}

	private void PhotoUpload_Click(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
			Title = "Select a resident photo"
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			_photoBytes = File.ReadAllBytes(openFileDialog.FileName);
			UpdatePhotoPreview();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Unable to read photo.\n" + ex.Message, "Photo Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void PhotoRemove_Click(object? sender, EventArgs e)
	{
		_photoBytes = null;
		UpdatePhotoPreview();
	}

	private void UpdatePhotoPreview()
	{
		if (_photoBytes == null || _photoBytes.Length == 0)
		{
			picPhoto.Image = null;
			lblPhotoCaption.Text = "No photo";
			btnPhotoRemove.Enabled = false;
			return;
		}
		try
		{
			using MemoryStream stream = new MemoryStream(_photoBytes);
			picPhoto.Image = Image.FromStream(stream);
			lblPhotoCaption.Text = "Photo selected";
			btnPhotoRemove.Enabled = true;
		}
		catch
		{
			picPhoto.Image = null;
			lblPhotoCaption.Text = "Invalid photo";
			btnPhotoRemove.Enabled = false;
		}
	}

	private static void SelectComboValue(ComboBox comboBox, string value)
	{
		if (comboBox.Items.Count == 0)
		{
			return;
		}
		for (int i = 0; i < comboBox.Items.Count; i++)
		{
			if (string.Equals(comboBox.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
			{
				comboBox.SelectedIndex = i;
				return;
			}
		}
		comboBox.SelectedIndex = 0;
	}

	private void Populate(ResidentDto resident)
	{
		txtFirstName.Text = resident.FirstName;
		txtMiddleName.Text = resident.MiddleName;
		txtLastName.Text = resident.LastName;
		SelectComboValue(cmbGender, resident.Gender);
		dtpBirthDate.Value = resident.DateOfBirth;
		SelectComboValue(cmbCivilStatus, resident.CivilStatus);
		txtContact.Text = resident.ContactNo;
		SelectComboValue(cmbStatus, resident.Status);
		_photoBytes = resident.PhotoBytes;
		UpdatePhotoPreview();
	}

	private void ValidateAndClose(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
		{
			MessageBox.Show("First name and last name are required.", "Missing data", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		if (dtpBirthDate.Value.Date > DateTime.Today)
		{
			MessageBox.Show("Date of birth cannot be in the future.", "Invalid date", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		base.DialogResult = DialogResult.OK;
		Close();
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
		this.cardPanel = new System.Windows.Forms.Panel();
		this.cardLayout = new System.Windows.Forms.TableLayoutPanel();
		this.lblHeader = new System.Windows.Forms.Label();
		this.lblSubHeader = new System.Windows.Forms.Label();
		this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
		this.fieldsTable = new System.Windows.Forms.TableLayoutPanel();
		this.lblFirstName = new System.Windows.Forms.Label();
		this.lblMiddleName = new System.Windows.Forms.Label();
		this.lblLastName = new System.Windows.Forms.Label();
		this.lblGender = new System.Windows.Forms.Label();
		this.lblBirthDate = new System.Windows.Forms.Label();
		this.lblCivilStatus = new System.Windows.Forms.Label();
		this.lblContact = new System.Windows.Forms.Label();
		this.lblStatus = new System.Windows.Forms.Label();
		this.txtFirstName = new System.Windows.Forms.TextBox();
		this.txtMiddleName = new System.Windows.Forms.TextBox();
		this.txtLastName = new System.Windows.Forms.TextBox();
		this.cmbGender = new System.Windows.Forms.ComboBox();
		this.dtpBirthDate = new System.Windows.Forms.DateTimePicker();
		this.cmbCivilStatus = new System.Windows.Forms.ComboBox();
		this.txtContact = new System.Windows.Forms.TextBox();
		this.cmbStatus = new System.Windows.Forms.ComboBox();
		this.photoPanel = new System.Windows.Forms.FlowLayoutPanel();
		this.lblPhotoCaption = new System.Windows.Forms.Label();
		this.picPhoto = new System.Windows.Forms.PictureBox();
		this.photoButtonRow = new System.Windows.Forms.FlowLayoutPanel();
		this.btnPhotoUpload = new System.Windows.Forms.Button();
		this.btnPhotoRemove = new System.Windows.Forms.Button();
		this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
		this.btnSave = new System.Windows.Forms.Button();
		this.btnCancel = new System.Windows.Forms.Button();
		this.cardPanel.SuspendLayout();
		this.cardLayout.SuspendLayout();
		this.bodyLayout.SuspendLayout();
		this.fieldsTable.SuspendLayout();
		this.photoPanel.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.picPhoto).BeginInit();
		this.photoButtonRow.SuspendLayout();
		this.buttonPanel.SuspendLayout();
		base.SuspendLayout();
		this.cardPanel.AutoSize = true;
		this.cardPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.cardPanel.BackColor = System.Drawing.Color.White;
		this.cardPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.cardPanel.Controls.Add(this.cardLayout);
		this.cardPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.cardPanel.Location = new System.Drawing.Point(16, 16);
		this.cardPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
		this.cardPanel.Name = "cardPanel";
		this.cardPanel.Padding = new System.Windows.Forms.Padding(16);
		this.cardPanel.Size = new System.Drawing.Size(748, 392);
		this.cardPanel.TabIndex = 0;
		this.cardLayout.AutoSize = true;
		this.cardLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.cardLayout.ColumnCount = 1;
		this.cardLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.cardLayout.Controls.Add(this.lblHeader, 0, 0);
		this.cardLayout.Controls.Add(this.lblSubHeader, 0, 1);
		this.cardLayout.Controls.Add(this.bodyLayout, 0, 2);
		this.cardLayout.Dock = System.Windows.Forms.DockStyle.Top;
		this.cardLayout.Location = new System.Drawing.Point(16, 16);
		this.cardLayout.Name = "cardLayout";
		this.cardLayout.RowCount = 3;
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.Size = new System.Drawing.Size(714, 358);
		this.cardLayout.TabIndex = 0;
		this.lblHeader.AutoSize = true;
		this.lblHeader.Font = new System.Drawing.Font("Century Gothic", 14f, System.Drawing.FontStyle.Bold);
		this.lblHeader.ForeColor = System.Drawing.Color.Black;
		this.lblHeader.Location = new System.Drawing.Point(0, 0);
		this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.lblHeader.Name = "lblHeader";
		this.lblHeader.Size = new System.Drawing.Size(156, 23);
		this.lblHeader.TabIndex = 0;
		this.lblHeader.Text = "Resident Details";
		this.lblSubHeader.AutoSize = true;
		this.lblSubHeader.Font = new System.Drawing.Font("Trebuchet MS", 9f);
		this.lblSubHeader.ForeColor = System.Drawing.Color.DimGray;
		this.lblSubHeader.Location = new System.Drawing.Point(0, 27);
		this.lblSubHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
		this.lblSubHeader.Name = "lblSubHeader";
		this.lblSubHeader.Size = new System.Drawing.Size(232, 18);
		this.lblSubHeader.TabIndex = 1;
		this.lblSubHeader.Text = "Fill out the resident information below.";
		this.bodyLayout.AutoSize = true;
		this.bodyLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.bodyLayout.ColumnCount = 2;
		this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 420f));
		this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240f));
		this.bodyLayout.Controls.Add(this.fieldsTable, 0, 0);
		this.bodyLayout.Controls.Add(this.photoPanel, 1, 0);
		this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Top;
		this.bodyLayout.Location = new System.Drawing.Point(0, 61);
		this.bodyLayout.Margin = new System.Windows.Forms.Padding(0);
		this.bodyLayout.Name = "bodyLayout";
		this.bodyLayout.RowCount = 1;
		this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.bodyLayout.Size = new System.Drawing.Size(660, 297);
		this.bodyLayout.TabIndex = 2;
		this.fieldsTable.AutoSize = true;
		this.fieldsTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.fieldsTable.ColumnCount = 2;
		this.fieldsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130f));
		this.fieldsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260f));
		this.fieldsTable.Controls.Add(this.lblFirstName, 0, 0);
		this.fieldsTable.Controls.Add(this.txtFirstName, 1, 0);
		this.fieldsTable.Controls.Add(this.lblMiddleName, 0, 1);
		this.fieldsTable.Controls.Add(this.txtMiddleName, 1, 1);
		this.fieldsTable.Controls.Add(this.lblLastName, 0, 2);
		this.fieldsTable.Controls.Add(this.txtLastName, 1, 2);
		this.fieldsTable.Controls.Add(this.lblGender, 0, 3);
		this.fieldsTable.Controls.Add(this.cmbGender, 1, 3);
		this.fieldsTable.Controls.Add(this.lblBirthDate, 0, 4);
		this.fieldsTable.Controls.Add(this.dtpBirthDate, 1, 4);
		this.fieldsTable.Controls.Add(this.lblCivilStatus, 0, 5);
		this.fieldsTable.Controls.Add(this.cmbCivilStatus, 1, 5);
		this.fieldsTable.Controls.Add(this.lblContact, 0, 6);
		this.fieldsTable.Controls.Add(this.txtContact, 1, 6);
		this.fieldsTable.Controls.Add(this.lblStatus, 0, 7);
		this.fieldsTable.Controls.Add(this.cmbStatus, 1, 7);
		this.fieldsTable.Dock = System.Windows.Forms.DockStyle.Top;
		this.fieldsTable.Location = new System.Drawing.Point(0, 0);
		this.fieldsTable.Margin = new System.Windows.Forms.Padding(0);
		this.fieldsTable.Name = "fieldsTable";
		this.fieldsTable.RowCount = 8;
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.fieldsTable.Size = new System.Drawing.Size(390, 297);
		this.fieldsTable.TabIndex = 0;
		this.lblFirstName.AutoSize = true;
		this.lblFirstName.Location = new System.Drawing.Point(3, 8);
		this.lblFirstName.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblFirstName.Name = "lblFirstName";
		this.lblFirstName.Size = new System.Drawing.Size(66, 15);
		this.lblFirstName.TabIndex = 0;
		this.lblFirstName.Text = "First name";
		this.lblMiddleName.AutoSize = true;
		this.lblMiddleName.Location = new System.Drawing.Point(3, 44);
		this.lblMiddleName.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblMiddleName.Name = "lblMiddleName";
		this.lblMiddleName.Size = new System.Drawing.Size(78, 15);
		this.lblMiddleName.TabIndex = 2;
		this.lblMiddleName.Text = "Middle name";
		this.lblLastName.AutoSize = true;
		this.lblLastName.Location = new System.Drawing.Point(3, 80);
		this.lblLastName.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblLastName.Name = "lblLastName";
		this.lblLastName.Size = new System.Drawing.Size(64, 15);
		this.lblLastName.TabIndex = 4;
		this.lblLastName.Text = "Last name";
		this.lblGender.AutoSize = true;
		this.lblGender.Location = new System.Drawing.Point(3, 116);
		this.lblGender.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblGender.Name = "lblGender";
		this.lblGender.Size = new System.Drawing.Size(45, 15);
		this.lblGender.TabIndex = 6;
		this.lblGender.Text = "Gender";
		this.lblBirthDate.AutoSize = true;
		this.lblBirthDate.Location = new System.Drawing.Point(3, 152);
		this.lblBirthDate.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblBirthDate.Name = "lblBirthDate";
		this.lblBirthDate.Size = new System.Drawing.Size(72, 15);
		this.lblBirthDate.TabIndex = 8;
		this.lblBirthDate.Text = "Date of birth";
		this.lblCivilStatus.AutoSize = true;
		this.lblCivilStatus.Location = new System.Drawing.Point(3, 188);
		this.lblCivilStatus.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblCivilStatus.Name = "lblCivilStatus";
		this.lblCivilStatus.Size = new System.Drawing.Size(62, 15);
		this.lblCivilStatus.TabIndex = 10;
		this.lblCivilStatus.Text = "Civil status";
		this.lblContact.AutoSize = true;
		this.lblContact.Location = new System.Drawing.Point(3, 224);
		this.lblContact.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblContact.Name = "lblContact";
		this.lblContact.Size = new System.Drawing.Size(63, 15);
		this.lblContact.TabIndex = 12;
		this.lblContact.Text = "Contact no.";
		this.lblStatus.AutoSize = true;
		this.lblStatus.Location = new System.Drawing.Point(3, 260);
		this.lblStatus.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
		this.lblStatus.Name = "lblStatus";
		this.lblStatus.Size = new System.Drawing.Size(39, 15);
		this.lblStatus.TabIndex = 14;
		this.lblStatus.Text = "Status";
		this.txtFirstName.Location = new System.Drawing.Point(133, 3);
		this.txtFirstName.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.txtFirstName.Name = "txtFirstName";
		this.txtFirstName.Size = new System.Drawing.Size(254, 23);
		this.txtFirstName.TabIndex = 1;
		this.txtMiddleName.Location = new System.Drawing.Point(133, 39);
		this.txtMiddleName.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.txtMiddleName.Name = "txtMiddleName";
		this.txtMiddleName.Size = new System.Drawing.Size(254, 23);
		this.txtMiddleName.TabIndex = 3;
		this.txtLastName.Location = new System.Drawing.Point(133, 75);
		this.txtLastName.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.txtLastName.Name = "txtLastName";
		this.txtLastName.Size = new System.Drawing.Size(254, 23);
		this.txtLastName.TabIndex = 5;
		this.cmbGender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbGender.FormattingEnabled = true;
		this.cmbGender.Items.AddRange(new object[3] { "Male", "Female", "Other" });
		this.cmbGender.Location = new System.Drawing.Point(133, 111);
		this.cmbGender.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.cmbGender.Name = "cmbGender";
		this.cmbGender.Size = new System.Drawing.Size(254, 23);
		this.cmbGender.TabIndex = 7;
		this.dtpBirthDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
		this.dtpBirthDate.Location = new System.Drawing.Point(133, 147);
		this.dtpBirthDate.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.dtpBirthDate.Name = "dtpBirthDate";
		this.dtpBirthDate.Size = new System.Drawing.Size(254, 23);
		this.dtpBirthDate.TabIndex = 9;
		this.cmbCivilStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbCivilStatus.FormattingEnabled = true;
		this.cmbCivilStatus.Items.AddRange(new object[4] { "Single", "Married", "Widowed", "Separated" });
		this.cmbCivilStatus.Location = new System.Drawing.Point(133, 183);
		this.cmbCivilStatus.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.cmbCivilStatus.Name = "cmbCivilStatus";
		this.cmbCivilStatus.Size = new System.Drawing.Size(254, 23);
		this.cmbCivilStatus.TabIndex = 11;
		this.txtContact.Location = new System.Drawing.Point(133, 219);
		this.txtContact.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.txtContact.Name = "txtContact";
		this.txtContact.Size = new System.Drawing.Size(254, 23);
		this.txtContact.TabIndex = 13;
		this.cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cmbStatus.FormattingEnabled = true;
		this.cmbStatus.Items.AddRange(new object[3] { "Active", "Inactive", "Deceased" });
		this.cmbStatus.Location = new System.Drawing.Point(133, 255);
		this.cmbStatus.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
		this.cmbStatus.Name = "cmbStatus";
		this.cmbStatus.Size = new System.Drawing.Size(254, 23);
		this.cmbStatus.TabIndex = 15;
		this.photoPanel.AutoSize = true;
		this.photoPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.photoPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
		this.photoPanel.Location = new System.Drawing.Point(420, 0);
		this.photoPanel.Margin = new System.Windows.Forms.Padding(0);
		this.photoPanel.Name = "photoPanel";
		this.photoPanel.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
		this.photoPanel.Size = new System.Drawing.Size(240, 240);
		this.photoPanel.TabIndex = 1;
		this.photoPanel.WrapContents = false;
		this.photoPanel.Controls.Add(this.lblPhotoCaption);
		this.photoPanel.Controls.Add(this.picPhoto);
		this.photoPanel.Controls.Add(this.photoButtonRow);
		this.lblPhotoCaption.AutoSize = true;
		this.lblPhotoCaption.Location = new System.Drawing.Point(12, 0);
		this.lblPhotoCaption.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
		this.lblPhotoCaption.Name = "lblPhotoCaption";
		this.lblPhotoCaption.Size = new System.Drawing.Size(83, 15);
		this.lblPhotoCaption.TabIndex = 0;
		this.lblPhotoCaption.Text = "Resident photo";
		this.picPhoto.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
		this.picPhoto.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.picPhoto.Location = new System.Drawing.Point(12, 21);
		this.picPhoto.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
		this.picPhoto.Name = "picPhoto";
		this.picPhoto.Size = new System.Drawing.Size(200, 200);
		this.picPhoto.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.picPhoto.TabIndex = 1;
		this.picPhoto.TabStop = false;
		this.photoButtonRow.AutoSize = true;
		this.photoButtonRow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.photoButtonRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
		this.photoButtonRow.Location = new System.Drawing.Point(12, 229);
		this.photoButtonRow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
		this.photoButtonRow.Name = "photoButtonRow";
		this.photoButtonRow.Size = new System.Drawing.Size(188, 32);
		this.photoButtonRow.TabIndex = 2;
		this.photoButtonRow.WrapContents = false;
		this.photoButtonRow.Controls.Add(this.btnPhotoUpload);
		this.photoButtonRow.Controls.Add(this.btnPhotoRemove);
		this.btnPhotoUpload.AutoSize = true;
		this.btnPhotoUpload.Location = new System.Drawing.Point(0, 0);
		this.btnPhotoUpload.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
		this.btnPhotoUpload.Name = "btnPhotoUpload";
		this.btnPhotoUpload.Size = new System.Drawing.Size(90, 32);
		this.btnPhotoUpload.TabIndex = 0;
		this.btnPhotoUpload.Text = "Upload";
		this.btnPhotoUpload.UseVisualStyleBackColor = true;
		this.btnPhotoUpload.Click += new System.EventHandler(PhotoUpload_Click);
		this.btnPhotoRemove.AutoSize = true;
		this.btnPhotoRemove.Location = new System.Drawing.Point(98, 0);
		this.btnPhotoRemove.Margin = new System.Windows.Forms.Padding(0);
		this.btnPhotoRemove.Name = "btnPhotoRemove";
		this.btnPhotoRemove.Size = new System.Drawing.Size(90, 32);
		this.btnPhotoRemove.TabIndex = 1;
		this.btnPhotoRemove.Text = "Remove";
		this.btnPhotoRemove.UseVisualStyleBackColor = true;
		this.btnPhotoRemove.Click += new System.EventHandler(PhotoRemove_Click);
		this.buttonPanel.AutoSize = true;
		this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
		this.buttonPanel.Location = new System.Drawing.Point(16, 420);
		this.buttonPanel.Margin = new System.Windows.Forms.Padding(0);
		this.buttonPanel.Name = "buttonPanel";
		this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
		this.buttonPanel.Size = new System.Drawing.Size(206, 42);
		this.buttonPanel.TabIndex = 1;
		this.buttonPanel.WrapContents = false;
		this.buttonPanel.Controls.Add(this.btnSave);
		this.buttonPanel.Controls.Add(this.btnCancel);
		this.btnSave.AutoSize = true;
		this.btnSave.Location = new System.Drawing.Point(131, 10);
		this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
		this.btnSave.Name = "btnSave";
		this.btnSave.Size = new System.Drawing.Size(75, 32);
		this.btnSave.TabIndex = 0;
		this.btnSave.Text = "Save";
		this.btnSave.UseVisualStyleBackColor = true;
		this.btnSave.Click += new System.EventHandler(ValidateAndClose);
		this.btnCancel.AutoSize = true;
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.Location = new System.Drawing.Point(48, 10);
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
		this.AutoScroll = true;
		this.AutoSize = true;
		base.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(780, 520);
		base.Controls.Add(this.buttonPanel);
		base.Controls.Add(this.cardPanel);
		this.MinimumSize = new System.Drawing.Size(780, 0);
		base.Name = "ResidentForm";
		base.Padding = new System.Windows.Forms.Padding(16);
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Resident";
		this.cardPanel.ResumeLayout(false);
		this.cardPanel.PerformLayout();
		this.cardLayout.ResumeLayout(false);
		this.cardLayout.PerformLayout();
		this.bodyLayout.ResumeLayout(false);
		this.bodyLayout.PerformLayout();
		this.fieldsTable.ResumeLayout(false);
		this.fieldsTable.PerformLayout();
		this.photoPanel.ResumeLayout(false);
		this.photoPanel.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.picPhoto).EndInit();
		this.photoButtonRow.ResumeLayout(false);
		this.photoButtonRow.PerformLayout();
		this.buttonPanel.ResumeLayout(false);
		this.buttonPanel.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
