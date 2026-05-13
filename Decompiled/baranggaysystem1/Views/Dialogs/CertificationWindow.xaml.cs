using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class CertificationWindow : Window
{
	private readonly CertificationViewModel _viewModel;
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
	}}
