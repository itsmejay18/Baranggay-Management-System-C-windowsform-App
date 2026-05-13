using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class AyudaReleaseWindow : Window
{
	private readonly AyudaReleaseViewModel _viewModel;
	public AyudaReleaseWindow()
		: this(null)
	{
	}

	public AyudaReleaseWindow(int? initialProgramId, int? releaseId = null)
	{
		InitializeComponent();
		_viewModel = new AyudaReleaseViewModel(initialProgramId, releaseId);
		base.DataContext = _viewModel;
		base.Loaded += AyudaReleaseWindow_Loaded;
		_viewModel.CloseRequested += HandleCloseRequested;
	}

	private async void AyudaReleaseWindow_Loaded(object sender, RoutedEventArgs e)
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
