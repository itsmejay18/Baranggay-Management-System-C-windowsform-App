using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public class ResidentDetailsViewModel : ObservableObject
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelCommand;

	public ObservableCollection<LookupItem> PurokOptions { get; } = new ObservableCollection<LookupItem>();

	public List<string> GenderOptions { get; } = new List<string> { "MALE", "FEMALE" };

	public List<string> CivilStatusOptions { get; } = new List<string> { "SINGLE", "MARRIED", "WIDOWED", "SEPARATED" };

	public Action<bool>? CloseAction { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Title
	{
		get
		{
			return _title;
		}
		[MemberNotNull("_title")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_title, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Title);
				_title = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Title);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string FirstName
	{
		get
		{
			return _firstName;
		}
		[MemberNotNull("_firstName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_firstName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FirstName);
				_firstName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FirstName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string MiddleName
	{
		get
		{
			return _middleName;
		}
		[MemberNotNull("_middleName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_middleName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MiddleName);
				_middleName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MiddleName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastName
	{
		get
		{
			return _lastName;
		}
		[MemberNotNull("_lastName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastName);
				_lastName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Suffix
	{
		get
		{
			return _suffix;
		}
		[MemberNotNull("_suffix")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_suffix, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Suffix);
				_suffix = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Suffix);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Gender
	{
		get
		{
			return _gender;
		}
		[MemberNotNull("_gender")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_gender, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Gender);
				_gender = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Gender);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public DateTime DateOfBirth
	{
		get
		{
			return _dateOfBirth;
		}
		set
		{
			if (!EqualityComparer<DateTime>.Default.Equals(_dateOfBirth, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DateOfBirth);
				_dateOfBirth = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DateOfBirth);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CivilStatus
	{
		get
		{
			return _civilStatus;
		}
		[MemberNotNull("_civilStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_civilStatus, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CivilStatus);
				_civilStatus = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CivilStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ContactNo
	{
		get
		{
			return _contactNo;
		}
		[MemberNotNull("_contactNo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_contactNo, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ContactNo);
				_contactNo = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ContactNo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsPwd
	{
		get
		{
			return _isPwd;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isPwd, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsPwd);
				_isPwd = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsPwd);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSenior
	{
		get
		{
			return _isSenior;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSenior, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSenior);
				_isSenior = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSenior);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Is4PsBeneficiary
	{
		get
		{
			return _is4PsBeneficiary;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_is4PsBeneficiary, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Is4PsBeneficiary);
				_is4PsBeneficiary = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Is4PsBeneficiary);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsRegisteredVoter
	{
		get
		{
			return _isRegisteredVoter;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isRegisteredVoter, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRegisteredVoter);
				_isRegisteredVoter = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRegisteredVoter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSoloParent
	{
		get
		{
			return _isSoloParent;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSoloParent, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSoloParent);
				_isSoloParent = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSoloParent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsYouth
	{
		get
		{
			return _isYouth;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isYouth, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsYouth);
				_isYouth = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsYouth);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsIndigent
	{
		get
		{
			return _isIndigent;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isIndigent, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsIndigent);
				_isIndigent = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsIndigent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int? PurokId
	{
		get
		{
			return _purokId;
		}
		set
		{
			if (!EqualityComparer<int?>.Default.Equals(_purokId, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PurokId);
				_purokId = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PurokId);
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
				OnSelectedPurokChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedPurok);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = saveCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)SaveAsync);
				AsyncRelayCommand val2 = val;
				saveCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedPurokChanged(LookupItem? value)
	{
		if (value != null && PurokId != value.Id)
		{
			PurokId = value.Id;
		}
	}
}
