using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdTransferWindow : Window
{
	public HouseholdTransferWindow(int householdId)
	{
		InitializeComponent();
		HouseholdTransferViewModel dataContext = new HouseholdTransferViewModel(householdId)
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
		base.DataContext = dataContext;
	}}
