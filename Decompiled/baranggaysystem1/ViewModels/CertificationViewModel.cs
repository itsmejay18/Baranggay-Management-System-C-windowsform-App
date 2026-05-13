using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class CertificationViewModel : ObservableObject
{
	private readonly CertificateRequestService _certificateService = new CertificateRequestService();

	private readonly BarangayOfficialService _barangayOfficialService = new BarangayOfficialService();

	private readonly bool _loadExistingRequest;

	private bool _loadedExistingFee;

	[ObservableProperty]
	private int? _requestId;

	[ObservableProperty]
	private int _residentId;

	[ObservableProperty]
	private string _residentName = string.Empty;

	[ObservableProperty]
	private CertificateDocumentTypeOption? _selectedType;

	[ObservableProperty]
	private string _purpose = "Identification / General Employment";

	[ObservableProperty]
	private string _orNumber = string.Empty;

	[ObservableProperty]
	private decimal _fee;

	[ObservableProperty]
	private bool _isProcessing;

	[ObservableProperty]
	private string _paymentMethod = "Cash";

	[ObservableProperty]
	private string _businessName = string.Empty;

	[ObservableProperty]
	private string _businessNature = string.Empty;

	[ObservableProperty]
	private DateTime _issuedDate = DateTime.Now;

	[ObservableProperty]
	private bool _showResidentPicker;

	[ObservableProperty]
	private OfficialResidentOption? _selectedResident;

	[ObservableProperty]
	private CertificateDialogMode _mode;

	[ObservableProperty]
	private string _windowTitle = "Certificate Request";

	[ObservableProperty]
	private string _headerEyebrow = "CERTIFICATE REQUEST";

	[ObservableProperty]
	private string _actionButtonText = "Submit Request";

	[ObservableProperty]
	private string _helperText = "Create a resident-linked document request.";

	[ObservableProperty]
	private string _loadingMessage = "Preparing certificate form...";

	public ObservableCollection<CertificateDocumentTypeOption> CertificationTypes { get; } = new ObservableCollection<CertificateDocumentTypeOption>();

	public ObservableCollection<OfficialResidentOption> ResidentOptions { get; } = new ObservableCollection<OfficialResidentOption>();

	public ObservableCollection<string> PaymentMethods { get; } = new ObservableCollection<string> { "Cash", "GCash", "Bank Transfer" };

	public bool ShowPaymentFields => Mode == CertificateDialogMode.Issue;

	public bool IsBusinessType
	{
		get
		{
			CertificateDocumentTypeOption? selectedType = SelectedType;
			if (selectedType == null)
			{
				return false;
			}
			return selectedType.Name.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}

	public event Action<bool?>? CloseRequested;

	public CertificationViewModel()
		: this(CertificateDialogMode.Request)
	{
	}

	public CertificationViewModel(CertificateDialogMode mode)
	{
		Mode = mode;
		ShowResidentPicker = true;
		ApplyModePresentation();
	}

	public CertificationViewModel(int residentId, string residentName, CertificateDialogMode mode = CertificateDialogMode.Issue)
	{
		Mode = mode;
		ResidentId = residentId;
		ResidentName = residentName;
		ShowResidentPicker = residentId <= 0;
		ApplyModePresentation();
	}

	public CertificationViewModel(int requestId, CertificateDialogMode mode, bool loadExistingRequest)
	{
		Mode = mode;
		RequestId = requestId;
		_loadExistingRequest = loadExistingRequest;
		ShowResidentPicker = false;
		ApplyModePresentation();
	}

	public async Task InitializeAsync()
	{
		_ = 2;
		try
		{
			IsProcessing = true;
			LoadingMessage = "Loading document types...";
			CertificationTypes.Clear();
			ResidentOptions.Clear();
			foreach (CertificateDocumentTypeOption item in await _certificateService.GetDocumentTypesAsync())
			{
				CertificationTypes.Add(item);
			}
			if (_loadExistingRequest && RequestId.HasValue && RequestId.Value > 0)
			{
				LoadingMessage = "Loading selected request...";
				CertificateRequestDraft request = await _certificateService.GetRequestAsync(RequestId.Value);
				if (request != null)
				{
					ResidentId = request.ResidentId;
					ResidentName = request.ResidentName;
					Purpose = request.Purpose;
					Fee = request.Fee;
					OrNumber = request.OrNumber;
					BusinessName = request.BusinessName;
					BusinessNature = request.BusinessNature;
					IssuedDate = DateTime.Now;
					_loadedExistingFee = request.Fee > 0m;
					SelectedType = CertificationTypes.FirstOrDefault((CertificateDocumentTypeOption type) => type.DocTypeId == request.DocTypeId) ?? CertificationTypes.FirstOrDefault((CertificateDocumentTypeOption type) => string.Equals(type.Name, request.DocumentTypeName, StringComparison.OrdinalIgnoreCase));
				}
			}
			if (ShowResidentPicker)
			{
				LoadingMessage = "Loading residents...";
				foreach (OfficialResidentOption item2 in (await _barangayOfficialService.GetResidentOptionsAsync()).OrderBy((OfficialResidentOption option) => option.FullName))
				{
					ResidentOptions.Add(item2);
				}
				if (ResidentId > 0)
				{
					SelectedResident = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == ResidentId);
				}
			}
			if (SelectedType == null && CertificationTypes.Count > 0)
			{
				SelectedType = CertificationTypes.FirstOrDefault((CertificateDocumentTypeOption type) => string.Equals(type.Name, "Barangay Clearance", StringComparison.OrdinalIgnoreCase)) ?? CertificationTypes[0];
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to initialize certification dialog.", ex);
			DialogService.Instance.ShowError("Could not load certificate request details.");
		}
		finally
		{
			IsProcessing = false;
			LoadingMessage = "Preparing certificate form...";
		}
	}

	[RelayCommand]
	private async Task Save()
	{
		if (ResidentId <= 0 || string.IsNullOrWhiteSpace(ResidentName))
		{
			DialogService.Instance.ShowWarning("Select a resident before saving this document.");
			return;
		}
		ValidationResult validationResult = ValidationService.ValidateCertificateDialogSave(SelectedType?.Name, Purpose, BusinessName, BusinessNature, Fee, OrNumber, ShowPaymentFields ? PaymentMethod : null, IssuedDate, Mode);
		if (!validationResult.IsValid)
		{
			DialogService.Instance.ShowWarning(validationResult.Message, validationResult.Title);
			return;
		}
		if (SelectedType == null)
		{
			DialogService.Instance.ShowWarning("Select a document type first.");
			return;
		}
		try
		{
			IsProcessing = true;
			LoadingMessage = ((Mode == CertificateDialogMode.Request) ? "Submitting certificate request..." : "Saving and releasing certificate...");
			CertificateRequestDraft draft = new CertificateRequestDraft
			{
				RequestId = RequestId,
				ResidentId = ResidentId,
				ResidentName = ResidentName,
				DocTypeId = SelectedType.DocTypeId,
				DocumentTypeName = SelectedType.Name,
				DocumentTypeCode = SelectedType.Code,
				ValidityDays = SelectedType.ValidityDays,
				Purpose = Purpose.Trim(),
				Fee = Fee,
				OrNumber = OrNumber.Trim(),
				BusinessName = BusinessName.Trim(),
				BusinessNature = BusinessNature.Trim(),
				IssuedDate = IssuedDate,
				Status = ((Mode == CertificateDialogMode.Request) ? "SUBMITTED" : "RELEASED")
			};
			string text = ((Mode != CertificateDialogMode.Request) ? (await _certificateService.IssueAsync(draft)) : (await _certificateService.CreateRequestAsync(draft)));
			string message = text;
			DialogService.Instance.ShowInfo(message);
			this.CloseRequested?.Invoke(true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to save certificate request.", ex);
			DialogService.Instance.ShowError("Could not save the certificate request.");
		}
		finally
		{
			IsProcessing = false;
			LoadingMessage = "Preparing certificate form...";
		}
	}

	private void ApplyModePresentation()
	{
		switch (Mode)
		{
		case CertificateDialogMode.Issue:
			WindowTitle = "Release Certificate";
			HeaderEyebrow = "CERTIFICATE RELEASE";
			ActionButtonText = "Release & Print";
			HelperText = "Verify the resident and finalize the document for release.";
			break;
		case CertificateDialogMode.EditRequest:
			WindowTitle = "Edit Certificate Request";
			HeaderEyebrow = "REQUEST UPDATE";
			ActionButtonText = "Save Changes";
			HelperText = "Update the details of this pending document request.";
			break;
		default:
			WindowTitle = "New Certificate Request";
			HeaderEyebrow = "CERTIFICATE REQUEST";
			ActionButtonText = "Submit Request";
			HelperText = "Create a resident-linked document request for queue processing.";
			break;
		}
		OnPropertyChanged("ShowPaymentFields");
		OnPropertyChanged("IsBusinessType");
	}

	partial void OnSelectedTypeChanged(CertificateDocumentTypeOption? value)
	{
		if (value != null && !_loadedExistingFee)
		{
			Fee = value.DefaultFee;
		}
		_loadedExistingFee = false;
		OnPropertyChanged("IsBusinessType");
		if (!IsBusinessType)
		{
			BusinessName = string.Empty;
			BusinessNature = string.Empty;
		}
	}

	partial void OnSelectedResidentChanged(OfficialResidentOption? value)
	{
		if (value != null)
		{
			ResidentId = value.ResidentId;
			ResidentName = value.FullName;
		}
	}
}
