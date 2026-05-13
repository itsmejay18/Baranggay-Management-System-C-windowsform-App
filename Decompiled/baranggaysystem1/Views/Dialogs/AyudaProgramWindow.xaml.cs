using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class AyudaProgramWindow : Window
{
	private readonly AyudaProgramViewModel _viewModel;
	public AyudaProgramWindow()
		: this(null)
	{
	}

	public AyudaProgramWindow(int? programId)
	{
		InitializeComponent();
		_viewModel = new AyudaProgramViewModel(programId);
		base.DataContext = _viewModel;
		base.Loaded += AyudaProgramWindow_Loaded;
		_viewModel.CloseRequested += HandleCloseRequested;
	}

	private async void AyudaProgramWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await _viewModel.InitializeAsync();
	}

	private void HandleCloseRequested(bool? dialogResult)
	{
		if (!((DispatcherObject)this).Dispatcher.CheckAccess())
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
			{
				HandleCloseRequested(dialogResult);
			});
		}
		else if (dialogResult.HasValue)
		{
			base.DialogResult = dialogResult.Value;
		}
		else
		{
			Close();
		}
	}}
