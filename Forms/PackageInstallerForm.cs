using System;
using System.Drawing;
using System.Windows.Forms;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

public sealed class PackageInstallerForm : Form
{
    private RadioButton radioLocal = null!;
    private RadioButton radioNetwork = null!;
    private TextBox txtServer = null!;
    private TextBox txtPort = null!;
    private TextBox txtDatabase = null!;
    private TextBox txtDbUser = null!;
    private TextBox txtDbPassword = null!;
    private CheckBox chkUseSsl = null!;
    private Button btnTestConnection = null!;

    private TextBox txtSuperAdminUsername = null!;
    private TextBox txtSuperAdminPassword = null!;
    private TextBox txtUserUsername = null!;
    private TextBox txtUserPassword = null!;

    private Label lblStatus = null!;
    private Button btnInstall = null!;
    private Button btnCancel = null!;

    public PackageInstallerForm()
    {
        InitializeLayout();
        ApplyTheme();
        LoadSavedProfile();
    }

    private void InitializeLayout()
    {
        Text = "Package Installer";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(760, 640);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var headerTitle = new Label
        {
            Text = "Package Installer",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };
        var headerSubtitle = new Label
        {
            Text = "Configure local/network database and create Super Admin + User starter accounts.",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };

        var headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 62
        };
        headerPanel.Controls.Add(headerTitle);
        headerPanel.Controls.Add(headerSubtitle);
        headerSubtitle.Top = 32;
        root.Controls.Add(headerPanel, 0, 0);

        var connectionGroup = BuildConnectionGroup();
        connectionGroup.Margin = new Padding(0, 0, 0, 12);
        root.Controls.Add(connectionGroup, 0, 1);

        var accountGroup = BuildAccountGroup();
        accountGroup.Margin = new Padding(0, 0, 0, 12);
        root.Controls.Add(accountGroup, 0, 2);

        lblStatus = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = "Ready.",
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(8),
            BorderStyle = BorderStyle.FixedSingle
        };
        root.Controls.Add(lblStatus, 0, 3);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        btnInstall = new Button
        {
            Text = "Install",
            AutoSize = true
        };
        btnInstall.Click += (_, _) => InstallPackage();

        btnCancel = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        actions.Controls.Add(btnInstall);
        actions.Controls.Add(btnCancel);
        root.Controls.Add(actions, 0, 4);

        AcceptButton = btnInstall;
        CancelButton = btnCancel;
    }

    private Control BuildConnectionGroup()
    {
        var group = new GroupBox
        {
            Text = "Database Connection",
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        group.Controls.Add(layout);

        radioLocal = new RadioButton { Text = "Local", AutoSize = true, Checked = true, Margin = new Padding(0, 6, 8, 6) };
        radioNetwork = new RadioButton { Text = "Network", AutoSize = true, Margin = new Padding(0, 6, 8, 6) };
        radioLocal.CheckedChanged += (_, _) => ApplyModeDefaults();
        radioNetwork.CheckedChanged += (_, _) => ApplyModeDefaults();

        var modePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        modePanel.Controls.Add(radioLocal);
        modePanel.Controls.Add(radioNetwork);

        layout.Controls.Add(new Label { Text = "Mode", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, 0);
        layout.Controls.Add(modePanel, 1, 0);
        layout.SetColumnSpan(modePanel, 3);

        txtServer = new TextBox { Dock = DockStyle.Fill };
        txtPort = new TextBox { Dock = DockStyle.Fill };
        txtDatabase = new TextBox { Dock = DockStyle.Fill };
        txtDbUser = new TextBox { Dock = DockStyle.Fill };
        txtDbPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        chkUseSsl = new CheckBox { Text = "Use SSL", AutoSize = true };
        btnTestConnection = new Button { Text = "Test Connection", AutoSize = true };
        btnTestConnection.Click += (_, _) => TestConnection();

        AddLabeledControl(layout, 0, "Server", txtServer, "Port", txtPort);
        AddLabeledControl(layout, 1, "Database", txtDatabase, "DB Username", txtDbUser);
        AddLabeledControl(layout, 2, "DB Password", txtDbPassword, string.Empty, chkUseSsl);

        var actionRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0)
        };
        actionRow.Controls.Add(btnTestConnection);
        int actionRowIndex = layout.RowStyles.Count;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(actionRow, 0, actionRowIndex);
        layout.SetColumnSpan(actionRow, 4);

        return group;
    }

    private Control BuildAccountGroup()
    {
        var group = new GroupBox
        {
            Text = "Starter Accounts",
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        group.Controls.Add(layout);

        txtSuperAdminUsername = new TextBox { Dock = DockStyle.Fill };
        txtSuperAdminPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        txtUserUsername = new TextBox { Dock = DockStyle.Fill };
        txtUserPassword = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

        AddLabeledControl(layout, 0, "Super Admin Username", txtSuperAdminUsername, "Super Admin Password", txtSuperAdminPassword);
        AddLabeledControl(layout, 1, "User Username", txtUserUsername, "User Password", txtUserPassword);

        return group;
    }

    private static void AddLabeledControl(
        TableLayoutPanel layout,
        int rowIndex,
        string leftLabel,
        Control leftControl,
        string rightLabel,
        Control rightControl)
    {
        int baseRow = rowIndex * 2;

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = leftLabel, AutoSize = true, Margin = new Padding(0, 8, 8, 4) }, 0, baseRow);
        layout.Controls.Add(leftControl, 1, baseRow + 1);

        if (!string.IsNullOrWhiteSpace(rightLabel))
        {
            layout.Controls.Add(new Label { Text = rightLabel, AutoSize = true, Margin = new Padding(10, 8, 8, 4) }, 2, baseRow);
        }

        rightControl.Margin = new Padding(10, 0, 0, 8);
        layout.Controls.Add(rightControl, 3, baseRow + 1);
    }

    private void ApplyTheme()
    {
        BackColor = UiTheme.Slate50;
        Font = UiTheme.BodyFont;

        foreach (Control control in Controls)
        {
            ApplyThemeRecursive(control);
        }

        UiTheme.StylePrimaryButton(btnInstall);
        UiTheme.StyleGhostButton(btnCancel);
        UiTheme.StyleSecondaryButton(btnTestConnection);
        lblStatus.BackColor = Color.White;
        lblStatus.ForeColor = UiTheme.Slate700;

        UiTheme.StandardizeButtonLayout(this);
    }

    private static void ApplyThemeRecursive(Control control)
    {
        if (control is TextBox textBox)
        {
            UiTheme.StyleTextBox(textBox);
        }
        else if (control is GroupBox group)
        {
            group.Font = UiTheme.LabelFont;
            group.ForeColor = UiTheme.Slate900;
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

        foreach (Control child in control.Controls)
        {
            ApplyThemeRecursive(child);
        }
    }

    private void LoadSavedProfile()
    {
        var profile = DbConnectionSettingsStore.LoadOrDefault();
        radioLocal.Checked = !string.Equals(profile.Mode, "Network", StringComparison.OrdinalIgnoreCase);
        radioNetwork.Checked = !radioLocal.Checked;
        txtServer.Text = profile.Server;
        txtPort.Text = profile.Port.ToString();
        txtDatabase.Text = profile.Database;
        txtDbUser.Text = profile.Username;
        txtDbPassword.Text = profile.Password;
        chkUseSsl.Checked = profile.UseSsl;

        txtSuperAdminUsername.Text = "superadmin";
        txtUserUsername.Text = "user";
    }

    private void ApplyModeDefaults()
    {
        if (radioLocal.Checked)
        {
            if (string.IsNullOrWhiteSpace(txtServer.Text) || string.Equals(txtServer.Text.Trim(), "localhost", StringComparison.OrdinalIgnoreCase))
            {
                txtServer.Text = "localhost";
            }

            if (string.IsNullOrWhiteSpace(txtPort.Text))
            {
                txtPort.Text = "3306";
            }
        }
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
            Mode = radioNetwork.Checked ? "Network" : "Local",
            Server = server,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            UseSsl = chkUseSsl.Checked
        };
    }

    private void TestConnection()
    {
        try
        {
            var profile = BuildProfileFromInputs();
            var result = PackageInstallerService.TestConnection(profile);
            SetStatus(result.Message, isError: !result.Success);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void InstallPackage()
    {
        try
        {
            var profile = BuildProfileFromInputs();

            var request = new PackageInstallRequest
            {
                ConnectionProfile = profile,
                SuperAdminUsername = txtSuperAdminUsername.Text.Trim(),
                SuperAdminPassword = txtSuperAdminPassword.Text,
                UserUsername = txtUserUsername.Text.Trim(),
                UserPassword = txtUserPassword.Text
            };

            SetBusyState(true);
            SetStatus("Running installer: preparing database, applying migrations, and creating starter accounts...");

            PackageInstallerService.Install(request);

            SetStatus("Installation completed successfully. You can now log in using the created accounts.");
            MessageBox.Show(
                this,
                "Installation completed successfully.",
                "Package Installer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Package installer failed.", ex);
            SetStatus(ex.Message, isError: true);
            MessageBox.Show(
                this,
                "Installation failed.\n\n" + ex.Message,
                "Package Installer",
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
        btnInstall.Enabled = !busy;
        btnCancel.Enabled = !busy;
        btnTestConnection.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetStatus(string message, bool isError = false)
    {
        lblStatus.Text = message;
        lblStatus.ForeColor = isError ? Color.Firebrick : UiTheme.Slate700;
    }
}
