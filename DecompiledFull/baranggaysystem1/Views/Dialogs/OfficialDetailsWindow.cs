using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace baranggaysystem1.Views.Dialogs;

public class OfficialDetailsWindow : Window, IComponentConnector
{
	internal StackPanel contentPanel;

	internal Button btnConfirm;

	private bool _contentLoaded;

	public OfficialDetailsWindow()
	{
		InitializeComponent();
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
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/officialdetailswindow.xaml", UriKind.Relative);
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
			contentPanel = (StackPanel)target;
			break;
		case 2:
			btnConfirm = (Button)target;
			btnConfirm.Click += BtnConfirm_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
