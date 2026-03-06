namespace baranggaysystem1
{
    partial class StaffDashboard
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelBody;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelSubtitle;
        private System.Windows.Forms.Label labelInfo;

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
            panelTop = new System.Windows.Forms.Panel();
            labelTitle = new System.Windows.Forms.Label();
            labelSubtitle = new System.Windows.Forms.Label();
            panelBody = new System.Windows.Forms.Panel();
            labelInfo = new System.Windows.Forms.Label();
            panelTop.SuspendLayout();
            panelBody.SuspendLayout();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.BackColor = System.Drawing.Color.White;
            panelTop.Controls.Add(labelSubtitle);
            panelTop.Controls.Add(labelTitle);
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Location = new System.Drawing.Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new System.Drawing.Size(800, 72);
            panelTop.TabIndex = 0;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Location = new System.Drawing.Point(24, 14);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new System.Drawing.Size(95, 15);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "Staff Dashboard";
            // 
            // labelSubtitle
            // 
            labelSubtitle.AutoSize = true;
            labelSubtitle.Location = new System.Drawing.Point(24, 40);
            labelSubtitle.Name = "labelSubtitle";
            labelSubtitle.Size = new System.Drawing.Size(224, 15);
            labelSubtitle.TabIndex = 1;
            labelSubtitle.Text = "Quick access to daily tasks and records";
            // 
            // panelBody
            // 
            panelBody.Controls.Add(labelInfo);
            panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            panelBody.Location = new System.Drawing.Point(0, 72);
            panelBody.Name = "panelBody";
            panelBody.Padding = new System.Windows.Forms.Padding(24);
            panelBody.Size = new System.Drawing.Size(800, 378);
            panelBody.TabIndex = 1;
            // 
            // labelInfo
            // 
            labelInfo.AutoSize = true;
            labelInfo.Location = new System.Drawing.Point(24, 24);
            labelInfo.MaximumSize = new System.Drawing.Size(520, 0);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new System.Drawing.Size(373, 15);
            labelInfo.TabIndex = 0;
            labelInfo.Text = "Use the tools provided to manage resident services and requests.";
            // 
            // StaffDashboard
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(panelBody);
            Controls.Add(panelTop);
            Name = "StaffDashboard";
            Text = "Barangay System - Staff";
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelBody.ResumeLayout(false);
            panelBody.PerformLayout();
            ResumeLayout(false);
        }
    }
}
