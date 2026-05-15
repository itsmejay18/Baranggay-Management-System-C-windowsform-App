using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;
using baranggaysystem1.Views.Dialogs;
using FontAwesome.Sharp;

namespace baranggaysystem1.Views.Pages;

public partial class RolePermissionsPage : UserControl, IRefreshable
{
	private readonly RolePermissionService _service = new RolePermissionService();

	private IReadOnlyList<RolePermissionSummary> _roles = Array.Empty<RolePermissionSummary>();

	private bool _isLoaded;













	private static bool CanManageRoles => string.Equals(UserSession.Role, "Super Admin", StringComparison.OrdinalIgnoreCase);

	private RolePermissionSummary? SelectedRole => mainGrid.SelectedItem as RolePermissionSummary;

	public RolePermissionsPage()
	{
		InitializeComponent();
		base.Loaded += async delegate
		{
			if (!_isLoaded)
			{
				_isLoaded = true;
				await LoadAsync();
			}
		};
	}

	public RolePermissionsPage(string route)
		: this()
	{
	}

	private async Task LoadAsync(int? selectRoleId = null)
	{
		if (!CanManageRoles)
		{
			btnAddRole.IsEnabled = false;
			btnRefresh.IsEnabled = false;
			mainGrid.ItemsSource = null;
			contextActionBar.Visibility = Visibility.Collapsed;
			emptyLabel.Text = "Only Super Admin accounts can manage roles and permissions.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Access restricted.";
			recordCountLabel.Text = "Access restricted.";
			return;
		}
		try
		{
			SetLoadingState(isLoading: true);
			_roles = await _service.GetRoleSummariesAsync();
			ApplyRoleFilter(selectRoleId);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("RolePermissionsPage load failed.", ex);
			mainGrid.ItemsSource = null;
			emptyLabel.Text = "Failed to load roles. Please refresh.";
			emptyState.Visibility = Visibility.Visible;
			footerCountLabel.Text = "Roles could not be loaded.";
		}
		finally
		{
			SetLoadingState(isLoading: false);
		}
	}

	private void SetLoadingState(bool isLoading)
	{
		btnAddRole.IsEnabled = !isLoading && CanManageRoles;
		btnRefresh.IsEnabled = !isLoading && CanManageRoles;
		footerCountLabel.Text = (isLoading ? "Loading roles..." : footerCountLabel.Text);
	}

	private void ApplyRoleFilter(int? selectRoleId = null)
	{
		string q = searchBox.Text.Trim();
		List<RolePermissionSummary> list = _roles.Where((RolePermissionSummary role) => MatchesSearch(role, q)).ToList();
		mainGrid.ItemsSource = list;
		emptyLabel.Text = (string.IsNullOrWhiteSpace(q) ? "No roles defined." : "No matching roles found.");
		emptyState.Visibility = ((list.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		recordCountLabel.Text = $"{_roles.Count:N0} role(s) configured for access control.";
		footerCountLabel.Text = $"Showing {list.Count:N0} of {_roles.Count:N0} role(s).";
		if (selectRoleId.HasValue)
		{
			RolePermissionSummary rolePermissionSummary = list.FirstOrDefault((RolePermissionSummary role) => role.RoleId == selectRoleId.Value);
			mainGrid.SelectedItem = rolePermissionSummary;
			if (rolePermissionSummary != null)
			{
				mainGrid.ScrollIntoView(rolePermissionSummary);
			}
		}
		UpdateSelectionState();
	}

	private static bool MatchesSearch(RolePermissionSummary role, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		if (!Contains(role.Name, query) && !Contains(role.Description, query))
		{
			return Contains(role.IsCoreRole ? "core" : "custom", query);
		}
		return true;
	}

	private static bool Contains(string? value, string query)
	{
		return (value ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyRoleFilter();
	}

	private void MainGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateSelectionState();
	}

	private void UpdateSelectionState()
	{
		RolePermissionSummary selectedRole = SelectedRole;
		if (selectedRole == null)
		{
			contextActionBar.Visibility = Visibility.Collapsed;
			return;
		}
		contextActionBar.Visibility = Visibility.Visible;
		selectedRecordLabel.Text = selectedRole.Name;
		selectedRoleMetaLabel.Text = (selectedRole.IsCoreRole ? $"Core role - {selectedRole.ActiveUserCount:N0} active user(s), {selectedRole.UserCount:N0} assigned total." : $"Custom role - {selectedRole.ActiveUserCount:N0} active user(s), {selectedRole.UserCount:N0} assigned total.");
		btnDeleteRole.IsEnabled = !selectedRole.IsCoreRole && selectedRole.UserCount == 0;
	}

	private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
	{
		mainGrid.SelectedItem = null;
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadAsync(SelectedRole?.RoleId);
	}

	private async void BtnAdd_Click(object sender, RoutedEventArgs e)
	{
		if (!CanManageRoles)
		{
			DialogService.Instance.ShowWarning("Only Super Admin accounts can create roles.", "Roles & Permissions");
			return;
		}
		RolePermissionWindow rolePermissionWindow = new RolePermissionWindow(_service.CreateNewRoleDraft());
		var adapter = new DialogContentAdapter(rolePermissionWindow);

		var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Role", IconChar.Save,
			(s, args) =>
			{
				NavigationService.Instance.NavigateBackFromFullscreen("RolePermissions", refreshOnReturn: true);
			});

		NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
		{
			Title = "Create New Role",
			Subtitle = "Define a new role with specific permissions",
			OriginRoute = "RolePermissions",
			Content = adapter,
			Icon = IconChar.UserShield,
			ToolbarItems = new List<UIElement> { saveButton },
			ShowSideToolbar = false,
			OnSaved = () => RefreshData()
		});
	}

	private async void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		await OpenSelectedRoleAsync();
	}

	private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		await OpenSelectedRoleAsync();
	}

	private async Task OpenSelectedRoleAsync()
	{
		RolePermissionSummary selectedRole = SelectedRole;
		if (selectedRole == null)
		{
			DialogService.Instance.ShowWarning("Please select a role to edit.", "Roles & Permissions");
			return;
		}
		try
		{
			RolePermissionEditorModel rolePermissionEditorModel = await _service.GetRoleEditorAsync(selectedRole.RoleId);
			if (rolePermissionEditorModel == null)
			{
				DialogService.Instance.ShowWarning("The selected role could not be found anymore.", "Roles & Permissions");
				await LoadAsync();
				return;
			}
			RolePermissionWindow rolePermissionWindow = new RolePermissionWindow(rolePermissionEditorModel);
			var adapter = new DialogContentAdapter(rolePermissionWindow);

			var saveButton = FullscreenToolbarHelper.CreateToolbarButton("Save Changes", IconChar.Save,
				(s, args) =>
				{
					NavigationService.Instance.NavigateBackFromFullscreen("RolePermissions", refreshOnReturn: true);
				});

			NavigationService.Instance.NavigateToFullscreen(new FullscreenViewConfig
			{
				Title = $"Edit Role - {selectedRole.Name}",
				Subtitle = "Modify role permissions and settings",
				OriginRoute = "RolePermissions",
				Content = adapter,
				Icon = IconChar.Edit,
				ToolbarItems = new List<UIElement> { saveButton },
				ShowSideToolbar = false,
				OnSaved = () => RefreshData()
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogError("RolePermissionsPage edit failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Roles & Permissions");
		}
	}

	private async void BtnDelete_Click(object sender, RoutedEventArgs e)
	{
		RolePermissionSummary selectedRole = SelectedRole;
		if (selectedRole == null)
		{
			DialogService.Instance.ShowWarning("Please select a role to delete.", "Roles & Permissions");
		}
		else if (selectedRole.IsCoreRole)
		{
			DialogService.Instance.ShowWarning("Core roles cannot be deleted.", "Roles & Permissions");
		}
		else if (selectedRole.UserCount > 0)
		{
			DialogService.Instance.ShowWarning("This role is still assigned to one or more staff accounts.", "Roles & Permissions");
		}
		else if (DialogService.Instance.Confirm("Delete the role '" + selectedRole.Name + "' and its permission grants?", "Delete Role"))
		{
			try
			{
				await _service.DeleteRoleAsync(selectedRole.RoleId);
				await LoadAsync();
			}
			catch (Exception ex)
			{
				AppLogger.LogError("RolePermissionsPage delete failed.", ex);
				DialogService.Instance.ShowError(ex.Message, "Roles & Permissions");
			}
		}
	}

	#region IRefreshable Implementation

	/// <summary>
	/// Refreshes the page data after returning from a fullscreen view.
	/// </summary>
	public void RefreshData()
	{
		_ = LoadAsync();
	}

	#endregion
}
