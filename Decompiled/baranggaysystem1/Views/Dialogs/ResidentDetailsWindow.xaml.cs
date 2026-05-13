using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class ResidentDetailsWindow : Window
{
	public ResidentDetailsWindow(ResidentDto? existingResident = null)
	{
		InitializeComponent();
		base.DataContext = new ResidentDetailsViewModel(existingResident)
		{
			CloseAction = delegate(bool result)
			{
				base.DialogResult = result;
				Close();
			}
		};
	}}
