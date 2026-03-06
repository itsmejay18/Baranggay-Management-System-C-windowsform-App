using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

public class Form1 : Form
{
	private Timer? _fadeTimer;

	private IContainer components = null;

	private TableLayoutPanel rootLayout;

	private Panel panelLeft;

	private Panel panelRight;

	private FlowLayoutPanel heroStack;

	private Label labelBrand;

	private Panel accentPanel;

	private Label labelTagline;

	private Panel panelCard;

	private TableLayoutPanel cardLayout;

	private TableLayoutPanel centerLayout;

	private FlowLayoutPanel registerRow;

	private Label labelSubtitle;

	private Button ss;

	private Label label1;

	private Label label2;

	private TextBox txtUsername;

	private TextBox txtPassword;

	private Button button1;

	private Label label3;

	private Label label4;

	public Form1()
	{
		InitializeComponent();
		ApplyLoginTheme();
	}

	private void button1_Click(object sender, EventArgs e)
	{
		string text = txtUsername.Text;
		string text2 = txtPassword.Text;
		string value = PasswordHelper.HashPassword(text2);
		string cmdText = "SELECT user_id, role\r\n                     FROM users\r\n                     WHERE username=@username\r\n                     AND password_hash=@password\r\n                     AND is_active=1";
		using MySqlConnection mySqlConnection = DBConnection.GetConnection();
		mySqlConnection.Open();
		MySqlCommand mySqlCommand = new MySqlCommand(cmdText, mySqlConnection);
		mySqlCommand.Parameters.AddWithValue("@username", text);
		mySqlCommand.Parameters.AddWithValue("@password", value);
		MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
		if (mySqlDataReader.Read())
		{
			UserSession.UserId = Convert.ToInt32(mySqlDataReader["user_id"]);
			UserSession.Role = mySqlDataReader["role"].ToString();
			UserSession.Username = text;
			if (UserSession.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
			{
				AdminDashboard adminDashboard = new AdminDashboard();
				adminDashboard.Show();
			}
			else
			{
				StaffDashboard staffDashboard = new StaffDashboard();
				staffDashboard.Show();
			}
			Hide();
		}
		else
		{
			MessageBox.Show("Invalid username or password");
		}
	}

	private void button1_Click_1(object sender, EventArgs e)
	{
		RegisterForm registerForm = new RegisterForm();
		registerForm.Show();
		Hide();
	}

	private void Form1_Load(object sender, EventArgs e)
	{
		StartFadeIn();
	}

	private void ApplyLoginTheme()
	{
		UiTheme.AttachGradient(panelLeft, UiTheme.Slate900, UiTheme.Slate700, 90f);
		UiTheme.AttachGradient(panelRight, UiTheme.Slate50, UiTheme.Slate100, 90f);
		UiTheme.ApplyCardStyle(panelCard);
		labelBrand.Font = new Font("Century Gothic", 26f, FontStyle.Bold);
		labelBrand.ForeColor = Color.White;
		labelBrand.BackColor = Color.Transparent;
		labelTagline.Font = UiTheme.LabelFont;
		labelTagline.ForeColor = UiTheme.Slate300;
		labelTagline.BackColor = Color.Transparent;
		label4.Text = "Welcome Back";
		label4.Font = new Font("Century Gothic", 16f, FontStyle.Bold);
		label4.ForeColor = UiTheme.Slate900;
		labelSubtitle.Text = "Sign in to continue";
		labelSubtitle.Font = UiTheme.LabelFont;
		labelSubtitle.ForeColor = UiTheme.Slate500;
		label1.Text = "Username";
		label1.Font = UiTheme.LabelFont;
		label1.ForeColor = UiTheme.Slate700;
		label2.Text = "Password";
		label2.Font = UiTheme.LabelFont;
		label2.ForeColor = UiTheme.Slate700;
		UiTheme.StyleTextBox(txtUsername);
		txtUsername.PlaceholderText = "Enter your username";
		UiTheme.StyleTextBox(txtPassword);
		txtPassword.UseSystemPasswordChar = true;
		txtPassword.PlaceholderText = "Enter your password";
		UiTheme.StylePrimaryButton(ss);
		ss.Text = "Log in";
		label3.Text = "Don't have an account?";
		label3.Font = UiTheme.LabelFont;
		label3.ForeColor = UiTheme.Slate500;
		UiTheme.StyleGhostButton(button1);
		button1.Text = "Register";
		base.AcceptButton = ss;
		Font = UiTheme.BodyFont;
		BackColor = UiTheme.Slate50;
		Text = "Barangay System - Login";
		MinimumSize = new Size(1040, 600);
		base.StartPosition = FormStartPosition.CenterScreen;
	}

	private void StartFadeIn()
	{
		base.Opacity = 0.0;
		_fadeTimer?.Stop();
		_fadeTimer = new Timer
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

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
		this.panelLeft = new System.Windows.Forms.Panel();
		this.heroStack = new System.Windows.Forms.FlowLayoutPanel();
		this.labelBrand = new System.Windows.Forms.Label();
		this.accentPanel = new System.Windows.Forms.Panel();
		this.labelTagline = new System.Windows.Forms.Label();
		this.panelRight = new System.Windows.Forms.Panel();
		this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
		this.panelCard = new System.Windows.Forms.Panel();
		this.cardLayout = new System.Windows.Forms.TableLayoutPanel();
		this.label4 = new System.Windows.Forms.Label();
		this.labelSubtitle = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.txtUsername = new System.Windows.Forms.TextBox();
		this.label2 = new System.Windows.Forms.Label();
		this.txtPassword = new System.Windows.Forms.TextBox();
		this.ss = new System.Windows.Forms.Button();
		this.registerRow = new System.Windows.Forms.FlowLayoutPanel();
		this.label3 = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.rootLayout.SuspendLayout();
		this.panelLeft.SuspendLayout();
		this.heroStack.SuspendLayout();
		this.panelRight.SuspendLayout();
		this.centerLayout.SuspendLayout();
		this.panelCard.SuspendLayout();
		this.cardLayout.SuspendLayout();
		this.registerRow.SuspendLayout();
		base.SuspendLayout();
		this.rootLayout.ColumnCount = 2;
		this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 360f));
		this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this.rootLayout.Location = new System.Drawing.Point(0, 0);
		this.rootLayout.Name = "rootLayout";
		this.rootLayout.RowCount = 1;
		this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.rootLayout.Size = new System.Drawing.Size(1040, 600);
		this.rootLayout.TabIndex = 0;
		this.rootLayout.Controls.Add(this.panelLeft, 0, 0);
		this.rootLayout.Controls.Add(this.panelRight, 1, 0);
		this.panelLeft.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);
		this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panelLeft.Padding = new System.Windows.Forms.Padding(32, 72, 32, 32);
		this.panelLeft.Controls.Add(this.heroStack);
		this.panelLeft.Name = "panelLeft";
		this.panelLeft.TabIndex = 0;
		this.heroStack.AutoSize = true;
		this.heroStack.BackColor = System.Drawing.Color.Transparent;
		this.heroStack.Dock = System.Windows.Forms.DockStyle.Top;
		this.heroStack.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
		this.heroStack.Location = new System.Drawing.Point(32, 72);
		this.heroStack.Margin = new System.Windows.Forms.Padding(0);
		this.heroStack.Name = "heroStack";
		this.heroStack.Size = new System.Drawing.Size(296, 160);
		this.heroStack.TabIndex = 0;
		this.heroStack.WrapContents = false;
		this.heroStack.Controls.Add(this.labelBrand);
		this.heroStack.Controls.Add(this.accentPanel);
		this.heroStack.Controls.Add(this.labelTagline);
		this.labelBrand.AutoSize = true;
		this.labelBrand.ForeColor = System.Drawing.Color.White;
		this.labelBrand.Location = new System.Drawing.Point(0, 0);
		this.labelBrand.Margin = new System.Windows.Forms.Padding(0);
		this.labelBrand.Name = "labelBrand";
		this.labelBrand.Size = new System.Drawing.Size(114, 30);
		this.labelBrand.TabIndex = 0;
		this.labelBrand.Text = "Barangay\r\nSystem";
		this.accentPanel.BackColor = System.Drawing.Color.White;
		this.accentPanel.Location = new System.Drawing.Point(0, 48);
		this.accentPanel.Margin = new System.Windows.Forms.Padding(0, 18, 0, 8);
		this.accentPanel.Name = "accentPanel";
		this.accentPanel.Size = new System.Drawing.Size(72, 4);
		this.accentPanel.TabIndex = 1;
		this.labelTagline.AutoSize = true;
		this.labelTagline.ForeColor = System.Drawing.Color.Silver;
		this.labelTagline.Location = new System.Drawing.Point(0, 60);
		this.labelTagline.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
		this.labelTagline.MaximumSize = new System.Drawing.Size(220, 0);
		this.labelTagline.Name = "labelTagline";
		this.labelTagline.Size = new System.Drawing.Size(220, 30);
		this.labelTagline.TabIndex = 2;
		this.labelTagline.Text = "Residents • Certificates • Services";
		this.panelRight.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
		this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panelRight.Padding = new System.Windows.Forms.Padding(56);
		this.panelRight.Controls.Add(this.centerLayout);
		this.panelRight.Name = "panelRight";
		this.panelRight.TabIndex = 1;
		this.centerLayout.ColumnCount = 1;
		this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this.centerLayout.Location = new System.Drawing.Point(56, 56);
		this.centerLayout.Name = "centerLayout";
		this.centerLayout.RowCount = 3;
		this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
		this.centerLayout.Size = new System.Drawing.Size(568, 488);
		this.centerLayout.TabIndex = 0;
		this.centerLayout.Controls.Add(this.panelCard, 0, 1);
		this.panelCard.AutoSize = true;
		this.panelCard.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.panelCard.MinimumSize = new System.Drawing.Size(420, 320);
		this.panelCard.Name = "panelCard";
		this.panelCard.TabIndex = 0;
		this.panelCard.Controls.Add(this.cardLayout);
		this.cardLayout.AutoSize = true;
		this.cardLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.cardLayout.ColumnCount = 1;
		this.cardLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.cardLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this.cardLayout.Location = new System.Drawing.Point(0, 0);
		this.cardLayout.Name = "cardLayout";
		this.cardLayout.RowCount = 8;
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.Size = new System.Drawing.Size(420, 320);
		this.cardLayout.TabIndex = 0;
		this.cardLayout.Controls.Add(this.label4, 0, 0);
		this.cardLayout.Controls.Add(this.labelSubtitle, 0, 1);
		this.cardLayout.Controls.Add(this.label1, 0, 2);
		this.cardLayout.Controls.Add(this.txtUsername, 0, 3);
		this.cardLayout.Controls.Add(this.label2, 0, 4);
		this.cardLayout.Controls.Add(this.txtPassword, 0, 5);
		this.cardLayout.Controls.Add(this.ss, 0, 6);
		this.cardLayout.Controls.Add(this.registerRow, 0, 7);
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(0, 0);
		this.label4.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(95, 15);
		this.label4.TabIndex = 0;
		this.label4.Text = "Welcome Back";
		this.labelSubtitle.AutoSize = true;
		this.labelSubtitle.Location = new System.Drawing.Point(0, 19);
		this.labelSubtitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);
		this.labelSubtitle.Name = "labelSubtitle";
		this.labelSubtitle.Size = new System.Drawing.Size(113, 15);
		this.labelSubtitle.TabIndex = 1;
		this.labelSubtitle.Text = "Sign in to continue";
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(0, 52);
		this.label1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(60, 15);
		this.label1.TabIndex = 2;
		this.label1.Text = "Username";
		this.txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
		this.txtUsername.Location = new System.Drawing.Point(0, 71);
		this.txtUsername.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
		this.txtUsername.Name = "txtUsername";
		this.txtUsername.Size = new System.Drawing.Size(420, 23);
		this.txtUsername.TabIndex = 3;
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(0, 106);
		this.label2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(57, 15);
		this.label2.TabIndex = 4;
		this.label2.Text = "Password";
		this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
		this.txtPassword.Location = new System.Drawing.Point(0, 125);
		this.txtPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
		this.txtPassword.Name = "txtPassword";
		this.txtPassword.Size = new System.Drawing.Size(420, 23);
		this.txtPassword.TabIndex = 5;
		this.txtPassword.UseSystemPasswordChar = true;
		this.ss.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.ss.Location = new System.Drawing.Point(0, 164);
		this.ss.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
		this.ss.Name = "ss";
		this.ss.Size = new System.Drawing.Size(420, 29);
		this.ss.TabIndex = 6;
		this.ss.Text = "Log in";
		this.ss.UseVisualStyleBackColor = true;
		this.ss.Click += new System.EventHandler(button1_Click);
		this.registerRow.AutoSize = true;
		this.registerRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
		this.registerRow.Location = new System.Drawing.Point(0, 203);
		this.registerRow.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
		this.registerRow.Name = "registerRow";
		this.registerRow.Size = new System.Drawing.Size(215, 23);
		this.registerRow.TabIndex = 7;
		this.registerRow.WrapContents = false;
		this.registerRow.Controls.Add(this.label3);
		this.registerRow.Controls.Add(this.button1);
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(0, 0);
		this.label3.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(130, 15);
		this.label3.TabIndex = 0;
		this.label3.Text = "Don't have an account?";
		this.button1.AutoSize = true;
		this.button1.Location = new System.Drawing.Point(136, 0);
		this.button1.Margin = new System.Windows.Forms.Padding(0);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 1;
		this.button1.Text = "Register";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click_1);
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1040, 600);
		base.Controls.Add(this.rootLayout);
		base.Name = "Form1";
		this.Text = "Barangay System - Login";
		base.Load += new System.EventHandler(Form1_Load);
		this.rootLayout.ResumeLayout(false);
		this.panelLeft.ResumeLayout(false);
		this.panelLeft.PerformLayout();
		this.heroStack.ResumeLayout(false);
		this.heroStack.PerformLayout();
		this.panelRight.ResumeLayout(false);
		this.centerLayout.ResumeLayout(false);
		this.centerLayout.PerformLayout();
		this.panelCard.ResumeLayout(false);
		this.panelCard.PerformLayout();
		this.cardLayout.ResumeLayout(false);
		this.cardLayout.PerformLayout();
		this.registerRow.ResumeLayout(false);
		this.registerRow.PerformLayout();
		base.ResumeLayout(false);
	}
}
