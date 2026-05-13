using System;
using System.Windows;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views;

/// <summary>
/// Self-service password reset window using security questions.
/// Accessible from the login screen.
/// </summary>
public partial class PasswordResetWindow : Window
{
    private SecurityQuestionSet? _questions;

    public PasswordResetWindow()
    {
        InitializeComponent();
    }

    private void UsernameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Reset state when username changes
        questionsPanel.Visibility = Visibility.Collapsed;
        noQuestionsLabel.Visibility = Visibility.Collapsed;
        resetButton.Visibility = Visibility.Collapsed;
        resultPanel.Visibility = Visibility.Collapsed;
        errorLabel.Visibility = Visibility.Collapsed;
        _questions = null;
    }

    private void LookupButton_Click(object sender, RoutedEventArgs e)
    {
        string username = usernameBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Please enter your username.");
            return;
        }

        try
        {
            _questions = PasswordResetService.GetSecurityQuestions(username);

            if (_questions == null)
            {
                noQuestionsLabel.Visibility = Visibility.Visible;
                questionsPanel.Visibility = Visibility.Collapsed;
                resetButton.Visibility = Visibility.Collapsed;
                return;
            }

            // Show questions
            question1Label.Text = _questions.Question1;
            questionsPanel.Visibility = Visibility.Visible;
            resetButton.Visibility = Visibility.Visible;
            noQuestionsLabel.Visibility = Visibility.Collapsed;
            lookupButton.Visibility = Visibility.Collapsed;

            if (_questions.HasQuestion2)
            {
                question2Label.Text = _questions.Question2;
                question2Label.Visibility = Visibility.Visible;
                answer2Box.Visibility = Visibility.Visible;
            }
            else
            {
                question2Label.Visibility = Visibility.Collapsed;
                answer2Box.Visibility = Visibility.Collapsed;
            }

            answer1Box.Focus();
        }
        catch (Exception ex)
        {
            ShowError($"Error looking up account: {ex.Message}");
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        string username = usernameBox.Text?.Trim() ?? "";
        string answer1 = answer1Box.Text?.Trim() ?? "";
        string? answer2 = answer2Box.Visibility == Visibility.Visible
            ? answer2Box.Text?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(answer1))
        {
            ShowError("Please answer the security question.");
            return;
        }

        try
        {
            var result = PasswordResetService.ResetViaSecurityQuestions(username, answer1, answer2);

            if (result.IsSuccess)
            {
                errorLabel.Visibility = Visibility.Collapsed;
                resultPanel.Visibility = Visibility.Visible;
                resultLabel.Text = result.Message;
                tempPasswordLabel.Text = $"Temporary Password: {result.TemporaryPassword}";
                resetButton.IsEnabled = false;

                // Hide input fields
                questionsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowError(result.Message);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Reset failed: {ex.Message}");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShowError(string message)
    {
        errorLabel.Text = message;
        errorLabel.Visibility = Visibility.Visible;
        resultPanel.Visibility = Visibility.Collapsed;
    }
}
