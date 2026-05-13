using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class HouseholdMemberPickerViewModel : ObservableObject
{
	private readonly int _householdId;

	private readonly int _barangayId;

	private readonly HouseholdRepository _householdRepository = new HouseholdRepository();

	private readonly ResidentHouseholdService _residentHouseholdService = new ResidentHouseholdService();

	[ObservableProperty]
	private string _searchText = string.Empty;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private bool _showEmptyState;

	[ObservableProperty]
	private string _emptyStateMessage = "Search residents to add into this household.";

	[ObservableProperty]
	private HouseholdResidentCandidate? _selectedResident;

	public ObservableCollection<HouseholdResidentCandidate> SearchResults { get; } = new ObservableCollection<HouseholdResidentCandidate>();

	public HouseholdMemberPickerViewModel(int householdId)
	{
		_householdId = householdId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
	}

	public async Task LoadResidentsAsync()
	{
		try
		{
			IsLoading = true;
			ShowEmptyState = false;
			SearchResults.Clear();
			foreach (ResidentPickerItem item in await Task.Run(() => _householdRepository.GetResidentsForHouseholdPicker(_barangayId, _householdId, SearchText)))
			{
				SearchResults.Add(new HouseholdResidentCandidate
				{
					ResidentId = item.ResidentId,
					FullName = item.FullName,
					ContactNo = item.ContactNo,
					CurrentAddress = item.CurrentAddress,
					CurrentHouseholdId = item.CurrentHouseholdId,
					CurrentPurokId = item.CurrentPurokId
				});
			}
			EmptyStateMessage = ((SearchResults.Count == 0) ? "No matching residents were found." : string.Empty);
			ShowEmptyState = SearchResults.Count == 0;
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to load residents for household picker", ex);
			EmptyStateMessage = "Unable to load residents right now.";
			ShowEmptyState = true;
		}
		finally
		{
			IsLoading = false;
		}
	}

	public async Task<bool> AttachSelectedAsync()
	{
		if (SelectedResident == null)
		{
			DialogService.Instance.ShowWarning("Select a resident to add first.");
			return false;
		}
		string message = (SelectedResident.CurrentHouseholdId.HasValue ? $"Move {SelectedResident.FullName} from Household #{SelectedResident.CurrentHouseholdId.Value} to this household?" : ("Add " + SelectedResident.FullName + " to this household?"));
		if (!DialogService.Instance.Confirm(message, "Confirm Family Assignment"))
		{
			return false;
		}
		try
		{
			IsLoading = true;
			await Task.Run(delegate
			{
				_residentHouseholdService.AddExistingResidentToHousehold(SelectedResident.ResidentId, _householdId, _barangayId, "Added from household family picker.");
			});
			DialogService.Instance.ShowInfo(SelectedResident.FullName + " was added to the household.");
			return true;
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to attach resident to household", ex);
			DialogService.Instance.ShowError(ex.Message, "Add Family Member");
			return false;
		}
		finally
		{
			IsLoading = false;
		}
	}
}
