using System;

using System.ComponentModel;
using System.Drawing;
using System.IO;
using baranggaysystem1.helper;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;



namespace baranggaysystem1;



public partial class RegisterForm : Form
{
	private System.Windows.Forms.Timer? _fadeTimer;
	private readonly RegisterFormController _controller;
	private string? _photoPath;

	internal event Action? BackToLoginRequested;
	internal event Action? RegistrationCompleted;


	public RegisterForm()
	{
		InitializeComponent();
		_controller = new RegisterFormController(this);
		ApplyRegisterTheme();
	}


	private void RegisterButton_Click(object? sender, EventArgs e)
	{
		_controller.HandleRegister();
	}


	private void BackToLoginButton_Click(object? sender, EventArgs e)
	{
		_controller.HandleBackToLogin();
	}

	private void buttonPhotoUpload_Click(object sender, EventArgs e)
	{
		_controller.HandleUploadPhoto();
	}

	private void buttonPhotoRemove_Click(object sender, EventArgs e)
	{
		_controller.HandleRemovePhoto();
	}


	private void RegisterForm_Load(object? sender, EventArgs e)
	{
		_controller.HandleLoad();
	}


	private void ApplyRegisterTheme()

	{

		UiTheme.AttachGradient(panelLeft, UiTheme.Slate900, UiTheme.Slate700, 90f);

		UiTheme.AttachGradient(panelRight, UiTheme.Slate50, UiTheme.Slate100, 90f);

		UiTheme.ApplyCardStyle(panelCard);

		labelBrand.Font = new Font("Century Gothic", 26f, FontStyle.Bold);

		labelBrand.ForeColor = Color.White;

		labelBrand.BackColor = Color.Transparent;

		UiTheme.ApplyLabelFont(UiTheme.LabelFont, labelTagline, labelSubtitle, label1, label2, labelRole, label3);

		labelTagline.ForeColor = UiTheme.Slate300;

		labelTagline.BackColor = Color.Transparent;

		label4.Text = "Create Account";

		label4.Font = new Font("Century Gothic", 16f, FontStyle.Bold);

		label4.ForeColor = UiTheme.Slate900;

		labelSubtitle.Text = "Add a new user and assign a role";

		labelSubtitle.ForeColor = UiTheme.Slate500;

		label1.Text = "Username";

		label1.ForeColor = UiTheme.Slate700;

		label2.Text = "Password";

		label2.ForeColor = UiTheme.Slate700;

		labelRole.Text = "Role";

		labelRole.ForeColor = UiTheme.Slate700;

		labelPhoto.Text = "Photo";
		labelPhoto.ForeColor = UiTheme.Slate700;

		UiTheme.StyleTextBoxes(txtUsername, txtPassword);

		txtUsername.PlaceholderText = "Choose a username";


		txtPassword.UseSystemPasswordChar = true;

		txtPassword.PlaceholderText = "Create a password";

		UiTheme.StyleComboBoxes(cmbRole);

		cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;

		if (cmbRole.Items.Count > 0 && cmbRole.SelectedIndex < 0)

		{
			int staffIndex = cmbRole.FindStringExact("Staff");
			cmbRole.SelectedIndex = staffIndex >= 0 ? staffIndex : 0;

		}

		UiTheme.StylePrimaryButtons(registerButton);

		registerButton.Text = "Register";

		UiTheme.StylePrimaryButtons(buttonPhotoUpload);
		buttonPhotoUpload.Text = "Upload";
		UiTheme.StyleGhostButton(buttonPhotoRemove);
		buttonPhotoRemove.Text = "Remove";
		photoPreview.BackColor = UiTheme.Slate50;

		SetPhotoPath(null);

		label3.Text = "Already have an account?";

		label3.ForeColor = UiTheme.Slate500;

		UiTheme.StyleGhostButton(backToLoginButton);

		backToLoginButton.Text = "Log in";

		base.AcceptButton = registerButton;

		Font = UiTheme.BodyFont;

		BackColor = UiTheme.Slate50;

		Text = "Barangay System - Register";

		MinimumSize = new Size(1040, 640);

		base.StartPosition = FormStartPosition.CenterScreen;

		base.Load -= RegisterForm_Load;

		base.Load += RegisterForm_Load;
		UiTheme.StandardizeButtonLayout(this);

	}

	internal string? GetPhotoPath()
	{
		return _photoPath;
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



	private void StartFadeIn()

	{

		base.Opacity = 0.0;

		_fadeTimer?.Stop();

		_fadeTimer = new System.Windows.Forms.Timer
		{

			Interval = 15

		};

		_fadeTimer.Tick += delegate

		{

			base.Opacity += 0.06;

			if (base.Opacity >= 1.0)

			{

				base.Opacity = 1.0;

				_fadeTimer?.Stop();

			}

		};

		_fadeTimer.Start();

	}

	internal void ReturnToLogin()
	{
		BackToLoginRequested?.Invoke();
	}

	internal void CompleteRegistration()
	{
		RegistrationCompleted?.Invoke();
	}



	



	

}



