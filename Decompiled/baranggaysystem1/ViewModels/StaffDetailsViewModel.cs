using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;

namespace baranggaysystem1.ViewModels;

public partial class StaffDetailsViewModel : ObservableObject
{
	private readonly StaffProfileDetails? _originalRecord;

	private readonly StaffService _dataService;

	[ObservableProperty]
	private string _title;

	[ObservableProperty]
	private string _username = string.Empty;

	[ObservableProperty]
	private string _firstName = string.Empty;

	[ObservableProperty]
	private string _middleName = string.Empty;

	[ObservableProperty]
	private string _lastName = string.Empty;

	[ObservableProperty]
	private string _email = string.Empty;

	[ObservableProperty]
	private string _contactNumber = string.Empty;

	[ObservableProperty]
	private string _position = string.Empty;

	[ObservableProperty]
	private string _department = string.Empty;

	[ObservableProperty]
	private string _roleName = "Standard";

	[ObservableProperty]
	private bool _isActive = true;

	[ObservableProperty]
	private bool _isNewUser;

	[ObservableProperty]
	private string _temporaryPassword = string.Empty;

	public Action? CloseAction { get; set; }

	public StaffDetailsViewModel(StaffProfileDetails? existingRecord = null)
	{
		_dataService = new StaffService();
		if (existingRecord != null)
		{
			Title = "Edit Staff Profile";
			IsNewUser = false;
			_originalRecord = existingRecord;
			Username = existingRecord.Username;
			FirstName = existingRecord.FirstName;
			MiddleName = existingRecord.MiddleName;
			LastName = existingRecord.LastName;
			Email = existingRecord.Email;
			ContactNumber = existingRecord.ContactNumber;
			Position = existingRecord.Position;
			Department = existingRecord.Department;
			RoleName = existingRecord.RoleName;
			IsActive = existingRecord.IsActive;
		}
		else
		{
			Title = "Register New Staff";
			IsNewUser = true;
			_originalRecord = null;
			TemporaryPassword = GenerateTemporaryPassword();
		}
	}

	private string GenerateTemporaryPassword()
	{
		return "Setup_" + new Random().Next(1000, 9999);
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
		{
			DialogService.Instance.ShowWarning("Username, First Name, and Last Name are required.");
			return;
		}
		StaffProfileDetails staffProfileDetails = new StaffProfileDetails
		{
			UserId = (_originalRecord?.UserId ?? 0),
			Username = Username.Trim(),
			FirstName = FirstName.Trim(),
			MiddleName = MiddleName.Trim(),
			LastName = LastName.Trim(),
			Email = Email.Trim(),
			ContactNumber = ContactNumber.Trim(),
			Position = Position.Trim(),
			Department = Department.Trim(),
			RoleName = RoleName,
			IsActive = IsActive
		};
		DialogService.Instance.ShowInfo("Successfully saved staff profile for " + staffProfileDetails.Username + ".");
		CloseAction?.Invoke();
		await Task.CompletedTask;
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke();
	}
}
