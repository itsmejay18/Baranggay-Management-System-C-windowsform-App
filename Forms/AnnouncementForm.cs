using System;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class AnnouncementForm : Form
    {
        private readonly AnnouncementFormController _controller;

        public AnnouncementForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            _controller = new AnnouncementFormController(this);
            _controller.Initialize();
            UiTheme.StandardizeButtonLayout(this);
        }

        internal string GetTitleText() => txtTitle.Text.Trim();
        internal string GetMessageText() => txtMessage.Text.Trim();
        internal string GetPriority() => cmbPriority.SelectedItem?.ToString() ?? "Normal";
        internal string GetStatus() => cmbStatus.SelectedItem?.ToString() ?? "Published";
        internal bool GetPinned() => chkPinned.Checked;

        internal void CloseWithSuccess()
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
