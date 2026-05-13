using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

internal class HouseholdCertificateViewModel : ObservableObject
{
	private readonly HouseholdRepository _repository = new HouseholdRepository();

	private readonly int _barangayId;

	private readonly int _householdId;

	[ObservableProperty]
	private string _householdLabel = string.Empty;

	[ObservableProperty]
	private string _householdAddress = string.Empty;

	[ObservableProperty]
	private string _purokLabel = string.Empty;

	[ObservableProperty]
	private string _memberSummary = string.Empty;

	[ObservableProperty]
	private string _purpose = "For household verification and barangay record purposes.";

	[ObservableProperty]
	private string _presentedTo = "Any concerned party";

	[ObservableProperty]
	private bool _includeMemberRoster = true;

	[ObservableProperty]
	private DateTime _issuedDate = DateTime.Today;

	[ObservableProperty]
	private bool _isGenerating;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? generateCommand;

	public Action<bool>? CloseAction { get; set; }

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
	public string HouseholdAddress
	{
		get
		{
			return _householdAddress;
		}
		[MemberNotNull("_householdAddress")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_householdAddress, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HouseholdAddress);
				_householdAddress = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HouseholdAddress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string PurokLabel
	{
		get
		{
			return _purokLabel;
		}
		[MemberNotNull("_purokLabel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_purokLabel, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PurokLabel);
				_purokLabel = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PurokLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string MemberSummary
	{
		get
		{
			return _memberSummary;
		}
		[MemberNotNull("_memberSummary")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_memberSummary, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MemberSummary);
				_memberSummary = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MemberSummary);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Purpose
	{
		get
		{
			return _purpose;
		}
		[MemberNotNull("_purpose")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_purpose, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Purpose);
				_purpose = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Purpose);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string PresentedTo
	{
		get
		{
			return _presentedTo;
		}
		[MemberNotNull("_presentedTo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_presentedTo, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PresentedTo);
				_presentedTo = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PresentedTo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IncludeMemberRoster
	{
		get
		{
			return _includeMemberRoster;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_includeMemberRoster, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IncludeMemberRoster);
				_includeMemberRoster = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IncludeMemberRoster);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public DateTime IssuedDate
	{
		get
		{
			return _issuedDate;
		}
		set
		{
			if (!EqualityComparer<DateTime>.Default.Equals(_issuedDate, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IssuedDate);
				_issuedDate = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IssuedDate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsGenerating
	{
		get
		{
			return _isGenerating;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isGenerating, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsGenerating);
				_isGenerating = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsGenerating);
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
	public IAsyncRelayCommand GenerateCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = generateCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)GenerateAsync);
				AsyncRelayCommand val2 = val;
				generateCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
		}
	}

	public HouseholdCertificateViewModel(int householdId)
	{
		_householdId = householdId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		LoadHouseholdSummary();
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke(obj: false);
	}

	[RelayCommand]
	private async Task GenerateAsync()
	{
		if (!Permissions.CanIssueCertificates)
		{
			DialogService.Instance.ShowWarning("You do not have permission to generate household certificates.");
			return;
		}
		if (string.IsNullOrWhiteSpace(Purpose))
		{
			DialogService.Instance.ShowWarning("Please enter the purpose of the household certificate.");
			return;
		}
		if (IssuedDate.Date > DateTime.Today)
		{
			DialogService.Instance.ShowWarning("Issued date cannot be in the future.");
			return;
		}
		try
		{
			IsGenerating = true;
			string text = await Task.Run(() => HouseholdCertificateService.GeneratePdf(_householdId, new HouseholdCertificateRequest
			{
				Purpose = Purpose.Trim(),
				PresentedTo = PresentedTo.Trim(),
				IncludeMemberRoster = IncludeMemberRoster,
				IssuedDate = IssuedDate,
				GeneratedBy = (string.IsNullOrWhiteSpace(UserSession.Username) ? "Barangay Staff" : UserSession.Username)
			}));
			HouseholdCertificateService.TryOpenGeneratedFile(text);
			DialogService.Instance.ShowInfo("Household certificate generated successfully.\n\nSaved to:\n" + text);
			CloseAction?.Invoke(obj: true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Household certificate generation failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Household Certificate");
		}
		finally
		{
			IsGenerating = false;
		}
	}

	private void LoadHouseholdSummary()
	{
		HouseholdDetailsDto details = _repository.GetDetails(_householdId, _barangayId);
		if (details == null)
		{
			throw new InvalidOperationException("Selected household could not be loaded.");
		}
		HouseholdLabel = $"Household #{details.HouseholdId}";
		HouseholdAddress = (string.IsNullOrWhiteSpace(details.FullAddress) ? $"Household #{details.HouseholdId}" : details.FullAddress);
		PurokLabel = (string.IsNullOrWhiteSpace(details.PurokName) ? "Purok not set" : details.PurokName);
		MemberSummary = $"{details.MemberCount} member(s) | Seniors: {details.SeniorCount} | PWD: {details.PwdCount} | 4Ps: {details.FourPsCount}";
	}
}
