using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public partial class ResidentModuleControl
{
    private readonly FlowLayoutPanel _residentPickerRow = new FlowLayoutPanel();
    private readonly Button _residentSelectButton = new Button();
    private readonly Label _residentPickerSummary = new Label();

    private void ConfigureResidentPickerControls()
    {
        // Residents now has a dedicated left list pane, so this inline picker row is not needed.
        if (profileContainer != null && profileContainer.Controls.Contains(_residentPickerRow))
        {
            profileContainer.Controls.Remove(_residentPickerRow);
        }
        _residentPickerRow.Visible = false;
    }

    private void UpdateResidentPickerSummary()
    {
        // Inline resident picker disabled in favor of the always-visible resident list pane.
    }

    private void ResidentSelectButton_Click(object? sender, EventArgs e)
    {
        if (_isEditing)
        {
            ControllerDialogs.Warning("Save or cancel your profile edits first.");
            return;
        }

        if (_residentTable == null || _residentTable.Rows.Count == 0)
        {
            LoadResidents();
        }

        if (_residentTable == null || _residentTable.Rows.Count == 0)
        {
            ControllerDialogs.Warning("No residents available to select.");
            return;
        }

        int? pickedResidentId = ShowResidentPickerDialog();
        if (!pickedResidentId.HasValue)
        {
            return;
        }

        if (!SelectResidentById(pickedResidentId.Value))
        {
            ControllerDialogs.Warning("Unable to load the selected resident.");
            return;
        }

        if (_residentTabs != null)
        {
            _residentTabs.SelectedTab = _tabProfile;
        }

        ApplyResponsiveDocking(force: true);
        ResetProfileViewport();
    }

    private int? ShowResidentPickerDialog()
    {
        if (_residentTable == null || _residentTable.Rows.Count == 0)
        {
            return null;
        }

        DataTable pickerTable = BuildResidentPickerTable();
        if (pickerTable.Rows.Count == 0)
        {
            return null;
        }

        using Form pickerForm = new Form
        {
            Text = "Select Resident",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(760, 520),
            MinimumSize = new Size(620, 420),
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.SizableToolWindow
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pickerForm.Controls.Add(root);

        var searchRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 10)
        };
        var searchBox = new TextBox
        {
            Width = 280,
            PlaceholderText = "Search resident..."
        };
        var clearButton = new Button
        {
            Text = "Clear",
            AutoSize = true
        };
        var resultCount = new Label
        {
            AutoSize = true,
            Margin = new Padding(12, 7, 0, 0),
            Font = UiTheme.LabelFont,
            ForeColor = UiTheme.Slate500
        };
        UiTheme.StyleTextBox(searchBox);
        UiTheme.StyleSecondaryButton(clearButton);
        searchRow.Controls.Add(searchBox);
        searchRow.Controls.Add(clearButton);
        searchRow.Controls.Add(resultCount);
        root.Controls.Add(searchRow, 0, 0);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        UiTheme.StyleGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "resident_id",
            DataPropertyName = "resident_id",
            Visible = false
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "full_name",
            DataPropertyName = "full_name",
            HeaderText = "Name",
            FillWeight = 46
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "gender",
            DataPropertyName = "gender",
            HeaderText = "Gender",
            FillWeight = 16
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "status",
            DataPropertyName = "status",
            HeaderText = "Status",
            FillWeight = 18
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "purok_name",
            DataPropertyName = "purok_name",
            HeaderText = "Purok",
            FillWeight = 20
        });

        var pickerView = new DataView(pickerTable);
        var bindingSource = new BindingSource { DataSource = pickerView };
        grid.DataSource = bindingSource;
        root.Controls.Add(grid, 0, 1);

        var actionRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0)
        };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var selectButton = new Button { Text = "Select", AutoSize = true };
        UiTheme.StyleSecondaryButton(cancelButton);
        UiTheme.StylePrimaryButton(selectButton);
        actionRow.Controls.Add(cancelButton);
        actionRow.Controls.Add(selectButton);
        root.Controls.Add(actionRow, 0, 2);

        pickerForm.AcceptButton = selectButton;
        pickerForm.CancelButton = cancelButton;

        int? selectedResidentId = null;

        void UpdateFilter()
        {
            string query = searchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                pickerView.RowFilter = string.Empty;
            }
            else
            {
                string escaped = query.Replace("[", "[[]").Replace("]", "[]]").Replace("'", "''");
                pickerView.RowFilter = $"full_name LIKE '%{escaped}%' OR purok_name LIKE '%{escaped}%'";
            }

            resultCount.Text = $"{pickerView.Count} resident(s)";
        }

        void SelectCurrentResidentIfVisible()
        {
            if (!_selectedResidentId.HasValue)
            {
                if (grid.Rows.Count > 0)
                {
                    grid.Rows[0].Selected = true;
                }
                return;
            }

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["resident_id"]?.Value == null || row.Cells["resident_id"].Value == DBNull.Value)
                {
                    continue;
                }

                if (Convert.ToInt32(row.Cells["resident_id"].Value) == _selectedResidentId.Value)
                {
                    row.Selected = true;
                    grid.CurrentCell = row.Cells["full_name"];
                    return;
                }
            }

            if (grid.Rows.Count > 0)
            {
                grid.Rows[0].Selected = true;
            }
        }

        void AcceptSelection()
        {
            if (grid.SelectedRows.Count == 0)
            {
                ControllerDialogs.Warning("Select a resident first.");
                return;
            }

            object? value = grid.SelectedRows[0].Cells["resident_id"]?.Value;
            if (value == null || value == DBNull.Value)
            {
                ControllerDialogs.Warning("Invalid resident row selected.");
                return;
            }

            selectedResidentId = Convert.ToInt32(value);
            pickerForm.DialogResult = DialogResult.OK;
            pickerForm.Close();
        }

        searchBox.TextChanged += (_, __) =>
        {
            UpdateFilter();
            SelectCurrentResidentIfVisible();
        };

        clearButton.Click += (_, __) =>
        {
            searchBox.Text = string.Empty;
            searchBox.Focus();
        };

        grid.CellDoubleClick += (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                AcceptSelection();
            }
        };

        selectButton.Click += (_, __) => AcceptSelection();
        pickerForm.Shown += (_, __) =>
        {
            UpdateFilter();
            SelectCurrentResidentIfVisible();
            searchBox.Focus();
        };

        DialogResult result = pickerForm.ShowDialog(FindForm());
        return result == DialogResult.OK ? selectedResidentId : null;
    }

    private DataTable BuildResidentPickerTable()
    {
        DataTable table = new DataTable();
        table.Columns.Add("resident_id", typeof(int));
        table.Columns.Add("full_name", typeof(string));
        table.Columns.Add("gender", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("purok_name", typeof(string));

        if (_residentTable == null)
        {
            return table;
        }

        foreach (DataRow row in _residentTable.Rows)
        {
            if (row["resident_id"] == DBNull.Value)
            {
                continue;
            }

            string first = row["firstname"]?.ToString()?.Trim() ?? string.Empty;
            string middle = row["middlename"]?.ToString()?.Trim() ?? string.Empty;
            string last = row["lastname"]?.ToString()?.Trim() ?? string.Empty;
            string fullName = string.Join(" ", new[] { first, middle, last }.Where(part => !string.IsNullOrWhiteSpace(part)));
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = $"Resident #{row["resident_id"]}";
            }

            table.Rows.Add(
                Convert.ToInt32(row["resident_id"]),
                fullName,
                row["gender"]?.ToString() ?? string.Empty,
                row["status"]?.ToString() ?? string.Empty,
                row["purok_name"]?.ToString() ?? string.Empty);
        }

        return table;
    }
}
