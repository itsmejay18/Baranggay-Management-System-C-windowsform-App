using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class ResidentDetailsWindow : Window, IComponentConnector
{
	internal StackPanel contentPanel;

	private bool _contentLoaded;

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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/residentdetailswindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			contentPanel = (StackPanel)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
