namespace baranggaysystem1
{
    partial class UsersListForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.FlowLayoutPanel filterRow;
        private System.Windows.Forms.Label labelSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label labelRole;
        private System.Windows.Forms.ComboBox cmbRole;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.FlowLayoutPanel actionsRow;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.DataGridView gridUsers;

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
            labelTitle = new System.Windows.Forms.Label();
            filterRow = new System.Windows.Forms.FlowLayoutPanel();
            labelSearch = new System.Windows.Forms.Label();
            txtSearch = new System.Windows.Forms.TextBox();
            labelRole = new System.Windows.Forms.Label();
            cmbRole = new System.Windows.Forms.ComboBox();
            labelStatus = new System.Windows.Forms.Label();
            cmbStatus = new System.Windows.Forms.ComboBox();
            actionsRow = new System.Windows.Forms.FlowLayoutPanel();
            btnRefresh = new System.Windows.Forms.Button();
            btnEdit = new System.Windows.Forms.Button();
            btnClose = new System.Windows.Forms.Button();
            gridUsers = new System.Windows.Forms.DataGridView();
            rootLayout.SuspendLayout();
            filterRow.SuspendLayout();
            actionsRow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridUsers).BeginInit();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            rootLayout.Location = new System.Drawing.Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new System.Windows.Forms.Padding(24);
            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            rootLayout.Size = new System.Drawing.Size(900, 600);
            rootLayout.TabIndex = 0;
            rootLayout.Controls.Add(labelTitle, 0, 0);
            rootLayout.Controls.Add(filterRow, 0, 1);
            rootLayout.Controls.Add(gridUsers, 0, 2);
            rootLayout.Controls.Add(actionsRow, 0, 3);
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Location = new System.Drawing.Point(24, 24);
            labelTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new System.Drawing.Size(123, 15);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "User Directory";
            // 
            // filterRow
            // 
            filterRow.AutoSize = true;
            filterRow.Controls.Add(labelSearch);
            filterRow.Controls.Add(txtSearch);
            filterRow.Controls.Add(labelRole);
            filterRow.Controls.Add(cmbRole);
            filterRow.Controls.Add(labelStatus);
            filterRow.Controls.Add(cmbStatus);
            filterRow.Dock = System.Windows.Forms.DockStyle.Fill;
            filterRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            filterRow.Location = new System.Drawing.Point(24, 51);
            filterRow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            filterRow.Name = "filterRow";
            filterRow.Size = new System.Drawing.Size(852, 31);
            filterRow.TabIndex = 1;
            filterRow.WrapContents = false;
            // 
            // labelSearch
            // 
            labelSearch.AutoSize = true;
            labelSearch.Location = new System.Drawing.Point(0, 6);
            labelSearch.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);
            labelSearch.Name = "labelSearch";
            labelSearch.Size = new System.Drawing.Size(42, 15);
            labelSearch.TabIndex = 0;
            labelSearch.Text = "Search";
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(48, 3);
            txtSearch.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(240, 23);
            txtSearch.TabIndex = 1;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // labelRole
            // 
            labelRole.AutoSize = true;
            labelRole.Location = new System.Drawing.Point(306, 6);
            labelRole.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);
            labelRole.Name = "labelRole";
            labelRole.Size = new System.Drawing.Size(30, 15);
            labelRole.TabIndex = 2;
            labelRole.Text = "Role";
            // 
            // cmbRole
            // 
            cmbRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbRole.FormattingEnabled = true;
            cmbRole.Items.AddRange(new object[] { "All", "Super Admin", "Admin", "Staff" });
            cmbRole.Location = new System.Drawing.Point(342, 3);
            cmbRole.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new System.Drawing.Size(140, 23);
            cmbRole.TabIndex = 3;
            cmbRole.SelectedIndexChanged += cmbRole_SelectedIndexChanged;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Location = new System.Drawing.Point(500, 6);
            labelStatus.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new System.Drawing.Size(42, 15);
            labelStatus.TabIndex = 4;
            labelStatus.Text = "Status";
            // 
            // cmbStatus
            // 
            cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Items.AddRange(new object[] { "All", "Active", "Inactive" });
            cmbStatus.Location = new System.Drawing.Point(548, 3);
            cmbStatus.Margin = new System.Windows.Forms.Padding(0);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new System.Drawing.Size(140, 23);
            cmbStatus.TabIndex = 5;
            cmbStatus.SelectedIndexChanged += cmbStatus_SelectedIndexChanged;
            // 
            // actionsRow
            // 
            actionsRow.AutoSize = true;
            actionsRow.Controls.Add(btnRefresh);
            actionsRow.Controls.Add(btnEdit);
            actionsRow.Controls.Add(btnClose);
            actionsRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            actionsRow.Location = new System.Drawing.Point(24, 548);
            actionsRow.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            actionsRow.Name = "actionsRow";
            actionsRow.Size = new System.Drawing.Size(285, 32);
            actionsRow.TabIndex = 3;
            actionsRow.WrapContents = false;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(0, 0);
            btnRefresh.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(90, 32);
            btnRefresh.TabIndex = 0;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnEdit
            // 
            btnEdit.Location = new System.Drawing.Point(100, 0);
            btnEdit.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new System.Drawing.Size(90, 32);
            btnEdit.TabIndex = 1;
            btnEdit.Text = "Edit";
            btnEdit.UseVisualStyleBackColor = true;
            btnEdit.Click += btnEdit_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new System.Drawing.Point(200, 0);
            btnClose.Margin = new System.Windows.Forms.Padding(0);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(85, 32);
            btnClose.TabIndex = 2;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // gridUsers
            // 
            gridUsers.AllowUserToAddRows = false;
            gridUsers.AllowUserToDeleteRows = false;
            gridUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            gridUsers.Location = new System.Drawing.Point(24, 94);
            gridUsers.Margin = new System.Windows.Forms.Padding(0);
            gridUsers.Name = "gridUsers";
            gridUsers.ReadOnly = true;
            gridUsers.RowHeadersWidth = 51;
            gridUsers.Size = new System.Drawing.Size(852, 442);
            gridUsers.TabIndex = 2;
            // 
            // UsersListForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 600);
            Controls.Add(rootLayout);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "UsersListForm";
            Text = "UsersListForm";
            Load += UsersListForm_Load;
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            filterRow.ResumeLayout(false);
            filterRow.PerformLayout();
            actionsRow.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridUsers).EndInit();
            ResumeLayout(false);
        }
    }
}
