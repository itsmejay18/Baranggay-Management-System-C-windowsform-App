using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class UpdateUserForm : Form
    {
        private readonly UpdateUserFormController _controller;
        private string? _photoPath;
        private readonly int _userId;

        public UpdateUserForm(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _controller = new UpdateUserFormController(this, userId);
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Text = "Update Staff Details";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScroll = false;
            MinimumSize = new Size(560, 760);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            UiTheme.ApplyLabelFont(UiTheme.LabelFont, labelTitle, labelSubtitle, labelUsername, labelRole, labelStatus, labelPhoto,
                labelFirstName, labelMiddleName, labelLastName, labelEmail, labelContact, labelPosition, labelDepartment, labelLastProject);
            var labelFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            UiTheme.ApplyLabelFont(labelFont, labelTitle, labelSubtitle, labelUsername, labelRole, labelStatus, labelPhoto,
                labelFirstName, labelMiddleName, labelLastName, labelEmail, labelContact, labelPosition, labelDepartment, labelLastProject);
            labelTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            labelTitle.ForeColor = Color.FromArgb(28, 28, 28);
            labelSubtitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            labelSubtitle.ForeColor = Color.FromArgb(108, 108, 108);
            headerDivider.BackColor = Color.FromArgb(225, 230, 235);

            UiTheme.StyleTextBoxes(txtUsername, txtFirstName, txtMiddleName, txtLastName, txtEmail, txtContact, txtPosition, txtDepartment, txtLastProject);
            UiTheme.StyleComboBoxes(cmbRole);
            UiTheme.StylePrimaryButtons(btnSave);
            UiTheme.StyleGhostButton(btnCancel);
            UiTheme.StylePrimaryButtons(btnUpload);
            UiTheme.StyleGhostButton(btnRemove);

            txtUsername.ReadOnly = true;
            txtUsername.BackColor = Color.FromArgb(247, 248, 250);
            photoPreview.BackColor = Color.FromArgb(246, 248, 251);
            photoPanel.BackColor = Color.White;
            photoPanel.BorderStyle = BorderStyle.FixedSingle;

            btnSave.Text = "Save Changes";
            btnCancel.Text = "Cancel";
            btnUpload.Text = "Upload";
            btnRemove.Text = "Remove";

            SetPhotoPath(null);
            UiTheme.StandardizeButtonLayout(this);
            NormalizeControlHeights();
            btnCancel.Width = 120;
            btnCancel.Height = 36;
            btnSave.Width = 132;
            btnSave.Height = 36;
            btnUpload.Width = 120;
            btnUpload.Height = 32;
            btnRemove.Width = 120;
            btnRemove.Height = 32;
            btnSave.Margin = Padding.Empty;
        }

        private void NormalizeControlHeights()
        {
            TextBox[] textInputs =
            {
                txtUsername, txtFirstName, txtMiddleName, txtLastName,
                txtEmail, txtContact, txtPosition, txtDepartment, txtLastProject
            };

            foreach (var input in textInputs)
            {
                input.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
                input.Height = 30;
            }

            cmbRole.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            cmbRole.Height = 30;
        }

        private void UpdateUserForm_Load(object sender, EventArgs e)
        {
            _controller.LoadUser();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _controller.SaveUser();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            _controller.UploadPhoto();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            _controller.RemovePhoto();
        }

        internal int GetUserId() => _userId;

        internal string GetUsername() => txtUsername.Text;
        internal string GetFirstName() => txtFirstName.Text;
        internal string GetMiddleName() => txtMiddleName.Text;
        internal string GetLastName() => txtLastName.Text;
        internal string GetEmail() => txtEmail.Text;
        internal string GetContact() => txtContact.Text;
        internal string GetPosition() => txtPosition.Text;
        internal string GetDepartment() => txtDepartment.Text;
        internal string GetLastProject() => txtLastProject.Text;

        internal string GetRole() => cmbRole.SelectedItem?.ToString() ?? string.Empty;

        internal bool GetIsActive() => chkActive.Checked;

        internal string? GetPhotoPath() => _photoPath;

        internal void SetUserFields(string username, string firstName, string middleName, string lastName, string email, string contact, string position,
            string department, string lastProject, string role, bool isActive, string? photoPath)
        {
            txtUsername.Text = username;
            txtFirstName.Text = firstName;
            txtMiddleName.Text = middleName;
            txtLastName.Text = lastName;
            txtEmail.Text = email;
            txtContact.Text = contact;
            txtPosition.Text = position;
            txtDepartment.Text = department;
            txtLastProject.Text = lastProject;
            if (!string.IsNullOrWhiteSpace(role) && cmbRole.FindStringExact(role) < 0)
            {
                cmbRole.Items.Add(role);
            }
            cmbRole.SelectedItem = role;
            chkActive.Checked = isActive;
            SetPhotoPath(photoPath);
        }

        internal void SetPhotoPath(string? path)
        {
            _photoPath = path;
            var old = photoPreview.Image;
            photoPreview.Image = LoadImageSafe(path) ?? AvatarHelper.CreateDefaultAvatar(photoPreview.Size);
            old?.Dispose();
        }

        private static Image? LoadImageSafe(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Image.FromStream(stream);
        }
    }
}
