using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class PaymentWindow : Window, IComponentConnector
{
	private readonly PaymentViewModel _viewModel;

	private bool _contentLoaded;

	public PaymentWindow()
		: this(new PaymentViewModel())
	{
	}

	public PaymentWindow(int residentId, string residentName)
		: this(new PaymentViewModel(residentId, residentName))
	{
	}

	private PaymentWindow(PaymentViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		base.DataContext = _viewModel;
		base.Loaded += PaymentWindow_Loaded;
		_viewModel.CloseRequested += delegate(bool? dialogResult)
		{
			base.DialogResult = dialogResult;
		};
	}

	private async void PaymentWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await _viewModel.InitializeAsync();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/paymentwindow.xaml", UriKind.Relative);
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
