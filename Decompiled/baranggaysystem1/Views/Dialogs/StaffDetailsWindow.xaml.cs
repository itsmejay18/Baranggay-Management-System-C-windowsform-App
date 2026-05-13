using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.Models;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class StaffDetailsWindow : Window
{
	public StaffDetailsWindow(StaffProfileDetails? existingRecord = null)
	{
		InitializeComponent();
		StaffDetailsViewModel dataContext = new StaffDetailsViewModel(existingRecord)
		{
			CloseAction = delegate
			{
				base.DialogResult = true;
				Close();
			}
		};
		base.DataContext = dataContext;
	}}
