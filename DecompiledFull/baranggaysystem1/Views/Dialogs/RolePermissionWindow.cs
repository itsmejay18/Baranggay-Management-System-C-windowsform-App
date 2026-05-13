using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class RolePermissionWindow : Window, IComponentConnector
{
	private readonly RolePermissionService _service = new RolePermissionService();

	private readonly RolePermissionEditorModel _editor;

	private bool _isSaving;

	internal TextBlock eyebrowText;

	internal TextBlock headerTitleText;

	internal TextBlock headerSubtitleText;

	internal TextBox txtRoleName;

	internal TextBox txtDescription;

	internal TextBlock roleMetaText;

	internal Border coreRoleNotice;

	internal TextBlock coreRoleNoticeText;

	internal Button btnAllowAll;

	internal Button btnClearAll;

	internal StackPanel permissionsPanel;

	internal Button btnConfirm;

	private bool _contentLoaded;

	public int SavedRoleId { get; private set; }

	private bool IsNewRole => _editor.RoleId <= 0;

	internal RolePermissionWindow(RolePermissionEditorModel editor)
	{
		InitializeComponent();
		_editor = editor ?? throw new ArgumentNullException("editor");
		SavedRoleId = _editor.RoleId;
		PopulateForm();
		BuildPermissionGroups();
	}

	private void PopulateForm()
	{
		base.Title = (IsNewRole ? "Create Role" : "Edit Role Permissions");
		eyebrowText.Text = (IsNewRole ? "NEW ROLE" : "ACCESS CONTROL");
		headerTitleText.Text = (IsNewRole ? "Create a staff access role" : ("Configure " + _editor.Name));
		headerSubtitleText.Text = (IsNewRole ? "Name the role, describe its purpose, and choose the permissions staff should receive." : "Choose what staff assigned to this role can open, create, update, or release.");
		btnConfirm.Content = (IsNewRole ? "Create Role" : "Save Changes");
		txtRoleName.Text = _editor.Name;
		txtDescription.Text = _editor.Description;
		txtRoleName.IsEnabled = IsNewRole;
		roleMetaText.Text = (IsNewRole ? "New roles can be assigned to staff accounts after saving." : $"{_editor.ActiveUserCount:N0} active staff account(s), {_editor.UserCount:N0} assigned total.");
		coreRoleNotice.Visibility = ((!_editor.IsCoreRole) ? Visibility.Collapsed : Visibility.Visible);
		if (_editor.IsSuperAdmin)
		{
			coreRoleNoticeText.Text = "Super Admin always has full access. Permission boxes are shown for review and saved as allowed.";
			btnAllowAll.IsEnabled = false;
			btnClearAll.IsEnabled = false;
		}
	}

	private void BuildPermissionGroups()
	{
		permissionsPanel.Children.Clear();
		foreach (IGrouping<string, RolePermissionGrantItem> item in from permission in _editor.Permissions
			orderby permission.GroupOrder, permission.ItemOrder
			group permission by permission.GroupName)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.Children.Add(new TextBlock
			{
				Text = item.Key,
				FontWeight = FontWeights.Bold,
				Foreground = BrushFrom("#0F172A"),
				FontSize = 13.0,
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
			});
			foreach (RolePermissionGrantItem item2 in item)
			{
				if (_editor.IsSuperAdmin)
				{
					item2.IsAllowed = true;
				}
				StackPanel stackPanel2 = new StackPanel
				{
					Margin = new Thickness(8.0, 0.0, 0.0, 0.0)
				};
				stackPanel2.Children.Add(new TextBlock
				{
					Text = item2.Label,
					FontWeight = FontWeights.SemiBold,
					Foreground = BrushFrom("#1E293B"),
					TextWrapping = TextWrapping.Wrap
				});
				stackPanel2.Children.Add(new TextBlock
				{
					Text = item2.Description,
					Foreground = BrushFrom("#64748B"),
					FontSize = 11.0,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
				});
				CheckBox checkBox = new CheckBox
				{
					Tag = item2,
					IsChecked = item2.IsAllowed,
					IsEnabled = !_editor.IsSuperAdmin,
					Content = stackPanel2,
					Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
					VerticalContentAlignment = VerticalAlignment.Top
				};
				checkBox.Checked += PermissionCheckChanged;
				checkBox.Unchecked += PermissionCheckChanged;
				stackPanel.Children.Add(checkBox);
			}
			Border element = new Border
			{
				Background = Brushes.White,
				BorderBrush = BrushFrom("#E2E8F0"),
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(8.0),
				Padding = new Thickness(16.0, 14.0, 16.0, 6.0),
				Margin = new Thickness(0.0, 0.0, 0.0, 12.0),
				Child = stackPanel
			};
			permissionsPanel.Children.Add(element);
		}
	}

	private static Brush BrushFrom(string hex)
	{
		return (Brush)new BrushConverter().ConvertFromString(hex);
	}

	private static void PermissionCheckChanged(object sender, RoutedEventArgs e)
	{
		if (sender is CheckBox { Tag: RolePermissionGrantItem tag } checkBox)
		{
			tag.IsAllowed = checkBox.IsChecked == true;
		}
	}

	private void BtnAllowAll_Click(object sender, RoutedEventArgs e)
	{
		foreach (RolePermissionGrantItem permission in _editor.Permissions)
		{
			permission.IsAllowed = true;
		}
		BuildPermissionGroups();
	}

	private void BtnClearAll_Click(object sender, RoutedEventArgs e)
	{
		foreach (RolePermissionGrantItem permission in _editor.Permissions)
		{
			permission.IsAllowed = false;
		}
		BuildPermissionGroups();
	}

	private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
	{
		await SaveAsync();
	}

	private async Task SaveAsync()
	{
		if (_isSaving)
		{
			return;
		}
		_editor.Name = txtRoleName.Text;
		_editor.Description = txtDescription.Text;
		try
		{
			_isSaving = true;
			btnConfirm.IsEnabled = false;
			btnConfirm.Content = "Saving...";
			SavedRoleId = await _service.SaveRoleAsync(_editor);
			base.DialogResult = true;
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError(ex.Message, "Roles & Permissions");
		}
		finally
		{
			_isSaving = false;
			btnConfirm.IsEnabled = true;
			btnConfirm.Content = (IsNewRole ? "Create Role" : "Save Changes");
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/rolepermissionwindow.xaml", UriKind.Relative);
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
			eyebrowText = (TextBlock)target;
			break;
		case 2:
			headerTitleText = (TextBlock)target;
			break;
		case 3:
			headerSubtitleText = (TextBlock)target;
			break;
		case 4:
			txtRoleName = (TextBox)target;
			break;
		case 5:
			txtDescription = (TextBox)target;
			break;
		case 6:
			roleMetaText = (TextBlock)target;
			break;
		case 7:
			coreRoleNotice = (Border)target;
			break;
		case 8:
			coreRoleNoticeText = (TextBlock)target;
			break;
		case 9:
			btnAllowAll = (Button)target;
			btnAllowAll.Click += BtnAllowAll_Click;
			break;
		case 10:
			btnClearAll = (Button)target;
			btnClearAll.Click += BtnClearAll_Click;
			break;
		case 11:
			permissionsPanel = (StackPanel)target;
			break;
		case 12:
			btnConfirm = (Button)target;
			btnConfirm.Click += BtnConfirm_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
