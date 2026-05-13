using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdEditorWindow : Window
{
	private readonly HouseholdEditorViewModel _vm;
	public int SavedHouseholdId => _vm.SavedHouseholdId;

	public bool WasNewRecord => !_vm.IsEditMode;

	public HouseholdEditorWindow(int? householdId = null)
	{
		InitializeComponent();
		_vm = new HouseholdEditorViewModel(householdId)
		{
			CloseAction = delegate(bool saved)
			{
				if (saved)
				{
					base.DialogResult = true;
				}
				else
				{
					Close();
				}
			}
		};
		base.DataContext = _vm;
	}}
