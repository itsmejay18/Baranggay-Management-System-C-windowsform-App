namespace baranggaysystem1
{
    partial class RegisterForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.FlowLayoutPanel heroStack;
        private System.Windows.Forms.Label labelBrand;
        private System.Windows.Forms.Panel accentPanel;
        private System.Windows.Forms.Label labelTagline;
        private System.Windows.Forms.Panel panelCard;
        private System.Windows.Forms.TableLayoutPanel cardLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.FlowLayoutPanel loginRow;
        private System.Windows.Forms.Label labelSubtitle;
        private System.Windows.Forms.Label labelRole;
        private System.Windows.Forms.Label labelPhoto;
        private System.Windows.Forms.Panel photoPanel;
        private System.Windows.Forms.PictureBox photoPreview;
        private System.Windows.Forms.FlowLayoutPanel photoButtons;
        private System.Windows.Forms.Button buttonPhotoUpload;
        private System.Windows.Forms.Button buttonPhotoRemove;

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.ComboBox cmbRole;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label4;

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
            rootLayout = new System.Windows.Forms.TableLayoutPanel();
            panelLeft = new System.Windows.Forms.Panel();
            heroStack = new System.Windows.Forms.FlowLayoutPanel();
            labelBrand = new System.Windows.Forms.Label();
            accentPanel = new System.Windows.Forms.Panel();
            labelTagline = new System.Windows.Forms.Label();
            panelRight = new System.Windows.Forms.Panel();
            centerLayout = new System.Windows.Forms.TableLayoutPanel();
            panelCard = new System.Windows.Forms.Panel();
            cardLayout = new System.Windows.Forms.TableLayoutPanel();
            label4 = new System.Windows.Forms.Label();
            labelSubtitle = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            txtUsername = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            txtPassword = new System.Windows.Forms.TextBox();
            labelRole = new System.Windows.Forms.Label();
            cmbRole = new System.Windows.Forms.ComboBox();
            labelPhoto = new System.Windows.Forms.Label();
            photoPanel = new System.Windows.Forms.Panel();
            photoPreview = new System.Windows.Forms.PictureBox();
            photoButtons = new System.Windows.Forms.FlowLayoutPanel();
            buttonPhotoUpload = new System.Windows.Forms.Button();
            buttonPhotoRemove = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            loginRow = new System.Windows.Forms.FlowLayoutPanel();
            label3 = new System.Windows.Forms.Label();
            button2 = new System.Windows.Forms.Button();
            rootLayout.SuspendLayout();
            panelLeft.SuspendLayout();
            heroStack.SuspendLayout();
            panelRight.SuspendLayout();
            centerLayout.SuspendLayout();
            panelCard.SuspendLayout();
            cardLayout.SuspendLayout();
            loginRow.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 2;
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 360F));
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            rootLayout.Location = new System.Drawing.Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 1;
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            rootLayout.Size = new System.Drawing.Size(1040, 640);
            rootLayout.TabIndex = 0;
            rootLayout.Controls.Add(panelLeft, 0, 0);
            rootLayout.Controls.Add(panelRight, 1, 0);
            // 
            // panelLeft
            // 
            panelLeft.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);
            panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            panelLeft.Padding = new System.Windows.Forms.Padding(32, 72, 32, 32);
            panelLeft.Controls.Add(heroStack);
            panelLeft.Name = "panelLeft";
            panelLeft.TabIndex = 0;
            // 
            // heroStack
            // 
            heroStack.AutoSize = true;
            heroStack.BackColor = System.Drawing.Color.Transparent;
            heroStack.Dock = System.Windows.Forms.DockStyle.Top;
            heroStack.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            heroStack.Location = new System.Drawing.Point(32, 72);
            heroStack.Margin = new System.Windows.Forms.Padding(0);
            heroStack.Name = "heroStack";
            heroStack.Size = new System.Drawing.Size(296, 160);
            heroStack.TabIndex = 0;
            heroStack.WrapContents = false;
            heroStack.Controls.Add(labelBrand);
            heroStack.Controls.Add(accentPanel);
            heroStack.Controls.Add(labelTagline);
            // 
            // labelBrand
            // 
            labelBrand.AutoSize = true;
            labelBrand.ForeColor = System.Drawing.Color.White;
            labelBrand.Location = new System.Drawing.Point(0, 0);
            labelBrand.Margin = new System.Windows.Forms.Padding(0);
            labelBrand.Name = "labelBrand";
            labelBrand.Size = new System.Drawing.Size(114, 30);
            labelBrand.TabIndex = 0;
            labelBrand.Text = "Barangay\r\nSystem";
            // 
            // accentPanel
            // 
            accentPanel.BackColor = System.Drawing.Color.White;
            accentPanel.Location = new System.Drawing.Point(0, 48);
            accentPanel.Margin = new System.Windows.Forms.Padding(0, 18, 0, 8);
            accentPanel.Name = "accentPanel";
            accentPanel.Size = new System.Drawing.Size(72, 4);
            accentPanel.TabIndex = 1;
            // 
            // labelTagline
            // 
            labelTagline.AutoSize = true;
            labelTagline.ForeColor = System.Drawing.Color.Silver;
            labelTagline.Location = new System.Drawing.Point(0, 60);
            labelTagline.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            labelTagline.MaximumSize = new System.Drawing.Size(220, 0);
            labelTagline.Name = "labelTagline";
            labelTagline.Size = new System.Drawing.Size(220, 30);
            labelTagline.TabIndex = 2;
            labelTagline.Text = "Create staff or admin accounts";
            // 
            // panelRight
            // 
            panelRight.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
            panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            panelRight.Padding = new System.Windows.Forms.Padding(56);
            panelRight.Controls.Add(centerLayout);
            panelRight.Name = "panelRight";
            panelRight.TabIndex = 1;
            // 
            // centerLayout
            // 
            centerLayout.ColumnCount = 1;
            centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            centerLayout.Location = new System.Drawing.Point(56, 56);
            centerLayout.Name = "centerLayout";
            centerLayout.RowCount = 3;
            centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            centerLayout.Size = new System.Drawing.Size(568, 528);
            centerLayout.TabIndex = 0;
            centerLayout.Controls.Add(panelCard, 0, 1);
            // 
            // panelCard
            // 
            panelCard.AutoSize = true;
            panelCard.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panelCard.MinimumSize = new System.Drawing.Size(420, 360);
            panelCard.Name = "panelCard";
            panelCard.TabIndex = 0;
            panelCard.Controls.Add(cardLayout);
            // 
            // cardLayout
            // 
            cardLayout.AutoSize = true;
            cardLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            cardLayout.ColumnCount = 1;
            cardLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            cardLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            cardLayout.Location = new System.Drawing.Point(0, 0);
            cardLayout.Name = "cardLayout";
            cardLayout.RowCount = 12;
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            cardLayout.Size = new System.Drawing.Size(420, 360);
            cardLayout.TabIndex = 0;
            cardLayout.Controls.Add(label4, 0, 0);
            cardLayout.Controls.Add(labelSubtitle, 0, 1);
            cardLayout.Controls.Add(label1, 0, 2);
            cardLayout.Controls.Add(txtUsername, 0, 3);
            cardLayout.Controls.Add(label2, 0, 4);
            cardLayout.Controls.Add(txtPassword, 0, 5);
            cardLayout.Controls.Add(labelRole, 0, 6);
            cardLayout.Controls.Add(cmbRole, 0, 7);
            cardLayout.Controls.Add(labelPhoto, 0, 8);
            cardLayout.Controls.Add(photoPanel, 0, 9);
            cardLayout.Controls.Add(button1, 0, 10);
            cardLayout.Controls.Add(loginRow, 0, 11);
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(0, 0);
            label4.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(87, 15);
            label4.TabIndex = 0;
            label4.Text = "Create Account";
            // 
            // labelSubtitle
            // 
            labelSubtitle.AutoSize = true;
            labelSubtitle.Location = new System.Drawing.Point(0, 19);
            labelSubtitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);
            labelSubtitle.Name = "labelSubtitle";
            labelSubtitle.Size = new System.Drawing.Size(192, 15);
            labelSubtitle.TabIndex = 1;
            labelSubtitle.Text = "Add a new user and assign a role";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(0, 52);
            label1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(60, 15);
            label1.TabIndex = 2;
            label1.Text = "Username";
            // 
            // txtUsername
            // 
            txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            txtUsername.Location = new System.Drawing.Point(0, 71);
            txtUsername.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new System.Drawing.Size(420, 23);
            txtUsername.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(0, 106);
            label2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(57, 15);
            label2.TabIndex = 4;
            label2.Text = "Password";
            // 
            // txtPassword
            // 
            txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            txtPassword.Location = new System.Drawing.Point(0, 125);
            txtPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new System.Drawing.Size(420, 23);
            txtPassword.TabIndex = 5;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // labelRole
            // 
            labelRole.AutoSize = true;
            labelRole.Location = new System.Drawing.Point(0, 160);
            labelRole.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            labelRole.Name = "labelRole";
            labelRole.Size = new System.Drawing.Size(30, 15);
            labelRole.TabIndex = 6;
            labelRole.Text = "Role";
            // 
            // cmbRole
            // 
            cmbRole.Dock = System.Windows.Forms.DockStyle.Fill;
            cmbRole.FormattingEnabled = true;
            cmbRole.Items.AddRange(new object[] { "Super Admin", "Admin", "Staff" });
            cmbRole.Location = new System.Drawing.Point(0, 179);
            cmbRole.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new System.Drawing.Size(420, 23);
            cmbRole.TabIndex = 7;
            // 
            // labelPhoto
            // 
            labelPhoto.AutoSize = true;
            labelPhoto.Location = new System.Drawing.Point(0, 218);
            labelPhoto.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            labelPhoto.Name = "labelPhoto";
            labelPhoto.Size = new System.Drawing.Size(41, 15);
            labelPhoto.TabIndex = 8;
            labelPhoto.Text = "Photo";
            // 
            // photoPanel
            // 
            photoPanel.Controls.Add(photoPreview);
            photoPanel.Controls.Add(photoButtons);
            photoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            photoPanel.Location = new System.Drawing.Point(0, 239);
            photoPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            photoPanel.Name = "photoPanel";
            photoPanel.Size = new System.Drawing.Size(420, 110);
            photoPanel.TabIndex = 9;
            // 
            // photoPreview
            // 
            photoPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            photoPreview.Location = new System.Drawing.Point(0, 0);
            photoPreview.Name = "photoPreview";
            photoPreview.Size = new System.Drawing.Size(120, 110);
            photoPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            photoPreview.TabIndex = 0;
            photoPreview.TabStop = false;
            // 
            // photoButtons
            // 
            photoButtons.Controls.Add(buttonPhotoUpload);
            photoButtons.Controls.Add(buttonPhotoRemove);
            photoButtons.Location = new System.Drawing.Point(132, 0);
            photoButtons.Margin = new System.Windows.Forms.Padding(0);
            photoButtons.Name = "photoButtons";
            photoButtons.Size = new System.Drawing.Size(288, 32);
            photoButtons.TabIndex = 1;
            photoButtons.WrapContents = false;
            // 
            // buttonPhotoUpload
            // 
            buttonPhotoUpload.Location = new System.Drawing.Point(0, 0);
            buttonPhotoUpload.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            buttonPhotoUpload.Name = "buttonPhotoUpload";
            buttonPhotoUpload.Size = new System.Drawing.Size(90, 32);
            buttonPhotoUpload.TabIndex = 0;
            buttonPhotoUpload.Text = "Upload";
            buttonPhotoUpload.UseVisualStyleBackColor = true;
            buttonPhotoUpload.Click += buttonPhotoUpload_Click;
            // 
            // buttonPhotoRemove
            // 
            buttonPhotoRemove.Location = new System.Drawing.Point(100, 0);
            buttonPhotoRemove.Margin = new System.Windows.Forms.Padding(0);
            buttonPhotoRemove.Name = "buttonPhotoRemove";
            buttonPhotoRemove.Size = new System.Drawing.Size(90, 32);
            buttonPhotoRemove.TabIndex = 1;
            buttonPhotoRemove.Text = "Remove";
            buttonPhotoRemove.UseVisualStyleBackColor = true;
            buttonPhotoRemove.Click += buttonPhotoRemove_Click;
            // 
            // button1
            // 
            button1.Anchor = ((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            button1.Location = new System.Drawing.Point(0, 365);
            button1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(420, 29);
            button1.TabIndex = 10;
            button1.Text = "Register";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // loginRow
            // 
            loginRow.AutoSize = true;
            loginRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            loginRow.Location = new System.Drawing.Point(0, 404);
            loginRow.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            loginRow.Name = "loginRow";
            loginRow.Size = new System.Drawing.Size(198, 23);
            loginRow.TabIndex = 11;
            loginRow.WrapContents = false;
            loginRow.Controls.Add(label3);
            loginRow.Controls.Add(button2);
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(0, 0);
            label3.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(138, 15);
            label3.TabIndex = 0;
            label3.Text = "Already have an account?";
            // 
            // button2
            // 
            button2.AutoSize = true;
            button2.Location = new System.Drawing.Point(144, 0);
            button2.Margin = new System.Windows.Forms.Padding(0);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(54, 23);
            button2.TabIndex = 1;
            button2.Text = "Log in";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // RegisterForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1040, 640);
            Controls.Add(rootLayout);
            Name = "RegisterForm";
            Text = "Barangay System - Register";
            Load += RegisterForm_Load;
            rootLayout.ResumeLayout(false);
            panelLeft.ResumeLayout(false);
            panelLeft.PerformLayout();
            heroStack.ResumeLayout(false);
            heroStack.PerformLayout();
            panelRight.ResumeLayout(false);
            centerLayout.ResumeLayout(false);
            centerLayout.PerformLayout();
            panelCard.ResumeLayout(false);
            panelCard.PerformLayout();
            cardLayout.ResumeLayout(false);
            cardLayout.PerformLayout();
            loginRow.ResumeLayout(false);
            loginRow.PerformLayout();
            ResumeLayout(false);
        }
    }
}
