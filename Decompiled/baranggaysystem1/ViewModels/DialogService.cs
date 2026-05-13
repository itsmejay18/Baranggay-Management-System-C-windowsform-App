using System;
using System.Windows;

namespace baranggaysystem1.ViewModels;

public sealed class DialogService
{
	private static readonly Lazy<DialogService> _instance = new Lazy<DialogService>(() => new DialogService());

	public static DialogService Instance => _instance.Value;

	private DialogService()
	{
	}

	public void ShowInfo(string message, string title = "Information")
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	public void ShowWarning(string message, string title = "Warning")
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
	}

	public void ShowError(string message, string title = "Error")
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	public bool Confirm(string message, string title = "Confirm")
	{
		return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
	}

	public bool? ShowDialog(Window window)
	{
		window.Owner = Application.Current.MainWindow;
		return window.ShowDialog();
	}
}
