using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class AyudaReleaseWindow : Window, IComponentConnector
{
	private readonly AyudaReleaseViewModel _viewModel;

	private bool _contentLoaded;

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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/ayudareleasewindow.xaml", UriKind.Relative);
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
