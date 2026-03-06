using System;
using System.Windows.Forms;

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
        UiTheme.ApplyLabelFont(UiTheme.LabelFont, lblMinWidth, lblAutoHideDelay, lblLeftEdge, lblAnimationStep);
        UiTheme.StyleSecondaryButtons(btnReset, btnCancel);
        UiTheme.StylePrimaryButton(btnSave);
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
}
