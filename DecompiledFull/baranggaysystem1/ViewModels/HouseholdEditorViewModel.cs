using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

internal class HouseholdEditorViewModel : ObservableObject
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? saveCommand;

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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string WindowTitle
	{
		get
		{
			return _windowTitle;
		}
		[MemberNotNull("_windowTitle")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_windowTitle, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WindowTitle);
				_windowTitle = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WindowTitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Subtitle
	{
		get
		{
			return _subtitle;
		}
		[MemberNotNull("_subtitle")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_subtitle, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Subtitle);
				_subtitle = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Subtitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SaveButtonText
	{
		get
		{
			return _saveButtonText;
		}
		[MemberNotNull("_saveButtonText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_saveButtonText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SaveButtonText);
				_saveButtonText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SaveButtonText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HouseholdLabel
	{
		get
		{
			return _householdLabel;
		}
		[MemberNotNull("_householdLabel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_householdLabel, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HouseholdLabel);
				_householdLabel = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HouseholdLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public LookupItem? SelectedPurok
	{
		get
		{
			return _selectedPurok;
		}
		set
		{
			if (!EqualityComparer<LookupItem>.Default.Equals(_selectedPurok, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedPurok);
				_selectedPurok = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedPurok);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HouseNo
	{
		get
		{
			return _houseNo;
		}
		[MemberNotNull("_houseNo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_houseNo, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HouseNo);
				_houseNo = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HouseNo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Street
	{
		get
		{
			return _street;
		}
		[MemberNotNull("_street")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_street, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Street);
				_street = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Street);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Subdivision
	{
		get
		{
			return _subdivision;
		}
		[MemberNotNull("_subdivision")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_subdivision, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Subdivision);
				_subdivision = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Subdivision);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string AddressNote
	{
		get
		{
			return _addressNote;
		}
		[MemberNotNull("_addressNote")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_addressNote, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AddressNote);
				_addressNote = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AddressNote);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LatitudeText
	{
		get
		{
			return _latitudeText;
		}
		[MemberNotNull("_latitudeText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_latitudeText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LatitudeText);
				_latitudeText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LatitudeText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LongitudeText
	{
		get
		{
			return _longitudeText;
		}
		[MemberNotNull("_longitudeText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_longitudeText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LongitudeText);
				_longitudeText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LongitudeText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSaving
	{
		get
		{
			return _isSaving;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSaving, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSaving);
				_isSaving = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSaving);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CancelCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			RelayCommand obj = cancelCommand;
			if (obj == null)
			{
				RelayCommand val = new RelayCommand((Action)Cancel);
				RelayCommand val2 = val;
				cancelCommand = val;
				obj = val2;
			}
			return (IRelayCommand)(object)obj;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SaveCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			RelayCommand obj = saveCommand;
			if (obj == null)
			{
				RelayCommand val = new RelayCommand((Action)Save);
				RelayCommand val2 = val;
				saveCommand = val;
				obj = val2;
			}
			return (IRelayCommand)(object)obj;
		}
	}

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
