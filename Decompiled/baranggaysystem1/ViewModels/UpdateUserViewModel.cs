using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Services;

namespace baranggaysystem1.ViewModels;

public partial class UpdateUserViewModel : ObservableObject
{
	private readonly StaffService _dataService;

	[ObservableProperty]
	private int _targetUserId;

	[ObservableProperty]
	private string _targetUsername = string.Empty;

	[ObservableProperty]
	private string _newPassword = string.Empty;

	[ObservableProperty]
	private string _confirmPassword = string.Empty;

	[ObservableProperty]
	private bool _requirePasswordChange = true;

	public Action? CloseAction { get; set; }

	public UpdateUserViewModel(int targetUserId, string targetUsername)
	{
		_dataService = new StaffService();
		TargetUserId = targetUserId;
		TargetUsername = targetUsername;
	}

	[RelayCommand]
	private async Task UpdatePasswordAsync()
	{
		if (string.IsNullOrWhiteSpace(NewPassword))
		{
			DialogService.Instance.ShowWarning("New password cannot be empty.");
			return;
		}
		if (NewPassword != ConfirmPassword)
		{
			DialogService.Instance.ShowWarning("Passwords do not match.");
			return;
		}
		if (NewPassword.Length < 6)
		{
			DialogService.Instance.ShowWarning("Password must be at least 6 characters long.");
			return;
		}
		DialogService.Instance.ShowInfo("Successfully updated password for user '" + TargetUsername + "'.");
		CloseAction?.Invoke();
		await Task.CompletedTask;
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke();
	}
}
