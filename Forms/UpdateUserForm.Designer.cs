namespace baranggaysystem1
{
    partial class UpdateUserForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelSubtitle;
        private System.Windows.Forms.Panel headerDivider;
        private System.Windows.Forms.Label labelUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label labelRole;
        private System.Windows.Forms.ComboBox cmbRole;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.Label labelFirstName;
        private System.Windows.Forms.TextBox txtFirstName;
        private System.Windows.Forms.Label labelMiddleName;
        private System.Windows.Forms.TextBox txtMiddleName;
        private System.Windows.Forms.Label labelLastName;
        private System.Windows.Forms.TextBox txtLastName;
        private System.Windows.Forms.Label labelEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label labelContact;
        private System.Windows.Forms.TextBox txtContact;
        private System.Windows.Forms.Label labelPosition;
        private System.Windows.Forms.TextBox txtPosition;
        private System.Windows.Forms.Label labelDepartment;
        private System.Windows.Forms.TextBox txtDepartment;
        private System.Windows.Forms.Label labelLastProject;
        private System.Windows.Forms.TextBox txtLastProject;
        private System.Windows.Forms.Label labelPhoto;
        private System.Windows.Forms.Panel photoPanel;
        private System.Windows.Forms.PictureBox photoPreview;
        private System.Windows.Forms.FlowLayoutPanel photoButtons;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.FlowLayoutPanel actionRow;
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
            rootLayout = new TableLayoutPanel();
            labelTitle = new Label();
            labelSubtitle = new Label();
            headerDivider = new Panel();
            labelUsername = new Label();
            txtUsername = new TextBox();
            labelFirstName = new Label();
            txtFirstName = new TextBox();
            labelMiddleName = new Label();
            txtMiddleName = new TextBox();
            labelLastName = new Label();
            txtLastName = new TextBox();
            labelEmail = new Label();
            txtEmail = new TextBox();
            labelContact = new Label();
            txtContact = new TextBox();
            labelRole = new Label();
            cmbRole = new ComboBox();
            labelPosition = new Label();
            txtPosition = new TextBox();
            labelDepartment = new Label();
            txtDepartment = new TextBox();
            labelLastProject = new Label();
            txtLastProject = new TextBox();
            labelStatus = new Label();
            chkActive = new CheckBox();
            labelPhoto = new Label();
            photoPanel = new Panel();
            photoPreview = new PictureBox();
            photoButtons = new FlowLayoutPanel();
            btnUpload = new Button();
            btnRemove = new Button();
            actionRow = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            rootLayout.SuspendLayout();
            photoPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)photoPreview).BeginInit();
            photoButtons.SuspendLayout();
            actionRow.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            rootLayout.Size = new Size(560, 760);
            rootLayout.TabIndex = 0;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Dock = DockStyle.Fill;
            labelTitle.Location = new Point(0, 0);
            labelTitle.Margin = new Padding(0, 0, 0, 8);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(336, 36);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "Update Staff Details";
            labelTitle.TextAlign = ContentAlignment.BottomLeft;
            // 
            // labelSubtitle
            // 
            labelSubtitle.AutoSize = true;
            labelSubtitle.Dock = DockStyle.Fill;
            labelSubtitle.Location = new Point(0, 44);
            labelSubtitle.Margin = new Padding(0, 0, 0, 10);
            labelSubtitle.Name = "labelSubtitle";
            labelSubtitle.Size = new Size(510, 20);
            labelSubtitle.TabIndex = 1;
            labelSubtitle.Text = "Edit account details and status";
            // 
            // headerDivider
            // 
            headerDivider.BackColor = Color.Gainsboro;
            headerDivider.Dock = DockStyle.Fill;
            headerDivider.Location = new Point(0, 74);
            headerDivider.Margin = new Padding(0);
            headerDivider.Name = "headerDivider";
            headerDivider.Size = new Size(510, 1);
            headerDivider.TabIndex = 2;
            // 
            // labelUsername
            // 
            labelUsername.AutoSize = true;
            labelUsername.Dock = DockStyle.Top;
            labelUsername.Location = new Point(0, 0);
            labelUsername.Margin = new Padding(0, 0, 0, 4);
            labelUsername.Name = "labelUsername";
            labelUsername.Size = new Size(75, 20);
            labelUsername.TabIndex = 0;
            labelUsername.Text = "Username";
            // 
            // txtUsername
            // 
            txtUsername.Dock = DockStyle.Top;
            txtUsername.Location = new Point(0, 24);
            txtUsername.Margin = new Padding(0);
            txtUsername.Name = "txtUsername";
            txtUsername.ReadOnly = true;
            txtUsername.Size = new Size(243, 27);
            txtUsername.TabIndex = 1;
            // 
            // labelFirstName
            // 
            labelFirstName.AutoSize = true;
            labelFirstName.Dock = DockStyle.Top;
            labelFirstName.Location = new Point(0, 0);
            labelFirstName.Margin = new Padding(0, 0, 0, 4);
            labelFirstName.Name = "labelFirstName";
            labelFirstName.Size = new Size(77, 20);
            labelFirstName.TabIndex = 0;
            labelFirstName.Text = "First name";
            // 
            // txtFirstName
            // 
            txtFirstName.Dock = DockStyle.Top;
            txtFirstName.Location = new Point(0, 24);
            txtFirstName.Margin = new Padding(0);
            txtFirstName.Name = "txtFirstName";
            txtFirstName.Size = new Size(243, 27);
            txtFirstName.TabIndex = 1;
            // 
            // labelMiddleName
            // 
            labelMiddleName.AutoSize = true;
            labelMiddleName.Dock = DockStyle.Top;
            labelMiddleName.Location = new Point(0, 0);
            labelMiddleName.Margin = new Padding(0, 0, 0, 4);
            labelMiddleName.Name = "labelMiddleName";
            labelMiddleName.Size = new Size(97, 20);
            labelMiddleName.TabIndex = 0;
            labelMiddleName.Text = "Middle name";
            // 
            // txtMiddleName
            // 
            txtMiddleName.Dock = DockStyle.Top;
            txtMiddleName.Location = new Point(0, 24);
            txtMiddleName.Margin = new Padding(0);
            txtMiddleName.Name = "txtMiddleName";
            txtMiddleName.Size = new Size(243, 27);
            txtMiddleName.TabIndex = 1;
            // 
            // labelLastName
            // 
            labelLastName.AutoSize = true;
            labelLastName.Dock = DockStyle.Top;
            labelLastName.Location = new Point(0, 0);
            labelLastName.Margin = new Padding(0, 0, 0, 4);
            labelLastName.Name = "labelLastName";
            labelLastName.Size = new Size(76, 20);
            labelLastName.TabIndex = 0;
            labelLastName.Text = "Last name";
            // 
            // txtLastName
            // 
            txtLastName.Dock = DockStyle.Top;
            txtLastName.Location = new Point(0, 24);
            txtLastName.Margin = new Padding(0);
            txtLastName.Name = "txtLastName";
            txtLastName.Size = new Size(243, 27);
            txtLastName.TabIndex = 1;
            // 
            // labelEmail
            // 
            labelEmail.AutoSize = true;
            labelEmail.Dock = DockStyle.Top;
            labelEmail.Location = new Point(0, 0);
            labelEmail.Margin = new Padding(0, 0, 0, 4);
            labelEmail.Name = "labelEmail";
            labelEmail.Size = new Size(46, 20);
            labelEmail.TabIndex = 0;
            labelEmail.Text = "Email";
            // 
            // txtEmail
            // 
            txtEmail.Dock = DockStyle.Top;
            txtEmail.Location = new Point(0, 24);
            txtEmail.Margin = new Padding(0);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(243, 27);
            txtEmail.TabIndex = 1;
            // 
            // labelContact
            // 
            labelContact.AutoSize = true;
            labelContact.Dock = DockStyle.Top;
            labelContact.Location = new Point(0, 0);
            labelContact.Margin = new Padding(0, 0, 0, 4);
            labelContact.Name = "labelContact";
            labelContact.Size = new Size(84, 20);
            labelContact.TabIndex = 0;
            labelContact.Text = "Contact no.";
            // 
            // txtContact
            // 
            txtContact.Dock = DockStyle.Top;
            txtContact.Location = new Point(0, 24);
            txtContact.Margin = new Padding(0);
            txtContact.Name = "txtContact";
            txtContact.Size = new Size(243, 27);
            txtContact.TabIndex = 1;
            // 
            // labelRole
            // 
            labelRole.AutoSize = true;
            labelRole.Dock = DockStyle.Top;
            labelRole.Location = new Point(0, 0);
            labelRole.Margin = new Padding(0, 0, 0, 4);
            labelRole.Name = "labelRole";
            labelRole.Size = new Size(39, 20);
            labelRole.TabIndex = 0;
            labelRole.Text = "Role";
            // 
            // cmbRole
            // 
            cmbRole.Dock = DockStyle.Top;
            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.FormattingEnabled = true;
            cmbRole.Items.AddRange(new object[] { "Super Admin", "Admin", "Staff" });
            cmbRole.Location = new Point(0, 24);
            cmbRole.Margin = new Padding(0);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new Size(243, 28);
            cmbRole.TabIndex = 1;
            // 
            // labelPosition
            // 
            labelPosition.AutoSize = true;
            labelPosition.Dock = DockStyle.Top;
            labelPosition.Location = new Point(0, 0);
            labelPosition.Margin = new Padding(0, 0, 0, 4);
            labelPosition.Name = "labelPosition";
            labelPosition.Size = new Size(61, 20);
            labelPosition.TabIndex = 0;
            labelPosition.Text = "Position";
            // 
            // txtPosition
            // 
            txtPosition.Dock = DockStyle.Top;
            txtPosition.Location = new Point(0, 24);
            txtPosition.Margin = new Padding(0);
            txtPosition.Name = "txtPosition";
            txtPosition.Size = new Size(243, 27);
            txtPosition.TabIndex = 1;
            // 
            // labelDepartment
            // 
            labelDepartment.AutoSize = true;
            labelDepartment.Dock = DockStyle.Top;
            labelDepartment.Location = new Point(0, 0);
            labelDepartment.Margin = new Padding(0, 0, 0, 4);
            labelDepartment.Name = "labelDepartment";
            labelDepartment.Size = new Size(89, 20);
            labelDepartment.TabIndex = 0;
            labelDepartment.Text = "Department";
            // 
            // txtDepartment
            // 
            txtDepartment.Dock = DockStyle.Top;
            txtDepartment.Location = new Point(0, 24);
            txtDepartment.Margin = new Padding(0);
            txtDepartment.Name = "txtDepartment";
            txtDepartment.Size = new Size(243, 27);
            txtDepartment.TabIndex = 1;
            // 
            // labelLastProject
            // 
            labelLastProject.AutoSize = true;
            labelLastProject.Dock = DockStyle.Top;
            labelLastProject.Location = new Point(0, 0);
            labelLastProject.Margin = new Padding(0, 0, 0, 4);
            labelLastProject.Name = "labelLastProject";
            labelLastProject.Size = new Size(86, 20);
            labelLastProject.TabIndex = 0;
            labelLastProject.Text = "Last project";
            // 
            // txtLastProject
            // 
            txtLastProject.Dock = DockStyle.Top;
            txtLastProject.Location = new Point(0, 24);
            txtLastProject.Margin = new Padding(0);
            txtLastProject.Name = "txtLastProject";
            txtLastProject.Size = new Size(243, 27);
            txtLastProject.TabIndex = 1;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Dock = DockStyle.Top;
            labelStatus.Location = new Point(0, 0);
            labelStatus.Margin = new Padding(0, 0, 0, 4);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(49, 20);
            labelStatus.TabIndex = 0;
            labelStatus.Text = "Status";
            // 
            // chkActive
            // 
            chkActive.AutoSize = true;
            chkActive.Dock = DockStyle.Top;
            chkActive.Location = new Point(0, 24);
            chkActive.Margin = new Padding(0);
            chkActive.Name = "chkActive";
            chkActive.Size = new Size(243, 24);
            chkActive.TabIndex = 1;
            chkActive.Text = "Active";
            chkActive.UseVisualStyleBackColor = true;
            // 
            // labelPhoto
            // 
            labelPhoto.AutoSize = true;
            labelPhoto.Dock = DockStyle.Top;
            labelPhoto.Location = new Point(0, 0);
            labelPhoto.Margin = new Padding(0, 0, 0, 8);
            labelPhoto.Name = "labelPhoto";
            labelPhoto.Size = new Size(48, 20);
            labelPhoto.TabIndex = 0;
            labelPhoto.Text = "Photo";
            // 
            // photoPanel
            // 
            photoPanel.BackColor = Color.White;
            photoPanel.BorderStyle = BorderStyle.FixedSingle;
            photoPanel.Controls.Add(photoPreview);
            photoPanel.Controls.Add(photoButtons);
            photoPanel.Controls.Add(labelPhoto);
            photoPanel.Dock = DockStyle.Top;
            photoPanel.Location = new Point(346, 0);
            photoPanel.Margin = new Padding(12, 0, 0, 0);
            photoPanel.Name = "photoPanel";
            photoPanel.Padding = new Padding(12);
            photoPanel.Size = new Size(164, 260);
            photoPanel.TabIndex = 1;
            // 
            // photoPreview
            // 
            photoPreview.BorderStyle = BorderStyle.FixedSingle;
            photoPreview.Dock = DockStyle.Top;
            photoPreview.Location = new Point(12, 40);
            photoPreview.Margin = new Padding(0);
            photoPreview.Name = "photoPreview";
            photoPreview.Size = new Size(138, 140);
            photoPreview.SizeMode = PictureBoxSizeMode.Zoom;
            photoPreview.TabIndex = 1;
            photoPreview.TabStop = false;
            // 
            // photoButtons
            // 
            photoButtons.AutoSize = true;
            photoButtons.Controls.Add(btnUpload);
            photoButtons.Controls.Add(btnRemove);
            photoButtons.Dock = DockStyle.Top;
            photoButtons.FlowDirection = FlowDirection.TopDown;
            photoButtons.Location = new Point(12, 180);
            photoButtons.Margin = new Padding(0, 10, 0, 0);
            photoButtons.Name = "photoButtons";
            photoButtons.Size = new Size(138, 72);
            photoButtons.TabIndex = 2;
            photoButtons.WrapContents = false;
            // 
            // btnUpload
            // 
            btnUpload.Location = new Point(0, 0);
            btnUpload.Margin = new Padding(0, 0, 0, 8);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new Size(120, 32);
            btnUpload.TabIndex = 0;
            btnUpload.Text = "Upload";
            btnUpload.UseVisualStyleBackColor = true;
            btnUpload.Click += btnUpload_Click;
            // 
            // btnRemove
            // 
            btnRemove.Location = new Point(0, 40);
            btnRemove.Margin = new Padding(0);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new Size(120, 32);
            btnRemove.TabIndex = 1;
            btnRemove.Text = "Remove";
            btnRemove.UseVisualStyleBackColor = true;
            btnRemove.Click += btnRemove_Click;
            // 
            // actionRow
            // 
            actionRow.AutoSize = true;
            actionRow.Controls.Add(btnSave);
            actionRow.Dock = DockStyle.Fill;
            actionRow.FlowDirection = FlowDirection.RightToLeft;
            actionRow.Location = new Point(255, 9);
            actionRow.Margin = new Padding(0);
            actionRow.Name = "actionRow";
            actionRow.Size = new Size(255, 47);
            actionRow.TabIndex = 1;
            actionRow.WrapContents = false;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(135, 0);
            btnSave.Margin = new Padding(0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(120, 36);
            btnSave.TabIndex = 0;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Left;
            btnCancel.Location = new Point(0, 10);
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(120, 36);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // UpdateUserForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(560, 760);
            Controls.Add(rootLayout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateUserForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Update Staff";
            Load += UpdateUserForm_Load;

            var panelHeader = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 12),
                BackColor = Color.White
            };

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));

            var labelHeaderMeta = new Label
            {
                Name = "labelHeaderMeta",
                AutoSize = true,
                Text = "Staff Account",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 8, 0, 0)
            };

            panelHeader.Controls.Add(headerLayout);
            headerLayout.Controls.Add(labelTitle, 0, 0);
            headerLayout.Controls.Add(labelHeaderMeta, 1, 0);
            headerLayout.Controls.Add(labelSubtitle, 0, 1);
            headerLayout.Controls.Add(headerDivider, 0, 2);
            headerLayout.SetColumnSpan(labelSubtitle, 2);
            headerLayout.SetColumnSpan(headerDivider, 2);

            var panelContentHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 12, 24, 8),
                BackColor = Color.White
            };

            var contentScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            panelContentHost.Controls.Add(contentScrollPanel);

            var tableContent = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty
            };
            tableContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176F));
            tableContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentScrollPanel.Controls.Add(tableContent);

            var fieldsCard = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12),
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            tableContent.Controls.Add(fieldsCard, 0, 0);
            tableContent.Controls.Add(photoPanel, 1, 0);

            var tableFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 6,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            tableFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 6; i++)
            {
                tableFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            fieldsCard.Controls.Add(tableFields);

            Panel BuildFieldGroup(Label label, Control input, Padding margin)
            {
                var group = new Panel
                {
                    Dock = DockStyle.Fill,
                    Height = 56,
                    Margin = margin
                };
                label.Dock = DockStyle.Top;
                label.Margin = new Padding(0, 0, 0, 4);
                input.Dock = DockStyle.Top;
                input.Margin = Padding.Empty;
                group.Controls.Add(input);
                group.Controls.Add(label);
                return group;
            }

            tableFields.Controls.Add(BuildFieldGroup(labelUsername, txtUsername, new Padding(0, 0, 8, 8)), 0, 0);
            tableFields.Controls.Add(BuildFieldGroup(labelRole, cmbRole, new Padding(8, 0, 0, 8)), 1, 0);
            tableFields.Controls.Add(BuildFieldGroup(labelFirstName, txtFirstName, new Padding(0, 0, 8, 8)), 0, 1);
            tableFields.Controls.Add(BuildFieldGroup(labelMiddleName, txtMiddleName, new Padding(8, 0, 0, 8)), 1, 1);
            tableFields.Controls.Add(BuildFieldGroup(labelLastName, txtLastName, new Padding(0, 0, 8, 8)), 0, 2);
            tableFields.Controls.Add(BuildFieldGroup(labelEmail, txtEmail, new Padding(8, 0, 0, 8)), 1, 2);
            tableFields.Controls.Add(BuildFieldGroup(labelContact, txtContact, new Padding(0, 0, 8, 8)), 0, 3);
            tableFields.Controls.Add(BuildFieldGroup(labelPosition, txtPosition, new Padding(8, 0, 0, 8)), 1, 3);
            tableFields.Controls.Add(BuildFieldGroup(labelDepartment, txtDepartment, new Padding(0, 0, 8, 8)), 0, 4);
            tableFields.Controls.Add(BuildFieldGroup(labelLastProject, txtLastProject, new Padding(8, 0, 0, 8)), 1, 4);
            tableFields.Controls.Add(BuildFieldGroup(labelStatus, chkActive, new Padding(0, 0, 8, 0)), 0, 5);
            tableFields.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) }, 1, 5);

            var panelFooter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 8, 24, 16),
                BackColor = Color.White
            };

            var footerDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.Gainsboro
            };
            panelFooter.Controls.Add(footerDivider);
            footerDivider.BringToFront();

            var footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            footerLayout.Controls.Add(btnCancel, 0, 0);
            footerLayout.Controls.Add(actionRow, 1, 0);
            panelFooter.Controls.Add(footerLayout);

            rootLayout.Controls.Add(panelHeader, 0, 0);
            rootLayout.Controls.Add(panelContentHost, 0, 1);
            rootLayout.Controls.Add(panelFooter, 0, 2);
            AcceptButton = btnSave;

            rootLayout.ResumeLayout(false);
            photoPanel.ResumeLayout(false);
            photoPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)photoPreview).EndInit();
            photoButtons.ResumeLayout(false);
            actionRow.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
