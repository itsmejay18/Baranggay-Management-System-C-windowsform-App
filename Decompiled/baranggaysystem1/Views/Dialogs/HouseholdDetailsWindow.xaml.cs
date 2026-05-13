using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdDetailsWindow : Window
{
	private readonly HouseholdDetailsViewModel _vm;
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
		}.ShowDialog().GetValueOrDefault())
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
	}}
