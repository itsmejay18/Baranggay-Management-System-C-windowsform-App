using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using baranggaysystem1.helper;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels.Navigation;
using baranggaysystem1.Views.Controls;

namespace baranggaysystem1.Views.Panels;

/// <summary>
/// Fullscreen form panel for Issue/Request certificate operations.
/// Extends FullscreenFormBase to provide dirty tracking, validation,
/// and async save workflow.
///
/// Requirements: 1.1
/// </summary>
public partial class CertificateFormPanel : FullscreenFormBase
{
    private readonly CertificateRequestService _certificateService;
    private readonly BarangayOfficialService _barangayOfficialService;
    private readonly CertificateDialogMode _mode;
    private readonly int? _requestId;
    private readonly int _residentId;
    private readonly string _residentName;
    private bool _isLoading;
    private bool _loadedExistingFee;
    private List<string> _validationErrors = new();

    public ObservableCollection<CertificateDocumentTypeOption> DocumentTypes { get; } = new();
    public ObservableCollection<OfficialResidentOption> ResidentOptions { get; } = new();

    private static readonly string[] PaymentMethods = { "Cash", "GCash", "Bank Transfer" };

    /// <summary>
    /// Constructor for creating a new certificate request (no pre-selected resident).
    /// </summary>
    public CertificateFormPanel(CertificateDialogMode mode)
        : this(mode, 0, string.Empty, null)
    {
    }

    /// <summary>
    /// Constructor for issuing/requesting a certificate for a specific resident.
    /// </summary>
    public CertificateFormPanel(CertificateDialogMode mode, int residentId, string residentName)
        : this(mode, residentId, residentName, null)
    {
    }

    /// <summary>
    /// Constructor for editing/issuing an existing certificate request.
    /// </summary>
    public CertificateFormPanel(CertificateDialogMode mode, int requestId, bool loadExisting)
        : this(mode, 0, string.Empty, requestId)
    {
    }

    private CertificateFormPanel(CertificateDialogMode mode, int residentId, string residentName, int? requestId)
    {
        InitializeComponent();

        _certificateService = new CertificateRequestService();
        _barangayOfficialService = new BarangayOfficialService();
        _mode = mode;
        _residentId = residentId;
        _residentName = residentName;
        _requestId = requestId;

        // Set up the validation panel reference from FullscreenFormBase
        ValidationPanel = validationPanel;

        InitializeControls();
        Loaded += CertificateFormPanel_Loaded;
    }

    private void InitializeControls()
    {
        _isLoading = true;
        try
        {
            // Payment methods
            cmbPaymentMethod.ItemsSource = PaymentMethods;
            cmbPaymentMethod.SelectedIndex = 0;

            // Default values
            txtPurpose.Text = "Identification / General Employment";
            dpIssuedDate.SelectedDate = DateTime.Now;

            // Show/hide sections based on mode
            bool showPayment = _mode == CertificateDialogMode.Issue;
            paymentSection.Visibility = showPayment ? Visibility.Visible : Visibility.Collapsed;

            // Show resident picker or display
            bool hasResident = _residentId > 0 && !string.IsNullOrWhiteSpace(_residentName);
            residentPickerSection.Visibility = hasResident ? Visibility.Collapsed : Visibility.Visible;
            residentDisplaySection.Visibility = hasResident ? Visibility.Visible : Visibility.Collapsed;

            if (hasResident)
            {
                txtResidentDisplay.Text = _residentName;
            }

            // Helper text
            txtHelperText.Text = _mode switch
            {
                CertificateDialogMode.Issue => "Verify the resident and finalize the document for release.",
                CertificateDialogMode.EditRequest => "Update the details of this pending document request.",
                _ => "Create a resident-linked document request for queue processing."
            };
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void CertificateFormPanel_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        try
        {
            // Load document types
            var types = await _certificateService.GetDocumentTypesAsync();
            DocumentTypes.Clear();
            foreach (var type in types)
            {
                DocumentTypes.Add(type);
            }
            cmbDocumentType.ItemsSource = DocumentTypes;

            // Load resident options if picker is visible
            if (residentPickerSection.Visibility == Visibility.Visible)
            {
                var residents = await _barangayOfficialService.GetResidentOptionsAsync();
                ResidentOptions.Clear();
                foreach (var resident in residents.OrderBy(r => r.FullName))
                {
                    ResidentOptions.Add(resident);
                }
                cmbResident.ItemsSource = ResidentOptions;

                // Pre-select resident if ID was provided
                if (_residentId > 0)
                {
                    cmbResident.SelectedItem = ResidentOptions.FirstOrDefault(r => r.ResidentId == _residentId);
                }
            }

            // Load existing request data if editing
            if (_requestId.HasValue && _requestId.Value > 0)
            {
                var request = await _certificateService.GetRequestAsync(_requestId.Value);
                if (request != null)
                {
                    PopulateFromExistingRequest(request);
                }
            }

            // Default document type selection
            if (cmbDocumentType.SelectedItem == null && DocumentTypes.Count > 0)
            {
                cmbDocumentType.SelectedItem = DocumentTypes.FirstOrDefault(t =>
                    string.Equals(t.Name, "Barangay Clearance", StringComparison.OrdinalIgnoreCase))
                    ?? DocumentTypes[0];
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to initialize certificate form panel.", ex);
            ToastService.Error("Load Error", "Could not load certificate form data.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void PopulateFromExistingRequest(CertificateRequestDraft request)
    {
        // Update resident display
        if (request.ResidentId > 0)
        {
            residentPickerSection.Visibility = Visibility.Collapsed;
            residentDisplaySection.Visibility = Visibility.Visible;
            txtResidentDisplay.Text = request.ResidentName;

            if (residentPickerSection.Visibility == Visibility.Visible)
            {
                cmbResident.SelectedItem = ResidentOptions.FirstOrDefault(r => r.ResidentId == request.ResidentId);
            }
        }

        txtPurpose.Text = request.Purpose;
        txtFee.Text = request.Fee > 0 ? request.Fee.ToString("F2", CultureInfo.InvariantCulture) : string.Empty;
        txtOrNumber.Text = request.OrNumber;
        txtBusinessName.Text = request.BusinessName;
        txtBusinessNature.Text = request.BusinessNature;
        dpIssuedDate.SelectedDate = DateTime.Now;
        _loadedExistingFee = request.Fee > 0;

        // Select matching document type
        cmbDocumentType.SelectedItem = DocumentTypes.FirstOrDefault(t => t.DocTypeId == request.DocTypeId)
            ?? DocumentTypes.FirstOrDefault(t => string.Equals(t.Name, request.DocumentTypeName, StringComparison.OrdinalIgnoreCase));
    }

    #region FullscreenFormBase Overrides

    protected override bool ValidateForm()
    {
        var errors = new List<string>();

        // Resident validation
        int residentId = GetSelectedResidentId();
        string residentName = GetSelectedResidentName();
        if (residentId <= 0 || string.IsNullOrWhiteSpace(residentName))
        {
            errors.Add("A resident must be selected.");
        }

        // Document type
        if (cmbDocumentType.SelectedItem is not CertificateDocumentTypeOption selectedType)
        {
            errors.Add("Document type is required.");
        }
        else
        {
            // Use existing validation logic
            var result = ValidationService.ValidateCertificateDialogSave(
                selectedType.Name,
                txtPurpose.Text.Trim(),
                txtBusinessName.Text.Trim(),
                txtBusinessNature.Text.Trim(),
                ParseFee(),
                txtOrNumber.Text.Trim(),
                _mode == CertificateDialogMode.Issue ? (cmbPaymentMethod.SelectedItem?.ToString() ?? string.Empty) : null,
                dpIssuedDate.SelectedDate ?? DateTime.Now,
                _mode);

            if (!result.IsValid)
            {
                errors.Add(result.Message);
            }
        }

        _validationErrors = errors;
        IsValid = errors.Count == 0;
        return IsValid;
    }

    protected override IReadOnlyList<string> GetValidationErrors()
    {
        return _validationErrors;
    }

    protected override async Task<bool> SaveAsync()
    {
        var selectedType = cmbDocumentType.SelectedItem as CertificateDocumentTypeOption;
        if (selectedType == null) return false;

        int residentId = GetSelectedResidentId();
        string residentName = GetSelectedResidentName();

        var draft = new CertificateRequestDraft
        {
            RequestId = _requestId,
            ResidentId = residentId,
            ResidentName = residentName,
            DocTypeId = selectedType.DocTypeId,
            DocumentTypeName = selectedType.Name,
            DocumentTypeCode = selectedType.Code,
            ValidityDays = selectedType.ValidityDays,
            Purpose = txtPurpose.Text.Trim(),
            Fee = ParseFee(),
            OrNumber = txtOrNumber.Text.Trim(),
            BusinessName = txtBusinessName.Text.Trim(),
            BusinessNature = txtBusinessNature.Text.Trim(),
            IssuedDate = dpIssuedDate.SelectedDate ?? DateTime.Now,
            Status = _mode == CertificateDialogMode.Request ? "SUBMITTED" : "RELEASED"
        };

        if (_mode == CertificateDialogMode.Request)
        {
            await _certificateService.CreateRequestAsync(draft);
        }
        else
        {
            await _certificateService.IssueAsync(draft);
        }

        return true;
    }

    protected override void ResetForm()
    {
        _isLoading = true;
        try
        {
            cmbResident.SelectedItem = null;
            cmbDocumentType.SelectedItem = null;
            txtPurpose.Text = "Identification / General Employment";
            txtFee.Text = string.Empty;
            txtOrNumber.Text = string.Empty;
            txtBusinessName.Text = string.Empty;
            txtBusinessNature.Text = string.Empty;
            cmbPaymentMethod.SelectedIndex = 0;
            dpIssuedDate.SelectedDate = DateTime.Now;
            businessFieldsSection.Visibility = Visibility.Collapsed;
        }
        finally
        {
            _isLoading = false;
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

    private void DocumentType_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        var selectedType = cmbDocumentType.SelectedItem as CertificateDocumentTypeOption;
        if (selectedType != null)
        {
            // Update fee from default if not loaded from existing
            if (!_loadedExistingFee)
            {
                txtFee.Text = selectedType.DefaultFee > 0
                    ? selectedType.DefaultFee.ToString("F2", CultureInfo.InvariantCulture)
                    : string.Empty;
            }
            _loadedExistingFee = false;

            // Show/hide business fields
            bool isBusiness = selectedType.Name.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0;
            businessFieldsSection.Visibility = isBusiness ? Visibility.Visible : Visibility.Collapsed;

            if (!isBusiness)
            {
                txtBusinessName.Text = string.Empty;
                txtBusinessNature.Text = string.Empty;
            }
        }

        MarkFieldDirty(validateImmediately: false);
    }

    #endregion

    #region Helpers

    private int GetSelectedResidentId()
    {
        if (_residentId > 0)
            return _residentId;

        if (_requestId.HasValue && _requestId.Value > 0)
        {
            // When editing an existing request, the resident is already set
            // Check if we have a display name (meaning resident was loaded from request)
            if (residentDisplaySection.Visibility == Visibility.Visible)
                return _residentId > 0 ? _residentId : GetResidentIdFromPicker();
        }

        return GetResidentIdFromPicker();
    }

    private int GetResidentIdFromPicker()
    {
        if (cmbResident.SelectedItem is OfficialResidentOption selected)
            return selected.ResidentId;
        return 0;
    }

    private string GetSelectedResidentName()
    {
        if (!string.IsNullOrWhiteSpace(_residentName))
            return _residentName;

        if (cmbResident.SelectedItem is OfficialResidentOption selected)
            return selected.FullName;

        return string.Empty;
    }

    private decimal ParseFee()
    {
        if (decimal.TryParse(txtFee.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fee))
            return fee;
        return 0m;
    }

    #endregion
}
