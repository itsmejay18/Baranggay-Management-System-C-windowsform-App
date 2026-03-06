using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public class StaffDashboard : Form
{
	private IContainer components = null;

	private Panel panelTop;

	private Panel panelBody;

	private Label labelTitle;

	private Label labelSubtitle;

	private Label labelInfo;

	public StaffDashboard()
	{
		InitializeComponent();
		base.WindowState = FormWindowState.Maximized;
		base.StartPosition = FormStartPosition.CenterScreen;
		ApplyStaffTheme();
	}

	private void ApplyStaffTheme()
	{
		BackColor = UiTheme.Slate100;
		Font = UiTheme.BodyFont;
		Text = "Barangay System - Staff";
		UiTheme.AttachGradient(panelTop, Color.White, UiTheme.Slate50, 90f);
		labelTitle.Text = "Staff Dashboard";
		labelTitle.Font = UiTheme.HeadingFont;
		labelTitle.ForeColor = UiTheme.Slate900;
		labelSubtitle.Text = "Quick access to daily tasks and records";
		labelSubtitle.Font = UiTheme.LabelFont;
		labelSubtitle.ForeColor = UiTheme.Slate500;
		labelInfo.Text = "Use the tools provided to manage resident services and requests.";
		labelInfo.Font = UiTheme.BodyFont;
		labelInfo.ForeColor = UiTheme.Slate700;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.panelTop = new System.Windows.Forms.Panel();
		this.labelTitle = new System.Windows.Forms.Label();
		this.labelSubtitle = new System.Windows.Forms.Label();
		this.panelBody = new System.Windows.Forms.Panel();
		this.labelInfo = new System.Windows.Forms.Label();
		this.panelTop.SuspendLayout();
		this.panelBody.SuspendLayout();
		base.SuspendLayout();
		this.panelTop.BackColor = System.Drawing.Color.White;
		this.panelTop.Controls.Add(this.labelSubtitle);
		this.panelTop.Controls.Add(this.labelTitle);
		this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
		this.panelTop.Location = new System.Drawing.Point(0, 0);
		this.panelTop.Name = "panelTop";
		this.panelTop.Size = new System.Drawing.Size(800, 72);
		this.panelTop.TabIndex = 0;
		this.labelTitle.AutoSize = true;
		this.labelTitle.Location = new System.Drawing.Point(24, 14);
		this.labelTitle.Name = "labelTitle";
		this.labelTitle.Size = new System.Drawing.Size(95, 15);
		this.labelTitle.TabIndex = 0;
		this.labelTitle.Text = "Staff Dashboard";
		this.labelSubtitle.AutoSize = true;
		this.labelSubtitle.Location = new System.Drawing.Point(24, 40);
		this.labelSubtitle.Name = "labelSubtitle";
		this.labelSubtitle.Size = new System.Drawing.Size(224, 15);
		this.labelSubtitle.TabIndex = 1;
		this.labelSubtitle.Text = "Quick access to daily tasks and records";
		this.panelBody.Controls.Add(this.labelInfo);
		this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panelBody.Location = new System.Drawing.Point(0, 72);
		this.panelBody.Name = "panelBody";
		this.panelBody.Padding = new System.Windows.Forms.Padding(24);
		this.panelBody.Size = new System.Drawing.Size(800, 378);
		this.panelBody.TabIndex = 1;
		this.labelInfo.AutoSize = true;
		this.labelInfo.Location = new System.Drawing.Point(24, 24);
		this.labelInfo.MaximumSize = new System.Drawing.Size(520, 0);
		this.labelInfo.Name = "labelInfo";
		this.labelInfo.Size = new System.Drawing.Size(373, 15);
		this.labelInfo.TabIndex = 0;
		this.labelInfo.Text = "Use the tools provided to manage resident services and requests.";
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(800, 450);
		base.Controls.Add(this.panelBody);
		base.Controls.Add(this.panelTop);
		base.Name = "StaffDashboard";
		this.Text = "Barangay System - Staff";
		this.panelTop.ResumeLayout(false);
		this.panelTop.PerformLayout();
		this.panelBody.ResumeLayout(false);
		this.panelBody.PerformLayout();
		base.ResumeLayout(false);
	}
}
