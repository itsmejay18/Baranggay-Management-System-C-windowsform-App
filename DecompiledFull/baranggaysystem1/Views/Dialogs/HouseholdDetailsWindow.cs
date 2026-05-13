using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class HouseholdDetailsWindow : Window, IComponentConnector
{
	private readonly HouseholdDetailsViewModel _vm;

	private bool _contentLoaded;

	public HouseholdDetailsWindow(int? householdId, int? currentResidentId = null)
	{
		InitializeComponent();
		_vm = new HouseholdDetailsViewModel(householdId, currentResidentId);
		base.DataContext = _vm;
	}

	private async void BtnAddMember_Click(object sender, RoutedEventArgs e)
	{
		if (!_vm.CanManageMembers || !_vm.HouseholdId.HasValue)
		{
			DialogService.Instance.ShowWarning("Create or select a household first before adding family members.");
		}
		else if (new HouseholdMemberPickerWindow(_vm.HouseholdId.Value)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			_vm.MarkChanged();
			await _vm.ReloadAsync();
		}
	}

	private void BtnClose_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = _vm.HasChanges;
	}

	private void BtnConfirm_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/householddetailswindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += BtnAddMember_Click;
			break;
		case 2:
			((Button)target).Click += BtnClose_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
