using System;
using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1.Controls
{
    public class ActionCalendarControl : UserControl
    {
        private readonly TableLayoutPanel _root = new TableLayoutPanel();
        private readonly Panel _headerPanel = new Panel();
        private readonly Label _monthLabel = new Label();
        private readonly Button _prevButton = new Button();
        private readonly Button _nextButton = new Button();
        private readonly TableLayoutPanel _grid = new TableLayoutPanel();
        private readonly Label[,] _cells = new Label[6, 7];

        private DateTime _displayMonth;
        private DateTime _selectedDate;

        public event EventHandler? SelectedDateChanged;

        public ActionCalendarControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;

            BuildLayout();
            ApplyTheme();
            BuildGrid();
            UpdateCalendar();
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (value.Date == _selectedDate.Date)
                {
                    return;
                }

                _selectedDate = value.Date;
                _displayMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
                UpdateCalendar();
                SelectedDateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Color AccentColor { get; set; } = Color.FromArgb(77, 163, 255);
        public Color MutedTextColor { get; set; } = Color.FromArgb(120, 120, 120);
        public Color HeaderBackColor { get; set; } = Color.FromArgb(245, 247, 250);

        public void ApplyTheme()
        {
            Font = new Font(UiTheme.BodyFont.FontFamily, 11F, FontStyle.Regular);
            _monthLabel.Font = new Font(UiTheme.BodyFont.FontFamily, 13F, FontStyle.Bold);
            _monthLabel.ForeColor = UiTheme.Slate900;

            _headerPanel.BackColor = HeaderBackColor;
            _prevButton.BackColor = Color.White;
            _nextButton.BackColor = Color.White;
            _prevButton.ForeColor = UiTheme.Slate700;
            _nextButton.ForeColor = UiTheme.Slate700;
            _prevButton.FlatStyle = FlatStyle.Flat;
            _nextButton.FlatStyle = FlatStyle.Flat;
            _prevButton.FlatAppearance.BorderSize = 0;
            _nextButton.FlatAppearance.BorderSize = 0;
            _prevButton.Cursor = Cursors.Hand;
            _nextButton.Cursor = Cursors.Hand;

            _grid.BackColor = Color.White;
        }

        private void BuildLayout()
        {
            _root.Dock = DockStyle.Fill;
            _root.RowCount = 2;
            _root.ColumnCount = 1;
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(_root);

            _headerPanel.Dock = DockStyle.Fill;
            _headerPanel.Padding = new Padding(10, 6, 10, 6);
            _root.Controls.Add(_headerPanel, 0, 0);

            _prevButton.Text = "<";
            _prevButton.Width = 28;
            _prevButton.Height = 28;
            _prevButton.Location = new Point(8, 8);
            _prevButton.Click += (_, __) => ChangeMonth(-1);

            _nextButton.Text = ">";
            _nextButton.Width = 28;
            _nextButton.Height = 28;
            _nextButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _nextButton.Location = new Point(Width - 36, 8);
            _nextButton.Click += (_, __) => ChangeMonth(1);

            _monthLabel.AutoSize = true;
            _monthLabel.Location = new Point(48, 10);

            _headerPanel.Controls.Add(_monthLabel);
            _headerPanel.Controls.Add(_prevButton);
            _headerPanel.Controls.Add(_nextButton);
            _headerPanel.Resize += (_, __) =>
            {
                _nextButton.Location = new Point(_headerPanel.Width - 36, 8);
            };

            _grid.Dock = DockStyle.Fill;
            _grid.ColumnCount = 7;
            _grid.RowCount = 7;
            for (int col = 0; col < 7; col++)
            {
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            }
            _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            for (int row = 1; row < 7; row++)
            {
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 6F));
            }

            _root.Controls.Add(_grid, 0, 1);
        }

        private void BuildGrid()
        {
            string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int col = 0; col < 7; col++)
            {
                var label = new Label
                {
                    Text = dayNames[col],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(UiTheme.BodyFont.FontFamily, 10F, FontStyle.Bold),
                    ForeColor = UiTheme.Slate600
                };
                _grid.Controls.Add(label, col, 0);
            }

            for (int row = 1; row < 7; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var cell = new Label
                    {
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(4),
                        BackColor = Color.White,
                        ForeColor = UiTheme.Slate900,
                        Cursor = Cursors.Hand
                    };
                    cell.Click += OnCellClick;
                    _cells[row - 1, col] = cell;
                    _grid.Controls.Add(cell, col, row);
                }
            }
        }

        private void ChangeMonth(int delta)
        {
            _displayMonth = _displayMonth.AddMonths(delta);
            UpdateCalendar();
        }

        private void UpdateCalendar()
        {
            _monthLabel.Text = _displayMonth.ToString("MMMM yyyy");

            DateTime firstDay = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
            int startCol = (int)firstDay.DayOfWeek;
            DateTime cursor = firstDay.AddDays(-startCol);

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var cell = _cells[row, col];
                    cell.Tag = cursor.Date;
                    cell.Text = cursor.Day.ToString();

                    bool isCurrentMonth = cursor.Month == _displayMonth.Month;
                    bool isSelected = cursor.Date == _selectedDate.Date;
                    bool isToday = cursor.Date == DateTime.Today;

                    cell.ForeColor = isCurrentMonth ? UiTheme.Slate900 : MutedTextColor;
                    cell.BackColor = Color.White;
                    cell.Font = new Font(UiTheme.BodyFont.FontFamily, 10.5F, FontStyle.Regular);

                    if (isToday && !isSelected)
                    {
                        cell.BackColor = Blend(Color.White, AccentColor, 10);
                        cell.Font = new Font(UiTheme.BodyFont.FontFamily, 10.5F, FontStyle.Bold);
                    }

                    if (isSelected)
                    {
                        cell.BackColor = Blend(Color.White, AccentColor, 22);
                        cell.ForeColor = AccentColor;
                        cell.Font = new Font(UiTheme.BodyFont.FontFamily, 10.5F, FontStyle.Bold);
                    }

                    cursor = cursor.AddDays(1);
                }
            }
        }

        private void OnCellClick(object? sender, EventArgs e)
        {
            if (sender is Label label && label.Tag is DateTime date)
            {
                SelectedDate = date;
            }
        }

        private static Color Blend(Color baseColor, Color overlay, int percent)
        {
            int r = baseColor.R + (overlay.R - baseColor.R) * percent / 100;
            int g = baseColor.G + (overlay.G - baseColor.G) * percent / 100;
            int b = baseColor.B + (overlay.B - baseColor.B) * percent / 100;
            return Color.FromArgb(r, g, b);
        }
    }
}
