using System.Drawing;
using System.Windows.Forms;

namespace baranggaysystem1.Controls;

internal sealed class LoadingOverlay : Panel
{
    private readonly TableLayoutPanel _root = new TableLayoutPanel();
    private readonly Panel _card = new Panel();
    private readonly TableLayoutPanel _cardLayout = new TableLayoutPanel();
    private readonly Label _title = new Label();
    private readonly ProgressBar _progress = new ProgressBar();

    public LoadingOverlay()
    {
        Dock = DockStyle.Fill;
        Visible = false;
        BackColor = UiTheme.Slate50;

        _root.Dock = DockStyle.Fill;
        _root.ColumnCount = 3;
        _root.RowCount = 3;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _card.AutoSize = true;
        _card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _card.Padding = new Padding(16, 14, 16, 14);
        _card.BackColor = Color.White;
        _card.BorderStyle = BorderStyle.FixedSingle;

        _cardLayout.AutoSize = true;
        _cardLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cardLayout.Dock = DockStyle.Fill;
        _cardLayout.ColumnCount = 1;
        _cardLayout.RowCount = 2;
        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _title.AutoSize = true;
        _title.Text = "Loading...";
        _title.Font = new Font(UiTheme.BodyFont, FontStyle.Bold);
        _title.ForeColor = UiTheme.Slate700;
        _title.Margin = new Padding(0, 0, 0, 8);

        _progress.Width = 220;
        _progress.Height = 12;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Maximum = 100;
        _progress.Value = 100;
        _progress.Margin = new Padding(0);

        _cardLayout.Controls.Add(_title, 0, 0);
        _cardLayout.Controls.Add(_progress, 0, 1);
        _card.Controls.Add(_cardLayout);
        _root.Controls.Add(_card, 1, 1);
        Controls.Add(_root);
    }

    public void ShowLoading(string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _title.Text = message.Trim();
        }

        Visible = true;
        BringToFront();
    }

    public void HideLoading()
    {
        Visible = false;
    }
}
