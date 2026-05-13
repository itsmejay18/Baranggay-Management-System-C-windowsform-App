using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class AyudaReleaseViewModel : ObservableObject
{
	private const int ResidentSearchLimit = 10;

	private readonly AyudaService _ayudaService = new AyudaService();

	private readonly BarangayOfficialService _barangayOfficialService = new BarangayOfficialService();

	private readonly int? _initialProgramId;

	private readonly int? _releaseId;

	private AyudaReleaseRecord? _existingRelease;

	private bool _isSynchronizingSelection;

	private CancellationTokenSource? _residentSearchCts;

	[ObservableProperty]
	private string _windowTitle = "Release Ayuda";

	[ObservableProperty]
	private string _windowEyebrowText = "RELEASE BARANGAY AYUDA";

	[ObservableProperty]
	private string _headerTitleText = "Stage beneficiaries for a batch release";

	[ObservableProperty]
	private string _headerDescriptionText = "Choose a budget program, add one or more beneficiaries, then post everything in a single ayuda release batch.";

	[ObservableProperty]
	private string _saveButtonText = "Post Ayuda Batch";

	[ObservableProperty]
	private string _stageButtonText = "Add Beneficiary";

	[ObservableProperty]
	private string _processingMessage = "Posting ayuda release...";

	[ObservableProperty]
	private string _beneficiarySummaryText = "No beneficiaries staged yet";

	[ObservableProperty]
	private int _programId;

	[ObservableProperty]
	private int _residentId;

	[ObservableProperty]
	private string _residentName = string.Empty;

	[ObservableProperty]
	private string _residentContactNo = string.Empty;

	[ObservableProperty]
	private decimal _amount;

	[ObservableProperty]
	private DateTime _releaseDate = DateTime.Today;

	[ObservableProperty]
	private string _referenceNo = "Generated batch and release references when posted";

	[ObservableProperty]
	private string _notes = "Barangay ayuda batch release";

	[ObservableProperty]
	private string _remainingBudgetText = "Select a budget program";

	[ObservableProperty]
	private string _residentSearchText = string.Empty;

	[ObservableProperty]
	private string _residentSearchStatusText = $"Showing up to {10} residents at a time to keep the list fast.";

	[ObservableProperty]
	private bool _isProcessing;

	[ObservableProperty]
	private bool _isExistingReleaseEdit;

	[ObservableProperty]
	private AyudaProgramOption? _selectedProgram;

	[ObservableProperty]
	private OfficialResidentOption? _selectedResident;

	[ObservableProperty]
	private AyudaBeneficiaryDraft? _selectedBatchItem;

	public ObservableCollection<AyudaProgramOption> ProgramOptions { get; } = new ObservableCollection<AyudaProgramOption>();

	public ObservableCollection<OfficialResidentOption> ResidentOptions { get; } = new ObservableCollection<OfficialResidentOption>();

	public ObservableCollection<AyudaBeneficiaryDraft> Beneficiaries { get; } = new ObservableCollection<AyudaBeneficiaryDraft>();

	public event Action<bool?>? CloseRequested;

	public AyudaReleaseViewModel(int? initialProgramId, int? releaseId = null)
	{
		_initialProgramId = initialProgramId;
		_releaseId = releaseId;
		ApplyModeText();
		UpdateBudgetSummary();
	}

	public AyudaReleaseViewModel()
		: this(null)
	{
	}

	public async Task InitializeAsync()
	{
		_ = 2;
		try
		{
			IsProcessing = true;
			AyudaReleaseRecord existingRelease = null;
			if (_releaseId.HasValue && _releaseId.Value > 0)
			{
				existingRelease = await _ayudaService.GetReleaseAsync(_releaseId.Value);
				if (existingRelease == null)
				{
					DialogService.Instance.ShowWarning("The selected ayuda release could not be found.");
					this.CloseRequested?.Invoke(false);
					return;
				}
			}
			ProgramOptions.Clear();
			AyudaService ayudaService = _ayudaService;
			AyudaReleaseRecord ayudaReleaseRecord = existingRelease;
			foreach (AyudaProgramOption item in (await ayudaService.GetProgramOptionsAsync((ayudaReleaseRecord != null) ? new int?(ayudaReleaseRecord.ProgramId) : _initialProgramId)).OrderBy<AyudaProgramOption, string>((AyudaProgramOption option) => option.ProgramName, StringComparer.OrdinalIgnoreCase))
			{
				ProgramOptions.Add(item);
			}
			await ReloadResidentOptionsAsync(existingRelease?.ResidentId).ConfigureAwait(continueOnCapturedContext: true);
			if (existingRelease != null)
			{
				LoadExistingRelease(existingRelease);
			}
			else if (_initialProgramId.HasValue)
			{
				SelectedProgram = ProgramOptions.FirstOrDefault((AyudaProgramOption option) => option.ProgramId == _initialProgramId.Value);
			}
			UpdateBudgetSummary();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda release dialog failed to initialize.", ex);
			DialogService.Instance.ShowError("Could not load ayuda release options.");
			this.CloseRequested?.Invoke(false);
		}
		finally
		{
			IsProcessing = false;
		}
	}

	[RelayCommand]
	private void StageBeneficiary()
	{
		TryStageCurrentBeneficiary(showWarnings: true);
	}

	[RelayCommand]
	private void ClearBeneficiaryEntry()
	{
		SelectedBatchItem = null;
		ClearCurrentEntry(resetSelection: true);
		RefreshStageButtonText();
		UpdateBudgetSummary();
	}

	[RelayCommand]
	private void RemoveSelectedBeneficiary()
	{
		if (IsExistingReleaseEdit)
		{
			DialogService.Instance.ShowWarning("Use Delete Release from the Ayuda page to remove this saved release.");
			return;
		}
		if (SelectedBatchItem == null)
		{
			DialogService.Instance.ShowWarning("Select a staged beneficiary first.");
			return;
		}
		Beneficiaries.Remove(SelectedBatchItem);
		SelectedBatchItem = null;
		ClearCurrentEntry(resetSelection: true);
		RefreshStageButtonText();
		UpdateBudgetSummary();
	}

	[RelayCommand]
	private async Task SaveRelease()
	{
		if (SelectedProgram == null || ProgramId <= 0)
		{
			DialogService.Instance.ShowWarning("Select an ayuda budget program first.");
		}
		else
		{
			if (HasDraftBeneficiaryInput() && !TryStageCurrentBeneficiary(showWarnings: true))
			{
				return;
			}
			if (Beneficiaries.Count != 0)
			{
				try
				{
					IsProcessing = true;
					if (IsExistingReleaseEdit)
					{
						AyudaBeneficiaryDraft ayudaBeneficiaryDraft = Beneficiaries[0];
						await _ayudaService.UpdateReleaseAsync(new AyudaReleaseRecord
						{
							ReleaseId = _releaseId.GetValueOrDefault(),
							ProgramId = ProgramId,
							ResidentId = ayudaBeneficiaryDraft.ResidentId,
							ResidentName = ayudaBeneficiaryDraft.ResidentName,
							Amount = ayudaBeneficiaryDraft.Amount,
							ReleasedAt = ReleaseDate,
							Notes = Notes
						});
						DialogService.Instance.ShowInfo("Ayuda release updated successfully.");
						this.CloseRequested?.Invoke(true);
					}
					else
					{
						AyudaBatchReleaseResult ayudaBatchReleaseResult = await _ayudaService.SaveBatchReleaseAsync(ProgramId, ReleaseDate, Notes, Beneficiaries.ToList());
						ReferenceNo = ayudaBatchReleaseResult.BatchReference;
						if (!string.IsNullOrWhiteSpace(ayudaBatchReleaseResult.ReportFilePath))
						{
							AyudaReleaseReportService.TryOpenGeneratedFile(ayudaBatchReleaseResult.ReportFilePath);
						}
						string text = $"Ayuda released successfully to {ayudaBatchReleaseResult.BeneficiaryCount:N0} beneficiary(ies).\nBatch Reference: {ayudaBatchReleaseResult.BatchReference}";
						if (!string.IsNullOrWhiteSpace(ayudaBatchReleaseResult.ReportFilePath))
						{
							text = text + "\nReport: " + ayudaBatchReleaseResult.ReportFilePath;
						}
						DialogService.Instance.ShowInfo(text);
						this.CloseRequested?.Invoke(true);
					}
					return;
				}
				catch (Exception ex)
				{
					AppLogger.LogError("Ayuda release save failed.", ex);
					DialogService.Instance.ShowError(ex.Message, "Ayuda Release");
					return;
				}
				finally
				{
					IsProcessing = false;
				}
			}
			DialogService.Instance.ShowWarning("Add at least one beneficiary before posting ayuda.");
		}
	}

	private void LoadExistingRelease(AyudaReleaseRecord existingRelease)
	{
		_existingRelease = existingRelease;
		IsExistingReleaseEdit = true;
		ReleaseDate = existingRelease.ReleasedAt;
		ReferenceNo = existingRelease.ReferenceNo;
		Notes = existingRelease.Notes;
		SelectedProgram = ProgramOptions.FirstOrDefault((AyudaProgramOption option) => option.ProgramId == existingRelease.ProgramId);
		Beneficiaries.Clear();
		AyudaBeneficiaryDraft ayudaBeneficiaryDraft = new AyudaBeneficiaryDraft
		{
			PersistedReleaseId = existingRelease.ReleaseId,
			ResidentId = existingRelease.ResidentId,
			ResidentName = existingRelease.ResidentName,
			ContactNo = existingRelease.ResidentContactNo,
			Amount = existingRelease.Amount
		};
		Beneficiaries.Add(ayudaBeneficiaryDraft);
		SelectedBatchItem = ayudaBeneficiaryDraft;
	}

	private bool TryStageCurrentBeneficiary(bool showWarnings)
	{
		if (SelectedResident == null || ResidentId <= 0)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning("Select a resident beneficiary first.");
			}
			return false;
		}
		if (Amount <= 0m)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning("Release amount must be greater than zero.");
			}
			return false;
		}
		if (Beneficiaries.FirstOrDefault((AyudaBeneficiaryDraft item) => item != SelectedBatchItem && item.ResidentId == ResidentId) != null)
		{
			if (showWarnings)
			{
				DialogService.Instance.ShowWarning("This resident is already in the staged beneficiary list.");
			}
			return false;
		}
		AyudaBeneficiaryDraft ayudaBeneficiaryDraft = SelectedBatchItem;
		if (IsExistingReleaseEdit && ayudaBeneficiaryDraft == null)
		{
			ayudaBeneficiaryDraft = Beneficiaries.FirstOrDefault();
		}
		if (ayudaBeneficiaryDraft == null)
		{
			ayudaBeneficiaryDraft = new AyudaBeneficiaryDraft();
			Beneficiaries.Add(ayudaBeneficiaryDraft);
		}
		ayudaBeneficiaryDraft.ResidentId = ResidentId;
		ayudaBeneficiaryDraft.ResidentName = ResidentName;
		ayudaBeneficiaryDraft.ContactNo = ResidentContactNo;
		ayudaBeneficiaryDraft.Amount = decimal.Round(Amount, 2, MidpointRounding.AwayFromZero);
		SelectedBatchItem = ayudaBeneficiaryDraft;
		if (!IsExistingReleaseEdit)
		{
			ClearCurrentEntry(resetSelection: true);
			SelectedBatchItem = null;
		}
		RefreshStageButtonText();
		UpdateBudgetSummary();
		return true;
	}

	private bool HasDraftBeneficiaryInput()
	{
		if (ResidentId <= 0 && SelectedResident == null)
		{
			return Amount > 0m;
		}
		return true;
	}

	private void ClearCurrentEntry(bool resetSelection)
	{
		if (resetSelection)
		{
			_isSynchronizingSelection = true;
			try
			{
				SelectedResident = null;
			}
			finally
			{
				_isSynchronizingSelection = false;
			}
		}
		ResidentId = 0;
		ResidentName = string.Empty;
		ResidentContactNo = string.Empty;
		Amount = 0m;
	}

	private void UpdateBudgetSummary()
	{
		decimal num = SelectedProgram?.RemainingBudget ?? 0m;
		if (_existingRelease != null && _existingRelease.ProgramId == ProgramId && !string.Equals(_existingRelease.ReleaseStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
		{
			num += _existingRelease.Amount;
		}
		decimal num2 = Beneficiaries.Sum((AyudaBeneficiaryDraft item) => item.Amount);
		decimal value = Math.Max(num - num2, 0m);
		RemainingBudgetText = ((SelectedProgram == null) ? "Select a budget program" : $"PHP {num:N2} available | Staged PHP {num2:N2} | After save PHP {value:N2}");
		BeneficiarySummaryText = ((Beneficiaries.Count == 0) ? "No beneficiaries staged yet" : $"{Beneficiaries.Count:N0} beneficiary(ies) staged | Total PHP {num2:N2}");
	}

	private void ApplyModeText()
	{
		if (_releaseId.HasValue && _releaseId.Value > 0)
		{
			WindowTitle = "Edit Ayuda Release";
			WindowEyebrowText = "UPDATE AYUDA RELEASE";
			HeaderTitleText = "Revise the selected ayuda release";
			HeaderDescriptionText = "Update the beneficiary, amount, release date, notes, or target program for the selected ayuda release.";
			SaveButtonText = "Save Release Changes";
			StageButtonText = "Update Beneficiary";
			ProcessingMessage = "Saving ayuda release...";
			Notes = string.Empty;
		}
	}

	private void RefreshStageButtonText()
	{
		if (IsExistingReleaseEdit)
		{
			StageButtonText = "Update Beneficiary";
		}
		else
		{
			StageButtonText = ((SelectedBatchItem == null) ? "Add Beneficiary" : "Update Beneficiary");
		}
	}

	private void ScheduleResidentSearch()
	{
		_residentSearchCts?.Cancel();
		_residentSearchCts?.Dispose();
		CancellationTokenSource cancellationTokenSource = (_residentSearchCts = new CancellationTokenSource());
		string residentSearchText = ResidentSearchText;
		int? preferredResidentId = ((ResidentId > 0) ? new int?(ResidentId) : SelectedResident?.ResidentId);
		RunResidentSearchAsync(residentSearchText, preferredResidentId, cancellationTokenSource.Token);
	}

	private async Task RunResidentSearchAsync(string searchText, int? preferredResidentId, CancellationToken cancellationToken)
	{
		_ = 1;
		try
		{
			await Task.Delay(250, cancellationToken).ConfigureAwait(continueOnCapturedContext: true);
			IReadOnlyList<OfficialResidentOption> residents = await _barangayOfficialService.SearchResidentOptionsAsync(searchText, 10, preferredResidentId).ConfigureAwait(continueOnCapturedContext: true);
			if (!cancellationToken.IsCancellationRequested && string.Equals(searchText, ResidentSearchText, StringComparison.Ordinal))
			{
				ApplyResidentOptions(residents);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			AppLogger.LogError("Ayuda resident search failed.", ex2);
			ResidentSearchStatusText = "Could not load residents. Try searching again.";
		}
	}

	private async Task ReloadResidentOptionsAsync(int? preferredResidentId = null)
	{
		ApplyResidentOptions(await _barangayOfficialService.SearchResidentOptionsAsync(ResidentSearchText, 10, preferredResidentId).ConfigureAwait(continueOnCapturedContext: true));
	}

	private void ApplyResidentOptions(IReadOnlyList<OfficialResidentOption> residents)
	{
		ResidentOptions.Clear();
		foreach (OfficialResidentOption resident in residents)
		{
			ResidentOptions.Add(resident);
		}
		EnsureResidentOptionAvailable(ResidentId, ResidentName, ResidentContactNo);
		RebindSelectedResident();
		UpdateResidentSearchStatus(residents.Count);
	}

	private void EnsureResidentOptionAvailable(int residentId, string residentName, string contactNo)
	{
		if (residentId <= 0 || string.IsNullOrWhiteSpace(residentName))
		{
			return;
		}
		OfficialResidentOption officialResidentOption = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == residentId);
		if (officialResidentOption != null)
		{
			officialResidentOption.FullName = residentName;
			officialResidentOption.ContactNo = contactNo;
			return;
		}
		ResidentOptions.Insert(0, new OfficialResidentOption
		{
			ResidentId = residentId,
			FullName = residentName,
			ContactNo = contactNo
		});
		while (ResidentOptions.Count > 10)
		{
			ResidentOptions.RemoveAt(ResidentOptions.Count - 1);
		}
	}

	private void RebindSelectedResident()
	{
		if (ResidentId <= 0)
		{
			return;
		}
		OfficialResidentOption officialResidentOption = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == ResidentId);
		if (officialResidentOption == null)
		{
			return;
		}
		_isSynchronizingSelection = true;
		try
		{
			SelectedResident = officialResidentOption;
		}
		finally
		{
			_isSynchronizingSelection = false;
		}
	}

	private void UpdateResidentSearchStatus(int resultCount)
	{
		if (resultCount <= 0)
		{
			ResidentSearchStatusText = (string.IsNullOrWhiteSpace(ResidentSearchText) ? "No active residents are available for this barangay yet." : "No matching residents found. Try a different name or contact number.");
		}
		else if (string.IsNullOrWhiteSpace(ResidentSearchText))
		{
			ResidentSearchStatusText = ((resultCount >= 10) ? $"Showing the first {10} active residents. Search by name or contact number to narrow the list." : $"Showing {resultCount} active resident(s).");
		}
		else
		{
			ResidentSearchStatusText = ((resultCount >= 10) ? $"Showing the first {10} matching residents." : $"Showing {resultCount} matching resident(s).");
		}
	}

	partial void OnSelectedBatchItemChanged(AyudaBeneficiaryDraft? value)
	{
		if (value == null)
		{
			return;
		}
		_isSynchronizingSelection = true;
		try
		{
			EnsureResidentOptionAvailable(value.ResidentId, value.ResidentName, value.ContactNo);
			SelectedResident = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == value.ResidentId);
			ResidentId = value.ResidentId;
			ResidentName = value.ResidentName;
			ResidentContactNo = value.ContactNo;
			Amount = value.Amount;
		}
		finally
		{
			_isSynchronizingSelection = false;
		}
	}
}
