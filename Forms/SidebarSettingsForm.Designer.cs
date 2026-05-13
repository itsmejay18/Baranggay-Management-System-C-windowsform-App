namespace baranggaysystem1;

partial class SidebarSettingsForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.TabControl tabControlOptions;
    private System.Windows.Forms.TabPage tabSidebar;
    private System.Windows.Forms.TabPage tabDatabase;
    private System.Windows.Forms.TableLayoutPanel layoutMain;
    private System.Windows.Forms.Label lblMinWidth;
    private System.Windows.Forms.Label lblAutoHideDelay;
    private System.Windows.Forms.Label lblLeftEdge;
    private System.Windows.Forms.Label lblAnimationStep;
    private System.Windows.Forms.NumericUpDown nudMinWidth;
    private System.Windows.Forms.NumericUpDown nudAutoHideDelay;
    private System.Windows.Forms.NumericUpDown nudLeftEdgePixels;
    private System.Windows.Forms.NumericUpDown nudAnimationStep;
    
    private System.Windows.Forms.TableLayoutPanel layoutDatabase;
    private System.Windows.Forms.Label lblDbHost;
    private System.Windows.Forms.Label lblDbPort;
    private System.Windows.Forms.Label lblDbName;
    private System.Windows.Forms.Label lblDbUser;
    private System.Windows.Forms.Label lblDbPass;
    private System.Windows.Forms.TextBox txtDbHost;
    private System.Windows.Forms.NumericUpDown numDbPort;
    private System.Windows.Forms.TextBox txtDbName;
    private System.Windows.Forms.TextBox txtDbUser;
    private System.Windows.Forms.TextBox txtDbPass;
    private System.Windows.Forms.Label lblDbMode;
    private System.Windows.Forms.Label lblDbModeValue;
    private System.Windows.Forms.FlowLayoutPanel dbModeActions;
    private System.Windows.Forms.Button btnDbTest;
    private System.Windows.Forms.Button btnDbSwitchOnline;
    private System.Windows.Forms.Button btnDbSwitchOffline;
    private System.Windows.Forms.Label lblDbStatus;

    private System.Windows.Forms.FlowLayoutPanel footerButtons;
    private System.Windows.Forms.Button btnReset;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Button btnSave;

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
        tabControlOptions = new System.Windows.Forms.TabControl();
        tabSidebar = new System.Windows.Forms.TabPage();
        tabDatabase = new System.Windows.Forms.TabPage();
        
        layoutMain = new System.Windows.Forms.TableLayoutPanel();
        lblMinWidth = new System.Windows.Forms.Label();
        lblAutoHideDelay = new System.Windows.Forms.Label();
        lblLeftEdge = new System.Windows.Forms.Label();
        lblAnimationStep = new System.Windows.Forms.Label();
        nudMinWidth = new System.Windows.Forms.NumericUpDown();
        nudAutoHideDelay = new System.Windows.Forms.NumericUpDown();
        nudLeftEdgePixels = new System.Windows.Forms.NumericUpDown();
        nudAnimationStep = new System.Windows.Forms.NumericUpDown();
        
        layoutDatabase = new System.Windows.Forms.TableLayoutPanel();
        lblDbHost = new System.Windows.Forms.Label();
        lblDbPort = new System.Windows.Forms.Label();
        lblDbName = new System.Windows.Forms.Label();
        lblDbUser = new System.Windows.Forms.Label();
        lblDbPass = new System.Windows.Forms.Label();
        txtDbHost = new System.Windows.Forms.TextBox();
        numDbPort = new System.Windows.Forms.NumericUpDown();
        txtDbName = new System.Windows.Forms.TextBox();
        txtDbUser = new System.Windows.Forms.TextBox();
        txtDbPass = new System.Windows.Forms.TextBox();
        lblDbMode = new System.Windows.Forms.Label();
        lblDbModeValue = new System.Windows.Forms.Label();
        dbModeActions = new System.Windows.Forms.FlowLayoutPanel();
        btnDbTest = new System.Windows.Forms.Button();
        btnDbSwitchOnline = new System.Windows.Forms.Button();
        btnDbSwitchOffline = new System.Windows.Forms.Button();
        lblDbStatus = new System.Windows.Forms.Label();
        
        footerButtons = new System.Windows.Forms.FlowLayoutPanel();
        btnReset = new System.Windows.Forms.Button();
        btnCancel = new System.Windows.Forms.Button();
        btnSave = new System.Windows.Forms.Button();
        
        tabControlOptions.SuspendLayout();
        tabSidebar.SuspendLayout();
        tabDatabase.SuspendLayout();
        layoutMain.SuspendLayout();
        layoutDatabase.SuspendLayout();
        dbModeActions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudMinWidth).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudAutoHideDelay).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudLeftEdgePixels).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudAnimationStep).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDbPort).BeginInit();
        footerButtons.SuspendLayout();
        SuspendLayout();
        
        // tabControlOptions
        tabControlOptions.Controls.Add(tabSidebar);
        tabControlOptions.Controls.Add(tabDatabase);
        tabControlOptions.Dock = System.Windows.Forms.DockStyle.Fill;
        tabControlOptions.Location = new System.Drawing.Point(12, 12);
        tabControlOptions.Name = "tabControlOptions";
        tabControlOptions.SelectedIndex = 0;
        tabControlOptions.Size = new System.Drawing.Size(424, 295);
        tabControlOptions.TabIndex = 0;
        
        // tabSidebar
        tabSidebar.Controls.Add(layoutMain);
        tabSidebar.Location = new System.Drawing.Point(4, 29);
        tabSidebar.Name = "tabSidebar";
        tabSidebar.Padding = new System.Windows.Forms.Padding(3);
        tabSidebar.Size = new System.Drawing.Size(416, 186);
        tabSidebar.TabIndex = 0;
        tabSidebar.Text = "Sidebar";
        tabSidebar.UseVisualStyleBackColor = true;
        
        // tabDatabase
        tabDatabase.Controls.Add(layoutDatabase);
        tabDatabase.Location = new System.Drawing.Point(4, 29);
        tabDatabase.Name = "tabDatabase";
        tabDatabase.Padding = new System.Windows.Forms.Padding(3);
        tabDatabase.Size = new System.Drawing.Size(416, 186);
        tabDatabase.TabIndex = 1;
        tabDatabase.Text = "Database";
        tabDatabase.UseVisualStyleBackColor = true;
        
        // layoutMain
        layoutMain.ColumnCount = 2;
        layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62F));
        layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
        layoutMain.Controls.Add(lblMinWidth, 0, 0);
        layoutMain.Controls.Add(lblAutoHideDelay, 0, 1);
        layoutMain.Controls.Add(lblLeftEdge, 0, 2);
        layoutMain.Controls.Add(lblAnimationStep, 0, 3);
        layoutMain.Controls.Add(nudMinWidth, 1, 0);
        layoutMain.Controls.Add(nudAutoHideDelay, 1, 1);
        layoutMain.Controls.Add(nudLeftEdgePixels, 1, 2);
        layoutMain.Controls.Add(nudAnimationStep, 1, 3);
        layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
        layoutMain.Location = new System.Drawing.Point(3, 3);
        layoutMain.Name = "layoutMain";
        layoutMain.RowCount = 5;
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        layoutMain.Size = new System.Drawing.Size(410, 180);
        layoutMain.TabIndex = 0;

        // lblMinWidth
        lblMinWidth.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblMinWidth.AutoSize = true;
        lblMinWidth.Location = new System.Drawing.Point(3, 10);
        lblMinWidth.Name = "lblMinWidth";
        lblMinWidth.Size = new System.Drawing.Size(177, 20);
        lblMinWidth.TabIndex = 0;
        lblMinWidth.Text = "Sidebar width (expanded)";

        // lblAutoHideDelay
        lblAutoHideDelay.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblAutoHideDelay.AutoSize = true;
        lblAutoHideDelay.Location = new System.Drawing.Point(3, 50);
        lblAutoHideDelay.Name = "lblAutoHideDelay";
        lblAutoHideDelay.Size = new System.Drawing.Size(167, 20);
        lblAutoHideDelay.TabIndex = 1;
        lblAutoHideDelay.Text = "Auto-hide delay (ms)";

        // lblLeftEdge
        lblLeftEdge.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblLeftEdge.AutoSize = true;
        lblLeftEdge.Location = new System.Drawing.Point(3, 90);
        lblLeftEdge.Name = "lblLeftEdge";
        lblLeftEdge.Size = new System.Drawing.Size(188, 20);
        lblLeftEdge.TabIndex = 2;
        lblLeftEdge.Text = "Left-edge open zone (px)";

        // lblAnimationStep
        lblAnimationStep.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblAnimationStep.AutoSize = true;
        lblAnimationStep.Location = new System.Drawing.Point(3, 130);
        lblAnimationStep.Name = "lblAnimationStep";
        lblAnimationStep.Size = new System.Drawing.Size(155, 20);
        lblAnimationStep.TabIndex = 3;
        lblAnimationStep.Text = "Animation speed step";

        // nudMinWidth
        nudMinWidth.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudMinWidth.Location = new System.Drawing.Point(257, 6);
        nudMinWidth.Maximum = new decimal(new int[] { 420, 0, 0, 0 });
        nudMinWidth.Minimum = new decimal(new int[] { 120, 0, 0, 0 });
        nudMinWidth.Name = "nudMinWidth";
        nudMinWidth.Size = new System.Drawing.Size(150, 27);
        nudMinWidth.TabIndex = 4;
        nudMinWidth.Value = new decimal(new int[] { 220, 0, 0, 0 });

        // nudAutoHideDelay
        nudAutoHideDelay.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudAutoHideDelay.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        nudAutoHideDelay.Location = new System.Drawing.Point(257, 46);
        nudAutoHideDelay.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
        nudAutoHideDelay.Minimum = new decimal(new int[] { 300, 0, 0, 0 });
        nudAutoHideDelay.Name = "nudAutoHideDelay";
        nudAutoHideDelay.Size = new System.Drawing.Size(150, 27);
        nudAutoHideDelay.TabIndex = 5;
        nudAutoHideDelay.Value = new decimal(new int[] { 1000, 0, 0, 0 });

        // nudLeftEdgePixels
        nudLeftEdgePixels.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudLeftEdgePixels.Location = new System.Drawing.Point(257, 86);
        nudLeftEdgePixels.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        nudLeftEdgePixels.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        nudLeftEdgePixels.Name = "nudLeftEdgePixels";
        nudLeftEdgePixels.Size = new System.Drawing.Size(150, 27);
        nudLeftEdgePixels.TabIndex = 6;
        nudLeftEdgePixels.Value = new decimal(new int[] { 10, 0, 0, 0 });

        // nudAnimationStep
        nudAnimationStep.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudAnimationStep.Location = new System.Drawing.Point(257, 126);
        nudAnimationStep.Maximum = new decimal(new int[] { 80, 0, 0, 0 });
        nudAnimationStep.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        nudAnimationStep.Name = "nudAnimationStep";
        nudAnimationStep.Size = new System.Drawing.Size(150, 27);
        nudAnimationStep.TabIndex = 7;
        nudAnimationStep.Value = new decimal(new int[] { 30, 0, 0, 0 });

        // layoutDatabase
        layoutDatabase.ColumnCount = 2;
        layoutDatabase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
        layoutDatabase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
        layoutDatabase.Controls.Add(lblDbHost, 0, 0);
        layoutDatabase.Controls.Add(lblDbPort, 0, 1);
        layoutDatabase.Controls.Add(lblDbName, 0, 2);
        layoutDatabase.Controls.Add(lblDbUser, 0, 3);
        layoutDatabase.Controls.Add(lblDbPass, 0, 4);
        layoutDatabase.Controls.Add(txtDbHost, 1, 0);
        layoutDatabase.Controls.Add(numDbPort, 1, 1);
        layoutDatabase.Controls.Add(txtDbName, 1, 2);
        layoutDatabase.Controls.Add(txtDbUser, 1, 3);
        layoutDatabase.Controls.Add(txtDbPass, 1, 4);
        layoutDatabase.Controls.Add(lblDbMode, 0, 5);
        layoutDatabase.Controls.Add(lblDbModeValue, 1, 5);
        layoutDatabase.Controls.Add(dbModeActions, 0, 6);
        layoutDatabase.Controls.Add(lblDbStatus, 0, 7);
        layoutDatabase.Dock = System.Windows.Forms.DockStyle.Fill;
        layoutDatabase.Location = new System.Drawing.Point(3, 3);
        layoutDatabase.Name = "layoutDatabase";
        layoutDatabase.RowCount = 9;
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
        layoutDatabase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        layoutDatabase.Size = new System.Drawing.Size(410, 180);
        layoutDatabase.TabIndex = 0;

        // lblDbHost
        lblDbHost.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbHost.AutoSize = true;
        lblDbHost.Location = new System.Drawing.Point(3, 7);
        lblDbHost.Name = "lblDbHost";
        lblDbHost.Size = new System.Drawing.Size(40, 20);
        lblDbHost.TabIndex = 0;
        lblDbHost.Text = "Host";

        // lblDbPort
        lblDbPort.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbPort.AutoSize = true;
        lblDbPort.Location = new System.Drawing.Point(3, 42);
        lblDbPort.Name = "lblDbPort";
        lblDbPort.Size = new System.Drawing.Size(35, 20);
        lblDbPort.TabIndex = 1;
        lblDbPort.Text = "Port";

        // lblDbName
        lblDbName.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbName.AutoSize = true;
        lblDbName.Location = new System.Drawing.Point(3, 77);
        lblDbName.Name = "lblDbName";
        lblDbName.Size = new System.Drawing.Size(72, 20);
        lblDbName.TabIndex = 2;
        lblDbName.Text = "Database";

        // lblDbUser
        lblDbUser.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbUser.AutoSize = true;
        lblDbUser.Location = new System.Drawing.Point(3, 112);
        lblDbUser.Name = "lblDbUser";
        lblDbUser.Size = new System.Drawing.Size(75, 20);
        lblDbUser.TabIndex = 3;
        lblDbUser.Text = "Username";

        // lblDbPass
        lblDbPass.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbPass.AutoSize = true;
        lblDbPass.Location = new System.Drawing.Point(3, 147);
        lblDbPass.Name = "lblDbPass";
        lblDbPass.Size = new System.Drawing.Size(70, 20);
        lblDbPass.TabIndex = 4;
        lblDbPass.Text = "Password";

        // txtDbHost
        txtDbHost.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtDbHost.Location = new System.Drawing.Point(167, 4);
        txtDbHost.Name = "txtDbHost";
        txtDbHost.Size = new System.Drawing.Size(240, 27);
        txtDbHost.TabIndex = 5;

        // numDbPort
        numDbPort.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        numDbPort.Location = new System.Drawing.Point(167, 39);
        numDbPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        numDbPort.Name = "numDbPort";
        numDbPort.Size = new System.Drawing.Size(240, 27);
        numDbPort.TabIndex = 6;

        // txtDbName
        txtDbName.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtDbName.Location = new System.Drawing.Point(167, 74);
        txtDbName.Name = "txtDbName";
        txtDbName.Size = new System.Drawing.Size(240, 27);
        txtDbName.TabIndex = 7;

        // txtDbUser
        txtDbUser.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtDbUser.Location = new System.Drawing.Point(167, 109);
        txtDbUser.Name = "txtDbUser";
        txtDbUser.Size = new System.Drawing.Size(240, 27);
        txtDbUser.TabIndex = 8;

        // txtDbPass
        txtDbPass.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        txtDbPass.Location = new System.Drawing.Point(167, 144);
        txtDbPass.Name = "txtDbPass";
        txtDbPass.PasswordChar = '*';
        txtDbPass.Size = new System.Drawing.Size(240, 27);
        txtDbPass.TabIndex = 9;

        // lblDbMode
        lblDbMode.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbMode.AutoSize = true;
        lblDbMode.Location = new System.Drawing.Point(3, 178);
        lblDbMode.Name = "lblDbMode";
        lblDbMode.Size = new System.Drawing.Size(109, 20);
        lblDbMode.TabIndex = 10;
        lblDbMode.Text = "Connection mode";

        // lblDbModeValue
        lblDbModeValue.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblDbModeValue.AutoSize = true;
        lblDbModeValue.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        lblDbModeValue.Location = new System.Drawing.Point(167, 178);
        lblDbModeValue.Name = "lblDbModeValue";
        lblDbModeValue.Size = new System.Drawing.Size(53, 20);
        lblDbModeValue.TabIndex = 11;
        lblDbModeValue.Text = "Online";

        // dbModeActions
        dbModeActions.AutoSize = true;
        dbModeActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        layoutDatabase.SetColumnSpan(dbModeActions, 2);
        dbModeActions.Controls.Add(btnDbTest);
        dbModeActions.Controls.Add(btnDbSwitchOnline);
        dbModeActions.Controls.Add(btnDbSwitchOffline);
        dbModeActions.Dock = System.Windows.Forms.DockStyle.Fill;
        dbModeActions.Location = new System.Drawing.Point(3, 203);
        dbModeActions.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
        dbModeActions.Name = "dbModeActions";
        dbModeActions.Size = new System.Drawing.Size(404, 36);
        dbModeActions.TabIndex = 12;

        // btnDbTest
        btnDbTest.Location = new System.Drawing.Point(0, 0);
        btnDbTest.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        btnDbTest.Name = "btnDbTest";
        btnDbTest.Size = new System.Drawing.Size(118, 34);
        btnDbTest.TabIndex = 0;
        btnDbTest.Text = "Test";
        btnDbTest.UseVisualStyleBackColor = true;
        btnDbTest.Click += btnDbTest_Click;

        // btnDbSwitchOnline
        btnDbSwitchOnline.Location = new System.Drawing.Point(124, 0);
        btnDbSwitchOnline.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
        btnDbSwitchOnline.Name = "btnDbSwitchOnline";
        btnDbSwitchOnline.Size = new System.Drawing.Size(136, 34);
        btnDbSwitchOnline.TabIndex = 1;
        btnDbSwitchOnline.Text = "Switch Online";
        btnDbSwitchOnline.UseVisualStyleBackColor = true;
        btnDbSwitchOnline.Click += btnDbSwitchOnline_Click;

        // btnDbSwitchOffline
        btnDbSwitchOffline.Location = new System.Drawing.Point(266, 0);
        btnDbSwitchOffline.Margin = new System.Windows.Forms.Padding(0);
        btnDbSwitchOffline.Name = "btnDbSwitchOffline";
        btnDbSwitchOffline.Size = new System.Drawing.Size(136, 34);
        btnDbSwitchOffline.TabIndex = 2;
        btnDbSwitchOffline.Text = "Switch Offline";
        btnDbSwitchOffline.UseVisualStyleBackColor = true;
        btnDbSwitchOffline.Click += btnDbSwitchOffline_Click;

        // lblDbStatus
        lblDbStatus.AutoEllipsis = true;
        layoutDatabase.SetColumnSpan(lblDbStatus, 2);
        lblDbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
        lblDbStatus.Location = new System.Drawing.Point(3, 239);
        lblDbStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
        lblDbStatus.Name = "lblDbStatus";
        lblDbStatus.Size = new System.Drawing.Size(404, 48);
        lblDbStatus.TabIndex = 13;
        lblDbStatus.Text = "Connection status is not checked yet.";
        lblDbStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // footerButtons
        footerButtons.Controls.Add(btnReset);
        footerButtons.Controls.Add(btnCancel);
        footerButtons.Controls.Add(btnSave);
        footerButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
        footerButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        footerButtons.Location = new System.Drawing.Point(12, 307);
        footerButtons.Margin = new System.Windows.Forms.Padding(0);
        footerButtons.Name = "footerButtons";
        footerButtons.Size = new System.Drawing.Size(424, 53);
        footerButtons.TabIndex = 8;

        // btnReset
        btnReset.Location = new System.Drawing.Point(243, 8);
        btnReset.Margin = new System.Windows.Forms.Padding(8);
        btnReset.Name = "btnReset";
        btnReset.Size = new System.Drawing.Size(173, 37);
        btnReset.TabIndex = 2;
        btnReset.Text = "Reset Defaults";
        btnReset.UseVisualStyleBackColor = true;
        btnReset.Click += btnReset_Click;

        // btnCancel
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        btnCancel.Location = new System.Drawing.Point(152, 8);
        btnCancel.Margin = new System.Windows.Forms.Padding(8);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new System.Drawing.Size(75, 37);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;

        // btnSave
        btnSave.Location = new System.Drawing.Point(62, 8);
        btnSave.Margin = new System.Windows.Forms.Padding(8);
        btnSave.Name = "btnSave";
        btnSave.Size = new System.Drawing.Size(74, 37);
        btnSave.TabIndex = 0;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;

        // SidebarSettingsForm
        AcceptButton = btnSave;
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new System.Drawing.Size(448, 372);
        Controls.Add(tabControlOptions);
        Controls.Add(footerButtons);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SidebarSettingsForm";
        Padding = new System.Windows.Forms.Padding(12);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Settings";
        Load += SidebarSettingsForm_Load;
        
        tabControlOptions.ResumeLayout(false);
        tabSidebar.ResumeLayout(false);
        tabDatabase.ResumeLayout(false);
        layoutMain.ResumeLayout(false);
        layoutMain.PerformLayout();
        layoutDatabase.ResumeLayout(false);
        layoutDatabase.PerformLayout();
        dbModeActions.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)nudMinWidth).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudAutoHideDelay).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudLeftEdgePixels).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudAnimationStep).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDbPort).EndInit();
        footerButtons.ResumeLayout(false);
        ResumeLayout(false);
    }
}
