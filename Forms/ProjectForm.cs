using System;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class ProjectForm : Form
    {
        private readonly ProjectFormController _controller;

        public ProjectForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            _controller = new ProjectFormController(this);
            _controller.Initialize();
            UiTheme.StandardizeButtonLayout(this);
        }

        internal string GetNameText() => txtName.Text.Trim();
        internal string GetStatus() => cmbStatus.SelectedItem?.ToString() ?? "Planned";
        internal decimal GetBudget() => numBudget.Value;
        internal DateTime? GetStartDate() => dtpStartDate.Checked ? dtpStartDate.Value.Date : null;
        internal DateTime? GetEndDate() => dtpEndDate.Checked ? dtpEndDate.Value.Date : null;
        internal string GetLead() => txtLead.Text.Trim();
        internal string GetRemarks() => txtRemarks.Text.Trim();

        internal void CloseWithSuccess()
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
