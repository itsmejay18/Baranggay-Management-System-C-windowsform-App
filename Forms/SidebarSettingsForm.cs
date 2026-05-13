using System;
using System.Windows.Forms;
using baranggaysystem1.Database;
using System.Drawing;

namespace baranggaysystem1;

public partial class SidebarSettingsForm : Form
{
    private readonly SidebarSettingsFormController _controller;

    public SidebarSettingsForm()
    {
        InitializeComponent();
        _controller = new SidebarSettingsFormController(this);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;
        UiTheme.ApplyLabelFont(UiTheme.LabelFont,
            lblMinWidth, lblAutoHideDelay, lblLeftEdge, lblAnimationStep,
            lblDbHost, lblDbPort, lblDbName, lblDbUser, lblDbPass, lblDbMode, lblDbStatus);
        lblDbModeValue.ForeColor = UiTheme.Slate700;
        UiTheme.StyleSecondaryButtons(btnReset, btnCancel);
        UiTheme.StylePrimaryButton(btnSave);
        UiTheme.StyleSecondaryButtons(btnDbTest);
        UiTheme.StylePrimaryButton(btnDbSwitchOnline);
        UiTheme.StyleDangerButton(btnDbSwitchOffline);
        SetDatabaseStatus("Connection status is not checked yet.", true, isError: false);
        UiTheme.StandardizeButtonLayout(this);
    }

    private void SidebarSettingsForm_Load(object sender, EventArgs e)
    {
        _controller.Load();
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        _controller.Save();
    }

    private void btnReset_Click(object sender, EventArgs e)
    {
        _controller.ResetToDefaults();
    }

    private void btnDbTest_Click(object sender, EventArgs e)
    {
        _controller.TestDatabaseConnection();
    }

    private void btnDbSwitchOnline_Click(object sender, EventArgs e)
    {
        _controller.SwitchToOnlineMode();
    }

    private void btnDbSwitchOffline_Click(object sender, EventArgs e)
    {
        _controller.SwitchToOfflineMode();
    }

    internal void ApplySettingsToInputs(SidebarBehaviorSettings settings)
    {
        nudMinWidth.Value = settings.MinExpandedWidth;
        nudAutoHideDelay.Value = settings.AutoHideDelayMs;
        nudLeftEdgePixels.Value = settings.LeftEdgePixels;
        nudAnimationStep.Value = settings.AnimationStep;
    }

    internal SidebarBehaviorSettings ReadSettingsFromInputs()
    {
        return new SidebarBehaviorSettings
        {
            MinExpandedWidth = (int)nudMinWidth.Value,
            AutoHideDelayMs = (int)nudAutoHideDelay.Value,
            LeftEdgePixels = (int)nudLeftEdgePixels.Value,
            AnimationStep = (int)nudAnimationStep.Value
        };
    }

    internal void ApplyDatabaseSettings(DatabaseConnectionProfile settings)
    {
        txtDbHost.Text = settings.Server;
        numDbPort.Value = settings.Port;
        txtDbName.Text = settings.Database;
        txtDbUser.Text = settings.Username;
        txtDbPass.Text = settings.Password;
    }

    internal DatabaseConnectionProfile ReadDatabaseSettings()
    {
        return new DatabaseConnectionProfile
        {
            Server = txtDbHost.Text,
            Port = (uint)numDbPort.Value,
            Database = txtDbName.Text,
            Username = txtDbUser.Text,
            Password = txtDbPass.Text
        };
    }

    internal void SetDatabaseModeLabel(bool isOffline)
    {
        lblDbModeValue.Text = isOffline ? "Offline (SQLite)" : "Online (MySQL)";
        lblDbModeValue.ForeColor = isOffline ? Color.FromArgb(217, 119, 6) : Color.FromArgb(5, 150, 105);
    }

    internal void SetDatabaseStatus(string message, bool isSuccess, bool isError)
    {
        lblDbStatus.Text = string.IsNullOrWhiteSpace(message)
            ? "No status message."
            : message.Trim();

        if (isError)
        {
            lblDbStatus.ForeColor = Color.FromArgb(185, 28, 28);
            return;
        }

        lblDbStatus.ForeColor = isSuccess ? Color.FromArgb(5, 150, 105) : UiTheme.Slate600;
    }

    internal void SetDatabaseActionBusy(bool busy)
    {
        btnDbTest.Enabled = !busy;
        btnDbSwitchOnline.Enabled = !busy;
        btnDbSwitchOffline.Enabled = !busy;
        btnSave.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
