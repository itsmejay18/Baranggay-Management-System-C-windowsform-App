namespace baranggaysystem1
{
    partial class AnnouncementForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.Label labelMessage;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Label labelPriority;
        private System.Windows.Forms.ComboBox cmbPriority;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Label labelPinned;
        private System.Windows.Forms.CheckBox chkPinned;
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
            rootLayout = new System.Windows.Forms.TableLayoutPanel();
            headerLabel = new System.Windows.Forms.Label();
            labelTitle = new System.Windows.Forms.Label();
            txtTitle = new System.Windows.Forms.TextBox();
            labelMessage = new System.Windows.Forms.Label();
            txtMessage = new System.Windows.Forms.TextBox();
            labelPriority = new System.Windows.Forms.Label();
            cmbPriority = new System.Windows.Forms.ComboBox();
            labelStatus = new System.Windows.Forms.Label();
            cmbStatus = new System.Windows.Forms.ComboBox();
            labelPinned = new System.Windows.Forms.Label();
            chkPinned = new System.Windows.Forms.CheckBox();
            actionRow = new System.Windows.Forms.FlowLayoutPanel();
            btnSave = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            rootLayout.SuspendLayout();
            actionRow.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 2;
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            rootLayout.Controls.Add(headerLabel, 0, 0);
            rootLayout.Controls.Add(labelTitle, 0, 1);
            rootLayout.Controls.Add(txtTitle, 1, 1);
            rootLayout.Controls.Add(labelMessage, 0, 2);
            rootLayout.Controls.Add(txtMessage, 1, 2);
            rootLayout.Controls.Add(labelPriority, 0, 3);
            rootLayout.Controls.Add(cmbPriority, 1, 3);
            rootLayout.Controls.Add(labelStatus, 0, 4);
            rootLayout.Controls.Add(cmbStatus, 1, 4);
            rootLayout.Controls.Add(labelPinned, 0, 5);
            rootLayout.Controls.Add(chkPinned, 1, 5);
            rootLayout.Controls.Add(actionRow, 0, 6);
            rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            rootLayout.Location = new System.Drawing.Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new System.Windows.Forms.Padding(20);
            rootLayout.RowCount = 7;
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            rootLayout.Size = new System.Drawing.Size(520, 360);
            rootLayout.TabIndex = 0;
            // 
            // headerLabel
            // 
            headerLabel.AutoSize = true;
            rootLayout.SetColumnSpan(headerLabel, 2);
            headerLabel.Location = new System.Drawing.Point(23, 20);
            headerLabel.Name = "headerLabel";
            headerLabel.Size = new System.Drawing.Size(146, 20);
            headerLabel.TabIndex = 0;
            headerLabel.Text = "New Announcement";
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Location = new System.Drawing.Point(23, 52);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new System.Drawing.Size(38, 20);
            labelTitle.TabIndex = 1;
            labelTitle.Text = "Title";
            // 
            // txtTitle
            // 
            txtTitle.Location = new System.Drawing.Point(163, 49);
            txtTitle.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new System.Drawing.Size(320, 27);
            txtTitle.TabIndex = 2;
            // 
            // labelMessage
            // 
            labelMessage.AutoSize = true;
            labelMessage.Location = new System.Drawing.Point(23, 88);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new System.Drawing.Size(67, 20);
            labelMessage.TabIndex = 3;
            labelMessage.Text = "Message";
            // 
            // txtMessage
            // 
            txtMessage.Location = new System.Drawing.Point(163, 85);
            txtMessage.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtMessage.Multiline = true;
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new System.Drawing.Size(320, 90);
            txtMessage.TabIndex = 4;
            // 
            // labelPriority
            // 
            labelPriority.AutoSize = true;
            labelPriority.Location = new System.Drawing.Point(23, 198);
            labelPriority.Name = "labelPriority";
            labelPriority.Size = new System.Drawing.Size(58, 20);
            labelPriority.TabIndex = 5;
            labelPriority.Text = "Priority";
            // 
            // cmbPriority
            // 
            cmbPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbPriority.FormattingEnabled = true;
            cmbPriority.Location = new System.Drawing.Point(163, 195);
            cmbPriority.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            cmbPriority.Name = "cmbPriority";
            cmbPriority.Size = new System.Drawing.Size(180, 28);
            cmbPriority.TabIndex = 6;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Location = new System.Drawing.Point(23, 234);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new System.Drawing.Size(49, 20);
            labelStatus.TabIndex = 7;
            labelStatus.Text = "Status";
            // 
            // cmbStatus
            // 
            cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new System.Drawing.Point(163, 231);
            cmbStatus.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new System.Drawing.Size(180, 28);
            cmbStatus.TabIndex = 8;
            // 
            // labelPinned
            // 
            labelPinned.AutoSize = true;
            labelPinned.Location = new System.Drawing.Point(23, 270);
            labelPinned.Name = "labelPinned";
            labelPinned.Size = new System.Drawing.Size(52, 20);
            labelPinned.TabIndex = 9;
            labelPinned.Text = "Pinned";
            // 
            // chkPinned
            // 
            chkPinned.AutoSize = true;
            chkPinned.Location = new System.Drawing.Point(163, 268);
            chkPinned.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            chkPinned.Name = "chkPinned";
            chkPinned.Size = new System.Drawing.Size(18, 17);
            chkPinned.TabIndex = 10;
            chkPinned.UseVisualStyleBackColor = true;
            // 
            // actionRow
            // 
            actionRow.AutoSize = true;
            rootLayout.SetColumnSpan(actionRow, 2);
            actionRow.Controls.Add(btnSave);
            actionRow.Controls.Add(btnCancel);
            actionRow.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            actionRow.Location = new System.Drawing.Point(23, 304);
            actionRow.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            actionRow.Name = "actionRow";
            actionRow.Size = new System.Drawing.Size(160, 32);
            actionRow.TabIndex = 11;
            // 
            // btnSave
            // 
            btnSave.Location = new System.Drawing.Point(77, 2);
            btnSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(80, 28);
            btnSave.TabIndex = 0;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(3, 2);
            btnCancel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(68, 28);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // AnnouncementForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(520, 360);
            Controls.Add(rootLayout);
            Name = "AnnouncementForm";
            Text = "Announcement";
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            actionRow.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
