using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class ResidentDetailsViewModel : ObservableObject
{
	private readonly ResidentsModuleDataService _dataService;

	private readonly ResidentDto _originalRecord;

	[ObservableProperty]
	private string _title;

	[ObservableProperty]
	private string _firstName = string.Empty;

	[ObservableProperty]
	private string _middleName = string.Empty;

	[ObservableProperty]
	private string _lastName = string.Empty;

	[ObservableProperty]
	private string _suffix = string.Empty;

	[ObservableProperty]
	private string _gender = "MALE";

	[ObservableProperty]
	private DateTime _dateOfBirth = DateTime.Today;

	[ObservableProperty]
	private string _civilStatus = "SINGLE";

	[ObservableProperty]
	private string _contactNo = string.Empty;

	[ObservableProperty]
	private bool _isPwd;

	[ObservableProperty]
	private bool _isSenior;

	[ObservableProperty]
	private bool _is4PsBeneficiary;

	[ObservableProperty]
	private bool _isRegisteredVoter;

	[ObservableProperty]
	private bool _isSoloParent;

	[ObservableProperty]
	private bool _isYouth;

	[ObservableProperty]
	private bool _isIndigent;

	[ObservableProperty]
	private int? _purokId;

	[ObservableProperty]
	private LookupItem? _selectedPurok;

	public Action<bool>? CloseAction { get; set; }

	public ObservableCollection<LookupItem> PurokOptions { get; } = new ObservableCollection<LookupItem>();

	public ResidentDetailsViewModel(ResidentDto? existingResident = null)
	{
		_dataService = new ResidentsModuleDataService();
		_originalRecord = ((existingResident != null) ? CloneResident(existingResident) : new ResidentDto());
		if (existingResident != null && existingResident.Id.HasValue && existingResident.Id.Value > 0)
		{
			Title = "Edit Resident";
		}
		else
		{
			Title = "Add Resident";
		}
		if (existingResident != null)
		{
			FirstName = existingResident.FirstName;
			MiddleName = existingResident.MiddleName;
			LastName = existingResident.LastName;
			Suffix = existingResident.Suffix;
			Gender = NormalizeGenderSelection(existingResident.Gender);
			DateOfBirth = existingResident.DateOfBirth;
			CivilStatus = NormalizeCivilStatusSelection(existingResident.CivilStatus);
			ContactNo = existingResident.ContactNo;
			IsPwd = existingResident.IsPwd;
			IsSenior = existingResident.IsSenior;
			Is4PsBeneficiary = existingResident.Is4PsBeneficiary;
			IsRegisteredVoter = existingResident.IsRegisteredVoter;
			IsSoloParent = existingResident.IsSoloParent;
			IsYouth = existingResident.IsYouth;
			IsIndigent = existingResident.IsIndigent;
			PurokId = existingResident.PurokId;
		}
		LoadPurokOptionsAsync(_originalRecord.PurokId);
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		ValidationResult validationResult = ValidationService.ValidateResidentFormSave(FirstName, LastName, DateOfBirth);
		if (!validationResult.IsValid)
		{
			DialogService.Instance.ShowWarning(validationResult.Message, validationResult.Title);
			return;
		}
		if (!PurokId.HasValue || PurokId.Value <= 0)
		{
			DialogService.Instance.ShowWarning("Please select a purok / zone before saving.", "Missing data");
			return;
		}
		bool isEdit = _originalRecord.Id.HasValue && _originalRecord.Id.Value > 0;
		ResidentDto dto = new ResidentDto
		{
			Id = _originalRecord.Id,
			FirstName = FirstName.Trim(),
			MiddleName = MiddleName.Trim(),
			LastName = LastName.Trim(),
			Suffix = Suffix.Trim(),
			Gender = Gender,
			DateOfBirth = DateOfBirth.Date,
			CivilStatus = CivilStatus,
			ContactNo = ContactNo.Trim(),
			IsPwd = IsPwd,
			IsSenior = IsSenior,
			Is4PsBeneficiary = Is4PsBeneficiary,
			IsRegisteredVoter = IsRegisteredVoter,
			IsSoloParent = IsSoloParent,
			IsYouth = IsYouth,
			IsIndigent = IsIndigent,
			BarangayId = _dataService.BarangayId,
			PurokId = PurokId,
			HouseholdId = _originalRecord.HouseholdId,
			PhotoBytes = _originalRecord.PhotoBytes,
			Status = (string.IsNullOrWhiteSpace(_originalRecord.Status) ? "ACTIVE" : _originalRecord.Status)
		};
		try
		{
			await _dataService.SaveResidentAsync(dto);
			string value = (isEdit ? "updated" : "added");
			DialogService.Instance.ShowInfo($"Resident {dto.FirstName} {dto.LastName} was {value} successfully.");
			CloseAction?.Invoke(obj: true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Resident save failed.", ex);
			DialogService.Instance.ShowError(ex.Message, Title);
		}
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke(obj: false);
	}

	private async Task LoadPurokOptionsAsync(int? selectedPurokId)
	{
		try
		{
			IReadOnlyList<LookupItem> source = await _dataService.GetPurokOptionsAsync();
			PurokOptions.Clear();
			foreach (LookupItem item in source.OrderBy<LookupItem, string>((LookupItem option) => option.Name, StringComparer.OrdinalIgnoreCase))
			{
				PurokOptions.Add(item);
			}
			SelectedPurok = PurokOptions.FirstOrDefault((LookupItem option) => option.Id == selectedPurokId);
			if (SelectedPurok == null && selectedPurokId.HasValue && selectedPurokId.Value > 0)
			{
				LookupItem lookupItem = new LookupItem(selectedPurokId.Value, $"Purok #{selectedPurokId.Value}");
				PurokOptions.Add(lookupItem);
				SelectedPurok = lookupItem;
			}
			if (SelectedPurok == null && PurokOptions.Count == 1)
			{
				SelectedPurok = PurokOptions[0];
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to load purok options for resident details.", ex);
		}
	}

	private static ResidentDto CloneResident(ResidentDto resident)
	{
		return new ResidentDto
		{
			Id = resident.Id,
			FirstName = resident.FirstName,
			MiddleName = resident.MiddleName,
			LastName = resident.LastName,
			Suffix = resident.Suffix,
			Gender = resident.Gender,
			DateOfBirth = resident.DateOfBirth,
			CivilStatus = resident.CivilStatus,
			ContactNo = resident.ContactNo,
			IsPwd = resident.IsPwd,
			IsSenior = resident.IsSenior,
			Is4PsBeneficiary = resident.Is4PsBeneficiary,
			IsRegisteredVoter = resident.IsRegisteredVoter,
			IsSoloParent = resident.IsSoloParent,
			IsYouth = resident.IsYouth,
			IsIndigent = resident.IsIndigent,
			Status = resident.Status,
			PhotoBytes = resident.PhotoBytes,
			BarangayId = resident.BarangayId,
			PurokId = resident.PurokId,
			HouseholdId = resident.HouseholdId
		};
	}

	private static string NormalizeGenderSelection(string? value)
	{
		return (value ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"M" => "MALE", 
			"MALE" => "MALE", 
			"F" => "FEMALE", 
			"FEMALE" => "FEMALE", 
			_ => "MALE", 
		};
	}

	private static string NormalizeCivilStatusSelection(string? value)
	{
		return (value ?? string.Empty).Trim().ToUpperInvariant() switch
		{
			"MARRIED" => "MARRIED", 
			"WIDOWED" => "WIDOWED", 
			"SEPARATED" => "SEPARATED", 
			_ => "SINGLE", 
		};
	}
}
