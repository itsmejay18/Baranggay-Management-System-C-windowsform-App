namespace baranggaysystem1;

partial class SidebarSettingsForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.TableLayoutPanel layoutMain;
    private System.Windows.Forms.Label lblMinWidth;
    private System.Windows.Forms.Label lblAutoHideDelay;
    private System.Windows.Forms.Label lblLeftEdge;
    private System.Windows.Forms.Label lblAnimationStep;
    private System.Windows.Forms.NumericUpDown nudMinWidth;
    private System.Windows.Forms.NumericUpDown nudAutoHideDelay;
    private System.Windows.Forms.NumericUpDown nudLeftEdgePixels;
    private System.Windows.Forms.NumericUpDown nudAnimationStep;
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
        layoutMain = new System.Windows.Forms.TableLayoutPanel();
        lblMinWidth = new System.Windows.Forms.Label();
        lblAutoHideDelay = new System.Windows.Forms.Label();
        lblLeftEdge = new System.Windows.Forms.Label();
        lblAnimationStep = new System.Windows.Forms.Label();
        nudMinWidth = new System.Windows.Forms.NumericUpDown();
        nudAutoHideDelay = new System.Windows.Forms.NumericUpDown();
        nudLeftEdgePixels = new System.Windows.Forms.NumericUpDown();
        nudAnimationStep = new System.Windows.Forms.NumericUpDown();
        footerButtons = new System.Windows.Forms.FlowLayoutPanel();
        btnReset = new System.Windows.Forms.Button();
        btnCancel = new System.Windows.Forms.Button();
        btnSave = new System.Windows.Forms.Button();
        layoutMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudMinWidth).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudAutoHideDelay).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudLeftEdgePixels).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudAnimationStep).BeginInit();
        footerButtons.SuspendLayout();
        SuspendLayout();
        // 
        // layoutMain
        // 
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
        layoutMain.Controls.Add(footerButtons, 0, 4);
        layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
        layoutMain.Location = new System.Drawing.Point(12, 12);
        layoutMain.Name = "layoutMain";
        layoutMain.RowCount = 5;
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
        layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        layoutMain.SetColumnSpan(footerButtons, 2);
        layoutMain.Size = new System.Drawing.Size(424, 229);
        layoutMain.TabIndex = 0;
        // 
        // lblMinWidth
        // 
        lblMinWidth.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblMinWidth.AutoSize = true;
        lblMinWidth.Location = new System.Drawing.Point(3, 12);
        lblMinWidth.Name = "lblMinWidth";
        lblMinWidth.Size = new System.Drawing.Size(177, 20);
        lblMinWidth.TabIndex = 0;
        lblMinWidth.Text = "Sidebar width (expanded)";
        // 
        // lblAutoHideDelay
        // 
        lblAutoHideDelay.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblAutoHideDelay.AutoSize = true;
        lblAutoHideDelay.Location = new System.Drawing.Point(3, 56);
        lblAutoHideDelay.Name = "lblAutoHideDelay";
        lblAutoHideDelay.Size = new System.Drawing.Size(167, 20);
        lblAutoHideDelay.TabIndex = 1;
        lblAutoHideDelay.Text = "Auto-hide delay (ms)";
        // 
        // lblLeftEdge
        // 
        lblLeftEdge.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblLeftEdge.AutoSize = true;
        lblLeftEdge.Location = new System.Drawing.Point(3, 100);
        lblLeftEdge.Name = "lblLeftEdge";
        lblLeftEdge.Size = new System.Drawing.Size(188, 20);
        lblLeftEdge.TabIndex = 2;
        lblLeftEdge.Text = "Left-edge open zone (px)";
        // 
        // lblAnimationStep
        // 
        lblAnimationStep.Anchor = System.Windows.Forms.AnchorStyles.Left;
        lblAnimationStep.AutoSize = true;
        lblAnimationStep.Location = new System.Drawing.Point(3, 144);
        lblAnimationStep.Name = "lblAnimationStep";
        lblAnimationStep.Size = new System.Drawing.Size(155, 20);
        lblAnimationStep.TabIndex = 3;
        lblAnimationStep.Text = "Animation speed step";
        // 
        // nudMinWidth
        // 
        nudMinWidth.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudMinWidth.Location = new System.Drawing.Point(265, 8);
        nudMinWidth.Maximum = new decimal(new int[] { 420, 0, 0, 0 });
        nudMinWidth.Minimum = new decimal(new int[] { 120, 0, 0, 0 });
        nudMinWidth.Name = "nudMinWidth";
        nudMinWidth.Size = new System.Drawing.Size(156, 27);
        nudMinWidth.TabIndex = 4;
        nudMinWidth.Value = new decimal(new int[] { 220, 0, 0, 0 });
        // 
        // nudAutoHideDelay
        // 
        nudAutoHideDelay.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudAutoHideDelay.Increment = new decimal(new int[] { 100, 0, 0, 0 });
        nudAutoHideDelay.Location = new System.Drawing.Point(265, 52);
        nudAutoHideDelay.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
        nudAutoHideDelay.Minimum = new decimal(new int[] { 300, 0, 0, 0 });
        nudAutoHideDelay.Name = "nudAutoHideDelay";
        nudAutoHideDelay.Size = new System.Drawing.Size(156, 27);
        nudAutoHideDelay.TabIndex = 5;
        nudAutoHideDelay.Value = new decimal(new int[] { 1000, 0, 0, 0 });
        // 
        // nudLeftEdgePixels
        // 
        nudLeftEdgePixels.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudLeftEdgePixels.Location = new System.Drawing.Point(265, 96);
        nudLeftEdgePixels.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        nudLeftEdgePixels.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        nudLeftEdgePixels.Name = "nudLeftEdgePixels";
        nudLeftEdgePixels.Size = new System.Drawing.Size(156, 27);
        nudLeftEdgePixels.TabIndex = 6;
        nudLeftEdgePixels.Value = new decimal(new int[] { 10, 0, 0, 0 });
        // 
        // nudAnimationStep
        // 
        nudAnimationStep.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        nudAnimationStep.Location = new System.Drawing.Point(265, 140);
        nudAnimationStep.Maximum = new decimal(new int[] { 80, 0, 0, 0 });
        nudAnimationStep.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        nudAnimationStep.Name = "nudAnimationStep";
        nudAnimationStep.Size = new System.Drawing.Size(156, 27);
        nudAnimationStep.TabIndex = 7;
        nudAnimationStep.Value = new decimal(new int[] { 30, 0, 0, 0 });
        // 
        // footerButtons
        // 
        footerButtons.Controls.Add(btnReset);
        footerButtons.Controls.Add(btnCancel);
        footerButtons.Controls.Add(btnSave);
        footerButtons.Dock = System.Windows.Forms.DockStyle.Fill;
        footerButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        footerButtons.Location = new System.Drawing.Point(0, 176);
        footerButtons.Margin = new System.Windows.Forms.Padding(0);
        footerButtons.Name = "footerButtons";
        footerButtons.Size = new System.Drawing.Size(424, 53);
        footerButtons.TabIndex = 8;
        // 
        // btnReset
        // 
        btnReset.Location = new System.Drawing.Point(243, 8);
        btnReset.Margin = new System.Windows.Forms.Padding(8);
        btnReset.Name = "btnReset";
        btnReset.Size = new System.Drawing.Size(173, 37);
        btnReset.TabIndex = 2;
        btnReset.Text = "Reset Defaults";
        btnReset.UseVisualStyleBackColor = true;
        btnReset.Click += btnReset_Click;
        // 
        // btnCancel
        // 
        btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        btnCancel.Location = new System.Drawing.Point(152, 8);
        btnCancel.Margin = new System.Windows.Forms.Padding(8);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new System.Drawing.Size(75, 37);
        btnCancel.TabIndex = 1;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // btnSave
        // 
        btnSave.Location = new System.Drawing.Point(62, 8);
        btnSave.Margin = new System.Windows.Forms.Padding(8);
        btnSave.Name = "btnSave";
        btnSave.Size = new System.Drawing.Size(74, 37);
        btnSave.TabIndex = 0;
        btnSave.Text = "Save";
        btnSave.UseVisualStyleBackColor = true;
        btnSave.Click += btnSave_Click;
        // 
        // SidebarSettingsForm
        // 
        AcceptButton = btnSave;
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton = btnCancel;
        ClientSize = new System.Drawing.Size(448, 253);
        Controls.Add(layoutMain);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SidebarSettingsForm";
        Padding = new System.Windows.Forms.Padding(12);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Sidebar Settings";
        Load += SidebarSettingsForm_Load;
        layoutMain.ResumeLayout(false);
        layoutMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudMinWidth).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudAutoHideDelay).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudLeftEdgePixels).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudAnimationStep).EndInit();
        footerButtons.ResumeLayout(false);
        ResumeLayout(false);
    }
}

