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

internal class HouseholdTransferViewModel : ObservableObject
{
	private readonly HouseholdRepository _repository = new HouseholdRepository();

	private readonly ResidentHouseholdService _residentHouseholdService = new ResidentHouseholdService();

	private readonly int _barangayId;

	private readonly int _sourceHouseholdId;

	[ObservableProperty]
	private string _sourceHouseholdLabel = string.Empty;

	[ObservableProperty]
	private string _sourcePurokLabel = string.Empty;

	[ObservableProperty]
	private int _memberCount;

	[ObservableProperty]
	private string _membersPreview = string.Empty;

	[ObservableProperty]
	private HouseholdTransferTargetOption? _selectedDestination;

	[ObservableProperty]
	private string _transferReason = string.Empty;

	[ObservableProperty]
	private bool _isBusy;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? transferCommand;

	public Action<bool>? CloseAction { get; set; }

	public ObservableCollection<HouseholdTransferTargetOption> DestinationHouseholds { get; } = new ObservableCollection<HouseholdTransferTargetOption>();

	public bool CanTransfer
	{
		get
		{
			if (!IsBusy)
			{
				return SelectedDestination != null;
			}
			return false;
		}
	}

	public string DestinationHint
	{
		get
		{
			if (DestinationHouseholds.Count <= 0)
			{
				return "No empty destination households are available yet.";
			}
			return "Only empty households are listed as transfer destinations.";
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SourceHouseholdLabel
	{
		get
		{
			return _sourceHouseholdLabel;
		}
		[MemberNotNull("_sourceHouseholdLabel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_sourceHouseholdLabel, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SourceHouseholdLabel);
				_sourceHouseholdLabel = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SourceHouseholdLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SourcePurokLabel
	{
		get
		{
			return _sourcePurokLabel;
		}
		[MemberNotNull("_sourcePurokLabel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_sourcePurokLabel, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SourcePurokLabel);
				_sourcePurokLabel = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SourcePurokLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int MemberCount
	{
		get
		{
			return _memberCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_memberCount, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MemberCount);
				_memberCount = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MemberCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string MembersPreview
	{
		get
		{
			return _membersPreview;
		}
		[MemberNotNull("_membersPreview")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_membersPreview, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MembersPreview);
				_membersPreview = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MembersPreview);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public HouseholdTransferTargetOption? SelectedDestination
	{
		get
		{
			return _selectedDestination;
		}
		set
		{
			if (!EqualityComparer<HouseholdTransferTargetOption>.Default.Equals(_selectedDestination, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedDestination);
				_selectedDestination = value;
				OnSelectedDestinationChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedDestination);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string TransferReason
	{
		get
		{
			return _transferReason;
		}
		[MemberNotNull("_transferReason")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_transferReason, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TransferReason);
				_transferReason = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TransferReason);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isBusy, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsBusy);
				_isBusy = value;
				OnIsBusyChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsBusy);
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
	public IAsyncRelayCommand TransferCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = transferCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)TransferAsync);
				AsyncRelayCommand val2 = val;
				transferCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
		}
	}

	public HouseholdTransferViewModel(int sourceHouseholdId)
	{
		_sourceHouseholdId = sourceHouseholdId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		LoadSourceDetails();
		LoadDestinationHouseholds();
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke(obj: false);
	}

	[RelayCommand]
	private async Task TransferAsync()
	{
		if (!Permissions.CanTransferHouseholds)
		{
			DialogService.Instance.ShowWarning("You do not have permission to transfer household members.");
		}
		else if (SelectedDestination == null)
		{
			DialogService.Instance.ShowWarning("Please select an empty destination household.");
		}
		else
		{
			if (!DialogService.Instance.Confirm($"Transfer {MemberCount} household member(s) from:\n{SourceHouseholdLabel}\n\nto:\n{SelectedDestination.AddressLabel}\n\nThe source household will remain in the registry but will become empty."))
			{
				return;
			}
			try
			{
				IsBusy = true;
				int value = await Task.Run(() => _residentHouseholdService.TransferEntireHousehold(_sourceHouseholdId, SelectedDestination.HouseholdId, _barangayId, TransferReason));
				DialogService.Instance.ShowInfo($"{value} household member(s) transferred successfully.");
				CloseAction?.Invoke(obj: true);
			}
			catch (Exception ex)
			{
				AppLogger.LogError("Household family transfer failed.", ex);
				DialogService.Instance.ShowError(ex.Message, "Transfer Family");
			}
			finally
			{
				IsBusy = false;
			}
		}
	}

	private void LoadSourceDetails()
	{
		HouseholdDetailsDto details = _repository.GetDetails(_sourceHouseholdId, _barangayId);
		if (details == null)
		{
			throw new InvalidOperationException("The selected household could not be loaded.");
		}
		SourceHouseholdLabel = (string.IsNullOrWhiteSpace(details.FullAddress) ? $"Household #{details.HouseholdId}" : details.FullAddress);
		SourcePurokLabel = (string.IsNullOrWhiteSpace(details.PurokName) ? "Purok not set" : details.PurokName);
		IReadOnlyList<HouseholdMemberRecord> members = _repository.GetMembers(_sourceHouseholdId, _barangayId);
		MemberCount = members.Count;
		if (MemberCount <= 0)
		{
			MembersPreview = "No household members are currently assigned.";
			return;
		}
		string[] array = (from member in members.Take(5)
			select member.FullName into name
			where !string.IsNullOrWhiteSpace(name)
			select name).ToArray();
		MembersPreview = string.Join(", ", array);
		if (members.Count > array.Length)
		{
			MembersPreview += $" + {members.Count - array.Length} more";
		}
	}

	private void LoadDestinationHouseholds()
	{
		DestinationHouseholds.Clear();
		foreach (HouseholdListItem item in _repository.Search(new HouseholdListFilters
		{
			BarangayId = _barangayId,
			EmptyHouseholdOnly = true,
			PageNumber = 1,
			PageSize = 200
		}).Items.Where((HouseholdListItem item) => item.HouseholdId != _sourceHouseholdId))
		{
			string text = HouseholdRepository.BuildAddressLabel(item.HouseNo, item.Street, item.Subdivision, item.PurokName);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = $"Household #{item.HouseholdId}";
			}
			DestinationHouseholds.Add(new HouseholdTransferTargetOption
			{
				HouseholdId = item.HouseholdId,
				AddressLabel = text,
				PurokName = item.PurokName
			});
		}
		SelectedDestination = DestinationHouseholds.FirstOrDefault();
		((ObservableObject)this).OnPropertyChanged("DestinationHint");
		((ObservableObject)this).OnPropertyChanged("CanTransfer");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedDestinationChanged(HouseholdTransferTargetOption? value)
	{
		((ObservableObject)this).OnPropertyChanged("CanTransfer");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsBusyChanged(bool value)
	{
		((ObservableObject)this).OnPropertyChanged("CanTransfer");
	}
}
