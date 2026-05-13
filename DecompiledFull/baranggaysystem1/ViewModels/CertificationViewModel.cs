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
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public class CertificationViewModel : ObservableObject
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveCommand;

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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int? RequestId
	{
		get
		{
			return _requestId;
		}
		set
		{
			if (!EqualityComparer<int?>.Default.Equals(_requestId, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RequestId);
				_requestId = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RequestId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ResidentId
	{
		get
		{
			return _residentId;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_residentId, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResidentId);
				_residentId = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResidentId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ResidentName
	{
		get
		{
			return _residentName;
		}
		[MemberNotNull("_residentName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_residentName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResidentName);
				_residentName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResidentName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public CertificateDocumentTypeOption? SelectedType
	{
		get
		{
			return _selectedType;
		}
		set
		{
			if (!EqualityComparer<CertificateDocumentTypeOption>.Default.Equals(_selectedType, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedType);
				_selectedType = value;
				OnSelectedTypeChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedType);
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
	public string OrNumber
	{
		get
		{
			return _orNumber;
		}
		[MemberNotNull("_orNumber")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_orNumber, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OrNumber);
				_orNumber = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OrNumber);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public decimal Fee
	{
		get
		{
			return _fee;
		}
		set
		{
			if (!EqualityComparer<decimal>.Default.Equals(_fee, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Fee);
				_fee = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Fee);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsProcessing
	{
		get
		{
			return _isProcessing;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isProcessing, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsProcessing);
				_isProcessing = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsProcessing);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string PaymentMethod
	{
		get
		{
			return _paymentMethod;
		}
		[MemberNotNull("_paymentMethod")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_paymentMethod, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PaymentMethod);
				_paymentMethod = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PaymentMethod);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string BusinessName
	{
		get
		{
			return _businessName;
		}
		[MemberNotNull("_businessName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_businessName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BusinessName);
				_businessName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BusinessName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string BusinessNature
	{
		get
		{
			return _businessNature;
		}
		[MemberNotNull("_businessNature")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_businessNature, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BusinessNature);
				_businessNature = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BusinessNature);
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
	public bool ShowResidentPicker
	{
		get
		{
			return _showResidentPicker;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showResidentPicker, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowResidentPicker);
				_showResidentPicker = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowResidentPicker);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public OfficialResidentOption? SelectedResident
	{
		get
		{
			return _selectedResident;
		}
		set
		{
			if (!EqualityComparer<OfficialResidentOption>.Default.Equals(_selectedResident, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedResident);
				_selectedResident = value;
				OnSelectedResidentChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedResident);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public CertificateDialogMode Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (!EqualityComparer<CertificateDialogMode>.Default.Equals(_mode, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Mode);
				_mode = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Mode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string WindowTitle
	{
		get
		{
			return _windowTitle;
		}
		[MemberNotNull("_windowTitle")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_windowTitle, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WindowTitle);
				_windowTitle = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WindowTitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HeaderEyebrow
	{
		get
		{
			return _headerEyebrow;
		}
		[MemberNotNull("_headerEyebrow")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_headerEyebrow, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HeaderEyebrow);
				_headerEyebrow = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HeaderEyebrow);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ActionButtonText
	{
		get
		{
			return _actionButtonText;
		}
		[MemberNotNull("_actionButtonText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_actionButtonText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ActionButtonText);
				_actionButtonText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ActionButtonText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HelperText
	{
		get
		{
			return _helperText;
		}
		[MemberNotNull("_helperText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_helperText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HelperText);
				_helperText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HelperText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LoadingMessage
	{
		get
		{
			return _loadingMessage;
		}
		[MemberNotNull("_loadingMessage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_loadingMessage, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LoadingMessage);
				_loadingMessage = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LoadingMessage);
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
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)Save);
				AsyncRelayCommand val2 = val;
				saveCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
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
		((ObservableObject)this).OnPropertyChanged("ShowPaymentFields");
		((ObservableObject)this).OnPropertyChanged("IsBusinessType");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedTypeChanged(CertificateDocumentTypeOption? value)
	{
		((ObservableObject)this).OnPropertyChanged("IsBusinessType");
		if (value != null)
		{
			if (!_loadedExistingFee && Fee <= 0m && value.DefaultFee > 0m)
			{
				Fee = value.DefaultFee;
			}
			if (!IsBusinessType)
			{
				BusinessName = string.Empty;
				BusinessNature = string.Empty;
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedResidentChanged(OfficialResidentOption? value)
	{
		if (value != null)
		{
			ResidentId = value.ResidentId;
			ResidentName = value.FullName;
		}
	}
}
