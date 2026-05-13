using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views;

/// <summary>
/// Dialog for setting up security questions for self-service password reset.
/// </summary>
public partial class SecurityQuestionsWindow : Window
{
    public SecurityQuestionsWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        string question1 = (question1Combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        string answer1 = answer1Box.Password?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(question1))
        {
            ShowStatus("Please select a security question.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(answer1) || answer1.Length < 2)
        {
            ShowStatus("Answer must be at least 2 characters.", isError: true);
            return;
        }

        string? question2 = null;
        string? answer2 = null;

        var q2Item = question2Combo.SelectedItem as ComboBoxItem;
        if (q2Item != null && q2Item.Content?.ToString() != "(None)")
        {
            question2 = q2Item.Content?.ToString();
            answer2 = answer2Box.Password?.Trim();

            if (!string.IsNullOrWhiteSpace(question2) && string.IsNullOrWhiteSpace(answer2))
            {
                ShowStatus("Please provide an answer for question 2, or select (None).", isError: true);
                return;
            }
        }

        bool success = PasswordResetService.SetSecurityQuestions(
            UserSession.UserId, question1, answer1, question2, answer2);

        if (success)
        {
            ShowStatus("Security questions saved successfully.", isError: false);
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                DialogResult = true;
                Close();
            };
            timer.Start();
        }
        else
        {
            ShowStatus("Failed to save security questions. Please try again.", isError: true);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowStatus(string message, bool isError)
    {
        statusLabel.Text = message;
        statusLabel.Foreground = isError
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
        statusLabel.Visibility = Visibility.Visible;
    }
}
