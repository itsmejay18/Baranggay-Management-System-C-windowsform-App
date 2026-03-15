using System;

using System.ComponentModel;

using System.Drawing;

using System.Windows.Forms;

using MySql.Data.MySqlClient;

using baranggaysystem1.Database;

using baranggaysystem1.helper;



namespace baranggaysystem1;



public partial class Form1 : Form
{
	private System.Windows.Forms.Timer? _fadeTimer;
	private readonly Form1Controller _controller;
	private bool _isLoginInProgress;

	internal event Action<Form>? LoginSucceeded;
	internal event Action? RegisterRequested;


	public Form1()
	{
		InitializeComponent();
		_controller = new Form1Controller(this);
		ApplyLoginTheme();
	}


	private void button1_Click(object sender, EventArgs e)
	{
		if (_isLoginInProgress)
		{
			return;
		}

		_controller.HandleLoginAsync();
	}


	private void button1_Click_1(object sender, EventArgs e)
	{
		_controller.HandleRegister();
	}


	private void Form1_Load(object sender, EventArgs e)
	{
		_controller.HandleLoad();
	}


	private void ApplyLoginTheme()

	{

		UiTheme.AttachGradient(panelLeft, UiTheme.Slate900, UiTheme.Slate700, 90f);

		UiTheme.AttachGradient(panelRight, UiTheme.Slate50, UiTheme.Slate100, 90f);

		UiTheme.ApplyCardStyle(panelCard);

		labelBrand.Font = new Font("Century Gothic", 26f, FontStyle.Bold);

		labelBrand.ForeColor = Color.White;

		labelBrand.BackColor = Color.Transparent;

		UiTheme.ApplyLabelFont(UiTheme.LabelFont, labelTagline, labelSubtitle, label1, label2, label3);

		labelTagline.ForeColor = UiTheme.Slate300;

		labelTagline.BackColor = Color.Transparent;

		label4.Text = "Welcome Back";

		label4.Font = new Font("Century Gothic", 16f, FontStyle.Bold);

		label4.ForeColor = UiTheme.Slate900;

		labelSubtitle.Text = "Sign in to continue";

		labelSubtitle.ForeColor = UiTheme.Slate500;

		label1.Text = "Username";

		label1.ForeColor = UiTheme.Slate700;

		label2.Text = "Password";

		label2.ForeColor = UiTheme.Slate700;

		UiTheme.StyleTextBoxes(txtUsername, txtPassword);

		txtUsername.PlaceholderText = "Enter your username";


		txtPassword.UseSystemPasswordChar = true;

		txtPassword.PlaceholderText = "Enter your password";

		UiTheme.StylePrimaryButtons(ss);

		ss.Text = "Log in";

		label3.Text = "Don't have an account?";

		label3.ForeColor = UiTheme.Slate500;

		UiTheme.StyleGhostButton(button1);

		button1.Text = "Register";

		base.AcceptButton = ss;

		Font = UiTheme.BodyFont;

		BackColor = UiTheme.Slate50;

		Text = "Barangay System - Login";

		MinimumSize = new Size(1040, 600);

		base.StartPosition = FormStartPosition.CenterScreen;
		UiTheme.StandardizeButtonLayout(this);

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

	internal void CompleteLogin(Form destinationForm)
	{
		_isLoginInProgress = false;
		HideLoginProgress();
		LoginSucceeded?.Invoke(destinationForm);
	}

	internal void OpenRegister()
	{
		RegisterRequested?.Invoke();
	}

	internal void ShowLoginProgress()
	{
		_isLoginInProgress = true;
		if (InvokeRequired)
		{
			BeginInvoke(new Action(ShowLoginProgress));
			return;
		}

		txtUsername.Enabled = false;
		txtPassword.Enabled = false;
		ss.Enabled = false;
		button1.Enabled = false;
		ss.Text = "Signing in...";
	}

	internal void HideLoginProgress()
	{
		_isLoginInProgress = false;
		if (InvokeRequired)
		{
			BeginInvoke(new Action(HideLoginProgress));
			return;
		}

		txtUsername.Enabled = true;
		txtPassword.Enabled = true;
		ss.Enabled = true;
		button1.Enabled = true;
		ss.Text = "Log in";
	}



	



	

}



