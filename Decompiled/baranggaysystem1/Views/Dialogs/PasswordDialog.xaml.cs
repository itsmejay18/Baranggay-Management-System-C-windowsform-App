using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace baranggaysystem1.Views.Dialogs;

public partial class PasswordDialog : Window
{

	public PasswordDialog()
	{
		InitializeComponent();
	}

	private void BtnConfirm_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
	}}
