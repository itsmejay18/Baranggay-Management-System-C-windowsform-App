using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using baranggaysystem1.Controls;

namespace baranggaysystem1;

internal sealed class AttachmentModuleForm : Form
{
    private readonly AttachmentEntityType _entityType;
    private readonly int _entityId;

    private readonly Label _title = new Label();
    private readonly Label _subtitle = new Label();
    private readonly FlowLayoutPanel _actions = new FlowLayoutPanel();
    private readonly Button _addButton = new Button();
    private readonly Button _openButton = new Button();
    private readonly Button _deleteButton = new Button();
    private readonly Button _refreshButton = new Button();
    private readonly DataGridView _grid = new DataGridView();
    private readonly Panel _stateHost = new Panel();
    private readonly LoadingOverlay _loadingOverlay = new LoadingOverlay();

    private DataTable? _table;

    public AttachmentModuleForm(AttachmentEntityType entityType, int entityId, string? entityLabel = null)
    {
        _entityType = entityType;
        _entityId = entityId;

        string typeLabel = AttachmentService.GetEntityDisplayName(entityType);
        string displayLabel = string.IsNullOrWhiteSpace(entityLabel)
            ? $"{typeLabel} #{entityId}"
            : $"{typeLabel}: {entityLabel.Trim()}";

        Text = "Attachment Manager";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new System.Drawing.Size(860, 520);
        Size = new System.Drawing.Size(980, 600);
        BackColor = UiTheme.Slate100;
        Font = UiTheme.BodyFont;

        BuildLayout(displayLabel);
        BindEvents();
        RefreshData();
    }

    private void BuildLayout(string displayLabel)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(14, 12, 14, 12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _title.AutoSize = true;
        _title.Text = "Attachments";
        _title.Font = UiTheme.HeadingFont;
        _title.ForeColor = UiTheme.Slate900;

        _subtitle.AutoSize = true;
        _subtitle.Text = displayLabel;
        _subtitle.Font = UiTheme.LabelFont;
        _subtitle.ForeColor = UiTheme.Slate600;
        _subtitle.Margin = new Padding(0, 2, 0, 8);

        _actions.Dock = DockStyle.Top;
        _actions.AutoSize = true;
        _actions.WrapContents = true;
        _actions.FlowDirection = FlowDirection.LeftToRight;
        _actions.Margin = new Padding(0, 0, 0, 8);
        _actions.Padding = new Padding(0);

        _addButton.Text = "Add File";
        _openButton.Text = "Open";
        _deleteButton.Text = "Delete";
        _refreshButton.Text = "Refresh";
        UiTheme.StylePrimaryButton(_addButton);
        UiTheme.StyleSecondaryButton(_openButton);
        UiTheme.StyleDangerButton(_deleteButton);
        UiTheme.StyleSecondaryButton(_refreshButton);
        _addButton.AutoSize = true;
        _openButton.AutoSize = true;
        _deleteButton.AutoSize = true;
        _refreshButton.AutoSize = true;

        _actions.Controls.Add(_addButton);
        _actions.Controls.Add(_openButton);
        _actions.Controls.Add(_deleteButton);
        _actions.Controls.Add(_refreshButton);

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        UiTheme.StyleGridContainer(gridHost);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        UiTheme.StyleGrid(_grid);

        _stateHost.Dock = DockStyle.Fill;
        _stateHost.BackColor = Color.Transparent;
        _stateHost.Visible = false;

        gridHost.Controls.Add(_grid);
        gridHost.Controls.Add(_stateHost);

        var footer = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Slate500,
            Margin = new Padding(0, 8, 0, 0),
            Text = "Supported file size: up to 20 MB per attachment."
        };

        root.Controls.Add(_title, 0, 0);
        root.Controls.Add(_subtitle, 0, 1);
        root.Controls.Add(_actions, 0, 2);
        root.Controls.Add(gridHost, 0, 3);
        root.Controls.Add(footer, 0, 4);

        _loadingOverlay.HideLoading();
        Controls.Add(_loadingOverlay);
        _loadingOverlay.BringToFront();

        UiTheme.StandardizeButtonLayout(this);
        UiTheme.SetTabOrder(_addButton, _openButton, _deleteButton, _refreshButton, _grid);
        UiTheme.EnhanceAccessibility(this);
    }

    private void BindEvents()
    {
        _addButton.Click += (_, __) => AddAttachment();
        _openButton.Click += (_, __) => OpenAttachment();
        _deleteButton.Click += (_, __) => DeleteAttachment();
        _refreshButton.Click += (_, __) => RefreshData();
        _grid.SelectionChanged += (_, __) => UpdateActionState();
    }

    private void RefreshData()
    {
        SetLoading(true, "Loading attachments...");

        try
        {
            var rows = AttachmentService.LoadList(_entityType, _entityId);
            var table = new DataTable();
            table.Columns.Add("attachment_id", typeof(long));
            table.Columns.Add("file_name", typeof(string));
            table.Columns.Add("size", typeof(string));
            table.Columns.Add("mime_type", typeof(string));
            table.Columns.Add("notes", typeof(string));
            table.Columns.Add("uploaded_by", typeof(string));
            table.Columns.Add("uploaded_at", typeof(DateTime));

            foreach (AttachmentListItem item in rows)
            {
                table.Rows.Add(
                    item.AttachmentId,
                    item.FileName,
                    FormatSize(item.FileSizeBytes),
                    item.MimeType,
                    item.Notes,
                    item.UploadedBy,
                    item.UploadedAt == DateTime.MinValue ? DBNull.Value : item.UploadedAt);
            }

            _table = table;
            _grid.DataSource = table;
            if (_grid.Columns.Contains("attachment_id"))
            {
                _grid.Columns["attachment_id"].Visible = false;
            }

            if (_grid.Columns.Contains("uploaded_at"))
            {
                _grid.Columns["uploaded_at"].HeaderText = "Uploaded";
                _grid.Columns["uploaded_at"].DefaultCellStyle.Format = "MMM dd, yyyy h:mm tt";
                _grid.Columns["uploaded_at"].FillWeight = 130;
            }

            if (_grid.Columns.Contains("file_name"))
            {
                _grid.Columns["file_name"].HeaderText = "File";
                _grid.Columns["file_name"].FillWeight = 220;
            }

            if (_grid.Columns.Contains("size"))
            {
                _grid.Columns["size"].HeaderText = "Size";
                _grid.Columns["size"].FillWeight = 70;
            }

            if (_grid.Columns.Contains("mime_type"))
            {
                _grid.Columns["mime_type"].HeaderText = "Type";
                _grid.Columns["mime_type"].FillWeight = 110;
            }

            if (_grid.Columns.Contains("notes"))
            {
                _grid.Columns["notes"].HeaderText = "Notes";
                _grid.Columns["notes"].FillWeight = 200;
            }

            if (_grid.Columns.Contains("uploaded_by"))
            {
                _grid.Columns["uploaded_by"].HeaderText = "Uploaded By";
                _grid.Columns["uploaded_by"].FillWeight = 110;
            }

            if (table.Rows.Count == 0)
            {
                ShowState(
                    "No attachments yet",
                    "Attach supporting files (PDF, image, or office docs) for this record.",
                    IconChar.FileCirclePlus,
                    UiTheme.Slate500);
            }
            else
            {
                HideState();
            }
        }
        catch (Exception ex)
        {
            helper.AppLogger.LogWarning("Failed to load attachments.", ex);
            ShowState(
                "Unable to load attachments",
                ex.Message,
                IconChar.TriangleExclamation,
                UiTheme.AccentRed);
        }
        finally
        {
            SetLoading(false);
            UpdateActionState();
        }
    }

    private void AddAttachment()
    {
        if (!helper.Permissions.CanManageAttachments)
        {
            ControllerDialogs.Warning("You do not have permission to add attachments.");
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Select attachment",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            Filter = "All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        string? notes = ControllerDialogs.Prompt("Notes (optional):", "Attachment Notes");
        if (notes == null)
        {
            notes = string.Empty;
        }

        try
        {
            AttachmentService.AddFromFile(_entityType, _entityId, dialog.FileName, notes);
            RefreshData();
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to add attachment.", "Attachments");
        }
    }

    private void OpenAttachment()
    {
        long? id = GetSelectedAttachmentId();
        if (!id.HasValue)
        {
            return;
        }

        try
        {
            AttachmentContent? content = AttachmentService.LoadContent(id.Value);
            if (content == null || content.Content.Length == 0)
            {
                ControllerDialogs.Warning("Selected attachment is not available.");
                return;
            }

            string safeFileName = SanitizeFileName(content.FileName);
            string tempPath = Path.Combine(
                Path.GetTempPath(),
                $"barangay_attachment_{content.AttachmentId}_{safeFileName}");
            File.WriteAllBytes(tempPath, content.Content);

            var psi = new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to open attachment.", "Attachments");
        }
    }

    private void DeleteAttachment()
    {
        if (!helper.Permissions.CanManageAttachments)
        {
            ControllerDialogs.Warning("You do not have permission to delete attachments.");
            return;
        }

        long? id = GetSelectedAttachmentId();
        if (!id.HasValue)
        {
            return;
        }

        DialogResult confirm = ControllerDialogs.Confirm(
            "Delete selected attachment?",
            "Confirm Delete");
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            AttachmentService.DeleteAttachment(id.Value);
            RefreshData();
        }
        catch (Exception ex)
        {
            ControllerDialogs.Error(ex, "Unable to delete attachment.", "Attachments");
        }
    }

    private void UpdateActionState()
    {
        bool hasRow = GetSelectedAttachmentId().HasValue;
        bool canManage = helper.Permissions.CanManageAttachments;
        _openButton.Enabled = hasRow;
        _deleteButton.Enabled = hasRow && canManage;
        _addButton.Enabled = canManage;
    }

    private long? GetSelectedAttachmentId()
    {
        if (_grid.CurrentRow == null || !_grid.Columns.Contains("attachment_id"))
        {
            return null;
        }

        object? value = _grid.CurrentRow.Cells["attachment_id"]?.Value;
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt64(value);
    }

    private void SetLoading(bool loading, string message = "Loading...")
    {
        if (loading)
        {
            _loadingOverlay.ShowLoading(message);
        }
        else
        {
            _loadingOverlay.HideLoading();
        }
    }

    private void ShowState(string title, string message, IconChar icon, Color accent)
    {
        _stateHost.Controls.Clear();
        _stateHost.Visible = true;
        _stateHost.BringToFront();
        _stateHost.Controls.Add(UiTheme.CreateStateCard(title, message, icon, accent));
    }

    private void HideState()
    {
        _stateHost.Visible = false;
        _stateHost.Controls.Clear();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        const double kb = 1024d;
        const double mb = kb * 1024d;

        if (bytes >= mb)
        {
            return $"{bytes / mb:0.##} MB";
        }

        if (bytes >= kb)
        {
            return $"{bytes / kb:0.##} KB";
        }

        return $"{bytes} B";
    }

    private static string SanitizeFileName(string input)
    {
        string safe = string.IsNullOrWhiteSpace(input) ? "attachment.bin" : input;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            safe = safe.Replace(c, '_');
        }

        return safe;
    }
}
