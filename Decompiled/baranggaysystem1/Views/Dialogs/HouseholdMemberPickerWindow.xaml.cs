using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdMemberPickerWindow : Window
{
	private readonly HouseholdMemberPickerViewModel _vm;

	private readonly DispatcherTimer _searchDebounceTimer;



	public HouseholdMemberPickerWindow(int householdId)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		InitializeComponent();
		_vm = new HouseholdMemberPickerViewModel(householdId);
		base.DataContext = _vm;
		_searchDebounceTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(280.0)
		};
		_searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
		base.Loaded += HouseholdMemberPickerWindow_Loaded;
	}

	private async void HouseholdMemberPickerWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await _vm.LoadResidentsAsync();
	}

	private async void SearchDebounceTimer_Tick(object? sender, EventArgs e)
	{
		_searchDebounceTimer.Stop();
		await _vm.LoadResidentsAsync();
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		_searchDebounceTimer.Stop();
		_searchDebounceTimer.Start();
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		_searchDebounceTimer.Stop();
		await _vm.LoadResidentsAsync();
	}

	private async void BtnAttach_Click(object sender, RoutedEventArgs e)
	{
		await AttachSelectedAsync();
	}

	private async void CandidateList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (_vm.SelectedResident != null)
		{
			await AttachSelectedAsync();
		}
	}

	private async Task AttachSelectedAsync()
	{
		if (await _vm.AttachSelectedAsync())
		{
			base.DialogResult = true;
		}
	}

	private void BtnClose_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}}
