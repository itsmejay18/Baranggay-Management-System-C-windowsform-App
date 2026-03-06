namespace baranggaysystem1
{
    partial class ResidentForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel cardPanel;
        private System.Windows.Forms.TableLayoutPanel cardLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblSubHeader;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.TableLayoutPanel fieldsTable;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.Label lblMiddleName;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.Label lblGender;
        private System.Windows.Forms.Label lblBirthDate;
        private System.Windows.Forms.Label lblCivilStatus;
        private System.Windows.Forms.Label lblContact;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.TextBox txtMiddleName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.ComboBox cmbGender;
        private System.Windows.Forms.DateTimePicker dtpBirthDate;
        private System.Windows.Forms.ComboBox cmbCivilStatus;
        private System.Windows.Forms.TextBox txtContact;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Panel rightColumnPanel;
        private System.Windows.Forms.FlowLayoutPanel photoPanel;
        private System.Windows.Forms.Label lblPhotoCaption;
        private System.Windows.Forms.PictureBox picPhoto;
        private System.Windows.Forms.FlowLayoutPanel photoButtonRow;
        private System.Windows.Forms.Button btnPhotoUpload;
        private System.Windows.Forms.Button btnPhotoRemove;
        private System.Windows.Forms.Panel historyPanel;
        private System.Windows.Forms.Label lblHistoryTitle;
        private System.Windows.Forms.Label lblHistoryCases;
        private System.Windows.Forms.Label lblHistoryActive;
        private System.Windows.Forms.Label lblHistoryCertificates;
        private System.Windows.Forms.Label lblHistoryPending;
        private System.Windows.Forms.Label lblHistoryLastAction;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            cardPanel = new System.Windows.Forms.Panel();
            cardLayout = new System.Windows.Forms.TableLayoutPanel();
            lblHeader = new System.Windows.Forms.Label();
            lblSubHeader = new System.Windows.Forms.Label();
            bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            fieldsTable = new System.Windows.Forms.TableLayoutPanel();
            lblFirstName = new System.Windows.Forms.Label();
            lblMiddleName = new System.Windows.Forms.Label();
            lblLastName = new System.Windows.Forms.Label();
            lblGender = new System.Windows.Forms.Label();
            lblBirthDate = new System.Windows.Forms.Label();
            lblCivilStatus = new System.Windows.Forms.Label();
            lblContact = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            txtFirstName = new System.Windows.Forms.TextBox();
            txtMiddleName = new System.Windows.Forms.TextBox();
            txtLastName = new System.Windows.Forms.TextBox();
            cmbGender = new System.Windows.Forms.ComboBox();
            dtpBirthDate = new System.Windows.Forms.DateTimePicker();
            cmbCivilStatus = new System.Windows.Forms.ComboBox();
            txtContact = new System.Windows.Forms.TextBox();
            cmbStatus = new System.Windows.Forms.ComboBox();
            rightColumnPanel = new System.Windows.Forms.Panel();
            photoPanel = new System.Windows.Forms.FlowLayoutPanel();
            lblPhotoCaption = new System.Windows.Forms.Label();
            picPhoto = new System.Windows.Forms.PictureBox();
            photoButtonRow = new System.Windows.Forms.FlowLayoutPanel();
            btnPhotoUpload = new System.Windows.Forms.Button();
            btnPhotoRemove = new System.Windows.Forms.Button();
            historyPanel = new System.Windows.Forms.Panel();
            lblHistoryTitle = new System.Windows.Forms.Label();
            lblHistoryCases = new System.Windows.Forms.Label();
            lblHistoryActive = new System.Windows.Forms.Label();
            lblHistoryCertificates = new System.Windows.Forms.Label();
            lblHistoryPending = new System.Windows.Forms.Label();
            lblHistoryLastAction = new System.Windows.Forms.Label();
            buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            btnSave = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            cardPanel.SuspendLayout();
            cardLayout.SuspendLayout();
            bodyLayout.SuspendLayout();
            fieldsTable.SuspendLayout();
            rightColumnPanel.SuspendLayout();
            photoPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPhoto).BeginInit();
            photoButtonRow.SuspendLayout();
            historyPanel.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // cardPanel
            // 
            cardPanel.BackColor = System.Drawing.Color.White;
            cardPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            cardPanel.Controls.Add(cardLayout);
            cardPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            cardPanel.Location = new System.Drawing.Point(16, 16);
            cardPanel.Name = "cardPanel";
            cardPanel.Padding = new System.Windows.Forms.Padding(14);
            cardPanel.Size = new System.Drawing.Size(952, 588);
            cardPanel.TabIndex = 0;
            // 
            // cardLayout
            // 
            cardLayout.ColumnCount = 1;
            cardLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            cardLayout.Controls.Add(lblHeader, 0, 0);
            cardLayout.Controls.Add(lblSubHeader, 0, 1);
            cardLayout.Controls.Add(bodyLayout, 0, 2);
            cardLayout.Controls.Add(buttonPanel, 0, 3);
            cardLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            cardLayout.Location = new System.Drawing.Point(14, 14);
            cardLayout.Name = "cardLayout";
            cardLayout.RowCount = 4;
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            cardLayout.Size = new System.Drawing.Size(922, 558);
            cardLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            lblHeader.AutoSize = true;
            lblHeader.Dock = System.Windows.Forms.DockStyle.Left;
            lblHeader.Font = new System.Drawing.Font("Century Gothic", 17F, System.Drawing.FontStyle.Bold);
            lblHeader.Location = new System.Drawing.Point(0, 0);
            lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            lblHeader.Name = "lblHeader";
            lblHeader.Size = new System.Drawing.Size(186, 27);
            lblHeader.TabIndex = 0;
            lblHeader.Text = "Profile Information";
            lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSubHeader
            // 
            lblSubHeader.AutoSize = true;
            lblSubHeader.Dock = System.Windows.Forms.DockStyle.Left;
            lblSubHeader.Font = new System.Drawing.Font("Trebuchet MS", 9.5F);
            lblSubHeader.ForeColor = System.Drawing.Color.DimGray;
            lblSubHeader.Location = new System.Drawing.Point(0, 42);
            lblSubHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            lblSubHeader.Name = "lblSubHeader";
            lblSubHeader.Size = new System.Drawing.Size(308, 18);
            lblSubHeader.TabIndex = 1;
            lblSubHeader.Text = "Fill out resident profile details and upload a photo.";
            // 
            // bodyLayout
            // 
            bodyLayout.ColumnCount = 2;
            bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 68F));
            bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32F));
            bodyLayout.Controls.Add(fieldsTable, 0, 0);
            bodyLayout.Controls.Add(rightColumnPanel, 1, 0);
            bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            bodyLayout.Location = new System.Drawing.Point(0, 72);
            bodyLayout.Margin = new System.Windows.Forms.Padding(0);
            bodyLayout.Name = "bodyLayout";
            bodyLayout.RowCount = 1;
            bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            bodyLayout.Size = new System.Drawing.Size(922, 430);
            bodyLayout.TabIndex = 2;
            // 
            // fieldsTable
            // 
            fieldsTable.ColumnCount = 2;
            fieldsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            fieldsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            fieldsTable.Controls.Add(lblFirstName, 0, 0);
            fieldsTable.Controls.Add(txtFirstName, 1, 0);
            fieldsTable.Controls.Add(lblMiddleName, 0, 1);
            fieldsTable.Controls.Add(txtMiddleName, 1, 1);
            fieldsTable.Controls.Add(lblLastName, 0, 2);
            fieldsTable.Controls.Add(txtLastName, 1, 2);
            fieldsTable.Controls.Add(lblGender, 0, 3);
            fieldsTable.Controls.Add(cmbGender, 1, 3);
            fieldsTable.Controls.Add(lblBirthDate, 0, 4);
            fieldsTable.Controls.Add(dtpBirthDate, 1, 4);
            fieldsTable.Controls.Add(lblCivilStatus, 0, 5);
            fieldsTable.Controls.Add(cmbCivilStatus, 1, 5);
            fieldsTable.Controls.Add(lblContact, 0, 6);
            fieldsTable.Controls.Add(txtContact, 1, 6);
            fieldsTable.Controls.Add(lblStatus, 0, 7);
            fieldsTable.Controls.Add(cmbStatus, 1, 7);
            fieldsTable.Dock = System.Windows.Forms.DockStyle.Top;
            fieldsTable.Location = new System.Drawing.Point(0, 0);
            fieldsTable.Margin = new System.Windows.Forms.Padding(0, 0, 16, 0);
            fieldsTable.Name = "fieldsTable";
            fieldsTable.RowCount = 8;
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            fieldsTable.Size = new System.Drawing.Size(610, 368);
            fieldsTable.TabIndex = 0;
            // 
            // lblFirstName
            // 
            lblFirstName.AutoSize = true;
            lblFirstName.Location = new System.Drawing.Point(3, 14);
            lblFirstName.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblFirstName.Name = "lblFirstName";
            lblFirstName.Size = new System.Drawing.Size(66, 15);
            lblFirstName.TabIndex = 0;
            lblFirstName.Text = "First name";
            // 
            // lblMiddleName
            // 
            lblMiddleName.AutoSize = true;
            lblMiddleName.Location = new System.Drawing.Point(3, 60);
            lblMiddleName.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblMiddleName.Name = "lblMiddleName";
            lblMiddleName.Size = new System.Drawing.Size(78, 15);
            lblMiddleName.TabIndex = 2;
            lblMiddleName.Text = "Middle name";
            // 
            // lblLastName
            // 
            lblLastName.AutoSize = true;
            lblLastName.Location = new System.Drawing.Point(3, 106);
            lblLastName.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblLastName.Name = "lblLastName";
            lblLastName.Size = new System.Drawing.Size(64, 15);
            lblLastName.TabIndex = 4;
            lblLastName.Text = "Last name";
            // 
            // lblGender
            // 
            lblGender.AutoSize = true;
            lblGender.Location = new System.Drawing.Point(3, 152);
            lblGender.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblGender.Name = "lblGender";
            lblGender.Size = new System.Drawing.Size(45, 15);
            lblGender.TabIndex = 6;
            lblGender.Text = "Gender";
            // 
            // lblBirthDate
            // 
            lblBirthDate.AutoSize = true;
            lblBirthDate.Location = new System.Drawing.Point(3, 198);
            lblBirthDate.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblBirthDate.Name = "lblBirthDate";
            lblBirthDate.Size = new System.Drawing.Size(58, 15);
            lblBirthDate.TabIndex = 8;
            lblBirthDate.Text = "Birth date";
            // 
            // lblCivilStatus
            // 
            lblCivilStatus.AutoSize = true;
            lblCivilStatus.Location = new System.Drawing.Point(3, 244);
            lblCivilStatus.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblCivilStatus.Name = "lblCivilStatus";
            lblCivilStatus.Size = new System.Drawing.Size(62, 15);
            lblCivilStatus.TabIndex = 10;
            lblCivilStatus.Text = "Civil status";
            // 
            // lblContact
            // 
            lblContact.AutoSize = true;
            lblContact.Location = new System.Drawing.Point(3, 290);
            lblContact.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblContact.Name = "lblContact";
            lblContact.Size = new System.Drawing.Size(69, 15);
            lblContact.TabIndex = 12;
            lblContact.Text = "Contact no.";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(3, 336);
            lblStatus.Margin = new System.Windows.Forms.Padding(3, 14, 3, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(39, 15);
            lblStatus.TabIndex = 14;
            lblStatus.Text = "Status";
            // 
            // txtFirstName
            // 
            txtFirstName.Dock = System.Windows.Forms.DockStyle.Fill;
            txtFirstName.Location = new System.Drawing.Point(153, 9);
            txtFirstName.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            txtFirstName.Name = "txtFirstName";
            txtFirstName.Size = new System.Drawing.Size(454, 23);
            txtFirstName.TabIndex = 1;
            // 
            // txtMiddleName
            // 
            txtMiddleName.Dock = System.Windows.Forms.DockStyle.Fill;
            txtMiddleName.Location = new System.Drawing.Point(153, 55);
            txtMiddleName.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            txtMiddleName.Name = "txtMiddleName";
            txtMiddleName.Size = new System.Drawing.Size(454, 23);
            txtMiddleName.TabIndex = 3;
            // 
            // txtLastName
            // 
            txtLastName.Dock = System.Windows.Forms.DockStyle.Fill;
            txtLastName.Location = new System.Drawing.Point(153, 101);
            txtLastName.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            txtLastName.Name = "txtLastName";
            txtLastName.Size = new System.Drawing.Size(454, 23);
            txtLastName.TabIndex = 5;
            // 
            // cmbGender
            // 
            cmbGender.Dock = System.Windows.Forms.DockStyle.Fill;
            cmbGender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbGender.FormattingEnabled = true;
            cmbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
            cmbGender.Location = new System.Drawing.Point(153, 147);
            cmbGender.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            cmbGender.Name = "cmbGender";
            cmbGender.Size = new System.Drawing.Size(454, 23);
            cmbGender.TabIndex = 7;
            // 
            // dtpBirthDate
            // 
            dtpBirthDate.Dock = System.Windows.Forms.DockStyle.Fill;
            dtpBirthDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpBirthDate.Location = new System.Drawing.Point(153, 193);
            dtpBirthDate.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            dtpBirthDate.Name = "dtpBirthDate";
            dtpBirthDate.Size = new System.Drawing.Size(454, 23);
            dtpBirthDate.TabIndex = 9;
            // 
            // cmbCivilStatus
            // 
            cmbCivilStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            cmbCivilStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbCivilStatus.FormattingEnabled = true;
            cmbCivilStatus.Items.AddRange(new object[] { "Single", "Married", "Widowed", "Separated" });
            cmbCivilStatus.Location = new System.Drawing.Point(153, 239);
            cmbCivilStatus.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            cmbCivilStatus.Name = "cmbCivilStatus";
            cmbCivilStatus.Size = new System.Drawing.Size(454, 23);
            cmbCivilStatus.TabIndex = 11;
            // 
            // txtContact
            // 
            txtContact.Dock = System.Windows.Forms.DockStyle.Fill;
            txtContact.Location = new System.Drawing.Point(153, 285);
            txtContact.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            txtContact.Name = "txtContact";
            txtContact.Size = new System.Drawing.Size(454, 23);
            txtContact.TabIndex = 13;
            // 
            // cmbStatus
            // 
            cmbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Items.AddRange(new object[] { "Active", "Inactive", "Deceased" });
            cmbStatus.Location = new System.Drawing.Point(153, 331);
            cmbStatus.Margin = new System.Windows.Forms.Padding(3, 9, 3, 9);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new System.Drawing.Size(454, 23);
            cmbStatus.TabIndex = 15;
            // 
            // rightColumnPanel
            // 
            rightColumnPanel.Controls.Add(historyPanel);
            rightColumnPanel.Controls.Add(photoPanel);
            rightColumnPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            rightColumnPanel.Location = new System.Drawing.Point(626, 0);
            rightColumnPanel.Margin = new System.Windows.Forms.Padding(0);
            rightColumnPanel.Name = "rightColumnPanel";
            rightColumnPanel.Size = new System.Drawing.Size(296, 430);
            rightColumnPanel.TabIndex = 1;
            // 
            // photoPanel
            // 
            photoPanel.Controls.Add(lblPhotoCaption);
            photoPanel.Controls.Add(picPhoto);
            photoPanel.Controls.Add(photoButtonRow);
            photoPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            photoPanel.Location = new System.Drawing.Point(16, 0);
            photoPanel.Margin = new System.Windows.Forms.Padding(0);
            photoPanel.Name = "photoPanel";
            photoPanel.Size = new System.Drawing.Size(264, 266);
            photoPanel.TabIndex = 0;
            photoPanel.WrapContents = false;
            // 
            // lblPhotoCaption
            // 
            lblPhotoCaption.AutoSize = true;
            lblPhotoCaption.Location = new System.Drawing.Point(0, 0);
            lblPhotoCaption.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            lblPhotoCaption.Name = "lblPhotoCaption";
            lblPhotoCaption.Size = new System.Drawing.Size(39, 15);
            lblPhotoCaption.TabIndex = 0;
            lblPhotoCaption.Text = "Photo";
            // 
            // picPhoto
            // 
            picPhoto.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
            picPhoto.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picPhoto.Location = new System.Drawing.Point(0, 23);
            picPhoto.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            picPhoto.Name = "picPhoto";
            picPhoto.Size = new System.Drawing.Size(236, 180);
            picPhoto.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            picPhoto.TabIndex = 1;
            picPhoto.TabStop = false;
            // 
            // photoButtonRow
            // 
            photoButtonRow.Controls.Add(btnPhotoUpload);
            photoButtonRow.Controls.Add(btnPhotoRemove);
            photoButtonRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            photoButtonRow.Location = new System.Drawing.Point(0, 213);
            photoButtonRow.Margin = new System.Windows.Forms.Padding(0);
            photoButtonRow.Name = "photoButtonRow";
            photoButtonRow.Size = new System.Drawing.Size(236, 38);
            photoButtonRow.TabIndex = 2;
            photoButtonRow.WrapContents = false;
            // 
            // btnPhotoUpload
            // 
            btnPhotoUpload.Location = new System.Drawing.Point(0, 0);
            btnPhotoUpload.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnPhotoUpload.Name = "btnPhotoUpload";
            btnPhotoUpload.Size = new System.Drawing.Size(114, 36);
            btnPhotoUpload.TabIndex = 0;
            btnPhotoUpload.Text = "Upload";
            btnPhotoUpload.UseVisualStyleBackColor = true;
            btnPhotoUpload.Click += PhotoUpload_Click;
            // 
            // btnPhotoRemove
            // 
            btnPhotoRemove.Location = new System.Drawing.Point(122, 0);
            btnPhotoRemove.Margin = new System.Windows.Forms.Padding(0);
            btnPhotoRemove.Name = "btnPhotoRemove";
            btnPhotoRemove.Size = new System.Drawing.Size(114, 36);
            btnPhotoRemove.TabIndex = 1;
            btnPhotoRemove.Text = "Remove";
            btnPhotoRemove.UseVisualStyleBackColor = true;
            btnPhotoRemove.Click += PhotoRemove_Click;
            // 
            // historyPanel
            // 
            historyPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            historyPanel.Controls.Add(lblHistoryTitle);
            historyPanel.Controls.Add(lblHistoryCases);
            historyPanel.Controls.Add(lblHistoryActive);
            historyPanel.Controls.Add(lblHistoryCertificates);
            historyPanel.Controls.Add(lblHistoryPending);
            historyPanel.Controls.Add(lblHistoryLastAction);
            historyPanel.Location = new System.Drawing.Point(16, 282);
            historyPanel.Name = "historyPanel";
            historyPanel.Size = new System.Drawing.Size(236, 132);
            historyPanel.TabIndex = 1;
            // 
            // lblHistoryTitle
            // 
            lblHistoryTitle.AutoSize = true;
            lblHistoryTitle.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            lblHistoryTitle.Location = new System.Drawing.Point(8, 8);
            lblHistoryTitle.Name = "lblHistoryTitle";
            lblHistoryTitle.Size = new System.Drawing.Size(112, 19);
            lblHistoryTitle.TabIndex = 0;
            lblHistoryTitle.Text = "Resident History";
            // 
            // lblHistoryCases
            // 
            lblHistoryCases.AutoSize = true;
            lblHistoryCases.Location = new System.Drawing.Point(8, 34);
            lblHistoryCases.Name = "lblHistoryCases";
            lblHistoryCases.Size = new System.Drawing.Size(87, 15);
            lblHistoryCases.TabIndex = 1;
            lblHistoryCases.Text = "Blotter cases: 0";
            // 
            // lblHistoryActive
            // 
            lblHistoryActive.AutoSize = true;
            lblHistoryActive.Location = new System.Drawing.Point(8, 54);
            lblHistoryActive.Name = "lblHistoryActive";
            lblHistoryActive.Size = new System.Drawing.Size(79, 15);
            lblHistoryActive.TabIndex = 2;
            lblHistoryActive.Text = "Active cases: 0";
            // 
            // lblHistoryCertificates
            // 
            lblHistoryCertificates.AutoSize = true;
            lblHistoryCertificates.Location = new System.Drawing.Point(8, 74);
            lblHistoryCertificates.Name = "lblHistoryCertificates";
            lblHistoryCertificates.Size = new System.Drawing.Size(71, 15);
            lblHistoryCertificates.TabIndex = 3;
            lblHistoryCertificates.Text = "Certificates: 0";
            // 
            // lblHistoryPending
            // 
            lblHistoryPending.AutoSize = true;
            lblHistoryPending.Location = new System.Drawing.Point(8, 94);
            lblHistoryPending.Name = "lblHistoryPending";
            lblHistoryPending.Size = new System.Drawing.Size(83, 15);
            lblHistoryPending.TabIndex = 4;
            lblHistoryPending.Text = "Pending certs: 0";
            // 
            // lblHistoryLastAction
            // 
            lblHistoryLastAction.AutoSize = true;
            lblHistoryLastAction.Location = new System.Drawing.Point(8, 112);
            lblHistoryLastAction.Name = "lblHistoryLastAction";
            lblHistoryLastAction.Size = new System.Drawing.Size(121, 15);
            lblHistoryLastAction.TabIndex = 5;
            lblHistoryLastAction.Text = "Last action: select item";
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            buttonPanel.Location = new System.Drawing.Point(0, 502);
            buttonPanel.Margin = new System.Windows.Forms.Padding(0);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Padding = new System.Windows.Forms.Padding(0, 12, 0, 0);
            buttonPanel.Size = new System.Drawing.Size(922, 56);
            buttonPanel.TabIndex = 3;
            buttonPanel.WrapContents = false;
            // 
            // btnSave
            // 
            btnSave.Location = new System.Drawing.Point(0, 12);
            btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(140, 36);
            btnSave.TabIndex = 0;
            btnSave.Text = "Save Resident";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += ValidateAndClose;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(150, 12);
            btnCancel.Margin = new System.Windows.Forms.Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(120, 36);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ResidentForm
            // 
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(241, 244, 250);
            ClientSize = new System.Drawing.Size(984, 620);
            Controls.Add(cardPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ResidentForm";
            Padding = new System.Windows.Forms.Padding(16);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Resident";
            cardPanel.ResumeLayout(false);
            cardLayout.ResumeLayout(false);
            cardLayout.PerformLayout();
            bodyLayout.ResumeLayout(false);
            fieldsTable.ResumeLayout(false);
            fieldsTable.PerformLayout();
            rightColumnPanel.ResumeLayout(false);
            photoPanel.ResumeLayout(false);
            photoPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPhoto).EndInit();
            photoButtonRow.ResumeLayout(false);
            historyPanel.ResumeLayout(false);
            historyPanel.PerformLayout();
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
