using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class CertificationWindow : Window, IComponentConnector
{
	private readonly CertificationViewModel _viewModel;

	private bool _contentLoaded;

	public CertificationWindow()
		: this(new CertificationViewModel())
	{
	}

	public CertificationWindow(int residentId, string residentName)
		: this(new CertificationViewModel(residentId, residentName))
	{
	}

	public CertificationWindow(CertificateDialogMode mode)
		: this(new CertificationViewModel(mode))
	{
	}

	public CertificationWindow(int requestId, CertificateDialogMode mode, bool loadExistingRequest)
		: this(new CertificationViewModel(requestId, mode, loadExistingRequest))
	{
	}

	private CertificationWindow(CertificationViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		base.DataContext = _viewModel;
		base.Loaded += CertificationWindow_Loaded;
		_viewModel.CloseRequested += ViewModel_CloseRequested;
	}

	private async void CertificationWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await _viewModel.InitializeAsync();
	}

	private void ViewModel_CloseRequested(bool? dialogResult)
	{
		base.DialogResult = dialogResult;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/certificationwindow.xaml", UriKind.Relative);
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
