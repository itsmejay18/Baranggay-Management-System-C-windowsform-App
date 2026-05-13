using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class BlotterDetailsWindow : Window, IComponentConnector
{
	private readonly BlotterDetailsViewModel _viewModel;

	private bool _contentLoaded;

	public BlotterDetailsWindow(BlotterDto? existingRecord = null)
	{
		InitializeComponent();
		base.WindowState = WindowState.Maximized;
		_viewModel = new BlotterDetailsViewModel(existingRecord)
		{
			CloseAction = base.Close
		};
		base.DataContext = _viewModel;
		base.Loaded += OnLoadedAsync;
	}

	private async void OnLoadedAsync(object sender, RoutedEventArgs e)
	{
		base.Loaded -= OnLoadedAsync;
		try
		{
			await _viewModel.InitializeAsync();
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError(ex.Message, "Blotter");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/blotterdetailswindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
