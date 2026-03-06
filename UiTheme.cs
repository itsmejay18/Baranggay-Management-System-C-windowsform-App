using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace baranggaysystem1
{
    internal static class UiTheme
    {
        public static readonly Color Slate900 = Color.FromArgb(18, 18, 18);
        public static readonly Color Slate700 = Color.FromArgb(45, 45, 45);
        public static readonly Color Slate600 = Color.FromArgb(70, 70, 70);
        public static readonly Color Slate500 = Color.FromArgb(96, 96, 96);
        public static readonly Color Slate300 = Color.FromArgb(200, 200, 200);
        public static readonly Color Slate100 = Color.FromArgb(238, 238, 238);
        public static readonly Color Slate50 = Color.FromArgb(250, 250, 250);
        public static readonly Color Teal600 = Color.FromArgb(18, 18, 18);
        public static readonly Color Teal500 = Color.FromArgb(45, 45, 45);
        public static readonly Color Amber500 = Color.FromArgb(96, 96, 96);
        public static readonly Color Rose500 = Color.FromArgb(18, 18, 18);
        public static readonly Color Ink900 = Color.FromArgb(15, 17, 21);
        public static readonly Color Ink800 = Color.FromArgb(22, 26, 34);
        public static readonly Color Ink700 = Color.FromArgb(42, 47, 58);
        public static readonly Color AccentBlue = Color.FromArgb(77, 163, 255);
        public static readonly Color AccentGreen = Color.FromArgb(66, 211, 146);
        public static readonly Color AccentAmber = Color.FromArgb(246, 195, 67);
        public static readonly Color AccentOrange = Color.FromArgb(255, 159, 67);
        public static readonly Color AccentRed = Color.FromArgb(255, 107, 107);

        public static readonly Font TitleFont = new Font("Century Gothic", 20F, FontStyle.Bold);
        public static readonly Font HeadingFont = new Font("Century Gothic", 14F, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Trebuchet MS", 10F, FontStyle.Regular);
        public static readonly Font LabelFont = new Font("Trebuchet MS", 9F, FontStyle.Regular);
        public static readonly Font SmallFont = new Font("Trebuchet MS", 8F, FontStyle.Regular);
        public static readonly Font ButtonFont = new Font("Trebuchet MS", 9.5F, FontStyle.Bold);
        public static readonly Font SectionHeaderFont = new Font(LabelFont, FontStyle.Bold);
        public const int StandardButtonHeight = 36;
        public const int StandardButtonMinWidth = 92;
        public static readonly Padding StandardButtonPadding = new Padding(12, 0, 12, 0);
        public static readonly Padding StandardFlowButtonMargin = new Padding(0, 0, 8, 6);
        public static readonly Padding StandardTableButtonMargin = new Padding(0, 0, 8, 4);
        private static readonly ConditionalWeakTable<Control, FocusVisualState> FocusStates = new ConditionalWeakTable<Control, FocusVisualState>();

        private sealed class FocusVisualState
        {
            public Color BackColor { get; set; }
            public Color BorderColor { get; set; }
            public int BorderSize { get; set; }
            public bool IsButton { get; set; }
        }

        private static void ApplyButtonBaseStyle(Button button)
        {
            button.AutoEllipsis = true;
            button.Padding = StandardButtonPadding;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.MinimumSize = new Size(
                Math.Max(StandardButtonMinWidth, button.MinimumSize.Width),
                Math.Max(StandardButtonHeight, button.MinimumSize.Height));
            if (!button.AutoSize)
            {
                button.Height = Math.Max(button.Height, StandardButtonHeight);
            }
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.MouseOverBackColor = Slate100;
            button.FlatAppearance.MouseDownBackColor = Slate300;
        }

        public static void StylePrimaryButton(Button button)
        {
            button.BackColor = Slate900;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = ButtonFont;
            button.Height = StandardButtonHeight;
            button.UseVisualStyleBackColor = false;
            ApplyButtonBaseStyle(button);
            button.FlatAppearance.MouseOverBackColor = Slate700;
            button.FlatAppearance.MouseDownBackColor = Slate600;
        }

        public static void StylePrimaryButtons(params Button[] buttons)
        {
            foreach (var button in buttons)
            {
                if (button == null) continue;
                StylePrimaryButton(button);
            }
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.BackColor = Color.White;
            button.ForeColor = Slate900;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Slate700;
            button.FlatAppearance.BorderSize = 1;
            button.Font = ButtonFont;
            button.Height = StandardButtonHeight;
            button.UseVisualStyleBackColor = false;
            ApplyButtonBaseStyle(button);
        }

        public static void StyleSecondaryButtons(params Button[] buttons)
        {
            foreach (var button in buttons)
            {
                if (button == null) continue;
                StyleSecondaryButton(button);
            }
        }

        public static void StyleDangerButton(Button button)
        {
            button.BackColor = AccentRed;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Font = ButtonFont;
            button.Height = StandardButtonHeight;
            button.UseVisualStyleBackColor = false;
            ApplyButtonBaseStyle(button);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 88, 88);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(198, 62, 62);
        }

        public static void StyleGhostButton(Button button)
        {
            button.BackColor = Color.Transparent;
            button.ForeColor = Slate900;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Slate100;
            button.Font = ButtonFont;
            button.AutoSize = true;
            button.MinimumSize = new Size(72, 32);
            button.Padding = new Padding(8, 0, 8, 0);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StandardizeButtonLayout(Control root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Control control in root.Controls)
            {
                if (control is Button button)
                {
                    StandardizeSingleButton(button);
                }

                if (control.HasChildren)
                {
                    StandardizeButtonLayout(control);
                }
            }
        }

        public static void EnhanceAccessibility(Control root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Control control in root.Controls)
            {
                if (IsInteractiveControl(control))
                {
                    if (!control.TabStop)
                    {
                        control.TabStop = true;
                    }

                    if (string.IsNullOrWhiteSpace(control.AccessibleName))
                    {
                        control.AccessibleName = ResolveAccessibleName(control);
                    }

                    WireFocusCue(control);
                }

                if (control.HasChildren)
                {
                    EnhanceAccessibility(control);
                }
            }
        }

        public static void SetTabOrder(params Control[] controls)
        {
            if (controls == null || controls.Length == 0)
            {
                return;
            }

            int tabIndex = 0;
            foreach (var control in controls)
            {
                if (control == null)
                {
                    continue;
                }

                control.TabStop = true;
                control.TabIndex = tabIndex++;
            }
        }

        private static void StandardizeSingleButton(Button button)
        {
            button.AutoEllipsis = true;
            if (button.Cursor == Cursors.Default)
            {
                button.Cursor = Cursors.Hand;
            }

            if (button.Font == null || button.Font.Size < 8.5f)
            {
                button.Font = ButtonFont;
            }

            if (button.Padding == Padding.Empty)
            {
                button.Padding = StandardButtonPadding;
            }

            int minWidth = Math.Max(72, button.MinimumSize.Width);
            int minHeight = Math.Max(32, button.MinimumSize.Height);
            if (button.FlatAppearance.BorderSize == 0 || button.FlatStyle != FlatStyle.Standard)
            {
                minWidth = Math.Max(StandardButtonMinWidth, minWidth);
                minHeight = Math.Max(StandardButtonHeight, minHeight);
            }

            button.MinimumSize = new Size(minWidth, minHeight);
            if (!button.AutoSize)
            {
                button.Height = Math.Max(button.Height, minHeight);
            }

            if (button.Parent is FlowLayoutPanel)
            {
                if (button.Margin == Padding.Empty || button.Margin == new Padding(3))
                {
                    button.Margin = StandardFlowButtonMargin;
                }
            }
            else if (button.Parent is TableLayoutPanel)
            {
                if (button.Margin == Padding.Empty || button.Margin == new Padding(3))
                {
                    button.Margin = StandardTableButtonMargin;
                }
            }
        }

        private static bool IsInteractiveControl(Control control)
        {
            return control is Button
                || control is TextBox
                || control is ComboBox
                || control is DateTimePicker
                || control is CheckBox
                || control is RadioButton
                || control is LinkLabel
                || control is DataGridView
                || control is TabControl
                || control is NumericUpDown;
        }

        private static string ResolveAccessibleName(Control control)
        {
            if (control is Button button && !string.IsNullOrWhiteSpace(button.Text))
            {
                return button.Text;
            }

            if (control is Label label && !string.IsNullOrWhiteSpace(label.Text))
            {
                return label.Text;
            }

            if (!string.IsNullOrWhiteSpace(control.Text))
            {
                return control.Text;
            }

            return control.Name;
        }

        private static void WireFocusCue(Control control)
        {
            if (FocusStates.TryGetValue(control, out _))
            {
                return;
            }

            var state = new FocusVisualState
            {
                BackColor = control.BackColor
            };

            if (control is Button button)
            {
                state.IsButton = true;
                state.BorderColor = button.FlatAppearance.BorderColor;
                state.BorderSize = button.FlatAppearance.BorderSize;
            }

            FocusStates.Add(control, state);

            control.Enter += (_, __) =>
            {
                if (!FocusStates.TryGetValue(control, out var visual))
                {
                    return;
                }

                if (control is Button focusedButton)
                {
                    focusedButton.FlatAppearance.BorderColor = AccentBlue;
                    focusedButton.FlatAppearance.BorderSize = Math.Max(2, visual.BorderSize);
                }
                else if (control is TextBox textBox && !textBox.ReadOnly)
                {
                    textBox.BackColor = Blend(Color.White, AccentBlue, 10);
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = Blend(Color.White, AccentBlue, 8);
                }
                else if (control is DateTimePicker picker)
                {
                    picker.CalendarMonthBackground = Blend(Color.White, AccentBlue, 8);
                }
            };

            control.Leave += (_, __) =>
            {
                if (!FocusStates.TryGetValue(control, out var visual))
                {
                    return;
                }

                if (control is Button focusedButton)
                {
                    focusedButton.FlatAppearance.BorderColor = visual.BorderColor;
                    focusedButton.FlatAppearance.BorderSize = visual.BorderSize;
                }
                else if (control is TextBox textBox && !textBox.ReadOnly)
                {
                    textBox.BackColor = visual.BackColor;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = visual.BackColor;
                }
                else if (control is DateTimePicker picker)
                {
                    picker.CalendarMonthBackground = visual.BackColor;
                }
            };
        }

        public static void StyleSectionCard(Panel panel, Color? backColor = null, bool enforceBorder = true, Padding? padding = null)
        {
            if (panel == null)
            {
                return;
            }

            panel.BackColor = backColor ?? Color.White;
            if (enforceBorder && panel.BorderStyle == BorderStyle.None)
            {
                panel.BorderStyle = BorderStyle.FixedSingle;
            }

            if (padding.HasValue)
            {
                panel.Padding = padding.Value;
            }
        }

        public static void StyleSectionHeader(Label label, Color? color = null, bool useHeadingFont = false)
        {
            if (label == null)
            {
                return;
            }

            label.Font = useHeadingFont ? HeadingFont : SectionHeaderFont;
            label.ForeColor = color ?? Slate900;
            label.AutoEllipsis = true;
        }

        public static void StyleGridContainer(Panel panel, DataGridView? grid = null, Padding? padding = null)
        {
            if (panel == null)
            {
                return;
            }

            StyleSectionCard(panel, Color.White, enforceBorder: true, padding: padding);
            if (grid != null)
            {
                StyleGrid(grid);
                grid.BackgroundColor = Color.White;
            }
        }

        public static Color Blend(Color from, Color to, int toPercent)
        {
            int percent = Math.Clamp(toPercent, 0, 100);
            int inv = 100 - percent;
            int r = (from.R * inv + to.R * percent) / 100;
            int g = (from.G * inv + to.G * percent) / 100;
            int b = (from.B * inv + to.B * percent) / 100;
            return Color.FromArgb(r, g, b);
        }

        public static Panel CreateStateCard(
            string title,
            string message,
            IconChar icon = IconChar.CircleInfo,
            Color? accent = null,
            string? primaryActionText = null,
            Action? primaryAction = null,
            string? secondaryActionText = null,
            Action? secondaryAction = null)
        {
            Color resolvedAccent = accent ?? Slate500;
            var card = new Panel
            {
                BackColor = Blend(Color.White, resolvedAccent, 7),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12, 10, 12, 10),
                Height = (primaryAction != null || secondaryAction != null) ? 132 : 108
            };

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Margin = new Padding(0)
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var iconView = new IconPictureBox
            {
                IconChar = icon,
                IconColor = resolvedAccent,
                IconSize = 20,
                IconFont = IconFont.Auto,
                BackColor = Color.Transparent,
                Size = new Size(26, 26),
                Margin = new Padding(0, 2, 10, 0)
            };

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            content.Controls.Add(new Label
            {
                AutoSize = true,
                Text = title,
                Font = new Font(BodyFont, FontStyle.Bold),
                ForeColor = Slate900,
                Margin = new Padding(0, 0, 0, 4)
            }, 0, 0);

            content.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                Text = message,
                Font = LabelFont,
                ForeColor = Slate600,
                MaximumSize = new Size(680, 0),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 1);

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            if (!string.IsNullOrWhiteSpace(primaryActionText) && primaryAction != null)
            {
                actions.Controls.Add(CreateStateActionButton(primaryActionText, primaryAction, true));
            }

            if (!string.IsNullOrWhiteSpace(secondaryActionText) && secondaryAction != null)
            {
                actions.Controls.Add(CreateStateActionButton(secondaryActionText, secondaryAction, false));
            }

            if (actions.Controls.Count > 0)
            {
                content.Controls.Add(actions, 0, 2);
            }

            shell.Controls.Add(iconView, 0, 0);
            shell.Controls.Add(content, 1, 0);
            card.Controls.Add(shell);
            return card;
        }

        public static void ConfigureStateLabels(Label titleLabel, Label messageLabel, int maxMessageWidth = 420)
        {
            titleLabel.Font = new Font(BodyFont, FontStyle.Bold);
            titleLabel.ForeColor = Slate700;
            titleLabel.AutoSize = true;
            titleLabel.Margin = new Padding(0, 0, 0, 6);

            messageLabel.Font = LabelFont;
            messageLabel.ForeColor = Slate500;
            messageLabel.AutoSize = true;
            messageLabel.MaximumSize = new Size(maxMessageWidth, 0);
        }

        private static Button CreateStateActionButton(string text, Action clickAction, bool primary)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 3, 10, 3),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat
            };

            if (primary)
            {
                StylePrimaryButton(button);
            }
            else
            {
                StyleSecondaryButton(button);
            }

            button.Click += (_, __) => clickAction();
            return button;
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Color.White;
            textBox.ForeColor = Slate900;
            textBox.Font = BodyFont;
        }

        public static void StyleTextBoxes(params TextBox[] textBoxes)
        {
            foreach (var textBox in textBoxes)
            {
                if (textBox == null) continue;
                StyleTextBox(textBox);
            }
        }

        public static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = Color.White;
            comboBox.ForeColor = Slate900;
            comboBox.Font = BodyFont;
        }

        public static void StyleComboBoxes(params ComboBox[] comboBoxes)
        {
            foreach (var comboBox in comboBoxes)
            {
                if (comboBox == null) continue;
                StyleComboBox(comboBox);
            }
        }

        public static void ApplyLabelFont(Font font, params Label[] labels)
        {
            foreach (var label in labels)
            {
                if (label == null) continue;
                label.Font = font;
            }
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(228, 228, 228);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Slate900;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(BodyFont, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.ForeColor = Slate900;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Slate700;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            grid.RowHeadersVisible = false;
            grid.AllowUserToResizeRows = false;
            grid.RowTemplate.Height = 34;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        public static void AttachGradient(Control control, Color startColor, Color endColor, float angle)
        {
            control.Paint += (_, e) =>
            {
                var rect = control.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                using var brush = new LinearGradientBrush(rect, startColor, endColor, angle);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillRectangle(brush, rect);
            };
        }

        public static void ApplyCardStyle(Panel panel)
        {
            if (panel.Tag is string tag && tag == "card")
            {
                return;
            }

            panel.Tag = "card";
            panel.BackColor = Color.Transparent;
            panel.Padding = new Padding(40, 36, 40, 36);
            panel.Resize += (_, __) => panel.Invalidate();

            panel.Paint += (_, e) =>
            {
                var rect = panel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                int shadow = 14;
                int radius = 14;

                var cardRect = new Rectangle(
                    shadow,
                    shadow,
                    rect.Width - shadow * 2,
                    rect.Height - shadow * 2);

                if (cardRect.Width <= 0 || cardRect.Height <= 0) return;

                var shadowRect = new Rectangle(
                    cardRect.X + 2,
                    cardRect.Y + 4,
                    cardRect.Width,
                    cardRect.Height);

                using var shadowPath = CreateRoundedRectangle(shadowRect, radius);
                using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
                e.Graphics.FillPath(shadowBrush, shadowPath);

                using var cardPath = CreateRoundedRectangle(cardRect, radius);
                using var cardBrush = new SolidBrush(Color.White);
                e.Graphics.FillPath(cardBrush, cardPath);

                using var borderPen = new Pen(Color.FromArgb(30, 0, 0, 0), 1f);
                e.Graphics.DrawPath(borderPen, cardPath);
            };
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
