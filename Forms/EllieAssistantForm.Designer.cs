namespace baranggaysystem1;

partial class EllieAssistantForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Panel panelHeader;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Label lblSubtitle;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.SplitContainer splitMain;
    private System.Windows.Forms.Panel panelAvatarCard;
    private System.Windows.Forms.PictureBox picEllie;
    private System.Windows.Forms.Label lblEllieName;
    private System.Windows.Forms.Label lblEllieTagline;
    private System.Windows.Forms.Panel panelPrompt;
    private System.Windows.Forms.TableLayoutPanel promptLayout;
    private System.Windows.Forms.Label lblPrompt;
    private System.Windows.Forms.Label lblPromptHint;
    private System.Windows.Forms.FlowLayoutPanel quickActionsPanel;
    private System.Windows.Forms.Button btnQuickFeatures;
    private System.Windows.Forms.Button btnQuickBlotter;
    private System.Windows.Forms.Button btnQuickCertificates;
    private System.Windows.Forms.RichTextBox chatBox;
    private System.Windows.Forms.Panel inputPanel;
    private System.Windows.Forms.TextBox txtQuestion;
    private System.Windows.Forms.Button btnSend;
    private System.Windows.Forms.Button btnClear;

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
        panelHeader = new System.Windows.Forms.Panel();
        lblStatus = new System.Windows.Forms.Label();
        lblSubtitle = new System.Windows.Forms.Label();
        lblTitle = new System.Windows.Forms.Label();
        splitMain = new System.Windows.Forms.SplitContainer();
        panelAvatarCard = new System.Windows.Forms.Panel();
        lblEllieTagline = new System.Windows.Forms.Label();
        lblEllieName = new System.Windows.Forms.Label();
        picEllie = new System.Windows.Forms.PictureBox();
        panelPrompt = new System.Windows.Forms.Panel();
        promptLayout = new System.Windows.Forms.TableLayoutPanel();
        lblPrompt = new System.Windows.Forms.Label();
        lblPromptHint = new System.Windows.Forms.Label();
        inputPanel = new System.Windows.Forms.Panel();
        txtQuestion = new System.Windows.Forms.TextBox();
        btnClear = new System.Windows.Forms.Button();
        btnSend = new System.Windows.Forms.Button();
        chatBox = new System.Windows.Forms.RichTextBox();
        quickActionsPanel = new System.Windows.Forms.FlowLayoutPanel();
        btnQuickFeatures = new System.Windows.Forms.Button();
        btnQuickBlotter = new System.Windows.Forms.Button();
        btnQuickCertificates = new System.Windows.Forms.Button();
        panelHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        panelAvatarCard.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picEllie).BeginInit();
        panelPrompt.SuspendLayout();
        promptLayout.SuspendLayout();
        inputPanel.SuspendLayout();
        quickActionsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // panelHeader
        // 
        panelHeader.BackColor = System.Drawing.Color.White;
        panelHeader.Controls.Add(lblStatus);
        panelHeader.Controls.Add(lblSubtitle);
        panelHeader.Controls.Add(lblTitle);
        panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
        panelHeader.Location = new System.Drawing.Point(0, 0);
        panelHeader.Name = "panelHeader";
        panelHeader.Padding = new System.Windows.Forms.Padding(18, 12, 18, 10);
        panelHeader.Size = new System.Drawing.Size(1080, 90);
        panelHeader.TabIndex = 0;
        // 
        // lblStatus
        // 
        lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        lblStatus.AutoSize = true;
        lblStatus.Location = new System.Drawing.Point(962, 16);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new System.Drawing.Size(43, 20);
        lblStatus.TabIndex = 2;
        lblStatus.Text = "Ready";
        // 
        // lblSubtitle
        // 
        lblSubtitle.AutoSize = true;
        lblSubtitle.Location = new System.Drawing.Point(20, 48);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new System.Drawing.Size(371, 20);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Ask about dashboard, residents, blotter, certificates, and reports.";
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
        lblTitle.Location = new System.Drawing.Point(16, 12);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new System.Drawing.Size(164, 32);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Ellie Assistant";
        // 
        // splitMain
        // 
        splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
        splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
        splitMain.IsSplitterFixed = true;
        splitMain.Location = new System.Drawing.Point(0, 90);
        splitMain.Name = "splitMain";
        // 
        // splitMain.Panel1
        // 
        splitMain.Panel1.Controls.Add(panelAvatarCard);
        splitMain.Panel1Collapsed = true;
        // 
        // splitMain.Panel2
        // 
        splitMain.Panel2.Controls.Add(inputPanel);
        splitMain.Panel2.Controls.Add(chatBox);
        splitMain.Panel2.Controls.Add(panelPrompt);
        splitMain.Panel2.Controls.Add(quickActionsPanel);
        splitMain.Size = new System.Drawing.Size(1080, 590);
        splitMain.SplitterDistance = 0;
        splitMain.SplitterWidth = 1;
        splitMain.TabIndex = 1;
        // 
        // panelAvatarCard
        // 
        panelAvatarCard.BackColor = System.Drawing.Color.White;
        panelAvatarCard.Controls.Add(lblEllieTagline);
        panelAvatarCard.Controls.Add(lblEllieName);
        panelAvatarCard.Controls.Add(picEllie);
        panelAvatarCard.Dock = System.Windows.Forms.DockStyle.Fill;
        panelAvatarCard.Location = new System.Drawing.Point(0, 0);
        panelAvatarCard.Name = "panelAvatarCard";
        panelAvatarCard.Padding = new System.Windows.Forms.Padding(20, 18, 20, 18);
        panelAvatarCard.Size = new System.Drawing.Size(320, 590);
        panelAvatarCard.TabIndex = 0;
        panelAvatarCard.Visible = false;
        // 
        // lblEllieTagline
        // 
        lblEllieTagline.AutoSize = true;
        lblEllieTagline.Location = new System.Drawing.Point(24, 550);
        lblEllieTagline.Name = "lblEllieTagline";
        lblEllieTagline.Size = new System.Drawing.Size(237, 20);
        lblEllieTagline.TabIndex = 2;
        lblEllieTagline.Text = "Your local Barangay AI companion";
        // 
        // lblEllieName
        // 
        lblEllieName.AutoSize = true;
        lblEllieName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        lblEllieName.Location = new System.Drawing.Point(22, 516);
        lblEllieName.Name = "lblEllieName";
        lblEllieName.Size = new System.Drawing.Size(54, 28);
        lblEllieName.TabIndex = 1;
        lblEllieName.Text = "Ellie";
        // 
        // picEllie
        // 
        picEllie.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        picEllie.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        picEllie.Location = new System.Drawing.Point(24, 22);
        picEllie.Name = "picEllie";
        picEllie.Size = new System.Drawing.Size(272, 486);
        picEllie.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        picEllie.TabIndex = 0;
        picEllie.TabStop = false;
        // 
        // panelPrompt
        // 
        panelPrompt.BackColor = System.Drawing.Color.White;
        panelPrompt.Controls.Add(promptLayout);
        panelPrompt.Dock = System.Windows.Forms.DockStyle.Fill;
        panelPrompt.Location = new System.Drawing.Point(0, 56);
        panelPrompt.Name = "panelPrompt";
        panelPrompt.Padding = new System.Windows.Forms.Padding(24, 24, 24, 24);
        panelPrompt.Size = new System.Drawing.Size(790, 455);
        panelPrompt.TabIndex = 3;
        // 
        // promptLayout
        // 
        promptLayout.ColumnCount = 1;
        promptLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        promptLayout.Controls.Add(lblPrompt, 0, 1);
        promptLayout.Controls.Add(lblPromptHint, 0, 2);
        promptLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        promptLayout.Location = new System.Drawing.Point(24, 24);
        promptLayout.Name = "promptLayout";
        promptLayout.RowCount = 4;
        promptLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
        promptLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        promptLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        promptLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
        promptLayout.Size = new System.Drawing.Size(742, 407);
        promptLayout.TabIndex = 0;
        // 
        // lblPrompt
        // 
        lblPrompt.Anchor = System.Windows.Forms.AnchorStyles.None;
        lblPrompt.AutoSize = true;
        lblPrompt.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Regular);
        lblPrompt.Location = new System.Drawing.Point(233, 173);
        lblPrompt.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new System.Drawing.Size(275, 41);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "What can I help with?";
        // 
        // lblPromptHint
        // 
        lblPromptHint.Anchor = System.Windows.Forms.AnchorStyles.None;
        lblPromptHint.AutoSize = true;
        lblPromptHint.Location = new System.Drawing.Point(171, 220);
        lblPromptHint.Name = "lblPromptHint";
        lblPromptHint.Size = new System.Drawing.Size(400, 20);
        lblPromptHint.TabIndex = 1;
        lblPromptHint.Text = "Ask Ellie about dashboard, residents, blotter, certificates, reports.";
        // 
        // inputPanel
        // 
        inputPanel.BackColor = System.Drawing.Color.White;
        inputPanel.Controls.Add(txtQuestion);
        inputPanel.Controls.Add(btnClear);
        inputPanel.Controls.Add(btnSend);
        inputPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
        inputPanel.Location = new System.Drawing.Point(0, 511);
        inputPanel.Name = "inputPanel";
        inputPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
        inputPanel.Size = new System.Drawing.Size(790, 64);
        inputPanel.TabIndex = 2;
        // 
        // txtQuestion
        // 
        txtQuestion.Dock = System.Windows.Forms.DockStyle.Fill;
        txtQuestion.Location = new System.Drawing.Point(12, 10);
        txtQuestion.Name = "txtQuestion";
        txtQuestion.PlaceholderText = "Ask Ellie anything about your system...";
        txtQuestion.Size = new System.Drawing.Size(602, 27);
        txtQuestion.TabIndex = 0;
        // 
        // btnClear
        // 
        btnClear.Dock = System.Windows.Forms.DockStyle.Right;
        btnClear.Location = new System.Drawing.Point(580, 10);
        btnClear.Name = "btnClear";
        btnClear.Size = new System.Drawing.Size(80, 44);
        btnClear.TabIndex = 2;
        btnClear.Text = "Clear";
        btnClear.UseVisualStyleBackColor = true;
        btnClear.Click += btnClear_Click;
        // 
        // btnSend
        // 
        btnSend.Dock = System.Windows.Forms.DockStyle.Right;
        btnSend.Location = new System.Drawing.Point(660, 10);
        btnSend.Name = "btnSend";
        btnSend.Size = new System.Drawing.Size(84, 44);
        btnSend.TabIndex = 1;
        btnSend.Text = "Send";
        btnSend.UseVisualStyleBackColor = true;
        btnSend.Click += btnSend_Click;
        // 
        // chatBox
        // 
        chatBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
        chatBox.Dock = System.Windows.Forms.DockStyle.Fill;
        chatBox.Location = new System.Drawing.Point(0, 56);
        chatBox.Name = "chatBox";
        chatBox.ReadOnly = true;
        chatBox.Size = new System.Drawing.Size(790, 455);
        chatBox.TabIndex = 1;
        chatBox.Text = "";
        // 
        // quickActionsPanel
        // 
        quickActionsPanel.BackColor = System.Drawing.Color.White;
        quickActionsPanel.Controls.Add(btnQuickFeatures);
        quickActionsPanel.Controls.Add(btnQuickBlotter);
        quickActionsPanel.Controls.Add(btnQuickCertificates);
        quickActionsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
        quickActionsPanel.Location = new System.Drawing.Point(0, 455);
        quickActionsPanel.Name = "quickActionsPanel";
        quickActionsPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
        quickActionsPanel.Size = new System.Drawing.Size(790, 56);
        quickActionsPanel.TabIndex = 0;
        // 
        // btnQuickFeatures
        // 
        btnQuickFeatures.Location = new System.Drawing.Point(15, 13);
        btnQuickFeatures.Name = "btnQuickFeatures";
        btnQuickFeatures.Size = new System.Drawing.Size(180, 32);
        btnQuickFeatures.TabIndex = 0;
        btnQuickFeatures.Text = "Company knowledge";
        btnQuickFeatures.UseVisualStyleBackColor = true;
        btnQuickFeatures.Click += btnQuickFeatures_Click;
        // 
        // btnQuickBlotter
        // 
        btnQuickBlotter.Location = new System.Drawing.Point(187, 13);
        btnQuickBlotter.Name = "btnQuickBlotter";
        btnQuickBlotter.Size = new System.Drawing.Size(166, 32);
        btnQuickBlotter.TabIndex = 1;
        btnQuickBlotter.Text = "Blotter guide";
        btnQuickBlotter.UseVisualStyleBackColor = true;
        btnQuickBlotter.Click += btnQuickBlotter_Click;
        btnQuickBlotter.Visible = false;
        // 
        // btnQuickCertificates
        // 
        btnQuickCertificates.Location = new System.Drawing.Point(359, 13);
        btnQuickCertificates.Name = "btnQuickCertificates";
        btnQuickCertificates.Size = new System.Drawing.Size(166, 32);
        btnQuickCertificates.TabIndex = 2;
        btnQuickCertificates.Text = "Certificate flow";
        btnQuickCertificates.UseVisualStyleBackColor = true;
        btnQuickCertificates.Click += btnQuickCertificates_Click;
        btnQuickCertificates.Visible = false;
        // 
        // EllieAssistantForm
        // 
        AcceptButton = btnSend;
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1080, 680);
        Controls.Add(splitMain);
        Controls.Add(panelHeader);
        MinimumSize = new System.Drawing.Size(900, 620);
        Name = "EllieAssistantForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "Ellie Assistant";
        Load += EllieAssistantForm_Load;
        panelHeader.ResumeLayout(false);
        panelHeader.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        panelAvatarCard.ResumeLayout(false);
        panelAvatarCard.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)picEllie).EndInit();
        panelPrompt.ResumeLayout(false);
        promptLayout.ResumeLayout(false);
        promptLayout.PerformLayout();
        inputPanel.ResumeLayout(false);
        inputPanel.PerformLayout();
        quickActionsPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
