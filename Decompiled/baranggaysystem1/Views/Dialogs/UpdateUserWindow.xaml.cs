using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class UpdateUserWindow : Window
{
	public UpdateUserWindow(int targetUserId, string targetUsername)
	{
		InitializeComponent();
		UpdateUserViewModel dataContext = new UpdateUserViewModel(targetUserId, targetUsername)
		{
			CloseAction = delegate
			{
				base.DialogResult = true;
				Close();
			}
		};
		base.DataContext = dataContext;
	}}
