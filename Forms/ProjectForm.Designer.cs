namespace baranggaysystem1
{
    partial class ProjectForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label headerLabel;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Label labelBudget;
        private System.Windows.Forms.NumericUpDown numBudget;
        private System.Windows.Forms.Label labelStartDate;
        private System.Windows.Forms.DateTimePicker dtpStartDate;
        private System.Windows.Forms.Label labelEndDate;
        private System.Windows.Forms.DateTimePicker dtpEndDate;
        private System.Windows.Forms.Label labelLead;
        private System.Windows.Forms.TextBox txtLead;
        private System.Windows.Forms.Label labelRemarks;
        private System.Windows.Forms.TextBox txtRemarks;
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
            labelName = new System.Windows.Forms.Label();
            txtName = new System.Windows.Forms.TextBox();
            labelStatus = new System.Windows.Forms.Label();
            cmbStatus = new System.Windows.Forms.ComboBox();
            labelBudget = new System.Windows.Forms.Label();
            numBudget = new System.Windows.Forms.NumericUpDown();
            labelStartDate = new System.Windows.Forms.Label();
            dtpStartDate = new System.Windows.Forms.DateTimePicker();
            labelEndDate = new System.Windows.Forms.Label();
            dtpEndDate = new System.Windows.Forms.DateTimePicker();
            labelLead = new System.Windows.Forms.Label();
            txtLead = new System.Windows.Forms.TextBox();
            labelRemarks = new System.Windows.Forms.Label();
            txtRemarks = new System.Windows.Forms.TextBox();
            actionRow = new System.Windows.Forms.FlowLayoutPanel();
            btnSave = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBudget).BeginInit();
            actionRow.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 2;
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            rootLayout.Controls.Add(headerLabel, 0, 0);
            rootLayout.Controls.Add(labelName, 0, 1);
            rootLayout.Controls.Add(txtName, 1, 1);
            rootLayout.Controls.Add(labelStatus, 0, 2);
            rootLayout.Controls.Add(cmbStatus, 1, 2);
            rootLayout.Controls.Add(labelBudget, 0, 3);
            rootLayout.Controls.Add(numBudget, 1, 3);
            rootLayout.Controls.Add(labelStartDate, 0, 4);
            rootLayout.Controls.Add(dtpStartDate, 1, 4);
            rootLayout.Controls.Add(labelEndDate, 0, 5);
            rootLayout.Controls.Add(dtpEndDate, 1, 5);
            rootLayout.Controls.Add(labelLead, 0, 6);
            rootLayout.Controls.Add(txtLead, 1, 6);
            rootLayout.Controls.Add(labelRemarks, 0, 7);
            rootLayout.Controls.Add(txtRemarks, 1, 7);
            rootLayout.Controls.Add(actionRow, 0, 8);
            rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            rootLayout.Location = new System.Drawing.Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new System.Windows.Forms.Padding(20);
            rootLayout.RowCount = 9;
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            rootLayout.Size = new System.Drawing.Size(560, 440);
            rootLayout.TabIndex = 0;
            // 
            // headerLabel
            // 
            headerLabel.AutoSize = true;
            rootLayout.SetColumnSpan(headerLabel, 2);
            headerLabel.Location = new System.Drawing.Point(23, 20);
            headerLabel.Name = "headerLabel";
            headerLabel.Size = new System.Drawing.Size(142, 20);
            headerLabel.TabIndex = 0;
            headerLabel.Text = "New Program/Project";
            // 
            // labelName
            // 
            labelName.AutoSize = true;
            labelName.Location = new System.Drawing.Point(23, 52);
            labelName.Name = "labelName";
            labelName.Size = new System.Drawing.Size(49, 20);
            labelName.TabIndex = 1;
            labelName.Text = "Name";
            // 
            // txtName
            // 
            txtName.Location = new System.Drawing.Point(163, 49);
            txtName.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtName.Name = "txtName";
            txtName.Size = new System.Drawing.Size(340, 27);
            txtName.TabIndex = 2;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Location = new System.Drawing.Point(23, 88);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new System.Drawing.Size(49, 20);
            labelStatus.TabIndex = 3;
            labelStatus.Text = "Status";
            // 
            // cmbStatus
            // 
            cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new System.Drawing.Point(163, 85);
            cmbStatus.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new System.Drawing.Size(200, 28);
            cmbStatus.TabIndex = 4;
            // 
            // labelBudget
            // 
            labelBudget.AutoSize = true;
            labelBudget.Location = new System.Drawing.Point(23, 124);
            labelBudget.Name = "labelBudget";
            labelBudget.Size = new System.Drawing.Size(56, 20);
            labelBudget.TabIndex = 5;
            labelBudget.Text = "Budget";
            // 
            // numBudget
            // 
            numBudget.DecimalPlaces = 2;
            numBudget.Location = new System.Drawing.Point(163, 121);
            numBudget.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            numBudget.Maximum = new decimal(new int[] { 100000000, 0, 0, 0 });
            numBudget.Name = "numBudget";
            numBudget.Size = new System.Drawing.Size(200, 27);
            numBudget.TabIndex = 6;
            // 
            // labelStartDate
            // 
            labelStartDate.AutoSize = true;
            labelStartDate.Location = new System.Drawing.Point(23, 160);
            labelStartDate.Name = "labelStartDate";
            labelStartDate.Size = new System.Drawing.Size(74, 20);
            labelStartDate.TabIndex = 7;
            labelStartDate.Text = "Start date";
            // 
            // dtpStartDate
            // 
            dtpStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpStartDate.Location = new System.Drawing.Point(163, 157);
            dtpStartDate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            dtpStartDate.Name = "dtpStartDate";
            dtpStartDate.ShowCheckBox = true;
            dtpStartDate.Size = new System.Drawing.Size(200, 27);
            dtpStartDate.TabIndex = 8;
            // 
            // labelEndDate
            // 
            labelEndDate.AutoSize = true;
            labelEndDate.Location = new System.Drawing.Point(23, 196);
            labelEndDate.Name = "labelEndDate";
            labelEndDate.Size = new System.Drawing.Size(68, 20);
            labelEndDate.TabIndex = 9;
            labelEndDate.Text = "End date";
            // 
            // dtpEndDate
            // 
            dtpEndDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpEndDate.Location = new System.Drawing.Point(163, 193);
            dtpEndDate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            dtpEndDate.Name = "dtpEndDate";
            dtpEndDate.ShowCheckBox = true;
            dtpEndDate.Size = new System.Drawing.Size(200, 27);
            dtpEndDate.TabIndex = 10;
            // 
            // labelLead
            // 
            labelLead.AutoSize = true;
            labelLead.Location = new System.Drawing.Point(23, 232);
            labelLead.Name = "labelLead";
            labelLead.Size = new System.Drawing.Size(42, 20);
            labelLead.TabIndex = 11;
            labelLead.Text = "Lead";
            // 
            // txtLead
            // 
            txtLead.Location = new System.Drawing.Point(163, 229);
            txtLead.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtLead.Name = "txtLead";
            txtLead.Size = new System.Drawing.Size(260, 27);
            txtLead.TabIndex = 12;
            // 
            // labelRemarks
            // 
            labelRemarks.AutoSize = true;
            labelRemarks.Location = new System.Drawing.Point(23, 268);
            labelRemarks.Name = "labelRemarks";
            labelRemarks.Size = new System.Drawing.Size(66, 20);
            labelRemarks.TabIndex = 13;
            labelRemarks.Text = "Remarks";
            // 
            // txtRemarks
            // 
            txtRemarks.Location = new System.Drawing.Point(163, 265);
            txtRemarks.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtRemarks.Multiline = true;
            txtRemarks.Name = "txtRemarks";
            txtRemarks.Size = new System.Drawing.Size(340, 76);
            txtRemarks.TabIndex = 14;
            // 
            // actionRow
            // 
            actionRow.AutoSize = true;
            rootLayout.SetColumnSpan(actionRow, 2);
            actionRow.Controls.Add(btnSave);
            actionRow.Controls.Add(btnCancel);
            actionRow.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            actionRow.Location = new System.Drawing.Point(23, 357);
            actionRow.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            actionRow.Name = "actionRow";
            actionRow.Size = new System.Drawing.Size(160, 32);
            actionRow.TabIndex = 15;
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
            // ProjectForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(560, 440);
            Controls.Add(rootLayout);
            Name = "ProjectForm";
            Text = "Project";
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numBudget).EndInit();
            actionRow.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
