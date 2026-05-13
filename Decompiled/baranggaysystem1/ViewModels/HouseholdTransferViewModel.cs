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

internal partial class HouseholdTransferViewModel : ObservableObject
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

	public Action<bool>? CloseAction { get; set; }

	public ObservableCollection<HouseholdTransferTargetOption> DestinationHouseholds { get; } = new ObservableCollection<HouseholdTransferTargetOption>();

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
		OnPropertyChanged("DestinationHint");
		OnPropertyChanged("CanTransfer");
	}

}
