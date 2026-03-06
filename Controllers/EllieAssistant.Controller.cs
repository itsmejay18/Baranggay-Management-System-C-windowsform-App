using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

public partial class EllieAssistantForm
{
    private sealed class EllieAssistantFormController
    {
        private readonly EllieAssistantForm _form;
        private readonly EllieAssistantService _service;
        private bool _isBusy;

        public EllieAssistantFormController(EllieAssistantForm form)
        {
            _form = form;
            _service = new EllieAssistantService();
        }

        public void Initialize()
        {
            _form.Font = UiTheme.BodyFont;
            _form.BackColor = Color.White;
            _form.panelHeader.BackColor = Color.White;
            _form.quickActionsPanel.BackColor = Color.White;
            _form.inputPanel.BackColor = Color.White;
            _form.panelPrompt.BackColor = Color.White;
            _form.chatBox.BackColor = Color.White;
            _form.chatBox.ForeColor = UiTheme.Slate900;
            _form.chatBox.Font = new Font(UiTheme.BodyFont.FontFamily, 11f, FontStyle.Regular);
            _form.chatBox.BorderStyle = BorderStyle.None;
            _form.txtQuestion.Font = new Font(UiTheme.BodyFont.FontFamily, 12f, FontStyle.Regular);
            _form.txtQuestion.BackColor = Color.White;
            _form.txtQuestion.ForeColor = UiTheme.Slate900;
            _form.txtQuestion.BorderStyle = BorderStyle.None;
            _form.lblStatus.Font = UiTheme.LabelFont;

            _form.lblTitle.Font = UiTheme.HeadingFont;
            _form.lblTitle.ForeColor = UiTheme.Slate900;
            _form.lblSubtitle.Font = UiTheme.LabelFont;
            _form.lblSubtitle.ForeColor = UiTheme.Slate600;
            _form.lblStatus.ForeColor = UiTheme.Slate500;
            _form.lblPrompt.ForeColor = UiTheme.Slate900;
            _form.lblPromptHint.ForeColor = UiTheme.Slate600;

            UiTheme.StyleSecondaryButton(_form.btnQuickFeatures);
            UiTheme.StyleSecondaryButton(_form.btnQuickBlotter);
            UiTheme.StyleSecondaryButton(_form.btnQuickCertificates);
            UiTheme.StyleSecondaryButton(_form.btnClear);
            UiTheme.StylePrimaryButton(_form.btnSend);

            _form.FormClosed -= Form_FormClosed;
            _form.FormClosed += Form_FormClosed;
            _form.SetBusy(false);
            ClearConversation();
        }

        public async Task SendAsync()
        {
            if (_isBusy)
            {
                return;
            }

            string question = _form.CurrentQuestion;
            if (string.IsNullOrWhiteSpace(question))
            {
                return;
            }

            _form.AppendMessage("You", question, Color.FromArgb(25, 118, 210));
            _form.ClearQuestion();

            _isBusy = true;
            _form.SetBusy(true);
            try
            {
                string answer = await _service.AskAsync(question).ConfigureAwait(true);
                _form.AppendMessage("Ellie", answer, Color.FromArgb(22, 163, 74));
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ellie assistant request failed.", ex);
                _form.AppendMessage("Ellie", "I hit an error: " + ex.Message, Color.FromArgb(220, 38, 38));
            }
            finally
            {
                _isBusy = false;
                _form.SetBusy(false);
            }
        }

        public async Task SendPresetAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return;
            }

            _form.SetQuestion(question);
            await SendAsync().ConfigureAwait(true);
        }

        public void ClearConversation()
        {
            _form.ClearConversationBody();
            _form.SetBusy(false);
            _form.ClearQuestion();
        }

        private void Form_FormClosed(object? sender, FormClosedEventArgs e)
        {
        }
    }
}
