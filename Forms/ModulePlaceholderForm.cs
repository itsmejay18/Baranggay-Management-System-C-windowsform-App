using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1;

public sealed class ModulePlaceholderForm : Form
{
    public ModulePlaceholderForm(string moduleName, string subtitle, string message)
    {
        Name = $"{moduleName.Replace(" ", string.Empty)}PlaceholderForm";
        Text = moduleName;
        BackColor = UiTheme.Slate100;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        FormBorderStyle = FormBorderStyle.None;

        var pagePadding = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            BackColor = UiTheme.Slate100
        };

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        UiTheme.StyleSectionCard(card, Color.White, enforceBorder: true);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.White
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var title = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Text = moduleName,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = UiTheme.Slate900,
            Margin = new Padding(0, 0, 0, 8)
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Text = subtitle,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = UiTheme.Slate700,
            Margin = new Padding(0, 0, 0, 10)
        };

        var messageLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Text = message,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = UiTheme.Slate600,
            Margin = new Padding(0)
        };

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitleLabel, 0, 1);
        layout.Controls.Add(messageLabel, 0, 2);

        card.Controls.Add(layout);
        pagePadding.Controls.Add(card);
        Controls.Add(pagePadding);
    }
}
