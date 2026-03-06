using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;

namespace baranggaysystem1;

public class RegisterForm : Form
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

	private FlowLayoutPanel loginRow;

	private Label labelSubtitle;

	private Label labelRole;

	private Button button1;

	private Label label1;

	private Label label2;

	private TextBox txtUsername;

	private TextBox txtPassword;

	private ComboBox cmbRole;

	private Label label3;

	private Button button2;

	private Label label4;

	public RegisterForm()
	{
		InitializeComponent();
		ApplyRegisterTheme();
	}

	private void button1_Click(object sender, EventArgs e)
	{
		string text = txtUsername.Text;
		string text2 = txtPassword.Text;
		string value = cmbRole.SelectedItem?.ToString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(value))
		{
			MessageBox.Show("Please select a role.");
			return;
		}
		string value2 = PasswordHelper.HashPassword(text2);
		string cmdText = "INSERT INTO users\r\n                    (username, password_hash, role, is_active, created_at)\r\n                    VALUES (@username, @password, @role, 1, NOW())";
		using (MySqlConnection mySqlConnection = DBConnection.GetConnection())
		{
			mySqlConnection.Open();
			MySqlCommand mySqlCommand = new MySqlCommand(cmdText, mySqlConnection);
			mySqlCommand.Parameters.AddWithValue("@username", text);
			mySqlCommand.Parameters.AddWithValue("@password", value2);
			mySqlCommand.Parameters.AddWithValue("@role", value);
			mySqlCommand.ExecuteNonQuery();
		}
		MessageBox.Show("User registered successfully!");
	}

	private void button2_Click(object sender, EventArgs e)
	{
		Form1 form = new Form1();
		form.ShowDialog();
	}

	private void RegisterForm_Load(object? sender, EventArgs e)
	{
		StartFadeIn();
	}

	private void ApplyRegisterTheme()
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
		label4.Text = "Create Account";
		label4.Font = new Font("Century Gothic", 16f, FontStyle.Bold);
		label4.ForeColor = UiTheme.Slate900;
		labelSubtitle.Text = "Add a new user and assign a role";
		labelSubtitle.Font = UiTheme.LabelFont;
		labelSubtitle.ForeColor = UiTheme.Slate500;
		label1.Text = "Username";
		label1.Font = UiTheme.LabelFont;
		label1.ForeColor = UiTheme.Slate700;
		label2.Text = "Password";
		label2.Font = UiTheme.LabelFont;
		label2.ForeColor = UiTheme.Slate700;
		labelRole.Text = "Role";
		labelRole.Font = UiTheme.LabelFont;
		labelRole.ForeColor = UiTheme.Slate700;
		UiTheme.StyleTextBox(txtUsername);
		txtUsername.PlaceholderText = "Choose a username";
		UiTheme.StyleTextBox(txtPassword);
		txtPassword.UseSystemPasswordChar = true;
		txtPassword.PlaceholderText = "Create a password";
		UiTheme.StyleComboBox(cmbRole);
		cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
		if (cmbRole.Items.Count > 0 && cmbRole.SelectedIndex < 0)
		{
			cmbRole.SelectedIndex = 0;
		}
		UiTheme.StylePrimaryButton(button1);
		button1.Text = "Register";
		label3.Text = "Already have an account?";
		label3.Font = UiTheme.LabelFont;
		label3.ForeColor = UiTheme.Slate500;
		UiTheme.StyleGhostButton(button2);
		button2.Text = "Log in";
		base.AcceptButton = button1;
		Font = UiTheme.BodyFont;
		BackColor = UiTheme.Slate50;
		Text = "Barangay System - Register";
		MinimumSize = new Size(1040, 640);
		base.StartPosition = FormStartPosition.CenterScreen;
		base.Load -= RegisterForm_Load;
		base.Load += RegisterForm_Load;
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
		this.labelRole = new System.Windows.Forms.Label();
		this.cmbRole = new System.Windows.Forms.ComboBox();
		this.button1 = new System.Windows.Forms.Button();
		this.loginRow = new System.Windows.Forms.FlowLayoutPanel();
		this.label3 = new System.Windows.Forms.Label();
		this.button2 = new System.Windows.Forms.Button();
		this.rootLayout.SuspendLayout();
		this.panelLeft.SuspendLayout();
		this.heroStack.SuspendLayout();
		this.panelRight.SuspendLayout();
		this.centerLayout.SuspendLayout();
		this.panelCard.SuspendLayout();
		this.cardLayout.SuspendLayout();
		this.loginRow.SuspendLayout();
		base.SuspendLayout();
		this.rootLayout.ColumnCount = 2;
		this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 360f));
		this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		this.rootLayout.Location = new System.Drawing.Point(0, 0);
		this.rootLayout.Name = "rootLayout";
		this.rootLayout.RowCount = 1;
		this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
		this.rootLayout.Size = new System.Drawing.Size(1040, 640);
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
		this.labelTagline.Text = "Create staff or admin accounts";
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
		this.centerLayout.Size = new System.Drawing.Size(568, 528);
		this.centerLayout.TabIndex = 0;
		this.centerLayout.Controls.Add(this.panelCard, 0, 1);
		this.panelCard.AutoSize = true;
		this.panelCard.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		this.panelCard.MinimumSize = new System.Drawing.Size(420, 360);
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
		this.cardLayout.RowCount = 10;
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		this.cardLayout.Size = new System.Drawing.Size(420, 360);
		this.cardLayout.TabIndex = 0;
		this.cardLayout.Controls.Add(this.label4, 0, 0);
		this.cardLayout.Controls.Add(this.labelSubtitle, 0, 1);
		this.cardLayout.Controls.Add(this.label1, 0, 2);
		this.cardLayout.Controls.Add(this.txtUsername, 0, 3);
		this.cardLayout.Controls.Add(this.label2, 0, 4);
		this.cardLayout.Controls.Add(this.txtPassword, 0, 5);
		this.cardLayout.Controls.Add(this.labelRole, 0, 6);
		this.cardLayout.Controls.Add(this.cmbRole, 0, 7);
		this.cardLayout.Controls.Add(this.button1, 0, 8);
		this.cardLayout.Controls.Add(this.loginRow, 0, 9);
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(0, 0);
		this.label4.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(87, 15);
		this.label4.TabIndex = 0;
		this.label4.Text = "Create Account";
		this.labelSubtitle.AutoSize = true;
		this.labelSubtitle.Location = new System.Drawing.Point(0, 19);
		this.labelSubtitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);
		this.labelSubtitle.Name = "labelSubtitle";
		this.labelSubtitle.Size = new System.Drawing.Size(192, 15);
		this.labelSubtitle.TabIndex = 1;
		this.labelSubtitle.Text = "Add a new user and assign a role";
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
		this.txtPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
		this.txtPassword.Name = "txtPassword";
		this.txtPassword.Size = new System.Drawing.Size(420, 23);
		this.txtPassword.TabIndex = 5;
		this.txtPassword.UseSystemPasswordChar = true;
		this.labelRole.AutoSize = true;
		this.labelRole.Location = new System.Drawing.Point(0, 160);
		this.labelRole.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
		this.labelRole.Name = "labelRole";
		this.labelRole.Size = new System.Drawing.Size(30, 15);
		this.labelRole.TabIndex = 6;
		this.labelRole.Text = "Role";
		this.cmbRole.Dock = System.Windows.Forms.DockStyle.Fill;
		this.cmbRole.FormattingEnabled = true;
		this.cmbRole.Items.AddRange(new object[2] { "Admin", "Staff" });
		this.cmbRole.Location = new System.Drawing.Point(0, 179);
		this.cmbRole.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
		this.cmbRole.Name = "cmbRole";
		this.cmbRole.Size = new System.Drawing.Size(420, 23);
		this.cmbRole.TabIndex = 7;
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.button1.Location = new System.Drawing.Point(0, 218);
		this.button1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(420, 29);
		this.button1.TabIndex = 8;
		this.button1.Text = "Register";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.loginRow.AutoSize = true;
		this.loginRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
		this.loginRow.Location = new System.Drawing.Point(0, 257);
		this.loginRow.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
		this.loginRow.Name = "loginRow";
		this.loginRow.Size = new System.Drawing.Size(198, 23);
		this.loginRow.TabIndex = 9;
		this.loginRow.WrapContents = false;
		this.loginRow.Controls.Add(this.label3);
		this.loginRow.Controls.Add(this.button2);
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(0, 0);
		this.label3.Margin = new System.Windows.Forms.Padding(0, 6, 6, 0);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(138, 15);
		this.label3.TabIndex = 0;
		this.label3.Text = "Already have an account?";
		this.button2.AutoSize = true;
		this.button2.Location = new System.Drawing.Point(144, 0);
		this.button2.Margin = new System.Windows.Forms.Padding(0);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(54, 23);
		this.button2.TabIndex = 1;
		this.button2.Text = "Log in";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(button2_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1040, 640);
		base.Controls.Add(this.rootLayout);
		base.Name = "RegisterForm";
		this.Text = "Barangay System - Register";
		base.Load += new System.EventHandler(RegisterForm_Load);
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
		this.loginRow.ResumeLayout(false);
		this.loginRow.PerformLayout();
		base.ResumeLayout(false);
	}
}
