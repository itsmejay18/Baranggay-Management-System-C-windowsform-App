using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using baranggaysystem1.helper;

namespace baranggaysystem1;

public sealed class GlobalSearchForm : Form
{
    private readonly TextBox _queryBox = new TextBox();
    private readonly ComboBox _scopeBox = new ComboBox();
    private readonly DataGridView _grid = new DataGridView();
    private readonly Button _openButton = new Button();
    private readonly Button _closeButton = new Button();
    private readonly Label _hintLabel = new Label();
    private readonly Label _statusLabel = new Label();
    private readonly System.Windows.Forms.Timer _debounceTimer = new System.Windows.Forms.Timer();

    private readonly List<GlobalSearchResult> _results = new List<GlobalSearchResult>();

    internal GlobalSearchResult? SelectedResult { get; private set; }

    public GlobalSearchForm()
    {
        Text = "Global Search";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        BackColor = Color.White;
        Font = UiTheme.BodyFont;
        ClientSize = new Size(920, 560);

        BuildLayout();
        ApplyTheme();
        WireEvents();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _queryBox.Focus();
        _queryBox.SelectAll();
        ExecuteSearch();
    }

    private void BuildLayout()
    {
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            Padding = new Padding(16, 14, 16, 10),
            BackColor = Color.White
        };

        var title = new Label
        {
            Text = "Search",
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 24,
            Font = UiTheme.HeadingFont,
            ForeColor = UiTheme.Slate900
        };

        var queryRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 34,
            ColumnCount = 3,
            Margin = new Padding(0, 8, 0, 0)
        };
        queryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        queryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        queryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));

        _queryBox.Dock = DockStyle.Fill;
        _queryBox.PlaceholderText = "Type a name, number, or keyword...";
        _scopeBox.Dock = DockStyle.Fill;
        _scopeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _scopeBox.Items.AddRange(new object[]
        {
            "All",
            "Residents",
            "Certificates",
            "Blotter",
            "Users"
        });
        _scopeBox.SelectedIndex = 0;

        _openButton.Text = "Open";
        _openButton.Dock = DockStyle.Fill;
        _openButton.Enabled = false;

        queryRow.Controls.Add(_queryBox, 0, 0);
        queryRow.Controls.Add(_scopeBox, 1, 0);
        queryRow.Controls.Add(_openButton, 2, 0);

        _hintLabel.Dock = DockStyle.Top;
        _hintLabel.AutoSize = false;
        _hintLabel.Height = 20;
        _hintLabel.Margin = new Padding(0, 8, 0, 0);
        _hintLabel.Text = "Tip: Ctrl+K opens search. Enter opens selection. Esc closes.";

        topPanel.Controls.Add(_hintLabel);
        topPanel.Controls.Add(queryRow);
        topPanel.Controls.Add(title);
        Controls.Add(topPanel);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colType",
            HeaderText = "Type",
            DataPropertyName = "type",
            FillWeight = 18
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTitle",
            HeaderText = "Result",
            DataPropertyName = "title",
            FillWeight = 46
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSubtitle",
            HeaderText = "Details",
            DataPropertyName = "subtitle",
            FillWeight = 36
        });
        Controls.Add(_grid);

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = UiTheme.Slate50
        };

        _statusLabel.Dock = DockStyle.Left;
        _statusLabel.AutoSize = false;
        _statusLabel.Width = 650;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "Type at least 2 characters.";

        _closeButton.Text = "Close";
        _closeButton.Dock = DockStyle.Right;
        _closeButton.Width = 120;

        bottomPanel.Controls.Add(_closeButton);
        bottomPanel.Controls.Add(_statusLabel);
        Controls.Add(bottomPanel);
    }

    private void ApplyTheme()
    {
        _hintLabel.Font = UiTheme.SmallFont;
        _hintLabel.ForeColor = UiTheme.Slate500;
        _statusLabel.Font = UiTheme.SmallFont;
        _statusLabel.ForeColor = UiTheme.Slate600;

        UiTheme.StyleTextBoxes(_queryBox);
        UiTheme.StyleComboBoxes(_scopeBox);
        UiTheme.StylePrimaryButton(_openButton);
        UiTheme.StyleGhostButton(_closeButton);
        UiTheme.StyleGrid(_grid);
        UiTheme.StandardizeButtonLayout(this);
    }

    private void WireEvents()
    {
        _debounceTimer.Interval = 260;
        _debounceTimer.Tick += (_, __) =>
        {
            _debounceTimer.Stop();
            ExecuteSearch();
        };

        _queryBox.TextChanged += (_, __) => ScheduleSearch(immediate: false);
        _scopeBox.SelectedIndexChanged += (_, __) => ScheduleSearch(immediate: true);

        _openButton.Click += (_, __) => OpenSelected();
        _closeButton.Click += (_, __) => Close();

        _grid.SelectionChanged += (_, __) => UpdateOpenButtonState();
        _grid.CellDoubleClick += (_, __) => OpenSelected();

        KeyDown += GlobalSearchForm_KeyDown;
        _queryBox.KeyDown += QueryBox_KeyDown;
        _grid.KeyDown += Grid_KeyDown;
    }

    private void GlobalSearchForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void QueryBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down)
        {
            if (_grid.Rows.Count > 0)
            {
                _grid.Focus();
                if (_grid.SelectedRows.Count == 0)
                {
                    _grid.Rows[0].Selected = true;
                }
            }
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            OpenSelected(preferFirst: true);
            e.Handled = true;
        }
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            OpenSelected();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void ScheduleSearch(bool immediate)
    {
        _debounceTimer.Stop();
        if (immediate)
        {
            ExecuteSearch();
            return;
        }

        _debounceTimer.Start();
    }

    private void ExecuteSearch()
    {
        _debounceTimer.Stop();

        string query = _queryBox.Text.Trim();
        var scope = ParseScope(_scopeBox.SelectedItem?.ToString());

        if (query.Length < 2)
        {
            _results.Clear();
            BindResults();
            _statusLabel.Text = "Type at least 2 characters.";
            return;
        }

        try
        {
            _results.Clear();
            _results.AddRange(GlobalSearchService.Search(query, scope));
            BindResults();

            _statusLabel.Text = _results.Count == 0
                ? "No results."
                : $"{_results.Count} result(s).";
        }
        catch (Exception ex)
        {
            _results.Clear();
            BindResults();
            _statusLabel.Text = "Search failed.";
            ControllerDialogs.Warning(ex, "Search failed.");
        }
    }

    private static GlobalSearchScope ParseScope(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "residents" => GlobalSearchScope.Residents,
            "certificates" => GlobalSearchScope.Certificates,
            "blotter" => GlobalSearchScope.Blotter,
            "users" => GlobalSearchScope.Users,
            _ => GlobalSearchScope.All
        };
    }

    private void BindResults()
    {
        var table = new System.Data.DataTable();
        table.Columns.Add("type", typeof(string));
        table.Columns.Add("title", typeof(string));
        table.Columns.Add("subtitle", typeof(string));

        foreach (var r in _results)
        {
            table.Rows.Add(r.EntityType.ToString(), r.Title, r.Subtitle);
        }

        _grid.DataSource = table;

        if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
        }

        UpdateOpenButtonState();
    }

    private void UpdateOpenButtonState()
    {
        _openButton.Enabled = GetSelectedResult() != null;
    }

    private GlobalSearchResult? GetSelectedResult()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            return null;
        }

        int index = _grid.SelectedRows[0].Index;
        return index >= 0 && index < _results.Count ? _results[index] : null;
    }

    private void OpenSelected(bool preferFirst = false)
    {
        GlobalSearchResult? selected = GetSelectedResult();
        if (selected == null && preferFirst && _results.Count > 0)
        {
            selected = _results[0];
        }

        if (selected == null)
        {
            return;
        }

        SelectedResult = selected;
        DialogResult = DialogResult.OK;
        Close();
    }
}
