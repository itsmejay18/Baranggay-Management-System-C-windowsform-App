using System;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1
{
    public partial class UsersListForm : Form
    {
        private readonly UsersListFormController _controller;

        public UsersListForm()
        {
            InitializeComponent();
            _controller = new UsersListFormController(this);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Text = "User List";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 600);
            BackColor = UiTheme.Slate50;
            Font = UiTheme.BodyFont;

            UiTheme.ApplyLabelFont(UiTheme.LabelFont, labelTitle, labelSearch, labelRole, labelStatus);
            labelTitle.Font = new Font(UiTheme.HeadingFont.FontFamily, 14f, FontStyle.Bold);
            labelTitle.ForeColor = UiTheme.Slate900;

            UiTheme.StyleTextBoxes(txtSearch);
            UiTheme.StyleComboBoxes(cmbRole, cmbStatus);
            UiTheme.StyleSecondaryButtons(btnRefresh, btnEdit);
            UiTheme.StyleGhostButton(btnClose);

            UiTheme.StyleGrid(gridUsers);
            UiTheme.StandardizeButtonLayout(this);
        }

        private void UsersListForm_Load(object sender, EventArgs e)
        {
            _controller.LoadUsers();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _controller.LoadUsers();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            _controller.EditSelected();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _controller.LoadUsers();
        }

        private void cmbRole_SelectedIndexChanged(object sender, EventArgs e)
        {
            _controller.LoadUsers();
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            _controller.LoadUsers();
        }

        internal string SearchText => txtSearch.Text;
        internal string RoleFilter => cmbRole.SelectedItem?.ToString() ?? "All";
        internal string StatusFilter => cmbStatus.SelectedItem?.ToString() ?? "All";
        internal DataGridView UsersGrid => gridUsers;
    }
}
