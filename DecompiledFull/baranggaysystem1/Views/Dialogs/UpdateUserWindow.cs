using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class UpdateUserWindow : Window, IComponentConnector
{
	private bool _contentLoaded;

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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/updateuserwindow.xaml", UriKind.Relative);
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
