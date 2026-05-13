using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

internal partial class HouseholdEditorViewModel : ObservableObject
{
	private readonly HouseholdRepository _repository = new HouseholdRepository();

	private readonly int _barangayId;

	private readonly int? _householdId;

	[ObservableProperty]
	private string _windowTitle = "New Household";

	[ObservableProperty]
	private string _subtitle = "Create the household address first, then add family members.";

	[ObservableProperty]
	private string _saveButtonText = "Create Household";

	[ObservableProperty]
	private string _householdLabel = "New household";

	[ObservableProperty]
	private LookupItem? _selectedPurok;

	[ObservableProperty]
	private string _houseNo = string.Empty;

	[ObservableProperty]
	private string _street = string.Empty;

	[ObservableProperty]
	private string _subdivision = string.Empty;

	[ObservableProperty]
	private string _addressNote = string.Empty;

	[ObservableProperty]
	private string _latitudeText = string.Empty;

	[ObservableProperty]
	private string _longitudeText = string.Empty;

	[ObservableProperty]
	private bool _isSaving;

	public Action<bool>? CloseAction { get; set; }

	public ObservableCollection<LookupItem> PurokOptions { get; } = new ObservableCollection<LookupItem>();

	public bool IsEditMode
	{
		get
		{
			if (_householdId.HasValue)
			{
				return _householdId.Value > 0;
			}
			return false;
		}
	}

	public int SavedHouseholdId { get; private set; }

	public HouseholdEditorViewModel(int? householdId = null)
	{
		_householdId = householdId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		LoadPurokOptions();
		LoadExistingRecord();
	}

	private void LoadPurokOptions()
	{
		PurokOptions.Clear();
		foreach (LookupItem purokOption in _repository.GetPurokOptions(_barangayId))
		{
			PurokOptions.Add(purokOption);
		}
		if (PurokOptions.Count > 0)
		{
			SelectedPurok = PurokOptions[0];
		}
	}

	private void LoadExistingRecord()
	{
		if (IsEditMode)
		{
			HouseholdEditRecord existing = _repository.GetForEdit(_householdId.Value, _barangayId);
			if (existing == null)
			{
				throw new InvalidOperationException("The selected household could not be loaded.");
			}
			WindowTitle = "Edit Household";
			Subtitle = "Update the household address and location details.";
			SaveButtonText = "Save Changes";
			HouseholdLabel = $"Household #{existing.HouseholdId}";
			SelectedPurok = PurokOptions.FirstOrDefault((LookupItem option) => option.Id == existing.PurokId) ?? SelectedPurok;
			HouseNo = existing.HouseNo;
			Street = existing.Street;
			Subdivision = existing.Subdivision;
			AddressNote = existing.AddressNote;
			LatitudeText = existing.Latitude?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
			LongitudeText = existing.Longitude?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
			SavedHouseholdId = existing.HouseholdId;
		}
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke(obj: false);
	}

	[RelayCommand]
	private void Save()
	{
		if (IsSaving)
		{
			return;
		}
		if (IsEditMode && !Permissions.CanEditHouseholds)
		{
			DialogService.Instance.ShowWarning("You do not have permission to edit household records.");
			return;
		}
		if (!IsEditMode && !Permissions.CanCreateHouseholds)
		{
			DialogService.Instance.ShowWarning("You do not have permission to create household records.");
			return;
		}
		HouseholdSaveRequest householdSaveRequest = BuildValidatedRequest();
		if (householdSaveRequest == null)
		{
			return;
		}
		try
		{
			IsSaving = true;
			if (IsEditMode)
			{
				_repository.Update(_householdId.Value, householdSaveRequest);
				SavedHouseholdId = _householdId.Value;
				DialogService.Instance.ShowInfo("Household details updated successfully.");
			}
			else
			{
				SavedHouseholdId = _repository.Create(householdSaveRequest);
				HouseholdLabel = $"Household #{SavedHouseholdId}";
				DialogService.Instance.ShowInfo("Household created. You can now add family members.");
			}
			CloseAction?.Invoke(obj: true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Household save failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Save Household");
		}
		finally
		{
			IsSaving = false;
		}
	}

	private HouseholdSaveRequest? BuildValidatedRequest()
	{
		if (SelectedPurok == null)
		{
			DialogService.Instance.ShowWarning("Please select a purok for this household.");
			return null;
		}
		string text = HouseNo.Trim();
		string text2 = Street.Trim();
		string subdivision = Subdivision.Trim();
		string addressNote = AddressNote.Trim();
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			DialogService.Instance.ShowWarning("Enter at least a house number or street so the household can be identified.");
			return null;
		}
		if (_repository.ExistsDuplicateAddress(_barangayId, SelectedPurok.Id, text, text2, IsEditMode ? _householdId : ((int?)null)))
		{
			DialogService.Instance.ShowWarning("A household with the same house number and street already exists in this purok.");
			return null;
		}
		if (!TryParseCoordinate(LatitudeText, "Latitude", out var parsedValue))
		{
			return null;
		}
		if (!TryParseCoordinate(LongitudeText, "Longitude", out var parsedValue2))
		{
			return null;
		}
		return new HouseholdSaveRequest
		{
			BarangayId = _barangayId,
			PurokId = SelectedPurok.Id,
			HouseNo = text,
			Street = text2,
			Subdivision = subdivision,
			AddressNote = addressNote,
			Latitude = parsedValue,
			Longitude = parsedValue2
		};
	}

	private static bool TryParseCoordinate(string rawValue, string label, out decimal? parsedValue)
	{
		string text = rawValue?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			parsedValue = null;
			return true;
		}
		if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
		{
			parsedValue = result;
			return true;
		}
		DialogService.Instance.ShowWarning(label + " must be a valid decimal value.");
		parsedValue = null;
		return false;
	}
}
