using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public partial class StaffDashboard : Form
{
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

		UiTheme.StandardizeButtonLayout(this);
	}

	

	
}


