using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class BlotterDetailsViewModel : ObservableObject
{
	private readonly BlotterDto _seedRecord;

	private readonly BlotterRepository _repository;

	private readonly AiBlotterService _aiService;

	private string _originalStatus = "ONGOING";

	[ObservableProperty]
	private string _title = "New Blotter Case";

	[ObservableProperty]
	private int _blotterId;

	[ObservableProperty]
	private string _caseNumber = "Draft case";

	[ObservableProperty]
	private bool _isExistingRecord;

	[ObservableProperty]
	private int _complainantId;

	[ObservableProperty]
	private string _complainantSearchText = string.Empty;

	[ObservableProperty]
	private string _complainantDisplayName = "No complainant selected";

	[ObservableProperty]
	private string _complainantAddress = "Use search to find a resident.";

	[ObservableProperty]
	private BlotterResidentLookupItem? _selectedComplainantResult;

	[ObservableProperty]
	private string _respondentResidentIdText = string.Empty;

	[ObservableProperty]
	private string _respondentName = string.Empty;

	[ObservableProperty]
	private string _incidentType = "Other";

	[ObservableProperty]
	private DateTime _incidentDate = DateTime.Today;

	[ObservableProperty]
	private string _incidentTimeText = string.Empty;

	[ObservableProperty]
	private string _incidentLocation = string.Empty;

	[ObservableProperty]
	private string _witnesses = string.Empty;

	[ObservableProperty]
	private string _incidentDetails = string.Empty;

	[ObservableProperty]
	private string _actionTaken = string.Empty;

	[ObservableProperty]
	private string _resolutionDetails = string.Empty;

	[ObservableProperty]
	private string _currentStatus = "ONGOING";

	[ObservableProperty]
	private string _referralDestination = string.Empty;

	[ObservableProperty]
	private string _closureNotes = string.Empty;

	[ObservableProperty]
	private bool _isAiBusy;

	[ObservableProperty]
	private string _aiSummary = "Save first to run AI analysis.";

	[ObservableProperty]
	private string _aiRiskLevel = "N/A";

	[ObservableProperty]
	private string _aiCategory = "N/A";

	[ObservableProperty]
	private DateTime _mediationDate = DateTime.Today.AddDays(1.0);

	[ObservableProperty]
	private string _mediationTimeText = "09:00";

	[ObservableProperty]
	private string _mediationVenue = "Barangay Hall";

	public Action? CloseAction { get; set; }

	public ObservableCollection<BlotterTimelineEventItem> TimelineEvents { get; } = new ObservableCollection<BlotterTimelineEventItem>();

	public ObservableCollection<BlotterResidentLookupItem> ResidentSearchResults { get; } = new ObservableCollection<BlotterResidentLookupItem>();

	public BlotterDetailsViewModel(BlotterDto? existingRecord = null)
	{
		_seedRecord = existingRecord ?? new BlotterDto();
		_repository = new BlotterRepository();
		_aiService = new AiBlotterService();
	}

	public async Task InitializeAsync()
	{
		if (_seedRecord.CaseId > 0)
		{
			BlotterDto blotterDto = await _repository.LoadCaseAsync(_seedRecord.CaseId);
			if (blotterDto == null)
			{
				throw new InvalidOperationException("The selected blotter case could not be found.");
			}
			ApplyRecord(blotterDto, isExistingRecord: true);
			if (ComplainantId > 0)
			{
				await LoadComplainantAsync(ComplainantId);
			}
		}
		else
		{
			ApplyRecord(_seedRecord, isExistingRecord: false);
			if (ComplainantId <= 0)
			{
				await SearchComplainantsAsync();
			}
			else
			{
				await LoadComplainantAsync(ComplainantId);
			}
		}
		await LoadTimelineAsync();
	}

	private void ApplyRecord(BlotterDto source, bool isExistingRecord)
	{
		BlotterId = source.CaseId;
		CaseNumber = (string.IsNullOrWhiteSpace(source.CaseNo) ? "Draft case" : source.CaseNo.Trim());
		IsExistingRecord = isExistingRecord;
		Title = (isExistingRecord ? "Edit Blotter Case" : "New Blotter Case");
		ComplainantId = source.ComplainantId;
		ComplainantDisplayName = (string.IsNullOrWhiteSpace(source.ComplainantName) ? "No complainant selected" : source.ComplainantName.Trim());
		ComplainantAddress = (string.IsNullOrWhiteSpace(source.ComplainantAddress) ? "Use search to find a resident." : source.ComplainantAddress.Trim());
		RespondentResidentIdText = source.RespondentResidentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
		RespondentName = source.RespondentName?.Trim() ?? string.Empty;
		IncidentType = (string.IsNullOrWhiteSpace(source.IncidentType) ? "Other" : source.IncidentType.Trim());
		IncidentDate = ((source.IncidentDate == default(DateTime)) ? DateTime.Today : source.IncidentDate.Date);
		IncidentTimeText = (source.IncidentTime.HasValue ? source.IncidentTime.Value.ToString("hh\\:mm") : string.Empty);
		IncidentLocation = source.IncidentLocation?.Trim() ?? string.Empty;
		Witnesses = source.Witnesses ?? string.Empty;
		IncidentDetails = source.IncidentDetails ?? string.Empty;
		ActionTaken = source.ActionTaken ?? string.Empty;
		ResolutionDetails = source.ResolutionDetails ?? string.Empty;
		ReferralDestination = source.ReferralDestination ?? string.Empty;
		ClosureNotes = source.ClosureNotes ?? string.Empty;
		CurrentStatus = (isExistingRecord ? WorkflowRules.NormalizeBlotterStatus(source.Status) : "ONGOING");
		_originalStatus = CurrentStatus;
		AiSummary = (string.IsNullOrWhiteSpace(source.AiSummary) ? "Save first to run AI analysis." : source.AiSummary);
		AiCategory = (string.IsNullOrWhiteSpace(source.AiCategory) ? "N/A" : source.AiCategory);
		AiRiskLevel = (string.IsNullOrWhiteSpace(source.AiRiskLevel) ? "N/A" : source.AiRiskLevel);
		if (source.ScheduledMediationAt.HasValue)
		{
			MediationDate = source.ScheduledMediationAt.Value.Date;
			MediationTimeText = source.ScheduledMediationAt.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
		}
		if (!string.IsNullOrWhiteSpace(source.MediationVenue))
		{
			MediationVenue = source.MediationVenue.Trim();
		}
	}

	private async Task LoadComplainantAsync(int residentId)
	{
		BlotterResidentLookupItem blotterResidentLookupItem = await _repository.GetResidentAsync(residentId);
		if (blotterResidentLookupItem != null)
		{
			ComplainantId = blotterResidentLookupItem.ResidentId;
			ComplainantDisplayName = blotterResidentLookupItem.FullName;
			ComplainantAddress = (string.IsNullOrWhiteSpace(blotterResidentLookupItem.Address) ? "No address on file." : blotterResidentLookupItem.Address);
		}
	}

	private async Task LoadTimelineAsync()
	{
		TimelineEvents.Clear();
		if (BlotterId <= 0)
		{
			return;
		}
		foreach (DataRow row in (await Task.Run(() => CaseTimelineService.LoadTimeline(BlotterId))).Rows)
		{
			DateTime date = ((row["created_at"] == DBNull.Value) ? DateTime.Now : Convert.ToDateTime(row["created_at"]));
			string text = Convert.ToString(row["event_title"]) ?? "Update";
			string text2 = Convert.ToString(row["event_details"]) ?? string.Empty;
			string text3 = Convert.ToString(row["from_status"]) ?? string.Empty;
			string text4 = Convert.ToString(row["to_status"]) ?? string.Empty;
			string text5 = Convert.ToString(row["created_by"]) ?? "System";
			if (string.IsNullOrWhiteSpace(text2) && (!string.IsNullOrWhiteSpace(text3) || !string.IsNullOrWhiteSpace(text4)))
			{
				text2 = ("Status: " + text3 + " -> " + text4).Trim();
			}
			TimelineEvents.Add(new BlotterTimelineEventItem
			{
				Date = date,
				Event = text,
				Details = text2,
				User = (string.IsNullOrWhiteSpace(text5) ? "System" : text5)
			});
		}
	}

	[RelayCommand]
	private async Task SearchComplainantsAsync()
	{
		IReadOnlyList<BlotterResidentLookupItem> obj = await _repository.SearchResidentsAsync(UserSession.BarangayId, ComplainantSearchText);
		ResidentSearchResults.Clear();
		foreach (BlotterResidentLookupItem item in obj)
		{
			ResidentSearchResults.Add(item);
		}
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (ComplainantId <= 0)
		{
			DialogService.Instance.ShowWarning("Select the complainant resident before saving the blotter.");
			return;
		}
		if (!TryParseOptionalInt(RespondentResidentIdText, out var parsedValue))
		{
			DialogService.Instance.ShowWarning("Respondent resident ID must be a valid number.");
			return;
		}
		if (!TryParseOptionalTime(IncidentTimeText, out var parsedValue2))
		{
			DialogService.Instance.ShowWarning("Incident time must use HH:mm format.", "Invalid time");
			return;
		}
		string status = ((BlotterId <= 0) ? "ONGOING" : "ONGOING");
		ValidationResult validationResult = ValidationService.ValidateBlotterFormSave(parsedValue.HasValue, RespondentName, IncidentType, IncidentLocation, IncidentDetails, IncidentDate, status, ResolutionDetails);
		if (!validationResult.IsValid)
		{
			DialogService.Instance.ShowWarning(validationResult.Message, validationResult.Title);
			return;
		}
		if (string.IsNullOrWhiteSpace(IncidentDetails))
		{
			DialogService.Instance.ShowWarning("Incident details are required.", "Missing data");
			return;
		}
		BlotterDto blotterDto = BuildCurrentRecord();
		blotterDto.RespondentResidentId = parsedValue;
		blotterDto.IncidentTime = parsedValue2;
		blotterDto.Status = ((BlotterId <= 0) ? "ONGOING" : _originalStatus);
		BlotterSaveResult blotterSaveResult = await _repository.SaveCaseAsync(blotterDto);
		BlotterId = blotterSaveResult.CaseId;
		CaseNumber = (string.IsNullOrWhiteSpace(blotterSaveResult.CaseNo) ? CaseNumber : blotterSaveResult.CaseNo);
		CurrentStatus = blotterSaveResult.Status;
		_originalStatus = blotterSaveResult.Status;
		IsExistingRecord = true;
		Title = "Edit Blotter Case";
		await LoadTimelineAsync();
		DialogService.Instance.ShowInfo("Blotter case " + CaseNumber + " saved successfully.");
		CloseAction?.Invoke();
	}

	[RelayCommand]
	private async Task UpdateStatusAsync()
	{
		if (BlotterId <= 0)
		{
			DialogService.Instance.ShowWarning("Save the blotter first before updating its status.");
			return;
		}
		string resolutionDetails = (string.IsNullOrWhiteSpace(ResolutionDetails) ? ClosureNotes : ResolutionDetails);
		ValidationResult validationResult = ValidationService.ValidateBlotterStatusTransition(_originalStatus, CurrentStatus, resolutionDetails, ReferralDestination);
		if (!validationResult.IsValid)
		{
			DialogService.Instance.ShowWarning(validationResult.Message, validationResult.Title);
			return;
		}
		BlotterSaveResult blotterSaveResult = await _repository.UpdateStatusAsync(BlotterId, _originalStatus, CurrentStatus, ResolutionDetails, ReferralDestination, ClosureNotes);
		CurrentStatus = blotterSaveResult.Status;
		_originalStatus = blotterSaveResult.Status;
		await LoadTimelineAsync();
		DialogService.Instance.ShowInfo("Case status updated to " + CurrentStatus + ".");
	}

	[RelayCommand]
	private async Task RunAiAnalysisAsync()
	{
		if (BlotterId <= 0)
		{
			DialogService.Instance.ShowWarning("Save the blotter first before running AI analysis.");
			return;
		}
		try
		{
			IsAiBusy = true;
			AiSummary = "Analyzing narrative...";
			AiBlotterAnalysis analysis = await _aiService.AnalyzeBlotterAsync(BlotterId);
			await _aiService.SaveAnalysisAsync(BlotterId, analysis);
			AiRiskLevel = analysis.RiskLevel;
			AiCategory = analysis.SuggestedCategory;
			AiSummary = analysis.Summary;
			CaseTimelineService.Log(BlotterId, "AI", "AI analysis refreshed", "Risk: " + analysis.RiskLevel + "\nCategory: " + analysis.SuggestedCategory, null, null, (UserSession.UserId > 0) ? new int?(UserSession.UserId) : ((int?)null));
			await LoadTimelineAsync();
			DialogService.Instance.ShowInfo("AI analysis completed and saved.", "AI Analysis");
		}
		catch (Exception ex)
		{
			AiSummary = "AI analysis failed: " + ex.Message;
			DialogService.Instance.ShowError(ex.Message, "AI Analysis");
		}
		finally
		{
			IsAiBusy = false;
		}
	}

	[RelayCommand]
	private async Task ScheduleMediationAsync()
	{
		if (BlotterId <= 0)
		{
			DialogService.Instance.ShowWarning("Save the blotter first before scheduling mediation.");
			return;
		}
		if (!TryParseOptionalTime(MediationTimeText, out var parsedValue) || !parsedValue.HasValue)
		{
			DialogService.Instance.ShowWarning("Mediation time must use HH:mm format.", "Invalid time");
			return;
		}
		if (string.IsNullOrWhiteSpace(MediationVenue))
		{
			DialogService.Instance.ShowWarning("Enter the mediation venue before scheduling.", "Missing data");
			return;
		}
		DateTime scheduleAt = MediationDate.Date.Add(parsedValue.Value);
		await _repository.ScheduleMediationAsync(BlotterId, scheduleAt, MediationVenue);
		await LoadTimelineAsync();
		DialogService.Instance.ShowInfo($"Mediation scheduled for {scheduleAt:MMM dd, yyyy hh:mm tt}.");
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke();
	}

	private BlotterDto BuildCurrentRecord()
	{
		return new BlotterDto
		{
			CaseId = BlotterId,
			CaseNo = CaseNumber,
			ComplainantId = ComplainantId,
			ComplainantName = ComplainantDisplayName,
			ComplainantAddress = ComplainantAddress,
			RespondentName = (RespondentName?.Trim() ?? string.Empty),
			IncidentType = (IncidentType?.Trim() ?? string.Empty),
			IncidentDate = IncidentDate.Date,
			IncidentLocation = (IncidentLocation?.Trim() ?? string.Empty),
			Witnesses = (Witnesses ?? string.Empty),
			IncidentDetails = (IncidentDetails ?? string.Empty),
			ActionTaken = (ActionTaken ?? string.Empty),
			ResolutionDetails = (ResolutionDetails ?? string.Empty),
			Status = CurrentStatus,
			ReferralDestination = (ReferralDestination ?? string.Empty),
			ClosureNotes = (ClosureNotes ?? string.Empty),
			RecordedBy = UserSession.UserId
		};
	}

	private static bool TryParseOptionalInt(string value, out int? parsedValue)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			parsedValue = null;
			return true;
		}
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result > 0)
		{
			parsedValue = result;
			return true;
		}
		parsedValue = null;
		return false;
	}

	private static bool TryParseOptionalTime(string value, out TimeSpan? parsedValue)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			parsedValue = null;
			return true;
		}
		string[] formats = new string[3] { "hh\\:mm", "h\\:mm", "hh\\:mm\\:ss" };
		if (TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out var result))
		{
			parsedValue = result;
			return true;
		}
		if (DateTime.TryParseExact(text, new string[2] { "HH:mm", "H:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
		{
			parsedValue = result2.TimeOfDay;
			return true;
		}
		parsedValue = null;
		return false;
	}
}
