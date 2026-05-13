using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class HouseholdCertificateWindow : Window, IComponentConnector
{
	private bool _contentLoaded;

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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/householdcertificatewindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
