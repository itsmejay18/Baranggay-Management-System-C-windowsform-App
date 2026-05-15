using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.ViewModels;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;

namespace baranggaysystem1.Views.Panels;

/// <summary>
/// Fullscreen form panel for Add/Edit/Resolve blotter case operations.
/// Extends FullscreenFormBase to provide dirty tracking, validation,
/// and async save workflow.
///
/// Requirements: 1.1, 5.7
/// </summary>
public partial class BlotterFormPanel : FullscreenFormBase
{
    private readonly BlotterRepository _repository;
    private readonly FormMode _mode;
    private readonly BlotterDto _seedRecord;
    private string _originalStatus = "ONGOING";
    private int _complainantId;
    private int _blotterId;
    private string _caseNumber = string.Empty;
    private bool _isLoading;

    private static readonly string[] StatusOptions = new[] { "ONGOING", "SETTLED", "REFERRED", "CLOSED" };

    public ObservableCollection<BlotterResidentLookupItem> ResidentSearchResults { get; } = new();

    public BlotterFormPanel(FormMode mode, BlotterDto? existingRecord = null)
    {
        InitializeComponent();

        _repository = new BlotterRepository();
        _mode = mode;
        _seedRecord = existingRecord ?? new BlotterDto();

        // Set up the validation panel reference from FullscreenFormBase
        ValidationPanel = validationPanel;

        InitializeComboBoxes();

        Loaded += OnLoadedAsync;
    }

    private void InitializeComboBoxes()
    {
        cmbStatus.ItemsSource = StatusOptions;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAsync;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _isLoading = true;
        try
        {
            if (_seedRecord.CaseId > 0)
            {
                // Edit mode: load full case details from database
                var fullRecord = await _repository.LoadCaseAsync(_seedRecord.CaseId);
                if (fullRecord == null)
                {
                    ToastService.Error("Error", "The selected blotter case could not be found.");
                    return;
                }
                ApplyRecord(fullRecord, isExisting: true);

                if (_complainantId > 0)
                {
                    await LoadComplainantDisplayAsync(_complainantId);
                }
            }
            else
            {
                // Create mode: apply seed data (may have pre-filled respondent from context)
                ApplyRecord(_seedRecord, isExisting: false);

                if (_complainantId > 0)
                {
                    await LoadComplainantDisplayAsync(_complainantId);
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("BlotterFormPanel initialization failed.", ex);
            ToastService.Error("Load Error", ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplyRecord(BlotterDto source, bool isExisting)
    {
        _blotterId = source.CaseId;
        _caseNumber = source.CaseNo ?? string.Empty;
        _complainantId = source.ComplainantId;
        _originalStatus = isExisting
            ? WorkflowRules.NormalizeBlotterStatus(source.Status)
            : "ONGOING";

        // Complainant display
        lblComplainantName.Text = string.IsNullOrWhiteSpace(source.ComplainantName)
            ? "No complainant selected"
            : source.ComplainantName.Trim();
        lblComplainantAddress.Text = string.IsNullOrWhiteSpace(source.ComplainantAddress)
            ? "Use search to find a resident."
            : source.ComplainantAddress.Trim();

        // Respondent
        txtRespondentName.Text = source.RespondentName?.Trim() ?? string.Empty;
        txtRespondentResidentId.Text = source.RespondentResidentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        // Incident info
        txtIncidentType.Text = string.IsNullOrWhiteSpace(source.IncidentType) ? "Other" : source.IncidentType.Trim();
        dpIncidentDate.SelectedDate = source.IncidentDate == default ? DateTime.Today : source.IncidentDate.Date;
        txtIncidentTime.Text = source.IncidentTime.HasValue ? source.IncidentTime.Value.ToString(@"hh\:mm") : string.Empty;
        txtIncidentLocation.Text = source.IncidentLocation?.Trim() ?? string.Empty;
        txtWitnesses.Text = source.Witnesses ?? string.Empty;
        txtIncidentDetails.Text = source.IncidentDetails ?? string.Empty;

        // Actions & Resolution
        txtActionTaken.Text = source.ActionTaken ?? string.Empty;
        txtResolutionDetails.Text = source.ResolutionDetails ?? string.Empty;
        cmbStatus.SelectedItem = _originalStatus;
        txtReferralDestination.Text = source.ReferralDestination ?? string.Empty;
        txtClosureNotes.Text = source.ClosureNotes ?? string.Empty;
    }

    private async Task LoadComplainantDisplayAsync(int residentId)
    {
        try
        {
            var resident = await _repository.GetResidentAsync(residentId);
            if (resident != null)
            {
                _complainantId = resident.ResidentId;
                lblComplainantName.Text = resident.FullName;
                lblComplainantAddress.Text = string.IsNullOrWhiteSpace(resident.Address)
                    ? "No address on file."
                    : resident.Address;
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to load complainant display.", ex);
        }
    }

    #region FullscreenFormBase Overrides

    protected override bool ValidateForm()
    {
        var errors = new List<string>();

        if (_complainantId <= 0)
            errors.Add("Complainant is required. Use search to select a resident.");

        if (string.IsNullOrWhiteSpace(txtRespondentName.Text))
            errors.Add("Respondent name is required.");

        if (string.IsNullOrWhiteSpace(txtIncidentType.Text))
            errors.Add("Incident type is required.");

        if (dpIncidentDate.SelectedDate == null)
            errors.Add("Incident date is required.");
        else if (dpIncidentDate.SelectedDate > DateTime.Today)
            errors.Add("Incident date cannot be in the future.");

        if (string.IsNullOrWhiteSpace(txtIncidentDetails.Text))
            errors.Add("Incident details are required.");

        if (!string.IsNullOrWhiteSpace(txtIncidentLocation.Text) && txtIncidentLocation.Text.Trim().Length > 120)
            errors.Add("Incident location should be 120 characters or less.");

        if (!string.IsNullOrWhiteSpace(txtRespondentResidentId.Text))
        {
            if (!int.TryParse(txtRespondentResidentId.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedId) || parsedId <= 0)
                errors.Add("Respondent resident ID must be a valid positive number.");
        }

        if (!string.IsNullOrWhiteSpace(txtIncidentTime.Text))
        {
            if (!TryParseTime(txtIncidentTime.Text, out _))
                errors.Add("Incident time must use HH:mm format.");
        }

        _validationErrors = errors;
        IsValid = errors.Count == 0;
        return IsValid;
    }

    private List<string> _validationErrors = new();

    protected override IReadOnlyList<string> GetValidationErrors()
    {
        return _validationErrors;
    }

    protected override async Task<bool> SaveAsync()
    {
        TryParseTime(txtIncidentTime.Text, out TimeSpan? incidentTime);

        int? respondentResidentId = null;
        if (!string.IsNullOrWhiteSpace(txtRespondentResidentId.Text))
        {
            if (int.TryParse(txtRespondentResidentId.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedId) && parsedId > 0)
                respondentResidentId = parsedId;
        }

        var dto = new BlotterDto
        {
            CaseId = _blotterId,
            CaseNo = _caseNumber,
            ComplainantId = _complainantId,
            ComplainantName = lblComplainantName.Text,
            ComplainantAddress = lblComplainantAddress.Text,
            RespondentResidentId = respondentResidentId,
            RespondentName = txtRespondentName.Text.Trim(),
            IncidentType = txtIncidentType.Text.Trim(),
            IncidentDate = dpIncidentDate.SelectedDate ?? DateTime.Today,
            IncidentTime = incidentTime,
            IncidentLocation = txtIncidentLocation.Text.Trim(),
            Witnesses = txtWitnesses.Text.Trim(),
            IncidentDetails = txtIncidentDetails.Text.Trim(),
            ActionTaken = txtActionTaken.Text.Trim(),
            ResolutionDetails = txtResolutionDetails.Text.Trim(),
            Status = (_blotterId <= 0) ? "ONGOING" : _originalStatus,
            ReferralDestination = txtReferralDestination.Text.Trim(),
            ClosureNotes = txtClosureNotes.Text.Trim(),
            RecordedBy = UserSession.UserId
        };

        var result = await _repository.SaveCaseAsync(dto);
        _blotterId = result.CaseId;
        _caseNumber = result.CaseNo ?? _caseNumber;
        _originalStatus = result.Status;
        return true;
    }

    protected override void ResetForm()
    {
        _isLoading = true;
        try
        {
            _complainantId = 0;
            _blotterId = 0;
            _caseNumber = string.Empty;
            _originalStatus = "ONGOING";

            lblComplainantName.Text = "No complainant selected";
            lblComplainantAddress.Text = "Use search to find a resident.";
            txtComplainantSearch.Text = string.Empty;
            lstComplainantResults.Visibility = Visibility.Collapsed;

            txtRespondentName.Text = string.Empty;
            txtRespondentResidentId.Text = string.Empty;
            txtIncidentType.Text = "Other";
            dpIncidentDate.SelectedDate = DateTime.Today;
            txtIncidentTime.Text = string.Empty;
            txtIncidentLocation.Text = string.Empty;
            txtWitnesses.Text = string.Empty;
            txtIncidentDetails.Text = string.Empty;
            txtActionTaken.Text = string.Empty;
            txtResolutionDetails.Text = string.Empty;
            cmbStatus.SelectedItem = "ONGOING";
            txtReferralDestination.Text = string.Empty;
            txtClosureNotes.Text = string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    #endregion

    #region Resolve Case

    /// <summary>
    /// Resolves the blotter case by updating its status to SETTLED.
    /// Called from the toolbar "Resolve Case" button.
    /// </summary>
    public async Task<bool> TryResolveAsync()
    {
        if (_blotterId <= 0)
        {
            ToastService.Error("Resolve Failed", "Save the blotter case first before resolving.");
            return false;
        }

        string resolutionDetails = string.IsNullOrWhiteSpace(txtResolutionDetails.Text)
            ? txtClosureNotes.Text.Trim()
            : txtResolutionDetails.Text.Trim();

        if (string.IsNullOrWhiteSpace(resolutionDetails))
        {
            ToastService.Error("Resolve Failed", "Resolution details or closure notes are required to resolve a case.");
            return false;
        }

        var validationResult = ValidationService.ValidateBlotterStatusTransition(
            _originalStatus, "SETTLED", resolutionDetails, txtReferralDestination.Text.Trim());

        if (!validationResult.IsValid)
        {
            ToastService.Error(validationResult.Title, validationResult.Message);
            return false;
        }

        try
        {
            var result = await _repository.UpdateStatusAsync(
                _blotterId, _originalStatus, "SETTLED",
                resolutionDetails, txtReferralDestination.Text.Trim(), txtClosureNotes.Text.Trim());

            _originalStatus = result.Status;
            cmbStatus.SelectedItem = result.Status;
            IsDirty = false;

            ToastService.Success("Resolved", $"Case {_caseNumber} has been resolved.");
            FullscreenNavigationExtensions.InvokeOnSavedCallback();
            return true;
        }
        catch (Exception ex)
        {
            ToastService.Error("Resolve Failed", ex.Message);
            return false;
        }
    }

    #endregion

    #region Event Handlers

    private void Field_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void Field_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: true);
        }
    }

    private void ComboField_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void DateField_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading)
        {
            MarkFieldDirty(validateImmediately: false);
        }
    }

    private void ComplainantSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Don't mark dirty for search text changes — only when a complainant is selected
    }

    private async void BtnSearchComplainant_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var results = await _repository.SearchResidentsAsync(
                UserSession.BarangayId, txtComplainantSearch.Text.Trim());

            ResidentSearchResults.Clear();
            foreach (var item in results)
            {
                ResidentSearchResults.Add(item);
            }

            lstComplainantResults.ItemsSource = ResidentSearchResults;
            lstComplainantResults.Visibility = ResidentSearchResults.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (ResidentSearchResults.Count == 0)
            {
                ToastService.Info("Search", "No residents found matching the search criteria.");
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Complainant search failed.", ex);
            ToastService.Error("Search Error", ex.Message);
        }
    }

    private void ComplainantResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstComplainantResults.SelectedItem is BlotterResidentLookupItem selected)
        {
            _complainantId = selected.ResidentId;
            lblComplainantName.Text = selected.FullName;
            lblComplainantAddress.Text = string.IsNullOrWhiteSpace(selected.Address)
                ? "No address on file."
                : selected.Address;

            lstComplainantResults.Visibility = Visibility.Collapsed;

            if (!_isLoading)
            {
                MarkFieldDirty(validateImmediately: false);
            }
        }
    }

    #endregion

    #region Helpers

    private static bool TryParseTime(string? value, out TimeSpan? parsedValue)
    {
        string text = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            parsedValue = null;
            return true;
        }

        string[] formats = new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" };
        if (TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out var result))
        {
            parsedValue = result;
            return true;
        }

        if (DateTime.TryParseExact(text, new[] { "HH:mm", "H:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtResult))
        {
            parsedValue = dtResult.TimeOfDay;
            return true;
        }

        parsedValue = null;
        return false;
    }

    #endregion
}
