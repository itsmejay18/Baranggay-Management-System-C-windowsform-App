using System;


using System.Collections;


using System.Collections.Generic;


using System.ComponentModel;


using System.Data;


using System.Drawing;


using System.Drawing.Printing;


using System.IO;


using System.Linq;


using System.Threading.Tasks;


using System.Security.Cryptography;
using System.Text.RegularExpressions;


using System.Windows.Forms;


using MySql.Data.MySqlClient;


using baranggaysystem1.Database;
using baranggaysystem1.Controls;


using baranggaysystem1.helper;


using QRCoder;





namespace baranggaysystem1;





public partial class ResidentModuleControl : UserControl


{

	private enum ResidentResponsiveMode
	{
		Unknown = 0,
		Wide,
		Medium,
		Narrow
	}

	private static readonly Color ResidentPrimaryBlue = Color.FromArgb(37, 99, 235);
	private static readonly Color ResidentPrimaryBlueHover = Color.FromArgb(29, 78, 216);
	private static readonly Color ResidentPrimaryBluePressed = Color.FromArgb(30, 64, 175);
	private static readonly Color ResidentDangerRed = Color.FromArgb(239, 68, 68);
	private static readonly Color ResidentDangerRedHover = Color.FromArgb(220, 38, 38);
	private static readonly Color ResidentDangerRedPressed = Color.FromArgb(185, 28, 28);

	private readonly ResidentModuleController _controller;
	private readonly Button _residentRestoreButton = new Button();
	private readonly CheckBox _residentShowDeletedToggle = new CheckBox();
	private bool _showDeletedResidents;
	private byte[]? _residentPhotoBytes;
	private int _residentPhotoLoadVersion;
	private readonly Dictionary<int, byte[]?> _residentPhotoCache = new Dictionary<int, byte[]?>();





	private byte[]? _residentPhotoPendingBytes;





	private bool _residentPhotoRemoved;
	private ComboBox _editBarangay = new ComboBox();
	private ComboBox _editPurok = new ComboBox();
	private ComboBox _editHousehold = new ComboBox();
	private DataTable? _residentTable;
	private const int ResidentPageSize = 50;
	private int _residentPageIndex;
	private readonly TableLayoutPanel _residentPagerPanel = new TableLayoutPanel();
	private readonly Button _residentPagePrev = new Button();
	private readonly Button _residentPageNext = new Button();
	private readonly Label _residentPageInfo = new Label();
	private readonly Panel _residentGridHost = new Panel();
	private readonly Panel _residentListLoadingPanel = new Panel();
	private readonly Label _residentListLoadingLabel = new Label();
	private readonly Panel _residentListEmptyPanel = new Panel();
	private readonly Label _residentListEmptyTitle = new Label();
	private readonly Label _residentListEmptyMessage = new Label();
	private readonly Button _residentListAddButton = new Button();
	private readonly Panel _residentStatusPanel = new Panel();
	private readonly Label _residentStatusLabel = new Label();
	private readonly TableLayoutPanel _residentStatusLayout = new TableLayoutPanel();
	private readonly FlowLayoutPanel _residentStatusLegend = new FlowLayoutPanel();
	private readonly Label _residentLegendActive = new Label();
	private readonly Label _residentLegendInactive = new Label();
	private readonly Label _residentLegendDeceased = new Label();
	private readonly TableLayoutPanel _residentSearchLayout = new TableLayoutPanel();
	private readonly TableLayoutPanel _residentActionsLayout = new TableLayoutPanel();
	private readonly Button _residentTopAddButton = new Button();
	private readonly Panel _panelSelectResidentEmpty = new Panel();
	private readonly Label _selectResidentEmptyTitle = new Label();
	private readonly Label _selectResidentEmptyMessage = new Label();
	private bool _residentListLoading;
	private readonly Button _btnResidentAttachments = new Button();
	private bool _residentLocationLoaded;
	private bool _suppressLocationEvents;
	private readonly LoadingOverlay _moduleLoadingOverlay = new LoadingOverlay();
	private int _moduleLoadingDepth;
	private ResidentResponsiveMode _responsiveMode = ResidentResponsiveMode.Unknown;
	private int _responsiveLastWidth;
	private bool _responsiveLayoutQueued;
	private bool _isApplyingResponsiveLayout;
	private readonly Panel _residentInsightPanel = new Panel();
	private readonly Label _residentInsightTitle = new Label();
	private readonly Label _residentInsightBlotter = new Label();
	private readonly Label _residentInsightBlotterActive = new Label();
	private readonly Label _residentInsightCertificates = new Label();
	private readonly Label _residentInsightCertificatesPending = new Label();
	private readonly Label _residentInsightLastAction = new Label();
	private readonly TableLayoutPanel _residentTabSelector = new TableLayoutPanel();
	private readonly Font _residentTabFontRegular = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	private readonly Font _residentTabFontBold = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
	private readonly Button _residentOverviewTabButton = new Button();
	private readonly Button _residentRegistryTabButton = new Button();
	private readonly Button _residentDocumentsTabButton = new Button();
	private readonly Button _residentCasesTabButton = new Button();
	private readonly Button _residentPaymentsTabButton = new Button();
	private readonly Button _residentAuditTabButton = new Button();
	private readonly Panel _residentOverviewTabUnderline = new Panel();
	private readonly Panel _residentRegistryTabUnderline = new Panel();
	private readonly Panel _residentDocumentsTabUnderline = new Panel();
	private readonly Panel _residentCasesTabUnderline = new Panel();
	private readonly Panel _residentPaymentsTabUnderline = new Panel();
	private readonly Panel _residentAuditTabUnderline = new Panel();
	private readonly TabPage _tabPayments = new TabPage();
	private readonly Panel _residentPaymentsContainer = new Panel();
	private readonly Label _residentPaymentsTitle = new Label();
	private readonly Label _residentPaymentsMessage = new Label();
	private string _currentProfileRouteSegment = "overview";
	private bool _suppressRouteChangedEvent;
	private bool _suppressAutoOverviewOnSelection;
	private readonly PictureBox _residentHeaderPhoto = new PictureBox();
	private readonly Label _residentHeaderAddress = new Label();
	private readonly Button _residentHeaderEditButton = new Button();
	private readonly Button _residentHeaderPrintButton = new Button();
	private readonly Button _residentHeaderDeactivateButton = new Button();
	private readonly Button _residentHeaderToggleButton = new Button();
	private readonly ContextMenuStrip _residentHeaderPhotoMenu = new ContextMenuStrip();
	private readonly ToolStripMenuItem _residentHeaderPhotoChange = new ToolStripMenuItem("Change Photo...");
	private readonly ToolStripMenuItem _residentHeaderPhotoRemove = new ToolStripMenuItem("Remove Photo");
	private readonly Panel _residentBottomSummaryHost = new Panel();
	private readonly TableLayoutPanel _residentBottomSummaryLayout = new TableLayoutPanel();
	private readonly Panel _residentRecentActivityPanel = new Panel();
	private readonly Label _residentRecentActivityTitle = new Label();
	private readonly ListView _residentRecentActivityList = new ListView();
	private readonly ColumnHeader _residentRecentActivityDateColumn = new ColumnHeader();
	private readonly ColumnHeader _residentRecentActivityActionColumn = new ColumnHeader();
	private readonly ColumnHeader _residentRecentActivityByColumn = new ColumnHeader();
	private readonly Panel _residentDocumentsToolbar = new Panel();
	private readonly Label _residentDocumentsTitle = new Label();
	private readonly Button _residentDocumentsImportButton = new Button();
	private readonly Label _residentRowsPerPageLabel = new Label();
	private readonly ComboBox _residentRowsPerPageCombo = new ComboBox();
	private readonly Panel _residentDocumentsFooterPanel = new Panel();
	private readonly TableLayoutPanel _residentDocumentsFooterLayout = new TableLayoutPanel();
	private readonly FlowLayoutPanel _residentDocumentsFooterLeft = new FlowLayoutPanel();
	private readonly FlowLayoutPanel _residentDocumentsFooterRight = new FlowLayoutPanel();
	private readonly TableLayoutPanel _profileCompactRoot = new TableLayoutPanel();
	private readonly TableLayoutPanel _profileHeaderBar = new TableLayoutPanel();
	private readonly FlowLayoutPanel _profileHeaderActions = new FlowLayoutPanel();
	private readonly Button _residentMoreDetailsButton = new Button();
	private readonly Label _residentEditModeBadge = new Label();
	private readonly FlowLayoutPanel _residentEditActions = new FlowLayoutPanel();
	private readonly Button _residentEditCancelButton = new Button();
	private readonly Button _residentEditSaveButton = new Button();
	private readonly Label _residentContactValidation = new Label();
	private readonly ComboBox _editGenderCombo = new ComboBox();
	private readonly ComboBox _editCivilCombo = new ComboBox();
	private readonly ComboBox _editStatusCombo = new ComboBox();
	private readonly Label _editPurokLabel = new Label();
	private readonly Label _editHouseholdLabel = new Label();
	private bool _isProfileDetailsExpanded;
	private const float ProfileDetailsExpandedHeight = 210F;
	private string _moreBirthPlace = string.Empty;
	private string _moreCitizenship = string.Empty;
	private string _moreReligion = string.Empty;
	private string _moreOccupation = string.Empty;
	private string _moreEmployer = string.Empty;
	private string _moreEducation = string.Empty;
	private string _morePrograms = string.Empty;
	private string _moreEmail = string.Empty;
	private string _moreNotes = string.Empty;
	private string _morePwdIdNo = string.Empty;
	private string _moreVoterPrecinctNo = string.Empty;
	private bool _moreIsPwd;
	private bool _moreIsSenior;
	private bool _moreIs4Ps;
	private bool _moreIsRegisteredVoter;
	private readonly ContextMenuStrip _certificateActionsMenu = new ContextMenuStrip();
	private readonly ToolStripMenuItem _certificateActionView = new ToolStripMenuItem("View");
	private readonly ToolStripMenuItem _certificateActionDownload = new ToolStripMenuItem("Download");
	private readonly ToolStripMenuItem _certificateActionPrint = new ToolStripMenuItem("Print");
	private readonly ToolStripMenuItem _certificateActionRelease = new ToolStripMenuItem("Release");
	private readonly ToolStripMenuItem _certificateActionReject = new ToolStripMenuItem("Reject");
	private const string CertificateRowNumberColumnName = "_rowNumber";
	private const string CertificateActionsColumnName = "_actions";





	private TabPage[]? _residentTabCache;





	private bool _historyOnlyMode;
	public event EventHandler<ResidentRouteChangedEventArgs>? RouteChanged;

	private int? _residentDetailsLoadedId;
	private int _residentDetailsLoadVersion;
	private int _residentAsyncLoadDepth;
	private bool _legacyProfileCleanupApplied;
	private bool _residentInitialLoadTriggered;
	private bool _residentLoadInProgress;
	private bool _residentReloadPending;
	private int _residentLoadVersion;
	private bool _residentSchemaInitQueued;
	private bool _suppressResidentSelectionChanged;
	private static bool _residentSchemasInitialized;
	private static readonly object ResidentSchemaInitLock = new object();





	private bool _useSidebarTabs = false;





	private DataGridView _certGrid = new DataGridView();





	private Button _btnCertNew = new Button();





	private Button _btnCertEdit = new Button();





	private Button _btnCertApprove = new Button();





	private Button _btnCertIssue = new Button();





	private Button _btnCertCancel = new Button();





	private Button _btnCertPrint = new Button();





	private Button _btnCertExport = new Button();





	private Button _btnCertRefresh = new Button();
	private Button _btnCertAttachments = new Button();





	private TextBox _certSearchBox = new TextBox();





	private ComboBox _certFilterType = new ComboBox();





	private ComboBox _certFilterStatus = new ComboBox();





	private DateTimePicker _certFilterFrom = new DateTimePicker();





	private DateTimePicker _certFilterTo = new DateTimePicker();





	private Button _certFilterClear = new Button();





	private Label _certSummaryTotal = new Label();





	private Label _certSummaryIssued = new Label();





	private Label _certSummaryPending = new Label();





	private Label _certSummaryCancelled = new Label();





	private ComboBox _certType = new ComboBox();





	private TextBox _certPurpose = new TextBox();





	private NumericUpDown _certFee = new NumericUpDown();





	private TextBox _certOR = new TextBox();





	private DateTimePicker _certValidUntil = new DateTimePicker();





	private TextBox _certBusinessName = new TextBox();





	private TextBox _certBusinessNature = new TextBox();





	private TextBox _certRemarks = new TextBox();





	private Label _certNumber = new Label();





	private Label _certStatus = new Label();
	private Label _certSla = new Label();





	private Label _certRequestedAt = new Label();





	private Label _certApprovedAt = new Label();





	private Label _certIssuedAt = new Label();





	private Label _certTypeValue = new Label();





	private Label _certPurposeValue = new Label();





	private Label _certFeeValue = new Label();





	private Label _certOrValue = new Label();





	private Label _certIssuedDateValue = new Label();
	private Label _certValidUntilValue = new Label();
	private Label _certPrintCountValue = new Label();
	private Label _certLastPrintedValue = new Label();
	private Label _certPaymentAmountValue = new Label();
	private Label _certPaymentMethodValue = new Label();
	private Label _certPaymentOrValue = new Label();
	private Label _certPaymentDateValue = new Label();
	private Label _certPaymentReceivedByValue = new Label();





	private Label _certBusinessNameValue = new Label();





	private Label _certBusinessNatureValue = new Label();





	private Label _certRemarksValue = new Label();





	private Label _lblBusinessName = new Label();





	private Label _lblBusinessNature = new Label();





	private Panel _certEmptyPanel = new Panel();





	private Label _certEmptyTitle = new Label();





	private Label _certEmptyMessage = new Label();





	private int? _selectedCertificateId;
	private string _certVerificationToken = string.Empty;





	private bool _isCertEditing;





	private DataTable? _certTable;
	private int _certificatePageSize = 10;
	private int _certPageIndex;
	private readonly FlowLayoutPanel _certPagerPanel = new FlowLayoutPanel();
	private readonly Button _certPagePrev = new Button();
	private readonly Button _certPageNext = new Button();
	private readonly Label _certPageInfo = new Label();





	private DataGridView _blotterGrid = new DataGridView();





	private Button _btnFileBlotter = new Button();





	private Button _btnRefreshBlotter = new Button();

	private Button _btnOpenBlotter = new Button();
	private Button _btnBlotterAttachments = new Button();

	private Panel _blotterCardsHost = new Panel();

	private FlowLayoutPanel _blotterCardsList = new FlowLayoutPanel();

	private List<BlotterRecordSummary> _blotterRecords = new List<BlotterRecordSummary>();

	private Dictionary<int, Panel> _blotterCardViews = new Dictionary<int, Panel>();

	private int? _selectedBlotterId;
	private const int BlotterPageSize = 8;
	private int _blotterPageIndex;
	private readonly FlowLayoutPanel _blotterPagerPanel = new FlowLayoutPanel();
	private readonly Button _blotterPagePrev = new Button();
	private readonly Button _blotterPageNext = new Button();
	private readonly Label _blotterPageInfo = new Label();
	private readonly SplitContainer _casesSplit = new SplitContainer();
	private readonly Panel _casesFilterPanel = new Panel();
	private readonly TableLayoutPanel _casesFilterLayout = new TableLayoutPanel();
	private readonly TextBox _caseSearchBox = new TextBox();
	private readonly ComboBox _caseStatusFilter = new ComboBox();
	private readonly DateTimePicker _caseFromDate = new DateTimePicker();
	private readonly DateTimePicker _caseToDate = new DateTimePicker();
	private readonly TableLayoutPanel _casesPagingLayout = new TableLayoutPanel();
	private readonly Label _casesTitle = new Label();
	private readonly Label _casesIncidentTitle = new Label();
	private readonly Label _casesMeta = new Label();
	private readonly Label _casesStatusBadge = new Label();
	private readonly Button _btnPrintBlotter = new Button();
	private readonly Button _btnCloseBlotter = new Button();
	private readonly TabControl _casesDetailTabs = new TabControl();
	private readonly TextBox _casesOverviewDetails = new TextBox();
	private readonly ListBox _casesOverviewWitnesses = new ListBox();
	private readonly Label _casesOverviewWitnessesEmptyState = new Label();
	private readonly DataGridView _casesTimelineGrid = new DataGridView();
	private readonly ListView _casesAttachmentsList = new ListView();
	private readonly Label _casesAttachmentsEmptyState = new Label();
	private readonly Button _casesAttachmentAdd = new Button();
	private readonly Button _casesAttachmentOpen = new Button();
	private readonly Button _casesAttachmentRemove = new Button();
	private readonly Label _casesFooter = new Label();
	private readonly System.Windows.Forms.Timer _casesSearchDebounce = new System.Windows.Forms.Timer();
	private List<BlotterRecordSummary> _blotterFilteredRecords = new List<BlotterRecordSummary>();
	private bool _blotterLayoutInitialized;
	private bool _blotterSelectionSync;
	private bool _blotterFiltersInitialized;

	private bool _supportsRespondentResidentId;
	private bool _supportsBlotterExtended;





	private Panel _blotterFormPanel = new Panel();





	private TextBox _blotterRespondent = new TextBox();





	private TextBox _blotterIncidentType = new TextBox();





	private DateTimePicker _blotterIncidentDate = new DateTimePicker();





	private TextBox _blotterDetails = new TextBox();





	private ComboBox _blotterStatus = new ComboBox();





	private Button _blotterSave = new Button();





	private Button _blotterCancel = new Button();





	private Panel _blotterEmptyPanel = new Panel();





	private Label _blotterEmptyTitle = new Label();





	private Label _blotterEmptyMessage = new Label();





	private bool _isEditing;
	private bool _suppressEditChangeTracking;
	private bool _residentEditDirty;





	private int? _selectedResidentId;





	private Label _detailName = new Label();





	private Label _detailGender = new Label();





	private Label _detailDob = new Label();





	private Label _detailCivil = new Label();





	private Label _detailContact = new Label();





	private Label _detailStatus = new Label();





	private DataGridView _historyGrid = new DataGridView();





	private DataTable? _historyTable;





	private TextBox _historySearchBox = new TextBox();





	private ComboBox _historyFilterModule = new ComboBox();





	private DateTimePicker _historyFilterFrom = new DateTimePicker();





	private DateTimePicker _historyFilterTo = new DateTimePicker();





	private Button _historyFilterClear = new Button();





	private Label _historySummary = new Label();





	private Panel _historyEmptyPanel = new Panel();





	private Label _historyEmptyTitle = new Label();





	private Label _historyEmptyMessage = new Label();
	private readonly TableLayoutPanel _historyAuditRoot = new TableLayoutPanel();
	private readonly Panel _historyFiltersCard = new Panel();
	private readonly TableLayoutPanel _historyFiltersLayout = new TableLayoutPanel();
	private readonly FlowLayoutPanel _historyQuickButtons = new FlowLayoutPanel();
	private readonly Panel _historyFilterSpacer = new Panel();
	private readonly Label _historyShowingLabel = new Label();
	private readonly TableLayoutPanel _historyListRoot = new TableLayoutPanel();
	private readonly Panel _historyGridHost = new Panel();
	private readonly RichTextBox _historyDetailRichText = new RichTextBox();
	private readonly System.Windows.Forms.Timer _historySearchDebounceTimer = new System.Windows.Forms.Timer();
	private bool _historyLayoutInitialized;




















































































































































































































































































































    public ResidentModuleControl()
    {
        InitializeComponent();
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;
        }

        RemoveLegacyProfileControls();
        ConfigureModuleLoadingOverlay();
        _controller = new ResidentModuleController(this);
        ApplyResidentModuleTheme();
        WireResponsiveLayoutEvents();
        ConfigureGrid();
        Resize += ResidentModuleControl_Resize;
    }

	private void RemoveLegacyProfileControls()
	{
		if (_legacyProfileCleanupApplied)
		{
			return;
		}

		_legacyProfileCleanupApplied = true;

		string[] legacyNames =
		{
			"groupProfile",
			"panelProfile",
			"profilePanel",
			"panelProfileInformation",
			"groupBoxProfile",
			"grpProfileInfo",
			"panelProfileOld",
			"ucProfileInformation",
			"ProfileInformation"
		};

		foreach (string legacyName in legacyNames)
		{
			Control? legacyControl = FindControlRecursive(this, legacyName);
			while (legacyControl != null)
			{
				Control? parent = legacyControl.Parent;
				parent?.Controls.Remove(legacyControl);
				legacyControl.Dispose();
				legacyControl = FindControlRecursive(this, legacyName);
			}
		}

		RemoveLegacyProfileContentFromHost(_tabCertificates, legacyNames);
		RemoveLegacyProfileContentFromHost(_tabHistory, legacyNames);
		RemoveLegacyProfileContentFromHost(datapanel, legacyNames);
		RemoveLegacyProfileContentFromHost(panelRightRoot, legacyNames);
	}

	private static void RemoveLegacyProfileContentFromHost(Control? host, IReadOnlyCollection<string> legacyNames)
	{
		if (host == null)
		{
			return;
		}

		var snapshot = host.Controls.Cast<Control>().ToList();
		foreach (Control child in snapshot)
		{
			bool hasLegacyName = legacyNames.Contains(child.Name, StringComparer.OrdinalIgnoreCase);
			bool hasLegacyText = string.Equals(child.Text?.Trim(), "Profile Information", StringComparison.OrdinalIgnoreCase);
			if (hasLegacyName || hasLegacyText)
			{
				host.Controls.Remove(child);
				child.Dispose();
				continue;
			}

			RemoveLegacyProfileContentFromHost(child, legacyNames);
		}
	}

	private static Control? FindControlRecursive(Control root, string name)
	{
		foreach (Control child in root.Controls)
		{
			if (string.Equals(child.Name, name, StringComparison.Ordinal))
			{
				return child;
			}

			Control? nested = FindControlRecursive(child, name);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	private static bool IsControlDescendantOf(Control control, Control? ancestor)
	{
		if (ancestor == null)
		{
			return false;
		}

		Control? current = control;
		while (current != null)
		{
			if (ReferenceEquals(current, ancestor))
			{
				return true;
			}

			current = current.Parent;
		}

		return false;
	}

	private void WireResponsiveLayoutEvents()
	{
		if (datapanel != null)
		{
			datapanel.SizeChanged -= ResponsiveHost_SizeChanged;
			datapanel.SizeChanged += ResponsiveHost_SizeChanged;
		}

		if (profileContainer != null)
		{
			profileContainer.SizeChanged -= ResponsiveHost_SizeChanged;
			profileContainer.SizeChanged += ResponsiveHost_SizeChanged;
		}

		if (_residentTabs != null)
		{
			_residentTabs.SizeChanged -= ResponsiveHost_SizeChanged;
			_residentTabs.SizeChanged += ResponsiveHost_SizeChanged;
			_residentTabs.SelectedIndexChanged -= ResidentTabs_SelectedIndexChanged;
			_residentTabs.SelectedIndexChanged += ResidentTabs_SelectedIndexChanged;
		}

		if (splitMain != null)
		{
			splitMain.SizeChanged -= ResponsiveHost_SizeChanged;
			splitMain.SizeChanged += ResponsiveHost_SizeChanged;
		}
	}

	private void ResidentTabs_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_residentTabs != null)
		{
			if (ReferenceEquals(_residentTabs.SelectedTab, _tabBlotter))
			{
				_currentProfileRouteSegment = "cases";
			}
			else if (ReferenceEquals(_residentTabs.SelectedTab, _tabCertificates))
			{
				_currentProfileRouteSegment = "documents";
			}
			else if (ReferenceEquals(_residentTabs.SelectedTab, _tabPayments))
			{
				_currentProfileRouteSegment = "payments";
			}
			else if (ReferenceEquals(_residentTabs.SelectedTab, _tabHistory))
			{
				_currentProfileRouteSegment = "activity";
			}
			else if (!string.Equals(_currentProfileRouteSegment, "registry", StringComparison.OrdinalIgnoreCase))
			{
				_currentProfileRouteSegment = "overview";
			}
		}

		UpdateResidentTabSelectionState();
		if (!_suppressRouteChangedEvent)
		{
			RaiseResidentRouteChanged();
		}
	}

	private void ResponsiveHost_SizeChanged(object? sender, EventArgs e)
	{
		if (contentPanel == null)
		{
			return;
		}

		int width = contentPanel.ClientSize.Width;
		if (width <= 0)
		{
			return;
		}

		if (_responsiveMode != ResidentResponsiveMode.Unknown && Math.Abs(width - _responsiveLastWidth) < 24)
		{
			return;
		}

		QueueResponsiveLayoutRefresh();
	}

	private void QueueResponsiveLayoutRefresh()
	{
		if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || _responsiveLayoutQueued || _isApplyingResponsiveLayout || !IsHandleCreated || IsDisposed)
		{
			return;
		}

		_responsiveLayoutQueued = true;
		BeginInvoke(new Action(() =>
		{
			_responsiveLayoutQueued = false;
			if (IsDisposed || _isApplyingResponsiveLayout)
			{
				return;
			}

			ApplyResponsiveDocking();
		}));
	}

	private void ConfigureModuleLoadingOverlay()
	{
		if (Controls.Contains(_moduleLoadingOverlay))
		{
			return;
		}

		_moduleLoadingOverlay.HideLoading();
		Controls.Add(_moduleLoadingOverlay);
		_moduleLoadingOverlay.BringToFront();
	}

	private void BeginModuleLoading(string message)
	{
		if (_moduleLoadingDepth == 0)
		{
			_moduleLoadingOverlay.ShowLoading(message);
			_moduleLoadingOverlay.BringToFront();
			UseWaitCursor = true;
			Cursor = Cursors.WaitCursor;
		}

		_moduleLoadingDepth++;
	}

	private void EndModuleLoading()
	{
		if (_moduleLoadingDepth > 0)
		{
			_moduleLoadingDepth--;
		}

		if (_moduleLoadingDepth != 0)
		{
			return;
		}

		_moduleLoadingOverlay.HideLoading();
		UseWaitCursor = false;
		Cursor = Cursors.Default;
	}

	private void ApplyResidentModuleTheme()
	{
		_useSidebarTabs = false;
		BackColor = Color.FromArgb(244, 246, 249);
		Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
		EnsureUnifiedResidentSurface();
		BuildResidentHeader();
		ArrangeResidentPanel();
		ConfigureProfileDesignerControls();
		ConfigureBlotterDesignerControls();
		ConfigureCertificatesDesignerControls();
		ConfigureHistoryDesignerControls();
		ConfigurePaymentsDesignerControls();
		ConfigureResidentTabSelector();
		ConfigureCertificateActionMenu();
		SetProfileDetailsExpanded(expanded: false);
		SetResidentProfileTab("overview", userInitiated: false, force: true);
		SetTabHeadersVisible(visible: false);
		UiTheme.StandardizeButtonLayout(this);
		UiTheme.SetTabOrder(
			button1,
			_searchBox,
			_searchClear,
			dgvResidents,
			add,
			button3,
			_residentQuickEdit,
			_btnResidentAttachments,
			_btnFileBlotter,
			_btnRefreshBlotter,
			_btnOpenBlotter,
			_btnBlotterAttachments,
			_btnCertNew,
			_btnCertEdit,
			_btnCertApprove,
			_btnCertIssue,
			_btnCertPrint,
			_btnCertExport,
			_btnCertCancel,
			_btnCertRefresh,
			_btnCertAttachments,
			_historyFilterClear,
			_historyExport);
		UiTheme.EnhanceAccessibility(this);
	}




















	private void ResidentModuleControl_Resize(object? sender, EventArgs e)
	{
		ApplyResponsiveDocking();
	}

	private void ApplyResponsiveDocking(bool force = false)
	{
		if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || contentPanel == null || contentPanel.ClientSize.Width <= 0)
		{
			return;
		}
		if (_isApplyingResponsiveLayout && !force)
		{
			return;
		}

		int width = contentPanel.ClientSize.Width;
		// Keep a stable desktop layout to avoid clipping/overlap on medium widths.
		var nextMode = ResidentResponsiveMode.Wide;

		// Re-apply layout if width changed materially even when still in the same mode.
		if (!force && nextMode == _responsiveMode && Math.Abs(width - _responsiveLastWidth) < 24)
		{
			return;
		}

		EnsureUnifiedResidentSurface();

		_responsiveMode = nextMode;
		_responsiveLastWidth = width;
		_isApplyingResponsiveLayout = true;

		contentPanel.SuspendLayout();
		splitMain.Panel1.SuspendLayout();
		splitMain.Panel2.SuspendLayout();
		splitMain.SuspendLayout();
		_listPanel.SuspendLayout();
		datapanel.SuspendLayout();
		profileBody.SuspendLayout();
		certBody.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)historySplit).BeginInit();
		historySplit.Panel1.SuspendLayout();
		historySplit.Panel2.SuspendLayout();

		try
		{
			contentPanel.Padding = new Padding(16);
			// Keep resident list/sidebar always visible across resident tabs.
			bool showResidentList = true;
			_listPanel.Visible = showResidentList;
			if (_residentHeader != null)
			{
				_residentHeader.Visible = true;
			}

			bool usingUnifiedSurface = splitMain.Parent == contentPanel;
			if (usingUnifiedSurface)
			{
				splitMain.Panel1Collapsed = !showResidentList;
				splitMain.FixedPanel = FixedPanel.Panel1;
				splitMain.IsSplitterFixed = false;

				if (showResidentList)
				{
					int suggestedListWidth = 320;

					int hostWidth = splitMain.ClientSize.Width;
					if (hostWidth <= 0)
					{
						hostWidth = contentPanel.ClientSize.Width - contentPanel.Padding.Horizontal;
					}
					if (hostWidth <= 0)
					{
						hostWidth = Width;
					}

					int splitterWidth = Math.Max(1, splitMain.SplitterWidth);
					int minLeft = Math.Max(280, splitMain.Panel1MinSize);
					int maxLeft = hostWidth - splitterWidth - Math.Max(700, splitMain.Panel2MinSize);
					if (maxLeft < minLeft)
					{
						maxLeft = hostWidth - splitterWidth - 320;
						minLeft = Math.Min(minLeft, Math.Max(200, maxLeft));
					}

					if (maxLeft >= 200)
					{
						int lower = Math.Max(200, minLeft);
						int upper = Math.Max(lower, maxLeft);
						int desiredLeft = Math.Clamp(suggestedListWidth, lower, upper);
						desiredLeft = Math.Min(desiredLeft, Math.Max(0, hostWidth - splitterWidth - 1));
						try
						{
							splitMain.SplitterDistance = desiredLeft;
						}
						catch
						{
							// Best effort only. Layout will normalize after the host has a stable size.
						}
					}
				}
			}
			ConfigureProfileResponsiveLayout(nextMode);
			ConfigureCertificateResponsiveLayout(nextMode);
			ConfigureHistoryResponsiveLayout(nextMode);
			ConfigureResidentListResponsiveLayout(nextMode);
		}
		finally
		{
			historySplit.Panel2.ResumeLayout();
			historySplit.Panel1.ResumeLayout();
			((System.ComponentModel.ISupportInitialize)historySplit).EndInit();
			certBody.ResumeLayout();
			profileBody.ResumeLayout();
			datapanel.ResumeLayout();
			_listPanel.ResumeLayout();
			splitMain.Panel2.ResumeLayout();
			splitMain.Panel1.ResumeLayout();
			splitMain.ResumeLayout();
			contentPanel.ResumeLayout();
			_isApplyingResponsiveLayout = false;
		}
	}

	private void EnsureUnifiedResidentSurface()
	{
		if (contentPanel == null || splitMain == null || _listPanel == null || datapanel == null || _residentTabs == null || _residentHeader == null)
		{
			return;
		}

		// Keep Residents in a stable left-list/right-details split.
		splitMain.Orientation = Orientation.Vertical;
		splitMain.Dock = DockStyle.Fill;
		splitMain.BorderStyle = BorderStyle.None;
		splitMain.FixedPanel = FixedPanel.Panel1;
		splitMain.IsSplitterFixed = false;
		splitMain.SplitterWidth = 6;
		try
		{
			splitMain.SplitterDistance = 320;
		}
		catch
		{
			// Ignore if container size is not stable yet.
		}
		try
		{
			splitMain.Panel1MinSize = 280;
			splitMain.Panel2MinSize = 700;
		}
		catch
		{
			// Keep the layout functional even when the host is temporarily smaller than target mins.
			splitMain.Panel1MinSize = 280;
			splitMain.Panel2MinSize = 320;
		}

		if (_listPanel.Parent != splitMain.Panel1)
		{
			_listPanel.Parent?.Controls.Remove(_listPanel);
			splitMain.Panel1.Controls.Add(_listPanel);
		}

		if (panelRightRoot != null && panelRightRoot.Parent != splitMain.Panel2)
		{
			panelRightRoot.Parent?.Controls.Remove(panelRightRoot);
			splitMain.Panel2.Controls.Add(panelRightRoot);
		}

		if (splitMain.Parent != contentPanel)
		{
			splitMain.Parent?.Controls.Remove(splitMain);
			contentPanel.Controls.Add(splitMain);
		}

		// Remove stray legacy controls that can float over the real layout.
		for (int i = contentPanel.Controls.Count - 1; i >= 0; i--)
		{
			Control child = contentPanel.Controls[i];
			if (ReferenceEquals(child, splitMain) || ReferenceEquals(child, _moduleLoadingOverlay))
			{
				continue;
			}

			contentPanel.Controls.RemoveAt(i);
		}

		if (panelRightRoot != null)
		{
			panelRightRoot.Dock = DockStyle.Fill;
			panelRightRoot.Padding = new Padding(16);
			panelRightRoot.Margin = Padding.Empty;
			panelRightRoot.BackColor = Color.FromArgb(244, 246, 249);
		}

		if (panelHeader != null)
		{
			panelHeader.Dock = DockStyle.Fill;
			panelHeader.Margin = new Padding(0, 0, 0, 12);
			panelHeader.BackColor = Color.White;
			panelHeader.Padding = new Padding(0);
			panelHeader.BorderStyle = BorderStyle.None;
		}

		if (panelProfileDetails != null)
		{
			panelProfileDetails.Dock = DockStyle.Fill;
			panelProfileDetails.Margin = new Padding(0, 0, 0, 12);
			panelProfileDetails.Padding = Padding.Empty;
			panelProfileDetails.BackColor = Color.White;
			panelProfileDetails.BorderStyle = BorderStyle.None;
			panelProfileDetails.MinimumSize = new Size(0, 200);

			if (profileContainer != null)
			{
				profileContainer.Dock = DockStyle.Fill;
				if (!ReferenceEquals(profileContainer.Parent, panelProfileDetails))
				{
					profileContainer.Parent?.Controls.Remove(profileContainer);
					panelProfileDetails.Controls.Add(profileContainer);
				}
			}
		}

		EnsureResidentBottomSummaryPanel();

		if (tableBody != null)
		{
			tableBody.Dock = DockStyle.Fill;
			tableBody.Margin = Padding.Empty;
			tableBody.ColumnCount = 1;
			tableBody.RowCount = 5;
			tableBody.ColumnStyles.Clear();
			tableBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableBody.RowStyles.Clear();
			tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
			tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, _isProfileDetailsExpanded ? ProfileDetailsExpandedHeight : 0F));
			tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
			tableBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));

			if (panelHeader != null && !ReferenceEquals(panelHeader.Parent, tableBody))
			{
				panelHeader.Parent?.Controls.Remove(panelHeader);
				tableBody.Controls.Add(panelHeader, 0, 0);
			}

			if (panelProfileDetails != null && !ReferenceEquals(panelProfileDetails.Parent, tableBody))
			{
				panelProfileDetails.Parent?.Controls.Remove(panelProfileDetails);
				tableBody.Controls.Add(panelProfileDetails, 0, 1);
			}

			if (panelTabBarHost != null && !ReferenceEquals(panelTabBarHost.Parent, tableBody))
			{
				panelTabBarHost.Parent?.Controls.Remove(panelTabBarHost);
				tableBody.Controls.Add(panelTabBarHost, 0, 2);
			}

			if (!ReferenceEquals(datapanel.Parent, tableBody))
			{
				datapanel.Parent?.Controls.Remove(datapanel);
				tableBody.Controls.Add(datapanel, 0, 3);
			}

			if (!ReferenceEquals(_residentBottomSummaryHost.Parent, tableBody))
			{
				_residentBottomSummaryHost.Parent?.Controls.Remove(_residentBottomSummaryHost);
				tableBody.Controls.Add(_residentBottomSummaryHost, 0, 4);
			}
		}

		if (panelTabBarHost != null)
		{
			panelTabBarHost.Dock = DockStyle.Fill;
			panelTabBarHost.Margin = Padding.Empty;
			panelTabBarHost.Padding = Padding.Empty;
			panelTabBarHost.BackColor = Color.Transparent;
		}

		_residentHeader.Dock = DockStyle.Fill;
		_residentHeader.Margin = Padding.Empty;
		_residentHeader.Padding = new Padding(16);
		_residentTabs.Dock = DockStyle.Fill;

		_listPanel.Dock = DockStyle.Fill;
		_listPanel.Margin = Padding.Empty;
		_listPanel.Padding = Padding.Empty;
		_listPanel.BackColor = Color.White;
		for (int i = _listPanel.Controls.Count - 1; i >= 0; i--)
		{
			Control child = _listPanel.Controls[i];
			if (!ReferenceEquals(child, tableLeftRoot))
			{
				_listPanel.Controls.RemoveAt(i);
			}
		}
		if (tableLeftRoot != null)
		{
			tableLeftRoot.Dock = DockStyle.Fill;
			tableLeftRoot.Margin = Padding.Empty;
			tableLeftRoot.Padding = new Padding(12);
			EnsureResidentListScaffold();
		}
		if (panelLeftPagerHost != null)
		{
			panelLeftPagerHost.Dock = DockStyle.Fill;
			panelLeftPagerHost.Padding = Padding.Empty;
			panelLeftPagerHost.Margin = Padding.Empty;
			panelLeftPagerHost.AutoScroll = false;
		}
		datapanel.Dock = DockStyle.Fill;
		datapanel.Padding = Padding.Empty;
		datapanel.Margin = Padding.Empty;
		datapanel.BorderStyle = BorderStyle.None;

		_listPanel.Visible = true;
		splitMain.Panel1Collapsed = false;

		contentPanel.Controls.SetChildIndex(splitMain, 0);
		panelHeader?.BringToFront();
		SetProfileDetailsExpanded(_isProfileDetailsExpanded);
	}

	private void SetProfileDetailsExpanded(bool expanded)
	{
		if (tableBody == null || panelProfileDetails == null || tableBody.RowStyles.Count < 2)
		{
			_isProfileDetailsExpanded = expanded;
			return;
		}

		float targetHeight = expanded ? ProfileDetailsExpandedHeight : 0F;
		RowStyle profileRow = tableBody.RowStyles[1];
		if (_isProfileDetailsExpanded == expanded
			&& panelProfileDetails.Visible == expanded
			&& Math.Abs(profileRow.Height - targetHeight) < 0.5f)
		{
			return;
		}

		_isProfileDetailsExpanded = expanded;

		tableBody.SuspendLayout();
		try
		{
			profileRow.SizeType = SizeType.Absolute;
			profileRow.Height = targetHeight;
			panelProfileDetails.Visible = expanded;
			_residentHeaderToggleButton.Text = expanded ? "v" : ">";
			_residentHeaderToggleButton.AccessibleDescription = expanded
				? "Collapse resident profile details"
				: "Expand resident profile details";
		}
		finally
		{
			tableBody.ResumeLayout(performLayout: true);
		}
	}

	private void ResidentHeaderToggleButton_Click(object? sender, EventArgs e)
	{
		SetProfileDetailsExpanded(!_isProfileDetailsExpanded);
	}

	private void ConfigureResidentTabSelector()
	{
		if (panelTabBarHost == null)
		{
			return;
		}

		panelTabBarHost.SuspendLayout();
		try
		{
			panelTabBarHost.Controls.Clear();
			panelTabBarHost.BackColor = Color.Transparent;
			panelTabBarHost.Padding = Padding.Empty;

			_residentTabSelector.SuspendLayout();
			_residentTabSelector.Controls.Clear();
			_residentTabSelector.Dock = DockStyle.Fill;
			_residentTabSelector.Margin = Padding.Empty;
			_residentTabSelector.Padding = Padding.Empty;
			_residentTabSelector.ColumnCount = 6;
			_residentTabSelector.RowCount = 1;
			_residentTabSelector.ColumnStyles.Clear();
			for (int index = 0; index < 6; index++)
			{
				_residentTabSelector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 6F));
			}
			_residentTabSelector.RowStyles.Clear();
			_residentTabSelector.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			ConfigureResidentTabButton(_residentOverviewTabButton, "Overview");
			ConfigureResidentTabButton(_residentRegistryTabButton, "Registry Details");
			ConfigureResidentTabButton(_residentDocumentsTabButton, "Requests & Documents");
			ConfigureResidentTabButton(_residentCasesTabButton, "Cases / Blotter");
			ConfigureResidentTabButton(_residentPaymentsTabButton, "Payments & Fees");
			ConfigureResidentTabButton(_residentAuditTabButton, "Activity Log");

			WireResidentProfileTabButton(_residentOverviewTabButton, "overview");
			WireResidentProfileTabButton(_residentRegistryTabButton, "registry");
			WireResidentProfileTabButton(_residentDocumentsTabButton, "documents");
			WireResidentProfileTabButton(_residentCasesTabButton, "cases");
			WireResidentProfileTabButton(_residentPaymentsTabButton, "payments");
			WireResidentProfileTabButton(_residentAuditTabButton, "activity");

			_residentTabSelector.Controls.Add(BuildResidentTabCell(_residentOverviewTabButton, _residentOverviewTabUnderline), 0, 0);
			_residentTabSelector.Controls.Add(BuildResidentTabCell(_residentRegistryTabButton, _residentRegistryTabUnderline), 1, 0);
			_residentTabSelector.Controls.Add(BuildResidentTabCell(_residentDocumentsTabButton, _residentDocumentsTabUnderline), 2, 0);
			_residentTabSelector.Controls.Add(BuildResidentTabCell(_residentCasesTabButton, _residentCasesTabUnderline), 3, 0);
			_residentTabSelector.Controls.Add(BuildResidentTabCell(_residentPaymentsTabButton, _residentPaymentsTabUnderline), 4, 0);
			_residentTabSelector.Controls.Add(BuildResidentTabCell(_residentAuditTabButton, _residentAuditTabUnderline), 5, 0);

			panelTabBarHost.Controls.Add(_residentTabSelector);
			_residentTabSelector.ResumeLayout(performLayout: true);
		}
		finally
		{
			panelTabBarHost.ResumeLayout(performLayout: true);
		}

		UpdateResidentTabSelectionState();
	}

	private static string NormalizeProfileSegment(string? segment)
	{
		string value = (segment ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
		return value switch
		{
			"overview" => "overview",
			"registry" => "registry",
			"registry-details" => "registry",
			"documents" => "documents",
			"requests" => "documents",
			"requests-documents" => "documents",
			"cases" => "cases",
			"blotter" => "cases",
			"payments" => "payments",
			"fees" => "payments",
			"payments-fees" => "payments",
			"activity" => "activity",
			"logs" => "activity",
			"audit" => "activity",
			_ => "overview"
		};
	}

	private void WireResidentProfileTabButton(Button button, string segment)
	{
		button.Tag = segment;
		button.Click -= ResidentProfileTabButton_Click;
		button.Click += ResidentProfileTabButton_Click;
		button.KeyDown -= ResidentProfileTabButton_KeyDown;
		button.KeyDown += ResidentProfileTabButton_KeyDown;
	}

	private void ResidentProfileTabButton_Click(object? sender, EventArgs e)
	{
		if (sender is not Button button)
		{
			return;
		}

		string segment = Convert.ToString(button.Tag) ?? "overview";
		SetResidentProfileTab(segment, userInitiated: true, force: true);
	}

	private void ResidentProfileTabButton_KeyDown(object? sender, KeyEventArgs e)
	{
		if (sender is not Button current)
		{
			return;
		}

		Button[] tabOrder =
		{
			_residentOverviewTabButton,
			_residentRegistryTabButton,
			_residentDocumentsTabButton,
			_residentCasesTabButton,
			_residentPaymentsTabButton,
			_residentAuditTabButton
		};

		int currentIndex = Array.IndexOf(tabOrder, current);
		if (currentIndex < 0)
		{
			return;
		}

		if (e.KeyCode == Keys.Right && currentIndex < tabOrder.Length - 1)
		{
			tabOrder[currentIndex + 1].Focus();
			e.Handled = true;
		}
		else if (e.KeyCode == Keys.Left && currentIndex > 0)
		{
			tabOrder[currentIndex - 1].Focus();
			e.Handled = true;
		}
	}

	private static void ConfigureResidentTabButton(Button button, string text)
	{
		button.Text = text;
		button.Dock = DockStyle.Fill;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.FlatAppearance.MouseDownBackColor = Color.FromArgb(231, 240, 255);
		button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 245, 255);
		button.BackColor = Color.White;
		button.ForeColor = Color.FromArgb(56, 66, 83);
		button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
		button.Margin = Padding.Empty;
		button.Padding = new Padding(0, 0, 0, 4);
		button.TextAlign = ContentAlignment.MiddleCenter;
	}

	private static Panel BuildResidentTabCell(Button button, Panel underline)
	{
		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 1,
			RowCount = 2
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 3F));

		underline.Dock = DockStyle.Fill;
		underline.Margin = Padding.Empty;
		underline.BackColor = Color.Transparent;

		layout.Controls.Add(button, 0, 0);
		layout.Controls.Add(underline, 0, 1);

		Panel host = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		host.Controls.Add(layout);
		return host;
	}

	private void SetResidentProfileTab(string segment, bool userInitiated, bool force = false)
	{
		if (_residentTabs == null)
		{
			return;
		}

		string normalized = NormalizeProfileSegment(segment);
		string previous = _currentProfileRouteSegment;
		_currentProfileRouteSegment = normalized;

		TabPage targetTab = normalized switch
		{
			"documents" => _tabCertificates,
			"cases" => _tabBlotter,
			"payments" => _tabPayments,
			"activity" => _tabHistory,
			_ => _tabProfile
		};
		bool expandRegistryDetails = normalized == "registry";

		bool previousSuppress = _suppressRouteChangedEvent;
		_suppressRouteChangedEvent = true;
		try
		{
			if (force || !ReferenceEquals(_residentTabs.SelectedTab, targetTab))
			{
				_residentTabs.SelectedTab = targetTab;
			}
		}
		finally
		{
			_suppressRouteChangedEvent = previousSuppress;
		}

		SetProfileDetailsExpanded(expandRegistryDetails);

		if (normalized == "activity" && _selectedResidentId.HasValue)
		{
			LoadResidentHistory(_selectedResidentId.Value);
			UpdateHistoryEmptyState();
		}

		UpdateResidentTabSelectionState();
		if ((userInitiated || !string.Equals(previous, normalized, StringComparison.OrdinalIgnoreCase)) && !_suppressRouteChangedEvent)
		{
			RaiseResidentRouteChanged();
		}
	}

	private void UpdateResidentTabSelectionState()
	{
		string selected = NormalizeProfileSegment(_currentProfileRouteSegment);
		ApplyResidentTabButtonState(_residentOverviewTabButton, _residentOverviewTabUnderline, selected == "overview");
		ApplyResidentTabButtonState(_residentRegistryTabButton, _residentRegistryTabUnderline, selected == "registry");
		ApplyResidentTabButtonState(_residentDocumentsTabButton, _residentDocumentsTabUnderline, selected == "documents");
		ApplyResidentTabButtonState(_residentCasesTabButton, _residentCasesTabUnderline, selected == "cases");
		ApplyResidentTabButtonState(_residentPaymentsTabButton, _residentPaymentsTabUnderline, selected == "payments");
		ApplyResidentTabButtonState(_residentAuditTabButton, _residentAuditTabUnderline, selected == "activity");
	}

	private void ApplyResidentTabButtonState(Button button, Panel underline, bool selected)
	{
		underline.BackColor = selected ? Color.FromArgb(37, 99, 235) : Color.Transparent;
		button.ForeColor = selected ? Color.FromArgb(24, 74, 178) : Color.FromArgb(84, 95, 116);
		button.Font = selected ? _residentTabFontBold : _residentTabFontRegular;
	}

	private void ConfigureProfileResponsiveLayout(ResidentResponsiveMode mode)
	{
		if (profileContainer == null || profileContainer.IsDisposed)
		{
			return;
		}

		profileContainer.SuspendLayout();
		profileBody.SuspendLayout();
		profileInfoTable.SuspendLayout();
		_profileHeaderBar.SuspendLayout();
		_profileCompactRoot.SuspendLayout();
		try
		{
			float photoColumnWidth = _isEditing ? 132F : 0F;
			float footerHeight = _isEditing ? 38F : 0F;

			profileContainer.AutoScroll = false;
			profileContainer.Padding = Padding.Empty;
			profileContainer.Margin = Padding.Empty;
			profileContainer.BackColor = Color.White;

			ConfigureCompactProfileLabel(lblFirstName, "First Name *");
			ConfigureCompactProfileLabel(lblMiddleName, "Middle Name");
			ConfigureCompactProfileLabel(lblLastName, "Last Name *");
			ConfigureCompactProfileLabel(lblGender, "Sex *");
			ConfigureCompactProfileLabel(lblBirthDate, "Birth Date *");
			ConfigureCompactProfileLabel(lblCivilStatus, "Civil Status");
			ConfigureCompactProfileLabel(lblContact, "Contact No");
			ConfigureCompactProfileLabel(lblStatus, "Status");
			ConfigureCompactProfileLabel(_editPurokLabel, "Purok/Sitio");
			ConfigureCompactProfileLabel(_editHouseholdLabel, "Household");

			ConfigureCompactProfileEditor(_editFirstName);
			ConfigureCompactProfileEditor(_editMiddleName);
			ConfigureCompactProfileEditor(_editLastName);
			ConfigureCompactProfileEditor(_editGenderCombo);
			ConfigureCompactProfileEditor(_editCivilCombo);
			ConfigureCompactProfileEditor(_editContact);
			ConfigureCompactProfileEditor(_editStatusCombo);
			ConfigureCompactProfileEditor(_editPurok);
			ConfigureCompactProfileEditor(_editHousehold);
			_editDob.Dock = DockStyle.Fill;
			_editDob.Margin = new Padding(0, 2, 12, 2);
			_editDob.MinimumSize = new Size(0, 28);
			_editDob.Format = DateTimePickerFormat.Short;
			_editDob.Font = UiTheme.BodyFont;

			profileInfoTable.Controls.Clear();
			profileInfoTable.ColumnStyles.Clear();
			profileInfoTable.RowStyles.Clear();
			profileInfoTable.ColumnCount = 4;
			profileInfoTable.RowCount = 5;
			profileInfoTable.Dock = DockStyle.Fill;
			profileInfoTable.Margin = Padding.Empty;
			profileInfoTable.Padding = Padding.Empty;
			profileInfoTable.AutoSize = false;
			profileInfoTable.AutoSizeMode = AutoSizeMode.GrowOnly;
			profileInfoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
			profileInfoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			profileInfoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
			profileInfoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			for (int row = 0; row < 5; row++)
			{
				profileInfoTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
			}

			AddCompactProfileRow(profileInfoTable, 0, lblFirstName, _editFirstName, lblLastName, _editLastName);
			AddCompactProfileRow(profileInfoTable, 1, lblMiddleName, _editMiddleName, lblGender, _editGenderCombo);
			AddCompactProfileRow(profileInfoTable, 2, lblBirthDate, _editDob, lblCivilStatus, _editCivilCombo);
			AddCompactProfileRow(profileInfoTable, 3, lblContact, _editContact, lblStatus, _editStatusCombo);
			AddCompactProfileRow(profileInfoTable, 4, _editPurokLabel, _editPurok, _editHouseholdLabel, _editHousehold);

			profileBody.Controls.Clear();
			profileBody.ColumnStyles.Clear();
			profileBody.RowStyles.Clear();
			profileBody.ColumnCount = 2;
			profileBody.RowCount = 2;
			profileBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			profileBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, photoColumnWidth));
			profileBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
			profileBody.RowStyles.Add(new RowStyle(SizeType.Absolute, footerHeight));
			profileBody.Dock = DockStyle.Fill;
			profileBody.Margin = Padding.Empty;
			profileBody.Padding = Padding.Empty;
			profileBody.AutoSize = false;
			profileBody.Height = 178;
			profileBody.Controls.Add(profileInfoTable, 0, 0);

			profilePhotoPanel.Controls.Clear();
			profilePhotoPanel.Dock = DockStyle.Fill;
			profilePhotoPanel.FlowDirection = FlowDirection.TopDown;
			profilePhotoPanel.WrapContents = false;
			profilePhotoPanel.AutoSize = false;
			profilePhotoPanel.Margin = new Padding(8, 0, 0, 0);
			profilePhotoPanel.Padding = Padding.Empty;
			profilePhotoPanel.Visible = _isEditing;
			profilePhotoPanel.Controls.Add(_residentPhotoCaption);
			profilePhotoPanel.Controls.Add(_residentPhoto);
			profilePhotoPanel.Controls.Add(_residentPhotoUpload);
			profilePhotoPanel.Controls.Add(_residentPhotoRemove);
			profileBody.Controls.Add(profilePhotoPanel, 1, 0);

			TableLayoutPanel footerLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				ColumnCount = 2,
				RowCount = 1
			};
			footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			footerLayout.Controls.Add(_residentContactValidation, 0, 0);
			footerLayout.Controls.Add(_residentEditActions, 1, 0);
			footerLayout.Visible = _isEditing;
			profileBody.Controls.Add(footerLayout, 0, 1);
			profileBody.SetColumnSpan(footerLayout, 2);

			profileActions.Visible = false;
			profileActions.Parent?.Controls.Remove(profileActions);

			_profileHeaderActions.Controls.Clear();
			_profileHeaderActions.Dock = DockStyle.Fill;
			_profileHeaderActions.FlowDirection = FlowDirection.LeftToRight;
			_profileHeaderActions.WrapContents = false;
			_profileHeaderActions.AutoSize = true;
			_profileHeaderActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			_profileHeaderActions.Margin = Padding.Empty;
			_profileHeaderActions.Padding = Padding.Empty;
			_profileHeaderActions.Controls.Add(_residentEditModeBadge);
			_profileHeaderActions.Controls.Add(_residentMoreDetailsButton);

			_profileHeaderBar.Controls.Clear();
			_profileHeaderBar.ColumnStyles.Clear();
			_profileHeaderBar.RowStyles.Clear();
			_profileHeaderBar.ColumnCount = 2;
			_profileHeaderBar.RowCount = 1;
			_profileHeaderBar.Dock = DockStyle.Fill;
			_profileHeaderBar.Margin = Padding.Empty;
			_profileHeaderBar.Padding = Padding.Empty;
			_profileHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_profileHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_profileHeaderBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
			_profileHeaderBar.Controls.Add(profileHeader, 0, 0);
			_profileHeaderBar.Controls.Add(_profileHeaderActions, 1, 0);

			_profileCompactRoot.Controls.Clear();
			_profileCompactRoot.ColumnStyles.Clear();
			_profileCompactRoot.RowStyles.Clear();
			_profileCompactRoot.ColumnCount = 1;
			_profileCompactRoot.RowCount = 2;
			_profileCompactRoot.Dock = DockStyle.Fill;
			_profileCompactRoot.Margin = Padding.Empty;
			_profileCompactRoot.Padding = Padding.Empty;
			_profileCompactRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_profileCompactRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
			_profileCompactRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			_profileCompactRoot.Controls.Add(_profileHeaderBar, 0, 0);
			_profileCompactRoot.Controls.Add(profileBody, 0, 1);
			_detailMessage.Visible = false;

			if (!ReferenceEquals(_profileCompactRoot.Parent, profileContainer))
			{
				_profileCompactRoot.Parent?.Controls.Remove(_profileCompactRoot);
				profileContainer.Controls.Add(_profileCompactRoot);
				_profileCompactRoot.BringToFront();
			}
		}
		finally
		{
			_profileCompactRoot.ResumeLayout(performLayout: true);
			_profileHeaderBar.ResumeLayout(performLayout: true);
			profileInfoTable.ResumeLayout(performLayout: true);
			profileBody.ResumeLayout(performLayout: true);
			profileContainer.ResumeLayout(performLayout: true);
		}

		UpdateResidentEditActionsState();
		ResetProfileViewport();
	}

	private static void ConfigureCompactProfileLabel(Label label, string text)
	{
		label.Text = text;
		label.AutoSize = false;
		label.Dock = DockStyle.Fill;
		label.TextAlign = ContentAlignment.MiddleLeft;
		label.Margin = new Padding(0, 2, 8, 2);
		label.Font = UiTheme.LabelFont;
		label.ForeColor = UiTheme.Slate700;
	}

	private static void ConfigureCompactProfileEditor(Control editor)
	{
		editor.Dock = DockStyle.Fill;
		editor.Margin = new Padding(0, 2, 12, 2);
		editor.MinimumSize = new Size(0, 28);
	}

	private static void AddCompactProfileRow(TableLayoutPanel table, int row, Label leftLabel, Control leftEditor, Label rightLabel, Control rightEditor)
	{
		if (leftLabel.Parent is TableLayoutPanel leftTable)
		{
			leftTable.Controls.Remove(leftLabel);
		}

		if (leftEditor.Parent is TableLayoutPanel leftEditorTable)
		{
			leftEditorTable.Controls.Remove(leftEditor);
		}

		if (rightLabel.Parent is TableLayoutPanel rightTable)
		{
			rightTable.Controls.Remove(rightLabel);
		}

		if (rightEditor.Parent is TableLayoutPanel rightEditorTable)
		{
			rightEditorTable.Controls.Remove(rightEditor);
		}

		table.Controls.Add(leftLabel, 0, row);
		table.Controls.Add(leftEditor, 1, row);
		table.Controls.Add(rightLabel, 2, row);
		table.Controls.Add(rightEditor, 3, row);
	}

	private void ResidentEditChoiceChanged(object? sender, EventArgs e)
	{
		SyncResidentChoiceTextFromEditors();
		ResidentEditFieldChanged(sender, e);
	}

	private void ResidentEditFieldChanged(object? sender, EventArgs e)
	{
		if (_suppressEditChangeTracking)
		{
			return;
		}

		if (_isEditing)
		{
			_residentEditDirty = true;
		}

		UpdateResidentHeader();
		UpdateResidentEditActionsState();
	}

	private void ResidentEditContactChanged(object? sender, EventArgs e)
	{
		UpdateResidentContactValidation(showWhenInvalid: _isEditing);
		ResidentEditFieldChanged(sender, e);
	}

	private void SyncResidentChoiceTextFromEditors()
	{
		_editGender.Text = _editGenderCombo.SelectedItem?.ToString() ?? string.Empty;
		_editCivil.Text = _editCivilCombo.SelectedItem?.ToString() ?? string.Empty;
		_editStatus.Text = _editStatusCombo.SelectedItem?.ToString() ?? string.Empty;
	}

	private void SyncResidentChoiceEditorsFromText()
	{
		bool previous = _suppressEditChangeTracking;
		_suppressEditChangeTracking = true;
		try
		{
			SelectResidentComboText(_editGenderCombo, NormalizeResidentSexEditorValue(_editGender.Text), "M");
			SelectResidentComboText(_editCivilCombo, NormalizeResidentCivilEditorValue(_editCivil.Text), "Single");
			SelectResidentComboText(_editStatusCombo, NormalizeResidentStatusEditorValue(_editStatus.Text), "ACTIVE");
			SyncResidentChoiceTextFromEditors();
		}
		finally
		{
			_suppressEditChangeTracking = previous;
		}
	}

	private static void SelectResidentComboText(ComboBox comboBox, string value, string fallback)
	{
		for (int i = 0; i < comboBox.Items.Count; i++)
		{
			if (string.Equals(comboBox.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
			{
				comboBox.SelectedIndex = i;
				return;
			}
		}

		for (int i = 0; i < comboBox.Items.Count; i++)
		{
			if (string.Equals(comboBox.Items[i]?.ToString(), fallback, StringComparison.OrdinalIgnoreCase))
			{
				comboBox.SelectedIndex = i;
				return;
			}
		}

		comboBox.SelectedIndex = comboBox.Items.Count > 0 ? 0 : -1;
	}

	private static string NormalizeResidentSexEditorValue(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "M";
		}

		return value.Trim().StartsWith("F", StringComparison.OrdinalIgnoreCase) ? "F" : "M";
	}

	private static string NormalizeResidentCivilEditorValue(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Single";
		}

		string trimmed = value.Trim();
		if (trimmed.Equals("Married", StringComparison.OrdinalIgnoreCase))
		{
			return "Married";
		}
		if (trimmed.Equals("Widowed", StringComparison.OrdinalIgnoreCase))
		{
			return "Widowed";
		}
		if (trimmed.Equals("Separated", StringComparison.OrdinalIgnoreCase))
		{
			return "Separated";
		}

		return "Single";
	}

	private static string NormalizeResidentStatusEditorValue(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "ACTIVE";
		}

		string trimmed = value.Trim();
		if (trimmed.Equals("Active", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
		{
			return "ACTIVE";
		}
		if (trimmed.Equals("Deceased", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("DECEASED", StringComparison.OrdinalIgnoreCase))
		{
			return "DECEASED";
		}
		if (trimmed.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("Moved out", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("MOVED_OUT", StringComparison.OrdinalIgnoreCase))
		{
			return "MOVED_OUT";
		}

		return "ACTIVE";
	}

	private static bool IsResidentContactValid(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		string trimmed = value.Trim();
		int digits = trimmed.Count(char.IsDigit);
		if (digits < 7 || digits > 15)
		{
			return false;
		}

		return Regex.IsMatch(trimmed, "^[0-9+\\-()\\s]+$");
	}

	private bool UpdateResidentContactValidation(bool showWhenInvalid)
	{
		bool valid = IsResidentContactValid(_editContact.Text);
		bool show = showWhenInvalid && !valid && !string.IsNullOrWhiteSpace(_editContact.Text.Trim());
		_residentContactValidation.Text = show ? "Invalid contact number format." : string.Empty;
		_residentContactValidation.Visible = show;
		return valid;
	}

	private void UpdateResidentEditActionsState()
	{
		bool hasSelection = IsResidentView() && _selectedResidentId.HasValue;
		bool canEdit = hasSelection && Permissions.CanUpdateResidents && !_showDeletedResidents;
		bool contactValid = UpdateResidentContactValidation(showWhenInvalid: _isEditing);

		_residentEditModeBadge.Visible = _isEditing && hasSelection;
		_residentEditActions.Visible = _isEditing && hasSelection;
		_residentEditCancelButton.Enabled = canEdit && _isEditing;
		_residentEditSaveButton.Enabled = canEdit && _isEditing && _residentEditDirty && contactValid;
		_residentPhotoUpload.Visible = _isEditing;
		_residentPhotoRemove.Visible = _isEditing;
	}

	private void ResetResidentEditDirty()
	{
		_residentEditDirty = false;
		UpdateResidentEditActionsState();
	}

	internal bool TryValidateResidentEditInputs(out string message, out string title)
	{
		SyncResidentChoiceTextFromEditors();

		if (string.IsNullOrWhiteSpace(_editFirstName.Text) || string.IsNullOrWhiteSpace(_editLastName.Text))
		{
			title = "Missing data";
			message = "First name and last name are required.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(_editGender.Text))
		{
			title = "Missing data";
			message = "Sex is required.";
			return false;
		}

		if (_editDob.Value.Date > DateTime.Today)
		{
			title = "Invalid date";
			message = "Birth date cannot be in the future.";
			return false;
		}

		if (!IsResidentContactValid(_editContact.Text))
		{
			UpdateResidentContactValidation(showWhenInvalid: true);
			title = "Invalid contact";
			message = "Please enter a valid contact number.";
			return false;
		}

		title = string.Empty;
		message = string.Empty;
		return true;
	}

	private void ResidentEditSaveButton_Click(object? sender, EventArgs e)
	{
		if (!_isEditing)
		{
			return;
		}

		if (!TryValidateResidentEditInputs(out string message, out string title))
		{
			ControllerDialogs.Warning(message, title);
			UpdateResidentEditActionsState();
			return;
		}

		_controller.HandleQuickEdit(sender, e);
	}

	private void ResidentEditCancelButton_Click(object? sender, EventArgs e)
	{
		CancelResidentEdit();
	}

	private void CancelResidentEdit()
	{
		if (!_isEditing)
		{
			return;
		}

		if (_selectedResidentId.HasValue && TrySelectResidentRow(_selectedResidentId) && dgvResidents.SelectedRows.Count > 0)
		{
			PopulateResidentDetails(dgvResidents.SelectedRows[0]);
			return;
		}

		ExitEditMode();
	}

	private int ResolveProfileAvailableWidth()
	{
		int availableWidth = 0;

		if (profileContainer != null && !profileContainer.IsDisposed)
		{
			availableWidth = profileContainer.ClientSize.Width - profileContainer.Padding.Horizontal;
		}

		if (availableWidth <= 0 && splitMain != null
			&& splitMain.Parent == contentPanel
			&& !splitMain.Panel2Collapsed)
		{
			availableWidth = splitMain.Panel2.ClientSize.Width - datapanel.Padding.Horizontal;
		}

		if (availableWidth <= 0 && datapanel != null)
		{
			availableWidth = datapanel.ClientSize.Width - datapanel.Padding.Horizontal;
		}

		if (availableWidth <= 0 && _tabProfile != null)
		{
			availableWidth = _tabProfile.ClientSize.Width - _tabProfile.Padding.Horizontal;
		}

		if (availableWidth <= 0 && _residentTabs != null)
		{
			availableWidth = _residentTabs.ClientSize.Width - (_residentTabs.Padding.X * 2) - 8;
		}

		if (availableWidth <= 0 && contentPanel != null)
		{
			availableWidth = contentPanel.ClientSize.Width - contentPanel.Padding.Horizontal;
		}

		return Math.Max(0, availableWidth);
	}

	private void ResetProfileViewport()
	{
		if (profileContainer == null || profileContainer.IsDisposed)
		{
			return;
		}

		try
		{
			profileContainer.AutoScrollPosition = Point.Empty;
			if (profileContainer.HorizontalScroll.Maximum > profileContainer.HorizontalScroll.Minimum)
			{
				profileContainer.HorizontalScroll.Value = profileContainer.HorizontalScroll.Minimum;
			}

			if (profileContainer.VerticalScroll.Maximum > profileContainer.VerticalScroll.Minimum)
			{
				profileContainer.VerticalScroll.Value = profileContainer.VerticalScroll.Minimum;
			}
		}
		catch
		{
			// Best effort only. Layout refresh below still recenters controls.
		}

		if (profileHeader != null && profileHeader.Visible)
		{
			profileContainer.ScrollControlIntoView(profileHeader);
		}

		profileBody.Left = 0;
		profileContainer.PerformLayout();
		profileContainer.Invalidate();
	}

	private void SetDetailMessage(string? message)
	{
		if (_detailMessage == null || _detailMessage.IsDisposed)
		{
			return;
		}

		_detailMessage.Text = message ?? string.Empty;
		_detailMessage.Visible = !string.IsNullOrWhiteSpace(_detailMessage.Text);
	}

	private void ConfigureCertificateResponsiveLayout(ResidentResponsiveMode mode)
	{
		certBody.Controls.Clear();
		certBody.ColumnStyles.Clear();
		certBody.RowStyles.Clear();
		certBody.ColumnCount = 1;
		certBody.RowCount = 1;
		certBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		certBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		certGridPanel.Padding = Padding.Empty;
		certBody.Controls.Add(certGridPanel, 0, 0);
		certDetailsPanel.Visible = false;
	}

	private void ConfigureHistoryResponsiveLayout(ResidentResponsiveMode mode)
	{
		_ = mode;
		if (historySplit == null || historySplit.IsDisposed)
		{
			return;
		}

		historySplit.Orientation = Orientation.Vertical;
		historySplit.FixedPanel = FixedPanel.Panel2;
		historyListPanel.Padding = new Padding(0, 0, 8, 0);
		UpdateHistorySplitterDistance();
	}

	private void UpdateHistorySplitterDistance()
	{
		if (historySplit == null || historySplit.IsDisposed || historySplit.Width <= 0)
		{
			return;
		}

		int splitterWidth = Math.Max(4, historySplit.SplitterWidth);
		int minLeft = Math.Max(220, historySplit.Panel1MinSize);
		int minRight = Math.Max(260, historySplit.Panel2MinSize);
		int maxLeft = Math.Max(minLeft, historySplit.Width - minRight - splitterWidth);
		int preferredLeft = (int)Math.Round(historySplit.Width * 0.75);
		int targetLeft = Math.Clamp(preferredLeft, minLeft, maxLeft);

		try
		{
			historySplit.SplitterDistance = targetLeft;
		}
		catch
		{
			// Ignore transient sizing errors while the parent layout is still settling.
		}
	}

	private static void StyleResidentPrimaryButton(Button button, int minWidth = UiTheme.StandardButtonMinWidth)
	{
		button.UseVisualStyleBackColor = false;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.FlatAppearance.MouseOverBackColor = ResidentPrimaryBlueHover;
		button.FlatAppearance.MouseDownBackColor = ResidentPrimaryBluePressed;
		button.BackColor = ResidentPrimaryBlue;
		button.ForeColor = Color.White;
		button.Font = UiTheme.ButtonFont;
		button.AutoSize = true;
		button.AutoEllipsis = false;
		button.MinimumSize = new Size(Math.Max(minWidth, 86), UiTheme.StandardButtonHeight);
		button.Cursor = Cursors.Hand;
	}

	private static void StyleResidentDangerButton(Button button, int minWidth = UiTheme.StandardButtonMinWidth)
	{
		button.UseVisualStyleBackColor = false;
		button.FlatStyle = FlatStyle.Flat;
		button.FlatAppearance.BorderSize = 0;
		button.FlatAppearance.MouseOverBackColor = ResidentDangerRedHover;
		button.FlatAppearance.MouseDownBackColor = ResidentDangerRedPressed;
		button.BackColor = ResidentDangerRed;
		button.ForeColor = Color.White;
		button.Font = UiTheme.ButtonFont;
		button.AutoSize = true;
		button.AutoEllipsis = false;
		button.MinimumSize = new Size(Math.Max(minWidth, 86), UiTheme.StandardButtonHeight);
		button.Cursor = Cursors.Hand;
	}

	private void ConfigureProfileDesignerControls()


	{


		if (_residentHeader != null)


		{


			_residentHeader.BackColor = Color.White;


			_residentHeaderName.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point);


			_residentHeaderName.ForeColor = UiTheme.Slate900;


			_residentHeaderMeta.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);


			_residentHeaderMeta.ForeColor = UiTheme.Slate500;


			_residentHeaderStatus.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);


			_residentHeaderStatus.AutoSize = true;


			_residentHeaderStatus.Padding = new Padding(10, 4, 10, 4);


			_residentHeaderStatus.Margin = new Padding(0, 6, 0, 0);


		}


		if (_residentTabs != null)


		{


			_residentTabs.Dock = DockStyle.Fill;


			_residentTabs.SizeMode = TabSizeMode.Fixed;


			_residentTabs.ItemSize = new Size(120, 32);


			_residentTabs.Appearance = TabAppearance.Normal;


			_residentTabs.Multiline = false;


		}


		PrepareDetailEditors();
		UiTheme.StyleSectionCard(datapanel, UiTheme.Slate50, enforceBorder: false, padding: new Padding(12, 12, 12, 12));
		UiTheme.StyleSectionCard(profileContainer, Color.White, enforceBorder: false, padding: Padding.Empty);
		datapanel.BorderStyle = BorderStyle.None;
		profileContainer.BorderStyle = BorderStyle.None;
		profileContainer.AutoScroll = false;
		profileContainer.HorizontalScroll.Enabled = false;
		profileContainer.HorizontalScroll.Visible = false;
		profileContainer.Margin = Padding.Empty;
		profileBody.Margin = Padding.Empty;
		profileInfoTable.Margin = Padding.Empty;
		profilePhotoPanel.Visible = false;
		profilePhotoButtons.FlowDirection = FlowDirection.LeftToRight;
		profilePhotoButtons.WrapContents = false;
		profilePhotoButtons.AutoSize = true;
		profilePhotoButtons.Padding = Padding.Empty;
		profilePhotoButtons.Margin = new Padding(0, 8, 0, 0);

		profileHeader.Text = "Resident Details";
		profileHeader.Font = UiTheme.HeadingFont;
		profileHeader.ForeColor = UiTheme.Slate900;
		profileHeader.AutoSize = false;
		profileHeader.Dock = DockStyle.Fill;
		profileHeader.TextAlign = ContentAlignment.MiddleLeft;
		profileHeader.Margin = new Padding(0, 0, 8, 0);
		_detailMessage.Font = UiTheme.LabelFont;
		_detailMessage.ForeColor = UiTheme.Slate500;
		_detailMessage.Dock = DockStyle.Fill;
		_detailMessage.Margin = new Padding(0, 4, 0, 6);

		UiTheme.ApplyLabelFont(UiTheme.LabelFont, lblFirstName, lblMiddleName, lblLastName, lblGender, lblBirthDate, lblCivilStatus, lblContact, lblStatus);
		lblFirstName.AutoSize = true;
		lblMiddleName.AutoSize = true;
		lblLastName.AutoSize = true;
		lblGender.AutoSize = true;
		lblBirthDate.AutoSize = true;
		lblCivilStatus.AutoSize = true;
		lblContact.AutoSize = true;
		lblStatus.AutoSize = true;
		lblFirstName.ForeColor = UiTheme.Slate700;
		lblMiddleName.ForeColor = UiTheme.Slate700;
		lblLastName.ForeColor = UiTheme.Slate700;
		lblGender.ForeColor = UiTheme.Slate700;
		lblBirthDate.ForeColor = UiTheme.Slate700;
		lblCivilStatus.ForeColor = UiTheme.Slate700;
		lblContact.ForeColor = UiTheme.Slate700;
		lblStatus.ForeColor = UiTheme.Slate700;
		_editPurokLabel.ForeColor = UiTheme.Slate700;
		_editHouseholdLabel.ForeColor = UiTheme.Slate700;

		ConfigureResidentPickerControls();
		EnsureLocationRows();

		_residentQuickEdit.Text = "Edit Profile";


		StyleResidentPrimaryButton(_residentQuickEdit, 136);

		StyleResidentPrimaryButton(add, 140);
		StyleResidentDangerButton(button3, 92);
		add.Text = "Add Resident";
		button3.Text = "Delete";
		add.AutoSize = true;
		button3.AutoSize = true;
		add.Click -= add_Click;
		add.Click += add_Click;
		button3.Click -= button3_Click;
		button3.Click += button3_Click;

		_residentRestoreButton.Text = "Restore";
		UiTheme.StyleSecondaryButton(_residentRestoreButton);
		_residentRestoreButton.AutoSize = true;
		_residentRestoreButton.Enabled = false;
		_residentRestoreButton.Click -= ResidentRestore_Click;
		_residentRestoreButton.Click += ResidentRestore_Click;

		_residentShowDeletedToggle.Text = "Show deleted";
		_residentShowDeletedToggle.AutoSize = true;
		_residentShowDeletedToggle.Font = UiTheme.LabelFont;
		_residentShowDeletedToggle.ForeColor = UiTheme.Slate700;
		_residentShowDeletedToggle.CheckedChanged -= ResidentShowDeletedToggle_CheckedChanged;
		_residentShowDeletedToggle.CheckedChanged += ResidentShowDeletedToggle_CheckedChanged;

		_residentQuickEdit.Click -= ResidentQuickEdit_Click;
		_residentQuickEdit.Click += ResidentQuickEdit_Click;

		_residentMoreDetailsButton.Text = "More details...";
		UiTheme.StyleSecondaryButton(_residentMoreDetailsButton);
		_residentMoreDetailsButton.AutoSize = false;
		_residentMoreDetailsButton.Size = new Size(132, UiTheme.StandardButtonHeight);
		_residentMoreDetailsButton.Anchor = AnchorStyles.Right;
		_residentMoreDetailsButton.Margin = new Padding(8, 0, 0, 0);
		_residentMoreDetailsButton.Click -= ResidentMoreDetailsButton_Click;
		_residentMoreDetailsButton.Click += ResidentMoreDetailsButton_Click;
		_residentEditModeBadge.Text = "EDITING";
		_residentEditModeBadge.AutoSize = true;
		_residentEditModeBadge.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
		_residentEditModeBadge.ForeColor = Color.FromArgb(29, 78, 216);
		_residentEditModeBadge.BackColor = Color.FromArgb(219, 234, 254);
		_residentEditModeBadge.Padding = new Padding(8, 4, 8, 4);
		_residentEditModeBadge.Margin = new Padding(0, 6, 0, 0);
		_residentEditModeBadge.Visible = false;

		_btnResidentAttachments.Text = "Attachments";
		UiTheme.StyleSecondaryButton(_btnResidentAttachments);
		_btnResidentAttachments.AutoSize = true;
		_btnResidentAttachments.Margin = new Padding(0, 0, 0, 0);
		_btnResidentAttachments.Enabled = false;
		_btnResidentAttachments.Click -= ResidentAttachments_Click;
		_btnResidentAttachments.Click += ResidentAttachments_Click;

		if (profileActions != null)
		{
			profileActions.Controls.Clear();
			profileActions.Controls.Add(_residentMoreDetailsButton);
			profileActions.FlowDirection = FlowDirection.LeftToRight;
			profileActions.WrapContents = false;
			profileActions.AutoSize = false;
			profileActions.Height = 30;
			profileActions.Dock = DockStyle.Fill;
			profileActions.Padding = Padding.Empty;
			profileActions.Margin = Padding.Empty;
		}


		_residentContactValidation.Text = string.Empty;
		_residentContactValidation.Dock = DockStyle.Fill;
		_residentContactValidation.TextAlign = ContentAlignment.MiddleLeft;
		_residentContactValidation.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
		_residentContactValidation.ForeColor = Color.FromArgb(185, 28, 28);
		_residentContactValidation.Margin = new Padding(0, 0, 8, 0);
		_residentContactValidation.Visible = false;

		_residentEditActions.FlowDirection = FlowDirection.LeftToRight;
		_residentEditActions.WrapContents = false;
		_residentEditActions.AutoSize = true;
		_residentEditActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_residentEditActions.Dock = DockStyle.Fill;
		_residentEditActions.Margin = Padding.Empty;
		_residentEditActions.Padding = Padding.Empty;
		_residentEditActions.Controls.Clear();

		_residentPhotoCaption.Text = "Resident photo";


		_residentPhotoCaption.Font = UiTheme.LabelFont;


		_residentPhotoCaption.ForeColor = UiTheme.Slate500;


		_residentPhotoCaption.AutoSize = true;
		_residentPhotoCaption.Margin = new Padding(10, 0, 0, 4);


		_residentPhoto.SizeMode = PictureBoxSizeMode.Zoom;


		_residentPhoto.BorderStyle = BorderStyle.FixedSingle;


		_residentPhoto.BackColor = UiTheme.Slate100;
		_residentPhoto.Size = new Size(90, 90);
		_residentPhoto.MinimumSize = new Size(90, 90);
		_residentPhoto.MaximumSize = new Size(90, 90);
		_residentPhoto.Margin = new Padding(10, 0, 0, 0);


		_residentPhotoUpload.Text = "Upload";


		_residentPhotoRemove.Text = "Remove";


		UiTheme.StyleSecondaryButton(_residentPhotoUpload);
		UiTheme.StyleSecondaryButton(_residentPhotoRemove);
		_residentPhotoUpload.AutoSize = false;
		_residentPhotoRemove.AutoSize = false;
		_residentPhotoUpload.Size = new Size(110, UiTheme.StandardButtonHeight);
		_residentPhotoRemove.Size = new Size(110, UiTheme.StandardButtonHeight);
		_residentPhotoUpload.Margin = new Padding(10, 8, 0, 0);
		_residentPhotoRemove.Margin = new Padding(10, 6, 0, 0);


		_residentPhotoUpload.Click -= ResidentPhotoUpload_Click;


		_residentPhotoUpload.Click += ResidentPhotoUpload_Click;


		_residentPhotoRemove.Click -= ResidentPhotoRemove_Click;


		_residentPhotoRemove.Click += ResidentPhotoRemove_Click;
		_residentEditCancelButton.Text = "Cancel";
		UiTheme.StyleSecondaryButton(_residentEditCancelButton);
		_residentEditCancelButton.AutoSize = false;
		_residentEditCancelButton.Size = new Size(90, UiTheme.StandardButtonHeight);
		_residentEditCancelButton.Margin = new Padding(0, 0, 8, 0);
		_residentEditCancelButton.Click -= ResidentEditCancelButton_Click;
		_residentEditCancelButton.Click += ResidentEditCancelButton_Click;

		_residentEditSaveButton.Text = "Save";
		UiTheme.StylePrimaryButton(_residentEditSaveButton);
		_residentEditSaveButton.AutoSize = false;
		_residentEditSaveButton.Size = new Size(110, UiTheme.StandardButtonHeight);
		_residentEditSaveButton.Margin = Padding.Empty;
		_residentEditSaveButton.Click -= ResidentEditSaveButton_Click;
		_residentEditSaveButton.Click += ResidentEditSaveButton_Click;

		_residentEditActions.Controls.Add(_residentEditCancelButton);
		_residentEditActions.Controls.Add(_residentEditSaveButton);
		SyncResidentChoiceEditorsFromText();
		ResetResidentEditDirty();
		ConfigureResidentInsightPanel();


	}





	private void ConfigureBlotterDesignerControls()
	{
		UiTheme.StyleSectionCard(blotterContainer);
		UiTheme.StyleSectionHeader(_casesTitle, useHeadingFont: true);
		_casesTitle.Text = "Blotter Cases";
		_casesTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point);

		_btnFileBlotter.Text = "New Blotter Case";
		_btnRefreshBlotter.Text = "Refresh";
		_btnOpenBlotter.Text = "Open";
		_btnPrintBlotter.Text = "Print";
		_btnCloseBlotter.Text = "Close Blotter Case";
		_btnBlotterAttachments.Text = "Attachments";
		_btnBlotterAttachments.Visible = false;

		UiTheme.StylePrimaryButton(_btnFileBlotter);
		UiTheme.StyleSecondaryButton(_btnRefreshBlotter);
		UiTheme.StyleSecondaryButton(_btnOpenBlotter);
		UiTheme.StyleSecondaryButton(_btnPrintBlotter);
		UiTheme.StyleSecondaryButton(_btnCloseBlotter);
		UiTheme.StyleSecondaryButton(_btnBlotterAttachments);
		UiTheme.StyleSecondaryButton(_blotterPagePrev);
		UiTheme.StyleSecondaryButton(_blotterPageNext);
		UiTheme.StyleSecondaryButton(_casesAttachmentAdd);
		UiTheme.StyleSecondaryButton(_casesAttachmentOpen);
		UiTheme.StyleSecondaryButton(_casesAttachmentRemove);

		_btnFileBlotter.AutoSize = false;
		_btnRefreshBlotter.AutoSize = false;
		_btnOpenBlotter.AutoSize = false;
		_btnPrintBlotter.AutoSize = false;
		_btnCloseBlotter.AutoSize = false;
		_btnBlotterAttachments.AutoSize = false;
		_btnFileBlotter.Size = new Size(160, 32);
		_btnRefreshBlotter.Size = new Size(100, 32);
		_btnOpenBlotter.Size = new Size(90, 32);
		_btnPrintBlotter.Size = new Size(90, 32);
		_btnCloseBlotter.Size = new Size(190, 32);
		_blotterPagePrev.Size = new Size(90, 34);
		_blotterPageNext.Size = new Size(90, 34);
		_casesAttachmentAdd.Size = new Size(150, 32);
		_casesAttachmentOpen.Size = new Size(90, 32);
		_casesAttachmentRemove.Size = new Size(120, 32);
		_btnFileBlotter.AutoEllipsis = false;
		_btnRefreshBlotter.AutoEllipsis = false;
		_btnOpenBlotter.AutoEllipsis = false;
		_btnPrintBlotter.AutoEllipsis = false;
		_btnCloseBlotter.AutoEllipsis = false;
		_casesAttachmentAdd.AutoEllipsis = false;
		_casesAttachmentOpen.AutoEllipsis = false;
		_casesAttachmentRemove.AutoEllipsis = false;

		_btnFileBlotter.Click -= FileBlotter_Click;
		_btnFileBlotter.Click += FileBlotter_Click;
		_btnRefreshBlotter.Click -= RefreshBlotter_Click;
		_btnRefreshBlotter.Click += RefreshBlotter_Click;
		_btnOpenBlotter.Click -= OpenBlotter_Click;
		_btnOpenBlotter.Click += OpenBlotter_Click;
		_btnBlotterAttachments.Click -= BlotterAttachments_Click;
		_btnBlotterAttachments.Click += BlotterAttachments_Click;
		_btnPrintBlotter.Click -= PrintBlotterCase_Click;
		_btnPrintBlotter.Click += PrintBlotterCase_Click;
		_btnCloseBlotter.Click -= CloseBlotterCase_Click;
		_btnCloseBlotter.Click += CloseBlotterCase_Click;
		_casesAttachmentAdd.Click -= CasesAttachmentManage_Click;
		_casesAttachmentAdd.Click += CasesAttachmentManage_Click;
		_casesAttachmentOpen.Click -= CasesAttachmentManage_Click;
		_casesAttachmentOpen.Click += CasesAttachmentManage_Click;
		_casesAttachmentRemove.Click -= CasesAttachmentManage_Click;
		_casesAttachmentRemove.Click += CasesAttachmentManage_Click;
		_blotterPagePrev.Click -= BlotterPagePrev_Click;
		_blotterPagePrev.Click += BlotterPagePrev_Click;
		_blotterPageNext.Click -= BlotterPageNext_Click;
		_blotterPageNext.Click += BlotterPageNext_Click;

		if (!_blotterLayoutInitialized)
		{
			BuildCasesLayout();
			_blotterLayoutInitialized = true;
		}

		if (!_blotterFiltersInitialized)
		{
			_caseStatusFilter.Items.Clear();
			_caseStatusFilter.Items.AddRange(new object[] { "All", "Open", "Ongoing", "Settled", "Referred", "Closed" });
			_caseStatusFilter.SelectedIndex = 0;
			_caseFromDate.Value = DateTime.Today.AddYears(-10);
			_caseToDate.Value = DateTime.Today;
			_caseSearchBox.TextChanged -= CaseSearchBox_TextChanged;
			_caseSearchBox.TextChanged += CaseSearchBox_TextChanged;
			_caseStatusFilter.SelectedIndexChanged -= BlotterFilterChanged;
			_caseStatusFilter.SelectedIndexChanged += BlotterFilterChanged;
			_caseFromDate.ValueChanged -= BlotterFilterChanged;
			_caseFromDate.ValueChanged += BlotterFilterChanged;
			_caseToDate.ValueChanged -= BlotterFilterChanged;
			_caseToDate.ValueChanged += BlotterFilterChanged;
			_casesSearchDebounce.Interval = 300;
			_casesSearchDebounce.Tick -= CasesSearchDebounce_Tick;
			_casesSearchDebounce.Tick += CasesSearchDebounce_Tick;
			_blotterFiltersInitialized = true;
		}

		ConfigureEmptyStatePanel(_blotterEmptyPanel, _blotterEmptyTitle, _blotterEmptyMessage);
		RenderBlotterCards();
		UpdateBlotterActionState();
		UpdateBlotterEmptyState();
	}

	private void BuildCasesLayout()
	{
		blotterContainer.SuspendLayout();
		try
		{
			ClearAndDisposeControls(blotterContainer);
			blotterContainer.Padding = new Padding(16);

			_casesSplit.Dock = DockStyle.Fill;
			_casesSplit.Orientation = Orientation.Vertical;
			_casesSplit.BorderStyle = BorderStyle.None;
			_casesSplit.FixedPanel = FixedPanel.None;
			_casesSplit.IsSplitterFixed = false;
			_casesSplit.Resize -= CasesSplit_Resize;
			_casesSplit.Resize += CasesSplit_Resize;
			UpdateCasesSplitterDistance();

			TableLayoutPanel leftLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				ColumnCount = 1,
				RowCount = 3
			};
			leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
			leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
			_casesSplit.Panel1.Controls.Clear();
			_casesSplit.Panel1.Controls.Add(leftLayout);

			_casesFilterPanel.Dock = DockStyle.Fill;
			_casesFilterPanel.Padding = Padding.Empty;
			_casesFilterPanel.Margin = Padding.Empty;
			_casesFilterPanel.BackColor = Color.White;

			TableLayoutPanel filterOuter = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				ColumnCount = 1,
				RowCount = 2
			};
			filterOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			filterOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
			filterOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

			_casesTitle.Dock = DockStyle.Fill;
			_casesTitle.Margin = new Padding(0, 0, 0, 4);
			_casesTitle.TextAlign = ContentAlignment.MiddleLeft;

			_casesFilterLayout.Dock = DockStyle.Fill;
			_casesFilterLayout.Margin = Padding.Empty;
			_casesFilterLayout.Padding = new Padding(8, 8, 8, 8);
			_casesFilterLayout.ColumnCount = 7;
			_casesFilterLayout.RowCount = 1;
			_casesFilterLayout.ColumnStyles.Clear();
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
			_casesFilterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
			_casesFilterLayout.RowStyles.Clear();
			_casesFilterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			Label fromLabel = new Label
			{
				Text = "From",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0, 0, 4, 0),
				ForeColor = UiTheme.Slate600
			};

			Label toLabel = new Label
			{
				Text = "To",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0, 0, 4, 0),
				ForeColor = UiTheme.Slate600
			};

			_caseFromDate.Dock = DockStyle.Fill;
			_caseFromDate.Format = DateTimePickerFormat.Short;
			_caseFromDate.Margin = new Padding(0, 0, 6, 0);
			_caseToDate.Dock = DockStyle.Fill;
			_caseToDate.Format = DateTimePickerFormat.Short;
			_caseToDate.Margin = new Padding(0, 0, 6, 0);

			_caseSearchBox.Dock = DockStyle.Fill;
			_caseSearchBox.Margin = new Padding(0, 0, 6, 0);
			_caseSearchBox.MinimumSize = new Size(140, 0);
			_caseSearchBox.PlaceholderText = "Search blotter case (incident, respondent)...";

			_btnFileBlotter.Dock = DockStyle.Fill;
			_btnFileBlotter.Margin = new Padding(0, 0, 6, 0);
			_btnFileBlotter.MinimumSize = new Size(160, 32);
			_btnRefreshBlotter.Dock = DockStyle.Fill;
			_btnRefreshBlotter.Margin = Padding.Empty;
			_btnRefreshBlotter.MinimumSize = new Size(100, 32);

			_caseStatusFilter.Visible = false;
			_casesFilterLayout.Controls.Clear();
			_casesFilterLayout.Controls.Add(fromLabel, 0, 0);
			_casesFilterLayout.Controls.Add(_caseFromDate, 1, 0);
			_casesFilterLayout.Controls.Add(toLabel, 2, 0);
			_casesFilterLayout.Controls.Add(_caseToDate, 3, 0);
			_casesFilterLayout.Controls.Add(_caseSearchBox, 4, 0);
			_casesFilterLayout.Controls.Add(_btnFileBlotter, 5, 0);
			_casesFilterLayout.Controls.Add(_btnRefreshBlotter, 6, 0);

			filterOuter.Controls.Add(_casesTitle, 0, 0);
			filterOuter.Controls.Add(_casesFilterLayout, 0, 1);
			_casesFilterPanel.Controls.Clear();
			_casesFilterPanel.Controls.Add(filterOuter);

			Panel leftGridHost = new Panel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			UiTheme.StyleGridContainer(leftGridHost, padding: Padding.Empty);

			_blotterGrid.Name = "dgvCases";
			_blotterGrid.Dock = DockStyle.Fill;
			_blotterGrid.ReadOnly = true;
			_blotterGrid.AllowUserToAddRows = false;
			_blotterGrid.AllowUserToDeleteRows = false;
			_blotterGrid.AllowUserToResizeRows = false;
			_blotterGrid.MultiSelect = false;
			_blotterGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			_blotterGrid.RowHeadersVisible = false;
			_blotterGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			_blotterGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
			_blotterGrid.BackgroundColor = Color.White;
			_blotterGrid.BorderStyle = BorderStyle.None;
			_blotterGrid.ColumnHeadersHeight = 34;
			_blotterGrid.RowTemplate.Height = 28;
			_blotterGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
			_blotterGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
			_blotterGrid.SelectionChanged -= BlotterGrid_SelectionChanged;
			_blotterGrid.SelectionChanged += BlotterGrid_SelectionChanged;
			_blotterGrid.CellDoubleClick -= BlotterGrid_CellDoubleClick;
			_blotterGrid.CellDoubleClick += BlotterGrid_CellDoubleClick;
			UiTheme.StyleGrid(_blotterGrid);

			_blotterEmptyPanel.Dock = DockStyle.Fill;
			_blotterEmptyPanel.Padding = new Padding(24);
			_blotterEmptyPanel.Controls.Clear();
			_blotterEmptyPanel.Controls.Add(_blotterEmptyTitle);
			_blotterEmptyPanel.Controls.Add(_blotterEmptyMessage);

			leftGridHost.Controls.Add(_blotterGrid);
			leftGridHost.Controls.Add(_blotterEmptyPanel);

			_casesPagingLayout.Dock = DockStyle.Fill;
			_casesPagingLayout.Margin = Padding.Empty;
			_casesPagingLayout.Padding = Padding.Empty;
			_casesPagingLayout.ColumnCount = 3;
			_casesPagingLayout.RowCount = 1;
			_casesPagingLayout.ColumnStyles.Clear();
			_casesPagingLayout.RowStyles.Clear();
			_casesPagingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 204F));
			_casesPagingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_casesPagingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
			_casesPagingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			FlowLayoutPanel pagerButtons = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				Margin = Padding.Empty,
				Padding = new Padding(0, 5, 0, 0)
			};
			_blotterPagePrev.Text = "Prev";
			_blotterPageNext.Text = "Next";
			_blotterPagePrev.Margin = new Padding(0, 0, 8, 0);
			_blotterPageNext.Margin = Padding.Empty;
			pagerButtons.Controls.Add(_blotterPagePrev);
			pagerButtons.Controls.Add(_blotterPageNext);

			_blotterPageInfo.Dock = DockStyle.Fill;
			_blotterPageInfo.TextAlign = ContentAlignment.MiddleCenter;
			_blotterPageInfo.ForeColor = UiTheme.Slate600;
			_blotterPageInfo.Margin = Padding.Empty;

			_casesFooter.Dock = DockStyle.Fill;
			_casesFooter.TextAlign = ContentAlignment.MiddleRight;
			_casesFooter.ForeColor = UiTheme.Slate500;
			_casesFooter.Text = "Showing 0 items";
			_casesFooter.Margin = new Padding(0, 0, 0, 0);

			_casesPagingLayout.Controls.Add(pagerButtons, 0, 0);
			_casesPagingLayout.Controls.Add(_blotterPageInfo, 1, 0);
			_casesPagingLayout.Controls.Add(_casesFooter, 2, 0);

			leftLayout.Controls.Add(_casesFilterPanel, 0, 0);
			leftLayout.Controls.Add(leftGridHost, 0, 1);
			leftLayout.Controls.Add(_casesPagingLayout, 0, 2);

			TableLayoutPanel rightLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				ColumnCount = 1,
				RowCount = 2
			};
			rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
			rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			_casesSplit.Panel2.Controls.Clear();
			_casesSplit.Panel2.Controls.Add(rightLayout);

			Panel detailHeader = new Panel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = new Padding(16, 8, 16, 8),
				BackColor = Color.White
			};
			TableLayoutPanel detailHeaderLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			detailHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			detailHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			detailHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			TableLayoutPanel detailInfo = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 3,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			detailInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			detailInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
			detailInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
			detailInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));

			_casesIncidentTitle.Dock = DockStyle.Fill;
			_casesIncidentTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
			_casesIncidentTitle.ForeColor = UiTheme.Slate900;
			_casesIncidentTitle.TextAlign = ContentAlignment.MiddleLeft;
			_casesIncidentTitle.Text = "Select a blotter case";

			_casesMeta.Dock = DockStyle.Fill;
			_casesMeta.Font = UiTheme.BodyFont;
			_casesMeta.ForeColor = UiTheme.Slate600;
			_casesMeta.TextAlign = ContentAlignment.MiddleLeft;
			_casesMeta.Text = "Respondent: - | Filed: -";

			_casesStatusBadge.AutoSize = true;
			_casesStatusBadge.Padding = new Padding(8, 2, 8, 2);
			_casesStatusBadge.Margin = new Padding(0, 2, 0, 0);
			_casesStatusBadge.Font = UiTheme.SmallFont;
			_casesStatusBadge.MinimumSize = new Size(0, 20);

			FlowLayoutPanel statusFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			statusFlow.Controls.Add(_casesStatusBadge);

			detailInfo.Controls.Add(_casesIncidentTitle, 0, 0);
			detailInfo.Controls.Add(_casesMeta, 0, 1);
			detailInfo.Controls.Add(statusFlow, 0, 2);

			FlowLayoutPanel detailActions = new FlowLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				Anchor = AnchorStyles.Right,
				Margin = new Padding(0, 20, 0, 0),
				Padding = Padding.Empty
			};
			_btnOpenBlotter.Margin = Padding.Empty;
			_btnPrintBlotter.Margin = new Padding(8, 0, 0, 0);
			_btnCloseBlotter.Margin = new Padding(8, 0, 0, 0);
			_btnCloseBlotter.MinimumSize = new Size(190, 32);
			_btnCloseBlotter.Size = new Size(190, 32);
			detailActions.Controls.Add(_btnOpenBlotter);
			detailActions.Controls.Add(_btnPrintBlotter);
			detailActions.Controls.Add(_btnCloseBlotter);

			detailHeaderLayout.Controls.Add(detailInfo, 0, 0);
			detailHeaderLayout.Controls.Add(detailActions, 1, 0);
			detailHeader.Controls.Add(detailHeaderLayout);

			_casesDetailTabs.Dock = DockStyle.Fill;
			_casesDetailTabs.Margin = new Padding(0, 8, 0, 0);
			_casesDetailTabs.Padding = new Point(12, 6);
			_casesDetailTabs.TabPages.Clear();
			_casesDetailTabs.TabPages.Add(BuildCaseOverviewTab());
			_casesDetailTabs.TabPages.Add(BuildCaseTimelineTab());
			_casesDetailTabs.TabPages.Add(BuildCaseAttachmentsTab());

			rightLayout.Controls.Add(detailHeader, 0, 0);
			rightLayout.Controls.Add(_casesDetailTabs, 0, 1);
			blotterContainer.Controls.Add(_casesSplit);
			UpdateCasesSplitterDistance();
		}
		finally
		{
			blotterContainer.ResumeLayout(performLayout: true);
		}
	}

	private void CasesSplit_Resize(object? sender, EventArgs e)
	{
		UpdateCasesSplitterDistance();
	}

	private void UpdateCasesSplitterDistance()
	{
		if (_casesSplit == null || _casesSplit.IsDisposed)
		{
			return;
		}

		int totalWidth = _casesSplit.ClientSize.Width;
		if (totalWidth <= 0 && blotterContainer != null && !blotterContainer.IsDisposed)
		{
			totalWidth = Math.Max(0, blotterContainer.ClientSize.Width - blotterContainer.Padding.Horizontal);
		}
		if (totalWidth <= 0)
		{
			return;
		}

		int splitterWidth = Math.Max(4, _casesSplit.SplitterWidth);
		int availableWidth = Math.Max(0, totalWidth - splitterWidth);
		const int desiredLeftMin = 420;
		const int desiredRightMin = 560;
		const int preferredLeft = 760;

		int minLeft = desiredLeftMin;
		int minRight = desiredRightMin;
		if (availableWidth < (minLeft + minRight))
		{
			// Let narrow hosts keep rendering without splitter exceptions.
			minLeft = 0;
			minRight = 0;
		}

		try
		{
			_casesSplit.Panel1MinSize = minLeft;
			_casesSplit.Panel2MinSize = minRight;
		}
		catch
		{
			// Keep layout resilient while host size is still settling.
			_casesSplit.Panel1MinSize = 0;
			_casesSplit.Panel2MinSize = 0;
		}

		int minDistance = Math.Max(0, _casesSplit.Panel1MinSize);
		int maxDistance = Math.Max(minDistance, totalWidth - splitterWidth - Math.Max(0, _casesSplit.Panel2MinSize));
		int preferredDistance = preferredLeft;
		int targetDistance = Math.Clamp(preferredDistance, minDistance, maxDistance);
		if (_casesSplit.Panel1MinSize == 0 && _casesSplit.Panel2MinSize == 0)
		{
			targetDistance = Math.Clamp(Math.Min(760, Math.Max(220, availableWidth / 2)), 0, Math.Max(0, availableWidth));
		}

		try
		{
			_casesSplit.SplitterDistance = targetDistance;
		}
		catch
		{
			// Ignore transient sizing exceptions; resize/layout pass will re-apply.
		}
	}

	private TabPage BuildCaseOverviewTab()
	{
		TabPage tab = new TabPage("Overview")
		{
			BackColor = Color.White
		};
		TableLayoutPanel overviewLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 1,
			RowCount = 2
		};
		overviewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
		overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

		GroupBox detailsGroup = new GroupBox
		{
			Text = "Blotter Case Details",
			Dock = DockStyle.Fill,
			Padding = new Padding(10),
			Font = UiTheme.LabelFont
		};
		_casesOverviewDetails.Dock = DockStyle.Fill;
		_casesOverviewDetails.Multiline = true;
		_casesOverviewDetails.ReadOnly = true;
		_casesOverviewDetails.ScrollBars = ScrollBars.Vertical;
		detailsGroup.Controls.Add(_casesOverviewDetails);

		GroupBox witnessesGroup = new GroupBox
		{
			Text = "Witnesses",
			Dock = DockStyle.Fill,
			Padding = new Padding(10),
			Font = UiTheme.LabelFont
		};
		Panel witnessesHost = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		_casesOverviewWitnesses.Dock = DockStyle.Fill;
		_casesOverviewWitnesses.BorderStyle = BorderStyle.FixedSingle;
		_casesOverviewWitnesses.HorizontalScrollbar = true;
		_casesOverviewWitnesses.IntegralHeight = false;
		_casesOverviewWitnessesEmptyState.Dock = DockStyle.Fill;
		_casesOverviewWitnessesEmptyState.TextAlign = ContentAlignment.MiddleCenter;
		_casesOverviewWitnessesEmptyState.ForeColor = UiTheme.Slate500;
		_casesOverviewWitnessesEmptyState.Font = UiTheme.BodyFont;
		_casesOverviewWitnessesEmptyState.Text = "No witnesses listed.";
		_casesOverviewWitnessesEmptyState.BackColor = Color.White;
		_casesOverviewWitnessesEmptyState.Visible = true;
		witnessesHost.Controls.Add(_casesOverviewWitnesses);
		witnessesHost.Controls.Add(_casesOverviewWitnessesEmptyState);
		witnessesGroup.Controls.Add(witnessesHost);

		tab.Padding = new Padding(12);
		overviewLayout.Controls.Add(detailsGroup, 0, 0);
		overviewLayout.Controls.Add(witnessesGroup, 0, 1);
		tab.Controls.Add(overviewLayout);
		return tab;
	}

	private static void ClearAndDisposeControls(Control container)
	{
		Control[] existing = container.Controls.Cast<Control>().ToArray();
		foreach (Control child in existing)
		{
			container.Controls.Remove(child);
			child.Dispose();
		}
	}

	private TabPage BuildCaseTimelineTab()
	{
		TabPage tab = new TabPage("Timeline / Logs")
		{
			BackColor = Color.White
		};
		_casesTimelineGrid.Dock = DockStyle.Fill;
		_casesTimelineGrid.ReadOnly = true;
		_casesTimelineGrid.MultiSelect = false;
		_casesTimelineGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_casesTimelineGrid.AllowUserToAddRows = false;
		_casesTimelineGrid.AllowUserToDeleteRows = false;
		_casesTimelineGrid.AllowUserToResizeRows = false;
		_casesTimelineGrid.RowHeadersVisible = false;
		_casesTimelineGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_casesTimelineGrid.ColumnHeadersHeight = 34;
		_casesTimelineGrid.RowTemplate.Height = 28;
		_casesTimelineGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
		UiTheme.StyleGrid(_casesTimelineGrid);
		tab.Padding = new Padding(12);
		tab.Controls.Add(_casesTimelineGrid);
		return tab;
	}

	private TabPage BuildCaseAttachmentsTab()
	{
		TabPage tab = new TabPage("Attachments")
		{
			BackColor = Color.White
		};
		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = new Padding(12),
			ColumnCount = 1,
			RowCount = 2
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		Panel actionBar = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = new Padding(12, 4, 0, 0)
		};
		FlowLayoutPanel actions = new FlowLayoutPanel
		{
			Dock = DockStyle.Left,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		_casesAttachmentAdd.Text = "Add Attachment";
		_casesAttachmentOpen.Text = "Open";
		_casesAttachmentRemove.Text = "Remove";
		_casesAttachmentAdd.Margin = Padding.Empty;
		_casesAttachmentOpen.Margin = new Padding(8, 0, 0, 0);
		_casesAttachmentRemove.Margin = new Padding(8, 0, 0, 0);
		actions.Controls.Add(_casesAttachmentAdd);
		actions.Controls.Add(_casesAttachmentOpen);
		actions.Controls.Add(_casesAttachmentRemove);
		actionBar.Controls.Add(actions);

		_casesAttachmentsList.Dock = DockStyle.Fill;
		_casesAttachmentsList.View = View.Details;
		_casesAttachmentsList.FullRowSelect = true;
		_casesAttachmentsList.MultiSelect = false;
		_casesAttachmentsList.HideSelection = false;
		_casesAttachmentsList.Columns.Clear();
		_casesAttachmentsList.Columns.Add("File Name", 260, HorizontalAlignment.Left);
		_casesAttachmentsList.Columns.Add("Type", 80, HorizontalAlignment.Left);
		_casesAttachmentsList.Columns.Add("Date Added", 150, HorizontalAlignment.Left);
		_casesAttachmentsList.Columns.Add("Added By", 120, HorizontalAlignment.Left);

		_casesAttachmentsEmptyState.Dock = DockStyle.Fill;
		_casesAttachmentsEmptyState.TextAlign = ContentAlignment.MiddleCenter;
		_casesAttachmentsEmptyState.ForeColor = UiTheme.Slate500;
		_casesAttachmentsEmptyState.Font = UiTheme.BodyFont;
		_casesAttachmentsEmptyState.Text = "No attachments for this blotter case yet.";
		_casesAttachmentsEmptyState.BackColor = Color.White;
		_casesAttachmentsEmptyState.Visible = true;

		Panel listHost = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		listHost.Controls.Add(_casesAttachmentsList);
		listHost.Controls.Add(_casesAttachmentsEmptyState);

		layout.Controls.Add(actionBar, 0, 0);
		layout.Controls.Add(listHost, 0, 1);
		tab.Controls.Add(layout);
		return tab;
	}





	private void EnsureResidentDocumentsToolbar()
	{
		if (_residentDocumentsToolbar.Controls.Count == 0)
		{
			_residentDocumentsToolbar.Dock = DockStyle.Top;
			_residentDocumentsToolbar.Height = 56;
			_residentDocumentsToolbar.Margin = new Padding(0, 0, 0, 8);
			_residentDocumentsToolbar.Padding = new Padding(8);
			_residentDocumentsToolbar.BackColor = Color.Transparent;

			TableLayoutPanel toolbarLayout = new TableLayoutPanel
			{
				Name = "tableDocsToolbar",
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				ColumnCount = 2,
				RowCount = 1
			};
			toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			FlowLayoutPanel leftFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				AutoSize = false,
				Margin = Padding.Empty,
				Padding = new Padding(0, 4, 0, 0)
			};

			leftFlow.Controls.Add(_residentDocumentsTitle);
			leftFlow.Controls.Add(_btnCertNew);
			leftFlow.Controls.Add(_residentDocumentsImportButton);
			leftFlow.Controls.Add(_btnCertExport);

			FlowLayoutPanel filterFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = Padding.Empty,
				Padding = new Padding(0, 4, 0, 0)
			};

			filterFlow.Controls.Add(_certSearchBox);
			filterFlow.Controls.Add(_certFilterStatus);

			toolbarLayout.Controls.Add(leftFlow, 0, 0);
			toolbarLayout.Controls.Add(filterFlow, 1, 0);
			_residentDocumentsToolbar.Controls.Add(toolbarLayout);
		}

		_residentDocumentsTitle.Text = "Documents";
		_residentDocumentsTitle.AutoSize = true;
		_residentDocumentsTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
		_residentDocumentsTitle.Font = UiTheme.SectionHeaderFont;
		_residentDocumentsTitle.ForeColor = UiTheme.Slate900;
		_residentDocumentsTitle.Margin = new Padding(0, 7, 12, 0);

		_btnCertNew.Text = "New Request";
		StyleResidentPrimaryButton(_btnCertNew, 140);
		_btnCertNew.AutoSize = false;
		_btnCertNew.Size = new Size(140, UiTheme.StandardButtonHeight);
		_btnCertNew.Margin = new Padding(8, 0, 0, 0);

		_residentDocumentsImportButton.Text = "Import";
		UiTheme.StyleSecondaryButton(_residentDocumentsImportButton);
		_residentDocumentsImportButton.AutoSize = false;
		_residentDocumentsImportButton.Size = new Size(100, UiTheme.StandardButtonHeight);
		_residentDocumentsImportButton.Margin = new Padding(8, 0, 0, 0);
		_residentDocumentsImportButton.Click -= ResidentDocumentsImportButton_Click;
		_residentDocumentsImportButton.Click += ResidentDocumentsImportButton_Click;

		_btnCertExport.Text = "Export";
		UiTheme.StyleSecondaryButton(_btnCertExport);
		_btnCertExport.AutoSize = false;
		_btnCertExport.Size = new Size(100, UiTheme.StandardButtonHeight);
		_btnCertExport.Margin = new Padding(8, 0, 0, 0);

		_certSearchBox.PlaceholderText = "Search documents...";
		_certSearchBox.Width = 220;
		_certSearchBox.Margin = new Padding(0, 2, 8, 0);
		_certFilterStatus.Width = 140;
		_certFilterStatus.Margin = new Padding(0, 2, 0, 0);
		if (string.IsNullOrWhiteSpace(_certFilterStatus.Text))
		{
			_certFilterStatus.Text = "All Status";
		}
	}

	private void ResidentDocumentsImportButton_Click(object? sender, EventArgs e)
	{
		ControllerDialogs.Info("Import is not available yet.", "Documents");
	}

	private void ConfigureCertificatesDesignerControls()


	{


		UiTheme.StyleSectionHeader(certTitle, useHeadingFont: true);
		UiTheme.StyleSectionCard(certContainer);
		UiTheme.StyleSectionHeader(certDetailsHeader, useHeadingFont: true);


		certDataHeader.Font = UiTheme.LabelFont;


		certDataHeader.ForeColor = UiTheme.Slate500;


		UiTheme.StylePrimaryButton(_btnCertNew);
		_btnCertNew.Text = "New";
		_btnCertNew.AutoSize = false;
		UiTheme.StyleSecondaryButton(_btnCertEdit);
		_btnCertEdit.Text = "Edit";
		_btnCertEdit.AutoSize = false;
		UiTheme.StyleSecondaryButton(_btnCertApprove);
		_btnCertApprove.Text = "Approve";
		_btnCertApprove.AutoSize = false;
		UiTheme.StylePrimaryButton(_btnCertIssue);
		_btnCertIssue.Text = "Issue";
		_btnCertIssue.AutoSize = false;
		UiTheme.StyleSecondaryButton(_btnCertPrint);
		_btnCertPrint.Text = "Print";
		_btnCertPrint.AutoSize = false;
		UiTheme.StyleSecondaryButton(_btnCertExport);
		_btnCertExport.Text = "Export";
		_btnCertExport.AutoSize = false;
		UiTheme.StyleDangerButton(_btnCertCancel);
		_btnCertCancel.Text = "Cancel";
		_btnCertCancel.AutoSize = false;
		UiTheme.StyleSecondaryButton(_btnCertRefresh);
		_btnCertRefresh.Text = "Refresh";
		_btnCertRefresh.AutoSize = false;
		UiTheme.StyleSecondaryButton(_btnCertAttachments);
		_btnCertAttachments.Text = "Attachments";
		_btnCertAttachments.AutoSize = false;
		_btnCertAttachments.Enabled = false;


		_btnCertNew.Margin = new Padding(0, 0, 10, 6);


		_btnCertEdit.Margin = new Padding(0, 0, 10, 6);


		_btnCertApprove.Margin = new Padding(0, 0, 10, 6);


		_btnCertIssue.Margin = new Padding(0, 0, 10, 6);


		_btnCertPrint.Margin = new Padding(0, 0, 10, 6);


		_btnCertExport.Margin = new Padding(0, 0, 10, 6);


		_btnCertCancel.Margin = new Padding(0, 0, 10, 6);


		_btnCertRefresh.Margin = new Padding(0, 0, 10, 6);
		_btnCertAttachments.Margin = new Padding(0, 0, 10, 6);


		_btnCertNew.Click -= CertNew_Click;


		_btnCertNew.Click += CertNew_Click;


		_btnCertEdit.Click -= CertEdit_Click;


		_btnCertEdit.Click += CertEdit_Click;


		_btnCertApprove.Click -= CertApprove_Click;


		_btnCertApprove.Click += CertApprove_Click;


		_btnCertIssue.Click -= CertIssue_Click;


		_btnCertIssue.Click += CertIssue_Click;


		_btnCertPrint.Click -= CertPrint_Click;


		_btnCertPrint.Click += CertPrint_Click;


		_btnCertExport.Click -= CertExport_Click;


		_btnCertExport.Click += CertExport_Click;


		_btnCertCancel.Click -= CertCancel_Click;


		_btnCertCancel.Click += CertCancel_Click;


		_btnCertRefresh.Click -= CertRefresh_Click;


		_btnCertRefresh.Click += CertRefresh_Click;
		_btnCertAttachments.Click -= CertAttachments_Click;
		_btnCertAttachments.Click += CertAttachments_Click;

		if (certActions != null)
		{
			// Keep certificates stable and avoid clipped/overlapping controls:
			// hide legacy action strip and drive actions from ribbon/top navigation.
			certActions.Controls.Clear();
			certActions.Visible = false;
			certActions.AutoSize = false;
			certActions.Height = 0;
			certActions.Margin = Padding.Empty;
			certActions.Padding = Padding.Empty;
			if (certContainer != null && certActions.Parent == certContainer)
			{
				certContainer.Controls.Remove(certActions);
			}
		}

		// Ensure legacy runtime certificate controls cannot remain floating over the designer layout.
		foreach (Control legacy in new Control[]
		{
			_btnCertNew, _btnCertEdit, _btnCertApprove, _btnCertIssue, _btnCertPrint,
			_btnCertExport, _btnCertCancel, _btnCertRefresh, _btnCertAttachments,
			_certSearchBox, _certFilterType, _certFilterStatus, _certFilterFrom,
			_certFilterTo, _certFilterClear
		})
		{
			if (legacy.Parent != null
				&& !IsControlDescendantOf(legacy, certContainer)
				&& !IsControlDescendantOf(legacy, certFilters))
			{
				legacy.Parent.Controls.Remove(legacy);
			}
		}


		_certSearchBox.Width = 220;


		_certSearchBox.PlaceholderText = "Search cert # or purpose";


		UiTheme.StyleTextBox(_certSearchBox);


		_certSearchBox.TextChanged -= CertificateFilter_Changed;


		_certSearchBox.TextChanged += CertificateFilter_Changed;


		UiTheme.StyleComboBox(_certFilterType);


		_certFilterType.DropDownStyle = ComboBoxStyle.DropDownList;


		_certFilterType.Items.Clear();


		_certFilterType.Items.Add("All types");


		_certFilterType.Items.AddRange(new object[4] { "Barangay Clearance", "Certificate of Residency", "Indigency", "Business Clearance" });


		_certFilterType.SelectedIndex = 0;


		_certFilterType.SelectedIndexChanged -= CertificateFilter_Changed;


		_certFilterType.SelectedIndexChanged += CertificateFilter_Changed;


		UiTheme.StyleComboBox(_certFilterStatus);


		_certFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;


		_certFilterStatus.Items.Clear();


		_certFilterStatus.Items.AddRange(new object[7] { "All Status", "Requested", "Approved", "Issued", "Cancelled", "Rejected", "Draft" });


		_certFilterStatus.SelectedIndex = 0;


		_certFilterStatus.SelectedIndexChanged -= CertificateFilter_Changed;


		_certFilterStatus.SelectedIndexChanged += CertificateFilter_Changed;


		_certFilterFrom.Format = DateTimePickerFormat.Short;


		_certFilterFrom.ShowCheckBox = true;


		_certFilterFrom.Font = UiTheme.BodyFont;


		_certFilterFrom.Checked = false;


		_certFilterFrom.ValueChanged -= CertificateFilter_Changed;


		_certFilterFrom.ValueChanged += CertificateFilter_Changed;


		_certFilterTo.Format = DateTimePickerFormat.Short;


		_certFilterTo.ShowCheckBox = true;


		_certFilterTo.Font = UiTheme.BodyFont;


		_certFilterTo.Checked = false;


		_certFilterTo.ValueChanged -= CertificateFilter_Changed;


		_certFilterTo.ValueChanged += CertificateFilter_Changed;


		_certFilterClear.Text = "Clear Filters";


		UiTheme.StyleSecondaryButton(_certFilterClear);


		_certFilterClear.Click -= CertFilterClear_Click;


		_certFilterClear.Click += CertFilterClear_Click;


		_certFilterFromLabel.Font = UiTheme.LabelFont;
		_certFilterFromLabel.ForeColor = UiTheme.Slate500;
		_certFilterToLabel.Font = UiTheme.LabelFont;
		_certFilterToLabel.ForeColor = UiTheme.Slate500;

		if (certFilters != null)
		{
			certFilters.SuspendLayout();
			certFilters.WrapContents = false;
			certFilters.FlowDirection = FlowDirection.LeftToRight;
			certFilters.AutoSize = false;
			certFilters.AutoSizeMode = AutoSizeMode.GrowOnly;
			certFilters.AutoScroll = false;
			certFilters.Padding = new Padding(0, 4, 0, 4);
			certFilters.Margin = Padding.Empty;
			certFilters.MinimumSize = new Size(0, 40);
			certFilters.Height = 40;
			certFilters.Dock = DockStyle.Bottom;
			certFilters.BackColor = Color.Transparent;
			certFilters.Controls.Clear();
			certFilters.Visible = false;
			certFilters.ResumeLayout(true);
		}

		EnsureResidentDocumentsToolbar();
		EnsureCertificatePagerControls(certContainer);

		certContainer.SuspendLayout();
		certContainer.Controls.Clear();
		certContainer.BackColor = Color.White;
		certContainer.Padding = new Padding(16);
		certContainer.BorderStyle = BorderStyle.None;
		certContainer.Margin = Padding.Empty;
		certContainer.Controls.Add(certBody);
		certContainer.Controls.Add(_residentDocumentsFooterPanel);
		certContainer.Controls.Add(_residentDocumentsToolbar);
		certContainer.ResumeLayout(true);

		certSummary.Visible = false;
		certTitle.Visible = false;

		_certSearchBox.Visible = true;
		_certFilterType.Visible = false;
		_certFilterStatus.Visible = true;
		_certFilterFrom.Visible = false;
		_certFilterTo.Visible = false;
		_certFilterClear.Visible = false;
		_certFilterFromLabel.Visible = false;
		_certFilterToLabel.Visible = false;


		_certSummaryTotal.Font = UiTheme.LabelFont;


		_certSummaryTotal.ForeColor = UiTheme.Slate500;


		_certSummaryIssued.Font = UiTheme.LabelFont;


		_certSummaryIssued.ForeColor = UiTheme.Slate500;


		_certSummaryPending.Font = UiTheme.LabelFont;


		_certSummaryPending.ForeColor = UiTheme.Slate500;


		_certSummaryCancelled.Font = UiTheme.LabelFont;


		_certSummaryCancelled.ForeColor = UiTheme.Slate500;


		_certGrid.ReadOnly = true;


		_certGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;


		_certGrid.MultiSelect = false;


		_certGrid.AllowUserToAddRows = false;


		_certGrid.AllowUserToDeleteRows = false;

		_certGrid.Dock = DockStyle.Fill;
		_certEmptyPanel.Dock = DockStyle.Fill;
		_certGrid.BackgroundColor = Color.White;
		_certGrid.BorderStyle = BorderStyle.None;
		_certGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
		_certGrid.GridColor = Color.FromArgb(233, 236, 243);
		_certGrid.EnableHeadersVisualStyles = false;
		_certGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
		_certGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 238, 255);
		_certGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(18, 53, 103);

		_certGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_certGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
		_certGrid.ColumnHeadersHeight = 36;
		_certGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
		_certGrid.RowHeadersVisible = false;
		_certGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
		_certGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(17, 24, 39);
		_certGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
		_certGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(17, 24, 39);
		_certGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
		_certGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
		UiTheme.StyleGrid(_certGrid);

		if (certGridPanel != null)
		{
			UiTheme.StyleGridContainer(certGridPanel, _certGrid, new Padding(0));
			certGridPanel.Dock = DockStyle.Fill;
			certGridPanel.Padding = Padding.Empty;
			if (!certGridPanel.Controls.Contains(_certGrid))
			{
				certGridPanel.Controls.Clear();
				certGridPanel.Controls.Add(_certGrid);
				certGridPanel.Controls.Add(_certEmptyPanel);
			}

			_certGrid.Dock = DockStyle.Fill;
			_certEmptyPanel.Dock = DockStyle.Fill;
		}

		certBody.SuspendLayout();
		certBody.Controls.Clear();
		certBody.ColumnStyles.Clear();
		certBody.RowStyles.Clear();
		certBody.ColumnCount = 1;
		certBody.RowCount = 1;
		certBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		certBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		certBody.Controls.Add(certGridPanel, 0, 0);
		certBody.Dock = DockStyle.Fill;
		certBody.Margin = Padding.Empty;
		certBody.ResumeLayout(performLayout: true);

		if (_certEmptyPanel.Controls.Count == 0)
		{
			_certEmptyPanel.Padding = new Padding(24);
			_certEmptyPanel.Controls.Add(_certEmptyTitle);
			_certEmptyPanel.Controls.Add(_certEmptyMessage);
		}


		_certGrid.SelectionChanged -= CertGrid_SelectionChanged;


		_certGrid.SelectionChanged += CertGrid_SelectionChanged;
		_certGrid.CellFormatting -= CertGrid_CellFormatting;
		_certGrid.CellFormatting += CertGrid_CellFormatting;
		_certGrid.CellContentClick -= CertGrid_CellContentClick;
		_certGrid.CellContentClick += CertGrid_CellContentClick;


		PrepareCertificateEditors();


		SetupValueLabel(_certTypeValue);


		SetupValueLabel(_certPurposeValue, 260);


		SetupValueLabel(_certFeeValue);


		SetupValueLabel(_certOrValue);


		SetupValueLabel(_certIssuedDateValue);
		SetupValueLabel(_certValidUntilValue);
		SetupValueLabel(_certPrintCountValue);
		SetupValueLabel(_certLastPrintedValue);
		SetupValueLabel(_certPaymentAmountValue);
		SetupValueLabel(_certPaymentMethodValue);
		SetupValueLabel(_certPaymentOrValue);
		SetupValueLabel(_certPaymentDateValue);
		SetupValueLabel(_certPaymentReceivedByValue);


		SetupValueLabel(_certBusinessNameValue);


		SetupValueLabel(_certBusinessNatureValue);


		SetupValueLabel(_certRemarksValue, 260);


		_certNumber.Font = UiTheme.BodyFont;


		_certNumber.ForeColor = UiTheme.Slate900;


		_certNumber.AutoSize = true;


		_certStatus.Font = UiTheme.BodyFont;


		_certStatus.ForeColor = UiTheme.Slate900;


		_certStatus.AutoSize = true;


		_certStatus.Padding = new Padding(8, 2, 8, 2);


		_certStatus.BackColor = UiTheme.Slate300;


		_certRequestedAt.Font = UiTheme.LabelFont;


		_certRequestedAt.ForeColor = UiTheme.Slate500;


		_certApprovedAt.Font = UiTheme.LabelFont;


		_certApprovedAt.ForeColor = UiTheme.Slate500;


		_certIssuedAt.Font = UiTheme.LabelFont;


		_certIssuedAt.ForeColor = UiTheme.Slate500;


		foreach (Control control3 in certSummaryTable.Controls)


		{


			if (control3 is Label label && label != _certNumber && label != _certStatus && label != _certRequestedAt && label != _certApprovedAt && label != _certIssuedAt)


			{


				label.Font = UiTheme.LabelFont;


				label.ForeColor = UiTheme.Slate500;


			}


		}


		foreach (Control control4 in certDetailTable.Controls)


		{


			if (control4 is Label label2 && label2 != _certTypeValue && label2 != _certPurposeValue && label2 != _certFeeValue && label2 != _certOrValue && label2 != _certIssuedDateValue && label2 != _certValidUntilValue && label2 != _certPrintCountValue && label2 != _certLastPrintedValue && label2 != _certPaymentAmountValue && label2 != _certPaymentMethodValue && label2 != _certPaymentOrValue && label2 != _certPaymentDateValue && label2 != _certPaymentReceivedByValue && label2 != _certBusinessNameValue && label2 != _certBusinessNatureValue && label2 != _certRemarksValue)


			{


				label2.Font = UiTheme.LabelFont;


				label2.ForeColor = UiTheme.Slate500;


			}


		}


		ConfigureEmptyStatePanel(_certEmptyPanel, _certEmptyTitle, _certEmptyMessage);


		ResetCertificateDetails();


		UpdateCertificateActionState();
		UpdateCertificatePagerState();


		UpdateCertificateSummary();


		UpdateCertificateEmptyState();


	}





private void ConfigureHistoryDesignerControls()
{
	EnsureHistoryAuditLayout();

	UiTheme.StyleSectionCard(historyContainer, Color.FromArgb(244, 246, 249), enforceBorder: false, padding: new Padding(12));
	if (historyTitle != null)
	{
		historyTitle.Visible = false;
	}

	_historyFiltersCard.BackColor = Color.White;
	_historyFiltersCard.BorderStyle = BorderStyle.None;

	_historySearchBox.PlaceholderText = "Search history...";
	_historySearchBox.Dock = DockStyle.Fill;
	_historySearchBox.Margin = Padding.Empty;
	_historySearchBox.MinimumSize = new Size(0, 30);
	UiTheme.StyleTextBox(_historySearchBox);
	_historySearchBox.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	_historySearchBox.TextChanged -= HistoryFilter_Changed;
	_historySearchBox.TextChanged -= HistorySearchBox_TextChanged;
	_historySearchBox.TextChanged += HistorySearchBox_TextChanged;
	_historySearchBox.KeyDown -= HistorySearchBox_KeyDown;
	_historySearchBox.KeyDown += HistorySearchBox_KeyDown;

	_historySearchDebounceTimer.Interval = 400;
	_historySearchDebounceTimer.Tick -= HistorySearchDebounceTimer_Tick;
	_historySearchDebounceTimer.Tick += HistorySearchDebounceTimer_Tick;

	UiTheme.StyleComboBox(_historyFilterModule);
	_historyFilterModule.Dock = DockStyle.Fill;
	_historyFilterModule.Margin = Padding.Empty;
	_historyFilterModule.DropDownStyle = ComboBoxStyle.DropDownList;
	_historyFilterModule.Items.Clear();
	_historyFilterModule.Items.AddRange(new object[4] { "All modules", "Residents", "Blotter", "Certificates" });
	_historyFilterModule.SelectedIndex = 0;
	_historyFilterModule.SelectedIndexChanged -= HistoryFilter_Changed;
	_historyFilterModule.SelectedIndexChanged += HistoryFilter_Changed;

	_historyFilterFromLabel.Text = "From";
	_historyFilterFromLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	_historyFilterFromLabel.ForeColor = UiTheme.Slate600;
	_historyFilterFromLabel.Dock = DockStyle.Fill;
	_historyFilterFromLabel.TextAlign = ContentAlignment.MiddleLeft;
	_historyFilterFromLabel.Margin = new Padding(6, 0, 6, 0);

	_historyFilterFrom.Format = DateTimePickerFormat.Short;
	_historyFilterFrom.ShowCheckBox = true;
	_historyFilterFrom.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	_historyFilterFrom.Dock = DockStyle.Fill;
	_historyFilterFrom.Margin = Padding.Empty;
	_historyFilterFrom.ValueChanged -= HistoryFilter_Changed;
	_historyFilterFrom.ValueChanged += HistoryFilter_Changed;

	_historyFilterToLabel.Text = "To";
	_historyFilterToLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	_historyFilterToLabel.ForeColor = UiTheme.Slate600;
	_historyFilterToLabel.Dock = DockStyle.Fill;
	_historyFilterToLabel.TextAlign = ContentAlignment.MiddleLeft;
	_historyFilterToLabel.Margin = new Padding(6, 0, 6, 0);

	_historyFilterTo.Format = DateTimePickerFormat.Short;
	_historyFilterTo.ShowCheckBox = true;
	_historyFilterTo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	_historyFilterTo.Dock = DockStyle.Fill;
	_historyFilterTo.Margin = Padding.Empty;
	_historyFilterTo.ValueChanged -= HistoryFilter_Changed;
	_historyFilterTo.ValueChanged += HistoryFilter_Changed;

	_historyFilterClear.Text = "Clear";
	UiTheme.StyleSecondaryButton(_historyFilterClear);
	_historyFilterClear.AutoSize = false;
	_historyFilterClear.Width = 80;
	_historyFilterClear.Height = 30;
	_historyFilterClear.Dock = DockStyle.Fill;
	_historyFilterClear.Margin = Padding.Empty;
	_historyFilterClear.Click -= HistoryFilterClear_Click;
	_historyFilterClear.Click += HistoryFilterClear_Click;

	_historyExport.Text = "Export";
	UiTheme.StyleSecondaryButton(_historyExport);
	_historyExport.AutoSize = false;
	_historyExport.Width = 90;
	_historyExport.Height = 30;
	_historyExport.Dock = DockStyle.Fill;
	_historyExport.Margin = Padding.Empty;
	_historyExport.Click -= HistoryExport_Click;
	_historyExport.Click += HistoryExport_Click;

	if (_historyFilterQuickLabel != null)
	{
		_historyFilterQuickLabel.Visible = false;
	}

	_historyQuickButtons.WrapContents = false;
	_historyQuickButtons.FlowDirection = FlowDirection.LeftToRight;
	_historyQuickButtons.Dock = DockStyle.Fill;
	_historyQuickButtons.Margin = Padding.Empty;
	_historyQuickButtons.Padding = Padding.Empty;

	_historyQuickToday.Text = "Today";
	UiTheme.StyleSecondaryButton(_historyQuickToday);
	_historyQuickToday.AutoSize = false;
	_historyQuickToday.Width = 70;
	_historyQuickToday.Height = 30;
	_historyQuickToday.Margin = new Padding(0, 0, 8, 0);
	_historyQuickToday.Click -= HistoryQuickToday_Click;
	_historyQuickToday.Click += HistoryQuickToday_Click;

	_historyQuickWeek.Text = "7d";
	UiTheme.StyleSecondaryButton(_historyQuickWeek);
	_historyQuickWeek.AutoSize = false;
	_historyQuickWeek.Width = 70;
	_historyQuickWeek.Height = 30;
	_historyQuickWeek.Margin = new Padding(0, 0, 8, 0);
	_historyQuickWeek.Click -= HistoryQuickWeek_Click;
	_historyQuickWeek.Click += HistoryQuickWeek_Click;

	_historyQuickMonth.Text = "30d";
	UiTheme.StyleSecondaryButton(_historyQuickMonth);
	_historyQuickMonth.AutoSize = false;
	_historyQuickMonth.Width = 70;
	_historyQuickMonth.Height = 30;
	_historyQuickMonth.Margin = Padding.Empty;
	_historyQuickMonth.Click -= HistoryQuickMonth_Click;
	_historyQuickMonth.Click += HistoryQuickMonth_Click;

	if (_historySummary != null)
	{
		_historySummary.Visible = false;
	}

	historySummaryPanel.Dock = DockStyle.Fill;
	historySummaryPanel.AutoSize = false;
	historySummaryPanel.WrapContents = false;
	historySummaryPanel.FlowDirection = FlowDirection.LeftToRight;
	historySummaryPanel.Padding = new Padding(0, 6, 0, 6);
	historySummaryPanel.Margin = Padding.Empty;

	StyleHistorySummaryCard(historySummaryCardTotal, historySummaryTotalValue, historySummaryTotalLabel, "Total", UiTheme.Slate900);
	StyleHistorySummaryCard(historySummaryCardResidents, historySummaryResidentsValue, historySummaryResidentsLabel, "Residents", UiTheme.AccentBlue);
	StyleHistorySummaryCard(historySummaryCardBlotter, historySummaryBlotterValue, historySummaryBlotterLabel, "Blotter", UiTheme.AccentOrange);
	StyleHistorySummaryCard(historySummaryCardCertificates, historySummaryCertificatesValue, historySummaryCertificatesLabel, "Certificates", UiTheme.AccentRed);

	if (historyDetailPanel != null)
	{
		historyDetailPanel.BackColor = Color.White;
		historyDetailPanel.Padding = new Padding(12);
	}

	historyDetailTitle.Text = "Log Details";
	historyDetailTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
	historyDetailTitle.ForeColor = UiTheme.Slate900;

	ConfigureHistoryDetailFieldLabel(historyDetailDateLabel, "Date/Time");
	ConfigureHistoryDetailFieldLabel(historyDetailModuleLabel, "Module");
	ConfigureHistoryDetailFieldLabel(historyDetailActionLabel, "Action");
	ConfigureHistoryDetailFieldLabel(historyDetailByLabel, "By");

	SetupValueLabel(historyDetailDateValue, 220);
	SetupValueLabel(historyDetailModuleValue, 220);
	SetupValueLabel(historyDetailActionValue, 220);
	SetupValueLabel(historyDetailByValue, 220);

	historyDetailTable.SuspendLayout();
	historyDetailTable.Controls.Clear();
	historyDetailTable.ColumnStyles.Clear();
	historyDetailTable.RowStyles.Clear();
	historyDetailTable.ColumnCount = 2;
	historyDetailTable.RowCount = 4;
	historyDetailTable.AutoSize = true;
	historyDetailTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
	historyDetailTable.Dock = DockStyle.Top;
	historyDetailTable.Margin = new Padding(0, 8, 0, 8);
	historyDetailTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
	historyDetailTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
	for (int i = 0; i < 4; i++)
	{
		historyDetailTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
	}
	historyDetailTable.Controls.Add(historyDetailDateLabel, 0, 0);
	historyDetailTable.Controls.Add(historyDetailDateValue, 1, 0);
	historyDetailTable.Controls.Add(historyDetailModuleLabel, 0, 1);
	historyDetailTable.Controls.Add(historyDetailModuleValue, 1, 1);
	historyDetailTable.Controls.Add(historyDetailActionLabel, 0, 2);
	historyDetailTable.Controls.Add(historyDetailActionValue, 1, 2);
	historyDetailTable.Controls.Add(historyDetailByLabel, 0, 3);
	historyDetailTable.Controls.Add(historyDetailByValue, 1, 3);
	historyDetailTable.ResumeLayout(performLayout: true);

	historyDetailDetailsLabel.Text = "Details";
	historyDetailDetailsLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
	historyDetailDetailsLabel.ForeColor = UiTheme.Slate700;
	historyDetailDetailsLabel.Dock = DockStyle.Top;
	historyDetailDetailsLabel.Margin = new Padding(0, 4, 0, 6);

	_historyDetailRichText.Dock = DockStyle.Fill;
	_historyDetailRichText.ReadOnly = true;
	_historyDetailRichText.ScrollBars = RichTextBoxScrollBars.Vertical;
	_historyDetailRichText.BorderStyle = BorderStyle.FixedSingle;
	_historyDetailRichText.BackColor = UiTheme.Slate50;
	_historyDetailRichText.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	_historyDetailRichText.Margin = Padding.Empty;

	if (historyDetailDetails != null)
	{
		historyDetailDetails.Visible = false;
	}

	historyDetailPanel.SuspendLayout();
	historyDetailPanel.Controls.Clear();
	historyDetailPanel.Controls.Add(_historyDetailRichText);
	historyDetailPanel.Controls.Add(historyDetailDetailsLabel);
	historyDetailPanel.Controls.Add(historyDetailTable);
	historyDetailPanel.Controls.Add(historyDetailTitle);
	historyDetailPanel.Controls.Add(historyDetailEmpty);
	historyDetailPanel.ResumeLayout(performLayout: true);

	historyDetailEmpty.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	historyDetailEmpty.ForeColor = UiTheme.Slate500;
	historyDetailEmpty.Dock = DockStyle.Top;
	historyDetailEmpty.Margin = new Padding(0, 8, 0, 0);

	_historyGrid.ReadOnly = true;
	_historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
	_historyGrid.MultiSelect = false;
	_historyGrid.AllowUserToAddRows = false;
	_historyGrid.AllowUserToDeleteRows = false;
	_historyGrid.AllowUserToResizeRows = false;
	_historyGrid.RowHeadersVisible = false;
	_historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
	_historyGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
	_historyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
	_historyGrid.ColumnHeadersHeight = 34;
	_historyGrid.RowTemplate.Height = 28;
	_historyGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
	UiTheme.StyleGrid(_historyGrid);
	_historyGrid.EnableHeadersVisualStyles = false;
	_historyGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(242, 246, 251);
	_historyGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
	_historyGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
	_historyGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
	_historyGrid.GridColor = Color.FromArgb(229, 233, 239);
	_historyGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
	_historyGrid.BorderStyle = BorderStyle.None;
	_historyGrid.Dock = DockStyle.Fill;
	_historyEmptyPanel.Dock = DockStyle.Fill;

	_historyGrid.SelectionChanged -= HistoryGrid_SelectionChanged;
	_historyGrid.SelectionChanged += HistoryGrid_SelectionChanged;

	if (!_historyGridHost.Controls.Contains(_historyGrid))
	{
		_historyGridHost.Controls.Clear();
		_historyGridHost.Controls.Add(_historyGrid);
		_historyGridHost.Controls.Add(_historyEmptyPanel);
	}

	if (_historyEmptyPanel.Controls.Count == 0)
	{
		_historyEmptyPanel.Padding = new Padding(24);
		_historyEmptyPanel.Controls.Add(_historyEmptyTitle);
		_historyEmptyPanel.Controls.Add(_historyEmptyMessage);
	}

	ConfigureEmptyStatePanel(_historyEmptyPanel, _historyEmptyTitle, _historyEmptyMessage);
	UpdateHistorySplitterDistance();
	UpdateHistorySummary();
	UpdateHistoryEmptyState();
	UpdateHistoryDetail();
}

private void ConfigurePaymentsDesignerControls()
{
	if (_residentTabs == null)
	{
		return;
	}

	_tabPayments.Name = "_tabPayments";
	_tabPayments.Text = "Payments";
	_tabPayments.Padding = new Padding(16, 12, 16, 16);
	_tabPayments.UseVisualStyleBackColor = true;
	_tabPayments.BackColor = Color.White;

	if (!_residentTabs.TabPages.Contains(_tabPayments))
	{
		_residentTabs.TabPages.Add(_tabPayments);
	}

	if (_residentPaymentsContainer.Parent != null && !ReferenceEquals(_residentPaymentsContainer.Parent, _tabPayments))
	{
		_residentPaymentsContainer.Parent.Controls.Remove(_residentPaymentsContainer);
	}

	if (_residentPaymentsContainer.Controls.Count == 0)
	{
		_residentPaymentsContainer.Dock = DockStyle.Fill;
		_residentPaymentsContainer.Padding = new Padding(16);
		_residentPaymentsContainer.BackColor = Color.White;

		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));

		Panel card = new Panel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Padding = new Padding(20),
			BackColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle,
			Anchor = AnchorStyles.None
		};

		FlowLayoutPanel textStack = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};

		_residentPaymentsTitle.AutoSize = true;
		_residentPaymentsTitle.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
		_residentPaymentsTitle.ForeColor = UiTheme.Slate900;
		_residentPaymentsTitle.Margin = new Padding(0, 0, 0, 8);
		_residentPaymentsTitle.Text = "Payments & Fees";

		_residentPaymentsMessage.AutoSize = true;
		_residentPaymentsMessage.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
		_residentPaymentsMessage.ForeColor = UiTheme.Slate500;
		_residentPaymentsMessage.Margin = Padding.Empty;
		_residentPaymentsMessage.MaximumSize = new Size(520, 0);
		_residentPaymentsMessage.Text = "No payment records yet. Select a resident and use this tab for fees, billing, and payment history.";

		textStack.Controls.Add(_residentPaymentsTitle);
		textStack.Controls.Add(_residentPaymentsMessage);
		card.Controls.Add(textStack);
		layout.Controls.Add(card, 0, 1);
		_residentPaymentsContainer.Controls.Add(layout);
	}

	if (!ReferenceEquals(_residentPaymentsContainer.Parent, _tabPayments))
	{
		_tabPayments.Controls.Clear();
		_tabPayments.Controls.Add(_residentPaymentsContainer);
	}
}

private void StyleHistorySummaryCard(Panel card, Label valueLabel, Label captionLabel, string caption, Color accent)
{
	if (card == null || valueLabel == null || captionLabel == null)
	{
		return;
	}

	card.SuspendLayout();
	try
	{
		card.Controls.Clear();
		card.AutoSize = true;
		card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		card.Height = 28;
		card.Padding = new Padding(10, 4, 10, 4);
		card.Margin = new Padding(0, 0, 8, 0);
		card.BorderStyle = BorderStyle.FixedSingle;
		card.BackColor = Color.White;

		FlowLayoutPanel chipFlow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			AutoSize = true
		};

		captionLabel.Text = $"{caption}:";
		captionLabel.AutoSize = true;
		captionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
		captionLabel.ForeColor = UiTheme.Slate600;
		captionLabel.Margin = new Padding(0, 2, 4, 0);

		valueLabel.AutoSize = true;
		valueLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
		valueLabel.ForeColor = accent;
		valueLabel.Margin = new Padding(0, 1, 0, 0);

		chipFlow.Controls.Add(captionLabel);
		chipFlow.Controls.Add(valueLabel);
		card.Controls.Add(chipFlow);
	}
	finally
	{
		card.ResumeLayout(performLayout: true);
	}
}

private void ConfigureHistoryDetailFieldLabel(Label label, string text)
{
	label.Text = text;
	label.AutoSize = true;
	label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
	label.ForeColor = UiTheme.Slate600;
	label.Margin = new Padding(0, 0, 8, 4);
}

private void HistorySearchBox_TextChanged(object? sender, EventArgs e)
{
	_ = sender;
	_historySearchDebounceTimer.Stop();
	_historySearchDebounceTimer.Start();
}

private void HistorySearchBox_KeyDown(object? sender, KeyEventArgs e)
{
	if (e.KeyCode != Keys.Enter)
	{
		return;
	}

	e.Handled = true;
	e.SuppressKeyPress = true;
	_historySearchDebounceTimer.Stop();
	ApplyHistoryFilters();
}

private void HistorySearchDebounceTimer_Tick(object? sender, EventArgs e)
{
	_ = sender;
	_historySearchDebounceTimer.Stop();
	ApplyHistoryFilters();
}

private void EnsureHistoryAuditLayout()
{
	if (historyContainer == null || historyContainer.IsDisposed)
	{
		return;
	}

	if (!_historyLayoutInitialized)
	{
		_historyAuditRoot.Name = "tlpAuditRoot";
		_historyAuditRoot.Dock = DockStyle.Fill;
		_historyAuditRoot.Padding = new Padding(12);
		_historyAuditRoot.Margin = Padding.Empty;
		_historyAuditRoot.ColumnCount = 1;
		_historyAuditRoot.RowCount = 3;
		_historyAuditRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_historyAuditRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
		_historyAuditRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
		_historyAuditRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		_historyFiltersCard.Name = "pnlFilters";
		_historyFiltersCard.Dock = DockStyle.Fill;
		_historyFiltersCard.Padding = new Padding(10);
		_historyFiltersCard.Margin = Padding.Empty;

		_historyFiltersLayout.Name = "tlpFilters";
		_historyFiltersLayout.Dock = DockStyle.Fill;
		_historyFiltersLayout.Margin = Padding.Empty;
		_historyFiltersLayout.Padding = Padding.Empty;
		_historyFiltersLayout.RowCount = 1;
		_historyFiltersLayout.ColumnCount = 10;
		_historyFiltersLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
		_historyFiltersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));

		_historyFilterSpacer.Dock = DockStyle.Fill;
		_historyFilterSpacer.Margin = Padding.Empty;

		_historyQuickButtons.Name = "flpQuick";
		_historyQuickButtons.Controls.Clear();
		_historyQuickButtons.Controls.Add(_historyQuickToday);
		_historyQuickButtons.Controls.Add(_historyQuickWeek);
		_historyQuickButtons.Controls.Add(_historyQuickMonth);

		_historyFiltersLayout.Controls.Clear();
		_historyFiltersLayout.Controls.Add(_historySearchBox, 0, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterModule, 1, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterFromLabel, 2, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterFrom, 3, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterToLabel, 4, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterTo, 5, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterSpacer, 6, 0);
		_historyFiltersLayout.Controls.Add(_historyQuickButtons, 7, 0);
		_historyFiltersLayout.Controls.Add(_historyFilterClear, 8, 0);
		_historyFiltersLayout.Controls.Add(_historyExport, 9, 0);
		_historyFiltersCard.Controls.Add(_historyFiltersLayout);

		historySummaryPanel.Name = "flpSummary";
		historySummaryPanel.Controls.Clear();
		historySummaryPanel.Controls.Add(historySummaryCardTotal);
		historySummaryPanel.Controls.Add(historySummaryCardResidents);
		historySummaryPanel.Controls.Add(historySummaryCardBlotter);
		historySummaryPanel.Controls.Add(historySummaryCardCertificates);

		historySplit.Dock = DockStyle.Fill;
		historySplit.Orientation = Orientation.Vertical;
		historySplit.FixedPanel = FixedPanel.Panel2;
		historySplit.Panel1MinSize = 280;
		historySplit.Panel2MinSize = 280;
		historySplit.SplitterWidth = 6;
		historySplit.Resize -= HistorySplit_Resize;
		historySplit.Resize += HistorySplit_Resize;

		_historyListRoot.Dock = DockStyle.Fill;
		_historyListRoot.Margin = Padding.Empty;
		_historyListRoot.Padding = Padding.Empty;
		_historyListRoot.ColumnCount = 1;
		_historyListRoot.RowCount = 2;
		_historyListRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_historyListRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_historyListRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));

		_historyGridHost.Dock = DockStyle.Fill;
		_historyGridHost.Margin = Padding.Empty;
		_historyGridHost.Padding = Padding.Empty;
		_historyGridHost.BackColor = Color.White;

		_historyShowingLabel.Dock = DockStyle.Fill;
		_historyShowingLabel.Margin = new Padding(0, 4, 0, 0);
		_historyShowingLabel.TextAlign = ContentAlignment.MiddleLeft;
		_historyShowingLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
		_historyShowingLabel.ForeColor = UiTheme.Slate500;
		_historyShowingLabel.Text = "Showing 0 items";

		_historyListRoot.Controls.Clear();
		_historyListRoot.Controls.Add(_historyGridHost, 0, 0);
		_historyListRoot.Controls.Add(_historyShowingLabel, 0, 1);

		historyListPanel.Controls.Clear();
		historyListPanel.Padding = Padding.Empty;
		historyListPanel.Controls.Add(_historyListRoot);

		historyContainer.Controls.Clear();
		historyContainer.Controls.Add(_historyAuditRoot);
		_historyAuditRoot.Controls.Add(_historyFiltersCard, 0, 0);
		_historyAuditRoot.Controls.Add(historySummaryPanel, 0, 1);
		_historyAuditRoot.Controls.Add(historySplit, 0, 2);

		_historyLayoutInitialized = true;
	}
}

private void HistorySplit_Resize(object? sender, EventArgs e)
{
	_ = sender;
	UpdateHistorySplitterDistance();
}

private void HistoryGrid_SelectionChanged(object? sender, EventArgs e)
{
    UpdateHistoryDetail();
}

private void UpdateHistoryDetail()
{
    if (historyDetailTable == null || historyDetailEmpty == null)
    {
        return;
    }

    if (_historyGrid == null || _historyGrid.CurrentRow == null || _historyGrid.CurrentRow.DataBoundItem == null)
    {
        historyDetailTable.Visible = false;
        historyDetailEmpty.Visible = true;
        historyDetailDateValue.Text = "-";
        historyDetailModuleValue.Text = "-";
        historyDetailActionValue.Text = "-";
        historyDetailByValue.Text = "-";
        historyDetailDetailsLabel.Visible = false;
        _historyDetailRichText.Visible = false;
        _historyDetailRichText.Text = string.Empty;
        return;
    }

    if (_historyGrid.CurrentRow.DataBoundItem is DataRowView view)
    {
        historyDetailDateValue.Text = FormatHistoryDate(view["action_at"]);
        historyDetailModuleValue.Text = SafeHistoryValue(view["module"]);
        historyDetailActionValue.Text = SafeHistoryValue(view["action"]);
        historyDetailByValue.Text = SafeHistoryValue(view["action_by"]);
        _historyDetailRichText.Text = SafeHistoryValue(view["details"]);
    }

    historyDetailTable.Visible = true;
    historyDetailDetailsLabel.Visible = true;
    _historyDetailRichText.Visible = true;
    historyDetailEmpty.Visible = false;
}

private static string SafeHistoryValue(object? value)
{
    if (value == null || value == DBNull.Value)
    {
        return "-";
    }
    string text = value.ToString() ?? "-";
    return string.IsNullOrWhiteSpace(text) ? "-" : text;
}

private static string FormatHistoryDate(object? value)
{
    if (value == null || value == DBNull.Value)
    {
        return "-";
    }

    if (value is DateTime dateTime)
    {
        return dateTime.ToString("MMM dd, yyyy h:mm tt");
    }

    if (DateTime.TryParse(value.ToString(), out DateTime parsed))
    {
        return parsed.ToString("MMM dd, yyyy h:mm tt");
    }

    return value.ToString() ?? "-";
}

private void HistoryQuickToday_Click(object? sender, EventArgs e)
{
    ApplyHistoryQuickRange(0);
}

private void HistoryQuickWeek_Click(object? sender, EventArgs e)
{
    ApplyHistoryQuickRange(7);
}

private void HistoryQuickMonth_Click(object? sender, EventArgs e)
{
    ApplyHistoryQuickRange(30);
}

private void ApplyHistoryQuickRange(int days)
{
    DateTime today = DateTime.Today;
    _historyFilterFrom.Checked = true;
    _historyFilterTo.Checked = true;

    if (days <= 0)
    {
        _historyFilterFrom.Value = today;
        _historyFilterTo.Value = today;
    }
    else
    {
        _historyFilterFrom.Value = today.AddDays(-(days - 1));
        _historyFilterTo.Value = today;
    }

    ApplyHistoryFilters();
}

private void HistoryExport_Click(object? sender, EventArgs e)
{
    if (_historyTable == null || _historyTable.DefaultView.Count == 0)
    {
        ControllerDialogs.Info("No history to export.", "Export");
        return;
    }

    using SaveFileDialog dialog = new SaveFileDialog
    {
        Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
        FileName = $"history_{DateTime.Now:yyyyMMdd}.csv",
        Title = "Export History"
    };

    if (dialog.ShowDialog() != DialogResult.OK)
    {
        return;
    }

    try
    {
        ExportHistoryCsv(dialog.FileName);
        ControllerDialogs.Info("History exported successfully.", "Export");
    }
    catch (Exception ex)
    {
        ControllerDialogs.Error(ex, "Unable to export history.", "Export");
    }
}

private void ExportHistoryCsv(string path)
{
    if (_historyTable == null)
    {
        return;
    }

    var view = _historyTable.DefaultView;
    using StreamWriter writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
    writer.WriteLine("Date,Module,Action,Details,By");
    foreach (DataRowView row in view)
    {
        string date = FormatHistoryDate(row["action_at"]);
        string module = SafeHistoryValue(row["module"]);
        string action = SafeHistoryValue(row["action"]);
        string details = SafeHistoryValue(row["details"]);
        string by = SafeHistoryValue(row["action_by"]);
        writer.WriteLine($"{EscapeCsv(date)},{EscapeCsv(module)},{EscapeCsv(action)},{EscapeCsv(details)},{EscapeCsv(by)}");
    }
}

private static string EscapeCsv(string value)
{
    if (value.Contains("\"") || value.Contains(",") || value.Contains("\n") || value.Contains("\r"))
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    return value;
}

private void ConfigureEmptyStatePanel(Panel panel, Label titleLabel, Label messageLabel)


	{
		UiTheme.ConfigureStateLabels(titleLabel, messageLabel);
		panel.BackColor = UiTheme.Slate50;


		panel.Visible = false;


	}

















































	private void SetHistoryOnlyMode(bool enabled)
	{
		if (_residentTabs == null || enabled == _historyOnlyMode)
		{
			return;
		}

		_historyOnlyMode = enabled;
		if (_listPanel != null)
		{
			_listPanel.Visible = true;
		}

		_residentHeader.Visible = true;
		EnsureResidentTabsVisible();
		SetTabHeadersVisible(!enabled && !_useSidebarTabs);
		if (enabled)
		{
			SetResidentProfileTab("activity", userInitiated: false, force: true);
		}
		else if (string.Equals(_currentProfileRouteSegment, "activity", StringComparison.OrdinalIgnoreCase))
		{
			SetResidentProfileTab("overview", userInitiated: false, force: true);
		}
	}

	private void EnsureResidentTabsVisible()
	{
		if (_residentTabs == null)
		{
			return;
		}

		if (_residentTabCache == null)
		{
			_residentTabCache = new[] { _tabProfile, _tabBlotter, _tabCertificates, _tabPayments, _tabHistory };
		}

		foreach (TabPage tab in _residentTabCache)
		{
			if (!_residentTabs.TabPages.Contains(tab))
			{
				_residentTabs.TabPages.Add(tab);
			}
		}
	}





	private void SetTabHeadersVisible(bool visible)


	{


		if (visible)


		{


			_residentTabs.Appearance = TabAppearance.Normal;


			_residentTabs.ItemSize = new Size(120, 32);


			_residentTabs.SizeMode = TabSizeMode.Fixed;


		}


		else


		{


			_residentTabs.Appearance = TabAppearance.FlatButtons;


			_residentTabs.ItemSize = new Size(0, 1);


			_residentTabs.SizeMode = TabSizeMode.Fixed;


		}


	}





	private void ConfigureGrid()


	{


		dgvResidents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;


		dgvResidents.MultiSelect = false;


		dgvResidents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;


		dgvResidents.ReadOnly = true;


		dgvResidents.AllowUserToAddRows = false;


		dgvResidents.AllowUserToDeleteRows = false;
		dgvResidents.AllowUserToResizeRows = false;
		dgvResidents.RowHeadersVisible = false;
		dgvResidents.BorderStyle = BorderStyle.None;
		dgvResidents.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
		dgvResidents.EnableHeadersVisualStyles = false;
		dgvResidents.BackgroundColor = Color.White;
		dgvResidents.GridColor = Color.FromArgb(233, 236, 243);
		dgvResidents.ScrollBars = ScrollBars.Vertical;


		UiTheme.StyleGrid(dgvResidents);
		dgvResidents.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
		dgvResidents.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
		dgvResidents.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(17, 24, 39);
		dgvResidents.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
		dgvResidents.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(17, 24, 39);
		dgvResidents.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
		dgvResidents.ColumnHeadersDefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
		dgvResidents.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
		dgvResidents.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
		dgvResidents.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 238, 255);
		dgvResidents.DefaultCellStyle.SelectionForeColor = Color.FromArgb(18, 53, 103);
		dgvResidents.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
		dgvResidents.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
		dgvResidents.RowTemplate.Height = 32;
		dgvResidents.ColumnHeadersHeight = 36;
		dgvResidents.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;


		dgvResidents.SelectionChanged -= DgvResidents_SelectionChanged;
		dgvResidents.SelectionChanged += DgvResidents_SelectionChanged;
		dgvResidents.CellFormatting -= DgvResidents_CellFormatting;
		dgvResidents.CellFormatting += DgvResidents_CellFormatting;


	}





	private void ArrangeResidentPanel()
	{
		EnsureResidentListScaffold();

		if (_actionsPanel != null)
		{
			_actionsPanel.Visible = false;
			_actionsPanel.Parent?.Controls.Remove(_actionsPanel);
		}

		if (_searchPanel != null)
		{
			_searchPanel.Visible = false;
			_searchPanel.Parent?.Controls.Remove(_searchPanel);
		}

		if (_listPanel != null)
		{
			UiTheme.StyleSectionCard(_listPanel, Color.White, enforceBorder: false, padding: Padding.Empty);
			_listPanel.Margin = Padding.Empty;
			_listPanel.Padding = Padding.Empty;
			_listPanel.BackColor = Color.White;
			_listPanel.AutoScroll = false;
			ConfigureResidentPagerControls();
			UpdateResidentListVisualState();
		}

		datapanel.BackColor = Color.White;
		datapanel.BorderStyle = BorderStyle.None;
		UpdateRightPanelSelectionState();
	}

	private void ConfigureResidentPagerControls()
	{
		if (_listPanel == null)
		{
			return;
		}

		if (panelLeftPagerHost == null)
		{
			return;
		}

		Control pagerHost = panelLeftPagerHost;
		_residentPagerPanel.SuspendLayout();
		try
		{
			_residentPagerPanel.Name = "tableLeftPaging";
			_residentPagerPanel.Dock = DockStyle.Fill;
			_residentPagerPanel.Margin = Padding.Empty;
			_residentPagerPanel.Padding = Padding.Empty;
			_residentPagerPanel.BackColor = Color.Transparent;
			_residentPagerPanel.ColumnCount = 3;
			_residentPagerPanel.RowCount = 1;
			_residentPagerPanel.ColumnStyles.Clear();
			_residentPagerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
			_residentPagerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_residentPagerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
			_residentPagerPanel.RowStyles.Clear();
			_residentPagerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			_residentPagerPanel.Controls.Clear();

			UiTheme.StyleSecondaryButton(_residentPagePrev);
			UiTheme.StyleSecondaryButton(_residentPageNext);
			_residentPagePrev.Name = "btnResidentPrev";
			_residentPageNext.Name = "btnResidentNext";
			_residentPageInfo.Name = "lblResidentPageInfo";
			_residentPagePrev.Text = "Prev";
			_residentPageNext.Text = "Next";
			_residentPagePrev.AutoSize = false;
			_residentPageNext.AutoSize = false;
			_residentPagePrev.Size = new Size(90, UiTheme.StandardButtonHeight);
			_residentPageNext.Size = new Size(90, UiTheme.StandardButtonHeight);
			_residentPagePrev.Margin = Padding.Empty;
			_residentPageNext.Margin = Padding.Empty;
			_residentPagePrev.Dock = DockStyle.Fill;
			_residentPageNext.Dock = DockStyle.Fill;
			_residentPageInfo.Dock = DockStyle.Fill;
			_residentPageInfo.AutoSize = false;
			_residentPageInfo.TextAlign = ContentAlignment.MiddleCenter;
			_residentPageInfo.Font = UiTheme.LabelFont;
			_residentPageInfo.ForeColor = UiTheme.Slate600;
			_residentPageInfo.AutoEllipsis = true;
			_residentPageInfo.Margin = Padding.Empty;
			if (string.IsNullOrWhiteSpace(_residentPageInfo.Text))
			{
				_residentPageInfo.Text = "1\u201310 of 10";
			}

			_residentPagePrev.Click -= ResidentPagePrev_Click;
			_residentPagePrev.Click += ResidentPagePrev_Click;
			_residentPageNext.Click -= ResidentPageNext_Click;
			_residentPageNext.Click += ResidentPageNext_Click;

			_residentPagerPanel.Controls.Add(_residentPagePrev, 0, 0);
			_residentPagerPanel.Controls.Add(_residentPageInfo, 1, 0);
			_residentPagerPanel.Controls.Add(_residentPageNext, 2, 0);
		}
		finally
		{
			_residentPagerPanel.ResumeLayout(performLayout: true);
		}

		pagerHost.SuspendLayout();
		try
		{
			for (int i = pagerHost.Controls.Count - 1; i >= 0; i--)
			{
				Control child = pagerHost.Controls[i];
				if (!ReferenceEquals(child, _residentPagerPanel))
				{
					pagerHost.Controls.RemoveAt(i);
				}
			}

			if (!ReferenceEquals(_residentPagerPanel.Parent, pagerHost))
			{
				_residentPagerPanel.Parent?.Controls.Remove(_residentPagerPanel);
				pagerHost.Controls.Add(_residentPagerPanel);
			}
		}
		finally
		{
			pagerHost.ResumeLayout(performLayout: true);
		}

		UpdateResidentPagerState();
	}

	private void ConfigureResidentListResponsiveLayout(ResidentResponsiveMode mode)
	{
		if (_listPanel == null || _listPanel.IsDisposed)
		{
			return;
		}

		ApplyResidentGridCompactMode(compact: false);
		UpdateResidentPagerState();
	}

	private void EnsureResidentListScaffold()
	{
		if (tableLeftRoot == null)
		{
			return;
		}

		tableLeftRoot.SuspendLayout();
		try
		{
			tableLeftRoot.AutoScroll = false;
			tableLeftRoot.Padding = new Padding(12);
			tableLeftRoot.RowCount = 3;
			tableLeftRoot.ColumnCount = 1;
			tableLeftRoot.ColumnStyles.Clear();
			tableLeftRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableLeftRoot.RowStyles.Clear();
			tableLeftRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableLeftRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
			tableLeftRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
			tableLeftRoot.Controls.Clear();

			_residentGridHost.Dock = DockStyle.Fill;
			_residentGridHost.Name = "residentsGridHost";
			_residentGridHost.Margin = Padding.Empty;
			_residentGridHost.Padding = Padding.Empty;
			_residentGridHost.BackColor = Color.White;
			_residentGridHost.AutoScroll = false;
			_residentGridHost.Parent?.Controls.Remove(_residentGridHost);
			tableLeftRoot.Controls.Add(_residentGridHost, 0, 0);

			if (panelLeftPagerHost != null)
			{
				panelLeftPagerHost.Dock = DockStyle.Fill;
				panelLeftPagerHost.Margin = Padding.Empty;
				panelLeftPagerHost.Padding = Padding.Empty;
				panelLeftPagerHost.AutoScroll = false;
				panelLeftPagerHost.Parent?.Controls.Remove(panelLeftPagerHost);
				tableLeftRoot.Controls.Add(panelLeftPagerHost, 0, 1);
			}

			dgvResidents.Dock = DockStyle.Fill;
			dgvResidents.Margin = Padding.Empty;
			dgvResidents.ScrollBars = ScrollBars.Vertical;
			if (!ReferenceEquals(dgvResidents.Parent, _residentGridHost))
			{
				dgvResidents.Parent?.Controls.Remove(dgvResidents);
				_residentGridHost.Controls.Add(dgvResidents);
			}

			_residentStatusPanel.Dock = DockStyle.Fill;
			_residentStatusPanel.Margin = Padding.Empty;
			_residentStatusPanel.Padding = Padding.Empty;
			_residentStatusPanel.BackColor = Color.Transparent;
			_residentStatusPanel.AutoScroll = false;
			_residentStatusPanel.MinimumSize = new Size(0, 34);
			if (_residentStatusLayout.Controls.Count == 0)
			{
				_residentStatusLayout.Name = "tableLeftFooter";
				_residentStatusLayout.Dock = DockStyle.Fill;
				_residentStatusLayout.Margin = Padding.Empty;
				_residentStatusLayout.Padding = Padding.Empty;
				_residentStatusLayout.ColumnCount = 2;
				_residentStatusLayout.RowCount = 1;
				_residentStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
				_residentStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
				_residentStatusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

				_residentStatusLabel.Dock = DockStyle.Fill;
				_residentStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
				_residentStatusLabel.Font = UiTheme.LabelFont;
				_residentStatusLabel.ForeColor = UiTheme.Slate600;
				_residentStatusLabel.AutoEllipsis = true;
				_residentStatusLabel.AutoSize = false;
				_residentStatusLabel.UseMnemonic = false;
				_residentStatusLabel.Name = "lblResidentShowing";
				_residentStatusLabel.Margin = new Padding(0, 0, 8, 0);
				_residentStatusLabel.Text = "Showing 1\u201310 of 10";
				_residentStatusLayout.Controls.Add(_residentStatusLabel, 0, 0);

				EnsureResidentStatusLegend();
				_residentStatusLayout.Controls.Add(_residentStatusLegend, 1, 0);
				_residentStatusPanel.Controls.Add(_residentStatusLayout);
			}

			_residentStatusPanel.Parent?.Controls.Remove(_residentStatusPanel);
			tableLeftRoot.Controls.Add(_residentStatusPanel, 0, 2);

			if (!_residentGridHost.Controls.Contains(_residentListEmptyPanel))
			{
				EnsureResidentListEmptyPanel();
				_residentGridHost.Controls.Add(_residentListEmptyPanel);
			}

			if (!_residentGridHost.Controls.Contains(_residentListLoadingPanel))
			{
				EnsureResidentListLoadingPanel();
				_residentGridHost.Controls.Add(_residentListLoadingPanel);
			}
		}
		finally
		{
			tableLeftRoot.ResumeLayout(performLayout: true);
		}
	}

	private void EnsureResidentListLoadingPanel()
	{
		if (_residentListLoadingPanel.Controls.Count > 0)
		{
			return;
		}

		_residentListLoadingPanel.Dock = DockStyle.Fill;
		_residentListLoadingPanel.Margin = Padding.Empty;
		_residentListLoadingPanel.Padding = new Padding(12);
		_residentListLoadingPanel.BackColor = Color.FromArgb(247, 249, 252);
		_residentListLoadingPanel.Visible = false;

		_residentListLoadingLabel.Dock = DockStyle.Fill;
		_residentListLoadingLabel.TextAlign = ContentAlignment.MiddleCenter;
		_residentListLoadingLabel.Font = UiTheme.BodyFont;
		_residentListLoadingLabel.ForeColor = UiTheme.Slate600;
		_residentListLoadingLabel.Text = "Loading residents...";
		_residentListLoadingPanel.Controls.Add(_residentListLoadingLabel);
	}

	private void EnsureResidentStatusLegend()
	{
		if (_residentStatusLegend.Controls.Count > 0)
		{
			return;
		}

		_residentStatusLegend.FlowDirection = FlowDirection.LeftToRight;
		_residentStatusLegend.Name = "flowLegend";
		_residentStatusLegend.WrapContents = false;
		_residentStatusLegend.AutoSize = true;
		_residentStatusLegend.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_residentStatusLegend.Dock = DockStyle.None;
		_residentStatusLegend.Anchor = AnchorStyles.Right;
		_residentStatusLegend.Margin = Padding.Empty;
		_residentStatusLegend.Padding = Padding.Empty;

		ConfigureResidentLegendLabel(_residentLegendActive, "\u25CF Active", Color.FromArgb(5, 150, 105));
		ConfigureResidentLegendLabel(_residentLegendInactive, "\u25CF Inactive", Color.FromArgb(107, 114, 128));
		ConfigureResidentLegendLabel(_residentLegendDeceased, "\u25CF Deceased", Color.FromArgb(220, 38, 38));

		_residentLegendActive.Margin = new Padding(0, 0, 4, 0);
		_residentLegendInactive.Margin = new Padding(0, 0, 4, 0);
		_residentLegendDeceased.Margin = Padding.Empty;

		_residentStatusLegend.Controls.Add(_residentLegendActive);
		_residentStatusLegend.Controls.Add(_residentLegendInactive);
		_residentStatusLegend.Controls.Add(_residentLegendDeceased);
	}

private static void ConfigureResidentLegendLabel(Label label, string text, Color color)
{
		label.AutoSize = true;
		label.Text = text;
		label.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
		label.ForeColor = color;
		label.TextAlign = ContentAlignment.MiddleRight;
}

	private void EnsureResidentListEmptyPanel()
	{
		if (_residentListEmptyPanel.Controls.Count > 0)
		{
			return;
		}

		_residentListEmptyPanel.Dock = DockStyle.Fill;
		_residentListEmptyPanel.Margin = Padding.Empty;
		_residentListEmptyPanel.Padding = new Padding(20);
		_residentListEmptyPanel.BackColor = Color.White;
		_residentListEmptyPanel.Visible = false;

		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

		FlowLayoutPanel stack = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.TopDown,
			WrapContents = false,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Anchor = AnchorStyles.None,
			Margin = Padding.Empty
		};

		_residentListEmptyTitle.AutoSize = true;
		_residentListEmptyTitle.Font = UiTheme.HeadingFont;
		_residentListEmptyTitle.ForeColor = UiTheme.Slate900;
		_residentListEmptyTitle.Text = "No residents found";
		_residentListEmptyTitle.Margin = new Padding(0, 0, 0, 6);

		_residentListEmptyMessage.AutoSize = true;
		_residentListEmptyMessage.Font = UiTheme.LabelFont;
		_residentListEmptyMessage.ForeColor = UiTheme.Slate500;
		_residentListEmptyMessage.Text = "Try a different search or add a resident.";
		_residentListEmptyMessage.Margin = new Padding(0, 0, 0, 12);

		_residentListAddButton.Text = "Add Resident";
		StyleResidentPrimaryButton(_residentListAddButton, 136);
		_residentListAddButton.AutoSize = true;
		_residentListAddButton.Click -= add_Click;
		_residentListAddButton.Click += add_Click;

		stack.Controls.Add(_residentListEmptyTitle);
		stack.Controls.Add(_residentListEmptyMessage);
		stack.Controls.Add(_residentListAddButton);
		layout.Controls.Add(stack, 0, 1);
		_residentListEmptyPanel.Controls.Add(layout);
	}

	private void SetResidentListLoading(bool enabled, string? message = null)
	{
		_residentListLoading = enabled;
		if (!string.IsNullOrWhiteSpace(message))
		{
			_residentListLoadingLabel.Text = message.Trim();
		}

		bool controlsEnabled = !enabled;
		dgvResidents.Enabled = controlsEnabled;
		_searchBox.Enabled = controlsEnabled;
		_searchClear.Enabled = controlsEnabled;
		button1.Enabled = controlsEnabled;
		_residentTopAddButton.Enabled = controlsEnabled && Permissions.CanCreateResidents;
		if (enabled)
		{
			_residentPagePrev.Enabled = false;
			_residentPageNext.Enabled = false;
		}

		UpdateResidentListVisualState();
	}

	private void UpdateResidentListVisualState()
	{
		bool residentView = IsResidentView();
		int total = residentView ? (_residentTable?.DefaultView.Count ?? 0) : 0;
		bool hasSearch = !string.IsNullOrWhiteSpace(_searchBox.Text);
		bool showEmpty = residentView && !_residentListLoading && total == 0;

		dgvResidents.Visible = !_residentListLoading && (!residentView || !showEmpty);
		_residentListEmptyPanel.Visible = residentView && showEmpty;
		_residentListLoadingPanel.Visible = _residentListLoading;
		_residentListAddButton.Visible = residentView && Permissions.CanCreateResidents;
		_residentTopAddButton.Visible = residentView && Permissions.CanCreateResidents;

		if (_residentListLoading)
		{
			_residentStatusLabel.Text = "Loading residents...";
			_residentListLoadingPanel.BringToFront();
			return;
		}

		if (showEmpty)
		{
			_residentListEmptyTitle.Text = "No residents found";
			_residentListEmptyMessage.Text = hasSearch
				? "Try a different search term or clear the search box."
				: "Try a different search or add a resident.";
			_residentListEmptyPanel.BringToFront();
		}
	}

	private void ApplyResidentGridCompactMode(bool compact)
	{
		if (dgvResidents == null || dgvResidents.Columns == null || dgvResidents.Columns.Count == 0)
		{
			return;
		}

		ApplyResidentColumnMode("firstname", "First", compact ? 80 : 92);
		ApplyResidentColumnMode("middlename", "Middle", compact ? 72 : 84);
		ApplyResidentColumnMode("lastname", "Last", compact ? 80 : 92);
	}

	private void ApplyResidentColumnMode(string columnName, string headerText, int minWidth)
	{
		if (dgvResidents.Columns[columnName] is not DataGridViewColumn column)
		{
			return;
		}

		column.HeaderText = headerText;
		column.MinimumWidth = minWidth;
	}

	private void ResidentPagePrev_Click(object? sender, EventArgs e)
	{
		if (_residentPageIndex <= 0)
		{
			return;
		}

		_residentPageIndex--;
		ApplyResidentSearch(resetPage: false);
	}

	private void ResidentPageNext_Click(object? sender, EventArgs e)
	{
		if (_residentTable == null)
		{
			return;
		}

		int total = _residentTable.DefaultView.Count;
		int maxPageIndex = total <= 0 ? 0 : (int)Math.Ceiling(total / (double)ResidentPageSize) - 1;
		if (_residentPageIndex >= maxPageIndex)
		{
			return;
		}

		_residentPageIndex++;
		ApplyResidentSearch(resetPage: false);
	}

	private void UpdateResidentPagerState()
	{
		bool residentView = IsResidentView();
		_residentPagerPanel.Visible = residentView;
		_residentStatusPanel.Visible = residentView;

		if (!residentView)
		{
			_residentPageInfo.Text = string.Empty;
			_residentStatusLabel.Text = string.Empty;
			return;
		}

		int total = _residentTable?.DefaultView.Count ?? 0;
		int totalPages = total <= 0 ? 1 : (int)Math.Ceiling(total / (double)ResidentPageSize);
		if (_residentPageIndex < 0)
		{
			_residentPageIndex = 0;
		}

		if (_residentPageIndex >= totalPages)
		{
			_residentPageIndex = totalPages - 1;
		}

		int first = total == 0 ? 0 : (_residentPageIndex * ResidentPageSize) + 1;
		int last = Math.Min(total, (_residentPageIndex + 1) * ResidentPageSize);
		_residentPageInfo.Text = $"{first}\u2013{last} of {total}";
		_residentPagePrev.Enabled = !_residentListLoading && _residentPageIndex > 0;
		_residentPageNext.Enabled = !_residentListLoading && (_residentPageIndex + 1) < totalPages;
		_residentStatusLabel.Text = total == 0
			? "Showing 0\u20130 of 0"
			: $"Showing {first}\u2013{last} of {total}";
		UpdateResidentListVisualState();
	}

	private void EnsureCertificatePagerControls(Control? host)
	{
		if (host == null)
		{
			return;
		}

		if (ReferenceEquals(host, _residentDocumentsFooterPanel))
		{
			host = certContainer;
		}

		if (host == null)
		{
			return;
		}

		UiTheme.StyleSecondaryButton(_certPagePrev);
		UiTheme.StyleSecondaryButton(_certPageNext);
		_certPagePrev.Text = "Prev";
		_certPageNext.Text = "Next";
		_certPagePrev.AutoSize = false;
		_certPageNext.AutoSize = false;
		_certPagePrev.Size = new Size(90, UiTheme.StandardButtonHeight);
		_certPageNext.Size = new Size(90, UiTheme.StandardButtonHeight);
		_certPagePrev.Margin = Padding.Empty;
		_certPageNext.Margin = new Padding(8, 0, 0, 0);
		_certPageInfo.AutoSize = false;
		_certPageInfo.Dock = DockStyle.Fill;
		_certPageInfo.TextAlign = ContentAlignment.MiddleCenter;
		_certPageInfo.AutoEllipsis = true;
		_certPageInfo.Font = UiTheme.LabelFont;
		_certPageInfo.ForeColor = UiTheme.Slate500;
		_certPageInfo.Margin = Padding.Empty;

		_residentRowsPerPageLabel.AutoSize = true;
		_residentRowsPerPageLabel.Font = UiTheme.LabelFont;
		_residentRowsPerPageLabel.ForeColor = UiTheme.Slate500;
		_residentRowsPerPageLabel.Margin = new Padding(0, 7, 8, 0);
		_residentRowsPerPageLabel.Text = "Rows per page:";

		_residentRowsPerPageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		_residentRowsPerPageCombo.Width = 70;
		_residentRowsPerPageCombo.Font = UiTheme.LabelFont;
		_residentRowsPerPageCombo.Margin = new Padding(0, 3, 0, 0);
		if (_residentRowsPerPageCombo.Items.Count == 0)
		{
			_residentRowsPerPageCombo.Items.AddRange(new object[] { "10", "25", "50" });
		}

		string selectedRowsPerPage = _certificatePageSize.ToString();
		if (!_residentRowsPerPageCombo.Items.Contains(selectedRowsPerPage))
		{
			_residentRowsPerPageCombo.Items.Insert(0, selectedRowsPerPage);
		}

		_residentRowsPerPageCombo.SelectedItem = selectedRowsPerPage;
		_residentRowsPerPageCombo.SelectedIndexChanged -= ResidentRowsPerPageCombo_SelectedIndexChanged;
		_residentRowsPerPageCombo.SelectedIndexChanged += ResidentRowsPerPageCombo_SelectedIndexChanged;

		_certPagePrev.Click -= CertPagePrev_Click;
		_certPagePrev.Click += CertPagePrev_Click;
		_certPageNext.Click -= CertPageNext_Click;
		_certPageNext.Click += CertPageNext_Click;

		_residentDocumentsFooterPanel.Dock = DockStyle.Bottom;
		_residentDocumentsFooterPanel.Height = 44;
		_residentDocumentsFooterPanel.Padding = new Padding(8);
		_residentDocumentsFooterPanel.Margin = Padding.Empty;
		_residentDocumentsFooterPanel.BackColor = Color.Transparent;

		_residentDocumentsFooterLayout.Name = "tableDocsFooter";
		_residentDocumentsFooterLayout.Dock = DockStyle.Fill;
		_residentDocumentsFooterLayout.Margin = Padding.Empty;
		_residentDocumentsFooterLayout.Padding = Padding.Empty;
		_residentDocumentsFooterLayout.ColumnCount = 3;
		_residentDocumentsFooterLayout.RowCount = 1;
		_residentDocumentsFooterLayout.ColumnStyles.Clear();
		_residentDocumentsFooterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
		_residentDocumentsFooterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_residentDocumentsFooterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
		_residentDocumentsFooterLayout.RowStyles.Clear();
		_residentDocumentsFooterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		_residentDocumentsFooterLeft.FlowDirection = FlowDirection.LeftToRight;
		_residentDocumentsFooterLeft.WrapContents = false;
		_residentDocumentsFooterLeft.AutoSize = false;
		_residentDocumentsFooterLeft.Dock = DockStyle.Fill;
		_residentDocumentsFooterLeft.Margin = Padding.Empty;
		_residentDocumentsFooterLeft.Padding = Padding.Empty;
		_residentDocumentsFooterLeft.Controls.Clear();
		_residentDocumentsFooterLeft.Controls.Add(_certPagePrev);
		_residentDocumentsFooterLeft.Controls.Add(_certPageNext);

		_residentDocumentsFooterRight.FlowDirection = FlowDirection.LeftToRight;
		_residentDocumentsFooterRight.WrapContents = false;
		_residentDocumentsFooterRight.AutoSize = true;
		_residentDocumentsFooterRight.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_residentDocumentsFooterRight.Dock = DockStyle.Right;
		_residentDocumentsFooterRight.Margin = Padding.Empty;
		_residentDocumentsFooterRight.Padding = Padding.Empty;
		_residentDocumentsFooterRight.Controls.Clear();
		_residentDocumentsFooterRight.Controls.Add(_residentRowsPerPageLabel);
		_residentDocumentsFooterRight.Controls.Add(_residentRowsPerPageCombo);

		_residentDocumentsFooterLayout.Controls.Clear();
		_residentDocumentsFooterLayout.Controls.Add(_residentDocumentsFooterLeft, 0, 0);
		_residentDocumentsFooterLayout.Controls.Add(_certPageInfo, 1, 0);
		_residentDocumentsFooterLayout.Controls.Add(_residentDocumentsFooterRight, 2, 0);

		_residentDocumentsFooterPanel.Controls.Clear();
		_residentDocumentsFooterPanel.Controls.Add(_residentDocumentsFooterLayout);

		if (!ReferenceEquals(_residentDocumentsFooterPanel.Parent, host))
		{
			_residentDocumentsFooterPanel.Parent?.Controls.Remove(_residentDocumentsFooterPanel);
			host.Controls.Add(_residentDocumentsFooterPanel);
		}

		UpdateCertificatePagerState();
	}

	private void ResidentRowsPerPageCombo_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (!int.TryParse(Convert.ToString(_residentRowsPerPageCombo.SelectedItem), out int selectedPageSize))
		{
			return;
		}

		if (selectedPageSize <= 0 || selectedPageSize == _certificatePageSize)
		{
			return;
		}

		_certificatePageSize = selectedPageSize;
		_certPageIndex = 0;
		ApplyCertificateFilters(resetPage: true);
	}

	private void CertPagePrev_Click(object? sender, EventArgs e)
	{
		if (_certPageIndex <= 0)
		{
			return;
		}

		_certPageIndex--;
		ApplyCertificateFilters(resetPage: false);
	}

	private void CertPageNext_Click(object? sender, EventArgs e)
	{
		if (_certTable == null)
		{
			return;
		}

		int total = _certTable.DefaultView.Count;
		int maxPageIndex = total <= 0 ? 0 : (int)Math.Ceiling(total / (double)_certificatePageSize) - 1;
		if (_certPageIndex >= maxPageIndex)
		{
			return;
		}

		_certPageIndex++;
		ApplyCertificateFilters(resetPage: false);
	}

	private void UpdateCertificatePagerState()
	{
		int total = _certTable?.DefaultView.Count ?? 0;
		int totalPages = total <= 0 ? 1 : (int)Math.Ceiling(total / (double)_certificatePageSize);

		if (_certPageIndex < 0)
		{
			_certPageIndex = 0;
		}

		if (_certPageIndex >= totalPages)
		{
			_certPageIndex = totalPages - 1;
		}

		int first = total == 0 ? 0 : (_certPageIndex * _certificatePageSize) + 1;
		int last = Math.Min(total, (_certPageIndex + 1) * _certificatePageSize);
		_certPageInfo.Text = $"{first}\u2013{last} of {total}";
		_certPagePrev.Enabled = _certPageIndex > 0;
		_certPageNext.Enabled = (_certPageIndex + 1) < totalPages;
	}

	private void ConfigureCertificateActionMenu()
	{
		_certificateActionsMenu.Items.Clear();
		_certificateActionsMenu.Items.Add(_certificateActionView);
		_certificateActionsMenu.Items.Add(_certificateActionDownload);
		_certificateActionsMenu.Items.Add(_certificateActionPrint);
		_certificateActionsMenu.Items.Add(_certificateActionRelease);
		_certificateActionsMenu.Items.Add(_certificateActionReject);

		_certificateActionView.Click -= CertificateActionView_Click;
		_certificateActionView.Click += CertificateActionView_Click;
		_certificateActionDownload.Click -= CertificateActionDownload_Click;
		_certificateActionDownload.Click += CertificateActionDownload_Click;
		_certificateActionPrint.Click -= CertificateActionPrint_Click;
		_certificateActionPrint.Click += CertificateActionPrint_Click;
		_certificateActionRelease.Click -= CertificateActionRelease_Click;
		_certificateActionRelease.Click += CertificateActionRelease_Click;
		_certificateActionReject.Click -= CertificateActionReject_Click;
		_certificateActionReject.Click += CertificateActionReject_Click;
	}

	private void CertGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		if (_certGrid.Columns[e.ColumnIndex] is not DataGridViewColumn column)
		{
			return;
		}

		if (string.Equals(column.Name, CertificateRowNumberColumnName, StringComparison.Ordinal))
		{
			e.Value = (_certPageIndex * _certificatePageSize + e.RowIndex + 1).ToString();
			e.FormattingApplied = true;
			e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			return;
		}

		if (!string.Equals(column.Name, "status", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		string raw = Convert.ToString(e.Value)?.Trim() ?? string.Empty;
		string normalized = raw switch
		{
			"Requested" => "Submitted",
			"Draft" => "Submitted",
			"Issued" => "Released",
			_ => raw
		};

		Color backColor;
		Color foreColor;
		switch (normalized)
		{
		case "Approved":
			backColor = Color.FromArgb(210, 245, 220);
			foreColor = Color.FromArgb(0, 97, 54);
			break;
		case "Submitted":
			backColor = Color.FromArgb(255, 237, 204);
			foreColor = Color.FromArgb(122, 69, 0);
			break;
		case "Released":
			backColor = Color.FromArgb(219, 234, 254);
			foreColor = Color.FromArgb(29, 78, 216);
			break;
		case "Rejected":
		case "Cancelled":
			backColor = Color.FromArgb(254, 226, 226);
			foreColor = Color.FromArgb(153, 27, 27);
			normalized = "Rejected";
			break;
		default:
			backColor = Color.FromArgb(235, 236, 240);
			foreColor = Color.FromArgb(77, 85, 102);
			break;
		}

		e.Value = normalized;
		e.FormattingApplied = true;
		e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
		e.CellStyle.BackColor = backColor;
		e.CellStyle.ForeColor = foreColor;
		e.CellStyle.SelectionBackColor = backColor;
		e.CellStyle.SelectionForeColor = foreColor;
	}

	private void CertGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0 || e.ColumnIndex < 0)
		{
			return;
		}

		if (_certGrid.Columns[e.ColumnIndex] is not DataGridViewColumn column
			|| !string.Equals(column.Name, CertificateActionsColumnName, StringComparison.Ordinal))
		{
			return;
		}

		if (e.RowIndex < _certGrid.Rows.Count)
		{
			_certGrid.ClearSelection();
			_certGrid.Rows[e.RowIndex].Selected = true;
			if (_certGrid.Rows[e.RowIndex].Cells.Count > 0)
			{
				_certGrid.CurrentCell = _certGrid.Rows[e.RowIndex].Cells[0];
			}
		}

		Point cursorPoint = _certGrid.PointToClient(Cursor.Position);
		_certificateActionsMenu.Show(_certGrid, cursorPoint);
	}

	private void CertificateActionPrint_Click(object? sender, EventArgs e)
	{
		CertPrint_Click(sender, e);
	}

	private void CertificateActionView_Click(object? sender, EventArgs e)
	{
		if (_certGrid.SelectedRows.Count > 0)
		{
			PopulateCertificateDetails(_certGrid.SelectedRows[0]);
		}
	}

	private void CertificateActionDownload_Click(object? sender, EventArgs e)
	{
		ControllerDialogs.Info("Download is not available yet.", "Documents");
	}

	private void CertificateActionRelease_Click(object? sender, EventArgs e)
	{
		CertIssue_Click(sender, e);
	}

	private void CertificateActionReject_Click(object? sender, EventArgs e)
	{
		CertCancel_Click(sender, e);
	}

	private void EnsureBlotterPagerControls(FlowLayoutPanel? host)
	{
		if (host == null)
		{
			return;
		}

		_blotterPagerPanel.AutoSize = false;
		_blotterPagerPanel.AutoSizeMode = AutoSizeMode.GrowOnly;
		_blotterPagerPanel.WrapContents = false;
		_blotterPagerPanel.FlowDirection = FlowDirection.LeftToRight;
		_blotterPagerPanel.Padding = Padding.Empty;
		_blotterPagerPanel.Margin = Padding.Empty;
		_blotterPagerPanel.MinimumSize = new Size(0, UiTheme.StandardButtonHeight);
		_blotterPagerPanel.Height = UiTheme.StandardButtonHeight;

		UiTheme.StyleSecondaryButton(_blotterPagePrev);
		UiTheme.StyleSecondaryButton(_blotterPageNext);
		_blotterPagePrev.Text = "Prev";
		_blotterPageNext.Text = "Next";
		_blotterPagePrev.AutoSize = false;
		_blotterPageNext.AutoSize = false;
		_blotterPagePrev.Height = UiTheme.StandardButtonHeight;
		_blotterPageNext.Height = UiTheme.StandardButtonHeight;
		_blotterPagePrev.Margin = new Padding(0, 0, 8, 0);
		_blotterPageInfo.AutoSize = true;
		_blotterPageInfo.AutoEllipsis = true;
		_blotterPageInfo.Font = UiTheme.LabelFont;
		_blotterPageInfo.ForeColor = UiTheme.Slate500;
		_blotterPageInfo.Margin = new Padding(0, 8, 8, 0);

		_blotterPagePrev.Click -= BlotterPagePrev_Click;
		_blotterPagePrev.Click += BlotterPagePrev_Click;
		_blotterPageNext.Click -= BlotterPageNext_Click;
		_blotterPageNext.Click += BlotterPageNext_Click;

		if (!_blotterPagerPanel.Controls.Contains(_blotterPagePrev))
		{
			_blotterPagerPanel.Controls.Add(_blotterPagePrev);
		}

		if (!_blotterPagerPanel.Controls.Contains(_blotterPageInfo))
		{
			_blotterPagerPanel.Controls.Add(_blotterPageInfo);
		}

		if (!_blotterPagerPanel.Controls.Contains(_blotterPageNext))
		{
			_blotterPagerPanel.Controls.Add(_blotterPageNext);
		}

		if (!host.Controls.Contains(_blotterPagerPanel))
		{
			host.Controls.Add(_blotterPagerPanel);
		}

		UpdateBlotterPagerState();
	}

	private void BlotterPagePrev_Click(object? sender, EventArgs e)
	{
		if (_blotterPageIndex <= 0)
		{
			return;
		}

		_blotterPageIndex--;
		RenderBlotterCards();
	}

	private void BlotterPageNext_Click(object? sender, EventArgs e)
	{
		int total = _blotterFilteredRecords?.Count ?? 0;
		int maxPageIndex = total <= 0 ? 0 : (int)Math.Ceiling(total / (double)BlotterPageSize) - 1;
		if (_blotterPageIndex >= maxPageIndex)
		{
			return;
		}

		_blotterPageIndex++;
		RenderBlotterCards();
	}

	private void UpdateBlotterPagerState()
	{
		int total = _blotterFilteredRecords?.Count ?? 0;
		int totalPages = total <= 0 ? 1 : (int)Math.Ceiling(total / (double)BlotterPageSize);

		if (_blotterPageIndex < 0)
		{
			_blotterPageIndex = 0;
		}

		if (_blotterPageIndex >= totalPages)
		{
			_blotterPageIndex = totalPages - 1;
		}

		int first = total == 0 ? 0 : (_blotterPageIndex * BlotterPageSize) + 1;
		int last = Math.Min(total, (_blotterPageIndex + 1) * BlotterPageSize);
		_blotterPageInfo.Text = $"Blotter Cases {first}-{last} of {total}";
		_blotterPagePrev.Enabled = _blotterPageIndex > 0;
		_blotterPageNext.Enabled = (_blotterPageIndex + 1) < totalPages;
		_casesFooter.Text = $"Showing {total} items";
	}

	public void EnsureResidentsLoaded()
	{
		if (!IsResidentView())
		{
			LoadResidents();
		}
	}

	public void ShowProfile()
	{
		EnsureResidentsLoaded();
		SetHistoryOnlyMode(false);
		if (_listPanel != null)
		{
			_listPanel.Visible = true;
		}

		if (_residentHeader != null)
		{
			_residentHeader.Visible = true;
		}
		SetResidentProfileTab("overview", userInitiated: false, force: true);
		QueueResponsiveLayoutRefresh();
	}

	public void RefreshLayoutNow()
	{
		if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || IsDisposed || !IsHandleCreated)
		{
			return;
		}

		QueueResponsiveLayoutRefresh();
	}

	public void ShowBlotter()
	{
		EnsureResidentsLoaded();
		SetHistoryOnlyMode(false);
		if (_listPanel != null)
		{
			_listPanel.Visible = true;
		}

		if (_residentHeader != null)
		{
			_residentHeader.Visible = true;
		}
		SetResidentProfileTab("cases", userInitiated: false, force: true);
	}

	public void ShowCertificates()
	{
		EnsureResidentsLoaded();
		SetHistoryOnlyMode(false);
		if (_listPanel != null)
		{
			_listPanel.Visible = true;
		}

		if (_residentHeader != null)
		{
			_residentHeader.Visible = true;
		}
		SetResidentProfileTab("documents", userInitiated: false, force: true);
	}

	public void ExecuteCertificateAction(CertificateAction action)
	{
		ShowCertificates();
		switch (action)
		{
		case CertificateAction.NewRequest:
			CertNew_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.EditRequest:
			CertEdit_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.Approve:
			CertApprove_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.Issue:
			CertIssue_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.Print:
			CertPrint_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.Export:
			CertExport_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.Cancel:
			CertCancel_Click(this, EventArgs.Empty);
			break;
		case CertificateAction.Refresh:
			CertRefresh_Click(this, EventArgs.Empty);
			break;
		}
	}

public void ShowHistory()
{
    EnsureResidentsLoaded();
    SetHistoryOnlyMode(false);
	if (_listPanel != null)
	{
		_listPanel.Visible = true;
	}

	if (_residentHeader != null)
	{
		_residentHeader.Visible = true;
	}
	SetResidentProfileTab("activity", userInitiated: false, force: true);
    if (_selectedResidentId.HasValue)
    {
        LoadResidentHistory(_selectedResidentId.Value);
    }
    else
    {
		UpdateHistoryEmptyState();
    }
    UpdateHistoryEmptyState();
}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		if (_residentInitialLoadTriggered)
		{
			return;
		}

		_residentInitialLoadTriggered = true;
		BeginInvoke(new Action(() =>
		{
			QueueResponsiveLayoutRefresh();
			ResetProfileViewport();
			if (_residentTable == null && !_residentLoadInProgress)
			{
				LoadResidents();
			}
			QueueResidentSchemaInitialization();
		}));
	}






	private void LoadResidents()


	{
		if (_residentLoadInProgress)
		{
			_residentReloadPending = true;
			return;
		}

		_ = LoadResidentsAsync();
	}

	private void QueueResidentSchemaInitialization()
	{
		if (_residentSchemasInitialized || _residentSchemaInitQueued)
		{
			return;
		}

		_residentSchemaInitQueued = true;
		_ = Task.Run(() =>
		{
			try
			{
				EnsureResidentSchemasInitialized();
			}
			finally
			{
				_residentSchemaInitQueued = false;
			}
		});
	}

	private async Task LoadResidentsAsync()
	{
		_residentLoadInProgress = true;
		_residentReloadPending = false;
		int loadVersion = ++_residentLoadVersion;

		SetResidentListLoading(enabled: true, "Loading residents...");

		try
		{
			ExitEditMode();
			_residentDetailsLoadedId = null;
			_residentDetailsLoadVersion++;

			string deletedFilter = _showDeletedResidents ? "IFNULL(r.is_deleted,0)=1" : "IFNULL(r.is_deleted,0)=0";
			DataTable dataTable = await Task.Run(() => QueryResidentsTable(deletedFilter));

			if (IsDisposed || loadVersion != _residentLoadVersion)
			{
				return;
			}

				_residentTable = dataTable;
				lock (_residentPhotoCache)
				{
					_residentPhotoCache.Clear();
				}
				ApplyResidentSearch(resetPage: true);
				UpdateResidentSoftDeleteButtons();
				UpdateRightPanelSelectionState();
			_residentTabs.Enabled = true;
		}
		catch (Exception ex)
		{
			if (!IsDisposed)
			{
				ControllerDialogs.Error(ex, "Unable to load residents.", "Error");
			}
		}
		finally
		{
			SetResidentListLoading(enabled: false);
			UpdateResidentPagerState();
			_residentLoadInProgress = false;
			if (_residentReloadPending && !IsDisposed)
			{
				_residentReloadPending = false;
				LoadResidents();
			}
		}
	}

	private static DataTable QueryResidentsTable(string deletedFilter)
	{
		return DbHelper.LoadTable($@"SELECT r.resident_id,
                                      r.barangay_id,
                                      r.purok_id,
                                      r.household_id,
                                      b.name AS barangay_name,
                                      p.name AS purok_name,
                                      COALESCE(NULLIF(TRIM(CONCAT_WS(' ', h.house_no, h.street, h.subdivision)), ''), CONCAT('Household #', h.household_id)) AS household_label,
                                      r.first_name AS firstname,
                                      r.middle_name AS middlename,
                                      r.last_name AS lastname,
                                      CASE r.sex
                                          WHEN 'M' THEN 'Male'
                                          WHEN 'F' THEN 'Female'
                                          ELSE 'Other'
                                      END AS gender,
                                      r.birth_date AS date_of_birth,
                                      r.civil_status,
                                      r.contact_no,
	                                      CASE r.status
	                                          WHEN 'ACTIVE' THEN 'Active'
	                                          WHEN 'DECEASED' THEN 'Deceased'
	                                          WHEN 'MOVED_OUT' THEN 'Inactive'
	                                          ELSE r.status
	                                      END AS status
	                               FROM resident r
                               LEFT JOIN barangay b ON b.barangay_id = r.barangay_id
                               LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
                               LEFT JOIN household h ON h.household_id = r.household_id
	                               WHERE {deletedFilter}
	                               ORDER BY r.last_name, r.first_name");
	}

	private async Task LoadResidentPhotoAsync(int residentId, int detailLoadVersion, int photoLoadVersion)
	{
		if (residentId <= 0 || IsDisposed)
		{
			return;
		}

		byte[]? photoBytes = await Task.Run(() => TryGetResidentPhotoCached(residentId)).ConfigureAwait(true);

		if (IsDisposed
			|| detailLoadVersion != _residentDetailsLoadVersion
			|| photoLoadVersion != _residentPhotoLoadVersion
			|| !_selectedResidentId.HasValue
			|| _selectedResidentId.Value != residentId
			|| _residentPhotoPendingBytes != null
			|| _residentPhotoRemoved)
		{
			return;
		}

		_residentPhotoBytes = photoBytes;
		LoadResidentPhoto(_residentPhotoBytes);
		UpdateResidentPhotoControls();
	}

	private byte[]? TryGetResidentPhotoCached(int residentId)
	{
		lock (_residentPhotoCache)
		{
			if (_residentPhotoCache.TryGetValue(residentId, out byte[]? cached))
			{
				return cached;
			}
		}

		byte[]? loaded = QueryResidentPhotoBytes(residentId);
		lock (_residentPhotoCache)
		{
			_residentPhotoCache[residentId] = loaded;
		}

		return loaded;
	}

	private static byte[]? QueryResidentPhotoBytes(int residentId)
	{
		try
		{
			using var conn = DBConnection.GetConnection();
			conn.Open();
			using var cmd = new MySqlCommand(
				@"SELECT photo
				  FROM resident
				  WHERE resident_id = @id
				  LIMIT 1", conn);
			cmd.Parameters.AddWithValue("@id", residentId);
			object? value = cmd.ExecuteScalar();
			return value == null || value == DBNull.Value ? null : value as byte[];
		}
		catch
		{
			return null;
		}
	}





	private void UpdateResidentSoftDeleteButtons()
	{
		bool isResidentView = IsResidentView();
		bool hasSelection = isResidentView && dgvResidents.SelectedRows.Count > 0;

		bool canCreate = Permissions.CanCreateResidents;
		bool canUpdate = Permissions.CanUpdateResidents;
		bool canDelete = Permissions.CanDeleteResidents;

		// Keep the UI clean: only show deleted controls when the user can delete/restore.
		_residentShowDeletedToggle.Visible = isResidentView && canDelete;
		_residentRestoreButton.Visible = isResidentView && canDelete && _showDeletedResidents;

		add.Enabled = isResidentView && canCreate;
		_residentTopAddButton.Enabled = isResidentView && canCreate && !_residentListLoading;
		_residentTopAddButton.Visible = isResidentView && canCreate;
		_residentListAddButton.Enabled = isResidentView && canCreate && !_residentListLoading;
		_residentListAddButton.Visible = isResidentView && canCreate;
		button3.Enabled = isResidentView && canDelete && hasSelection && !_showDeletedResidents;
		_residentRestoreButton.Enabled = isResidentView && canDelete && hasSelection && _showDeletedResidents;
		_residentQuickEdit.Enabled = isResidentView && canUpdate && hasSelection && !_showDeletedResidents && !_isEditing;
		_btnResidentAttachments.Enabled = isResidentView && hasSelection;
		UpdateResidentPickerSummary();
		UpdateResidentEditActionsState();
	}

	private void LoadUsers()


	{
		SetResidentListLoading(enabled: true, "Loading records...");
		BeginModuleLoading("Loading users...");


		try


		{


			ExitEditMode();
			_residentDetailsLoadedId = null;
			_residentDetailsLoadVersion++;


			DataTable dataTable = DbHelper.LoadTable(@"SELECT ua.user_id,
                                       ua.username,
                                       COALESCE(r.name, 'Staff') AS role,
                                       ua.is_active,
                                       ua.created_at
                                       FROM user_account ua
                                       LEFT JOIN user_role ur ON ur.user_id = ua.user_id
                                       LEFT JOIN role r ON r.role_id = ur.role_id
                                       ORDER BY ua.username");

			_residentTable = null;
			_residentPageIndex = 0;
			dgvResidents.DataSource = dataTable;
			UpdateResidentListVisualState();


			ConfigureUserGridColumns();


			ClearResidentDetails("Select a resident to view details.");


			_residentTabs.Enabled = false;


			_searchPanel?.Hide();


			_searchBox.Text = string.Empty;
			UpdateResidentPagerState();
			UpdateRightPanelSelectionState();

			UpdateResidentSoftDeleteButtons();




		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to load users.", "Error");


		}
		finally
		{
			EndModuleLoading();
			SetResidentListLoading(enabled: false);
			UpdateResidentPagerState();
		}


	}










	private void EnsureResidentSchemasInitialized()
	{
		if (_residentSchemasInitialized)
		{
			return;
		}

		lock (ResidentSchemaInitLock)
		{
			if (_residentSchemasInitialized)
			{
				return;
			}

			EnsureResidentsSchema();
			EnsureCertificatesSchema();
			EnsureBlotterSchema();
			_residentSchemasInitialized = true;
		}
	}

	private void EnsureCertificatesSchema()


	{


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();
			mySqlConnection.Open();
			SchemaBootstrap.EnsureCoreDefaults(mySqlConnection);


			EnsureCertificateColumns(mySqlConnection);

			EnsureDocumentNumberSequenceSchema(mySqlConnection);


			EnsureCertificateAuditSchema(mySqlConnection);


			EnsureActivityLogSchema(mySqlConnection);

			EnsureDocumentPaymentOrNumberIndex(mySqlConnection);


		}


		catch (Exception ex)


		{


			ControllerDialogs.Warning(ex, "Certificates table check failed.", "Warning");


		}


	}





	private void EnsureResidentsSchema()


	{


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();
			mySqlConnection.Open();
			SchemaBootstrap.EnsureCoreDefaults(mySqlConnection);


			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


			using (MySqlCommand mySqlCommand = new MySqlCommand("SELECT COLUMN_NAME\r\n                             FROM INFORMATION_SCHEMA.COLUMNS\r\n                             WHERE TABLE_SCHEMA = DATABASE()\r\n                               AND TABLE_NAME = 'resident';", mySqlConnection))


			{


				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();


				while (mySqlDataReader.Read())


				{


					hashSet.Add(mySqlDataReader.GetString(0));


				}


			}


			AddResidentColumnIfMissing(mySqlConnection, hashSet, "photo", "LONGBLOB NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "birth_place", "VARCHAR(150) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "citizenship", "VARCHAR(100) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "religion", "VARCHAR(100) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "email", "VARCHAR(150) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "occupation", "VARCHAR(150) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "employer", "VARCHAR(150) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "education_level", "VARCHAR(100) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "is_pwd", "TINYINT(1) NOT NULL DEFAULT 0");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "pwd_id_no", "VARCHAR(100) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "is_senior", "TINYINT(1) NOT NULL DEFAULT 0");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "is_4ps_beneficiary", "TINYINT(1) NOT NULL DEFAULT 0");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "is_registered_voter", "TINYINT(1) NOT NULL DEFAULT 0");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "voter_precinct_no", "VARCHAR(50) NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "is_deleted", "TINYINT(1) NOT NULL DEFAULT 0");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "deleted_at", "DATETIME NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "deleted_by_user_id", "INT NULL");
			AddResidentColumnIfMissing(mySqlConnection, hashSet, "delete_reason", "VARCHAR(255) NULL");


		}


		catch (Exception ex)


		{


			ControllerDialogs.Warning(ex, "Residents table check failed.", "Warning");


		}


	}

	private static void AddResidentColumnIfMissing(MySqlConnection conn, HashSet<string> existing, string columnName, string definition)
	{
		if (existing.Contains(columnName))
		{
			return;
		}

		using MySqlCommand mySqlCommand = new MySqlCommand($"ALTER TABLE resident ADD COLUMN {columnName} {definition};", conn);
		mySqlCommand.ExecuteNonQuery();
		existing.Add(columnName);
	}





	private void EnsureResidentLocationLookups()
	{
		if (_residentLocationLoaded)
		{
			return;
		}

		bool previous = _suppressLocationEvents;
		_suppressLocationEvents = true;
		try
		{
			using var conn = OpenLookupConnection();
			var barangays = LoadLookupItems(conn, "SELECT barangay_id, name FROM barangay ORDER BY name");
			BindCombo(_editBarangay, barangays, includeNone: false);
			SelectComboById(_editBarangay, SchemaDefaults.DefaultBarangayId);
			int barangayId = GetSelectedLookupId(_editBarangay) ?? SchemaDefaults.DefaultBarangayId;
			ReloadPurokList(conn, barangayId, SchemaDefaults.DefaultPurokId);
			int? purokId = GetSelectedLookupId(_editPurok);
			ReloadHouseholdList(conn, barangayId, purokId, null);
			_residentLocationLoaded = true;
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load barangay data.", "Warning");
		}
		finally
		{
			_suppressLocationEvents = previous;
		}
	}


	private void EnsureBlotterSchema()


	{


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();


			mySqlConnection.Open();


			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


			using (MySqlCommand mySqlCommand = new MySqlCommand("SELECT COLUMN_NAME\r\n                             FROM INFORMATION_SCHEMA.COLUMNS\r\n                             WHERE TABLE_SCHEMA = DATABASE()\r\n                               AND TABLE_NAME = 'case_record';", mySqlConnection))


			{


				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();


				while (mySqlDataReader.Read())


				{


					hashSet.Add(mySqlDataReader.GetString(0));


				}


			}


			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "complainant_id", "INT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "respondent_resident_id", "INT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "respondent_name", "VARCHAR(255) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "incident_type", "VARCHAR(100) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "incident_time", "TIME NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "incident_location", "VARCHAR(255) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "witness_names", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "action_taken", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "resolution_details", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "incident_details", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "recorded_by", "INT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_summary", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_key_points", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_category", "VARCHAR(150) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_category_confidence", "DECIMAL(5,4) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_risk_level", "VARCHAR(20) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_risk_score", "INT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_risk_reasons", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_entities", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_recommended_next_action", "TEXT NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_model", "VARCHAR(100) NULL");
			AddBlotterColumnIfMissing(mySqlConnection, hashSet, "ai_processed_at", "DATETIME NULL");

			_supportsRespondentResidentId = true;
			_supportsBlotterExtended = true;


		}


		catch


		{


			_supportsRespondentResidentId = false;
			_supportsBlotterExtended = false;


		}


		UpdateHistoryDetail();

	}

	private void EnsureDocumentNumberSequenceSchema(MySqlConnection conn)
	{
		using MySqlCommand cmd = new MySqlCommand(
			@"CREATE TABLE IF NOT EXISTS document_number_sequence (
                    doc_type_id INT NOT NULL,
                    year INT NOT NULL,
                    last_no INT NOT NULL DEFAULT 0,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    PRIMARY KEY (doc_type_id, year),
                    FOREIGN KEY (doc_type_id) REFERENCES document_type(doc_type_id) ON DELETE CASCADE
                );",
			conn);
		cmd.ExecuteNonQuery();
	}

	private static void EnsureDocumentPaymentOrNumberIndex(MySqlConnection conn)
	{
		// Best-effort: prefer a unique OR number for payments, but don't break startup if legacy data has duplicates.
		try
		{
			using var exists = new MySqlCommand(
				@"SELECT COUNT(*)
                  FROM INFORMATION_SCHEMA.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'document_payment'
                    AND INDEX_NAME IN ('ux_document_payment_or_no', 'idx_document_payment_or_no')",
				conn);
			int count = Convert.ToInt32(exists.ExecuteScalar() ?? 0);
			if (count > 0)
			{
				return;
			}

			try
			{
				using var createUnique = new MySqlCommand(
					"CREATE UNIQUE INDEX ux_document_payment_or_no ON document_payment (or_no)",
					conn);
				createUnique.ExecuteNonQuery();
			}
			catch
			{
				using var create = new MySqlCommand(
					"CREATE INDEX idx_document_payment_or_no ON document_payment (or_no)",
					conn);
				create.ExecuteNonQuery();
			}
		}
		catch
		{
			// Ignore: table might not exist yet or user might not have index permissions.
		}
	}




	private static void AddBlotterColumnIfMissing(MySqlConnection conn, HashSet<string> existing, string columnName, string definition)
	{
		if (existing.Contains(columnName))
		{
			return;
		}

		using MySqlCommand mySqlCommand = new MySqlCommand($"ALTER TABLE case_record ADD COLUMN {columnName} {definition};", conn);
		mySqlCommand.ExecuteNonQuery();
	}


	private static MySqlConnection OpenLookupConnection()
	{
		var conn = DBConnection.GetConnection();
		conn.Open();
		SchemaBootstrap.EnsureCoreDefaults(conn);
		return conn;
	}


	private static List<LookupItem> LoadLookupItems(MySqlConnection conn, string sql, params MySqlParameter[] parameters)
	{
		var items = new List<LookupItem>();
		using var cmd = new MySqlCommand(sql, conn);
		if (parameters != null && parameters.Length > 0)
		{
			cmd.Parameters.AddRange(parameters);
		}

		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			int id = reader.GetInt32(0);
			string name = reader.IsDBNull(1) ? $"#{id}" : reader.GetString(1);
			items.Add(new LookupItem(id, name));
		}
		return items;
	}


	private static void BindCombo(ComboBox comboBox, List<LookupItem> items, bool includeNone)
	{
		var data = includeNone ? new List<LookupItem> { new LookupItem(0, "(None)") } : new List<LookupItem>();
		data.AddRange(items);

		comboBox.DataSource = null;
		comboBox.DisplayMember = nameof(LookupItem.Name);
		comboBox.ValueMember = nameof(LookupItem.Id);
		comboBox.DataSource = data;
	}


	private void ReloadPurokList(MySqlConnection conn, int barangayId, int? selectedId)
	{
		var puroks = LoadLookupItems(conn,
			"SELECT purok_id, name FROM purok_sitio WHERE barangay_id = @barangayId ORDER BY name",
			new MySqlParameter("@barangayId", barangayId));
		BindCombo(_editPurok, puroks, includeNone: false);
		SelectComboById(_editPurok, selectedId ?? SchemaDefaults.DefaultPurokId);
	}


	private void ReloadHouseholdList(MySqlConnection conn, int barangayId, int? purokId, int? selectedId)
	{
		string sql = @"SELECT household_id,
                              COALESCE(NULLIF(TRIM(CONCAT_WS(' ', house_no, street, subdivision)), ''), CONCAT('Household #', household_id)) AS label
                       FROM household
                       WHERE barangay_id = @barangayId
                         AND (@purokId IS NULL OR purok_id = @purokId)
                       ORDER BY household_id";
		var households = LoadLookupItems(conn, sql,
			new MySqlParameter("@barangayId", barangayId),
			new MySqlParameter("@purokId", (object?)purokId ?? DBNull.Value));
		BindCombo(_editHousehold, households, includeNone: true);
		SelectComboById(_editHousehold, selectedId);
	}


	private static int? GetSelectedLookupId(ComboBox comboBox)
	{
		if (comboBox.SelectedValue is int idValue)
		{
			return idValue == 0 ? (int?)null : idValue;
		}

		if (comboBox.SelectedItem is LookupItem item)
		{
			return item.Id == 0 ? (int?)null : item.Id;
		}

		return null;
	}


	private static void SelectComboById(ComboBox comboBox, int? id)
	{
		if (comboBox.Items.Count == 0)
		{
			return;
		}

		if (id.HasValue)
		{
			comboBox.SelectedValue = id.Value;
			if (comboBox.SelectedIndex >= 0)
			{
				return;
			}
		}

		comboBox.SelectedIndex = 0;
	}


	private void ResidentBarangayChanged(object? sender, EventArgs e)
	{
		if (_suppressLocationEvents)
		{
			return;
		}

		try
		{
			using var conn = OpenLookupConnection();
			int barangayId = GetSelectedLookupId(_editBarangay) ?? SchemaDefaults.DefaultBarangayId;
			ReloadPurokList(conn, barangayId, null);
			int? purokId = GetSelectedLookupId(_editPurok);
			ReloadHouseholdList(conn, barangayId, purokId, null);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load purok list.", "Warning");
		}
	}


	private void ResidentPurokChanged(object? sender, EventArgs e)
	{
		if (_suppressLocationEvents)
		{
			return;
		}

		try
		{
			using var conn = OpenLookupConnection();
			int barangayId = GetSelectedLookupId(_editBarangay) ?? SchemaDefaults.DefaultBarangayId;
			int? purokId = GetSelectedLookupId(_editPurok);
			ReloadHouseholdList(conn, barangayId, purokId, null);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load household list.", "Warning");
		}
	}



	private void EnsureCertificateColumns(MySqlConnection conn)


	{


		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


		using (MySqlCommand mySqlCommand = new MySqlCommand("SELECT COLUMN_NAME\r\n                         FROM INFORMATION_SCHEMA.COLUMNS\r\n                         WHERE TABLE_SCHEMA = DATABASE()\r\n                           AND TABLE_NAME = 'document_request';", conn))


		{


			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();


			while (mySqlDataReader.Read())


			{


				hashSet.Add(mySqlDataReader.GetString(0));


			}


		}


		AddCertificateColumnIfMissing(conn, hashSet, "document_no", "VARCHAR(50) NULL");


		AddCertificateColumnIfMissing(conn, hashSet, "status", "ENUM('DRAFT','SUBMITTED','APPROVED','RELEASED','REJECTED','CANCELLED') NOT NULL DEFAULT 'SUBMITTED'");


		AddCertificateColumnIfMissing(conn, hashSet, "fee", "DECIMAL(10,2) DEFAULT 0");


		AddCertificateColumnIfMissing(conn, hashSet, "or_number", "VARCHAR(100) NULL");


		AddCertificateColumnIfMissing(conn, hashSet, "business_name", "VARCHAR(255) NULL");


		AddCertificateColumnIfMissing(conn, hashSet, "business_nature", "VARCHAR(255) NULL");


		AddCertificateColumnIfMissing(conn, hashSet, "print_count", "INT NOT NULL DEFAULT 0");


		AddCertificateColumnIfMissing(conn, hashSet, "last_printed_at", "DATETIME NULL");


	}





	private static void AddCertificateColumnIfMissing(MySqlConnection conn, HashSet<string> existing, string columnName, string definition)


	{


		if (existing.Contains(columnName))


		{


			return;


		}


	using MySqlCommand mySqlCommand = new MySqlCommand($"ALTER TABLE document_request ADD COLUMN {columnName} {definition};", conn);


		mySqlCommand.ExecuteNonQuery();


	}





	private void EnsureCertificateAuditSchema(MySqlConnection conn)


	{


		using MySqlCommand mySqlCommand = new MySqlCommand("CREATE TABLE IF NOT EXISTS certificate_audit (\r\n                    audit_id INT AUTO_INCREMENT PRIMARY KEY,\r\n                    certificate_id INT NOT NULL,\r\n                    action VARCHAR(50) NOT NULL,\r\n                    action_by INT NULL,\r\n                    action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    notes VARCHAR(255) NULL,\r\n                    INDEX idx_audit_cert (certificate_id)\r\n                );", conn);


		mySqlCommand.ExecuteNonQuery();


	}





	private void EnsureActivityLogSchema(MySqlConnection conn)


	{


		using MySqlCommand mySqlCommand = new MySqlCommand("CREATE TABLE IF NOT EXISTS activity_log (\r\n                    log_id INT AUTO_INCREMENT PRIMARY KEY,\r\n                    resident_id INT NOT NULL,\r\n                    module VARCHAR(40) NOT NULL,\r\n                    action VARCHAR(50) NOT NULL,\r\n                    details VARCHAR(255) NULL,\r\n                    action_by INT NULL,\r\n                    action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,\r\n                    INDEX idx_activity_resident (resident_id),\r\n                    INDEX idx_activity_module (module)\r\n                );", conn);


		mySqlCommand.ExecuteNonQuery();


	}





	private void add_Click(object sender, EventArgs e)


	{

		_controller.HandleAddResident(sender, e);

	}





	private void button2_Click(object sender, EventArgs e)


	{

		_controller.HandleUpdateResident(sender, e);

	}





	private void button1_Click(object sender, EventArgs e)


	{

		_controller.HandleRefreshResidents(sender, e);

	}





	private void button3_Click(object sender, EventArgs e)


	{

		_controller.HandleDeleteResident(sender, e);

	}

	private void ResidentRestore_Click(object? sender, EventArgs e)
	{
		_controller.HandleRestoreResident(sender, e);
	}

	private void ResidentShowDeletedToggle_CheckedChanged(object? sender, EventArgs e)
	{
		_showDeletedResidents = _residentShowDeletedToggle.Checked;
		LoadResidents();
	}





	private ResidentDto? GetSelectedResident()


	{


		if (dgvResidents.Columns["resident_id"] == null)


		{


			ControllerDialogs.Warning("Please load the Residents list first (click Refresh).", "Nothing to edit");


			return null;


		}


		if (dgvResidents.SelectedRows.Count == 0)


		{


			ControllerDialogs.Warning("Please select a resident row first.", "Nothing selected");


			return null;


		}


		DataGridViewRow dataGridViewRow = dgvResidents.SelectedRows[0];


		ResidentDto residentDto = new ResidentDto();


		residentDto.Id = Convert.ToInt32(dataGridViewRow.Cells["resident_id"].Value);


		residentDto.FirstName = dataGridViewRow.Cells["firstname"].Value?.ToString() ?? string.Empty;


		residentDto.MiddleName = dataGridViewRow.Cells["middlename"].Value?.ToString() ?? string.Empty;


		residentDto.LastName = dataGridViewRow.Cells["lastname"].Value?.ToString() ?? string.Empty;


		residentDto.Gender = dataGridViewRow.Cells["gender"].Value?.ToString() ?? string.Empty;


		residentDto.DateOfBirth = SafeDate(dataGridViewRow.Cells["date_of_birth"].Value);


		residentDto.CivilStatus = dataGridViewRow.Cells["civil_status"].Value?.ToString() ?? string.Empty;


		residentDto.ContactNo = dataGridViewRow.Cells["contact_no"].Value?.ToString() ?? string.Empty;


		residentDto.Status = dataGridViewRow.Cells["status"].Value?.ToString() ?? string.Empty;


			residentDto.PhotoBytes = _residentPhotoPendingBytes ?? (_residentPhotoRemoved ? null : _residentPhotoBytes);
			residentDto.BarangayId = GetCellNullableInt(dataGridViewRow, "barangay_id");
			residentDto.PurokId = GetCellNullableInt(dataGridViewRow, "purok_id");
			residentDto.HouseholdId = GetCellNullableInt(dataGridViewRow, "household_id");


		return residentDto;


	}





	private static DateTime SafeDate(object? value)


	{


		if (value is DateTime dateTime)


		{


			return dateTime.Date;


		}


		DateTime result;


		return DateTime.TryParse(Convert.ToString(value), out result) ? result.Date : DateTime.Today;


	}


	private static string MapResidentGenderToDb(string gender)
	{
		if (string.IsNullOrWhiteSpace(gender))
		{
			return "M";
		}

		gender = gender.Trim();
		if (gender.StartsWith("M", StringComparison.OrdinalIgnoreCase))
		{
			return "M";
		}
		if (gender.StartsWith("F", StringComparison.OrdinalIgnoreCase))
		{
			return "F";
		}

		return "M";
	}


	private static string MapResidentStatusToDb(string status)
	{
		if (string.IsNullOrWhiteSpace(status))
		{
			return "ACTIVE";
		}

		if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
		{
			return "ACTIVE";
		}
		if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
		{
			return "MOVED_OUT";
		}
		if (status.Equals("Deceased", StringComparison.OrdinalIgnoreCase))
		{
			return "DECEASED";
		}

		return status.ToUpperInvariant();
	}


	private static string MapCertificateStatusToDb(string status)
	{
		return WorkflowRules.NormalizeCertificateStatus(status);
	}


	private static string MapBlotterStatusToDb(string status)
	{
		return WorkflowRules.NormalizeBlotterStatus(status);
	}


	private static int GetOrCreateDocumentTypeId(MySqlConnection conn, string? typeName, MySqlTransaction? tx = null)
	{
		string name = string.IsNullOrWhiteSpace(typeName) ? "Other" : typeName.Trim();
		using var select = new MySqlCommand("SELECT doc_type_id FROM document_type WHERE name = @name LIMIT 1", conn);
		select.Transaction = tx;
		select.Parameters.AddWithValue("@name", name);
		object? existing = select.ExecuteScalar();
		if (existing != null && existing != DBNull.Value)
		{
			return Convert.ToInt32(existing);
		}

		string code = name switch
		{
			"Barangay Clearance" => "BC",
			"Certificate of Residency" => "CR",
			"Indigency" => "IND",
			"Business Clearance" => "BUS",
			_ => "DOC"
		};

		using var insert = new MySqlCommand("INSERT INTO document_type (name, code, requires_approval) VALUES (@name, @code, 1)", conn);
		insert.Transaction = tx;
		insert.Parameters.AddWithValue("@name", name);
		insert.Parameters.AddWithValue("@code", code);
		insert.ExecuteNonQuery();
		return (int)insert.LastInsertedId;
	}


	private static int GetOrCreateCaseTypeId(MySqlConnection conn, string? typeName, MySqlTransaction? tx = null)
	{
		string name = string.IsNullOrWhiteSpace(typeName) ? "Other" : typeName.Trim();
		using var select = new MySqlCommand("SELECT case_type_id FROM case_type WHERE name = @name LIMIT 1", conn);
		select.Transaction = tx;
		select.Parameters.AddWithValue("@name", name);
		object? existing = select.ExecuteScalar();
		if (existing != null && existing != DBNull.Value)
		{
			return Convert.ToInt32(existing);
		}

		using var insert = new MySqlCommand("INSERT INTO case_type (name) VALUES (@name)", conn);
		insert.Transaction = tx;
		insert.Parameters.AddWithValue("@name", name);
		insert.ExecuteNonQuery();
		return (int)insert.LastInsertedId;
	}

	private static object? ReadResidentAuditSnapshot(MySqlConnection conn, int residentId, MySqlTransaction? tx = null)
	{
		using var cmd = new MySqlCommand(
			@"SELECT resident_id, barangay_id, purok_id, household_id, first_name, middle_name, last_name,
	                     sex, birth_date, civil_status, contact_no, status,
	                     IFNULL(is_deleted,0) AS is_deleted, deleted_at, deleted_by_user_id, delete_reason
              FROM resident
              WHERE resident_id=@id
              LIMIT 1", conn);
		cmd.Transaction = tx;
		cmd.Parameters.AddWithValue("@id", residentId);
		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			return null;
		}

		return new
		{
			ResidentId = Convert.ToInt32(reader["resident_id"]),
			BarangayId = reader["barangay_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["barangay_id"]),
			PurokId = reader["purok_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["purok_id"]),
			HouseholdId = reader["household_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["household_id"]),
			FirstName = Convert.ToString(reader["first_name"]) ?? string.Empty,
			MiddleName = Convert.ToString(reader["middle_name"]) ?? string.Empty,
			LastName = Convert.ToString(reader["last_name"]) ?? string.Empty,
			Sex = Convert.ToString(reader["sex"]) ?? string.Empty,
			BirthDate = reader["birth_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["birth_date"]),
			CivilStatus = Convert.ToString(reader["civil_status"]) ?? string.Empty,
			ContactNo = Convert.ToString(reader["contact_no"]) ?? string.Empty,
			Status = Convert.ToString(reader["status"]) ?? string.Empty,
			IsDeleted = Convert.ToInt32(reader["is_deleted"]) == 1,
			DeletedAt = reader["deleted_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["deleted_at"]),
			DeletedByUserId = reader["deleted_by_user_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["deleted_by_user_id"]),
			DeleteReason = Convert.ToString(reader["delete_reason"]) ?? string.Empty
		};
	}

	private static object? ReadCertificateAuditSnapshot(MySqlConnection conn, int certificateId, MySqlTransaction? tx = null)
	{
		using var cmd = new MySqlCommand(
			@"SELECT doc_request_id, resident_id, status, purpose, fee, or_number, document_no,
	                     verification_token, verification_token_created_at,
	                     requested_at, approved_at, released_at, business_name, business_nature, remarks
              FROM document_request
              WHERE doc_request_id=@id
              LIMIT 1", conn);
		cmd.Transaction = tx;
		cmd.Parameters.AddWithValue("@id", certificateId);
		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			return null;
		}

		return new
		{
			CertificateId = Convert.ToInt32(reader["doc_request_id"]),
			ResidentId = reader["resident_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["resident_id"]),
			Status = Convert.ToString(reader["status"]) ?? string.Empty,
			Purpose = Convert.ToString(reader["purpose"]) ?? string.Empty,
			Fee = reader["fee"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["fee"]),
			OrNumber = Convert.ToString(reader["or_number"]) ?? string.Empty,
			DocumentNo = Convert.ToString(reader["document_no"]) ?? string.Empty,
			VerificationToken = Convert.ToString(reader["verification_token"]) ?? string.Empty,
			VerificationTokenCreatedAt = reader["verification_token_created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["verification_token_created_at"]),
			RequestedAt = reader["requested_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["requested_at"]),
			ApprovedAt = reader["approved_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["approved_at"]),
			ReleasedAt = reader["released_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["released_at"]),
			BusinessName = Convert.ToString(reader["business_name"]) ?? string.Empty,
			BusinessNature = Convert.ToString(reader["business_nature"]) ?? string.Empty,
			Remarks = Convert.ToString(reader["remarks"]) ?? string.Empty
		};
	}





	private int InsertResident(ResidentDto resident)


	{
		if (!Permissions.CanCreateResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to add residents.");
		}

		var householdValidation = ValidationService.ValidateHouseholdConsistency(resident, resident.Id);
		if (!householdValidation.IsValid)
		{
			throw new InvalidOperationException(householdValidation.Message);
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		SchemaBootstrap.EnsureCoreDefaults(mySqlConnection);
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();


		using MySqlCommand mySqlCommand = new MySqlCommand("INSERT INTO resident\r\n                                    (barangay_id, purok_id, household_id, first_name, middle_name, last_name, sex,\r\n                                     birth_date, civil_status, contact_no, status, photo)\r\n                                   VALUES\r\n                                    (@barangayId, @purokId, @householdId, @fn, @mn, @ln, @sex, @dob, @civil, @contact, @status, @photo)", mySqlConnection);
		mySqlCommand.Transaction = tx;


		int barangayId = resident.BarangayId ?? SchemaDefaults.DefaultBarangayId;
		int purokId = resident.PurokId ?? SchemaDefaults.DefaultPurokId;
		mySqlCommand.Parameters.AddWithValue("@barangayId", barangayId);
		mySqlCommand.Parameters.AddWithValue("@purokId", purokId);
		mySqlCommand.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? resident.HouseholdId.Value : (object)DBNull.Value);
		mySqlCommand.Parameters.AddWithValue("@fn", resident.FirstName);
		mySqlCommand.Parameters.AddWithValue("@mn", resident.MiddleName);
		mySqlCommand.Parameters.AddWithValue("@ln", resident.LastName);
		mySqlCommand.Parameters.AddWithValue("@sex", MapResidentGenderToDb(resident.Gender));
		mySqlCommand.Parameters.AddWithValue("@dob", resident.DateOfBirth);
		mySqlCommand.Parameters.AddWithValue("@civil", resident.CivilStatus);
		mySqlCommand.Parameters.AddWithValue("@contact", resident.ContactNo);
		mySqlCommand.Parameters.AddWithValue("@status", MapResidentStatusToDb(resident.Status));


		MySqlParameter mySqlParameter = mySqlCommand.Parameters.Add("@photo", MySqlDbType.LongBlob);


		mySqlParameter.Value = ((object)resident.PhotoBytes) ?? ((object)DBNull.Value);


		mySqlCommand.ExecuteNonQuery();


		int num = (int)mySqlCommand.LastInsertedId;


		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Residents",
			"resident",
			num,
			"CREATE",
			null,
			new
			{
				ResidentId = num,
				resident.FirstName,
				resident.MiddleName,
				resident.LastName,
				resident.Gender,
				resident.DateOfBirth,
				resident.CivilStatus,
				resident.ContactNo,
				resident.Status,
				resident.BarangayId,
				resident.PurokId,
				resident.HouseholdId
			});
		tx.Commit();
		LogActivity(num, "Residents", "Created", (resident.FirstName + " " + resident.LastName).Trim());


		return num;


	}





	private void UpdateResident(ResidentDto resident)


	{
		if (!Permissions.CanUpdateResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to update residents.");
		}

		var householdValidation = ValidationService.ValidateHouseholdConsistency(resident, resident.Id);
		if (!householdValidation.IsValid)
		{
			throw new InvalidOperationException(householdValidation.Message);
		}


		if (!resident.Id.HasValue)


		{


			throw new InvalidOperationException("Missing resident id for update.");


		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();
		object? beforeSnapshot = ReadResidentAuditSnapshot(mySqlConnection, resident.Id.Value, tx);
		ResidentLocationSnapshot? beforeLocation = LoadResidentLocationSnapshot(mySqlConnection, tx, resident.Id.Value);


		using MySqlCommand mySqlCommand = new MySqlCommand("UPDATE resident\r\n                                   SET barangay_id=@barangayId, purok_id=@purokId, household_id=@householdId,\r\n                                       first_name=@fn, middle_name=@mn, last_name=@ln,\r\n                                       sex=@sex, birth_date=@dob,\r\n                                       civil_status=@civil, contact_no=@contact, status=@status,\r\n                                       photo=@photo\r\n                                   WHERE resident_id=@id", mySqlConnection);
		mySqlCommand.Transaction = tx;


		int barangayId = resident.BarangayId ?? SchemaDefaults.DefaultBarangayId;
		int purokId = resident.PurokId ?? SchemaDefaults.DefaultPurokId;
		mySqlCommand.Parameters.AddWithValue("@barangayId", barangayId);
		mySqlCommand.Parameters.AddWithValue("@purokId", purokId);
		mySqlCommand.Parameters.AddWithValue("@householdId", resident.HouseholdId.HasValue ? resident.HouseholdId.Value : (object)DBNull.Value);
		mySqlCommand.Parameters.AddWithValue("@fn", resident.FirstName);
		mySqlCommand.Parameters.AddWithValue("@mn", resident.MiddleName);
		mySqlCommand.Parameters.AddWithValue("@ln", resident.LastName);
		mySqlCommand.Parameters.AddWithValue("@sex", MapResidentGenderToDb(resident.Gender));
		mySqlCommand.Parameters.AddWithValue("@dob", resident.DateOfBirth);
		mySqlCommand.Parameters.AddWithValue("@civil", resident.CivilStatus);
		mySqlCommand.Parameters.AddWithValue("@contact", resident.ContactNo);
		mySqlCommand.Parameters.AddWithValue("@status", MapResidentStatusToDb(resident.Status));


		MySqlParameter mySqlParameter = mySqlCommand.Parameters.Add("@photo", MySqlDbType.LongBlob);


		mySqlParameter.Value = ((object)resident.PhotoBytes) ?? ((object)DBNull.Value);


		mySqlCommand.Parameters.AddWithValue("@id", resident.Id.Value);


		mySqlCommand.ExecuteNonQuery();

		string? transferDetails = null;
		int newPurokId = resident.PurokId ?? SchemaDefaults.DefaultPurokId;
		int? newHouseholdId = resident.HouseholdId;
		if (beforeLocation != null &&
		    (beforeLocation.PurokId != newPurokId || beforeLocation.HouseholdId != newHouseholdId))
		{
			string newAddress = ResolveResidentAddressLabel(mySqlConnection, tx, newPurokId, newHouseholdId);
			InsertResidentTransferHistory(
				mySqlConnection,
				tx,
				resident.Id.Value,
				beforeLocation,
				newPurokId,
				newHouseholdId,
				newAddress,
				"Profile location updated");

			string oldAddress = string.IsNullOrWhiteSpace(beforeLocation.AddressLabel) ? "-" : beforeLocation.AddressLabel;
			transferDetails = $"From: {oldAddress} | To: {newAddress}";
		}


		object? afterSnapshot = ReadResidentAuditSnapshot(mySqlConnection, resident.Id.Value, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Residents",
			"resident",
			resident.Id.Value,
			"UPDATE",
			beforeSnapshot,
			afterSnapshot,
			"Profile updated");
		tx.Commit();
		LogActivity(resident.Id.Value, "Residents", "Updated", "Profile updated");
		if (!string.IsNullOrWhiteSpace(transferDetails))
		{
			LogActivity(resident.Id.Value, "Residents", "Transferred", transferDetails);
		}


	}

	private sealed class ResidentLocationSnapshot
	{
		public int? PurokId { get; init; }
		public int? HouseholdId { get; init; }
		public string AddressLabel { get; init; } = string.Empty;
	}

	private static ResidentLocationSnapshot? LoadResidentLocationSnapshot(MySqlConnection conn, MySqlTransaction tx, int residentId)
	{
		using var cmd = new MySqlCommand(
			@"SELECT r.purok_id,
                     r.household_id,
                     p.name AS purok_name,
                     h.house_no,
                     h.street,
                     h.subdivision
              FROM resident r
              LEFT JOIN purok_sitio p ON p.purok_id = r.purok_id
              LEFT JOIN household h ON h.household_id = r.household_id
              WHERE r.resident_id = @id
              LIMIT 1
              FOR UPDATE",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@id", residentId);

		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			return null;
		}

		int? purokId = reader["purok_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["purok_id"]);
		int? householdId = reader["household_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["household_id"]);
		string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
		string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
		string street = Convert.ToString(reader["street"]) ?? string.Empty;
		string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;

		return new ResidentLocationSnapshot
		{
			PurokId = purokId,
			HouseholdId = householdId,
			AddressLabel = BuildAddressLabel(purokName, householdId, houseNo, street, subdivision)
		};
	}

	private static string ResolveResidentAddressLabel(MySqlConnection conn, MySqlTransaction tx, int purokId, int? householdId)
	{
		using var cmd = new MySqlCommand(
			@"SELECT p.name AS purok_name,
                     h.house_no,
                     h.street,
                     h.subdivision
              FROM purok_sitio p
              LEFT JOIN household h ON h.household_id = @householdId
              WHERE p.purok_id = @purokId
              LIMIT 1",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@purokId", purokId);
		cmd.Parameters.AddWithValue("@householdId", householdId.HasValue ? householdId.Value : (object)DBNull.Value);

		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			string fallbackHouse = householdId.HasValue ? $"Household #{householdId.Value}" : "-";
			return $"Purok #{purokId} | {fallbackHouse}";
		}

		string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
		string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
		string street = Convert.ToString(reader["street"]) ?? string.Empty;
		string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;
		return BuildAddressLabel(purokName, householdId, houseNo, street, subdivision);
	}

	private static string BuildAddressLabel(string purokName, int? householdId, string houseNo, string street, string subdivision)
	{
		string housePart = string.Join(" ", new[] { houseNo?.Trim(), street?.Trim(), subdivision?.Trim() }.Where(part => !string.IsNullOrWhiteSpace(part)));
		if (string.IsNullOrWhiteSpace(housePart) && householdId.HasValue)
		{
			housePart = $"Household #{householdId.Value}";
		}

		string purokPart = string.IsNullOrWhiteSpace(purokName) ? string.Empty : purokName.Trim();
		if (!string.IsNullOrWhiteSpace(housePart) && !string.IsNullOrWhiteSpace(purokPart))
		{
			return housePart + ", " + purokPart;
		}

		if (!string.IsNullOrWhiteSpace(housePart))
		{
			return housePart;
		}

		if (!string.IsNullOrWhiteSpace(purokPart))
		{
			return purokPart;
		}

		return "-";
	}

	private static void InsertResidentTransferHistory(
		MySqlConnection conn,
		MySqlTransaction tx,
		int residentId,
		ResidentLocationSnapshot beforeLocation,
		int newPurokId,
		int? newHouseholdId,
		string newAddress,
		string reason)
	{
		using var cmd = new MySqlCommand(
			@"INSERT INTO resident_transfer_history
                (resident_id, old_purok_id, old_household_id, old_address,
                 new_purok_id, new_household_id, new_address, transfer_reason,
                 transferred_by_user_id, transferred_at)
              VALUES
                (@residentId, @oldPurokId, @oldHouseholdId, @oldAddress,
                 @newPurokId, @newHouseholdId, @newAddress, @reason,
                 @userId, NOW())",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@residentId", residentId);
		cmd.Parameters.AddWithValue("@oldPurokId", beforeLocation.PurokId.HasValue ? beforeLocation.PurokId.Value : (object)DBNull.Value);
		cmd.Parameters.AddWithValue("@oldHouseholdId", beforeLocation.HouseholdId.HasValue ? beforeLocation.HouseholdId.Value : (object)DBNull.Value);
		cmd.Parameters.AddWithValue("@oldAddress", string.IsNullOrWhiteSpace(beforeLocation.AddressLabel) ? DBNull.Value : beforeLocation.AddressLabel);
		cmd.Parameters.AddWithValue("@newPurokId", newPurokId);
		cmd.Parameters.AddWithValue("@newHouseholdId", newHouseholdId.HasValue ? newHouseholdId.Value : (object)DBNull.Value);
		cmd.Parameters.AddWithValue("@newAddress", string.IsNullOrWhiteSpace(newAddress) ? DBNull.Value : newAddress);
		cmd.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason);
		cmd.Parameters.AddWithValue("@userId", UserSession.UserId > 0 ? UserSession.UserId : (object)DBNull.Value);
		cmd.ExecuteNonQuery();
	}





	private void DeleteResident(int residentId, string reason)


	{
		if (!Permissions.CanDeleteResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to delete residents.");
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();
		object? beforeSnapshot = ReadResidentAuditSnapshot(mySqlConnection, residentId, tx);


		using MySqlCommand mySqlCommand = new MySqlCommand(
			@"UPDATE resident
              SET is_deleted=1,
                  deleted_at=NOW(),
                  deleted_by_user_id=@by,
                  delete_reason=@reason
              WHERE resident_id=@id", mySqlConnection);
		mySqlCommand.Transaction = tx;


		mySqlCommand.Parameters.AddWithValue("@id", residentId);
		mySqlCommand.Parameters.AddWithValue("@by", UserSession.UserId);
		mySqlCommand.Parameters.AddWithValue("@reason", reason);


		int affected = mySqlCommand.ExecuteNonQuery();
		if (affected <= 0)
		{
			throw new InvalidOperationException("Resident not found.");
		}

		object? afterSnapshot = ReadResidentAuditSnapshot(mySqlConnection, residentId, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Residents",
			"resident",
			residentId,
			"SOFT_DELETE",
			beforeSnapshot,
			afterSnapshot,
			reason);
		tx.Commit();


	}

	private void RestoreResident(int residentId)
	{
		if (!Permissions.CanDeleteResidents)
		{
			throw new UnauthorizedAccessException("You do not have permission to restore residents.");
		}

		using MySqlConnection mySqlConnection = DBConnection.GetConnection();
		mySqlConnection.Open();
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();
		object? beforeSnapshot = ReadResidentAuditSnapshot(mySqlConnection, residentId, tx);

		using MySqlCommand mySqlCommand = new MySqlCommand(
			@"UPDATE resident
              SET is_deleted=0,
                  deleted_at=NULL,
                  deleted_by_user_id=NULL,
                  delete_reason=NULL
              WHERE resident_id=@id", mySqlConnection);
		mySqlCommand.Transaction = tx;
		mySqlCommand.Parameters.AddWithValue("@id", residentId);

		int affected = mySqlCommand.ExecuteNonQuery();
		if (affected <= 0)
		{
			throw new InvalidOperationException("Resident not found.");
		}

		object? afterSnapshot = ReadResidentAuditSnapshot(mySqlConnection, residentId, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Residents",
			"resident",
			residentId,
			"RESTORE",
			beforeSnapshot,
			afterSnapshot);
		tx.Commit();
	}





	private void DgvResidents_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)


	{


		if (e.RowIndex < 0 || e.ColumnIndex < 0)


		{


			return;


		}


		if (dgvResidents.Columns[e.ColumnIndex] is not DataGridViewColumn column)


		{


			return;


		}


		bool isResidentStatusColumn = string.Equals(column.Name, "status", StringComparison.OrdinalIgnoreCase) && IsResidentView();
		bool isUserStatusColumn = string.Equals(column.Name, "is_active", StringComparison.OrdinalIgnoreCase);
		if (!isResidentStatusColumn && !isUserStatusColumn)


		{


			return;


		}


		string statusText = Convert.ToString(e.Value)?.Trim() ?? string.Empty;
		if (isResidentStatusColumn)
		{
			Color dotColor = Color.FromArgb(107, 114, 128);
			if (statusText.Equals("Active", StringComparison.OrdinalIgnoreCase))
			{
				dotColor = Color.FromArgb(5, 150, 105);
			}
			else if (statusText.Equals("Deceased", StringComparison.OrdinalIgnoreCase))
			{
				dotColor = Color.FromArgb(220, 38, 38);
			}

			e.Value = "●";
			e.FormattingApplied = true;
			e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			e.CellStyle.ForeColor = dotColor;
			e.CellStyle.SelectionForeColor = dotColor;
			e.CellStyle.BackColor = Color.White;
			e.CellStyle.SelectionBackColor = Color.FromArgb(225, 238, 255);
			return;
		}

		bool isActive = string.Equals(statusText, "1", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(statusText, "true", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(statusText, "active", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(statusText, "yes", StringComparison.OrdinalIgnoreCase);

		e.Value = isActive ? "Active" : "Inactive";
		e.FormattingApplied = true;
		e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
		e.CellStyle.SelectionBackColor = isActive ? Color.FromArgb(210, 245, 220) : Color.FromArgb(235, 236, 240);
		e.CellStyle.SelectionForeColor = isActive ? Color.FromArgb(0, 97, 54) : Color.FromArgb(77, 85, 102);
		e.CellStyle.BackColor = isActive ? Color.FromArgb(210, 245, 220) : Color.FromArgb(235, 236, 240);
		e.CellStyle.ForeColor = isActive ? Color.FromArgb(0, 97, 54) : Color.FromArgb(77, 85, 102);


	}





	private void DgvResidents_SelectionChanged(object? sender, EventArgs e)

	{
		if (_suppressResidentSelectionChanged)
		{
			return;
		}

		if (!_isEditing)


		{


			if (!IsResidentView())


			{


				ClearResidentDetails("Select a resident to view details.");

				UpdateResidentSoftDeleteButtons();

				return;


			}


			if (dgvResidents.SelectedRows.Count == 0)


			{


				ClearResidentDetails();

				UpdateResidentSoftDeleteButtons();

				return;


			}


			DataGridViewRow row = dgvResidents.SelectedRows[0];
			if (!TryGetResidentId(row, out int residentId))
			{
				ClearResidentDetails();
				UpdateResidentSoftDeleteButtons();
				return;
			}

			if (_residentDetailsLoadedId.HasValue
				&& _selectedResidentId.HasValue
				&& _residentDetailsLoadedId.Value == residentId
				&& _selectedResidentId.Value == residentId)
			{
				UpdateResidentSoftDeleteButtons();
				return;
			}

			PopulateResidentDetails(row);


		}

		UpdateRightPanelSelectionState();
		UpdateResidentSoftDeleteButtons();


	}





	private bool IsResidentView()


	{


		return dgvResidents.Columns["resident_id"] != null;


	}





	private void ConfigureResidentGridColumns()


	{


		foreach (DataGridViewColumn column in dgvResidents.Columns)


		{


			column.Visible = false;


		}


		ShowResidentColumn("firstname", "First", 0);
		ShowResidentColumn("middlename", "Middle", 1);
		ShowResidentColumn("lastname", "Last", 2);
		ShowResidentColumn("status", "\u25CF", 3);


		dgvResidents.ClearSelection();
		UpdateResidentListVisualState();


	}





	private void ShowResidentColumn(string columnName, string headerText, int displayIndex)


	{


		DataGridViewColumn dataGridViewColumn = dgvResidents.Columns[columnName];


		if (dataGridViewColumn != null)


		{


			dataGridViewColumn.Visible = true;


			dataGridViewColumn.HeaderText = headerText;


			dataGridViewColumn.DisplayIndex = displayIndex;
			if (string.Equals(columnName, "status", StringComparison.OrdinalIgnoreCase))
			{
				dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				dataGridViewColumn.Width = 50;
				dataGridViewColumn.MinimumWidth = 50;
				dataGridViewColumn.Resizable = DataGridViewTriState.False;
				dataGridViewColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
				dataGridViewColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
			}
			else
			{
				dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
				dataGridViewColumn.FillWeight = columnName switch
				{
					"firstname" => 35f,
					"middlename" => 25f,
					_ => 35f
				};
				dataGridViewColumn.MinimumWidth = columnName == "middlename" ? 82 : 92;
				dataGridViewColumn.Resizable = DataGridViewTriState.True;
			}


		}


	}





	private void ConfigureUserGridColumns()


	{


		foreach (DataGridViewColumn column in dgvResidents.Columns)


		{


			column.Visible = false;


		}


		ShowUserColumn("username", "Username", 0);


		ShowUserColumn("role", "Role", 1);


		ShowUserColumn("is_active", "Active", 2);


		dgvResidents.ClearSelection();
		UpdateResidentListVisualState();


	}





	private void ShowUserColumn(string columnName, string headerText, int displayIndex)


	{


		DataGridViewColumn dataGridViewColumn = dgvResidents.Columns[columnName];


		if (dataGridViewColumn != null)


		{


			dataGridViewColumn.Visible = true;


			dataGridViewColumn.HeaderText = headerText;


			dataGridViewColumn.DisplayIndex = displayIndex;


		}


	}




	private Control BuildResidentHeader()
	{
		_residentHeader.SuspendLayout();
		_residentHeader.Controls.Clear();
		_residentHeader.AutoSize = false;
		_residentHeader.AutoSizeMode = AutoSizeMode.GrowOnly;
		_residentHeader.Dock = DockStyle.Fill;
		_residentHeader.Margin = Padding.Empty;
		_residentHeader.Padding = new Padding(16);
		_residentHeader.BackColor = Color.White;

		TableLayoutPanel root = new TableLayoutPanel
		{
			ColumnCount = 3,
			RowCount = 1,
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			BackColor = Color.Transparent
		};
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		_residentHeaderPhoto.Dock = DockStyle.Fill;
		_residentHeaderPhoto.Margin = new Padding(0, 0, 12, 0);
		_residentHeaderPhoto.SizeMode = PictureBoxSizeMode.Zoom;
		_residentHeaderPhoto.BackColor = Color.FromArgb(238, 241, 247);
		_residentHeaderPhoto.MaximumSize = new Size(72, 72);
		_residentHeaderPhoto.MinimumSize = new Size(72, 72);
		_residentHeaderPhoto.Resize -= ResidentHeaderPhoto_Resize;
		_residentHeaderPhoto.Resize += ResidentHeaderPhoto_Resize;
		_residentHeaderPhoto.Cursor = Cursors.Hand;
		_residentHeaderPhotoMenu.ShowImageMargin = false;
		_residentHeaderPhotoMenu.Items.Clear();
		_residentHeaderPhotoMenu.Items.AddRange(new ToolStripItem[] { _residentHeaderPhotoChange, _residentHeaderPhotoRemove });
		_residentHeaderPhotoChange.Click -= ResidentPhotoUpload_Click;
		_residentHeaderPhotoChange.Click += ResidentPhotoUpload_Click;
		_residentHeaderPhotoRemove.Click -= ResidentPhotoRemove_Click;
		_residentHeaderPhotoRemove.Click += ResidentPhotoRemove_Click;
		_residentHeaderPhoto.ContextMenuStrip = _residentHeaderPhotoMenu;
		ApplyCircularPhotoMask(_residentHeaderPhoto);

		TableLayoutPanel center = new TableLayoutPanel
		{
			ColumnCount = 1,
			RowCount = 3,
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		center.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
		center.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
		center.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		FlowLayoutPanel titleRow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			AutoSize = false
		};

		_residentHeaderName.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point);
		_residentHeaderName.ForeColor = Color.FromArgb(22, 29, 49);
		_residentHeaderName.AutoSize = true;
		_residentHeaderName.Margin = new Padding(0, 1, 10, 0);
		_residentHeaderName.TextAlign = ContentAlignment.MiddleLeft;

		_residentHeaderStatus.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
		_residentHeaderStatus.AutoSize = true;
		_residentHeaderStatus.Padding = new Padding(10, 4, 10, 4);
		_residentHeaderStatus.Margin = new Padding(0, 6, 0, 0);
		_residentHeaderStatus.Text = "Active";

		_residentHeaderMeta.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
		_residentHeaderMeta.ForeColor = Color.FromArgb(95, 108, 130);
		_residentHeaderMeta.AutoEllipsis = true;
		_residentHeaderMeta.Dock = DockStyle.Fill;
		_residentHeaderMeta.Margin = new Padding(0, 0, 0, 0);
		_residentHeaderMeta.TextAlign = ContentAlignment.MiddleLeft;

		_residentHeaderAddress.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
		_residentHeaderAddress.ForeColor = Color.FromArgb(108, 119, 140);
		_residentHeaderAddress.AutoEllipsis = true;
		_residentHeaderAddress.Dock = DockStyle.Fill;
		_residentHeaderAddress.Margin = Padding.Empty;
		_residentHeaderAddress.TextAlign = ContentAlignment.TopLeft;

		titleRow.Controls.Add(_residentHeaderName);
		titleRow.Controls.Add(_residentHeaderStatus);
		center.Controls.Add(titleRow, 0, 0);
		center.Controls.Add(_residentHeaderMeta, 0, 1);
		center.Controls.Add(_residentHeaderAddress, 0, 2);

		_residentHeaderEditButton.Text = "Edit Profile";
		StyleResidentPrimaryButton(_residentHeaderEditButton, 150);
		_residentHeaderEditButton.AutoSize = false;
		_residentHeaderEditButton.Size = new Size(150, UiTheme.StandardButtonHeight);
		_residentHeaderEditButton.AutoEllipsis = false;
		_residentHeaderEditButton.Margin = Padding.Empty;
		_residentHeaderEditButton.Click -= ResidentQuickEdit_Click;
		_residentHeaderEditButton.Click += ResidentQuickEdit_Click;

		_residentHeaderPrintButton.Text = "Print";
		UiTheme.StyleSecondaryButton(_residentHeaderPrintButton);
		_residentHeaderPrintButton.AutoSize = false;
		_residentHeaderPrintButton.Size = new Size(90, UiTheme.StandardButtonHeight);
		_residentHeaderPrintButton.AutoEllipsis = false;
		_residentHeaderPrintButton.Margin = new Padding(8, 0, 0, 0);
		_residentHeaderPrintButton.Click -= ResidentHeaderPrintButton_Click;
		_residentHeaderPrintButton.Click += ResidentHeaderPrintButton_Click;

		_residentHeaderDeactivateButton.Text = "Deactivate";
		UiTheme.StyleSecondaryButton(_residentHeaderDeactivateButton);
		_residentHeaderDeactivateButton.AutoSize = false;
		_residentHeaderDeactivateButton.Size = new Size(150, UiTheme.StandardButtonHeight);
		_residentHeaderDeactivateButton.AutoEllipsis = false;
		_residentHeaderDeactivateButton.Margin = new Padding(8, 0, 0, 0);
		_residentHeaderDeactivateButton.ForeColor = ResidentDangerRed;
		_residentHeaderDeactivateButton.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
		_residentHeaderDeactivateButton.FlatAppearance.BorderSize = 1;
		_residentHeaderDeactivateButton.Click -= button3_Click;
		_residentHeaderDeactivateButton.Click += button3_Click;

		_residentHeaderToggleButton.Text = _isProfileDetailsExpanded ? "v" : ">";
		_residentHeaderToggleButton.AutoSize = false;
		_residentHeaderToggleButton.Size = new Size(44, UiTheme.StandardButtonHeight);
		_residentHeaderToggleButton.FlatStyle = FlatStyle.Flat;
		_residentHeaderToggleButton.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 228);
		_residentHeaderToggleButton.FlatAppearance.BorderSize = 1;
		_residentHeaderToggleButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(233, 239, 250);
		_residentHeaderToggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 252);
		_residentHeaderToggleButton.BackColor = Color.White;
		_residentHeaderToggleButton.ForeColor = Color.FromArgb(95, 108, 130);
		_residentHeaderToggleButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
		_residentHeaderToggleButton.Margin = new Padding(8, 0, 0, 0);
		_residentHeaderToggleButton.Padding = Padding.Empty;
		_residentHeaderToggleButton.TabStop = false;
		_residentHeaderToggleButton.Click -= ResidentHeaderToggleButton_Click;
		_residentHeaderToggleButton.Click += ResidentHeaderToggleButton_Click;

		FlowLayoutPanel actionRow = new FlowLayoutPanel
		{
			Name = "flowHeaderActions",
			Dock = DockStyle.Right,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};

		actionRow.Controls.Add(_residentHeaderEditButton);
		actionRow.Controls.Add(_residentHeaderPrintButton);
		actionRow.Controls.Add(_residentHeaderDeactivateButton);
		actionRow.Controls.Add(_residentHeaderToggleButton);

		root.Controls.Add(_residentHeaderPhoto, 0, 0);
		root.Controls.Add(center, 1, 0);
		root.Controls.Add(actionRow, 2, 0);
		_residentHeader.Controls.Add(root);
		_residentHeader.ResumeLayout(performLayout: true);

		UpdateResidentHeader();
		SetProfileDetailsExpanded(_isProfileDetailsExpanded);
		return _residentHeader;


	}

	private void ResidentHeaderPhoto_Resize(object? sender, EventArgs e)
	{
		if (sender is PictureBox picture)
		{
			ApplyCircularPhotoMask(picture);
		}
	}

	private static void ApplyCircularPhotoMask(PictureBox picture)
	{
		using System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
		path.AddEllipse(0, 0, picture.Width, picture.Height);
		picture.Region = new Region(path);
	}




	private Control BuildCertificatesTab()
	{


		Panel panel = new Panel


		{


			Dock = DockStyle.Fill,


			Padding = new Padding(0, 12, 0, 0)


		};


		Label value = new Label


		{


			Text = "Certificates",


			Font = UiTheme.HeadingFont,


			ForeColor = UiTheme.Slate900,


			AutoSize = true,


			Dock = DockStyle.Top


		};


		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel


		{


			Dock = DockStyle.Top,


			AutoSize = true,


			FlowDirection = FlowDirection.LeftToRight,


			WrapContents = true,


			Padding = new Padding(0, 8, 0, 8)


		};


		_btnCertNew.Text = "New Request";


		_btnCertEdit.Text = "Edit";


		_btnCertApprove.Text = "Approve";


		_btnCertIssue.Text = "Issue";


		_btnCertPrint.Text = "Print";


		_btnCertExport.Text = "Export";


		_btnCertCancel.Text = "Cancel";


		_btnCertRefresh.Text = "Refresh";
		_btnCertAttachments.Text = "Attachments";


		UiTheme.StylePrimaryButton(_btnCertNew);
		UiTheme.StyleSecondaryButton(_btnCertEdit);
		UiTheme.StyleSecondaryButton(_btnCertApprove);
		UiTheme.StylePrimaryButton(_btnCertIssue);
		UiTheme.StyleSecondaryButton(_btnCertPrint);
		UiTheme.StyleSecondaryButton(_btnCertExport);
		UiTheme.StyleDangerButton(_btnCertCancel);
		UiTheme.StyleSecondaryButton(_btnCertRefresh);
		UiTheme.StyleSecondaryButton(_btnCertAttachments);

		_btnCertNew.AutoSize = true;
		_btnCertEdit.AutoSize = true;
		_btnCertApprove.AutoSize = true;
		_btnCertIssue.AutoSize = true;
		_btnCertPrint.AutoSize = true;
		_btnCertExport.AutoSize = true;
		_btnCertCancel.AutoSize = true;
		_btnCertRefresh.AutoSize = true;
		_btnCertAttachments.AutoSize = true;
		_btnCertNew.Margin = new Padding(0, 0, 10, 6);


		_btnCertEdit.Margin = new Padding(0, 0, 10, 6);


		_btnCertApprove.Margin = new Padding(0, 0, 10, 6);


		_btnCertIssue.Margin = new Padding(0, 0, 10, 6);


		_btnCertPrint.Margin = new Padding(0, 0, 10, 6);


		_btnCertExport.Margin = new Padding(0, 0, 10, 6);


		_btnCertCancel.Margin = new Padding(0, 0, 10, 6);


		_btnCertRefresh.Margin = new Padding(0, 0, 10, 6);
		_btnCertAttachments.Margin = new Padding(0, 0, 10, 6);


		_btnCertNew.Click -= CertNew_Click;


		_btnCertNew.Click += CertNew_Click;


		_btnCertEdit.Click -= CertEdit_Click;


		_btnCertEdit.Click += CertEdit_Click;


		_btnCertApprove.Click -= CertApprove_Click;


		_btnCertApprove.Click += CertApprove_Click;


		_btnCertIssue.Click -= CertIssue_Click;


		_btnCertIssue.Click += CertIssue_Click;


		_btnCertPrint.Click -= CertPrint_Click;


		_btnCertPrint.Click += CertPrint_Click;


		_btnCertExport.Click -= CertExport_Click;


		_btnCertExport.Click += CertExport_Click;


		_btnCertCancel.Click -= CertCancel_Click;


		_btnCertCancel.Click += CertCancel_Click;


		_btnCertRefresh.Click -= CertRefresh_Click;


		_btnCertRefresh.Click += CertRefresh_Click;
		_btnCertAttachments.Click -= CertAttachments_Click;
		_btnCertAttachments.Click += CertAttachments_Click;


		flowLayoutPanel.Controls.Add(_btnCertNew);


		flowLayoutPanel.Controls.Add(_btnCertEdit);


		flowLayoutPanel.Controls.Add(_btnCertApprove);


		flowLayoutPanel.Controls.Add(_btnCertIssue);


		flowLayoutPanel.Controls.Add(_btnCertPrint);


		flowLayoutPanel.Controls.Add(_btnCertExport);


		flowLayoutPanel.Controls.Add(_btnCertCancel);


		flowLayoutPanel.Controls.Add(_btnCertRefresh);
		flowLayoutPanel.Controls.Add(_btnCertAttachments);


		FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel


		{


			Dock = DockStyle.Top,


			AutoSize = true,


			FlowDirection = FlowDirection.LeftToRight,


			WrapContents = true,


			Padding = new Padding(0, 0, 0, 8)


		};


		_certSearchBox.Width = 220;


		_certSearchBox.PlaceholderText = "Search cert # or purpose";


		UiTheme.StyleTextBox(_certSearchBox);


		_certSearchBox.TextChanged -= CertificateFilter_Changed;


		_certSearchBox.TextChanged += CertificateFilter_Changed;


		UiTheme.StyleComboBox(_certFilterType);


		_certFilterType.DropDownStyle = ComboBoxStyle.DropDownList;


		_certFilterType.Items.Clear();


		_certFilterType.Items.Add("All types");


		_certFilterType.Items.AddRange(new object[4] { "Barangay Clearance", "Certificate of Residency", "Indigency", "Business Clearance" });


		_certFilterType.SelectedIndex = 0;


		_certFilterType.SelectedIndexChanged -= CertificateFilter_Changed;


		_certFilterType.SelectedIndexChanged += CertificateFilter_Changed;


		UiTheme.StyleComboBox(_certFilterStatus);


		_certFilterStatus.DropDownStyle = ComboBoxStyle.DropDownList;


		_certFilterStatus.Items.Clear();


		_certFilterStatus.Items.AddRange(new object[7] { "All Status", "Requested", "Approved", "Issued", "Cancelled", "Rejected", "Draft" });


		_certFilterStatus.SelectedIndex = 0;


		_certFilterStatus.SelectedIndexChanged -= CertificateFilter_Changed;


		_certFilterStatus.SelectedIndexChanged += CertificateFilter_Changed;


		_certFilterFrom.Format = DateTimePickerFormat.Short;


		_certFilterFrom.ShowCheckBox = true;


		_certFilterFrom.Font = UiTheme.BodyFont;


		_certFilterFrom.Checked = false;


		_certFilterFrom.ValueChanged -= CertificateFilter_Changed;


		_certFilterFrom.ValueChanged += CertificateFilter_Changed;


		_certFilterTo.Format = DateTimePickerFormat.Short;


		_certFilterTo.ShowCheckBox = true;


		_certFilterTo.Font = UiTheme.BodyFont;


		_certFilterTo.Checked = false;


		_certFilterTo.ValueChanged -= CertificateFilter_Changed;


		_certFilterTo.ValueChanged += CertificateFilter_Changed;


		_certFilterClear.Text = "Clear Filters";


		UiTheme.StyleSecondaryButton(_certFilterClear);


		_certFilterClear.Click -= CertFilterClear_Click;


		_certFilterClear.Click += CertFilterClear_Click;


		flowLayoutPanel2.Controls.Add(_certSearchBox);


		flowLayoutPanel2.Controls.Add(_certFilterType);


		flowLayoutPanel2.Controls.Add(_certFilterStatus);


		flowLayoutPanel2.Controls.Add(new Label


		{


			Text = "From",


			AutoSize = true,


			ForeColor = UiTheme.Slate500,


			Font = UiTheme.LabelFont,


			Margin = new Padding(6, 8, 4, 0)


		});


		flowLayoutPanel2.Controls.Add(_certFilterFrom);


		flowLayoutPanel2.Controls.Add(new Label


		{


			Text = "To",


			AutoSize = true,


			ForeColor = UiTheme.Slate500,


			Font = UiTheme.LabelFont,


			Margin = new Padding(6, 8, 4, 0)


		});


		flowLayoutPanel2.Controls.Add(_certFilterTo);


		flowLayoutPanel2.Controls.Add(_certFilterClear);


		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel


		{


			Dock = DockStyle.Top,


			AutoSize = true,


			FlowDirection = FlowDirection.LeftToRight,


			WrapContents = true,


			Padding = new Padding(0, 0, 0, 8)


		};


		_certSummaryTotal.Font = UiTheme.LabelFont;


		_certSummaryTotal.ForeColor = UiTheme.Slate500;


		_certSummaryIssued.Font = UiTheme.LabelFont;


		_certSummaryIssued.ForeColor = UiTheme.Slate500;


		_certSummaryPending.Font = UiTheme.LabelFont;


		_certSummaryPending.ForeColor = UiTheme.Slate500;


		_certSummaryCancelled.Font = UiTheme.LabelFont;


		_certSummaryCancelled.ForeColor = UiTheme.Slate500;


		flowLayoutPanel3.Controls.Add(_certSummaryTotal);


		flowLayoutPanel3.Controls.Add(_certSummaryIssued);


		flowLayoutPanel3.Controls.Add(_certSummaryPending);


		flowLayoutPanel3.Controls.Add(_certSummaryCancelled);


		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel


		{


			Dock = DockStyle.Fill,


			ColumnCount = 2,


			RowCount = 1


		};


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));


		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));


		Panel panel2 = new Panel


		{


			Dock = DockStyle.Fill,


			Padding = new Padding(0, 0, 16, 0)


		};


		_certGrid.Dock = DockStyle.Fill;


		_certGrid.ReadOnly = true;


		_certGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;


		_certGrid.MultiSelect = false;


		_certGrid.AllowUserToAddRows = false;


		_certGrid.AllowUserToDeleteRows = false;


		_certGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;


		UiTheme.StyleGrid(_certGrid);


		_certGrid.SelectionChanged -= CertGrid_SelectionChanged;


		_certGrid.SelectionChanged += CertGrid_SelectionChanged;


		_certEmptyPanel = CreateEmptyStatePanel(_certEmptyTitle, _certEmptyMessage);


		panel2.Controls.Add(_certGrid);


		panel2.Controls.Add(_certEmptyPanel);


		Panel panel3 = new Panel


		{


			Dock = DockStyle.Fill,


			AutoScroll = true,


			Padding = new Padding(0, 0, 0, 8)


		};


		PrepareCertificateEditors();


		panel3.Controls.Add(BuildCertificateDetailsPanel());


		tableLayoutPanel.Controls.Add(panel2, 0, 0);


		tableLayoutPanel.Controls.Add(panel3, 1, 0);


		panel.Controls.Add(tableLayoutPanel);


		panel.Controls.Add(flowLayoutPanel3);


		panel.Controls.Add(flowLayoutPanel2);


		panel.Controls.Add(flowLayoutPanel);


		panel.Controls.Add(value);


		ResetCertificateDetails();


		UpdateCertificateActionState();


		UpdateCertificateSummary();


		UpdateCertificateEmptyState();


		return panel;


	}





	private Control BuildCertificateDetailsPanel()


	{


		Panel panel = new Panel


		{


			Dock = DockStyle.Top,


			AutoSize = true,


			AutoSizeMode = AutoSizeMode.GrowAndShrink


		};


		Label label = new Label


		{


			Text = "Certificate Details",


			Font = UiTheme.HeadingFont,


			ForeColor = UiTheme.Slate900,


			AutoSize = true,


			Margin = new Padding(0, 0, 0, 8)


		};


		label.Dock = DockStyle.Top;


		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel


		{


			ColumnCount = 2,


			AutoSize = true,


			AutoSizeMode = AutoSizeMode.GrowAndShrink,


			GrowStyle = TableLayoutPanelGrowStyle.AddRows,


			Margin = new Padding(0, 0, 0, 12)


		};


		tableLayoutPanel.Dock = DockStyle.Top;


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));


		_certNumber.Font = UiTheme.BodyFont;


		_certNumber.ForeColor = UiTheme.Slate900;


		_certNumber.AutoSize = true;


		_certStatus.Font = UiTheme.BodyFont;


		_certStatus.ForeColor = UiTheme.Slate900;


		_certStatus.AutoSize = true;


		_certStatus.Padding = new Padding(8, 2, 8, 2);


		_certStatus.BackColor = UiTheme.Slate300;


		_certStatus.ForeColor = UiTheme.Slate900;

		_certSla.Font = UiTheme.SmallFont;
		_certSla.AutoSize = true;
		_certSla.Padding = new Padding(8, 2, 8, 2);
		_certSla.BackColor = Color.FromArgb(235, 235, 235);
		_certSla.ForeColor = UiTheme.Slate700;
		_certSla.Visible = false;


		_certRequestedAt.Font = UiTheme.LabelFont;


		_certRequestedAt.ForeColor = UiTheme.Slate500;


		_certRequestedAt.AutoSize = true;


		_certApprovedAt.Font = UiTheme.LabelFont;


		_certApprovedAt.ForeColor = UiTheme.Slate500;


		_certApprovedAt.AutoSize = true;


		_certIssuedAt.Font = UiTheme.LabelFont;


		_certIssuedAt.ForeColor = UiTheme.Slate500;


		_certIssuedAt.AutoSize = true;


		AddDetailRow(tableLayoutPanel, "Certificate no.", _certNumber);

		var certStatusRow = new FlowLayoutPanel
		{
			AutoSize = true,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = new Padding(0)
		};
		_certStatus.Margin = new Padding(0, 0, 8, 0);
		_certSla.Margin = new Padding(0);
		certStatusRow.Controls.Add(_certStatus);
		certStatusRow.Controls.Add(_certSla);
		AddDetailRow(tableLayoutPanel, "Status", certStatusRow);


		AddDetailRow(tableLayoutPanel, "Requested", _certRequestedAt);


		AddDetailRow(tableLayoutPanel, "Approved", _certApprovedAt);


		AddDetailRow(tableLayoutPanel, "Issued", _certIssuedAt);


		Label label2 = new Label


		{


			Text = "Certificate Data",


			Font = UiTheme.LabelFont,


			ForeColor = UiTheme.Slate500,


			AutoSize = true,


			Margin = new Padding(0, 6, 0, 6)


		};


		label2.Dock = DockStyle.Top;


		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel


		{


			ColumnCount = 2,


			AutoSize = true,


			AutoSizeMode = AutoSizeMode.GrowAndShrink,


			GrowStyle = TableLayoutPanelGrowStyle.AddRows


		};


		tableLayoutPanel2.Dock = DockStyle.Top;


		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));


		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));


		SetupValueLabel(_certTypeValue);


		SetupValueLabel(_certPurposeValue, 260);


		SetupValueLabel(_certFeeValue);


		SetupValueLabel(_certOrValue);


		SetupValueLabel(_certIssuedDateValue);
		SetupValueLabel(_certValidUntilValue);
		SetupValueLabel(_certPrintCountValue);
		SetupValueLabel(_certLastPrintedValue);
		SetupValueLabel(_certPaymentAmountValue);
		SetupValueLabel(_certPaymentMethodValue);
		SetupValueLabel(_certPaymentOrValue);
		SetupValueLabel(_certPaymentDateValue);
		SetupValueLabel(_certPaymentReceivedByValue);


		SetupValueLabel(_certBusinessNameValue);


		SetupValueLabel(_certBusinessNatureValue);


		SetupValueLabel(_certRemarksValue, 260);


		AddDetailRow(tableLayoutPanel2, "Type", _certTypeValue);


		AddDetailRow(tableLayoutPanel2, "Purpose", _certPurposeValue);


		AddDetailRow(tableLayoutPanel2, "Fee", _certFeeValue);


		AddDetailRow(tableLayoutPanel2, "OR number", _certOrValue);


		AddDetailRow(tableLayoutPanel2, "Issued date", _certIssuedDateValue);


		_lblBusinessName = AddDetailRowWithLabel(tableLayoutPanel2, "Business name", _certBusinessNameValue);


		_lblBusinessNature = AddDetailRowWithLabel(tableLayoutPanel2, "Business nature", _certBusinessNatureValue);


		AddDetailRow(tableLayoutPanel2, "Remarks", _certRemarksValue);


		UpdateBusinessFieldsVisibility();

		Label label3 = new Label
		{
			Text = "Validity & Printing",
			Font = UiTheme.LabelFont,
			ForeColor = UiTheme.Slate500,
			AutoSize = true,
			Margin = new Padding(0, 6, 0, 6)
		};

		label3.Dock = DockStyle.Top;

		TableLayoutPanel validityTable = new TableLayoutPanel
		{
			ColumnCount = 2,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			GrowStyle = TableLayoutPanelGrowStyle.AddRows,
			Margin = new Padding(0, 0, 0, 12)
		};

		validityTable.Dock = DockStyle.Top;
		validityTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
		validityTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

		AddDetailRow(validityTable, "Valid until", _certValidUntilValue);
		AddDetailRow(validityTable, "Print count", _certPrintCountValue);
		AddDetailRow(validityTable, "Last printed", _certLastPrintedValue);

		Label label4 = new Label
		{
			Text = "Payment Details",
			Font = UiTheme.LabelFont,
			ForeColor = UiTheme.Slate500,
			AutoSize = true,
			Margin = new Padding(0, 6, 0, 6)
		};

		label4.Dock = DockStyle.Top;

		TableLayoutPanel paymentTable = new TableLayoutPanel
		{
			ColumnCount = 2,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			GrowStyle = TableLayoutPanelGrowStyle.AddRows,
			Margin = new Padding(0, 0, 0, 12)
		};

		paymentTable.Dock = DockStyle.Top;
		paymentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));
		paymentTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

		AddDetailRow(paymentTable, "Amount", _certPaymentAmountValue);
		AddDetailRow(paymentTable, "Method", _certPaymentMethodValue);
		AddDetailRow(paymentTable, "OR number", _certPaymentOrValue);
		AddDetailRow(paymentTable, "Paid at", _certPaymentDateValue);
		AddDetailRow(paymentTable, "Received by", _certPaymentReceivedByValue);


		panel.Controls.Add(paymentTable);
		panel.Controls.Add(label4);
		panel.Controls.Add(validityTable);
		panel.Controls.Add(label3);
		panel.Controls.Add(tableLayoutPanel2);
		panel.Controls.Add(label2);
		panel.Controls.Add(tableLayoutPanel);
		panel.Controls.Add(label);


		return panel;


	}





	private void PrepareCertificateEditors()


	{


		UiTheme.StyleComboBox(_certType);


		UiTheme.StyleTextBox(_certPurpose);


		UiTheme.StyleTextBox(_certOR);


		UiTheme.StyleTextBox(_certBusinessName);


		UiTheme.StyleTextBox(_certBusinessNature);


		UiTheme.StyleTextBox(_certRemarks);


		_certType.DropDownStyle = ComboBoxStyle.DropDownList;


		if (_certType.Items.Count == 0)


		{


			_certType.Items.AddRange(new object[4] { "Barangay Clearance", "Certificate of Residency", "Indigency", "Business Clearance" });


		}


		_certType.SelectedIndexChanged -= CertType_SelectedIndexChanged;


		_certType.SelectedIndexChanged += CertType_SelectedIndexChanged;


		_certPurpose.Multiline = true;


		_certPurpose.Height = 60;


		_certPurpose.ScrollBars = ScrollBars.Vertical;


		_certType.Dock = DockStyle.Fill;


		_certPurpose.Dock = DockStyle.Fill;


		_certRemarks.Multiline = true;


		_certRemarks.Height = 70;


		_certRemarks.ScrollBars = ScrollBars.Vertical;


		_certOR.Dock = DockStyle.Fill;


		_certBusinessName.Dock = DockStyle.Fill;


		_certBusinessNature.Dock = DockStyle.Fill;


		_certRemarks.Dock = DockStyle.Fill;


		_certFee.DecimalPlaces = 2;


		_certFee.Maximum = 1000000m;


		_certFee.Minimum = 0m;


		_certFee.Increment = 50m;


		_certFee.Font = UiTheme.BodyFont;


		_certFee.BackColor = Color.White;


		_certFee.ForeColor = UiTheme.Slate900;


		_certFee.BorderStyle = BorderStyle.FixedSingle;


		_certFee.TextAlign = HorizontalAlignment.Right;


		_certFee.Dock = DockStyle.Left;


		_certFee.Width = 140;


		_certValidUntil.Format = DateTimePickerFormat.Short;


		_certValidUntil.Font = UiTheme.BodyFont;


		_certValidUntil.ShowCheckBox = true;


		_certValidUntil.Dock = DockStyle.Left;


		_certValidUntil.Width = 160;


		SetCertificateEditing(enabled: false);


	}





	private void CertType_SelectedIndexChanged(object? sender, EventArgs e)


	{


		UpdateBusinessFieldsVisibility();


	}





	private void UpdateBusinessFieldsVisibility()


	{


		string text = _certType.SelectedItem?.ToString() ?? string.Empty;


		bool visible = text.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0;


		if (_lblBusinessName != null)


		{


			_lblBusinessName.Visible = visible;


		}


		if (_lblBusinessNature != null)


		{


			_lblBusinessNature.Visible = visible;


		}


		_certBusinessNameValue.Visible = visible;


		_certBusinessNatureValue.Visible = visible;


		_certBusinessName.Visible = visible;


		_certBusinessNature.Visible = visible;


	}





	private void CertNew_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertNew(sender, e);

	}





	private void CertEdit_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertEdit(sender, e);

	}





	private void CertApprove_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertApprove(sender, e);

	}





	private void CertIssue_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertIssue(sender, e);

	}





	private void CertCancel_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertCancel(sender, e);

	}





	private void CertRefresh_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertRefresh(sender, e);

	}





	private void CertPrint_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertPrint(sender, e);

	}





	private void CertExport_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertExport(sender, e);

	}





	private void CertificateFilter_Changed(object? sender, EventArgs e)


	{


		ApplyCertificateFilters();


	}





	private void CertFilterClear_Click(object? sender, EventArgs e)


	{

		_controller.HandleCertFilterClear(sender, e);

	}





	private void ApplyCertificateFilters(bool resetPage = true)


	{


		if (_certTable == null)


		{


			_certGrid.DataSource = null;
			UpdateCertificatePagerState();
			UpdateCertificateSummary();
			UpdateCertificateEmptyState();


			return;


		}


		List<string> list = new List<string>();


		string text = _certSearchBox.Text.Trim();


		if (!string.IsNullOrWhiteSpace(text))


		{


			text = text.Replace("[", "[[]").Replace("]", "[]]").Replace("'", "''");


			list.Add($"(certificate_no LIKE '%{text}%' OR purpose LIKE '%{text}%' OR certificate_type LIKE '%{text}%')");


		}


		if (_certFilterType.SelectedIndex > 0)


		{


			string text2 = _certFilterType.SelectedItem?.ToString() ?? string.Empty;


			text2 = text2.Replace("'", "''");


			list.Add("certificate_type = '" + text2 + "'");


		}


		if (_certFilterStatus.SelectedIndex > 0)


		{


			string text3 = _certFilterStatus.SelectedItem?.ToString() ?? string.Empty;


			text3 = text3.Replace("'", "''");


			list.Add("status = '" + text3 + "'");


		}


		if (_certFilterFrom.Checked)


		{


			DateTime date = _certFilterFrom.Value.Date;


			list.Add($"requested_at >= #{date:MM/dd/yyyy}#");


		}


		if (_certFilterTo.Checked)


		{


			DateTime value = _certFilterTo.Value.Date.AddDays(1.0);


			list.Add($"requested_at < #{value:MM/dd/yyyy}#");


		}


		_certTable.DefaultView.RowFilter = string.Join(" AND ", list);
		ApplyCertificatePaging(resetPage);
		UpdateCertificateSummary();
		UpdateCertificateEmptyState();


		if (!_isCertEditing)


		{


			if (_certGrid.Rows.Count == 0)


			{


				_selectedCertificateId = null;
				ResetCertificateDetails();
				UpdateCertificateActionState();


			}


			else if (!_selectedCertificateId.HasValue || !TrySelectCertificateRowInCurrentPage(_selectedCertificateId.Value))


			{


				_certGrid.Rows[0].Selected = true;
				PopulateCertificateDetails(_certGrid.Rows[0]);


			}


		}


	}

	private void ApplyCertificatePaging(bool resetPage)
	{
		if (_certTable == null)
		{
			_certGrid.DataSource = null;
			UpdateCertificatePagerState();
			return;
		}

		DataView view = _certTable.DefaultView;
		int total = view.Count;
		int totalPages = total <= 0 ? 1 : (int)Math.Ceiling(total / (double)_certificatePageSize);
		if (resetPage)
		{
			_certPageIndex = 0;
		}

		if (_certPageIndex < 0)
		{
			_certPageIndex = 0;
		}

		if (_certPageIndex >= totalPages)
		{
			_certPageIndex = totalPages - 1;
		}

		int startIndex = _certPageIndex * _certificatePageSize;
		int endIndex = Math.Min(total, startIndex + _certificatePageSize);
		DataTable pageTable = _certTable.Clone();
		for (int i = startIndex; i < endIndex; i++)
		{
			pageTable.ImportRow(view[i].Row);
		}

		_certGrid.DataSource = pageTable;
		ConfigureCertificateGridColumns();
		UpdateCertificatePagerState();
	}

	private void ConfigureCertificateGridColumns()
	{
		foreach (DataGridViewColumn column in _certGrid.Columns)
		{
			column.Visible = false;
		}

		if (_certGrid.Columns["certificate_id"] is DataGridViewColumn certId)
		{
			certId.Visible = false;
		}

		if (_certGrid.Columns[CertificateRowNumberColumnName] == null)
		{
			_certGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = CertificateRowNumberColumnName,
				HeaderText = "#",
				ReadOnly = true,
				SortMode = DataGridViewColumnSortMode.NotSortable,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
				MinimumWidth = 46
			});
		}

		if (_certGrid.Columns["certificate_type"] is DataGridViewColumn typeColumn)
		{
			typeColumn.Visible = true;
			typeColumn.HeaderText = "Document Type";
			typeColumn.DisplayIndex = 1;
			typeColumn.FillWeight = 30F;
		}

		if (_certGrid.Columns["purpose"] is DataGridViewColumn purposeColumn)
		{
			purposeColumn.Visible = true;
			purposeColumn.HeaderText = "Purpose";
			purposeColumn.DisplayIndex = 2;
			purposeColumn.FillWeight = 30F;
		}

		if (_certGrid.Columns["status"] is DataGridViewColumn statusColumn)
		{
			statusColumn.Visible = true;
			statusColumn.HeaderText = "Status";
			statusColumn.DisplayIndex = 3;
			statusColumn.FillWeight = 14F;
		}

		DataGridViewColumn? requestedAtColumn = _certGrid.Columns["requested_at"] ?? _certGrid.Columns["issued_date"];
		if (requestedAtColumn != null)
		{
			requestedAtColumn.Visible = true;
			requestedAtColumn.HeaderText = "Requested At";
			requestedAtColumn.DisplayIndex = 4;
			requestedAtColumn.DefaultCellStyle.Format = "MMM dd, yyyy";
			requestedAtColumn.FillWeight = 16F;
		}

		if (_certGrid.Columns[CertificateActionsColumnName] == null)
		{
			DataGridViewButtonColumn actionsColumn = new DataGridViewButtonColumn
			{
				Name = CertificateActionsColumnName,
				HeaderText = "Actions",
				Text = "\u25C9   \u2B07   \u2399   \u22EE",
				UseColumnTextForButtonValue = true,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
				Width = 120,
				MinimumWidth = 120,
				FlatStyle = FlatStyle.Flat
			};
			_certGrid.Columns.Add(actionsColumn);
		}

		if (_certGrid.Columns[CertificateRowNumberColumnName] is DataGridViewColumn numberColumn)
		{
			numberColumn.Visible = true;
			numberColumn.DisplayIndex = 0;
			numberColumn.ReadOnly = true;
			numberColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
		}

		if (_certGrid.Columns[CertificateActionsColumnName] is DataGridViewColumn actions)
		{
			actions.Visible = true;
			actions.DisplayIndex = 5;
			actions.SortMode = DataGridViewColumnSortMode.NotSortable;
			actions.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
			actions.Width = 120;
			actions.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			actions.DefaultCellStyle.Font = new Font("Segoe UI Symbol", 9F, FontStyle.Regular, GraphicsUnit.Point);
		}
	}





	private void UpdateCertificateSummary()


	{


		if (_certTable == null)


		{


			_certSummaryTotal.Text = "Total: 0";


			_certSummaryIssued.Text = "Issued: 0";


			_certSummaryPending.Text = "Requested: 0 | Approved: 0";


			_certSummaryCancelled.Text = "Cancelled: 0";


			return;


		}


		DataView defaultView = _certTable.DefaultView;


		int count = defaultView.Count;


		int num = 0;


		int num2 = 0;


		int num3 = 0;


		int num4 = 0;
		int num5 = 0;
		int num6 = 0;


		foreach (DataRowView item in defaultView)


		{


			string a = item["status"]?.ToString() ?? string.Empty;


			if (string.Equals(a, "Issued", StringComparison.OrdinalIgnoreCase))


			{


				num++;


			}


			else if (string.Equals(a, "Requested", StringComparison.OrdinalIgnoreCase))


			{


				num2++;


			}


			else if (string.Equals(a, "Approved", StringComparison.OrdinalIgnoreCase))


			{


				num3++;


			}


			else if (string.Equals(a, "Cancelled", StringComparison.OrdinalIgnoreCase))


			{


				num4++;


			}
			else if (string.Equals(a, "Rejected", StringComparison.OrdinalIgnoreCase))
			{
				num5++;
			}
			else if (string.Equals(a, "Draft", StringComparison.OrdinalIgnoreCase))
			{
				num6++;
			}


		}


		_certSummaryTotal.Text = $"Total: {count}";


		_certSummaryIssued.Text = $"Issued: {num}";


		_certSummaryPending.Text = $"Requested: {num2} | Approved: {num3}";


		_certSummaryCancelled.Text = $"Cancelled: {num4}";
		if (num5 > 0 || num6 > 0)
		{
			_certSummaryCancelled.Text = $"Cancelled: {num4} | Rejected: {num5} | Draft: {num6}";
		}


	}





	private void DrawCertificatePrint(Graphics graphics, Rectangle bounds)


	{


		Font font = new Font(UiTheme.TitleFont.FontFamily, 18f, FontStyle.Bold);


		Font font2 = new Font(UiTheme.BodyFont.FontFamily, 12f, FontStyle.Bold);


		Font bodyFont = UiTheme.BodyFont;


		int left = bounds.Left;


		int top = bounds.Top;


		int num = 24;
		string token = _certVerificationToken.Trim();
		string certificateNo = (_certNumber.Text ?? string.Empty).Trim();
		if (!string.IsNullOrWhiteSpace(token))
		{
			int qrSize = 110;
			int qrX = bounds.Right - qrSize;
			int qrY = bounds.Top;
			DrawQrCode(graphics, new Rectangle(qrX, qrY, qrSize, qrSize), BuildCertificateQrPayload(certificateNo, token));
		}


		graphics.DrawString("Barangay Certificate", font, Brushes.Black, left, top);


		top += num + 6;


		graphics.DrawString("Certificate No: " + _certNumber.Text, bodyFont, Brushes.Black, left, top);


		top += num;


		string residentFullName = GetResidentFullName();


		graphics.DrawString("Resident: " + residentFullName, bodyFont, Brushes.Black, left, top);


		top += num;


		graphics.DrawString($"Type: {_certType.SelectedItem}", bodyFont, Brushes.Black, left, top);


		top += num;


		if (!string.IsNullOrWhiteSpace(_certPurpose.Text))


		{


			graphics.DrawString("Purpose: " + _certPurpose.Text, bodyFont, Brushes.Black, left, top);


			top += num;


		}


		graphics.DrawString("Issued Date: " + GetIssuedDateText(), bodyFont, Brushes.Black, left, top);


		top += num;


		if (_certFee.Value > 0m)


		{


			graphics.DrawString($"Fee: {_certFee.Value:C}", bodyFont, Brushes.Black, left, top);


			top += num;


		}


		if (!string.IsNullOrWhiteSpace(_certOR.Text))


		{


			graphics.DrawString("OR No: " + _certOR.Text, bodyFont, Brushes.Black, left, top);


			top += num;


		}


		if (_certBusinessName.Visible && !string.IsNullOrWhiteSpace(_certBusinessName.Text))


		{


			graphics.DrawString("Business Name: " + _certBusinessName.Text, bodyFont, Brushes.Black, left, top);


			top += num;


		}


		if (_certBusinessNature.Visible && !string.IsNullOrWhiteSpace(_certBusinessNature.Text))


		{


			graphics.DrawString("Business Nature: " + _certBusinessNature.Text, bodyFont, Brushes.Black, left, top);


			top += num;


		}


		if (!string.IsNullOrWhiteSpace(_certRemarks.Text))


		{


			graphics.DrawString("Remarks: " + _certRemarks.Text, bodyFont, Brushes.Black, left, top);


			top += num;


		}


		top += num;


		graphics.DrawString("____________________________", font2, Brushes.Black, left, top);


		top += num;


		graphics.DrawString("Issued by: " + GetIssuedByName(), bodyFont, Brushes.Black, left, top);
		if (!string.IsNullOrWhiteSpace(token))
		{
			top += num;
			using Font font3 = new Font(UiTheme.BodyFont.FontFamily, 9f, FontStyle.Regular);
			graphics.DrawString("Verification Code: " + token, font3, Brushes.DimGray, left, top);
		}


	}





	private string GetResidentFullName()


	{


		List<string> list = new List<string>();


		if (!string.IsNullOrWhiteSpace(_editFirstName.Text))


		{


			list.Add(_editFirstName.Text.Trim());


		}


		if (!string.IsNullOrWhiteSpace(_editMiddleName.Text))


		{


			list.Add(_editMiddleName.Text.Trim());


		}


		if (!string.IsNullOrWhiteSpace(_editLastName.Text))


		{


			list.Add(_editLastName.Text.Trim());


		}


		return (list.Count == 0) ? "Resident" : string.Join(" ", list);


	}





	private void RegisterCertificatePrint(int certificateId)


	{


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();


			mySqlConnection.Open();


			using MySqlCommand mySqlCommand = new MySqlCommand("UPDATE document_request\r\n                                       SET print_count = COALESCE(print_count, 0) + 1,\r\n                                           last_printed_at = NOW()\r\n                                       WHERE doc_request_id=@id", mySqlConnection);


			mySqlCommand.Parameters.AddWithValue("@id", certificateId);


			mySqlCommand.ExecuteNonQuery();


			LogCertificateAudit(certificateId, "Printed", null);


			if (_selectedResidentId.HasValue)


			{


				LoadResidentHistory(_selectedResidentId.Value);


			}


		}


		catch


		{


		}


	}





	private void LogCertificateAudit(int certificateId, string action, string? notes)


	{


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();


			mySqlConnection.Open();


			using MySqlCommand mySqlCommand = new MySqlCommand("INSERT INTO certificate_audit\r\n                                       (certificate_id, action, action_by, notes)\r\n                                       VALUES\r\n                                       (@id, @action, @by, @notes)", mySqlConnection);


			mySqlCommand.Parameters.AddWithValue("@id", certificateId);


			mySqlCommand.Parameters.AddWithValue("@action", action);


			mySqlCommand.Parameters.AddWithValue("@by", UserSession.UserId);


			mySqlCommand.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? ((IConvertible)DBNull.Value) : ((IConvertible)notes));


			mySqlCommand.ExecuteNonQuery();


		}


		catch


		{


		}


	}





	private void LogActivity(int residentId, string module, string action, string? details)


	{


		if (residentId <= 0)


		{


			return;


		}


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();


			mySqlConnection.Open();


			using MySqlCommand mySqlCommand = new MySqlCommand("INSERT INTO activity_log\r\n                                       (resident_id, module, action, details, action_by)\r\n                                       VALUES\r\n                                       (@rid, @module, @action, @details, @by)", mySqlConnection);


			mySqlCommand.Parameters.AddWithValue("@rid", residentId);


			mySqlCommand.Parameters.AddWithValue("@module", module);


			mySqlCommand.Parameters.AddWithValue("@action", action);


			mySqlCommand.Parameters.AddWithValue("@details", string.IsNullOrWhiteSpace(details) ? ((IConvertible)DBNull.Value) : ((IConvertible)details));


			mySqlCommand.Parameters.AddWithValue("@by", UserSession.UserId);


			mySqlCommand.ExecuteNonQuery();


		}


		catch


		{


		}


	}





	internal static string EscapeCsv(object? value)


	{


		string text = value?.ToString() ?? string.Empty;




		{




		}


		if (text.Contains(",") || text.Contains("\n") || text.Contains("\r"))


		{




		}


		return text;


	}





	private void CertGrid_SelectionChanged(object? sender, EventArgs e)


	{


		if (_certGrid.SelectedRows.Count == 0)


		{


			ResetCertificateDetails();


			UpdateCertificateActionState();


		}


		else


		{


			PopulateCertificateDetails(_certGrid.SelectedRows[0]);


		}


	}





	private CertificateEntry BuildCertificateEntryFromDetails()


	{


		return new CertificateEntry


		{


			Type = (_certType.SelectedItem?.ToString() ?? string.Empty),


			Purpose = _certPurpose.Text.Trim(),


			Fee = _certFee.Value,


			OrNumber = _certOR.Text.Trim(),


			IssuedDate = (_certValidUntil.Checked ? new DateTime?(_certValidUntil.Value.Date) : ((DateTime?)null)),


			BusinessName = _certBusinessName.Text.Trim(),


			BusinessNature = _certBusinessNature.Text.Trim(),


			Remarks = _certRemarks.Text.Trim()


		};


	}





	private int CreateCertificateRequest(CertificateEntry entry)


	{
		if (!Permissions.CanRequestCertificates)
		{
			throw new UnauthorizedAccessException("You do not have permission to create certificate requests.");
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		SchemaBootstrap.EnsureCoreDefaults(mySqlConnection);
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();


		int docTypeId = GetOrCreateDocumentTypeId(mySqlConnection, entry.Type, tx);
		bool isClearance = IsBarangayClearanceDocumentType(mySqlConnection, tx, docTypeId);
		int? renewedFromRequestId = isClearance
			? FindLatestReleasedClearanceRequestId(mySqlConnection, tx, _selectedResidentId.Value, docTypeId)
			: (int?)null;
		using MySqlCommand mySqlCommand = new MySqlCommand("INSERT INTO document_request\r\n                                   (barangay_id, doc_type_id, resident_id, purpose, status, fee, or_number,\r\n                                    requested_by_user_id, requested_at, business_name, business_nature, remarks, renewed_from_request_id)\r\n                                   VALUES\r\n                                   (@barangayId, @docTypeId, @rid, @purpose, 'SUBMITTED', @fee, @or,\r\n                                    @reqBy, NOW(), @bizName, @bizNature, @remarks, @renewedFromRequestId)", mySqlConnection);
		mySqlCommand.Transaction = tx;


		mySqlCommand.Parameters.AddWithValue("@barangayId", SchemaDefaults.DefaultBarangayId);
		mySqlCommand.Parameters.AddWithValue("@docTypeId", docTypeId);
		mySqlCommand.Parameters.AddWithValue("@rid", _selectedResidentId.Value);


		mySqlCommand.Parameters.AddWithValue("@purpose", string.IsNullOrWhiteSpace(entry.Purpose) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.Purpose));


		mySqlCommand.Parameters.AddWithValue("@fee", entry.Fee);


		mySqlCommand.Parameters.AddWithValue("@or", string.IsNullOrWhiteSpace(entry.OrNumber) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.OrNumber));


		mySqlCommand.Parameters.AddWithValue("@reqBy", UserSession.UserId);


		mySqlCommand.Parameters.AddWithValue("@bizName", string.IsNullOrWhiteSpace(entry.BusinessName) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.BusinessName));


		mySqlCommand.Parameters.AddWithValue("@bizNature", string.IsNullOrWhiteSpace(entry.BusinessNature) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.BusinessNature));


		mySqlCommand.Parameters.AddWithValue("@remarks", string.IsNullOrWhiteSpace(entry.Remarks) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.Remarks));
		mySqlCommand.Parameters.AddWithValue("@renewedFromRequestId", renewedFromRequestId.HasValue ? renewedFromRequestId.Value : (object)DBNull.Value);


		mySqlCommand.ExecuteNonQuery();


		int num = (int)mySqlCommand.LastInsertedId;


		object? afterSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, num, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Certificates",
			"document_request",
			num,
			"CREATE_REQUEST",
			null,
			afterSnapshot);
		tx.Commit();
		LogCertificateAudit(num, "Requested", null);


		return num;


	}





	private void UpdateCertificateRequest(int certificateId, CertificateEntry entry)


	{
		if (!Permissions.CanEditCertificateRequests)
		{
			throw new UnauthorizedAccessException("You do not have permission to edit certificate requests.");
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		SchemaBootstrap.EnsureCoreDefaults(mySqlConnection);
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();
		object? beforeSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, certificateId, tx);


		int docTypeId = GetOrCreateDocumentTypeId(mySqlConnection, entry.Type, tx);
		using MySqlCommand mySqlCommand = new MySqlCommand("UPDATE document_request\r\n                                    SET doc_type_id=@docTypeId,\r\n                                        purpose=@purpose,\r\n                                        fee=@fee,\r\n                                        or_number=@or,\r\n                                        business_name=@bizName,\r\n                                        business_nature=@bizNature,\r\n                                        remarks=@remarks\r\n                                    WHERE doc_request_id=@id\r\n                                      AND status='SUBMITTED'", mySqlConnection);
		mySqlCommand.Transaction = tx;


		mySqlCommand.Parameters.AddWithValue("@docTypeId", docTypeId);


		mySqlCommand.Parameters.AddWithValue("@purpose", string.IsNullOrWhiteSpace(entry.Purpose) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.Purpose));


		mySqlCommand.Parameters.AddWithValue("@fee", entry.Fee);


		mySqlCommand.Parameters.AddWithValue("@or", string.IsNullOrWhiteSpace(entry.OrNumber) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.OrNumber));


		mySqlCommand.Parameters.AddWithValue("@bizName", string.IsNullOrWhiteSpace(entry.BusinessName) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.BusinessName));


		mySqlCommand.Parameters.AddWithValue("@bizNature", string.IsNullOrWhiteSpace(entry.BusinessNature) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.BusinessNature));


		mySqlCommand.Parameters.AddWithValue("@remarks", string.IsNullOrWhiteSpace(entry.Remarks) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.Remarks));


		mySqlCommand.Parameters.AddWithValue("@id", certificateId);


		if (mySqlCommand.ExecuteNonQuery() == 0)


		{


			throw new InvalidOperationException("Unable to update. The certificate status may have changed.");


		}


		object? afterSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, certificateId, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Certificates",
			"document_request",
			certificateId,
			"UPDATE_REQUEST",
			beforeSnapshot,
			afterSnapshot,
			"Certificate request updated.");
		tx.Commit();
		LogCertificateAudit(certificateId, "Updated", null);


	}





	private void IssueCertificate(int certificateId, CertificateEntry entry)


	{
		if (!Permissions.CanIssueCertificates)
		{
			throw new UnauthorizedAccessException("You do not have permission to issue certificates.");
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();

		DateTime dateTime = entry.IssuedDate ?? DateTime.Today;
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();
		object? beforeSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, certificateId, tx);

		string orNo = entry.OrNumber?.Trim() ?? string.Empty;
		string paymentMethod = entry.PaymentMethod?.Trim() ?? string.Empty;
		if (entry.Fee < 0m)
		{
			throw new InvalidOperationException("Fee cannot be negative.");
		}

		bool needsPayment = entry.Fee > 0m || !string.IsNullOrWhiteSpace(orNo);
		if (needsPayment && string.IsNullOrWhiteSpace(orNo))
		{
			throw new InvalidOperationException("OR number is required when fee is greater than 0.");
		}

		if (needsPayment && string.IsNullOrWhiteSpace(paymentMethod))
		{
			throw new InvalidOperationException("Payment method is required.");
		}

		if (!string.IsNullOrWhiteSpace(orNo))
		{
			EnsureOrNumberAvailable(mySqlConnection, tx, certificateId, orNo);
		}

		entry.OrNumber = orNo;
		entry.PaymentMethod = paymentMethod;

		// Make sure an issued/approved certificate always has a stable document number.
		EnsureCertificateNumber(mySqlConnection, tx, certificateId, dateTime.Year);
		// Used for printed output / QR verification.
		EnsureCertificateVerificationToken(mySqlConnection, tx, certificateId);
		DateTime? expiresAt = ResolveCertificateExpiryDate(mySqlConnection, tx, certificateId, dateTime);

		using MySqlCommand mySqlCommand = new MySqlCommand("UPDATE document_request\r\n                                   SET status='RELEASED',\r\n                                       released_at=@date,\r\n                                       released_by_user_id=@uid,\r\n                                       or_number=@or,\r\n                                       fee=@fee,\r\n                                       remarks=@remarks,\r\n                                       business_name=@bizName,\r\n                                       business_nature=@bizNature,\r\n                                       expires_at=@expiresAt\r\n                                   WHERE doc_request_id=@id\r\n                                     AND status='APPROVED'", mySqlConnection);
		mySqlCommand.Transaction = tx;


		mySqlCommand.Parameters.AddWithValue("@date", dateTime);
		mySqlCommand.Parameters.AddWithValue("@uid", UserSession.UserId);
		mySqlCommand.Parameters.AddWithValue("@or", string.IsNullOrWhiteSpace(orNo) ? ((IConvertible)DBNull.Value) : ((IConvertible)orNo));
		mySqlCommand.Parameters.AddWithValue("@fee", entry.Fee);
		mySqlCommand.Parameters.AddWithValue("@remarks", string.IsNullOrWhiteSpace(entry.Remarks) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.Remarks));
		mySqlCommand.Parameters.AddWithValue("@bizName", string.IsNullOrWhiteSpace(entry.BusinessName) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.BusinessName));
		mySqlCommand.Parameters.AddWithValue("@bizNature", string.IsNullOrWhiteSpace(entry.BusinessNature) ? ((IConvertible)DBNull.Value) : ((IConvertible)entry.BusinessNature));
		mySqlCommand.Parameters.AddWithValue("@expiresAt", expiresAt.HasValue ? expiresAt.Value : (object)DBNull.Value);
		mySqlCommand.Parameters.AddWithValue("@id", certificateId);


		if (mySqlCommand.ExecuteNonQuery() == 0)


		{


			throw new InvalidOperationException("Unable to issue. The certificate status may have changed.");


		}

		bool paymentRecorded = false;
		try
		{
			paymentRecorded = RecordCertificatePayment(mySqlConnection, certificateId, entry, dateTime, tx);
			if (needsPayment && !paymentRecorded)
			{
				throw new InvalidOperationException("Payment details are required before issuing this certificate.");
			}
		}
		catch (MySqlException ex) when (ex.Number == 1062)
		{
			throw new InvalidOperationException("OR number is already used by another certificate payment.", ex);
		}
		catch (Exception ex)
		{
			if (needsPayment)
			{
				throw new InvalidOperationException("Unable to record payment. Certificate issuance was cancelled.", ex);
			}

			ControllerDialogs.Warning(ex, "Certificate issued, but payment could not be recorded.");
		}

		object? afterSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, certificateId, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Certificates",
			"document_request",
			certificateId,
			"ISSUE",
			beforeSnapshot,
			afterSnapshot,
			paymentRecorded ? "Certificate issued with payment recorded." : "Certificate issued.");
		tx.Commit();
		if (paymentRecorded)
		{
			LogCertificateAudit(certificateId, "Payment Recorded", null);
		}
		LogCertificateAudit(certificateId, "Issued", null);
		try
		{
			OutboundNotificationService.QueueCertificateRelease(certificateId);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to queue certificate release notification.", ex);
		}


	}

	private static bool IsBarangayClearanceDocumentType(MySqlConnection conn, MySqlTransaction tx, int docTypeId)
	{
		using var cmd = new MySqlCommand(
			@"SELECT code, name
              FROM document_type
              WHERE doc_type_id = @id
              LIMIT 1",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@id", docTypeId);
		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			return false;
		}

		string code = Convert.ToString(reader["code"]) ?? string.Empty;
		string name = Convert.ToString(reader["name"]) ?? string.Empty;
		return string.Equals(code.Trim(), "BC", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(name.Trim(), "Barangay Clearance", StringComparison.OrdinalIgnoreCase);
	}

	private static int? FindLatestReleasedClearanceRequestId(MySqlConnection conn, MySqlTransaction tx, int residentId, int docTypeId)
	{
		using var cmd = new MySqlCommand(
			@"SELECT dr.doc_request_id
              FROM document_request dr
              INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
              WHERE dr.resident_id = @residentId
                AND dr.status = 'RELEASED'
                AND (dr.doc_type_id = @docTypeId OR UPPER(dt.code) = 'BC' OR UPPER(dt.name) = 'BARANGAY CLEARANCE')
              ORDER BY COALESCE(dr.expires_at, dr.released_at) DESC, dr.doc_request_id DESC
              LIMIT 1",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@residentId", residentId);
		cmd.Parameters.AddWithValue("@docTypeId", docTypeId);
		object? value = cmd.ExecuteScalar();
		if (value == null || value == DBNull.Value)
		{
			return null;
		}

		return Convert.ToInt32(value);
	}

	private static DateTime? ResolveCertificateExpiryDate(MySqlConnection conn, MySqlTransaction tx, int docRequestId, DateTime releasedAt)
	{
		using var cmd = new MySqlCommand(
			@"SELECT dt.validity_days
              FROM document_request dr
              INNER JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
              WHERE dr.doc_request_id = @id
              LIMIT 1",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@id", docRequestId);
		object? value = cmd.ExecuteScalar();
		if (value == null || value == DBNull.Value)
		{
			return null;
		}

		int validityDays = Convert.ToInt32(value);
		if (validityDays <= 0)
		{
			return null;
		}

		return releasedAt.Date.AddDays(validityDays);
	}

	private static void EnsureOrNumberAvailable(MySqlConnection conn, MySqlTransaction tx, int certificateId, string orNumber)
	{
		if (string.IsNullOrWhiteSpace(orNumber))
		{
			return;
		}

		using (var cmd = new MySqlCommand(
			       "SELECT COUNT(*) FROM document_payment WHERE or_no=@or AND doc_request_id<>@id",
			       conn,
			       tx))
		{
			cmd.Parameters.AddWithValue("@or", orNumber);
			cmd.Parameters.AddWithValue("@id", certificateId);
			int count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
			if (count > 0)
			{
				throw new InvalidOperationException("OR number is already used by another certificate payment.");
			}
		}

		using (var cmd = new MySqlCommand(
			       "SELECT COUNT(*) FROM document_request WHERE or_number=@or AND doc_request_id<>@id AND or_number IS NOT NULL AND or_number<>''",
			       conn,
			       tx))
		{
			cmd.Parameters.AddWithValue("@or", orNumber);
			cmd.Parameters.AddWithValue("@id", certificateId);
			int count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
			if (count > 0)
			{
				throw new InvalidOperationException("OR number is already used by another certificate.");
			}
		}
	}

	private bool RecordCertificatePayment(MySqlConnection conn, int certificateId, CertificateEntry entry, DateTime paidAt, MySqlTransaction? tx = null)
	{
		if (entry == null)
		{
			return false;
		}

		if (entry.Fee <= 0m && string.IsNullOrWhiteSpace(entry.OrNumber))
		{
			return false;
		}

		using (MySqlCommand existsCmd = new MySqlCommand("SELECT COUNT(*) FROM document_payment WHERE doc_request_id=@id", conn))
		{
			existsCmd.Transaction = tx;
			existsCmd.Parameters.AddWithValue("@id", certificateId);
			int count = Convert.ToInt32(existsCmd.ExecuteScalar());
			if (count > 0)
			{
				return true;
			}
		}

		string method = string.IsNullOrWhiteSpace(entry.PaymentMethod) ? "Cash" : entry.PaymentMethod.Trim();
		string orNo = entry.OrNumber?.Trim() ?? string.Empty;

		using MySqlCommand insertCmd = new MySqlCommand(
			@"INSERT INTO document_payment
                (doc_request_id, amount, or_no, payment_method, paid_at, received_by_user_id)
              VALUES
                (@id, @amount, @or, @method, @paidAt, @uid)", conn);
		insertCmd.Transaction = tx;
		insertCmd.Parameters.AddWithValue("@id", certificateId);
		insertCmd.Parameters.AddWithValue("@amount", entry.Fee);
		insertCmd.Parameters.AddWithValue("@or", string.IsNullOrWhiteSpace(orNo) ? (object)DBNull.Value : orNo);
		insertCmd.Parameters.AddWithValue("@method", method);
		insertCmd.Parameters.AddWithValue("@paidAt", paidAt);
		insertCmd.Parameters.AddWithValue("@uid", UserSession.UserId);
		insertCmd.ExecuteNonQuery();
		return true;
	}





	private void CancelCertificate(int certificateId, string cancelReason)


	{
		if (!Permissions.CanCancelCertificates)
		{
			throw new UnauthorizedAccessException("You do not have permission to cancel certificates.");
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();
		object? beforeSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, certificateId, tx);


		using MySqlCommand mySqlCommand = new MySqlCommand("UPDATE document_request\r\n                                   SET status='CANCELLED',\r\n                                       remarks = CASE\r\n                                           WHEN @reason IS NULL OR @reason = '' THEN remarks\r\n                                           WHEN remarks IS NULL OR remarks = '' THEN CONCAT('[CANCELLED] ', @reason)\r\n                                           ELSE CONCAT(remarks, CHAR(10), '[CANCELLED] ', @reason)\r\n                                       END\r\n                                   WHERE doc_request_id=@id\r\n                                     AND status IN ('SUBMITTED','APPROVED')", mySqlConnection);
		mySqlCommand.Transaction = tx;


		mySqlCommand.Parameters.AddWithValue("@id", certificateId);
		mySqlCommand.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(cancelReason) ? ((IConvertible)DBNull.Value) : ((IConvertible)cancelReason));


		if (mySqlCommand.ExecuteNonQuery() == 0)


		{


			throw new InvalidOperationException("Unable to cancel. The certificate status may have changed.");


		}


		object? afterSnapshot = ReadCertificateAuditSnapshot(mySqlConnection, certificateId, tx);
		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Certificates",
			"document_request",
			certificateId,
			"CANCEL",
			beforeSnapshot,
			afterSnapshot,
			cancelReason);
		tx.Commit();
		LogCertificateAudit(certificateId, "Cancelled", cancelReason);


	}





	private void LoadCertificatesForResident(int residentId)


	{
		BeginModuleLoading("Loading certificates...");


		try
		{
			DataTable dataTable = QueryCertificatesForResident(residentId);
			ApplyCertificatesData(dataTable);
		}


		catch (Exception ex)


		{


			ControllerDialogs.Error(ex, "Unable to load certificates.", "Error");


		}
		finally
		{
			EndModuleLoading();
		}


	}

	private DataTable QueryCertificatesForResident(int residentId)
	{
		return DbHelper.LoadTable(@"SELECT dr.doc_request_id AS certificate_id,
                                              dr.document_no AS certificate_no,
                                              dt.name AS certificate_type,
                                              dr.purpose,
                                              dr.status AS status_raw,
                                              CASE dr.status
                                                  WHEN 'SUBMITTED' THEN 'Requested'
                                                  WHEN 'APPROVED' THEN 'Approved'
                                                  WHEN 'RELEASED' THEN 'Issued'
                                                  WHEN 'CANCELLED' THEN 'Cancelled'
                                                  WHEN 'REJECTED' THEN 'Rejected'
                                                  WHEN 'DRAFT' THEN 'Draft'
                                                  ELSE dr.status
                                              END AS status,
                                              dr.fee,
                                              dr.or_number,
                                              dr.requested_at,
                                              dr.approved_at,
                                              DATE(dr.released_at) AS issued_date,
                                              dr.business_name,
                                              dr.business_nature,
                                               dr.remarks,
                                               dr.print_count,
                                               dr.last_printed_at,
                                               dr.verification_token,
                                               dr.renewed_from_request_id,
                                               dt.validity_days,
                                               COALESCE(
                                                  dr.expires_at,
                                                  CASE
                                                      WHEN dr.released_at IS NOT NULL AND dt.validity_days IS NOT NULL THEN DATE_ADD(dr.released_at, INTERVAL dt.validity_days DAY)
                                                      ELSE NULL
                                                  END
                                               ) AS valid_until,
                                              COALESCE(pay.amount, dr.fee) AS payment_amount,
                                              CASE
                                                  WHEN pay.payment_method IS NOT NULL THEN pay.payment_method
                                                  WHEN dr.released_at IS NOT NULL AND dr.fee IS NOT NULL AND dr.fee > 0 THEN 'Cash'
                                                  WHEN dr.released_at IS NOT NULL AND dr.or_number IS NOT NULL AND dr.or_number <> '' THEN 'Cash'
                                                  ELSE NULL
                                              END AS payment_method,
                                              COALESCE(pay.or_no, dr.or_number) AS payment_or_no,
                                              COALESCE(pay.paid_at, dr.released_at) AS payment_date,
                                              COALESCE(pay.received_by_user_id, dr.released_by_user_id) AS payment_received_by,
                                              COALESCE(payuser.username, iss.username) AS payment_received_by_name,
                                              dr.requested_by_user_id AS requested_by,
                                              dr.approved_by_user_id AS approved_by,
                                              dr.released_by_user_id AS issued_by,
                                              req.username AS requested_by_name,
                                              app.username AS approved_by_name,
                                              iss.username AS issued_by_name
                                       FROM document_request dr
                                       LEFT JOIN document_type dt ON dt.doc_type_id = dr.doc_type_id
                                       LEFT JOIN document_payment pay ON pay.payment_id = (
                                            SELECT p.payment_id
                                            FROM document_payment p
                                            WHERE p.doc_request_id = dr.doc_request_id
                                            ORDER BY p.paid_at DESC, p.payment_id DESC
                                            LIMIT 1
                                       )
                                       LEFT JOIN user_account payuser ON pay.received_by_user_id = payuser.user_id
                                       LEFT JOIN user_account req ON dr.requested_by_user_id = req.user_id
                                       LEFT JOIN user_account app ON dr.approved_by_user_id = app.user_id
                                       LEFT JOIN user_account iss ON dr.released_by_user_id = iss.user_id
                                       WHERE dr.resident_id = @rid
                                       ORDER BY dr.requested_at DESC, dr.doc_request_id DESC", cmd => cmd.Parameters.AddWithValue("@rid", residentId));
	}

	private void ApplyCertificatesData(DataTable dataTable)
	{
		_certTable = dataTable;
		_certPageIndex = 0;
		ApplyCertificateFilters(resetPage: true);
		UpdateResidentInsightPanel();
	}





	private void PopulateCertificateDetails(DataGridViewRow row)


	{


		_selectedCertificateId = Convert.ToInt32(row.Cells["certificate_id"].Value);


		string cellText = GetCellText(row, "certificate_no");


		_certNumber.Text = ((cellText == "-") ? $"#{_selectedCertificateId}" : cellText);
		string tokenText = GetCellText(row, "verification_token");
		_certVerificationToken = tokenText == "-" ? string.Empty : tokenText;


		string cellText2 = GetCellText(row, "status");


		_certStatus.Text = ((cellText2 == "-") ? "Requested" : cellText2);


		UpdateCertificateStatusBadge(_certStatus.Text);


		_certRequestedAt.Text = FormatPersonStamp(row.Cells["requested_at"]?.Value, row.Cells["requested_by_name"]?.Value);


		_certApprovedAt.Text = FormatPersonStamp(row.Cells["approved_at"]?.Value, row.Cells["approved_by_name"]?.Value);


		_certIssuedAt.Text = FormatPersonStamp(row.Cells["issued_date"]?.Value, row.Cells["issued_by_name"]?.Value);

		string rawStatus = row.Cells["status_raw"]?.Value?.ToString();
		if (string.IsNullOrWhiteSpace(rawStatus))
		{
			rawStatus = row.Cells["status"]?.Value?.ToString();
		}
		rawStatus ??= string.Empty;
		DateTime? requestedAt = TryParseDateTime(row.Cells["requested_at"]?.Value);
		DateTime? approvedAt = TryParseDateTime(row.Cells["approved_at"]?.Value);
		UpdateCertificateSlaBadge(rawStatus, requestedAt, approvedAt);


		string text = row.Cells["certificate_type"]?.Value?.ToString() ?? string.Empty;


		if (!string.IsNullOrWhiteSpace(text))


		{


			if (!_certType.Items.Contains(text))


			{


				_certType.Items.Add(text);


			}


			_certType.SelectedItem = text;


		}


		else


		{


			_certType.SelectedIndex = -1;


		}


		_certTypeValue.Text = (string.IsNullOrWhiteSpace(text) ? "-" : text);


		string text2 = row.Cells["purpose"]?.Value?.ToString() ?? string.Empty;


		_certPurpose.Text = text2;


		_certPurposeValue.Text = (string.IsNullOrWhiteSpace(text2) ? "-" : text2);


		string text3 = row.Cells["or_number"]?.Value?.ToString() ?? string.Empty;


		_certOR.Text = text3;


		_certOrValue.Text = (string.IsNullOrWhiteSpace(text3) ? "-" : text3);


		object obj = row.Cells["fee"]?.Value;


		decimal num = ((obj == DBNull.Value || obj == null) ? 0m : Convert.ToDecimal(obj));


		_certFee.Value = num;


		_certFeeValue.Text = ((num > 0m) ? num.ToString("N2") : "-");


		object obj2 = row.Cells["issued_date"]?.Value;


		_certIssuedDateValue.Text = FormatDate(obj2);

		_certValidUntilValue.Text = FormatDate(row.Cells["valid_until"]?.Value);

		int? cellNullableInt = GetCellNullableInt(row, "print_count");
		_certPrintCountValue.Text = cellNullableInt.HasValue ? cellNullableInt.Value.ToString() : "-";

		_certLastPrintedValue.Text = FormatDateTime(row.Cells["last_printed_at"]?.Value);

		object obj3 = row.Cells["payment_amount"]?.Value;
		decimal num2 = ((obj3 == DBNull.Value || obj3 == null) ? 0m : Convert.ToDecimal(obj3));
		_certPaymentAmountValue.Text = ((num2 > 0m) ? num2.ToString("N2") : "-");

		string paymentMethod = row.Cells["payment_method"]?.Value?.ToString() ?? string.Empty;
		_certPaymentMethodValue.Text = (string.IsNullOrWhiteSpace(paymentMethod) ? "-" : paymentMethod);

		string paymentOrNo = row.Cells["payment_or_no"]?.Value?.ToString() ?? string.Empty;
		_certPaymentOrValue.Text = (string.IsNullOrWhiteSpace(paymentOrNo) ? "-" : paymentOrNo);

		_certPaymentDateValue.Text = FormatDateTime(row.Cells["payment_date"]?.Value);

		string paymentReceivedBy = row.Cells["payment_received_by_name"]?.Value?.ToString() ?? string.Empty;
		_certPaymentReceivedByValue.Text = (string.IsNullOrWhiteSpace(paymentReceivedBy) ? "-" : paymentReceivedBy);

		object? validUntilValue = row.Cells["valid_until"]?.Value;
		if (validUntilValue != null && validUntilValue != DBNull.Value && DateTime.TryParse(validUntilValue.ToString(), out var result))


		{


			_certValidUntil.Value = result;


			_certValidUntil.Checked = true;


		}


		else


		{


			_certValidUntil.Value = DateTime.Today;


			_certValidUntil.Checked = false;


		}


		string text4 = row.Cells["business_name"]?.Value?.ToString() ?? string.Empty;


		string text5 = row.Cells["business_nature"]?.Value?.ToString() ?? string.Empty;


		string text6 = row.Cells["remarks"]?.Value?.ToString() ?? string.Empty;


		_certBusinessName.Text = text4;


		_certBusinessNature.Text = text5;


		_certRemarks.Text = text6;


		_certBusinessNameValue.Text = (string.IsNullOrWhiteSpace(text4) ? "-" : text4);


		_certBusinessNatureValue.Text = (string.IsNullOrWhiteSpace(text5) ? "-" : text5);


		_certRemarksValue.Text = (string.IsNullOrWhiteSpace(text6) ? "-" : text6);


		UpdateBusinessFieldsVisibility();


		SetCertificateEditing(enabled: false);


		UpdateCertificateActionState();


	}





	private void ClearCertificates()


	{


		_certGrid.DataSource = null;


		_certTable = null;
		_certPageIndex = 0;
		UpdateCertificatePagerState();


		ResetCertificateDetails();


		UpdateCertificateActionState();


		UpdateCertificateSummary();


		UpdateCertificateEmptyState();
		UpdateResidentInsightPanel();


	}





	private void RefreshCertificates(int? selectId)


	{


		if (_selectedResidentId.HasValue)


		{


			LoadCertificatesForResident(_selectedResidentId.Value);


			LoadResidentHistory(_selectedResidentId.Value);


			if (selectId.HasValue)


			{


				SelectCertificateRow(selectId.Value);


			}


		}


	}





	private void LoadResidentHistory(int residentId)


	{
		BeginModuleLoading("Loading history...");


		try
		{
			DataTable dataTable = QueryResidentHistory(residentId);
			ApplyResidentHistoryData(dataTable);
		}


		catch (Exception ex)


		{
			ControllerDialogs.Error(ex, "Unable to load certificate history.", "Error");
			ClearHistory();


		}
		finally
		{
			EndModuleLoading();
		}


	}

	private DataTable QueryResidentHistory(int residentId)
	{
		DataTable dataTable = DbHelper.LoadTable(@"SELECT h.action_at,
                                              h.module,
                                              h.action,
                                              h.details,
                                              h.action_by
                                       FROM (
                                           SELECT a.action_at AS action_at,
                                                  'Certificates' AS module,
                                                  a.action AS action,
                                                  CONCAT(COALESCE(dr.document_no, CONCAT('#', dr.doc_request_id)),
                                                         ' - ', COALESCE(dt.name, 'Document')) AS details,
                                                  u.username AS action_by
                                           FROM certificate_audit a
                                           INNER JOIN document_request dr ON a.certificate_id = dr.doc_request_id
                                           LEFT JOIN document_type dt ON dr.doc_type_id = dt.doc_type_id
                                           LEFT JOIN user_account u ON a.action_by = u.user_id
                                           WHERE dr.resident_id = @rid
                                           UNION ALL
                                           SELECT l.action_at,
                                                  l.module,
                                                  l.action,
                                                  l.details,
                                                  u.username
                                           FROM activity_log l
                                           LEFT JOIN user_account u ON l.action_by = u.user_id
                                           WHERE l.resident_id = @rid
                                           UNION ALL
                                           SELECT t.transferred_at AS action_at,
                                                  'Residents' AS module,
                                                  'Transfer' AS action,
                                                  CONCAT('Address moved: ',
                                                         COALESCE(t.old_address, '-'),
                                                         ' -> ',
                                                         COALESCE(t.new_address, '-')) AS details,
                                                  u.username AS action_by
                                           FROM resident_transfer_history t
                                           LEFT JOIN user_account u ON t.transferred_by_user_id = u.user_id
                                           WHERE t.resident_id = @rid
                                       ) h
                                       ORDER BY h.action_at DESC", cmd => cmd.Parameters.AddWithValue("@rid", residentId));

		if (dataTable.Rows.Count == 0)
		{
			dataTable = LoadDerivedHistory(residentId);
		}

		if (dataTable.Rows.Count == 0)
		{
			dataTable = LoadDerivedHistory(null);
		}

		return dataTable;
	}

	private void ApplyResidentHistoryData(DataTable dataTable)
	{
		_historyTable = dataTable;
		_historyGrid.DataSource = dataTable;
		ApplyHistoryGridFormatting();
		ApplyHistoryFilters();
		UpdateResidentInsightPanel();
	}

	private DataTable LoadDerivedHistory(int? residentId)
	{
		string residentsSql = @"SELECT r.created_at AS action_at,
                                       'Residents' AS module,
                                       'Registered' AS action,
                                       CONCAT(r.last_name, ', ', r.first_name, ' ', COALESCE(r.middle_name, '')) AS details,
                                       NULL AS action_by
                                FROM resident r";
		if (residentId.HasValue)
		{
			residentsSql += " WHERE r.resident_id = @rid";
		}

		string certificatesSql = @"SELECT COALESCE(dr.requested_at, dr.approved_at, dr.released_at) AS action_at,
                                          'Certificates' AS module,
                                          CASE dr.status
                                              WHEN 'SUBMITTED' THEN 'Requested'
                                              WHEN 'APPROVED' THEN 'Approved'
                                              WHEN 'RELEASED' THEN 'Issued'
                                              WHEN 'CANCELLED' THEN 'Cancelled'
                                              WHEN 'REJECTED' THEN 'Rejected'
                                              WHEN 'DRAFT' THEN 'Draft'
                                              ELSE dr.status
                                          END AS action,
                                          CONCAT(COALESCE(dr.document_no, CONCAT('#', dr.doc_request_id)),
                                                 ' - ', COALESCE(dt.name, 'Document')) AS details,
                                          u.username AS action_by
                                   FROM document_request dr
                                   LEFT JOIN document_type dt ON dr.doc_type_id = dt.doc_type_id
                                   LEFT JOIN user_account u ON dr.released_by_user_id = u.user_id";
		if (residentId.HasValue)
		{
			certificatesSql += " WHERE dr.resident_id = @rid";
		}
		certificatesSql = "SELECT * FROM (" + certificatesSql + ") cert WHERE cert.action_at IS NOT NULL";

		string blotterSql = @"SELECT b.created_at AS action_at,
                                     'Blotter' AS module,
                                     CASE b.status
                                         WHEN 'OPEN' THEN 'Ongoing'
                                         WHEN 'ONGOING' THEN 'Ongoing'
                                         WHEN 'SETTLED' THEN 'Settled'
                                         WHEN 'REFERRED' THEN 'Referred'
                                         WHEN 'CLOSED' THEN 'Closed'
                                         ELSE b.status
                                     END AS action,
                                     CONCAT(b.respondent_name, ' - ', b.incident_type) AS details,
                                     u.username AS action_by
                              FROM case_record b
                              LEFT JOIN user_account u ON b.recorded_by = u.user_id";
		if (residentId.HasValue)
		{
			blotterSql += " WHERE b.complainant_id = @rid";
		}

		string transferSql = @"SELECT t.transferred_at AS action_at,
                                      'Residents' AS module,
                                      'Transfer' AS action,
                                      CONCAT('Address moved: ',
                                             COALESCE(t.old_address, '-'),
                                             ' -> ',
                                             COALESCE(t.new_address, '-')) AS details,
                                      u.username AS action_by
                               FROM resident_transfer_history t
                               LEFT JOIN user_account u ON u.user_id = t.transferred_by_user_id";
		if (residentId.HasValue)
		{
			transferSql += " WHERE t.resident_id = @rid";
		}

		string sql = @"SELECT h.action_at,
                              h.module,
                              h.action,
                              h.details,
                              h.action_by
                       FROM (
                           " + residentsSql + @"
                           UNION ALL
                           " + certificatesSql + @"
                           UNION ALL
                           " + blotterSql + @"
                           UNION ALL
                           " + transferSql + @"
                       ) h
                       ORDER BY h.action_at DESC";

		return DbHelper.LoadTable(sql, cmd =>
		{
			if (residentId.HasValue)
			{
				cmd.Parameters.AddWithValue("@rid", residentId.Value);
			}
		});
	}

	private void ApplyHistoryGridFormatting()
	{
		if (_historyGrid.Columns["action_at"] != null)
		{
			_historyGrid.Columns["action_at"].HeaderText = "Date";
			_historyGrid.Columns["action_at"].DefaultCellStyle.Format = "MMM dd, yyyy h:mm tt";
			_historyGrid.Columns["action_at"].FillWeight = 22F;
		}
		if (_historyGrid.Columns["module"] != null)
		{
			_historyGrid.Columns["module"].HeaderText = "Module";
			_historyGrid.Columns["module"].FillWeight = 14F;
		}
		if (_historyGrid.Columns["action"] != null)
		{
			_historyGrid.Columns["action"].HeaderText = "Action";
			_historyGrid.Columns["action"].FillWeight = 18F;
		}
		if (_historyGrid.Columns["details"] != null)
		{
			_historyGrid.Columns["details"].HeaderText = "Details";
			_historyGrid.Columns["details"].FillWeight = 34F;
		}
		if (_historyGrid.Columns["action_by"] != null)
		{
			_historyGrid.Columns["action_by"].HeaderText = "By";
			_historyGrid.Columns["action_by"].FillWeight = 12F;
		}
	}

	private void LoadAllHistory()
	{
		BeginModuleLoading("Loading history...");

		try
		{
			DataTable dataTable = DbHelper.LoadTable(@"SELECT h.action_at,
                                              h.module,
                                              h.action,
                                              h.details,
                                              h.action_by
                                       FROM (
                                           SELECT a.action_at AS action_at,
                                                  'Certificates' AS module,
                                                  a.action AS action,
                                                  CONCAT(COALESCE(dr.document_no, CONCAT('#', dr.doc_request_id)),
                                                         ' - ', COALESCE(dt.name, 'Document')) AS details,
                                                  u.username AS action_by
                                           FROM certificate_audit a
                                           INNER JOIN document_request dr ON a.certificate_id = dr.doc_request_id
                                           LEFT JOIN document_type dt ON dr.doc_type_id = dt.doc_type_id
                                           LEFT JOIN user_account u ON a.action_by = u.user_id
                                           UNION ALL
                                           SELECT l.action_at,
                                                  l.module,
                                                  l.action,
                                                  l.details,
                                                  u.username
                                           FROM activity_log l
                                           LEFT JOIN user_account u ON l.action_by = u.user_id
                                           UNION ALL
                                           SELECT t.transferred_at AS action_at,
                                                  'Residents' AS module,
                                                  'Transfer' AS action,
                                                  CONCAT('Address moved: ',
                                                         COALESCE(t.old_address, '-'),
                                                         ' -> ',
                                                         COALESCE(t.new_address, '-')) AS details,
                                                  u.username AS action_by
                                           FROM resident_transfer_history t
                                           LEFT JOIN user_account u ON t.transferred_by_user_id = u.user_id
                                       ) h
                                       ORDER BY h.action_at DESC");

			_historyTable = dataTable;
			_historyGrid.DataSource = dataTable;
			ApplyHistoryGridFormatting();

			ApplyHistoryFilters();
			UpdateResidentInsightPanel();
		}
		catch (Exception ex)
		{
			var fallback = LoadDerivedHistory(null);
			if (fallback.Rows.Count > 0)
			{
				_historyTable = fallback;
				_historyGrid.DataSource = fallback;
				ApplyHistoryGridFormatting();
				ApplyHistoryFilters();
				return;
			}

			ControllerDialogs.Error(ex, "Unable to load activity history.", "Error");
			ClearHistory();
		}
		finally
		{
			EndModuleLoading();
		}
	}





	private void ClearHistory()


	{


		_historyGrid.DataSource = null;


		_historyTable = null;


		UpdateHistorySummary();


		UpdateHistoryEmptyState();
		UpdateResidentInsightPanel();


	}





	private void ResetCertificateDetails()


	{


		_selectedCertificateId = null;
		_certVerificationToken = string.Empty;


		_certNumber.Text = "-";


		_certStatus.Text = "No certificate selected.";


		UpdateCertificateStatusBadge(_certStatus.Text);

		_certSla.Text = string.Empty;
		_certSla.Visible = false;


		_certRequestedAt.Text = "-";


		_certApprovedAt.Text = "-";


		_certIssuedAt.Text = "-";


		_certType.SelectedIndex = -1;


		_certPurpose.Text = string.Empty;


		_certFee.Value = 0m;


		_certOR.Text = string.Empty;


		_certValidUntil.Value = DateTime.Today;


		_certValidUntil.Checked = false;


		_certBusinessName.Text = string.Empty;


		_certBusinessNature.Text = string.Empty;


		_certRemarks.Text = string.Empty;


		_certTypeValue.Text = "-";


		_certPurposeValue.Text = "-";


		_certFeeValue.Text = "-";


		_certOrValue.Text = "-";


		_certIssuedDateValue.Text = "-";
		_certValidUntilValue.Text = "-";
		_certPrintCountValue.Text = "-";
		_certLastPrintedValue.Text = "-";
		_certPaymentAmountValue.Text = "-";
		_certPaymentMethodValue.Text = "-";
		_certPaymentOrValue.Text = "-";
		_certPaymentDateValue.Text = "-";
		_certPaymentReceivedByValue.Text = "-";


		_certBusinessNameValue.Text = "-";


		_certBusinessNatureValue.Text = "-";


		_certRemarksValue.Text = "-";


		UpdateBusinessFieldsVisibility();


		SetCertificateEditing(enabled: false);


	}





	private void SetCertificateEditing(bool enabled)


	{


		_isCertEditing = false;


		_certGrid.Enabled = true;


		_btnCertNew.Text = "New Request";


		_btnCertCancel.Text = "Cancel";


		UpdateCertificateEditorState();


		UpdateCertificateActionState();


	}





	private void UpdateCertificateEditorState()


	{


		_certType.Enabled = false;


		_certPurpose.ReadOnly = true;


		_certFee.Enabled = false;


		_certOR.ReadOnly = true;


		_certValidUntil.Enabled = false;


		_certBusinessName.ReadOnly = true;


		_certBusinessNature.ReadOnly = true;


		_certRemarks.ReadOnly = true;


		_certPurpose.BackColor = Color.White;


		_certOR.BackColor = Color.White;


		_certBusinessName.BackColor = Color.White;


		_certBusinessNature.BackColor = Color.White;


		_certRemarks.BackColor = Color.White;


	}





	private void UpdateCertificateActionState()


	{


		bool hasValue = _selectedResidentId.HasValue;


		bool hasValue2 = _selectedCertificateId.HasValue;
		_residentDocumentsFooterPanel.Visible = hasValue;


		_btnCertNew.Enabled = hasValue;


		_btnCertRefresh.Enabled = hasValue;

		_btnCertAttachments.Enabled = hasValue && hasValue2;


		_btnCertExport.Enabled = _certGrid.Rows.Count > 0 && Permissions.CanExportCertificates;


		if (!hasValue || !hasValue2)


		{


			_btnCertEdit.Enabled = false;


			_btnCertApprove.Enabled = false;


			_btnCertIssue.Enabled = false;


			_btnCertPrint.Enabled = false;


			_btnCertCancel.Enabled = false;


		}


		else


		{


			string a = _certStatus.Text ?? string.Empty;


			bool flag = string.Equals(a, "Requested", StringComparison.OrdinalIgnoreCase);


			bool flag2 = string.Equals(a, "Approved", StringComparison.OrdinalIgnoreCase);


			bool enabled = string.Equals(a, "Issued", StringComparison.OrdinalIgnoreCase);


			_btnCertEdit.Enabled = flag;


			_btnCertApprove.Enabled = flag && Permissions.CanApproveCertificates;


			_btnCertIssue.Enabled = flag2 && Permissions.CanIssueCertificates;


			_btnCertPrint.Enabled = enabled;


			_btnCertCancel.Enabled = (flag || flag2) && Permissions.CanCancelCertificates;


		}

		UpdateCertificatePagerState();


	}





	private void SelectCertificateRow(int certificateId)
	{
		if (!EnsureCertificatePageContains(certificateId))
		{
			return;
		}

		TrySelectCertificateRowInCurrentPage(certificateId, populateDetails: true);
	}

	private bool EnsureCertificatePageContains(int certificateId)
	{
		if (_certTable == null)
		{
			return false;
		}

		DataView view = _certTable.DefaultView;
		for (int i = 0; i < view.Count; i++)
		{
			object? raw = view[i]["certificate_id"];
			if (raw == null || raw == DBNull.Value || Convert.ToInt32(raw) != certificateId)
			{
				continue;
			}

			int requiredPageIndex = i / _certificatePageSize;
			if (requiredPageIndex != _certPageIndex)
			{
				_certPageIndex = requiredPageIndex;
				ApplyCertificatePaging(resetPage: false);
			}

			return true;
		}

		return false;
	}

	private bool TrySelectCertificateRowInCurrentPage(int certificateId, bool populateDetails = false)
	{
		foreach (DataGridViewRow item in (IEnumerable)_certGrid.Rows)
		{
			if (item.Cells["certificate_id"]?.Value == null || Convert.ToInt32(item.Cells["certificate_id"].Value) != certificateId)
			{
				continue;
			}

			item.Selected = true;
			_certGrid.CurrentCell = item.Cells["certificate_type"] ?? item.Cells[0];
			if (populateDetails)
			{
				PopulateCertificateDetails(item);
			}

			return true;
		}

		return false;
	}

	private string EnsureCertificateNumber(MySqlConnection conn, MySqlTransaction tx, int docRequestId, int year)
	{
		if (year < 2000 || year > 3000)
		{
			year = DateTime.Today.Year;
		}

		using var select = new MySqlCommand(
			@"SELECT doc_type_id, document_no
              FROM document_request
              WHERE doc_request_id=@id
              LIMIT 1
              FOR UPDATE",
			conn,
			tx);
		select.Parameters.AddWithValue("@id", docRequestId);

		int docTypeId = 0;
		string existing = string.Empty;
		using (var reader = select.ExecuteReader())
		{
			if (!reader.Read())
			{
				throw new InvalidOperationException($"Certificate request {docRequestId} was not found.");
			}

			docTypeId = reader["doc_type_id"] != DBNull.Value ? Convert.ToInt32(reader["doc_type_id"]) : 0;
			existing = reader["document_no"]?.ToString() ?? string.Empty;
		}

		if (!string.IsNullOrWhiteSpace(existing))
		{
			return existing.Trim();
		}

		if (docTypeId <= 0)
		{
			throw new InvalidOperationException("Certificate request is missing document type.");
		}

		string newNo = GenerateCertificateNumber(conn, tx, docTypeId, year);
		using var update = new MySqlCommand(
			@"UPDATE document_request
              SET document_no=@no
              WHERE doc_request_id=@id
                AND (document_no IS NULL OR document_no = '')",
			conn,
			tx);
		update.Parameters.AddWithValue("@no", newNo);
		update.Parameters.AddWithValue("@id", docRequestId);
		update.ExecuteNonQuery();
		return newNo;
	}

	private string EnsureCertificateVerificationToken(int docRequestId)
	{
		using MySqlConnection conn = DBConnection.GetConnection();
		conn.Open();
		using MySqlTransaction tx = conn.BeginTransaction();
		string token = EnsureCertificateVerificationToken(conn, tx, docRequestId);
		tx.Commit();
		_certVerificationToken = token;
		return token;
	}

	private string EnsureCertificateVerificationToken(MySqlConnection conn, MySqlTransaction tx, int docRequestId)
	{
		using var select = new MySqlCommand(
			@"SELECT verification_token
              FROM document_request
              WHERE doc_request_id=@id
              LIMIT 1
              FOR UPDATE",
			conn,
			tx);
		select.Parameters.AddWithValue("@id", docRequestId);

		string existing = Convert.ToString(select.ExecuteScalar()) ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(existing))
		{
			return existing.Trim();
		}

		for (int attempt = 0; attempt < 6; attempt++)
		{
			string token = GenerateVerificationToken();

			using (var check = new MySqlCommand(
				       "SELECT COUNT(*) FROM document_request WHERE verification_token=@t LIMIT 1",
				       conn,
				       tx))
			{
				check.Parameters.AddWithValue("@t", token);
				int count = Convert.ToInt32(check.ExecuteScalar() ?? 0);
				if (count > 0)
				{
					continue;
				}
			}

			using var update = new MySqlCommand(
				@"UPDATE document_request
                  SET verification_token=@t,
                      verification_token_created_at = COALESCE(verification_token_created_at, NOW())
                  WHERE doc_request_id=@id
                    AND (verification_token IS NULL OR verification_token='')",
				conn,
				tx);
			update.Parameters.AddWithValue("@t", token);
			update.Parameters.AddWithValue("@id", docRequestId);

			try
			{
				update.ExecuteNonQuery();
				return token;
			}
			catch (MySqlException ex) when (ex.Number == 1062)
			{
				// Token collision (unique index); retry.
			}
		}

		throw new InvalidOperationException("Unable to allocate a certificate verification token. Please retry.");
	}

	private static string GenerateVerificationToken()
	{
		Span<byte> bytes = stackalloc byte[10];
		RandomNumberGenerator.Fill(bytes);

		const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		Span<char> chars = stackalloc char[16];

		int bitBuffer = 0;
		int bitCount = 0;
		int index = 0;

		foreach (byte b in bytes)
		{
			bitBuffer = (bitBuffer << 8) | b;
			bitCount += 8;

			while (bitCount >= 5 && index < chars.Length)
			{
				int charIndex = (bitBuffer >> (bitCount - 5)) & 31;
				chars[index++] = alphabet[charIndex];
				bitCount -= 5;
			}
		}

		string raw = new string(chars);
		return $"{raw[..4]}-{raw[4..8]}-{raw[8..12]}-{raw[12..16]}";
	}

	private static string BuildCertificateQrPayload(string certificateNo, string verificationToken)
	{
		string docNo = (certificateNo ?? string.Empty).Trim();
		string token = (verificationToken ?? string.Empty).Trim();
		return $"BSYS|CERT|{docNo}|{token}";
	}

	private static void DrawQrCode(Graphics graphics, Rectangle target, string payload)
	{
		try
		{
			using var generator = new QRCodeGenerator();
			using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
			using var qrCode = new QRCode(data);
			using Bitmap bitmap = qrCode.GetGraphic(20);
			graphics.DrawImage(bitmap, target);
		}
		catch
		{
			// Ignore QR rendering issues; token text will still print.
		}
	}

	private static string GenerateCertificateNumber(MySqlConnection conn, MySqlTransaction tx, int docTypeId, int year)
	{
		string code = GetDocumentTypeCode(conn, tx, docTypeId);
		int next = AllocateDocumentSequence(conn, tx, docTypeId, year);
		return $"{code}-{year}-{next:0000}";
	}

	private static string GetDocumentTypeCode(MySqlConnection conn, MySqlTransaction tx, int docTypeId)
	{
		using var cmd = new MySqlCommand(
			"SELECT code FROM document_type WHERE doc_type_id=@id LIMIT 1",
			conn,
			tx);
		cmd.Parameters.AddWithValue("@id", docTypeId);
		string code = Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;
		code = code.Trim();
		return string.IsNullOrWhiteSpace(code) ? "DOC" : code;
	}

	private static int AllocateDocumentSequence(MySqlConnection conn, MySqlTransaction tx, int docTypeId, int year)
	{
		using var select = new MySqlCommand(
			@"SELECT last_no
              FROM document_number_sequence
              WHERE doc_type_id=@id AND year=@year
              FOR UPDATE",
			conn,
			tx);
		select.Parameters.AddWithValue("@id", docTypeId);
		select.Parameters.AddWithValue("@year", year);
		object? existing = select.ExecuteScalar();

		int next;
		if (existing == null || existing == DBNull.Value)
		{
			next = 1;
			using var insert = new MySqlCommand(
				@"INSERT INTO document_number_sequence (doc_type_id, year, last_no)
                  VALUES (@id, @year, @no)",
				conn,
				tx);
			insert.Parameters.AddWithValue("@id", docTypeId);
			insert.Parameters.AddWithValue("@year", year);
			insert.Parameters.AddWithValue("@no", next);
			insert.ExecuteNonQuery();
			return next;
		}

		int last = Convert.ToInt32(existing);
		next = last + 1;

		using var update = new MySqlCommand(
			@"UPDATE document_number_sequence
              SET last_no=@no
              WHERE doc_type_id=@id AND year=@year",
			conn,
			tx);
		update.Parameters.AddWithValue("@no", next);
		update.Parameters.AddWithValue("@id", docTypeId);
		update.Parameters.AddWithValue("@year", year);
		update.ExecuteNonQuery();
		return next;
	}





	private Control BuildHistoryTab()
{
    ConfigureHistoryDesignerControls();

    if (historyContainer != null)
    {
        historyContainer.Dock = DockStyle.Fill;
        if (historyContainer.Parent != null)
        {
            historyContainer.Parent.Controls.Remove(historyContainer);
        }
        return historyContainer;
    }

    return new Panel { Dock = DockStyle.Fill };
}

	private void PrepareDetailEditors()


	{


		UiTheme.StyleTextBox(_editFirstName);


		UiTheme.StyleTextBox(_editMiddleName);


		UiTheme.StyleTextBox(_editLastName);


		UiTheme.StyleTextBox(_editContact);
		UiTheme.StyleTextBox(_editGender);
		UiTheme.StyleTextBox(_editCivil);
		UiTheme.StyleTextBox(_editStatus);
		UiTheme.StyleComboBoxes(_editGenderCombo, _editCivilCombo, _editStatusCombo, _editBarangay, _editPurok, _editHousehold);
		_editGenderCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		_editCivilCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		_editStatusCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		_editGenderCombo.Items.Clear();
		_editGenderCombo.Items.AddRange(new object[] { "M", "F" });
		_editCivilCombo.Items.Clear();
		_editCivilCombo.Items.AddRange(new object[] { "Single", "Married", "Widowed", "Separated" });
		_editStatusCombo.Items.Clear();
		_editStatusCombo.Items.AddRange(new object[] { "ACTIVE", "DECEASED", "MOVED_OUT" });
		_editGenderCombo.SelectedIndex = 0;
		_editCivilCombo.SelectedIndex = 0;
		_editStatusCombo.SelectedIndex = 0;
		_editFirstName.MaxLength = 100;
		_editMiddleName.MaxLength = 100;
		_editLastName.MaxLength = 100;
		_editContact.MaxLength = 24;
		_editGender.Visible = false;
		_editCivil.Visible = false;
		_editStatus.Visible = false;
		_editBarangay.DropDownStyle = ComboBoxStyle.DropDownList;
		_editPurok.DropDownStyle = ComboBoxStyle.DropDownList;
		_editHousehold.DropDownStyle = ComboBoxStyle.DropDownList;


		_editDob.Format = DateTimePickerFormat.Short;


		_editDob.Font = UiTheme.BodyFont;


		_editFirstName.Dock = DockStyle.Fill;


		_editMiddleName.Dock = DockStyle.Fill;


		_editLastName.Dock = DockStyle.Fill;


		_editGenderCombo.Dock = DockStyle.Fill;


		_editCivilCombo.Dock = DockStyle.Fill;


		_editContact.Dock = DockStyle.Fill;


		_editStatusCombo.Dock = DockStyle.Fill;
		_editBarangay.Dock = DockStyle.Fill;
		_editPurok.Dock = DockStyle.Fill;
		_editHousehold.Dock = DockStyle.Fill;


		_editDob.Dock = DockStyle.Fill;
		_editDob.MinimumSize = new Size(0, 28);
		_editFirstName.MinimumSize = new Size(0, 28);
		_editMiddleName.MinimumSize = new Size(0, 28);
		_editLastName.MinimumSize = new Size(0, 28);
		_editGenderCombo.MinimumSize = new Size(0, 28);
		_editCivilCombo.MinimumSize = new Size(0, 28);
		_editContact.MinimumSize = new Size(0, 28);
		_editStatusCombo.MinimumSize = new Size(0, 28);

		_editGenderCombo.SelectedIndexChanged -= ResidentEditChoiceChanged;
		_editGenderCombo.SelectedIndexChanged += ResidentEditChoiceChanged;
		_editCivilCombo.SelectedIndexChanged -= ResidentEditChoiceChanged;
		_editCivilCombo.SelectedIndexChanged += ResidentEditChoiceChanged;
		_editStatusCombo.SelectedIndexChanged -= ResidentEditChoiceChanged;
		_editStatusCombo.SelectedIndexChanged += ResidentEditChoiceChanged;
		_editFirstName.TextChanged -= ResidentEditFieldChanged;
		_editFirstName.TextChanged += ResidentEditFieldChanged;
		_editMiddleName.TextChanged -= ResidentEditFieldChanged;
		_editMiddleName.TextChanged += ResidentEditFieldChanged;
		_editLastName.TextChanged -= ResidentEditFieldChanged;
		_editLastName.TextChanged += ResidentEditFieldChanged;
		_editContact.TextChanged -= ResidentEditContactChanged;
		_editContact.TextChanged += ResidentEditContactChanged;
		_editDob.ValueChanged -= ResidentEditFieldChanged;
		_editDob.ValueChanged += ResidentEditFieldChanged;


		SetDetailEditing(enabled: false);


	}


	private void EnsureLocationRows()
	{
		_editBarangay.SelectedIndexChanged -= ResidentBarangayChanged;
		_editBarangay.SelectedIndexChanged += ResidentBarangayChanged;
		_editPurok.SelectedIndexChanged -= ResidentPurokChanged;
		_editPurok.SelectedIndexChanged += ResidentPurokChanged;
		_editPurok.SelectedIndexChanged -= ResidentEditFieldChanged;
		_editPurok.SelectedIndexChanged += ResidentEditFieldChanged;
		_editHousehold.SelectedIndexChanged -= ResidentEditFieldChanged;
		_editHousehold.SelectedIndexChanged += ResidentEditFieldChanged;
	}





	private void SetupValueLabel(Label label, int maxWidth = 0)


	{


		label.Font = UiTheme.BodyFont;


		label.ForeColor = UiTheme.Slate900;


		label.AutoSize = true;


		label.TextAlign = ContentAlignment.MiddleLeft;


		label.Text = "-";


		if (maxWidth > 0)


		{


			label.MaximumSize = new Size(maxWidth, 0);


		}


	}





private Panel CreateEmptyStatePanel(Label titleLabel, Label messageLabel)


	{
		UiTheme.ConfigureStateLabels(titleLabel, messageLabel);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel


		{


			FlowDirection = FlowDirection.TopDown,


			WrapContents = false,


			AutoSize = true,


			AutoSizeMode = AutoSizeMode.GrowAndShrink,


			Margin = new Padding(0),


			Anchor = AnchorStyles.None


		};


		flowLayoutPanel.Controls.Add(titleLabel);


		flowLayoutPanel.Controls.Add(messageLabel);


		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel


		{


			Dock = DockStyle.Fill,


			ColumnCount = 1,


			RowCount = 3


		};


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));


		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));


		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));


		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));


		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 1);


		Panel panel = new Panel


		{


			Dock = DockStyle.Fill,


			BackColor = UiTheme.Slate50,


			Visible = false


		};


		panel.Controls.Add(tableLayoutPanel);


		return panel;


	}





	private void AddDetailRow(TableLayoutPanel table, string labelText, Control valueControl)


	{


		AddDetailRowWithLabel(table, labelText, valueControl);


	}





	private Label AddDetailRowWithLabel(TableLayoutPanel table, string labelText, Control valueControl)


	{


		Label label = new Label


		{


			Text = labelText,


			Font = UiTheme.LabelFont,


			ForeColor = UiTheme.Slate500,


			AutoSize = true,


			Margin = new Padding(0, 8, 0, 4)


		};


		valueControl.Margin = new Padding(0, 6, 0, 6);


		int nextRow = table.RowCount;
		foreach (Control existing in table.Controls)
		{
			int existingRow = table.GetRow(existing) + 1;
			if (existingRow > nextRow)
			{
				nextRow = existingRow;
			}
		}

		int row = nextRow;
		table.RowCount = row + 1;


		table.RowStyles.Add(new RowStyle(SizeType.AutoSize));


		table.Controls.Add(label, 0, row);


		table.Controls.Add(valueControl, 1, row);


		return label;


	}

	private void EnsureResidentBottomSummaryPanel()
	{
		if (_residentBottomSummaryLayout.Controls.Count == 0)
		{
			_residentBottomSummaryHost.Dock = DockStyle.Fill;
			_residentBottomSummaryHost.Margin = new Padding(0, 12, 0, 0);
			_residentBottomSummaryHost.Padding = Padding.Empty;
			_residentBottomSummaryHost.BackColor = Color.Transparent;

			_residentBottomSummaryLayout.Dock = DockStyle.Fill;
			_residentBottomSummaryLayout.Margin = Padding.Empty;
			_residentBottomSummaryLayout.Padding = Padding.Empty;
			_residentBottomSummaryLayout.ColumnCount = 2;
			_residentBottomSummaryLayout.RowCount = 1;
			_residentBottomSummaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			_residentBottomSummaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			_residentBottomSummaryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			_residentRecentActivityPanel.Dock = DockStyle.Fill;
			_residentRecentActivityPanel.Margin = new Padding(8, 0, 0, 0);
			_residentRecentActivityPanel.Padding = new Padding(16);
			_residentRecentActivityPanel.BackColor = Color.White;
			_residentRecentActivityPanel.BorderStyle = BorderStyle.None;

			TableLayoutPanel recentLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
				ColumnCount = 1,
				RowCount = 2
			};
			recentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			recentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			recentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			_residentRecentActivityTitle.Text = "Recent Activity";
			_residentRecentActivityTitle.AutoSize = true;
			_residentRecentActivityTitle.Font = UiTheme.SectionHeaderFont;
			_residentRecentActivityTitle.ForeColor = UiTheme.Slate900;
			_residentRecentActivityTitle.Margin = new Padding(0, 0, 0, 8);

			recentLayout.Controls.Add(_residentRecentActivityTitle, 0, 0);
			_residentRecentActivityList.Dock = DockStyle.Fill;
			_residentRecentActivityList.Margin = Padding.Empty;
			_residentRecentActivityList.View = View.Details;
			_residentRecentActivityList.FullRowSelect = true;
			_residentRecentActivityList.HeaderStyle = ColumnHeaderStyle.None;
			_residentRecentActivityList.GridLines = false;
			_residentRecentActivityList.MultiSelect = false;
			_residentRecentActivityList.HideSelection = false;
			_residentRecentActivityList.Scrollable = true;
			_residentRecentActivityList.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

			_residentRecentActivityDateColumn.Text = "Date";
			_residentRecentActivityActionColumn.Text = "Action";
			_residentRecentActivityByColumn.Text = "By";
			_residentRecentActivityList.Columns.Clear();
			_residentRecentActivityList.Columns.AddRange(new[]
			{
				_residentRecentActivityDateColumn,
				_residentRecentActivityActionColumn,
				_residentRecentActivityByColumn
			});
			_residentRecentActivityList.Resize -= ResidentRecentActivityList_Resize;
			_residentRecentActivityList.Resize += ResidentRecentActivityList_Resize;

			recentLayout.Controls.Add(_residentRecentActivityList, 0, 1);
			_residentRecentActivityPanel.Controls.Add(recentLayout);
			ResizeResidentRecentActivityColumns();

			_residentBottomSummaryLayout.Controls.Add(_residentRecentActivityPanel, 1, 0);
			_residentBottomSummaryHost.Controls.Add(_residentBottomSummaryLayout);
		}
	}

	private void ConfigureResidentInsightPanel()
	{
		EnsureResidentBottomSummaryPanel();
		if (_residentInsightPanel.Controls.Count == 0)
		{
			_residentInsightPanel.Dock = DockStyle.Fill;
			_residentInsightPanel.Margin = new Padding(0, 0, 8, 0);
			_residentInsightPanel.Padding = new Padding(16);
			_residentInsightPanel.BorderStyle = BorderStyle.None;
			_residentInsightPanel.BackColor = Color.White;

			_residentInsightTitle.Text = "Resident Summary";
			_residentInsightTitle.AutoSize = true;
			_residentInsightTitle.Font = UiTheme.SectionHeaderFont;
			_residentInsightTitle.ForeColor = UiTheme.Slate900;
			_residentInsightTitle.Margin = new Padding(0, 0, 0, 8);

			UiTheme.ApplyLabelFont(UiTheme.SmallFont,
				_residentInsightBlotter,
				_residentInsightBlotterActive,
				_residentInsightCertificates,
				_residentInsightCertificatesPending,
				_residentInsightLastAction);

			foreach (Label summaryLine in new[]
			{
				_residentInsightBlotter,
				_residentInsightBlotterActive,
				_residentInsightCertificates,
				_residentInsightCertificatesPending,
				_residentInsightLastAction
			})
			{
				summaryLine.AutoSize = true;
				summaryLine.ForeColor = UiTheme.Slate700;
				summaryLine.Margin = new Padding(0, 0, 0, 4);
			}

			_residentInsightLastAction.ForeColor = UiTheme.Slate500;
			_residentInsightLastAction.Margin = Padding.Empty;

			FlowLayoutPanel stack = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = false,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};

			stack.Controls.Add(_residentInsightTitle);
			stack.Controls.Add(_residentInsightBlotter);
			stack.Controls.Add(_residentInsightBlotterActive);
			stack.Controls.Add(_residentInsightCertificates);
			stack.Controls.Add(_residentInsightCertificatesPending);
			stack.Controls.Add(_residentInsightLastAction);
			_residentInsightPanel.Controls.Add(stack);
		}

		if (!ReferenceEquals(_residentInsightPanel.Parent, _residentBottomSummaryLayout))
		{
			_residentInsightPanel.Parent?.Controls.Remove(_residentInsightPanel);
			_residentBottomSummaryLayout.Controls.Add(_residentInsightPanel, 0, 0);
		}

		UpdateResidentInsightPanel();
	}

	private void UpdateResidentInsightPanel()
	{
		if (_residentInsightPanel == null || _residentInsightPanel.IsDisposed)
		{
			return;
		}

		if (!_selectedResidentId.HasValue)
		{
			_residentInsightBlotter.Text = "Documents: 0";
			_residentInsightBlotterActive.Text = "Active cases: 0";
			_residentInsightCertificates.Text = "Pending requests: 0";
			_residentInsightCertificatesPending.Text = "Total cases: 0";
			_residentInsightLastAction.Text = "Last action: select a resident";
			UpdateResidentRecentActivityPanel();
			return;
		}

		int blotterTotal = _blotterRecords?.Count ?? 0;
		int blotterActive = (_blotterRecords ?? new List<BlotterRecordSummary>())
			.Count(record => IsBlotterCaseActive(record.StatusRaw));

		int certTotal = _certTable?.Rows.Count ?? 0;
		int certPending = 0;
		if (_certTable != null)
		{
			foreach (DataRow row in _certTable.Rows)
			{
				string raw = row["status_raw"]?.ToString()
					?? row["status"]?.ToString()
					?? string.Empty;
				string normalized = WorkflowRules.NormalizeCertificateStatus(raw);
				if (normalized == "SUBMITTED" || normalized == "APPROVED" || normalized == "DRAFT")
				{
					certPending++;
				}
			}
		}

		string lastAction = "-";
		string lastModule = "-";
		DateTime? lastAt = null;
		if (_historyTable != null)
		{
			foreach (DataRow row in _historyTable.Rows)
			{
				DateTime? actionAt = TryParseDateTime(row["action_at"]);
				if (!actionAt.HasValue)
				{
					continue;
				}

				if (!lastAt.HasValue || actionAt.Value > lastAt.Value)
				{
					lastAt = actionAt.Value;
					lastAction = SafeHistoryValue(row["action"]);
					lastModule = SafeHistoryValue(row["module"]);
				}
			}
		}

		_residentInsightBlotter.Text = $"Documents: {certTotal}";
		_residentInsightBlotterActive.Text = $"Active cases: {blotterActive}";
		_residentInsightCertificates.Text = $"Pending requests: {certPending}";
		_residentInsightCertificatesPending.Text = $"Total cases: {blotterTotal}";
		_residentInsightLastAction.Text = lastAt.HasValue
			? $"Last action: {lastModule} - {lastAction} ({lastAt:MMM dd, yyyy})"
			: "Last action: none yet";

		UpdateResidentRecentActivityPanel();
	}

	private void ResidentRecentActivityList_Resize(object? sender, EventArgs e)
	{
		ResizeResidentRecentActivityColumns();
	}

	private void ResizeResidentRecentActivityColumns()
	{
		if (_residentRecentActivityList == null || _residentRecentActivityList.IsDisposed || _residentRecentActivityList.Columns.Count < 3)
		{
			return;
		}

		int contentWidth = Math.Max(0, _residentRecentActivityList.ClientSize.Width - 4);
		if (contentWidth <= 0)
		{
			return;
		}

		int dateWidth = Math.Max(80, (int)(contentWidth * 0.28));
		int byWidth = Math.Max(70, (int)(contentWidth * 0.2));
		int actionWidth = Math.Max(80, contentWidth - dateWidth - byWidth);

		_residentRecentActivityDateColumn.Width = dateWidth;
		_residentRecentActivityActionColumn.Width = actionWidth;
		_residentRecentActivityByColumn.Width = byWidth;
	}

	private void UpdateResidentRecentActivityPanel()
	{
		if (_residentRecentActivityPanel == null || _residentRecentActivityPanel.IsDisposed || _residentRecentActivityList == null || _residentRecentActivityList.IsDisposed)
		{
			return;
		}

		var timeline = new List<(DateTime sortAt, string date, string action, string by)>();
		if (_historyTable != null)
		{
			foreach (DataRow row in _historyTable.Rows)
			{
				string rawAction = SafeHistoryValue(row["action"]);
				string rawModule = SafeHistoryValue(row["module"]);
				string rawBy = SafeHistoryValue(row["action_by"]);
				DateTime? actionAt = TryParseDateTime(row["action_at"]);
				DateTime sortAt = actionAt ?? DateTime.MinValue;
				string dateText = actionAt.HasValue ? actionAt.Value.ToString("MMM dd, yyyy") : "Unknown date";
				string actionText = BuildResidentActivityAction(rawAction, rawModule);
				string byText = BuildResidentActivityBy(rawBy);
				timeline.Add((sortAt, dateText, actionText, byText));
			}
		}

		var topItems = timeline
			.OrderByDescending(item => item.sortAt)
			.Take(8)
			.ToList();

		_residentRecentActivityList.BeginUpdate();
		try
		{
			_residentRecentActivityList.Items.Clear();
			if (!_selectedResidentId.HasValue)
			{
				ListViewItem empty = new ListViewItem("Select a resident to view activity.");
				empty.SubItems.Add(string.Empty);
				empty.SubItems.Add(string.Empty);
				_residentRecentActivityList.Items.Add(empty);
			}
			else if (topItems.Count == 0)
			{
				ListViewItem empty = new ListViewItem("No recent activity yet.");
				empty.SubItems.Add(string.Empty);
				empty.SubItems.Add(string.Empty);
				_residentRecentActivityList.Items.Add(empty);
			}
			else
			{
				foreach (var item in topItems)
				{
					ListViewItem row = new ListViewItem(item.date);
					row.SubItems.Add(item.action);
					row.SubItems.Add(item.by);
					_residentRecentActivityList.Items.Add(row);
				}
			}
		}
		finally
		{
			_residentRecentActivityList.EndUpdate();
		}

		ResizeResidentRecentActivityColumns();
	}

	private static string BuildResidentActivityAction(string action, string module)
	{
		string safeAction = NormalizeResidentActivityText(action, "Updated");
		string safeModule = NormalizeResidentActivityText(module, "Residents");
		if (safeAction.IndexOf(safeModule, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return safeAction;
		}

		return $"{safeAction} {safeModule}";
	}

	private static string BuildResidentActivityBy(string by)
	{
		string safeBy = NormalizeResidentActivityText(by, "system");
		return $"by {safeBy}";
	}

	private static string NormalizeResidentActivityText(string value, string fallback)
	{
		if (string.IsNullOrWhiteSpace(value) || value == "-")
		{
			return fallback;
		}

		string cleaned = value.Replace('_', ' ').Trim();
		if (string.IsNullOrWhiteSpace(cleaned))
		{
			return fallback;
		}

		string[] words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < words.Length; i++)
		{
			string token = words[i].ToLowerInvariant();
			words[i] = token.Length > 1
				? char.ToUpperInvariant(token[0]) + token[1..]
				: token.ToUpperInvariant();
		}

		return string.Join(" ", words);
	}

	private static bool IsBlotterCaseActive(string? status)
	{
		string normalized = WorkflowRules.NormalizeBlotterStatus(status);
		return normalized != "SETTLED" && normalized != "CLOSED";
	}





	private void ClearResidentDetails(string? message = null)


	{


		SetDetailMessage(message ?? "Select a resident to view details.");
		bool previous = _suppressEditChangeTracking;
		_suppressEditChangeTracking = true;
		try
		{
			_editFirstName.Text = string.Empty;
			_editMiddleName.Text = string.Empty;
			_editLastName.Text = string.Empty;
			_editGender.Text = string.Empty;
			_editCivil.Text = string.Empty;
			_editContact.Text = string.Empty;
			_editStatus.Text = string.Empty;
			_editDob.Value = DateTime.Today;
			SyncResidentChoiceEditorsFromText();
		}
		finally
		{
			_suppressEditChangeTracking = previous;
		}


		_selectedResidentId = null;
		_residentDetailsLoadedId = null;
		_residentDetailsLoadVersion++;


		SetDetailEditing(enabled: false);


		UpdateResidentHeader();


		_residentPhotoBytes = null;


		_residentPhotoPendingBytes = null;


		_residentPhotoRemoved = false;


		LoadResidentPhoto(null);


		UpdateResidentPhotoControls();


		ClearBlotters();

		UpdateBlotterActionState();


		ClearCertificates();


		ClearHistory();
		UpdateResidentInsightPanel();
		ResetProfileViewport();
		SetResidentProfileTab("overview", userInitiated: false, force: true);
		UpdateRightPanelSelectionState();
		RaiseResidentRouteChanged();


	}





	private void EnsureRightSelectionEmptyPanel()
	{
		if (panelRightRoot == null)
		{
			return;
		}

		if (_panelSelectResidentEmpty.Controls.Count == 0)
		{
			_panelSelectResidentEmpty.Dock = DockStyle.Fill;
			_panelSelectResidentEmpty.Margin = Padding.Empty;
			_panelSelectResidentEmpty.Padding = new Padding(20);
			_panelSelectResidentEmpty.BackColor = Color.Transparent;
			_panelSelectResidentEmpty.Visible = false;

			TableLayoutPanel layout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 3,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

			Panel card = new Panel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Padding = new Padding(24),
				BackColor = Color.White,
				BorderStyle = BorderStyle.FixedSingle,
				Anchor = AnchorStyles.None
			};

			FlowLayoutPanel stack = new FlowLayoutPanel
			{
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Margin = Padding.Empty
			};

			_selectResidentEmptyTitle.AutoSize = true;
			_selectResidentEmptyTitle.Font = UiTheme.HeadingFont;
			_selectResidentEmptyTitle.ForeColor = UiTheme.Slate900;
			_selectResidentEmptyTitle.Text = "No resident selected";
			_selectResidentEmptyTitle.Margin = new Padding(0, 0, 0, 8);

			_selectResidentEmptyMessage.AutoSize = true;
			_selectResidentEmptyMessage.Font = UiTheme.BodyFont;
			_selectResidentEmptyMessage.ForeColor = UiTheme.Slate500;
			_selectResidentEmptyMessage.Text = "Select a resident from the list to view details.";
			_selectResidentEmptyMessage.Margin = Padding.Empty;

			stack.Controls.Add(_selectResidentEmptyTitle);
			stack.Controls.Add(_selectResidentEmptyMessage);
			card.Controls.Add(stack);
			layout.Controls.Add(card, 0, 1);
			_panelSelectResidentEmpty.Controls.Add(layout);
		}

		if (!ReferenceEquals(_panelSelectResidentEmpty.Parent, panelRightRoot))
		{
			_panelSelectResidentEmpty.Parent?.Controls.Remove(_panelSelectResidentEmpty);
			panelRightRoot.Controls.Add(_panelSelectResidentEmpty);
		}
	}

	private void UpdateRightPanelSelectionState()
	{
		EnsureRightSelectionEmptyPanel();
		bool hasSelection = IsResidentView() && _selectedResidentId.HasValue;
		if (tableBody != null)
		{
			tableBody.Visible = hasSelection;
		}

		_panelSelectResidentEmpty.Visible = !hasSelection;
		if (!hasSelection)
		{
			_panelSelectResidentEmpty.BringToFront();
		}
	}



	private void UpdateResidentHeader()


	{


		if (_residentHeader == null)


		{


			return;


		}


		if (!IsResidentView() || !_selectedResidentId.HasValue)


		{


			_residentHeaderName.Text = "No resident selected";


			_residentHeaderMeta.Text = "Select a resident to view details.";
			_residentHeaderAddress.Text = "Select a resident from the list to view details.";


			_residentHeaderStatus.Visible = false;
			_residentHeaderPhoto.Image = null;


			UpdateResidentHeaderActions();
			UpdateResidentPickerSummary();


			return;


		}


		_residentHeaderName.Text = GetResidentFullName();


		List<string> list = new List<string>();


		if (!string.IsNullOrWhiteSpace(_editGender.Text))


		{


			list.Add(_editGender.Text.Trim());


		}


		int? residentAge = GetResidentAge(_editDob.Value.Date);


		if (residentAge.HasValue)


		{


			list.Add($"{residentAge.Value} yrs");


		}


		if (!string.IsNullOrWhiteSpace(_editCivil.Text))


		{


			list.Add(_editCivil.Text.Trim());


		}


		if (!string.IsNullOrWhiteSpace(_editContact.Text))


		{


			list.Add(_editContact.Text.Trim());


		}


		_residentHeaderMeta.Text = ((list.Count > 0) ? string.Join(" | ", list) : "Profile info incomplete.");
		_residentHeaderAddress.Text = ComposeResidentAddressSummary();


		string normalizedStatus = NormalizeResidentStatusEditorValue(_editStatus.Text);
		string text = normalizedStatus switch
		{
			"ACTIVE" => "Active",
			"DECEASED" => "Deceased",
			"MOVED_OUT" => "Inactive",
			_ => "Unknown"
		};


		_residentHeaderStatus.Text = text;


		_residentHeaderStatus.Visible = true;
		_residentHeaderPhoto.Image = _residentPhoto?.Image;


		UpdateResidentStatusBadge(text);


		UpdateResidentHeaderActions();
		UpdateResidentPickerSummary();


	}





	private static int? GetResidentAge(DateTime dob)


	{


		if (dob == DateTime.MinValue)


		{


			return null;


		}


		DateTime today = DateTime.Today;


		int num = today.Year - dob.Year;


		if (dob.Date > today.AddYears(-num))


		{


			num--;


		}


		return (num < 0) ? ((int?)null) : new int?(num);


	}





	private void UpdateResidentStatusBadge(string status)


	{


		string text = status?.Trim() ?? string.Empty;


		if (string.IsNullOrWhiteSpace(text))


		{


			_residentHeaderStatus.BackColor = UiTheme.Slate300;


			_residentHeaderStatus.ForeColor = UiTheme.Slate900;


			return;


		}


		switch (text)


		{


		case "Active":
		case "ACTIVE":


			_residentHeaderStatus.BackColor = Color.FromArgb(210, 245, 220);


			_residentHeaderStatus.ForeColor = Color.FromArgb(0, 100, 40);


			break;


		case "Inactive":
		case "MOVED_OUT":
		case "Moved out":


			_residentHeaderStatus.BackColor = Color.FromArgb(235, 235, 235);


			_residentHeaderStatus.ForeColor = UiTheme.Slate500;


			break;


		case "Deceased":
		case "DECEASED":


			_residentHeaderStatus.BackColor = Color.FromArgb(254, 226, 226);


			_residentHeaderStatus.ForeColor = Color.FromArgb(153, 27, 27);


			break;


		default:


			_residentHeaderStatus.BackColor = UiTheme.Slate300;


			_residentHeaderStatus.ForeColor = UiTheme.Slate900;


			break;


		}


	}





	private void UpdateResidentHeaderActions()


	{


		bool hasSelection = IsResidentView() && _selectedResidentId.HasValue;
		bool canEdit = hasSelection && Permissions.CanUpdateResidents && !_showDeletedResidents;


		_residentQuickEdit.Enabled = canEdit && !_isEditing;
		_residentHeaderEditButton.Enabled = canEdit && !_isEditing;
		_residentHeaderPrintButton.Enabled = hasSelection;
		_residentHeaderDeactivateButton.Enabled = hasSelection && Permissions.CanDeleteResidents && !_showDeletedResidents && !_isEditing;
		_btnResidentAttachments.Enabled = hasSelection;
		_residentMoreDetailsButton.Enabled = hasSelection;
		_residentQuickEdit.Text = "Edit Profile";
		_residentHeaderEditButton.Text = "Edit Profile";
		UpdateResidentEditActionsState();


	}

	private string ComposeResidentAddressSummary()
	{
		List<string> parts = new List<string>();

		string barangay = (_editBarangay.Text ?? string.Empty).Trim();
		string purok = (_editPurok.Text ?? string.Empty).Trim();
		string household = (_editHousehold.Text ?? string.Empty).Trim();

		if (!string.IsNullOrWhiteSpace(barangay) && !string.Equals(barangay, "(None)", StringComparison.OrdinalIgnoreCase))
		{
			parts.Add(barangay);
		}

		if (!string.IsNullOrWhiteSpace(purok) && !string.Equals(purok, "(None)", StringComparison.OrdinalIgnoreCase))
		{
			parts.Add(purok);
		}

		if (!string.IsNullOrWhiteSpace(household) && !string.Equals(household, "(None)", StringComparison.OrdinalIgnoreCase))
		{
			parts.Add(household);
		}

		return parts.Count > 0 ? string.Join(", ", parts) : "Address info incomplete.";
	}

	private void ResidentMoreDetailsButton_Click(object? sender, EventArgs e)
	{
		if (!IsResidentView() || !_selectedResidentId.HasValue)
		{
			ControllerDialogs.Warning("Select a resident first.");
			return;
		}

		OpenResidentDetailsModal(readOnly: true, initialTabIndex: 1);
	}

	private static ComboBox CreateMoreDetailsComboBox()
	{
		ComboBox combo = new ComboBox
		{
			Dock = DockStyle.Fill,
			DropDownStyle = ComboBoxStyle.DropDownList,
			Margin = new Padding(0, 3, 12, 3),
			Font = UiTheme.BodyFont
		};
		UiTheme.StyleComboBoxes(combo);
		return combo;
	}

	private static TextBox CreateMoreDetailsTextBox(bool multiline = false)
	{
		TextBox textBox = new TextBox
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0, 3, 12, 3),
			Multiline = multiline,
			ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
		};
		if (multiline)
		{
			textBox.MinimumSize = new Size(0, 110);
		}
		UiTheme.StyleTextBox(textBox);
		return textBox;
	}

	private static void AddMoreDetailsRow(TableLayoutPanel table, int row, string leftLabelText, Control leftControl, string rightLabelText, Control rightControl)
	{
		Label leftLabel = new Label
		{
			Text = leftLabelText,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(0, 3, 8, 3),
			Font = UiTheme.LabelFont,
			ForeColor = UiTheme.Slate700
		};
		Label rightLabel = new Label
		{
			Text = rightLabelText,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(0, 3, 8, 3),
			Font = UiTheme.LabelFont,
			ForeColor = UiTheme.Slate700
		};

		table.Controls.Add(leftLabel, 0, row);
		table.Controls.Add(leftControl, 1, row);
		table.Controls.Add(rightLabel, 2, row);
		table.Controls.Add(rightControl, 3, row);
	}

	private void PopulateMoreDetailsLookupCombos(ComboBox barangayBox, ComboBox purokBox, ComboBox householdBox)
	{
		try
		{
			using var conn = OpenLookupConnection();
			var barangays = LoadLookupItems(conn, "SELECT barangay_id, name FROM barangay ORDER BY name");
			BindCombo(barangayBox, barangays, includeNone: false);
			SelectComboById(barangayBox, GetSelectedLookupId(_editBarangay) ?? SchemaDefaults.DefaultBarangayId);
			PopulateMoreDetailsPurokAndHousehold(barangayBox, purokBox, householdBox);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load location details.", "Warning");
		}
	}

	private void PopulateMoreDetailsPurokAndHousehold(ComboBox barangayBox, ComboBox purokBox, ComboBox householdBox)
	{
		try
		{
			using var conn = OpenLookupConnection();
			int barangayId = GetSelectedLookupId(barangayBox) ?? SchemaDefaults.DefaultBarangayId;
			var puroks = LoadLookupItems(conn,
				"SELECT purok_id, name FROM purok_sitio WHERE barangay_id = @barangayId ORDER BY name",
				new MySqlParameter("@barangayId", barangayId));
			BindCombo(purokBox, puroks, includeNone: false);
			SelectComboById(purokBox, GetSelectedLookupId(_editPurok) ?? SchemaDefaults.DefaultPurokId);
			PopulateMoreDetailsHouseholds(barangayBox, purokBox, householdBox);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load purok options.", "Warning");
		}
	}

	private void PopulateMoreDetailsHouseholds(ComboBox barangayBox, ComboBox purokBox, ComboBox householdBox)
	{
		try
		{
			using var conn = OpenLookupConnection();
			int barangayId = GetSelectedLookupId(barangayBox) ?? SchemaDefaults.DefaultBarangayId;
			int? purokId = GetSelectedLookupId(purokBox);
			string sql = @"SELECT household_id,
                              COALESCE(NULLIF(TRIM(CONCAT_WS(' ', house_no, street, subdivision)), ''), CONCAT('Household #', household_id)) AS label
                       FROM household
                       WHERE barangay_id = @barangayId
                         AND (@purokId IS NULL OR purok_id = @purokId)
                       ORDER BY household_id";
			var households = LoadLookupItems(conn, sql,
				new MySqlParameter("@barangayId", barangayId),
				new MySqlParameter("@purokId", (object?)purokId ?? DBNull.Value));
			BindCombo(householdBox, households, includeNone: true);
			SelectComboById(householdBox, GetSelectedLookupId(_editHousehold));
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load household options.", "Warning");
		}
	}

	private void ApplyMoreDetailsLocationSelection(ComboBox barangayBox, ComboBox purokBox, ComboBox householdBox)
	{
		bool previous = _suppressLocationEvents;
		_suppressLocationEvents = true;
		try
		{
			using var conn = OpenLookupConnection();
			int selectedBarangay = GetSelectedLookupId(barangayBox) ?? SchemaDefaults.DefaultBarangayId;
			int? selectedPurok = GetSelectedLookupId(purokBox);
			int? selectedHousehold = GetSelectedLookupId(householdBox);

			SelectComboById(_editBarangay, selectedBarangay);
			int barangayId = GetSelectedLookupId(_editBarangay) ?? SchemaDefaults.DefaultBarangayId;
			ReloadPurokList(conn, barangayId, selectedPurok);
			int? purokId = GetSelectedLookupId(_editPurok);
			ReloadHouseholdList(conn, barangayId, purokId, selectedHousehold);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to apply location changes.", "Warning");
		}
		finally
		{
			_suppressLocationEvents = previous;
		}

		UpdateResidentHeader();
	}

	private void ResidentHeaderPrintButton_Click(object? sender, EventArgs e)
	{
		if (!IsResidentView() || !_selectedResidentId.HasValue)
		{
			return;
		}

		if (_selectedCertificateId.HasValue)
		{
			CertPrint_Click(sender, e);
			return;
		}

			ControllerDialogs.Info("Select a document in Requests & Documents to print.", "Print");
	}





	private void ResidentQuickEdit_Click(object? sender, EventArgs e)


	{

		_controller.HandleQuickEdit(sender, e);

	}

	internal bool OpenResidentDetailsModal(bool readOnly, int initialTabIndex = 0)
	{
		if (!IsResidentView() || !_selectedResidentId.HasValue)
		{
			ControllerDialogs.Warning("Select a resident first.");
			return false;
		}

		try
		{
			using var modal = new ResidentDetailsModal(_selectedResidentId.Value, readOnly, initialTabIndex);
			DialogResult result = modal.ShowDialog(FindForm());
			if (result != DialogResult.OK)
			{
				return false;
			}

			LoadResidents();
			return true;
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to open resident details.");
			return false;
		}
	}

	private void ResidentAttachments_Click(object? sender, EventArgs e)

	{

		_controller.HandleResidentAttachments(sender, e);

	}

	private void BlotterAttachments_Click(object? sender, EventArgs e)

	{

		_controller.HandleBlotterAttachments(sender, e);

	}

	private void CertAttachments_Click(object? sender, EventArgs e)

	{

		_controller.HandleCertAttachments(sender, e);

	}

	internal void OpenAttachmentManager(AttachmentEntityType entityType, int entityId, string? entityLabel)
	{
		using var form = new AttachmentModuleForm(entityType, entityId, entityLabel);
		form.ShowDialog(FindForm());
	}















	private void ResidentPhotoUpload_Click(object? sender, EventArgs e)


	{

		_controller.HandlePhotoUpload(sender, e);

	}





	private void ResidentPhotoRemove_Click(object? sender, EventArgs e)


	{

		_controller.HandlePhotoRemove(sender, e);

	}





	public void UploadResidentPhoto()

	{

		if (!_isEditing || !_selectedResidentId.HasValue)
		{
			return;
		}

		using OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Select a resident photo",
			Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
			CheckFileExists = true,
			CheckPathExists = true
		};
		if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
		{
			return;
		}

		try
		{
			byte[] photoBytes = File.ReadAllBytes(dialog.FileName);
			_residentPhotoPendingBytes = photoBytes;
			_residentPhotoRemoved = false;
			LoadResidentPhoto(photoBytes);
			_residentEditDirty = true;
			UpdateResidentPhotoControls();
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to read photo.", "Photo Error");
		}

	}




	public void RemoveResidentPhoto()

	{

		if (!_isEditing || !_selectedResidentId.HasValue)
		{
			return;
		}

		_residentPhotoPendingBytes = null;
		_residentPhotoRemoved = true;
		LoadResidentPhoto(null);
		_residentEditDirty = true;
		UpdateResidentPhotoControls();

	}




	private void UpdateResidentPhotoControls()


	{


		bool flag = _isEditing && _selectedResidentId.HasValue && IsResidentView();


		_residentPhotoUpload.Enabled = flag;


		bool canRemovePhoto = flag && (!_residentPhotoRemoved || _residentPhotoBytes != null || _residentPhotoPendingBytes != null);
		_residentPhotoRemove.Enabled = canRemovePhoto;
		_residentHeaderPhotoChange.Enabled = flag;
		_residentHeaderPhotoRemove.Enabled = canRemovePhoto;
		_residentHeaderPhotoMenu.Enabled = flag;


		if (_residentPhotoRemoved)


		{


			_residentPhotoCaption.Text = "Photo removed (pending save)";


		}


		else if (_residentPhotoPendingBytes != null)


		{


			_residentPhotoCaption.Text = "New photo (pending save)";


		}


		else


		{


			_residentPhotoCaption.Text = ((_residentPhotoBytes == null) ? "No photo" : "Photo");


		}

		UpdateResidentEditActionsState();


	}





	private void LoadResidentPhoto(byte[]? photoBytes)


	{


		if (photoBytes == null || photoBytes.Length == 0)


		{


			SetResidentPhotoImage(null);


			return;


		}


		try


		{


			using MemoryStream stream = new MemoryStream(photoBytes);


			using Image original = Image.FromStream(stream);


			SetResidentPhotoImage(new Bitmap(original));


		}


		catch


		{


			SetResidentPhotoImage(null);


		}


	}





	private void SetResidentPhotoImage(Image? image)


	{


		Image image2 = _residentPhoto.Image;


		_residentPhoto.Image = image;


		image2?.Dispose();


	}





	private void UpdateBlotterEmptyState()
	{
		if (_blotterEmptyPanel == null)
		{
			return;
		}

		if (!IsResidentView() || !_selectedResidentId.HasValue)
		{
			_blotterEmptyTitle.Text = "No resident selected";
			_blotterEmptyMessage.Text = "Select a resident from the list, then click New Blotter Case to add a record.";
			_blotterEmptyPanel.Visible = true;
			return;
		}

		if (_blotterRecords.Count <= 0)
		{
			_blotterEmptyTitle.Text = "No blotter cases found";
			_blotterEmptyMessage.Text = Permissions.CanCreateBlotter
				? "No records yet. Click New Blotter Case to file the first blotter entry."
				: "No incidents recorded for this resident yet.";
			_blotterEmptyPanel.Visible = true;
			return;
		}

		if (_blotterFilteredRecords.Count <= 0)
		{
			_blotterEmptyTitle.Text = "No matching results";
			_blotterEmptyMessage.Text = "Try changing the search text or date range.";
			_blotterEmptyPanel.Visible = true;
			return;
		}

		_blotterEmptyPanel.Visible = false;
	}





	private void UpdateBlotterActionState()
	{
		bool hasResident = IsResidentView() && _selectedResidentId.HasValue;
		bool hasSelection = hasResident && _selectedBlotterId.HasValue;

		_btnFileBlotter.Enabled = hasResident;
		_btnRefreshBlotter.Enabled = hasResident;
		_btnOpenBlotter.Enabled = hasSelection;
		_btnPrintBlotter.Enabled = hasSelection;
		_btnBlotterAttachments.Enabled = hasSelection;
		_casesAttachmentAdd.Enabled = hasSelection;
		_casesAttachmentOpen.Enabled = hasSelection;
		_casesAttachmentRemove.Enabled = hasSelection;

		if (hasSelection)
		{
			BlotterRecordSummary? selected = GetSelectedBlotterRecord();
			string from = selected?.StatusRaw ?? selected?.Status ?? string.Empty;
			_btnCloseBlotter.Enabled = WorkflowRules.TryValidateBlotterTransition(from, "CLOSED", out _);
		}
		else
		{
			_btnCloseBlotter.Enabled = false;
		}

		UpdateBlotterPagerState();
	}





	private void UpdateCertificateEmptyState()


	{


		if (_certEmptyPanel != null)


		{


			if (!_historyOnlyMode && (!IsResidentView() || !_selectedResidentId.HasValue))


			{


				_certEmptyTitle.Text = "No resident selected";


				_certEmptyMessage.Text = "Select a resident from the list, then click New to start a certificate request.";


				_certEmptyPanel.Visible = true;


			}


			else if (_certGrid.DataSource == null || _certGrid.Rows.Count <= 0)


			{


				_certEmptyTitle.Text = "No certificates found";


				_certEmptyMessage.Text = Permissions.CanRequestCertificates
					? "No certificate requests yet. Next step: click New and submit the first request."
					: "No certificate requests yet. Ask an authorized user to create a request.";


				_certEmptyPanel.Visible = true;


			}


			else


			{


				_certEmptyPanel.Visible = false;


			}


		}


	}





	private void UpdateHistoryEmptyState()


	{


		if (_historyEmptyPanel != null)


		{


			if (!_historyOnlyMode && (!IsResidentView() || !_selectedResidentId.HasValue))


			{


				_historyEmptyTitle.Text = "No resident selected";


				_historyEmptyMessage.Text = "Select a resident from the list to load activity logs.";


				_historyEmptyPanel.Visible = true;


			}


			else if (_historyGrid.DataSource == null || _historyGrid.Rows.Count <= 0)


			{


				_historyEmptyTitle.Text = "No history yet";


				_historyEmptyMessage.Text = "No actions logged yet. Next step: create, approve, or issue a certificate to generate history.";


				_historyEmptyPanel.Visible = true;


			}


			else


			{


				_historyEmptyPanel.Visible = false;


			}


		}


	}





	private void HistoryFilter_Changed(object? sender, EventArgs e)


	{


		ApplyHistoryFilters();


	}





	private void HistoryFilterClear_Click(object? sender, EventArgs e)


	{
		_historySearchDebounceTimer.Stop();

		_controller.HandleHistoryFilterClear(sender, e);
		_historySearchDebounceTimer.Stop();

	}





	private void ApplyHistoryFilters()


	{


		if (_historyTable == null)


		{


			UpdateHistorySummary();


			UpdateHistoryEmptyState();


			return;


		}


		List<string> list = new List<string>();


		string text = _historySearchBox.Text.Trim();


		if (!string.IsNullOrWhiteSpace(text))


		{


			text = text.Replace("[", "[[]").Replace("]", "[]]").Replace("'", "''");


			list.Add($"(module LIKE '%{text}%' OR action LIKE '%{text}%' OR details LIKE '%{text}%' OR action_by LIKE '%{text}%')");


		}


		if (_historyFilterModule.SelectedIndex > 0)


		{


			string text2 = _historyFilterModule.SelectedItem?.ToString() ?? string.Empty;


			text2 = text2.Replace("'", "''");


			list.Add("module = '" + text2 + "'");


		}


		if (_historyFilterFrom.Checked)


		{


			DateTime date = _historyFilterFrom.Value.Date;


			list.Add($"action_at >= #{date:MM/dd/yyyy}#");


		}


		if (_historyFilterTo.Checked)


		{


			DateTime value = _historyFilterTo.Value.Date.AddDays(1.0);


			list.Add($"action_at < #{value:MM/dd/yyyy}#");


		}


		_historyTable.DefaultView.RowFilter = string.Join(" AND ", list);

		EnsureHistorySelection();

		UpdateHistorySummary();


		UpdateHistoryEmptyState();
		UpdateHistoryDetail();


	}

	private void EnsureHistorySelection()
	{
		if (_historyGrid == null || _historyGrid.Rows.Count == 0)
		{
			return;
		}

		if (_historyGrid.CurrentRow == null)
		{
			_historyGrid.ClearSelection();
			_historyGrid.Rows[0].Selected = true;
			_historyGrid.CurrentCell = _historyGrid.Rows[0].Cells[0];
		}
	}





private void UpdateHistorySummary()


{


    int total = _historyTable?.DefaultView.Count ?? 0;
    _historySummary.Text = $"Total: {total}";
    _historyShowingLabel.Text = total == 1 ? "Showing 1 item" : $"Showing {total} items";

    if (historySummaryTotalValue != null)
    {
        historySummaryTotalValue.Text = total.ToString();
    }

    int residents = CountHistoryModule("Residents");
    int blotter = CountHistoryModule("Blotter");
    int certificates = CountHistoryModule("Certificates");

    if (historySummaryResidentsValue != null)
    {
        historySummaryResidentsValue.Text = residents.ToString();
    }

    if (historySummaryBlotterValue != null)
    {
        historySummaryBlotterValue.Text = blotter.ToString();
    }

    if (historySummaryCertificatesValue != null)
    {
        historySummaryCertificatesValue.Text = certificates.ToString();
    }


}

private int CountHistoryModule(string module)
{
    if (_historyTable == null)
    {
        return 0;
    }

    int count = 0;
    foreach (DataRowView row in _historyTable.DefaultView)
    {
        string value = row["module"]?.ToString() ?? string.Empty;
        if (value.Equals(module, StringComparison.OrdinalIgnoreCase))
        {
            count++;
        }
    }
    return count;
}





	private static string GetCellText(DataGridViewRow row, string columnName)


	{


		if (row.Cells[columnName] == null)


		{


			return "-";


		}


		object value = row.Cells[columnName].Value;


		if (value == null || value == DBNull.Value)


		{


			return "-";


		}


		string text = value.ToString() ?? "-";


		return string.IsNullOrWhiteSpace(text) ? "-" : text;


	}


	private static int? GetCellNullableInt(DataGridViewRow row, string columnName)
	{
		if (row.Cells[columnName] == null)
		{
			return null;
		}

		object value = row.Cells[columnName].Value;
		if (value == null || value == DBNull.Value)
		{
			return null;
		}

		if (value is int intValue)
		{
			return intValue;
		}

		return int.TryParse(value.ToString(), out int parsed) ? parsed : (int?)null;
	}





	private static string FormatDate(object? value)


	{


		if (value == null || value == DBNull.Value)


		{


			return "-";


		}


		if (value is DateTime dateTime)


		{


			return dateTime.ToString("MMM dd, yyyy");


		}


		DateTime result;


		return DateTime.TryParse(value.ToString(), out result) ? result.ToString("MMM dd, yyyy") : (value.ToString() ?? "-");


	}





	private static string FormatDateTime(object? value)


	{


		if (value == null || value == DBNull.Value)


		{


			return "-";


		}


		if (value is DateTime dateTime)


		{


			return dateTime.ToString("MMM dd, yyyy h:mm tt");


		}


		DateTime result;


		return DateTime.TryParse(value.ToString(), out result) ? result.ToString("MMM dd, yyyy h:mm tt") : (value.ToString() ?? "-");


	}





	private static string FormatPersonStamp(object? dateValue, object? nameValue)


	{


		string text = ((!(dateValue is DateTime dateTime) || !(dateTime.TimeOfDay == TimeSpan.Zero)) ? FormatDateTime(dateValue) : dateTime.ToString("MMM dd, yyyy"));


		string text2 = nameValue?.ToString();


		if (text == "-" && string.IsNullOrWhiteSpace(text2))


		{


			return "-";


		}


		if (text == "-")


		{


			return text2 ?? "-";


		}


		if (string.IsNullOrWhiteSpace(text2))


		{


			return text;


		}


		return text + " | " + text2;


	}





	private void UpdateCertificateStatusBadge(string status)


	{


		string text = status?.Trim() ?? string.Empty;


		_certStatus.Text = (string.IsNullOrWhiteSpace(text) ? "Requested" : text);


		switch (_certStatus.Text)


		{


		case "Requested":


			_certStatus.BackColor = Color.FromArgb(255, 230, 200);


			_certStatus.ForeColor = Color.FromArgb(122, 69, 0);


			break;


		case "Approved":


			_certStatus.BackColor = Color.FromArgb(210, 230, 255);


			_certStatus.ForeColor = Color.FromArgb(0, 70, 140);


			break;


		case "Issued":


			_certStatus.BackColor = Color.FromArgb(210, 245, 220);


			_certStatus.ForeColor = Color.FromArgb(0, 100, 40);


			break;


		case "Cancelled":


			_certStatus.BackColor = Color.FromArgb(235, 235, 235);


			_certStatus.ForeColor = UiTheme.Slate500;


			break;
		case "Rejected":
			_certStatus.BackColor = Color.FromArgb(255, 220, 220);
			_certStatus.ForeColor = Color.FromArgb(140, 30, 30);
			break;
		case "Draft":
			_certStatus.BackColor = Color.FromArgb(245, 245, 245);
			_certStatus.ForeColor = UiTheme.Slate600;
			break;


		default:


			_certStatus.BackColor = UiTheme.Slate300;


			_certStatus.ForeColor = UiTheme.Slate900;


			break;


		}


	}





	private string GetIssuedDateText()


	{


		string text = _certIssuedAt.Text ?? string.Empty;


		if (string.IsNullOrWhiteSpace(text) || text == "-")


		{


			return DateTime.Today.ToString("MMM dd, yyyy");


		}


		string[] array = text.Split(new[] { " | " }, StringSplitOptions.None);


		return (array.Length != 0) ? array[0].Trim() : text;


	}





	private string GetIssuedByName()


	{


		string text = _certIssuedAt.Text ?? string.Empty;


		if (string.IsNullOrWhiteSpace(text) || text == "-")


		{


			return UserSession.Username ?? string.Empty;


		}


		string[] array = text.Split(new[] { " | " }, StringSplitOptions.None);


		if (array.Length > 1)


		{


			return array[1].Trim();


		}


		return UserSession.Username ?? string.Empty;


	}





	private void SearchClear_Click(object? sender, EventArgs e)


	{

		_controller.HandleSearchClear(sender, e);

	}





	private void SearchBox_TextChanged(object? sender, EventArgs e)


	{


		ApplyResidentSearch();


	}





	private void ApplyResidentSearch(bool resetPage = true)


	{


		if (_residentTable == null)


		{


			UpdateResidentPagerState();
			UpdateResidentListVisualState();


			return;


		}


		string text = _searchBox.Text.Trim();


		if (string.IsNullOrWhiteSpace(text))


		{


			_residentTable.DefaultView.RowFilter = string.Empty;


		}


		else


		{


			text = text.Replace("[", "[[]").Replace("]", "[]]").Replace("'", "''");


			_residentTable.DefaultView.RowFilter = $"firstname LIKE '%{text}%' OR middlename LIKE '%{text}%' OR lastname LIKE '%{text}%'";


		}


		ApplyResidentPaging(resetPage);


	}

	private void ApplyResidentPaging(bool resetPage)
	{
		if (_residentTable == null)
		{
			dgvResidents.DataSource = null;
			ClearResidentDetails();
			UpdateResidentPagerState();
			UpdateResidentListVisualState();
			return;
		}

		DataView view = _residentTable.DefaultView;
		int total = view.Count;
		int totalPages = total <= 0 ? 1 : (int)Math.Ceiling(total / (double)ResidentPageSize);
		if (resetPage)
		{
			_residentPageIndex = 0;
		}

		if (_residentPageIndex < 0)
		{
			_residentPageIndex = 0;
		}

		if (_residentPageIndex >= totalPages)
		{
			_residentPageIndex = totalPages - 1;
		}

		int startIndex = _residentPageIndex * ResidentPageSize;
		int endIndex = Math.Min(total, startIndex + ResidentPageSize);
		DataTable pageTable = _residentTable.Clone();
		for (int i = startIndex; i < endIndex; i++)
		{
			pageTable.ImportRow(view[i].Row);
		}

		int? previousResidentId = _selectedResidentId;
		bool hasRows;
		_suppressResidentSelectionChanged = true;
		try
		{
			dgvResidents.DataSource = pageTable;
			ConfigureResidentGridColumns();
			UpdateResidentPagerState();
			UpdateResidentListVisualState();

			hasRows = dgvResidents.Rows.Count > 0;
			if (hasRows && !TrySelectResidentRow(previousResidentId))
			{
				dgvResidents.ClearSelection();
				dgvResidents.CurrentCell = null;
			}
		}
		finally
		{
			_suppressResidentSelectionChanged = false;
		}

		if (!hasRows)
		{
			_selectedResidentId = null;
			ClearResidentDetails();
			UpdateResidentSoftDeleteButtons();
			return;
		}

		DgvResidents_SelectionChanged(dgvResidents, EventArgs.Empty);
	}

	private bool TrySelectResidentRow(int? residentId)
	{
		if (!residentId.HasValue)
		{
			return false;
		}

		foreach (DataGridViewRow row in dgvResidents.Rows)
		{
			if (row.Cells["resident_id"]?.Value == null || row.Cells["resident_id"].Value == DBNull.Value)
			{
				continue;
			}

			if (Convert.ToInt32(row.Cells["resident_id"].Value) != residentId.Value)
			{
				continue;
			}

			row.Selected = true;
			dgvResidents.CurrentCell = row.Cells["firstname"] ?? row.Cells[0];
			return true;
		}

		return false;
	}

	private static bool TryGetResidentId(DataGridViewRow row, out int residentId)
	{
		residentId = 0;
		if (row.Cells["resident_id"]?.Value == null || row.Cells["resident_id"].Value == DBNull.Value)
		{
			return false;
		}

		return int.TryParse(row.Cells["resident_id"].Value.ToString(), out residentId) && residentId > 0;
	}





	private void SetResidentLocationSelection(int? barangayId, int? purokId, int? householdId)
	{
		_suppressLocationEvents = true;
		try
		{
			EnsureResidentLocationLookups();
			int selectedBarangay = barangayId ?? SchemaDefaults.DefaultBarangayId;
			SelectComboById(_editBarangay, selectedBarangay);

			using var conn = OpenLookupConnection();
			int barangayValue = GetSelectedLookupId(_editBarangay) ?? SchemaDefaults.DefaultBarangayId;
			ReloadPurokList(conn, barangayValue, purokId ?? SchemaDefaults.DefaultPurokId);
			int? purokValue = GetSelectedLookupId(_editPurok);
			ReloadHouseholdList(conn, barangayValue, purokValue, householdId);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load location data.", "Warning");
		}
		finally
		{
			_suppressLocationEvents = false;
		}
	}


	private void PopulateResidentDetails(DataGridViewRow row)


	{
		if (!TryGetResidentId(row, out int residentId))
		{
			return;
		}

			_selectedResidentId = residentId;
			_residentDetailsLoadedId = residentId;
			int loadVersion = ++_residentDetailsLoadVersion;
			UpdateRightPanelSelectionState();
			if (!_suppressAutoOverviewOnSelection)
			{
				SetResidentProfileTab("overview", userInitiated: false, force: true);
		}

		bool previous = _suppressEditChangeTracking;
		_suppressEditChangeTracking = true;
		try
		{

			_editFirstName.Text = GetCellText(row, "firstname");


			_editMiddleName.Text = GetCellText(row, "middlename");


			_editLastName.Text = GetCellText(row, "lastname");


			_editGender.Text = GetCellText(row, "gender");


			_editCivil.Text = GetCellText(row, "civil_status");


			_editContact.Text = GetCellText(row, "contact_no");


			_editStatus.Text = GetCellText(row, "status");
			SetResidentLocationSelection(
				GetCellNullableInt(row, "barangay_id"),
				GetCellNullableInt(row, "purok_id"),
				GetCellNullableInt(row, "household_id"));


			object obj = row.Cells["date_of_birth"]?.Value;


			_editDob.Value = ((obj is DateTime dateTime) ? dateTime.Date : (DateTime.TryParse(obj?.ToString(), out var result) ? result.Date : DateTime.Today));
			SyncResidentChoiceEditorsFromText();
		}
		finally
		{
			_suppressEditChangeTracking = previous;
		}


			_residentPhotoBytes = null;
			_residentPhotoPendingBytes = null;
			_residentPhotoRemoved = false;
			LoadResidentPhoto(null);
			UpdateResidentPhotoControls();
			int photoLoadVersion = ++_residentPhotoLoadVersion;
			_ = LoadResidentPhotoAsync(residentId, loadVersion, photoLoadVersion);


		SetDetailMessage(null);


		SetDetailEditing(enabled: false);
		ResetResidentEditDirty();


		UpdateResidentHeader();


		LoadBlottersForResident(residentId);
		UpdateBlotterActionState();


			_ = LoadResidentDocumentsAndAuditAsync(residentId, loadVersion);
			ResetProfileViewport();
			RaiseResidentRouteChanged();


	}

	private async Task LoadResidentDocumentsAndAuditAsync(int residentId, int loadVersion)
	{
		if (residentId <= 0 || IsDisposed)
		{
			return;
		}

		_residentAsyncLoadDepth++;
		if (panelRightRoot != null)
		{
			panelRightRoot.Enabled = false;
		}

		BeginModuleLoading("Loading resident documents and audit logs...");
		try
		{
			Task<DataTable> certificatesTask = Task.Run(() => QueryCertificatesForResident(residentId));
			Task<DataTable> historyTask = Task.Run(() => QueryResidentHistory(residentId));
			await Task.WhenAll(certificatesTask, historyTask);

			if (IsDisposed
				|| loadVersion != _residentDetailsLoadVersion
				|| !_selectedResidentId.HasValue
				|| _selectedResidentId.Value != residentId)
			{
				return;
			}

			ApplyCertificatesData(certificatesTask.Result);
			ApplyResidentHistoryData(historyTask.Result);
			UpdateResidentInsightPanel();
		}
		catch (Exception ex)
		{
			if (!IsDisposed && loadVersion == _residentDetailsLoadVersion)
			{
				_residentDetailsLoadedId = null;
				ControllerDialogs.Error(ex, "Unable to load resident documents and audit logs.", "Error");
			}
		}
		finally
		{
			EndModuleLoading();
			_residentAsyncLoadDepth = Math.Max(0, _residentAsyncLoadDepth - 1);
			if (!IsDisposed && panelRightRoot != null && _residentAsyncLoadDepth == 0)
			{
				panelRightRoot.Enabled = true;
			}
		}
	}





	private void SetDetailEditing(bool enabled)


	{


		_isEditing = enabled;


		_editFirstName.ReadOnly = !enabled;


		_editMiddleName.ReadOnly = !enabled;


		_editLastName.ReadOnly = !enabled;


		_editGender.ReadOnly = !enabled;
		_editCivil.ReadOnly = !enabled;


		_editContact.ReadOnly = !enabled;


		_editStatus.ReadOnly = !enabled;
		_editGenderCombo.Enabled = enabled;
		_editCivilCombo.Enabled = enabled;
		_editStatusCombo.Enabled = enabled;
		_editBarangay.Enabled = enabled;
		_editPurok.Enabled = enabled;
		_editHousehold.Enabled = enabled;


		_editDob.Enabled = enabled;


		_editFirstName.BackColor = Color.White;


		_editMiddleName.BackColor = Color.White;


		_editLastName.BackColor = Color.White;


		_editGender.BackColor = Color.White;


		_editCivil.BackColor = Color.White;


		_editContact.BackColor = Color.White;


		_editStatus.BackColor = Color.White;
		_editBarangay.BackColor = Color.White;
		_editPurok.BackColor = Color.White;
		_editHousehold.BackColor = Color.White;


		dgvResidents.Enabled = !enabled;


		add.Enabled = !enabled;


		button3.Enabled = !enabled;


		button1.Enabled = !enabled;


		_residentEditModeBadge.Visible = enabled;
		UpdateResidentHeader();
		ConfigureProfileResponsiveLayout(_responsiveMode);


		UpdateResidentPhotoControls();
		UpdateResidentPickerSummary();
		UpdateResidentEditActionsState();


	}





	private void EnterEditMode()


	{


		SetProfileDetailsExpanded(expanded: true);
		SyncResidentChoiceEditorsFromText();
		ResetResidentEditDirty();
		SetDetailEditing(enabled: true);


	}





	private void ExitEditMode()


	{


		SetDetailEditing(enabled: false);
		ResetResidentEditDirty();


	}





	private Control BuildBlotterSection()


	{


		Panel panel = new Panel


		{


			Dock = DockStyle.Fill,


			Padding = new Padding(0, 12, 0, 0),


			AutoScroll = true


		};


		Label value = new Label


		{


			Text = "Blotter Cases",


			Font = UiTheme.HeadingFont,


			ForeColor = UiTheme.Slate900,


			AutoSize = true,


			Dock = DockStyle.Top


		};


		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel


		{


			Dock = DockStyle.Top,


			AutoSize = true,


			FlowDirection = FlowDirection.LeftToRight,


			WrapContents = false,


			Padding = new Padding(0, 8, 0, 8)


		};


		_btnFileBlotter.Text = "New Blotter Case";


		_btnRefreshBlotter.Text = "Refresh";
		_btnOpenBlotter.Text = "Open";
		_btnBlotterAttachments.Text = "Attachments";


		UiTheme.StylePrimaryButton(_btnFileBlotter);


		UiTheme.StyleSecondaryButton(_btnRefreshBlotter);
		UiTheme.StyleSecondaryButton(_btnOpenBlotter);
		UiTheme.StyleSecondaryButton(_btnBlotterAttachments);

		_btnFileBlotter.AutoSize = true;
		_btnRefreshBlotter.AutoSize = true;
		_btnOpenBlotter.AutoSize = true;
		_btnBlotterAttachments.AutoSize = true;
		_btnFileBlotter.Enabled = false;


		_btnRefreshBlotter.Enabled = false;
		_btnOpenBlotter.Enabled = false;
		_btnBlotterAttachments.Enabled = false;


		_btnFileBlotter.Margin = new Padding(0, 0, 12, 0);


		_btnRefreshBlotter.Margin = new Padding(0, 0, 12, 0);
		_btnOpenBlotter.Margin = new Padding(0, 0, 12, 0);
		_btnBlotterAttachments.Margin = new Padding(0, 0, 0, 0);


		_btnFileBlotter.Click -= FileBlotter_Click;


		_btnFileBlotter.Click += FileBlotter_Click;


		_btnRefreshBlotter.Click -= RefreshBlotter_Click;


		_btnRefreshBlotter.Click += RefreshBlotter_Click;
		_btnOpenBlotter.Click -= OpenBlotter_Click;
		_btnOpenBlotter.Click += OpenBlotter_Click;
		_btnBlotterAttachments.Click -= BlotterAttachments_Click;
		_btnBlotterAttachments.Click += BlotterAttachments_Click;


		flowLayoutPanel.Controls.Add(_btnFileBlotter);


		flowLayoutPanel.Controls.Add(_btnRefreshBlotter);
		flowLayoutPanel.Controls.Add(_btnOpenBlotter);
		flowLayoutPanel.Controls.Add(_btnBlotterAttachments);
		EnsureBlotterPagerControls(flowLayoutPanel);


		_blotterGrid.Dock = DockStyle.Fill;


		_blotterGrid.ReadOnly = true;


		_blotterGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;


		_blotterGrid.MultiSelect = false;


		_blotterGrid.AllowUserToAddRows = false;


		_blotterGrid.AllowUserToDeleteRows = false;


		_blotterGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;


		UiTheme.StyleGrid(_blotterGrid);


		Panel panel2 = new Panel


		{


			Dock = DockStyle.Fill,


			Padding = new Padding(0, 8, 0, 0)


		};


		_blotterEmptyPanel = CreateEmptyStatePanel(_blotterEmptyTitle, _blotterEmptyMessage);


		panel2.Controls.Add(_blotterGrid);


		panel2.Controls.Add(_blotterEmptyPanel);


		panel.Controls.Add(panel2);


		panel.Controls.Add(flowLayoutPanel);


		panel.Controls.Add(value);


		UpdateBlotterEmptyState();


		return panel;


	}





	private void FileBlotter_Click(object? sender, EventArgs e)


	{

		_controller.HandleFileBlotter(sender, e);

	}





	private void RefreshBlotter_Click(object? sender, EventArgs e)


	{

		_controller.HandleRefreshBlotter(sender, e);

	}

	private void UpdateCertificateSlaBadge(string rawStatus, DateTime? requestedAt, DateTime? approvedAt)
	{
		SlaEvaluation evaluation = SlaRules.EvaluateCertificate(rawStatus, requestedAt, approvedAt, DateTime.Now);
		if (!evaluation.Applies)
		{
			_certSla.Visible = false;
			_certSla.Text = string.Empty;
			return;
		}

		_certSla.Visible = true;
		_certSla.Text = SlaRules.FormatShortLabel(evaluation);

		switch (evaluation.State)
		{
			case SlaState.OnTrack:
				_certSla.BackColor = Color.FromArgb(235, 235, 235);
				_certSla.ForeColor = UiTheme.Slate700;
				break;
			case SlaState.DueSoon:
				_certSla.BackColor = Color.FromArgb(255, 230, 200);
				_certSla.ForeColor = Color.FromArgb(122, 69, 0);
				break;
			case SlaState.Overdue:
				_certSla.BackColor = Color.FromArgb(255, 220, 220);
				_certSla.ForeColor = Color.FromArgb(140, 30, 30);
				break;
			default:
				_certSla.BackColor = Color.FromArgb(235, 235, 235);
				_certSla.ForeColor = UiTheme.Slate700;
				break;
		}
	}

	private static DateTime? TryParseDateTime(object? value)
	{
		if (value == null || value == DBNull.Value)
		{
			return null;
		}

		if (value is DateTime dt)
		{
			return dt;
		}

		if (value is DateTimeOffset dto)
		{
			return dto.DateTime;
		}

		return DateTime.TryParse(value.ToString(), out DateTime parsed) ? parsed : (DateTime?)null;
	}





	private void LoadBlottersForResident(int residentId)


	{
		BeginModuleLoading("Loading blotter records...");


		try


		{


			string respondentIdSelect = _supportsRespondentResidentId
				? "respondent_resident_id,\r\n                         "
				: string.Empty;

			string sql = _supportsBlotterExtended
				? $@"SELECT case_id AS blotter_id,
                         {respondentIdSelect}respondent_name,
                         incident_type,
                         incident_date,
                         incident_time,
                         incident_location,
                         witness_names,
                         action_taken,
                         resolution_details,
                         incident_details,
                         status AS status_raw,
                         CASE status
                             WHEN 'OPEN' THEN 'Ongoing'
                             WHEN 'ONGOING' THEN 'Ongoing'
                             WHEN 'SETTLED' THEN 'Settled'
                             WHEN 'REFERRED' THEN 'Referred'
                             WHEN 'CLOSED' THEN 'Closed'
                             ELSE status
                         END AS status,
                         created_at
                  FROM case_record
                  WHERE complainant_id = @cid
                  ORDER BY incident_date DESC, created_at DESC"
				: $@"SELECT case_id AS blotter_id,
                         {respondentIdSelect}respondent_name,
                         incident_type,
                         incident_date,
                         incident_details,
                         status AS status_raw,
                         CASE status
                             WHEN 'OPEN' THEN 'Ongoing'
                             WHEN 'ONGOING' THEN 'Ongoing'
                             WHEN 'SETTLED' THEN 'Settled'
                             WHEN 'REFERRED' THEN 'Referred'
                             WHEN 'CLOSED' THEN 'Closed'
                             ELSE status
                         END AS status,
                         created_at
                  FROM case_record
                  WHERE complainant_id = @cid
                  ORDER BY incident_date DESC, created_at DESC";

			DataTable dataTable = DbHelper.LoadTable(sql, cmd => cmd.Parameters.AddWithValue("@cid", residentId));

			_blotterRecords = new List<BlotterRecordSummary>();
			foreach (DataRow row in dataTable.Rows)
			{
				int blotterId = row["blotter_id"] != DBNull.Value ? Convert.ToInt32(row["blotter_id"]) : 0;
				if (blotterId <= 0)
				{
					continue;
				}

				int? respondentResidentId = null;
				if (dataTable.Columns.Contains("respondent_resident_id") && row["respondent_resident_id"] != DBNull.Value)
				{
					int parsedRespondentId = Convert.ToInt32(row["respondent_resident_id"]);
					if (parsedRespondentId > 0)
					{
						respondentResidentId = parsedRespondentId;
					}
				}

				DateTime incidentDate = DateTime.Today;
				if (row["incident_date"] != DBNull.Value && DateTime.TryParse(row["incident_date"]?.ToString(), out DateTime parsedIncidentDate))
				{
					incidentDate = parsedIncidentDate.Date;
				}

				DateTime createdAt = DateTime.MinValue;
				if (row["created_at"] != DBNull.Value && DateTime.TryParse(row["created_at"]?.ToString(), out DateTime parsedCreatedAt))
				{
					createdAt = parsedCreatedAt;
				}

				TimeSpan? incidentTime = null;
				if (dataTable.Columns.Contains("incident_time") && row["incident_time"] != DBNull.Value)
				{
					if (row["incident_time"] is TimeSpan ts)
					{
						incidentTime = ts;
					}
					else if (TimeSpan.TryParse(row["incident_time"]?.ToString(), out TimeSpan parsedTime))
					{
						incidentTime = parsedTime;
					}
				}

				_blotterRecords.Add(new BlotterRecordSummary
				{
					BlotterId = blotterId,
					RespondentResidentId = respondentResidentId,
					RespondentName = row["respondent_name"]?.ToString() ?? string.Empty,
					IncidentType = row["incident_type"]?.ToString() ?? string.Empty,
					IncidentDate = incidentDate,
					IncidentTime = incidentTime,
					IncidentLocation = dataTable.Columns.Contains("incident_location") ? row["incident_location"]?.ToString() ?? string.Empty : string.Empty,
					Witnesses = dataTable.Columns.Contains("witness_names") ? row["witness_names"]?.ToString() ?? string.Empty : string.Empty,
					ActionTaken = dataTable.Columns.Contains("action_taken") ? row["action_taken"]?.ToString() ?? string.Empty : string.Empty,
					ResolutionDetails = dataTable.Columns.Contains("resolution_details") ? row["resolution_details"]?.ToString() ?? string.Empty : string.Empty,
					IncidentDetails = row["incident_details"]?.ToString() ?? string.Empty,
					Status = row["status"]?.ToString() ?? string.Empty,
					StatusRaw = dataTable.Columns.Contains("status_raw") ? row["status_raw"]?.ToString() ?? string.Empty : string.Empty,
					CreatedAt = createdAt
				});
			}

			ApplyRepeatRespondentCounts();
			_blotterPageIndex = 0;
			RenderBlotterCards();
			UpdateBlotterEmptyState();
			UpdateBlotterActionState();
			UpdateResidentInsightPanel();


		}


		catch (Exception ex)


		{


			ControllerDialogs.Error(ex, "Unable to load blotter records.", "Error");


		}
		finally
		{
			EndModuleLoading();
		}


	}

	private void ApplyRepeatRespondentCounts()
	{
		if (_blotterRecords == null || _blotterRecords.Count == 0)
		{
			return;
		}

		var residentIds = new HashSet<int>();
		var namesAll = new HashSet<string>(StringComparer.Ordinal);
		var namesNullIdOnly = new HashSet<string>(StringComparer.Ordinal);

		foreach (BlotterRecordSummary record in _blotterRecords)
		{
			string normalized = RepeatRespondentService.NormalizeName(record.RespondentName);
			record.NormalizedRespondentName = normalized;

			if (record.RespondentResidentId.HasValue && record.RespondentResidentId.Value > 0)
			{
				residentIds.Add(record.RespondentResidentId.Value);
				if (!string.IsNullOrWhiteSpace(normalized))
				{
					namesNullIdOnly.Add(normalized);
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(normalized))
				{
					namesAll.Add(normalized);
				}
			}
		}

		RepeatRespondentBatch batch = RepeatRespondentService.LoadCounts(residentIds, namesAll, namesNullIdOnly);

		foreach (BlotterRecordSummary record in _blotterRecords)
		{
			string normalized = record.NormalizedRespondentName;
			RepeatRespondentCounts counts = RepeatRespondentCounts.Zero;

			if (record.RespondentResidentId.HasValue && record.RespondentResidentId.Value > 0)
			{
				if (batch.ByResidentId.TryGetValue(record.RespondentResidentId.Value, out RepeatRespondentCounts byId))
				{
					counts = counts.Add(byId);
				}

				if (!string.IsNullOrWhiteSpace(normalized) &&
					batch.ByNameNullIdOnly.TryGetValue(normalized, out RepeatRespondentCounts byLegacyName))
				{
					counts = counts.Add(byLegacyName);
				}
			}
			else if (!string.IsNullOrWhiteSpace(normalized))
			{
				if (batch.ByNameAll.TryGetValue(normalized, out RepeatRespondentCounts byName))
				{
					counts = byName;
				}
			}

			record.RepeatTotalCases = counts.TotalCases;
			record.RepeatActiveCases = counts.ActiveCases;
		}
	}





	private void ClearBlotters()


	{


		_blotterRecords.Clear();
		_blotterFilteredRecords.Clear();
		_selectedBlotterId = null;
		_blotterPageIndex = 0;
		_blotterCardViews.Clear();
		_blotterCardsList.Controls.Clear();
		_blotterGrid.DataSource = null;
		UpdateCaseDetailPanel(null);


		UpdateBlotterEmptyState();
		UpdateBlotterActionState();
		UpdateBlotterPagerState();
		UpdateResidentInsightPanel();


	}





	private void OpenBlotter_Click(object? sender, EventArgs e)


	{

		_controller.HandleOpenBlotter(sender, e);

	}





	private void BlotterCardsList_Resize(object? sender, EventArgs e)


	{

		ReflowBlotterCards();

	}





	private void ReflowBlotterCards()


	{

		if (_blotterCardsList == null || _blotterCardsList.Controls.Count == 0)
		{
			return;
		}

		int available = _blotterCardsList.ClientSize.Width - 8;
		if (_blotterCardsList.VerticalScroll.Visible)
		{
			available -= SystemInformation.VerticalScrollBarWidth;
		}

		if (available < 240)
		{
			available = 240;
		}

		foreach (Control control in _blotterCardsList.Controls)
		{
			control.Width = available;
		}

	}





	private void RenderBlotterCards()


	{
		_blotterFilteredRecords = GetFilteredBlotterRecords();
		List<BlotterRecordSummary> pageRecords = GetCurrentBlotterPageRecords();

		DataTable table = new DataTable();
		table.Columns.Add("case_id", typeof(int));
		table.Columns.Add("Blotter #", typeof(string));
		table.Columns.Add("Incident", typeof(string));
		table.Columns.Add("Respondent", typeof(string));
		table.Columns.Add("Date Filed", typeof(string));
		table.Columns.Add("Status", typeof(string));
		table.Columns.Add("Last Updated", typeof(string));

		foreach (BlotterRecordSummary record in pageRecords)
		{
			string dateText = record.IncidentDate == DateTime.MinValue ? "-" : record.IncidentDate.ToString("MMM dd, yyyy");
			string updatedText = record.CreatedAt == DateTime.MinValue ? "-" : record.CreatedAt.ToString("MMM dd, yyyy hh:mm tt");
			table.Rows.Add(
				record.BlotterId,
				$"#{record.BlotterId}",
				string.IsNullOrWhiteSpace(record.IncidentType) ? "-" : record.IncidentType.Trim(),
				string.IsNullOrWhiteSpace(record.RespondentName) ? "-" : record.RespondentName.Trim(),
				dateText,
				WorkflowRules.NormalizeBlotterStatus(string.IsNullOrWhiteSpace(record.Status) ? record.StatusRaw : record.Status).ToUpperInvariant(),
				updatedText);
		}

		_blotterSelectionSync = true;
		try
		{
			_blotterGrid.DataSource = table;
		}
		finally
		{
			_blotterSelectionSync = false;
		}

		if (_blotterGrid.Columns.Contains("case_id"))
		{
			_blotterGrid.Columns["case_id"].Visible = false;
		}
		if (_blotterGrid.Columns.Contains("Blotter #"))
		{
			_blotterGrid.Columns["Blotter #"].FillWeight = 20;
		}
		if (_blotterGrid.Columns.Contains("Incident"))
		{
			_blotterGrid.Columns["Incident"].FillWeight = 22;
		}
		if (_blotterGrid.Columns.Contains("Respondent"))
		{
			_blotterGrid.Columns["Respondent"].FillWeight = 24;
		}
		if (_blotterGrid.Columns.Contains("Date Filed"))
		{
			_blotterGrid.Columns["Date Filed"].FillWeight = 16;
		}
		if (_blotterGrid.Columns.Contains("Status"))
		{
			_blotterGrid.Columns["Status"].FillWeight = 14;
			_blotterGrid.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
		}
		if (_blotterGrid.Columns.Contains("Last Updated"))
		{
			_blotterGrid.Columns["Last Updated"].FillWeight = 20;
		}

		UpdateBlotterPagerState();
		UpdateBlotterEmptyState();

		if (_blotterFilteredRecords.Count == 0)
		{
			_selectedBlotterId = null;
			UpdateCaseDetailPanel(null);
			UpdateBlotterActionState();
			return;
		}

		int targetId = pageRecords.Count > 0 ? pageRecords[0].BlotterId : _blotterFilteredRecords[0].BlotterId;
		if (_selectedBlotterId.HasValue && _blotterFilteredRecords.Any(record => record.BlotterId == _selectedBlotterId.Value))
		{
			targetId = _selectedBlotterId.Value;
		}

		SelectBlotterCard(targetId);

	}

	private List<BlotterRecordSummary> GetCurrentBlotterPageRecords()
	{
		if (_blotterFilteredRecords.Count == 0)
		{
			return new List<BlotterRecordSummary>();
		}

		int start = _blotterPageIndex * BlotterPageSize;
		if (start < 0)
		{
			start = 0;
		}

		if (start >= _blotterFilteredRecords.Count)
		{
			start = Math.Max(0, (_blotterFilteredRecords.Count - 1) / BlotterPageSize * BlotterPageSize);
		}

		return _blotterFilteredRecords.Skip(start).Take(BlotterPageSize).ToList();
	}





	private Panel BuildBlotterCard(BlotterRecordSummary record)


	{

		Panel card = new Panel
		{
			Height = 106,
			Margin = new Padding(0, 0, 0, 10),
			Padding = new Padding(12),
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = Color.White,
			Cursor = Cursors.Hand,
			Tag = record.BlotterId
		};

		Panel selectionBar = new Panel
		{
			Name = "SelectionBar",
			Dock = DockStyle.Left,
			Width = 4,
			BackColor = Color.Transparent
		};

		Panel content = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(10, 0, 0, 0),
			BackColor = Color.Transparent
		};

		Label title = new Label
		{
			Dock = DockStyle.Top,
			AutoSize = false,
			Height = 25,
			Font = UiTheme.BodyFont,
			ForeColor = UiTheme.Slate900,
			Text = string.IsNullOrWhiteSpace(record.IncidentType) ? "(No incident type)" : record.IncidentType.Trim()
		};

		Label respondent = new Label
		{
			Dock = DockStyle.Top,
			AutoSize = false,
			Height = 24,
			Font = UiTheme.LabelFont,
			ForeColor = UiTheme.Slate700,
			Text = "Respondent: " + (string.IsNullOrWhiteSpace(record.RespondentName) ? "-" : record.RespondentName.Trim())
		};

		string incidentDate = record.IncidentDate == DateTime.MinValue ? "-" : record.IncidentDate.ToString("dd MMM yyyy");
		string incidentTime = record.IncidentTime.HasValue ? DateTime.Today.Add(record.IncidentTime.Value).ToString("hh:mm tt") : string.Empty;
		string recordedDate = record.CreatedAt == DateTime.MinValue ? "-" : record.CreatedAt.ToString("dd MMM yyyy hh:mm tt");
		string location = string.IsNullOrWhiteSpace(record.IncidentLocation) ? string.Empty : record.IncidentLocation.Trim();
		string incidentMeta = incidentDate;
		if (!string.IsNullOrWhiteSpace(incidentTime))
		{
			incidentMeta = incidentMeta + " " + incidentTime;
		}
		if (!string.IsNullOrWhiteSpace(location))
		{
			incidentMeta = incidentMeta + " | " + location;
		}
		Label meta = new Label
		{
			Dock = DockStyle.Top,
			AutoSize = false,
			Height = 22,
			Font = UiTheme.SmallFont,
			ForeColor = UiTheme.Slate500,
			Text = $"Incident: {incidentMeta}    Recorded: {recordedDate}"
		};

		Label status = new Label
		{
			Anchor = AnchorStyles.Top | AnchorStyles.Right,
			AutoSize = false,
			Size = new Size(90, 24),
			TextAlign = ContentAlignment.MiddleCenter,
			Font = UiTheme.SmallFont,
			Text = string.IsNullOrWhiteSpace(record.Status) ? "Unknown" : record.Status.Trim(),
			BackColor = Color.White,
			ForeColor = UiTheme.Slate900,
			BorderStyle = BorderStyle.FixedSingle,
			Location = new Point(Math.Max(0, card.Width - 118), 12)
		};

		DateTime? createdAt = record.CreatedAt == DateTime.MinValue ? (DateTime?)null : record.CreatedAt;
		SlaEvaluation slaEvaluation = SlaRules.EvaluateBlotter(record.StatusRaw, createdAt, DateTime.Now);

		Label sla = new Label
		{
			Anchor = AnchorStyles.Top | AnchorStyles.Right,
			AutoSize = false,
			Size = new Size(90, 20),
			TextAlign = ContentAlignment.MiddleCenter,
			Font = UiTheme.SmallFont,
			BackColor = Color.White,
			ForeColor = UiTheme.Slate900,
			BorderStyle = BorderStyle.FixedSingle,
			Location = new Point(Math.Max(0, card.Width - 118), 42),
			Visible = slaEvaluation.State is SlaState.DueSoon or SlaState.Overdue
		};

		if (sla.Visible)
		{
			sla.Text = SlaRules.FormatShortLabel(slaEvaluation);
			if (slaEvaluation.State == SlaState.DueSoon)
			{
				sla.BackColor = Color.FromArgb(255, 230, 200);
				sla.ForeColor = Color.FromArgb(122, 69, 0);
			}
			else
			{
				sla.BackColor = Color.FromArgb(255, 220, 220);
				sla.ForeColor = Color.FromArgb(140, 30, 30);
			}
		}

		Label repeat = new Label
		{
			Anchor = AnchorStyles.Top | AnchorStyles.Right,
			AutoSize = false,
			Size = new Size(90, 20),
			TextAlign = ContentAlignment.MiddleCenter,
			Font = UiTheme.SmallFont,
			BackColor = Color.White,
			ForeColor = UiTheme.Slate900,
			BorderStyle = BorderStyle.FixedSingle,
			Location = new Point(Math.Max(0, card.Width - 118), sla.Visible ? (sla.Location.Y + sla.Height + 6) : (status.Location.Y + status.Height + 6)),
			Visible = record.RepeatTotalCases >= 2
		};

		if (repeat.Visible)
		{
			repeat.Text = $"Repeat x{record.RepeatTotalCases}";
			if (record.RepeatActiveCases >= 2)
			{
				repeat.BackColor = Color.FromArgb(255, 220, 220);
				repeat.ForeColor = Color.FromArgb(140, 30, 30);
			}
			else if (record.RepeatActiveCases == 1)
			{
				repeat.BackColor = Color.FromArgb(255, 230, 200);
				repeat.ForeColor = Color.FromArgb(122, 69, 0);
			}
			else
			{
				repeat.BackColor = Color.FromArgb(240, 240, 240);
				repeat.ForeColor = UiTheme.Slate700;
			}
		}

		card.Resize += (_, __) =>
		{
			int right = card.ClientSize.Width - 12;
			status.Location = new Point(Math.Max(8, right - status.Width), 12);
			sla.Location = new Point(Math.Max(8, right - sla.Width), status.Bottom + 6);
			int nextY = sla.Visible ? sla.Bottom + 6 : status.Bottom + 6;
			repeat.Location = new Point(Math.Max(8, right - repeat.Width), nextY);
		};

		string normalizedStatus = (record.Status ?? string.Empty).Trim();
		if (string.Equals(normalizedStatus, "Ongoing", StringComparison.OrdinalIgnoreCase))
		{
			status.ForeColor = Color.FromArgb(196, 90, 0);
		}
		else if (string.Equals(normalizedStatus, "Settled", StringComparison.OrdinalIgnoreCase))
		{
			status.ForeColor = Color.FromArgb(30, 130, 30);
		}
		else if (string.Equals(normalizedStatus, "Referred", StringComparison.OrdinalIgnoreCase))
		{
			status.ForeColor = Color.FromArgb(31, 95, 170);
		}

		content.Controls.Add(meta);
		content.Controls.Add(respondent);
		content.Controls.Add(title);
		card.Controls.Add(content);
		card.Controls.Add(selectionBar);
		card.Controls.Add(status);
		card.Controls.Add(sla);
		card.Controls.Add(repeat);

		AttachBlotterCardClickHandlers(card, record.BlotterId);
		return card;

	}





	private void AttachBlotterCardClickHandlers(Control control, int blotterId)


	{

		control.Click += (_, __) => SelectBlotterCard(blotterId);
		foreach (Control child in control.Controls)
		{
			AttachBlotterCardClickHandlers(child, blotterId);
		}

	}





	private void SelectBlotterCard(int blotterId)


	{
		_selectedBlotterId = blotterId;
		if (_blotterGrid.DataSource != null && _blotterGrid.Rows.Count > 0)
		{
			_blotterSelectionSync = true;
			try
			{
				_blotterGrid.ClearSelection();
				foreach (DataGridViewRow row in _blotterGrid.Rows)
				{
					object? idValue = row.Cells["case_id"]?.Value;
					if (idValue == null || idValue == DBNull.Value)
					{
						continue;
					}

					if (Convert.ToInt32(idValue) != blotterId)
					{
						continue;
					}

					row.Selected = true;
					if (row.Cells.Count > 1)
					{
						_blotterGrid.CurrentCell = row.Cells[1];
					}

					break;
				}
			}
			finally
			{
				_blotterSelectionSync = false;
			}
		}

		UpdateCaseDetailPanel(GetSelectedBlotterRecord());
		UpdateBlotterActionState();

	}

	private void BlotterGrid_SelectionChanged(object? sender, EventArgs e)
	{
		if (_blotterSelectionSync)
		{
			return;
		}

		if (_blotterGrid.SelectedRows.Count <= 0)
		{
			_selectedBlotterId = null;
			UpdateCaseDetailPanel(null);
			UpdateBlotterActionState();
			return;
		}

		object? idValue = _blotterGrid.SelectedRows[0].Cells["case_id"]?.Value;
		if (idValue == null || idValue == DBNull.Value)
		{
			_selectedBlotterId = null;
			UpdateCaseDetailPanel(null);
			UpdateBlotterActionState();
			return;
		}

		_selectedBlotterId = Convert.ToInt32(idValue);
		UpdateCaseDetailPanel(GetSelectedBlotterRecord());
		UpdateBlotterActionState();
	}

	private void BlotterGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0)
		{
			return;
		}

		_controller.HandleOpenBlotter(sender, EventArgs.Empty);
	}

	private void CaseSearchBox_TextChanged(object? sender, EventArgs e)
	{
		_casesSearchDebounce.Stop();
		_casesSearchDebounce.Start();
	}

	private void BlotterFilterChanged(object? sender, EventArgs e)
	{
		_blotterPageIndex = 0;
		RenderBlotterCards();
	}

	private void CasesSearchDebounce_Tick(object? sender, EventArgs e)
	{
		_casesSearchDebounce.Stop();
		_blotterPageIndex = 0;
		RenderBlotterCards();
	}

	private List<BlotterRecordSummary> GetFilteredBlotterRecords()
	{
		IEnumerable<BlotterRecordSummary> query = _blotterRecords;

		if (!string.IsNullOrWhiteSpace(_caseSearchBox.Text))
		{
			string term = _caseSearchBox.Text.Trim();
			query = query.Where(record =>
				(record.IncidentType ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
				(record.RespondentName ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
				(record.IncidentLocation ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
				(record.IncidentDetails ?? string.Empty).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		string statusFilter = (_caseStatusFilter.SelectedItem?.ToString() ?? "All").Trim();
		if (!string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
		{
			string normalizedFilter = WorkflowRules.NormalizeBlotterStatus(statusFilter);
			query = query.Where(record =>
			{
				string normalized = WorkflowRules.NormalizeBlotterStatus(record.StatusRaw);
				return string.Equals(normalized, normalizedFilter, StringComparison.OrdinalIgnoreCase);
			});
		}

		DateTime fromDate = _caseFromDate.Value.Date;
		DateTime toDate = _caseToDate.Value.Date;
		if (fromDate > toDate)
		{
			(fromDate, toDate) = (toDate, fromDate);
		}

		query = query.Where(record =>
			record.IncidentDate == DateTime.MinValue ||
			(record.IncidentDate.Date >= fromDate && record.IncidentDate.Date <= toDate));

		return query
			.OrderByDescending(record => record.IncidentDate)
			.ThenByDescending(record => record.CreatedAt)
			.ToList();
	}

	private void UpdateCaseDetailPanel(BlotterRecordSummary? record)
	{
		if (record == null)
		{
			_casesIncidentTitle.Text = "Select a blotter case";
			_casesMeta.Text = "Respondent: - | Filed: -";
			_casesStatusBadge.Text = "UNKNOWN";
			_casesStatusBadge.BackColor = Color.FromArgb(241, 245, 249);
			_casesStatusBadge.ForeColor = UiTheme.Slate700;
			_casesOverviewDetails.Text = string.Empty;
			_casesOverviewWitnesses.Items.Clear();
			_casesOverviewWitnessesEmptyState.Visible = true;
			_casesTimelineGrid.DataSource = null;
			_casesAttachmentsList.Items.Clear();
			UpdateCaseAttachmentsEmptyState("No attachments for this blotter case yet.");
			return;
		}

		string incident = string.IsNullOrWhiteSpace(record.IncidentType) ? $"Blotter #{record.BlotterId}" : record.IncidentType.Trim();
		string respondent = string.IsNullOrWhiteSpace(record.RespondentName) ? "-" : record.RespondentName.Trim();
		string dateText = record.IncidentDate == DateTime.MinValue ? "-" : record.IncidentDate.ToString("MMM dd, yyyy");
		string location = string.IsNullOrWhiteSpace(record.IncidentLocation) ? "-" : record.IncidentLocation.Trim();
		string statusText = string.IsNullOrWhiteSpace(record.Status) ? WorkflowRules.NormalizeBlotterStatus(record.StatusRaw) : record.Status.Trim();

		_casesIncidentTitle.Text = $"Blotter Case: {incident}";
		_casesMeta.Text = $"Respondent: {respondent} | Filed: {dateText} | Location: {location}";
		_casesStatusBadge.Text = statusText.ToUpperInvariant();
		ApplyCaseStatusBadgeStyle(statusText);

		string overview = string.IsNullOrWhiteSpace(record.IncidentDetails) ? "-" : record.IncidentDetails.Trim();
		if (!string.IsNullOrWhiteSpace(record.ActionTaken))
		{
			overview += Environment.NewLine + Environment.NewLine + "Action Taken:" + Environment.NewLine + record.ActionTaken.Trim();
		}
		if (!string.IsNullOrWhiteSpace(record.ResolutionDetails))
		{
			overview += Environment.NewLine + Environment.NewLine + "Resolution:" + Environment.NewLine + record.ResolutionDetails.Trim();
		}
		_casesOverviewDetails.Text = overview;

		_casesOverviewWitnesses.Items.Clear();
		string[] witnesses = (record.Witnesses ?? string.Empty)
			.Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string witness in witnesses)
		{
			string cleaned = witness.Trim();
			if (cleaned.Length == 0)
			{
				continue;
			}

			_casesOverviewWitnesses.Items.Add(cleaned);
		}

		if (_casesOverviewWitnesses.Items.Count == 0)
		{
			_casesOverviewWitnessesEmptyState.Visible = true;
		}
		else
		{
			_casesOverviewWitnessesEmptyState.Visible = false;
		}

		LoadCaseTimeline(record.BlotterId);
		LoadCaseAttachments(record.BlotterId);
	}

	private void ApplyCaseStatusBadgeStyle(string status)
	{
		string normalized = WorkflowRules.NormalizeBlotterStatus(status);
		if (string.Equals(normalized, "ONGOING", StringComparison.OrdinalIgnoreCase))
		{
			_casesStatusBadge.BackColor = Color.FromArgb(219, 234, 254);
			_casesStatusBadge.ForeColor = Color.FromArgb(30, 64, 175);
			return;
		}

		if (string.Equals(normalized, "SETTLED", StringComparison.OrdinalIgnoreCase))
		{
			_casesStatusBadge.BackColor = Color.FromArgb(220, 252, 231);
			_casesStatusBadge.ForeColor = Color.FromArgb(22, 101, 52);
			return;
		}

		if (string.Equals(normalized, "REFERRED", StringComparison.OrdinalIgnoreCase))
		{
			_casesStatusBadge.BackColor = Color.FromArgb(254, 249, 195);
			_casesStatusBadge.ForeColor = Color.FromArgb(133, 77, 14);
			return;
		}

		if (string.Equals(normalized, "CLOSED", StringComparison.OrdinalIgnoreCase))
		{
			_casesStatusBadge.BackColor = Color.FromArgb(226, 232, 240);
			_casesStatusBadge.ForeColor = Color.FromArgb(51, 65, 85);
			return;
		}

		_casesStatusBadge.BackColor = Color.FromArgb(241, 245, 249);
		_casesStatusBadge.ForeColor = UiTheme.Slate700;
	}

	private void LoadCaseTimeline(int caseId)
	{
		try
		{
			DataTable raw = CaseTimelineService.LoadTimeline(caseId, limit: 120);
			DataTable display = new DataTable();
			display.Columns.Add("Date/Time", typeof(string));
			display.Columns.Add("Action", typeof(string));
			display.Columns.Add("By", typeof(string));
			display.Columns.Add("Notes", typeof(string));

			foreach (DataRow row in raw.Rows)
			{
				DateTime timestamp = DateTime.MinValue;
				if (row["created_at"] != DBNull.Value && DateTime.TryParse(row["created_at"]?.ToString(), out DateTime parsed))
				{
					timestamp = parsed;
				}

				string action = row["event_title"]?.ToString() ?? "Update";
				string by = string.IsNullOrWhiteSpace(row["created_by"]?.ToString()) ? "-" : row["created_by"]?.ToString()!;
				string note = row["event_details"]?.ToString() ?? string.Empty;
				display.Rows.Add(
					timestamp == DateTime.MinValue ? "-" : timestamp.ToString("MMM dd, yyyy hh:mm tt"),
					action,
					by,
					note);
			}

			_casesTimelineGrid.DataSource = display;
			if (_casesTimelineGrid.Columns.Contains("Date/Time"))
			{
				_casesTimelineGrid.Columns["Date/Time"].FillWeight = 24;
			}
			if (_casesTimelineGrid.Columns.Contains("Action"))
			{
				_casesTimelineGrid.Columns["Action"].FillWeight = 26;
			}
			if (_casesTimelineGrid.Columns.Contains("By"))
			{
				_casesTimelineGrid.Columns["By"].FillWeight = 16;
			}
			if (_casesTimelineGrid.Columns.Contains("Notes"))
			{
				_casesTimelineGrid.Columns["Notes"].FillWeight = 34;
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to load case timeline.", ex);
			_casesTimelineGrid.DataSource = null;
		}
	}

	private void LoadCaseAttachments(int caseId)
	{
		_casesAttachmentsList.Items.Clear();
		if (caseId <= 0)
		{
			UpdateCaseAttachmentsEmptyState("No attachments for this blotter case yet.");
			return;
		}

		try
		{
			DataTable table = DbHelper.LoadTable(
				@"SELECT attachment_id,
                         file_name,
                         file_ext,
                         uploaded_at
                  FROM record_attachment
                  WHERE entity_type = 'CASE' AND entity_id = @id
                  ORDER BY uploaded_at DESC, attachment_id DESC",
				cmd => cmd.Parameters.AddWithValue("@id", caseId));

			foreach (DataRow row in table.Rows)
			{
				long attachmentId = row["attachment_id"] != DBNull.Value ? Convert.ToInt64(row["attachment_id"]) : 0L;
				string fileName = row["file_name"]?.ToString() ?? "Attachment";
				string fileExt = row["file_ext"]?.ToString() ?? string.Empty;
				DateTime uploadedAt = row["uploaded_at"] != DBNull.Value && DateTime.TryParse(row["uploaded_at"]?.ToString(), out DateTime parsed)
					? parsed
					: DateTime.MinValue;

				ListViewItem item = new ListViewItem(fileName);
				item.SubItems.Add(string.IsNullOrWhiteSpace(fileExt) ? "-" : fileExt.ToUpperInvariant());
				item.SubItems.Add(uploadedAt == DateTime.MinValue ? "-" : uploadedAt.ToString("MMM dd, yyyy hh:mm tt"));
				item.SubItems.Add("-");
				item.Tag = attachmentId;
				_casesAttachmentsList.Items.Add(item);
			}
			UpdateCaseAttachmentsEmptyState("No attachments for this blotter case yet.");
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Failed to load case attachments.", ex);
			UpdateCaseAttachmentsEmptyState("Unable to load attachments.");
		}
	}

	private void UpdateCaseAttachmentsEmptyState(string message)
	{
		if (_casesAttachmentsEmptyState == null || _casesAttachmentsEmptyState.IsDisposed)
		{
			return;
		}

		_casesAttachmentsEmptyState.Text = message;
		_casesAttachmentsEmptyState.Visible = _casesAttachmentsList.Items.Count == 0;
	}

	private static string FormatFileSize(long bytes)
	{
		if (bytes <= 0)
		{
			return "-";
		}

		string[] units = { "B", "KB", "MB", "GB" };
		double size = bytes;
		int index = 0;
		while (size >= 1024 && index < units.Length - 1)
		{
			size /= 1024;
			index++;
		}

		return $"{size:0.#} {units[index]}";
	}

	private void CasesAttachmentManage_Click(object? sender, EventArgs e)
	{
		_controller.HandleBlotterAttachments(sender, e);
		if (_selectedBlotterId.HasValue)
		{
			LoadCaseAttachments(_selectedBlotterId.Value);
		}
	}

	private void PrintBlotterCase_Click(object? sender, EventArgs e)
	{
		_controller.HandleOpenBlotter(sender, e);
	}

	private void CloseBlotterCase_Click(object? sender, EventArgs e)
	{
		if (!_selectedBlotterId.HasValue)
		{
			ControllerDialogs.Warning("Select a blotter case first.");
			return;
		}

		BlotterRecordSummary? selected = GetSelectedBlotterRecord();
		if (selected == null)
		{
			ControllerDialogs.Warning("Select a blotter case first.");
			return;
		}

		string fromStatus = selected.StatusRaw;
		if (!WorkflowRules.TryValidateBlotterTransition(fromStatus, "CLOSED", out string transitionMessage))
		{
			ControllerDialogs.Warning(string.IsNullOrWhiteSpace(transitionMessage) ? "This blotter case cannot be closed yet." : transitionMessage);
			return;
		}

		if (ControllerDialogs.Confirm("Close the selected blotter case?", "Close Blotter Case") != DialogResult.Yes)
		{
			return;
		}

		try
		{
			DbHelper.ExecuteNonQuery(
				@"UPDATE case_record
                  SET status = @status
                  WHERE case_id = @id",
				cmd =>
				{
					cmd.Parameters.AddWithValue("@status", "CLOSED");
					cmd.Parameters.AddWithValue("@id", selected.BlotterId);
				});

			CaseTimelineService.Log(
				selected.BlotterId,
				"STATUS",
				"Blotter case closed",
				"Status changed to CLOSED.",
				WorkflowRules.NormalizeBlotterStatus(fromStatus),
				"CLOSED",
				UserSession.UserId);

			if (_selectedResidentId.HasValue)
			{
				LogActivity(_selectedResidentId.Value, "Blotter", "Closed", $"Blotter case #{selected.BlotterId} closed.");
				LoadBlottersForResident(_selectedResidentId.Value);
				LoadResidentHistory(_selectedResidentId.Value);
			}
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to close the selected blotter case.");
		}
	}





	private BlotterRecordSummary? GetSelectedBlotterRecord()


	{

		if (!_selectedBlotterId.HasValue)
		{
			return null;
		}

		return _blotterRecords.FirstOrDefault(record => record.BlotterId == _selectedBlotterId.Value);

	}





	private List<string> LoadResidentNameSuggestions()


	{


		List<string> list = new List<string>();


		try


		{


			using MySqlConnection mySqlConnection = DBConnection.GetConnection();


			mySqlConnection.Open();


			using MySqlCommand mySqlCommand = new MySqlCommand("SELECT first_name, middle_name, last_name\r\n                                       FROM resident\r\n                                       WHERE IFNULL(is_deleted,0)=0\r\n                                       ORDER BY last_name, first_name", mySqlConnection);


			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();


			while (mySqlDataReader.Read())


			{


				string text = mySqlDataReader["first_name"]?.ToString() ?? string.Empty;
				string text2 = mySqlDataReader["middle_name"]?.ToString() ?? string.Empty;
				string text3 = mySqlDataReader["last_name"]?.ToString() ?? string.Empty;


				string text4 = string.Join(" ", new string[3] { text, text2, text3 }.Where((string part) => !string.IsNullOrWhiteSpace(part)));


				if (!string.IsNullOrWhiteSpace(text4) && !list.Contains(text4))


				{


					list.Add(text4);


				}


			}


		}


		catch


		{


		}


		return list;


	}





	private void InsertBlotter(BlotterDto blotter)


	{

		if (!Permissions.CanCreateBlotter)
		{
			throw new UnauthorizedAccessException("You do not have permission to file blotter cases.");
		}

		if (!WorkflowRules.TryValidateNewBlotterStatus(blotter.Status, out string blotterStatusMessage))
		{
			throw new InvalidOperationException(blotterStatusMessage);
		}


		using MySqlConnection mySqlConnection = DBConnection.GetConnection();


		mySqlConnection.Open();
		SchemaBootstrap.EnsureCoreDefaults(mySqlConnection);
		using MySqlTransaction tx = mySqlConnection.BeginTransaction();


		int caseTypeId = GetOrCreateCaseTypeId(mySqlConnection, blotter.IncidentType, tx);

		const string sql = @"INSERT INTO case_record
                               (barangay_id, case_type_id, date_filed, incident_date, incident_location, summary, status, handled_by_user_id,
                                complainant_id, respondent_resident_id, respondent_name, incident_type, incident_time, witness_names, action_taken,
                                resolution_details, incident_details, recorded_by, created_at)
                             VALUES
                               (@barangayId, @caseTypeId, @filed, @date, @location, @summary, @status, @handled,
                                @cid, @resp_rid, @resp, @type, @time, @witness, @action,
                                @resolution, @details, @recorded, NOW())";

		using MySqlCommand mySqlCommand = new MySqlCommand(sql, mySqlConnection, tx);
		mySqlCommand.Parameters.AddWithValue("@barangayId", SchemaDefaults.DefaultBarangayId);
		mySqlCommand.Parameters.AddWithValue("@caseTypeId", caseTypeId);
		mySqlCommand.Parameters.AddWithValue("@filed", DateTime.Today);
		mySqlCommand.Parameters.AddWithValue("@date", blotter.IncidentDate);
		mySqlCommand.Parameters.AddWithValue("@location", string.IsNullOrWhiteSpace(blotter.IncidentLocation) ? DBNull.Value : blotter.IncidentLocation);
		mySqlCommand.Parameters.AddWithValue("@summary", string.IsNullOrWhiteSpace(blotter.IncidentDetails) ? DBNull.Value : blotter.IncidentDetails);
		mySqlCommand.Parameters.AddWithValue("@status", MapBlotterStatusToDb(blotter.Status));
		mySqlCommand.Parameters.AddWithValue("@handled", blotter.RecordedBy);
		mySqlCommand.Parameters.AddWithValue("@cid", blotter.ComplainantId);
		mySqlCommand.Parameters.AddWithValue("@resp_rid", blotter.RespondentResidentId.HasValue ? blotter.RespondentResidentId.Value : DBNull.Value);
		mySqlCommand.Parameters.AddWithValue("@resp", blotter.RespondentName);
		mySqlCommand.Parameters.AddWithValue("@type", blotter.IncidentType);
		mySqlCommand.Parameters.AddWithValue("@time", blotter.IncidentTime.HasValue ? blotter.IncidentTime.Value : DBNull.Value);
		mySqlCommand.Parameters.AddWithValue("@witness", string.IsNullOrWhiteSpace(blotter.Witnesses) ? DBNull.Value : blotter.Witnesses);
		mySqlCommand.Parameters.AddWithValue("@action", string.IsNullOrWhiteSpace(blotter.ActionTaken) ? DBNull.Value : blotter.ActionTaken);
		mySqlCommand.Parameters.AddWithValue("@resolution", string.IsNullOrWhiteSpace(blotter.ResolutionDetails) ? DBNull.Value : blotter.ResolutionDetails);
		mySqlCommand.Parameters.AddWithValue("@details", blotter.IncidentDetails);
		mySqlCommand.Parameters.AddWithValue("@recorded", blotter.RecordedBy);
		mySqlCommand.ExecuteNonQuery();
		int caseId = (int)mySqlCommand.LastInsertedId;


		string details = (blotter.IncidentType + " - " + blotter.RespondentName).Trim();


		AuditTrailService.LogTransactional(
			mySqlConnection,
			tx,
			"Blotter",
			"case_record",
			caseId,
			"CREATE",
			null,
			new
			{
				CaseId = caseId,
				blotter.ComplainantId,
				blotter.RespondentResidentId,
				blotter.RespondentName,
				blotter.IncidentType,
				blotter.IncidentDate,
				blotter.IncidentTime,
				blotter.IncidentLocation,
				blotter.Witnesses,
				blotter.ActionTaken,
				blotter.ResolutionDetails,
				blotter.IncidentDetails,
				Status = WorkflowRules.NormalizeBlotterStatus(blotter.Status),
				blotter.RecordedBy
			},
			"Blotter case filed.",
			blotter.RecordedBy);

		string status = WorkflowRules.NormalizeBlotterStatus(blotter.Status);
		string timelineDetails = $"Status: {status}";
		if (!string.IsNullOrWhiteSpace(blotter.IncidentType))
		{
			timelineDetails += $"\nType: {blotter.IncidentType.Trim()}";
		}
		if (blotter.IncidentDate != DateTime.MinValue)
		{
			timelineDetails += $"\nIncident date: {blotter.IncidentDate:yyyy-MM-dd}";
		}
		if (!string.IsNullOrWhiteSpace(blotter.RespondentName))
		{
			timelineDetails += $"\nRespondent: {blotter.RespondentName.Trim()}";
		}
		CaseTimelineService.LogTransactional(
			mySqlConnection,
			tx,
			caseId,
			"CREATE",
			"Blotter case filed",
			timelineDetails,
			null,
			status,
			blotter.RecordedBy);
		tx.Commit();
		LogActivity(blotter.ComplainantId, "Blotter", "Filed", details);


	}





	private void BuildBlotterForm()


	{


		_blotterFormPanel.Dock = DockStyle.Top;


		_blotterFormPanel.Visible = false;


		_blotterFormPanel.Padding = new Padding(0, 0, 0, 8);


		_blotterFormPanel.AutoSize = true;


		_blotterFormPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;


		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel


		{


			ColumnCount = 2,


			Dock = DockStyle.Top,


			AutoSize = true,


			AutoSizeMode = AutoSizeMode.GrowAndShrink


		};


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));


		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));


		_blotterRespondent = new TextBox();


		_blotterIncidentType = new TextBox();


		_blotterIncidentDate = new DateTimePicker


		{


			Format = DateTimePickerFormat.Short


		};


		_blotterDetails = new TextBox


		{


			Multiline = true,


			Height = 80,


			ScrollBars = ScrollBars.Vertical


		};


		_blotterStatus = new ComboBox


		{


			DropDownStyle = ComboBoxStyle.DropDownList


		};


		_blotterStatus.Items.Add("Ongoing");


		if (_blotterStatus.Items.Count > 0)


		{


			_blotterStatus.SelectedIndex = 0;


		}


		UiTheme.StyleTextBox(_blotterRespondent);


		UiTheme.StyleTextBox(_blotterIncidentType);


		UiTheme.StyleTextBox(_blotterDetails);


		UiTheme.StyleComboBox(_blotterStatus);


		_blotterIncidentDate.Font = UiTheme.BodyFont;


		AddDetailRow(tableLayoutPanel, "Respondent", _blotterRespondent);


		AddDetailRow(tableLayoutPanel, "Incident type", _blotterIncidentType);


		AddDetailRow(tableLayoutPanel, "Incident date", _blotterIncidentDate);


		AddDetailRow(tableLayoutPanel, "Details", _blotterDetails);


		AddDetailRow(tableLayoutPanel, "Status", _blotterStatus);


		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel


		{


			Dock = DockStyle.Top,


			AutoSize = true,


			FlowDirection = FlowDirection.LeftToRight,


			WrapContents = false,


			Padding = new Padding(0, 8, 0, 8)


		};


		_blotterSave.Text = "Save Blotter";


		_blotterCancel.Text = "Cancel";


		UiTheme.StylePrimaryButton(_blotterSave);


		UiTheme.StyleSecondaryButton(_blotterCancel);


		_blotterSave.Margin = new Padding(0, 0, 12, 0);


		_blotterSave.Click -= BlotterSave_Click;


		_blotterSave.Click += BlotterSave_Click;


		_blotterCancel.Click -= BlotterCancel_Click;


		_blotterCancel.Click += BlotterCancel_Click;


		flowLayoutPanel.Controls.Add(_blotterSave);


		flowLayoutPanel.Controls.Add(_blotterCancel);


		_blotterFormPanel.Controls.Clear();


		_blotterFormPanel.Controls.Add(flowLayoutPanel);


		_blotterFormPanel.Controls.Add(tableLayoutPanel);


	}





	private void BlotterSave_Click(object? sender, EventArgs e)


	{

		_controller.HandleBlotterSave(sender, e);

	}





	private void BlotterCancel_Click(object? sender, EventArgs e)


	{

		_controller.HandleBlotterCancel(sender, e);

	}





	private void ShowBlotterForm(bool show)


	{


		_blotterFormPanel.Visible = show;


		if (show)


		{


			_blotterRespondent.Text = string.Empty;


			_blotterIncidentType.Text = string.Empty;


			_blotterDetails.Text = string.Empty;


			_blotterIncidentDate.Value = DateTime.Today;


			if (_blotterStatus.Items.Count > 0)


			{


				_blotterStatus.SelectedIndex = 0;


			}


		}


	}



	private sealed class BlotterRecordSummary
	{
		public int BlotterId { get; set; }

		public int? RespondentResidentId { get; set; }

		public string RespondentName { get; set; } = string.Empty;

		public string NormalizedRespondentName { get; set; } = string.Empty;

		public string IncidentType { get; set; } = string.Empty;

		public DateTime IncidentDate { get; set; }

		public TimeSpan? IncidentTime { get; set; }

		public string IncidentLocation { get; set; } = string.Empty;

		public string Witnesses { get; set; } = string.Empty;

		public string ActionTaken { get; set; } = string.Empty;

		public string ResolutionDetails { get; set; } = string.Empty;

		public string IncidentDetails { get; set; } = string.Empty;

		public string Status { get; set; } = string.Empty;

		public string StatusRaw { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; }

		public int RepeatTotalCases { get; set; }

		public int RepeatActiveCases { get; set; }
	}
}














