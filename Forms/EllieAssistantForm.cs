using System;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public partial class EllieAssistantForm : Form
{
    private readonly EllieAssistantFormController _controller;

    public EllieAssistantForm()
    {
        InitializeComponent();
        _controller = new EllieAssistantFormController(this);
    }

    private void EllieAssistantForm_Load(object sender, EventArgs e)
    {
        _controller.Initialize();
    }

    private async void btnSend_Click(object sender, EventArgs e)
    {
        await _controller.SendAsync().ConfigureAwait(true);
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        _controller.ClearConversation();
    }

    private async void btnQuickFeatures_Click(object sender, EventArgs e)
    {
        await _controller.SendPresetAsync("What features can I use in this Barangay System?").ConfigureAwait(true);
    }

    private async void btnQuickBlotter_Click(object sender, EventArgs e)
    {
        await _controller.SendPresetAsync("How do I file and review blotter records properly?").ConfigureAwait(true);
    }

    private async void btnQuickCertificates_Click(object sender, EventArgs e)
    {
        await _controller.SendPresetAsync("How do certificates flow from Requested to Issued?").ConfigureAwait(true);
    }

    internal string CurrentQuestion => txtQuestion.Text.Trim();

    internal void SetQuestion(string question)
    {
        txtQuestion.Text = question;
    }

    internal void ClearQuestion()
    {
        txtQuestion.Clear();
        txtQuestion.Focus();
    }

    internal void SetBusy(bool busy)
    {
        btnSend.Enabled = !busy;
        btnClear.Enabled = !busy;
        btnQuickFeatures.Enabled = !busy;
        btnQuickBlotter.Enabled = !busy;
        btnQuickCertificates.Enabled = !busy;
        txtQuestion.Enabled = !busy;
        lblStatus.Text = busy ? "Thinking..." : "Ready";
        lblStatus.ForeColor = busy ? UiTheme.AccentBlue : UiTheme.Slate500;
    }

    internal void SetPromptVisible(bool visible)
    {
        panelPrompt.Visible = visible;
        if (visible)
        {
            panelPrompt.BringToFront();
        }
    }

    internal void AppendMessage(string speaker, string message, Color speakerColor)
    {
        SetPromptVisible(false);
        if (chatBox.TextLength > 0)
        {
            chatBox.AppendText(Environment.NewLine + Environment.NewLine);
        }

        chatBox.SelectionStart = chatBox.TextLength;
        chatBox.SelectionColor = speakerColor;
        chatBox.SelectionFont = new Font(chatBox.Font, FontStyle.Bold);
        chatBox.AppendText($"{speaker}  {DateTime.Now:hh:mm tt}");

        chatBox.SelectionStart = chatBox.TextLength;
        chatBox.SelectionColor = UiTheme.Slate900;
        chatBox.SelectionFont = new Font(chatBox.Font, FontStyle.Regular);
        chatBox.AppendText(Environment.NewLine + message.Trim());
        chatBox.ScrollToCaret();
    }

    internal void ClearConversationBody()
    {
        chatBox.Clear();
        SetPromptVisible(true);
    }
}
