using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class HouseholdCertificateWindow : Window
{
	public HouseholdCertificateWindow(int householdId)
	{
		InitializeComponent();
		HouseholdCertificateViewModel dataContext = new HouseholdCertificateViewModel(householdId)
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
