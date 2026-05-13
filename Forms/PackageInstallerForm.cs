using System;
using System.Drawing;
using System.Windows.Forms;
using baranggaysystem1.Database;

namespace baranggaysystem1;

public sealed class PackageInstallerForm : Form
{
    private TextBox txtServer = null!;
    private TextBox txtPort = null!;
    private TextBox txtDatabase = null!;
    private TextBox txtDbUser = null!;
    private TextBox txtDbPassword = null!;
    private CheckBox chkUseSsl = null!;
    private Button btnTestConnection = null!;
    private Button btnCancel = null!;
    private Panel headerPanel = null!;
    private Panel connectionPanel = null!;
    private Panel statusPanel = null!;
    private Label lblHeaderTitle = null!;
    private Label lblHeaderSubtitle = null!;
    private Label lblHeaderTip = null!;
    private Label lblConnectionTitle = null!;
    private Label lblStatusTitle = null!;
    private Label lblStatus = null!;

    private enum StatusTone
    {
        Neutral,
        Success,
        Warning,
        Error
    }

    public PackageInstallerForm()
    {
        InitializeLayout();
        ApplyTheme();
        LoadSavedProfile();
    }

    private void InitializeLayout()
    {
        Text = "Connect to Database";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(980, 560);
        MinimumSize = new Size(980, 560);
        Shown += (_, _) => BringDialogToFront();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24),
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        headerPanel = BuildHeaderPanel();
        headerPanel.Margin = new Padding(0, 0, 0, 18);
        root.Controls.Add(headerPanel, 0, 0);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = new Padding(0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        connectionPanel = BuildConnectionPanel();
        connectionPanel.Margin = new Padding(0);
        body.Controls.Add(connectionPanel, 0, 0);

        root.Controls.Add(body, 0, 1);

        statusPanel = BuildStatusPanel();
        statusPanel.Margin = new Padding(0, 18, 0, 14);
        root.Controls.Add(statusPanel, 0, 2);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0)
        };

        btnTestConnection = new Button
        {
            Text = "Test && Continue",
            AutoSize = true
        };
        btnTestConnection.Click += (_, _) => TestConnection();

        btnCancel = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        actions.Controls.Add(btnTestConnection);
        actions.Controls.Add(btnCancel);
        root.Controls.Add(actions, 0, 3);

        AcceptButton = btnTestConnection;
        CancelButton = btnCancel;
    }

    private void BringDialogToFront()
    {
        Activate();
        BringToFront();
        TopMost = true;

        BeginInvoke(new Action(() =>
        {
            TopMost = false;
            Activate();
        }));
    }

    private Panel BuildHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 118,
            Padding = new Padding(24, 18, 24, 18)
        };

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblHeaderTitle = new Label
        {
            AutoSize = true,
            Text = "Connect to your database",
            Margin = new Padding(0, 0, 0, 6),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        lblHeaderSubtitle = new Label
        {
            AutoSize = true,
            Text = "Enter your database connection details, test it, and continue to the login screen.",
            Margin = new Padding(0, 0, 0, 10),
            ForeColor = UiTheme.Slate300,
            BackColor = Color.Transparent
        };

        lblHeaderTip = new Label
        {
            AutoSize = true,
            Text = "Tip: enable SSL if your database host requires encrypted connections.",
            ForeColor = Color.FromArgb(223, 229, 235),
            BackColor = Color.Transparent
        };

        shell.Controls.Add(lblHeaderTitle, 0, 0);
        shell.Controls.Add(lblHeaderSubtitle, 0, 1);
        shell.Controls.Add(lblHeaderTip, 0, 2);
        panel.Controls.Add(shell);
        return panel;
    }

    private Panel BuildConnectionPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18)
        };

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        lblConnectionTitle = new Label
        {
            Text = "Connection Details",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        var formGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Margin = new Padding(0)
        };
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));

        txtServer = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "db.example.com or localhost" };
        txtPort = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "3306" };
        txtDatabase = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Database name" };
        txtDbUser = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Database username" };
        txtDbPassword = new TextBox
        {
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true,
            PlaceholderText = "Database password"
        };
        chkUseSsl = new CheckBox
        {
            Text = "Use SSL",
            AutoSize = true
        };

        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        formGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblServer = new Label { Text = "Server", AutoSize = true, Margin = new Padding(0, 8, 8, 4) };
        var lblPort = new Label { Text = "Port", AutoSize = true, Margin = new Padding(16, 8, 8, 4) };
        var lblDatabase = new Label { Text = "Database", AutoSize = true, Margin = new Padding(0, 8, 8, 4) };
        var lblUsername = new Label { Text = "DB Username", AutoSize = true, Margin = new Padding(0, 8, 8, 4) };
        var lblPassword = new Label { Text = "DB Password", AutoSize = true, Margin = new Padding(0, 8, 8, 4) };

        txtServer.Margin = new Padding(0, 0, 0, 8);
        txtPort.Margin = new Padding(16, 0, 0, 8);
        txtDatabase.Margin = new Padding(0, 0, 0, 8);
        txtDbUser.Margin = new Padding(0, 0, 0, 8);
        txtDbPassword.Margin = new Padding(0, 0, 0, 8);
        chkUseSsl.Margin = new Padding(16, 6, 0, 8);

        formGrid.Controls.Add(lblServer, 0, 0);
        formGrid.Controls.Add(lblPort, 2, 0);
        formGrid.Controls.Add(txtServer, 1, 1);
        formGrid.Controls.Add(txtPort, 3, 1);

        formGrid.Controls.Add(lblDatabase, 0, 2);
        formGrid.SetColumnSpan(lblDatabase, 4);
        formGrid.Controls.Add(txtDatabase, 0, 3);
        formGrid.SetColumnSpan(txtDatabase, 4);

        formGrid.Controls.Add(lblUsername, 0, 4);
        formGrid.SetColumnSpan(lblUsername, 4);
        formGrid.Controls.Add(txtDbUser, 0, 5);
        formGrid.SetColumnSpan(txtDbUser, 4);

        formGrid.Controls.Add(lblPassword, 0, 6);
        formGrid.Controls.Add(chkUseSsl, 3, 6);
        formGrid.Controls.Add(txtDbPassword, 0, 7);
        formGrid.SetColumnSpan(txtDbPassword, 4);

        shell.Controls.Add(lblConnectionTitle, 0, 0);
        shell.Controls.Add(formGrid, 0, 1);
        panel.Controls.Add(shell);
        return panel;
    }

    private Panel BuildStatusPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 92,
            Padding = new Padding(18, 14, 18, 14)
        };

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        lblStatusTitle = new Label
        {
            Text = "Status",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = "Enter your connection details, then test and continue.",
            TextAlign = ContentAlignment.TopLeft
        };

        shell.Controls.Add(lblStatusTitle, 0, 0);
        shell.Controls.Add(lblStatus, 0, 1);
        panel.Controls.Add(shell);
        return panel;
    }

    private void ApplyTheme()
    {
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;

        UiTheme.AttachGradient(headerPanel, UiTheme.Ink900, UiTheme.Ink700, 0f);

        UiTheme.StyleSectionCard(connectionPanel, Color.White, enforceBorder: true, padding: new Padding(18));
        UiTheme.StyleSectionCard(statusPanel, UiTheme.Blend(Color.White, UiTheme.AccentBlue, 6), enforceBorder: true, padding: new Padding(18, 14, 18, 14));

        foreach (Control control in Controls)
        {
            ApplyThemeRecursive(control);
        }

        lblHeaderTitle.Font = UiTheme.TitleFont;
        lblHeaderTitle.ForeColor = Color.White;
        lblHeaderSubtitle.Font = UiTheme.BodyFont;
        lblHeaderSubtitle.ForeColor = UiTheme.Slate300;
        lblHeaderTip.Font = UiTheme.SmallFont;
        lblHeaderTip.ForeColor = Color.FromArgb(223, 229, 235);

        UiTheme.StyleSectionHeader(lblConnectionTitle);
        UiTheme.StyleSectionHeader(lblStatusTitle);

        UiTheme.StylePrimaryButton(btnTestConnection);
        UiTheme.StyleGhostButton(btnCancel);
        UiTheme.StandardizeButtonLayout(this);
        UiTheme.EnhanceAccessibility(this);
        UiTheme.SetTabOrder(
            txtServer,
            txtPort,
            txtDatabase,
            txtDbUser,
            txtDbPassword,
            chkUseSsl,
            btnTestConnection,
            btnCancel);

        SetStatus("Enter your connection details, then test and continue.", StatusTone.Neutral);
    }

    private static void ApplyThemeRecursive(Control control)
    {
        if (control is TextBox textBox)
        {
            UiTheme.StyleTextBox(textBox);
        }
        else if (control is Label label)
        {
            label.Font = UiTheme.LabelFont;
            label.ForeColor = UiTheme.Slate700;
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.Font = UiTheme.LabelFont;
            checkBox.ForeColor = UiTheme.Slate700;
        }
        else if (control is RadioButton radioButton)
        {
            radioButton.Font = UiTheme.LabelFont;
            radioButton.ForeColor = UiTheme.Slate900;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeRecursive(child);
        }
    }

    private void LoadSavedProfile()
    {
        var profile = DbConnectionSettingsStore.LoadOrDefault();
        txtServer.Text = profile.Server;
        txtPort.Text = profile.Port.ToString();
        txtDatabase.Text = profile.Database;
        txtDbUser.Text = profile.Username;
        txtDbPassword.Text = profile.Password;
        chkUseSsl.Checked = profile.UseSsl;
    }

    private DatabaseConnectionProfile BuildProfileFromInputs()
    {
        string server = txtServer.Text.Trim();
        string database = txtDatabase.Text.Trim();
        string username = txtDbUser.Text.Trim();
        string password = txtDbPassword.Text;

        if (string.IsNullOrWhiteSpace(server))
        {
            throw new InvalidOperationException("Server is required.");
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("Database is required.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("DB username is required.");
        }

        if (!uint.TryParse(txtPort.Text.Trim(), out uint port) || port == 0)
        {
            throw new InvalidOperationException("Port must be a valid number.");
        }

        return new DatabaseConnectionProfile
        {
            Server = server,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            UseSsl = chkUseSsl.Checked
        };
    }

    private async void TestConnection()
    {
        try
        {
            var profile = BuildProfileFromInputs();

            SetBusyState(true);
            SetStatus("Testing database connection...", StatusTone.Neutral);

            var result = await System.Threading.Tasks.Task.Run(() => PackageInstallerService.TestConnection(profile));
            if (!result.Success)
            {
                SetStatus(result.Message, StatusTone.Error);
                MessageBox.Show(
                    this,
                    result.Message,
                    "Connection Test Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (result.DatabaseMissing)
            {
                const string message = "Connection reached the server, but the selected database does not exist yet. Use your initialized Hostinger database before continuing to login.";
                SetStatus(message, StatusTone.Warning);
                MessageBox.Show(
                    this,
                    message,
                    "Database Not Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!TryActivateConnectionProfile(profile, out string activationError))
            {
                SetStatus(activationError, StatusTone.Error);
                MessageBox.Show(
                    this,
                    activationError,
                    "Connection Test Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            SetStatus("Connection successful. Opening login...", StatusTone.Success);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, StatusTone.Error);
            MessageBox.Show(
                this,
                ex.Message,
                "Connection Test Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void SetBusyState(bool busy)
    {
        UseWaitCursor = busy;
        btnCancel.Enabled = !busy;
        btnTestConnection.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetStatus(string message, StatusTone tone)
    {
        lblStatus.Text = message;

        switch (tone)
        {
            case StatusTone.Success:
                statusPanel.BackColor = UiTheme.Blend(Color.White, UiTheme.AccentGreen, 10);
                lblStatus.ForeColor = UiTheme.Slate900;
                break;
            case StatusTone.Warning:
                statusPanel.BackColor = UiTheme.Blend(Color.White, UiTheme.AccentAmber, 14);
                lblStatus.ForeColor = UiTheme.Slate900;
                break;
            case StatusTone.Error:
                statusPanel.BackColor = UiTheme.Blend(Color.White, UiTheme.AccentRed, 10);
                lblStatus.ForeColor = UiTheme.Slate900;
                break;
            default:
                statusPanel.BackColor = UiTheme.Blend(Color.White, UiTheme.AccentBlue, 6);
                lblStatus.ForeColor = UiTheme.Slate700;
                break;
        }
    }

    private static bool TryActivateConnectionProfile(DatabaseConnectionProfile profile, out string errorMessage)
    {
        string connectionString = DbConnectionSettingsStore.BuildConnectionString(profile, includeDatabase: true);
        if (!DBConnection.TryGetWorkingConnectionString(connectionString, out string workingConnectionString, out errorMessage))
        {
            return false;
        }

        DbConnectionSettingsStore.Save(profile);
        DBConnection.SetRuntimeConnectionString(workingConnectionString);
        return true;
    }
}
