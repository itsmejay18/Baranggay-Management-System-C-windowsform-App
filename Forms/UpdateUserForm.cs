using System;
using System.Drawing;
using System.IO;
using System.Net.Mail;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public partial class UpdateUserForm : Form
    {
        private readonly UpdateUserFormController _controller;
        private string? _photoPath;
        private readonly int _userId;
        private readonly ErrorProvider _errorProvider = new ErrorProvider();
        private bool _isDirty;
        private bool _isLoadingUser;
        private bool _suppressDirtyEvents;

        public UpdateUserForm(int userId)
        {
            InitializeComponent();
            _userId = userId;
            _controller = new UpdateUserFormController(this, userId);
            ApplyTheme();
            WireInputChangeTracking();
            UpdateFormUxState();
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
            txtFirstName.PlaceholderText = "First name";
            txtMiddleName.PlaceholderText = "Middle name (optional)";
            txtLastName.PlaceholderText = "Last name";
            txtEmail.PlaceholderText = "name@domain.com";
            txtContact.PlaceholderText = "e.g. 09171234567";
            txtPosition.PlaceholderText = "Position";
            txtDepartment.PlaceholderText = "Department";
            txtLastProject.PlaceholderText = "Last project";

            photoPreview.BackColor = Color.FromArgb(246, 248, 251);
            photoPanel.BackColor = Color.White;
            photoPanel.BorderStyle = BorderStyle.FixedSingle;

            btnSave.Text = "Save Changes";
            btnCancel.Text = "Cancel";
            btnUpload.Text = "Upload";
            btnRemove.Text = "Remove";

            _suppressDirtyEvents = true;
            SetPhotoPath(null);
            _suppressDirtyEvents = false;
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

            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            _errorProvider.ContainerControl = this;
        }

        private void WireInputChangeTracking()
        {
            txtFirstName.TextChanged += OnInputValueChanged;
            txtMiddleName.TextChanged += OnInputValueChanged;
            txtLastName.TextChanged += OnInputValueChanged;
            txtEmail.TextChanged += OnInputValueChanged;
            txtContact.TextChanged += OnInputValueChanged;
            txtPosition.TextChanged += OnInputValueChanged;
            txtDepartment.TextChanged += OnInputValueChanged;
            txtLastProject.TextChanged += OnInputValueChanged;
            cmbRole.SelectedIndexChanged += OnInputValueChanged;
            chkActive.CheckedChanged += OnInputValueChanged;
        }

        private void OnInputValueChanged(object? sender, EventArgs e)
        {
            if (_isLoadingUser || _suppressDirtyEvents)
            {
                return;
            }

            _isDirty = true;
            UpdateFormUxState();
        }

        private void UpdateFormUxState()
        {
            bool isValid = ValidateInputs(showMessage: false, out string? message, out Control? invalidControl);
            btnSave.Enabled = _isDirty && isValid;

            if (!_isDirty)
            {
                _errorProvider.Clear();
                labelSubtitle.ForeColor = Color.FromArgb(108, 108, 108);
                labelSubtitle.Text = "Edit account details and status";
                return;
            }

            if (isValid)
            {
                _errorProvider.Clear();
                labelSubtitle.ForeColor = Color.FromArgb(180, 120, 10);
                labelSubtitle.Text = "You have unsaved changes. Press Save Changes to apply updates.";
                return;
            }

            if (invalidControl != null)
            {
                _errorProvider.SetError(invalidControl, message ?? "Invalid input.");
            }

            labelSubtitle.ForeColor = Color.FromArgb(180, 30, 40);
            labelSubtitle.Text = string.IsNullOrWhiteSpace(message)
                ? "Please fix the highlighted fields."
                : message;
        }

        private bool ValidateInputs(bool showMessage, out string? message, out Control? invalidControl)
        {
            message = null;
            invalidControl = null;
            _errorProvider.Clear();

            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                message = "First name is required.";
                invalidControl = txtFirstName;
            }
            else if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                message = "Last name is required.";
                invalidControl = txtLastName;
            }
            else if (cmbRole.SelectedItem == null)
            {
                message = "Please select a role.";
                invalidControl = cmbRole;
            }
            else if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmailAddress(txtEmail.Text.Trim()))
            {
                message = "Email format is invalid.";
                invalidControl = txtEmail;
            }
            else if (!string.IsNullOrWhiteSpace(txtContact.Text) && !LooksLikePhoneNumber(txtContact.Text.Trim()))
            {
                message = "Contact number format is invalid.";
                invalidControl = txtContact;
            }

            if (invalidControl == null)
            {
                return true;
            }

            _errorProvider.SetError(invalidControl, message ?? "Invalid input.");
            if (showMessage)
            {
                ControllerDialogs.Warning(message ?? "Please check the form values.", "Validation");
                invalidControl.Focus();
            }

            return false;
        }

        private static bool IsValidEmailAddress(string email)
        {
            try
            {
                var _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikePhoneNumber(string contact)
        {
            int digitCount = 0;
            foreach (char ch in contact)
            {
                if (char.IsDigit(ch))
                {
                    digitCount++;
                    continue;
                }

                if (ch == '+' || ch == '-' || ch == ' ' || ch == '(' || ch == ')')
                {
                    continue;
                }

                return false;
            }

            return digitCount >= 7 && digitCount <= 15;
        }

        private void MarkFormClean()
        {
            _isDirty = false;
            UpdateFormUxState();
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
            _isLoadingUser = true;
            try
            {
                _controller.LoadUser();
            }
            finally
            {
                _isLoadingUser = false;
            }

            MarkFormClean();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(showMessage: true, out _, out _))
            {
                return;
            }

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
            _suppressDirtyEvents = true;
            try
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
            finally
            {
                _suppressDirtyEvents = false;
            }

            MarkFormClean();
        }

        internal void SetPhotoPath(string? path)
        {
            bool changed = !string.Equals(_photoPath ?? string.Empty, path ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            _photoPath = path;
            var old = photoPreview.Image;
            photoPreview.Image = LoadImageSafe(path) ?? AvatarHelper.CreateDefaultAvatar(photoPreview.Size);
            old?.Dispose();

            if (changed && !_isLoadingUser && !_suppressDirtyEvents)
            {
                _isDirty = true;
                UpdateFormUxState();
            }
        }

        internal void MarkSaved()
        {
            MarkFormClean();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDirty && e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = ControllerDialogs.Confirm(
                    "You have unsaved changes. Close without saving?",
                    "Unsaved Changes",
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S) && btnSave.Enabled)
            {
                btnSave.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
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
