using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace baranggaysystem1.Views.Dialogs;

public partial class EllieAssistantWindow : Window
{
	private readonly EllieAssistantService _service = new EllieAssistantService();
	private CancellationTokenSource? _cts;

	public EllieAssistantWindow()
	{
		InitializeComponent();
		txtQuestion.Focus();
	}

	private async void BtnSend_Click(object sender, RoutedEventArgs e)
	{
		await SendQuestionAsync();
	}

	private async void TxtQuestion_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtQuestion.Text))
		{
			await SendQuestionAsync();
		}
	}

	private async Task SendQuestionAsync()
	{
		string question = txtQuestion.Text.Trim();
		if (string.IsNullOrWhiteSpace(question))
			return;

		txtQuestion.Text = string.Empty;
		txtQuestion.IsEnabled = false;
		btnSend.IsEnabled = false;

		AddChatBubble(question, isUser: true);
		AddChatBubble("Thinking...", isUser: false, isLoading: true);

		_cts?.Cancel();
		_cts = new CancellationTokenSource();

		try
		{
			string answer = await Task.Run(() => _service.AskAsync(question, _cts.Token));
			RemoveLastBubble();
			AddChatBubble(answer, isUser: false);
		}
		catch (OperationCanceledException)
		{
			RemoveLastBubble();
		}
		catch (Exception ex)
		{
			RemoveLastBubble();
			AddChatBubble("Sorry, I couldn't process that: " + ex.Message, isUser: false);
		}
		finally
		{
			txtQuestion.IsEnabled = true;
			btnSend.IsEnabled = true;
			txtQuestion.Focus();
		}
	}

	private void AddChatBubble(string text, bool isUser, bool isLoading = false)
	{
		var bubble = new Border
		{
			Background = new SolidColorBrush(isUser ? (Color)ColorConverter.ConvertFromString("#E0F2FE") : (Color)ColorConverter.ConvertFromString("#F1F5F9")),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 8, 12, 8),
			Margin = isUser ? new Thickness(40, 0, 0, 8) : new Thickness(0, 0, 40, 8),
			HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
			Tag = isLoading ? "loading" : null
		};

		var tb = new TextBlock
		{
			Text = text,
			TextWrapping = TextWrapping.Wrap,
			FontSize = 12,
			Foreground = new SolidColorBrush(isUser ? (Color)ColorConverter.ConvertFromString("#0C4A6E") : (Color)ColorConverter.ConvertFromString("#1E293B")),
			FontStyle = isLoading ? FontStyles.Italic : FontStyles.Normal
		};

		bubble.Child = tb;
		chatPanel.Children.Add(bubble);
		chatScroller.ScrollToEnd();
	}

	private void RemoveLastBubble()
	{
		if (chatPanel.Children.Count > 0)
		{
			var last = chatPanel.Children[chatPanel.Children.Count - 1] as Border;
			if (last?.Tag as string == "loading")
				chatPanel.Children.RemoveAt(chatPanel.Children.Count - 1);
		}
	}
}
