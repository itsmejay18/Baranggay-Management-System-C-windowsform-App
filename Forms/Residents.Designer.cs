namespace baranggaysystem1
{
    partial class Residents
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelSidebar;
        private FontAwesome.Sharp.IconButton btnDashboard;
        private FontAwesome.Sharp.IconButton btnHistory;
        private FontAwesome.Sharp.IconButton btnProfile;
        private FontAwesome.Sharp.IconButton btnBlotter;
        private FontAwesome.Sharp.IconButton btnCertificates;
        private FontAwesome.Sharp.IconButton btnReports;
        private FontAwesome.Sharp.IconButton btnSettings;
        private System.Windows.Forms.Panel mainPanel;
        private FontAwesome.Sharp.IconButton sidebarToggleButton;
        private global::baranggaysystem1.ResidentModuleControl residentModuleControl;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelSidebar = new Panel();
            btnReports = new FontAwesome.Sharp.IconButton();
            btnCertificates = new FontAwesome.Sharp.IconButton();
            btnBlotter = new FontAwesome.Sharp.IconButton();
            btnProfile = new FontAwesome.Sharp.IconButton();
            btnHistory = new FontAwesome.Sharp.IconButton();
            btnDashboard = new FontAwesome.Sharp.IconButton();
            btnSettings = new FontAwesome.Sharp.IconButton();
            mainPanel = new Panel();
            sidebarToggleButton = new FontAwesome.Sharp.IconButton();
            residentModuleControl = new ResidentModuleControl();
            panelSidebar.SuspendLayout();
            mainPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panelSidebar
            // 
            panelSidebar.Controls.Add(btnSettings);
            panelSidebar.Controls.Add(btnReports);
            panelSidebar.Controls.Add(btnCertificates);
            panelSidebar.Controls.Add(btnBlotter);
            panelSidebar.Controls.Add(btnProfile);
            panelSidebar.Controls.Add(btnHistory);
            panelSidebar.Controls.Add(btnDashboard);
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Location = new Point(0, 0);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Padding = new Padding(12, 20, 12, 12);
            panelSidebar.Size = new Size(220, 720);
            panelSidebar.TabIndex = 0;
            // 
            // btnSettings
            // 
            btnSettings.Dock = DockStyle.Top;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.IconChar = FontAwesome.Sharp.IconChar.None;
            btnSettings.IconColor = Color.Black;
            btnSettings.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnSettings.Location = new Point(12, 296);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(196, 46);
            btnSettings.TabIndex = 6;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = true;
            // 
            // btnReports
            // 
            btnReports.Dock = DockStyle.Top;
            btnReports.FlatAppearance.BorderSize = 0;
            btnReports.FlatStyle = FlatStyle.Flat;
            btnReports.IconChar = FontAwesome.Sharp.IconChar.None;
            btnReports.IconColor = Color.Black;
            btnReports.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnReports.Location = new Point(12, 250);
            btnReports.Name = "btnReports";
            btnReports.Size = new Size(196, 46);
            btnReports.TabIndex = 5;
            btnReports.Text = "Reports";
            btnReports.UseVisualStyleBackColor = true;
            // 
            // btnCertificates
            // 
            btnCertificates.Dock = DockStyle.Top;
            btnCertificates.FlatAppearance.BorderSize = 0;
            btnCertificates.FlatStyle = FlatStyle.Flat;
            btnCertificates.IconChar = FontAwesome.Sharp.IconChar.None;
            btnCertificates.IconColor = Color.Black;
            btnCertificates.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnCertificates.Location = new Point(12, 204);
            btnCertificates.Name = "btnCertificates";
            btnCertificates.Size = new Size(196, 46);
            btnCertificates.TabIndex = 4;
            btnCertificates.Text = "Certificates";
            btnCertificates.UseVisualStyleBackColor = true;
            // 
            // btnBlotter
            // 
            btnBlotter.Dock = DockStyle.Top;
            btnBlotter.FlatAppearance.BorderSize = 0;
            btnBlotter.FlatStyle = FlatStyle.Flat;
            btnBlotter.IconChar = FontAwesome.Sharp.IconChar.None;
            btnBlotter.IconColor = Color.Black;
            btnBlotter.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnBlotter.Location = new Point(12, 158);
            btnBlotter.Name = "btnBlotter";
            btnBlotter.Size = new Size(196, 46);
            btnBlotter.TabIndex = 3;
            btnBlotter.Text = "Blotter";
            btnBlotter.UseVisualStyleBackColor = true;
            // 
            // btnProfile
            // 
            btnProfile.Dock = DockStyle.Top;
            btnProfile.FlatAppearance.BorderSize = 0;
            btnProfile.FlatStyle = FlatStyle.Flat;
            btnProfile.IconChar = FontAwesome.Sharp.IconChar.None;
            btnProfile.IconColor = Color.Black;
            btnProfile.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnProfile.Location = new Point(12, 112);
            btnProfile.Name = "btnProfile";
            btnProfile.Size = new Size(196, 46);
            btnProfile.TabIndex = 2;
            btnProfile.Text = "Profile";
            btnProfile.UseVisualStyleBackColor = true;
            // 
            // btnHistory
            // 
            btnHistory.Dock = DockStyle.Top;
            btnHistory.FlatAppearance.BorderSize = 0;
            btnHistory.FlatStyle = FlatStyle.Flat;
            btnHistory.IconChar = FontAwesome.Sharp.IconChar.None;
            btnHistory.IconColor = Color.Black;
            btnHistory.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnHistory.Location = new Point(12, 66);
            btnHistory.Name = "btnHistory";
            btnHistory.Size = new Size(196, 46);
            btnHistory.TabIndex = 1;
            btnHistory.Text = "History";
            btnHistory.UseVisualStyleBackColor = true;
            // 
            // btnDashboard
            // 
            btnDashboard.Dock = DockStyle.Top;
            btnDashboard.FlatAppearance.BorderSize = 0;
            btnDashboard.FlatStyle = FlatStyle.Flat;
            btnDashboard.IconChar = FontAwesome.Sharp.IconChar.None;
            btnDashboard.IconColor = Color.Black;
            btnDashboard.IconFont = FontAwesome.Sharp.IconFont.Auto;
            btnDashboard.Location = new Point(12, 20);
            btnDashboard.Name = "btnDashboard";
            btnDashboard.Size = new Size(196, 46);
            btnDashboard.TabIndex = 0;
            btnDashboard.Text = "Dashboard";
            btnDashboard.UseVisualStyleBackColor = true;
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(residentModuleControl);
            mainPanel.Controls.Add(sidebarToggleButton);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(220, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(980, 720);
            mainPanel.TabIndex = 1;
            // 
            // sidebarToggleButton
            // 
            sidebarToggleButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            sidebarToggleButton.FlatStyle = FlatStyle.Flat;
            sidebarToggleButton.IconChar = FontAwesome.Sharp.IconChar.Bars;
            sidebarToggleButton.IconColor = Color.Black;
            sidebarToggleButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            sidebarToggleButton.IconSize = 18;
            sidebarToggleButton.Location = new Point(12, 12);
            sidebarToggleButton.Name = "sidebarToggleButton";
            sidebarToggleButton.Size = new Size(36, 36);
            sidebarToggleButton.TabIndex = 1;
            sidebarToggleButton.UseVisualStyleBackColor = true;
            sidebarToggleButton.Visible = false;
            // 
            // residentModuleControl
            // 
            residentModuleControl.BackColor = Color.FromArgb(238, 238, 238);
            residentModuleControl.Dock = DockStyle.Fill;
            residentModuleControl.Font = new Font("Trebuchet MS", 10F);
            residentModuleControl.Location = new Point(0, 0);
            residentModuleControl.Name = "residentModuleControl";
            residentModuleControl.Size = new Size(980, 720);
            residentModuleControl.TabIndex = 0;
            // 
            // Residents
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 720);
            Controls.Add(mainPanel);
            Controls.Add(panelSidebar);
            Name = "Residents";
            Text = "Residents";
            panelSidebar.ResumeLayout(false);
            mainPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
    }
}
