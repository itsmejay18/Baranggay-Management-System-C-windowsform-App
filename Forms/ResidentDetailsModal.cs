using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1;

internal sealed class ResidentDetailsModal : Form
{
	private readonly int _residentId;
	private readonly bool _readOnly;
	private readonly int _initialTabIndex;

	private bool _suppressLookupEvents;
	private bool _relationshipTableAvailable;
	private byte[]? _photoBytes;
	private bool _photoRemoved;
	private int _originalPurokId;
	private int? _originalHouseholdId;
	private string _originalAddress = string.Empty;

	private readonly Dictionary<int, string> _residentDirectory = new Dictionary<int, string>();
	private readonly DataTable _relationshipTable = new DataTable();
	private readonly HashSet<string> _residentColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly TableLayoutPanel _root = new TableLayoutPanel();
	private readonly Panel _bodyPanel = new Panel();
	private readonly TableLayoutPanel _header = new TableLayoutPanel();
	private readonly Label _title = new Label();
	private readonly Label _subtitle = new Label();
	private readonly Label _statusBadge = new Label();
	private readonly PictureBox _photo = new PictureBox();
	private readonly Button _uploadPhoto = new Button();
	private readonly Button _removePhoto = new Button();
	private readonly TabControl _tabs = new TabControl();
	private readonly Panel _footerSeparator = new Panel();
	private readonly TableLayoutPanel _footer = new TableLayoutPanel();
	private readonly Label _validationHint = new Label();
	private readonly FlowLayoutPanel _footerActions = new FlowLayoutPanel();
	private readonly Button _cancel = new Button();
	private readonly Button _save = new Button();

	private readonly TextBox _firstName = new TextBox();
	private readonly TextBox _middleName = new TextBox();
	private readonly TextBox _lastName = new TextBox();
	private readonly ComboBox _sex = new ComboBox();
	private readonly DateTimePicker _birthDate = new DateTimePicker();
	private readonly ComboBox _civilStatus = new ComboBox();
	private readonly TextBox _contact = new TextBox();
	private readonly TextBox _email = new TextBox();
	private readonly ComboBox _status = new ComboBox();

	private readonly ComboBox _barangay = new ComboBox();
	private readonly ComboBox _purok = new ComboBox();
	private readonly ComboBox _household = new ComboBox();
	private readonly TextBox _addressPreview = new TextBox();

	private readonly TextBox _birthPlace = new TextBox();
	private readonly TextBox _citizenship = new TextBox();
	private readonly TextBox _religion = new TextBox();
	private readonly TextBox _occupation = new TextBox();
	private readonly TextBox _employer = new TextBox();
	private readonly ComboBox _education = new ComboBox();

	private readonly CheckBox _isPwd = new CheckBox();
	private readonly TextBox _pwdIdNo = new TextBox();
	private readonly CheckBox _isSenior = new CheckBox();
	private readonly CheckBox _is4Ps = new CheckBox();
	private readonly CheckBox _isVoter = new CheckBox();
	private readonly TextBox _voterPrecinctNo = new TextBox();

	private readonly DataGridView _relationshipsGrid = new DataGridView();
	private readonly Button _relationshipAdd = new Button();
	private readonly Button _relationshipEdit = new Button();
	private readonly Button _relationshipRemove = new Button();
	private readonly TextBox _relationshipSearch = new TextBox();
	private readonly Label _relationshipsHint = new Label();
	private readonly Label _relationshipsEmptyState = new Label();

	private readonly DataGridView _historyGrid = new DataGridView();
	private readonly ComboBox _historyModuleFilter = new ComboBox();
	private readonly TextBox _historySearch = new TextBox();
	private DataTable _historyTable = new DataTable();

	public ResidentDetailsModal(int residentId, bool readOnly = false, int initialTabIndex = 0)
	{
		_residentId = residentId;
		_readOnly = readOnly;
		_initialTabIndex = initialTabIndex;

		InitializeComponent();
		WireEvents();
		ApplyTheme();
	}

	private void InitializeComponent()
	{
		SuspendLayout();

		Text = "Resident Details";
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MinimumSize = new Size(900, 650);
		Size = new Size(980, 710);
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;
		BackColor = UiTheme.Slate50;
		Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

		_root.Dock = DockStyle.Fill;
		_root.Padding = Padding.Empty;
		_root.Margin = Padding.Empty;
		_root.ColumnCount = 1;
		_root.RowCount = 3;
		_root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_root.RowStyles.Add(new RowStyle(SizeType.Absolute, 162F));
		_root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));

		ConfigureHeader();
		ConfigureTabs();
		ConfigureFooter();

		_root.Controls.Add(_header, 0, 0);
		_root.Controls.Add(_bodyPanel, 0, 1);
		_root.Controls.Add(_footer, 0, 2);

		Controls.Add(_root);
		AcceptButton = _save;
		CancelButton = _cancel;

		ResumeLayout(performLayout: true);
	}

	private void ConfigureHeader()
	{
		_header.Dock = DockStyle.Fill;
		_header.Margin = Padding.Empty;
		_header.Padding = new Padding(24, 12, 24, 12);
		_header.BackColor = Color.White;
		_header.ColumnCount = 3;
		_header.RowCount = 1;
		_header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		TableLayoutPanel left = new TableLayoutPanel
		{
			Dock = DockStyle.Left,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 1,
			RowCount = 3
		};
		left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
		left.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
		left.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		_title.Dock = DockStyle.Fill;
		_title.TextAlign = ContentAlignment.MiddleLeft;
		_title.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
		_title.ForeColor = UiTheme.Slate900;
		_title.Text = "Resident Details";
		_title.Margin = Padding.Empty;

		_subtitle.Dock = DockStyle.Fill;
		_subtitle.TextAlign = ContentAlignment.MiddleLeft;
		_subtitle.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
		_subtitle.ForeColor = UiTheme.Slate600;
		_subtitle.Text = string.Empty;
		_subtitle.Margin = new Padding(0, 0, 0, 2);

		FlowLayoutPanel statusFlow = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			AutoSize = false,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		_statusBadge.AutoSize = true;
		_statusBadge.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
		_statusBadge.Padding = new Padding(12, 4, 12, 4);
		_statusBadge.Margin = new Padding(0, 2, 0, 0);
		statusFlow.Controls.Add(_statusBadge);

		left.Controls.Add(_title, 0, 0);
		left.Controls.Add(_subtitle, 0, 1);
		left.Controls.Add(statusFlow, 0, 2);

		Panel photoCard = new Panel
		{
			Dock = DockStyle.Right,
			Size = new Size(228, 132),
			Margin = Padding.Empty,
			Padding = new Padding(10, 8, 10, 8),
			BackColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle
		};
		TableLayoutPanel photoLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 1,
			RowCount = 2
		};
		photoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		photoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
		photoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

		_photo.Dock = DockStyle.None;
		_photo.SizeMode = PictureBoxSizeMode.Zoom;
		_photo.Size = new Size(96, 96);
		_photo.BackColor = UiTheme.Slate100;
		_photo.BorderStyle = BorderStyle.FixedSingle;
		_photo.Anchor = AnchorStyles.None;
		_photo.Margin = Padding.Empty;

		_uploadPhoto.Text = "Upload";
		_uploadPhoto.AutoSize = false;
		_uploadPhoto.Size = new Size(100, 30);
		_uploadPhoto.Margin = Padding.Empty;

		_removePhoto.Text = "Remove";
		_removePhoto.AutoSize = false;
		_removePhoto.Size = new Size(100, 30);
		_removePhoto.Margin = new Padding(8, 0, 0, 0);

		FlowLayoutPanel photoActions = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		photoActions.Controls.Add(_uploadPhoto);
		photoActions.Controls.Add(_removePhoto);

		photoLayout.Controls.Add(_photo, 0, 0);
		photoLayout.Controls.Add(photoActions, 0, 1);
		photoCard.Controls.Add(photoLayout);

		_header.Controls.Add(left, 0, 0);
		_header.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty }, 1, 0);
		_header.Controls.Add(photoCard, 2, 0);
	}

	private void ConfigureTabs()
	{
		_bodyPanel.Dock = DockStyle.Fill;
		_bodyPanel.Margin = Padding.Empty;
		_bodyPanel.Padding = new Padding(16);
		_bodyPanel.BackColor = UiTheme.Slate50;

		_tabs.Dock = DockStyle.Fill;
		_tabs.Margin = Padding.Empty;
		_tabs.Padding = new Point(16, 6);
		_tabs.Appearance = TabAppearance.Normal;
		_tabs.SizeMode = TabSizeMode.Normal;
		_tabs.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

		_tabs.TabPages.Add(BuildBasicInfoTab());
		_tabs.TabPages.Add(BuildAddressTab());
		_tabs.TabPages.Add(BuildPersonalTab());
		_tabs.TabPages.Add(BuildFlagsTab());
		_tabs.TabPages.Add(BuildRelationshipsTab());
		_tabs.TabPages.Add(BuildHistoryTab());

		_bodyPanel.Controls.Add(_tabs);
	}

	private void ConfigureFooter()
	{
		_footer.Dock = DockStyle.Fill;
		_footer.Margin = Padding.Empty;
		_footer.Padding = new Padding(16, 10, 16, 16);
		_footer.BackColor = Color.White;
		_footer.ColumnCount = 2;
		_footer.RowCount = 2;
		_footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
		_footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		_footerSeparator.Dock = DockStyle.Fill;
		_footerSeparator.Margin = Padding.Empty;
		_footerSeparator.BackColor = Color.FromArgb(226, 232, 240);

		_validationHint.Dock = DockStyle.Fill;
		_validationHint.TextAlign = ContentAlignment.MiddleCenter;
		_validationHint.ForeColor = Color.FromArgb(185, 28, 28);
		_validationHint.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
		_validationHint.Text = string.Empty;
		_validationHint.Margin = new Padding(0, 8, 0, 0);

		_footerActions.AutoSize = true;
		_footerActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_footerActions.FlowDirection = FlowDirection.LeftToRight;
		_footerActions.WrapContents = false;
		_footerActions.Dock = DockStyle.Right;
		_footerActions.Margin = Padding.Empty;
		_footerActions.Padding = new Padding(0, 8, 0, 0);

		_cancel.Text = "Cancel";
		_cancel.AutoSize = false;
		_cancel.Size = new Size(100, 32);
		_cancel.Margin = new Padding(0, 0, 8, 0);

		_save.Text = "Save Changes";
		_save.AutoSize = false;
		_save.Size = new Size(120, 32);
		_save.Margin = Padding.Empty;

		_footerActions.Controls.Add(_cancel);
		_footerActions.Controls.Add(_save);
		_footer.Controls.Add(_footerSeparator, 0, 0);
		_footer.SetColumnSpan(_footerSeparator, 2);
		_footer.Controls.Add(_validationHint, 0, 1);
		_footer.Controls.Add(_footerActions, 1, 1);
	}

	private TabPage BuildBasicInfoTab()
	{
		TableLayoutPanel stack = CreateSectionStack();
		TableLayoutPanel identityForm = CreateFormGrid(rows: 3);
		TableLayoutPanel contactForm = CreateFormGrid(rows: 2);

		_firstName.MaxLength = 100;
		_middleName.MaxLength = 100;
		_lastName.MaxLength = 100;
		_contact.MaxLength = 24;
		_email.MaxLength = 150;

		_sex.DropDownStyle = ComboBoxStyle.DropDownList;
		_sex.Items.AddRange(new object[] { "M", "F" });

		_birthDate.Format = DateTimePickerFormat.Short;
		_birthDate.MaxDate = DateTime.Today;

		_civilStatus.DropDownStyle = ComboBoxStyle.DropDownList;
		_civilStatus.Items.AddRange(new object[] { "Single", "Married", "Widowed", "Separated" });

		_status.DropDownStyle = ComboBoxStyle.DropDownList;
		_status.Items.AddRange(new object[] { "ACTIVE", "DECEASED", "MOVED_OUT" });

		AddFormRow(identityForm, 0, "First Name *", _firstName, "Last Name *", _lastName);
		AddFormRow(identityForm, 1, "Middle Name", _middleName, "Sex *", _sex);
		AddFormRow(identityForm, 2, "Birth Date *", _birthDate, "Civil Status *", _civilStatus);

		AddFormRow(contactForm, 0, "Contact No", _contact, "Email", _email);
		AddFormRow(contactForm, 1, "Status", _status, string.Empty, new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty });

		stack.Controls.Add(CreateSectionCard("Identity", identityForm), 0, 0);
		stack.Controls.Add(CreateSectionCard("Contact", contactForm), 0, 1);
		return CreateStandardTab("Basic Info", stack, fillContent: false, allowScroll: true);
	}

	private TabPage BuildAddressTab()
	{
		TableLayoutPanel stack = CreateSectionStack();
		TableLayoutPanel form = CreateFormGrid(rows: 2);

		_barangay.DropDownStyle = ComboBoxStyle.DropDownList;
		_purok.DropDownStyle = ComboBoxStyle.DropDownList;
		_household.DropDownStyle = ComboBoxStyle.DropDownList;
		_addressPreview.ReadOnly = true;
		_addressPreview.TabStop = false;

		AddFormRow(form, 0, "Barangay", _barangay, "Purok/Sitio", _purok);
		AddFormRow(form, 1, "Household", _household, "Address Preview", _addressPreview);
		stack.Controls.Add(CreateSectionCard("Location", form), 0, 0);
		return CreateStandardTab("Address / Household", stack, fillContent: false, allowScroll: true);
	}

	private TabPage BuildPersonalTab()
	{
		TableLayoutPanel stack = CreateSectionStack();
		TableLayoutPanel form = CreateFormGrid(rows: 3);

		_birthPlace.MaxLength = 150;
		_citizenship.MaxLength = 100;
		_religion.MaxLength = 100;
		_occupation.MaxLength = 150;
		_employer.MaxLength = 150;
		_education.DropDownStyle = ComboBoxStyle.DropDownList;
		_education.Items.AddRange(new object[]
		{
			"(None)",
			"Elementary",
			"High School",
			"Vocational",
			"College",
			"Postgraduate"
		});

		AddFormRow(form, 0, "Birth Place", _birthPlace, "Citizenship", _citizenship);
		AddFormRow(form, 1, "Religion", _religion, "Occupation", _occupation);
		AddFormRow(form, 2, "Employer", _employer, "Education", _education);

		stack.Controls.Add(CreateSectionCard("Personal Background", form), 0, 0);
		return CreateStandardTab("Personal / Other", stack, fillContent: false, allowScroll: true);
	}

	private TabPage BuildFlagsTab()
	{
		TableLayoutPanel root = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 2,
			RowCount = 2
		};
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 155F));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));

		_isPwd.Text = "PWD";
		_isPwd.AutoSize = false;
		_isPwd.Height = 24;
		_isSenior.Text = "Senior Citizen";
		_isSenior.AutoSize = false;
		_isSenior.Height = 24;
		_is4Ps.Text = "4Ps Beneficiary";
		_is4Ps.AutoSize = false;
		_is4Ps.Height = 24;
		_isVoter.Text = "Registered Voter";
		_isVoter.AutoSize = false;
		_isVoter.Height = 24;

		_pwdIdNo.MaxLength = 100;
		_voterPrecinctNo.MaxLength = 50;
		_pwdIdNo.Dock = DockStyle.Top;
		_voterPrecinctNo.Dock = DockStyle.Top;
		_pwdIdNo.Height = 28;
		_voterPrecinctNo.Height = 28;
		_pwdIdNo.Margin = new Padding(0, 6, 0, 0);
		_voterPrecinctNo.Margin = new Padding(0, 6, 0, 0);

		GroupBox voterGroup = CreateFlagGroup("Voter");
		TableLayoutPanel voterLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = new Padding(6, 4, 6, 6)
		};
		voterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		voterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
		voterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
		voterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
		voterLayout.Controls.Add(_isVoter, 0, 0);
		voterLayout.Controls.Add(_voterPrecinctNo, 0, 1);
		voterLayout.Controls.Add(CreateHelperTextLabel("Only required if applicable."), 0, 2);
		voterGroup.Controls.Add(voterLayout);

		GroupBox programsGroup = CreateFlagGroup("Programs");
		TableLayoutPanel programsLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = new Padding(6, 4, 6, 6)
		};
		programsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		programsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
		programsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
		programsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
		programsLayout.Controls.Add(_isSenior, 0, 0);
		programsLayout.Controls.Add(_is4Ps, 0, 1);
		programsLayout.Controls.Add(CreateHelperTextLabel("Only required if applicable."), 0, 2);
		programsGroup.Controls.Add(programsLayout);

		GroupBox pwdGroup = CreateFlagGroup("PWD");
		TableLayoutPanel pwdLayout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = new Padding(6, 4, 6, 6)
		};
		pwdLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		pwdLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
		pwdLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
		pwdLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
		pwdLayout.Controls.Add(_isPwd, 0, 0);
		pwdLayout.Controls.Add(_pwdIdNo, 0, 1);
		pwdLayout.Controls.Add(CreateHelperTextLabel("Only required if applicable."), 0, 2);
		pwdGroup.Controls.Add(pwdLayout);

		voterGroup.Margin = new Padding(0, 0, 8, 8);
		programsGroup.Margin = new Padding(8, 0, 0, 8);
		pwdGroup.Margin = new Padding(0, 0, 0, 0);

		root.Controls.Add(voterGroup, 0, 0);
		root.Controls.Add(programsGroup, 1, 0);
		root.Controls.Add(pwdGroup, 0, 1);
		root.SetColumnSpan(pwdGroup, 2);

		return CreateStandardTab("Flags / Voter", root, fillContent: false, allowScroll: true);
	}

	private TabPage BuildRelationshipsTab()
	{
		_relationshipSearch.PlaceholderText = "Search related resident...";
		_relationshipSearch.Width = 230;
		_relationshipSearch.Margin = Padding.Empty;
		_relationshipSearch.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		TableLayoutPanel actionBar = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		actionBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		FlowLayoutPanel actions = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = new Padding(0, 0, 0, 8)
		};
		_relationshipAdd.Text = "Add";
		_relationshipEdit.Text = "Edit";
		_relationshipRemove.Text = "Remove";
		_relationshipAdd.Size = new Size(110, 32);
		_relationshipEdit.Size = new Size(100, 32);
		_relationshipRemove.Size = new Size(110, 32);
		_relationshipAdd.Margin = Padding.Empty;
		_relationshipEdit.Margin = new Padding(8, 0, 0, 0);
		_relationshipRemove.Margin = new Padding(8, 0, 0, 0);
		actions.Controls.Add(_relationshipAdd);
		actions.Controls.Add(_relationshipEdit);
		actions.Controls.Add(_relationshipRemove);

		FlowLayoutPanel searchBar = new FlowLayoutPanel
		{
			Dock = DockStyle.Right,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = new Padding(0, 2, 0, 8),
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink
		};
		searchBar.Controls.Add(_relationshipSearch);

		_relationshipsHint.Dock = DockStyle.Fill;
		_relationshipsHint.TextAlign = ContentAlignment.MiddleLeft;
		_relationshipsHint.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
		_relationshipsHint.ForeColor = UiTheme.Slate500;
		_relationshipsHint.Text = string.Empty;

		_relationshipsGrid.Dock = DockStyle.Fill;
		_relationshipsGrid.ReadOnly = true;
		_relationshipsGrid.MultiSelect = false;
		_relationshipsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_relationshipsGrid.AllowUserToAddRows = false;
		_relationshipsGrid.AllowUserToDeleteRows = false;
		_relationshipsGrid.AllowUserToResizeRows = false;
		_relationshipsGrid.RowHeadersVisible = false;
		_relationshipsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_relationshipsGrid.ColumnHeadersHeight = 36;
		_relationshipsGrid.RowTemplate.Height = 32;
		_relationshipsGrid.BackgroundColor = Color.White;

		_relationshipsEmptyState.Dock = DockStyle.Fill;
		_relationshipsEmptyState.TextAlign = ContentAlignment.MiddleCenter;
		_relationshipsEmptyState.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point);
		_relationshipsEmptyState.ForeColor = UiTheme.Slate500;
		_relationshipsEmptyState.BackColor = Color.Transparent;
		_relationshipsEmptyState.Text = "No relationships yet. Click Add to create one.";
		_relationshipsEmptyState.Visible = false;

		Panel gridHost = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		gridHost.Controls.Add(_relationshipsGrid);
		gridHost.Controls.Add(_relationshipsEmptyState);

		actionBar.Controls.Add(actions, 0, 0);
		actionBar.Controls.Add(searchBar, 1, 0);
		layout.Controls.Add(actionBar, 0, 0);
		layout.Controls.Add(_relationshipsHint, 0, 1);
		layout.Controls.Add(gridHost, 0, 2);
		return CreateStandardTab("Relationships", layout, fillContent: true, allowScroll: false);
	}

	private TabPage BuildHistoryTab()
	{
		_historyModuleFilter.DropDownStyle = ComboBoxStyle.DropDownList;
		_historyModuleFilter.Width = 170;
		_historyModuleFilter.Margin = Padding.Empty;
		_historyModuleFilter.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
		_historySearch.PlaceholderText = "Search logs...";
		_historySearch.Width = 220;
		_historySearch.Margin = new Padding(8, 0, 0, 0);
		_historySearch.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		TableLayoutPanel filterBar = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			Margin = Padding.Empty,
			Padding = new Padding(0, 0, 0, 8)
		};
		filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		filterBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		FlowLayoutPanel leftFilters = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		leftFilters.Controls.Add(_historyModuleFilter);

		FlowLayoutPanel rightFilters = new FlowLayoutPanel
		{
			Dock = DockStyle.Right,
			FlowDirection = FlowDirection.LeftToRight,
			WrapContents = false,
			Margin = Padding.Empty,
			Padding = Padding.Empty
		};
		rightFilters.Controls.Add(_historySearch);

		_historyGrid.Dock = DockStyle.Fill;
		_historyGrid.ReadOnly = true;
		_historyGrid.MultiSelect = false;
		_historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_historyGrid.AllowUserToAddRows = false;
		_historyGrid.AllowUserToDeleteRows = false;
		_historyGrid.AllowUserToResizeRows = false;
		_historyGrid.RowHeadersVisible = false;
		_historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_historyGrid.ColumnHeadersHeight = 36;
		_historyGrid.RowTemplate.Height = 32;
		_historyGrid.BackgroundColor = Color.White;

		filterBar.Controls.Add(leftFilters, 0, 0);
		filterBar.Controls.Add(rightFilters, 1, 0);
		layout.Controls.Add(filterBar, 0, 0);
		layout.Controls.Add(_historyGrid, 0, 1);
		return CreateStandardTab("History / Logs", layout, fillContent: true, allowScroll: false);
	}

	private TabPage CreateStandardTab(string title, Control content, bool fillContent, bool allowScroll)
	{
		TabPage tab = new TabPage(title)
		{
			BackColor = UiTheme.Slate50,
			Padding = Padding.Empty
		};
		Panel panelTab = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(16),
			BackColor = Color.White,
			AutoScroll = allowScroll
		};
		TableLayoutPanel centered = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 3,
			RowCount = 1
		};
		centered.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		centered.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 820F));
		centered.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		centered.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		Panel host = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			BackColor = Color.White
		};
		content.Dock = fillContent ? DockStyle.Fill : DockStyle.Top;
		host.Controls.Add(content);

		centered.Controls.Add(host, 1, 0);
		panelTab.Controls.Add(centered);
		tab.Controls.Add(panelTab);
		return tab;
	}

	private static TableLayoutPanel CreateSectionStack()
	{
		return new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 1,
			RowCount = 0,
			GrowStyle = TableLayoutPanelGrowStyle.AddRows
		};
	}

	private static Panel CreateSectionCard(string title, Control body)
	{
		Panel card = new Panel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Margin = new Padding(0, 0, 0, 12),
			Padding = new Padding(12),
			BackColor = Color.White,
			BorderStyle = BorderStyle.FixedSingle
		};
		TableLayoutPanel layout = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 1,
			RowCount = 3
		};
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		Label titleLabel = new Label
		{
			Dock = DockStyle.Fill,
			Text = title,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = Padding.Empty,
			Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = UiTheme.Slate700
		};
		Panel separator = new Panel
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0, 4, 0, 8),
			BackColor = Color.FromArgb(226, 232, 240)
		};
		body.Margin = Padding.Empty;
		body.Padding = Padding.Empty;

		layout.Controls.Add(titleLabel, 0, 0);
		layout.Controls.Add(separator, 0, 1);
		layout.Controls.Add(body, 0, 2);
		card.Controls.Add(layout);
		return card;
	}

	private static GroupBox CreateFlagGroup(string title)
	{
		return new GroupBox
		{
			Text = title,
			Dock = DockStyle.Fill,
			Margin = Padding.Empty,
			Padding = new Padding(8, 8, 8, 8),
			Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
			ForeColor = UiTheme.Slate700
		};
	}

	private static Label CreateHelperTextLabel(string text)
	{
		return new Label
		{
			Text = text,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Margin = new Padding(0),
			Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
			ForeColor = UiTheme.Slate500
		};
	}

	private static TableLayoutPanel CreateFormGrid(int rows)
	{
		TableLayoutPanel table = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			ColumnCount = 4,
			RowCount = rows
		};
		table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
		table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
		table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
		for (int i = 0; i < rows; i++)
		{
			table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
		}

		return table;
	}

	private void AddFormRow(TableLayoutPanel table, int row, string leftLabelText, Control leftControl, string rightLabelText, Control rightControl)
	{
		Label leftLabel = CreateFormLabel(leftLabelText);
		Label rightLabel = CreateFormLabel(rightLabelText);
		PrepareEditor(leftControl);
		PrepareEditor(rightControl);

		table.Controls.Add(leftLabel, 0, row);
		table.Controls.Add(leftControl, 1, row);
		table.Controls.Add(rightLabel, 2, row);
		table.Controls.Add(rightControl, 3, row);
	}

	private static Label CreateFormLabel(string text)
	{
		return new Label
		{
			Text = text,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleRight,
			AutoSize = false,
			Margin = new Padding(0, 3, 10, 3),
			Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
			ForeColor = UiTheme.Slate700
		};
	}

	private static void PrepareEditor(Control control)
	{
		control.Dock = DockStyle.Fill;
		control.Margin = new Padding(0, 3, 12, 3);
		if (control is TextBox || control is ComboBox || control is DateTimePicker || control is MaskedTextBox)
		{
			control.MinimumSize = new Size(0, 28);
			control.Height = 28;
		}
	}

	private void WireEvents()
	{
		Load += ResidentDetailsModal_Load;
		_cancel.Click += (_, __) => Close();
		_save.Click += Save_Click;
		_uploadPhoto.Click += UploadPhoto_Click;
		_removePhoto.Click += RemovePhoto_Click;

		_barangay.SelectedIndexChanged += Barangay_SelectedIndexChanged;
		_purok.SelectedIndexChanged += Purok_SelectedIndexChanged;
		_household.SelectedIndexChanged += (_, __) => UpdateAddressPreview();
		_isPwd.CheckedChanged += (_, __) => _pwdIdNo.Enabled = _isPwd.Checked;
		_isVoter.CheckedChanged += (_, __) => _voterPrecinctNo.Enabled = _isVoter.Checked;

		_relationshipAdd.Click += RelationshipAdd_Click;
		_relationshipEdit.Click += RelationshipEdit_Click;
		_relationshipRemove.Click += RelationshipRemove_Click;
		_relationshipSearch.TextChanged += (_, __) => ApplyRelationshipFilter();
		_historyModuleFilter.SelectedIndexChanged += (_, __) => ApplyHistoryFilter();
		_historySearch.TextChanged += (_, __) => ApplyHistoryFilter();

		HookValidationEvents(_tabs);
	}

	private void ApplyTheme()
	{
		UiTheme.StyleTextBoxes(
			_firstName, _middleName, _lastName, _contact, _email, _addressPreview,
			_birthPlace, _citizenship, _religion, _occupation, _employer, _pwdIdNo, _voterPrecinctNo,
			_relationshipSearch, _historySearch);
		UiTheme.StyleComboBoxes(_sex, _civilStatus, _status, _barangay, _purok, _household, _education);
		UiTheme.StyleComboBoxes(_historyModuleFilter);
		UiTheme.StyleSecondaryButton(_uploadPhoto);
		UiTheme.StyleSecondaryButton(_cancel);
		UiTheme.StylePrimaryButton(_relationshipAdd);
		UiTheme.StyleSecondaryButton(_relationshipEdit);
		UiTheme.StyleDangerButton(_removePhoto);
		UiTheme.StyleDangerButton(_relationshipRemove);
		UiTheme.StylePrimaryButton(_save);
		UiTheme.StyleGrid(_relationshipsGrid);
		UiTheme.StyleGrid(_historyGrid);

		_uploadPhoto.Height = 32;
		_removePhoto.Height = 32;
		_relationshipAdd.Height = 32;
		_relationshipEdit.Height = 32;
		_relationshipRemove.Height = 32;
		_save.Height = 32;
		_cancel.Height = 32;
		_historyModuleFilter.DropDownStyle = ComboBoxStyle.DropDownList;
		_addressPreview.BackColor = Color.FromArgb(248, 250, 252);
		_addressPreview.ReadOnly = true;
	}

	private void ResidentDetailsModal_Load(object? sender, EventArgs e)
	{
		try
		{
			using var conn = OpenConnection();

			// Keep modal available even when DB user cannot alter/create schema objects.
			TryEnsureSchema(() => EnsureResidentProfileColumns(conn), "resident profile columns");
			TryEnsureSchema(() => EnsureActivityLogSchema(conn), "activity_log schema");
			TryEnsureSchema(() => EnsureTransferHistorySchema(conn), "resident_transfer_history schema");
			_relationshipTableAvailable = TryEnsureRelationshipSchema(conn);
			RefreshResidentColumnMetadata(conn);

			LoadResidentRecord(conn);

			try
			{
				LoadResidentDirectory(conn);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("ResidentDetailsModal: unable to load resident directory.", ex);
				_residentDirectory.Clear();
			}

			try
			{
				LoadRelationships(conn);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("ResidentDetailsModal: unable to load relationships.", ex);
				_relationshipTableAvailable = false;
				_relationshipsHint.Text = "Relationships are unavailable.";
				_relationshipTable.Clear();
				_relationshipsGrid.DataSource = _relationshipTable.DefaultView;
				_relationshipAdd.Enabled = false;
				_relationshipEdit.Enabled = false;
				_relationshipRemove.Enabled = false;
				_relationshipSearch.Enabled = false;
				UpdateRelationshipsEmptyState();
			}

			try
			{
				LoadHistory(conn);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("ResidentDetailsModal: unable to load history.", ex);
				_historyTable = new DataTable();
				_historyGrid.DataSource = _historyTable.DefaultView;
				PopulateHistoryModuleFilter();
			}
			UpdateAddressPreview();
			UpdatePhotoPreview();
			ApplyReadOnlyMode();
			UpdateSaveState();

			if (_tabs.TabCount > 0)
			{
				int index = Math.Max(0, Math.Min(_initialTabIndex, _tabs.TabCount - 1));
				_tabs.SelectedIndex = index;
			}
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error($"Unable to open resident details.\n\nReason: {ex.Message}", "Resident Details", ex);
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}

	private void ApplyReadOnlyMode()
	{
		if (!_readOnly)
		{
			return;
		}

		_cancel.Text = "Close";
		_save.Visible = false;
		AcceptButton = null;

		SetInputsEnabled(this, enabled: false);
		_tabs.Enabled = true;
		_historyGrid.Enabled = true;
		_relationshipsGrid.Enabled = true;
		_cancel.Enabled = true;
	}

	private static void SetInputsEnabled(Control root, bool enabled)
	{
		foreach (Control control in root.Controls)
		{
			switch (control)
			{
				case TextBox textBox:
					textBox.ReadOnly = !enabled || textBox.ReadOnly;
					break;
				case ComboBox comboBox:
					comboBox.Enabled = enabled;
					break;
				case DateTimePicker picker:
					picker.Enabled = enabled;
					break;
				case CheckBox checkBox:
					checkBox.Enabled = enabled;
					break;
				case Button button:
					button.Enabled = enabled;
					break;
			}

			SetInputsEnabled(control, enabled);
		}
	}

	private void HookValidationEvents(Control root)
	{
		foreach (Control control in root.Controls)
		{
			switch (control)
			{
				case TextBox textBox:
					textBox.TextChanged += (_, __) => UpdateSaveState();
					break;
				case ComboBox comboBox:
					comboBox.SelectedIndexChanged += (_, __) => UpdateSaveState();
					break;
				case DateTimePicker picker:
					picker.ValueChanged += (_, __) => UpdateSaveState();
					break;
				case CheckBox checkBox:
					checkBox.CheckedChanged += (_, __) => UpdateSaveState();
					break;
			}

			HookValidationEvents(control);
		}
	}

	private void UpdateSaveState()
	{
		if (_readOnly)
		{
			_save.Enabled = false;
			_validationHint.Text = string.Empty;
			return;
		}

		if (!TryValidateInputs(out string message))
		{
			_save.Enabled = false;
			_validationHint.Text = message;
			return;
		}

		_validationHint.Text = string.Empty;
		_save.Enabled = true;
	}

	private bool TryValidateInputs(out string message)
	{
		if (string.IsNullOrWhiteSpace(_firstName.Text))
		{
			message = "First name is required.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_lastName.Text))
		{
			message = "Last name is required.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_sex.Text))
		{
			message = "Sex is required.";
			return false;
		}
		if (_birthDate.Value.Date > DateTime.Today)
		{
			message = "Birth date cannot be in the future.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_civilStatus.Text))
		{
			message = "Civil status is required.";
			return false;
		}
		if (!IsContactValid(_contact.Text))
		{
			message = "Contact number format is invalid.";
			return false;
		}
		if (_isPwd.Checked && string.IsNullOrWhiteSpace(_pwdIdNo.Text))
		{
			message = "PWD ID No is required when PWD is checked.";
			return false;
		}
		if (_isVoter.Checked && string.IsNullOrWhiteSpace(_voterPrecinctNo.Text))
		{
			message = "Voter precinct is required when Registered Voter is checked.";
			return false;
		}

		message = string.Empty;
		return true;
	}

	private static bool IsContactValid(string value)
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

	private void UploadPhoto_Click(object? sender, EventArgs e)
	{
		if (_readOnly)
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
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		try
		{
			_photoBytes = File.ReadAllBytes(dialog.FileName);
			_photoRemoved = false;
			UpdatePhotoPreview();
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to read photo.", "Photo Error");
		}
	}

	private void RemovePhoto_Click(object? sender, EventArgs e)
	{
		if (_readOnly)
		{
			return;
		}

		_photoRemoved = true;
		_photoBytes = null;
		UpdatePhotoPreview();
	}

	private void UpdatePhotoPreview()
	{
		Image? previous = _photo.Image;
		_photo.Image = null;
		previous?.Dispose();

		if (_photoBytes == null || _photoBytes.Length == 0)
		{
			_removePhoto.Enabled = !_readOnly && !_photoRemoved;
			return;
		}

		try
		{
			using MemoryStream stream = new MemoryStream(_photoBytes);
			_photo.Image = Image.FromStream(stream);
			_removePhoto.Enabled = !_readOnly;
		}
		catch
		{
			_photo.Image = null;
			_removePhoto.Enabled = false;
		}
	}

	private void Barangay_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_suppressLookupEvents)
		{
			return;
		}

		try
		{
			using var conn = OpenConnection();
			int barangayId = GetSelectedLookupId(_barangay) ?? SchemaDefaults.DefaultBarangayId;
			LoadPurokOptions(conn, barangayId, null);
			int? purokId = GetSelectedLookupId(_purok);
			LoadHouseholdOptions(conn, barangayId, purokId, null);
			UpdateAddressPreview();
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load purok list.", "Location");
		}
	}

	private void Purok_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_suppressLookupEvents)
		{
			return;
		}

		try
		{
			using var conn = OpenConnection();
			int barangayId = GetSelectedLookupId(_barangay) ?? SchemaDefaults.DefaultBarangayId;
			int? purokId = GetSelectedLookupId(_purok);
			LoadHouseholdOptions(conn, barangayId, purokId, null);
			UpdateAddressPreview();
		}
		catch (Exception ex)
		{
			ControllerDialogs.Warning(ex, "Unable to load household list.", "Location");
		}
	}

	private void UpdateAddressPreview()
	{
		string barangayText = (_barangay.Text ?? string.Empty).Trim();
		string purokText = (_purok.Text ?? string.Empty).Trim();
		string householdText = (_household.Text ?? string.Empty).Trim();
		List<string> parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(barangayText) && !string.Equals(barangayText, "(None)", StringComparison.OrdinalIgnoreCase))
		{
			parts.Add(barangayText);
		}
		if (!string.IsNullOrWhiteSpace(purokText) && !string.Equals(purokText, "(None)", StringComparison.OrdinalIgnoreCase))
		{
			parts.Add(purokText);
		}
		if (!string.IsNullOrWhiteSpace(householdText) && !string.Equals(householdText, "(None)", StringComparison.OrdinalIgnoreCase))
		{
			parts.Add(householdText);
		}
		_addressPreview.Text = parts.Count > 0 ? string.Join(", ", parts) : "Address info incomplete.";
	}
	private void RelationshipAdd_Click(object? sender, EventArgs e)
	{
		if (_readOnly || !_relationshipTableAvailable)
		{
			return;
		}

		using var dialog = new RelationshipEditorDialog(_residentDirectory);
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		DataRow row = _relationshipTable.NewRow();
		row["relationship_id"] = DBNull.Value;
		row["related_resident_id"] = dialog.RelatedResidentId;
		row["relation_type"] = dialog.RelationType;
		row["notes"] = dialog.Notes;
		row["related_name"] = _residentDirectory.TryGetValue(dialog.RelatedResidentId, out string? name) ? name : $"Resident #{dialog.RelatedResidentId}";
		_relationshipTable.Rows.Add(row);
		ApplyRelationshipFilter();
	}

	private void RelationshipEdit_Click(object? sender, EventArgs e)
	{
		if (_readOnly || !_relationshipTableAvailable || _relationshipsGrid.SelectedRows.Count == 0)
		{
			return;
		}

		if (_relationshipsGrid.SelectedRows[0].DataBoundItem is not DataRowView rowView)
		{
			return;
		}

		int relatedId = Convert.ToInt32(rowView["related_resident_id"]);
		string relationType = Convert.ToString(rowView["relation_type"]) ?? "Other";
		string notes = Convert.ToString(rowView["notes"]) ?? string.Empty;

		using var dialog = new RelationshipEditorDialog(_residentDirectory, relatedId, relationType, notes);
		if (dialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		rowView["related_resident_id"] = dialog.RelatedResidentId;
		rowView["relation_type"] = dialog.RelationType;
		rowView["notes"] = dialog.Notes;
		rowView["related_name"] = _residentDirectory.TryGetValue(dialog.RelatedResidentId, out string? name) ? name : $"Resident #{dialog.RelatedResidentId}";
		ApplyRelationshipFilter();
	}

	private void RelationshipRemove_Click(object? sender, EventArgs e)
	{
		if (_readOnly || !_relationshipTableAvailable || _relationshipsGrid.SelectedRows.Count == 0)
		{
			return;
		}

		if (_relationshipsGrid.SelectedRows[0].DataBoundItem is not DataRowView rowView)
		{
			return;
		}

		rowView.Row.Delete();
		ApplyRelationshipFilter();
	}

	private void Save_Click(object? sender, EventArgs e)
	{
		if (_readOnly)
		{
			return;
		}

		if (!TryValidateInputs(out string message))
		{
			_validationHint.Text = message;
			return;
		}

		try
		{
			using var conn = OpenConnection();
			using var tx = conn.BeginTransaction();

			var beforeLocation = ReadCurrentLocation(conn, tx, _residentId);
			UpdateResidentRecord(conn, tx);

			try
			{
				SaveRelationships(conn, tx);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("ResidentDetailsModal: unable to save relationships.", ex);
			}

			try
			{
				InsertTransferHistoryIfNeeded(conn, tx, beforeLocation);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("ResidentDetailsModal: unable to write transfer history.", ex);
			}

			try
			{
				LogResidentUpdate(conn, tx);
			}
			catch (Exception ex)
			{
				AppLogger.LogWarning("ResidentDetailsModal: unable to write activity log.", ex);
			}

			tx.Commit();
			DialogResult = DialogResult.OK;
			Close();
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to save resident details.", "Resident Details");
		}
	}

	private void LoadResidentRecord(MySqlConnection conn)
	{
		using var cmd = new MySqlCommand("SELECT * FROM resident WHERE resident_id = @id LIMIT 1", conn);
		cmd.Parameters.AddWithValue("@id", _residentId);

		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			throw new InvalidOperationException("Resident not found.");
		}

		HashSet<string> recordColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < reader.FieldCount; i++)
		{
			recordColumns.Add(reader.GetName(i));
		}
		if (_residentColumns.Count == 0)
		{
			foreach (string column in recordColumns)
			{
				_residentColumns.Add(column);
			}
		}

		bool HasRecordColumn(string name) => recordColumns.Contains(name);

		int barangayId = HasRecordColumn("barangay_id") && reader["barangay_id"] != DBNull.Value
			? Convert.ToInt32(reader["barangay_id"])
			: SchemaDefaults.DefaultBarangayId;
		int purokId = HasRecordColumn("purok_id") && reader["purok_id"] != DBNull.Value
			? Convert.ToInt32(reader["purok_id"])
			: SchemaDefaults.DefaultPurokId;
		int? householdId = HasRecordColumn("household_id") && reader["household_id"] != DBNull.Value
			? Convert.ToInt32(reader["household_id"])
			: (int?)null;

		_firstName.Text = HasRecordColumn("first_name") ? (Convert.ToString(reader["first_name"]) ?? string.Empty) : string.Empty;
		_middleName.Text = HasRecordColumn("middle_name") ? (Convert.ToString(reader["middle_name"]) ?? string.Empty) : string.Empty;
		_lastName.Text = HasRecordColumn("last_name") ? (Convert.ToString(reader["last_name"]) ?? string.Empty) : string.Empty;
		_sex.Text = NormalizeSex(HasRecordColumn("sex") ? Convert.ToString(reader["sex"]) : null);
		DateTime birthDateValue = DateTime.Today;
		if (HasRecordColumn("birth_date") && reader["birth_date"] != DBNull.Value)
		{
			if (reader["birth_date"] is DateTime dateValue)
			{
				birthDateValue = dateValue;
			}
			else if (!DateTime.TryParse(Convert.ToString(reader["birth_date"]), out birthDateValue))
			{
				birthDateValue = DateTime.Today;
			}
		}
		if (birthDateValue < _birthDate.MinDate)
		{
			birthDateValue = _birthDate.MinDate;
		}
		if (birthDateValue > _birthDate.MaxDate)
		{
			birthDateValue = _birthDate.MaxDate;
		}
		_birthDate.Value = birthDateValue;
		_civilStatus.Text = NormalizeCivilStatus(HasRecordColumn("civil_status") ? Convert.ToString(reader["civil_status"]) : null);
		_contact.Text = HasRecordColumn("contact_no") ? (Convert.ToString(reader["contact_no"]) ?? string.Empty) : string.Empty;
		_email.Text = HasRecordColumn("email") ? (Convert.ToString(reader["email"]) ?? string.Empty) : string.Empty;
		_status.Text = NormalizeStatus(HasRecordColumn("status") ? Convert.ToString(reader["status"]) : null);

		_birthPlace.Text = HasRecordColumn("birth_place") ? (Convert.ToString(reader["birth_place"]) ?? string.Empty) : string.Empty;
		_citizenship.Text = HasRecordColumn("citizenship") ? (Convert.ToString(reader["citizenship"]) ?? string.Empty) : string.Empty;
		_religion.Text = HasRecordColumn("religion") ? (Convert.ToString(reader["religion"]) ?? string.Empty) : string.Empty;
		_occupation.Text = HasRecordColumn("occupation") ? (Convert.ToString(reader["occupation"]) ?? string.Empty) : string.Empty;
		_employer.Text = HasRecordColumn("employer") ? (Convert.ToString(reader["employer"]) ?? string.Empty) : string.Empty;
		_education.Text = HasRecordColumn("education_level") ? (Convert.ToString(reader["education_level"]) ?? "(None)") : "(None)";

		_isPwd.Checked = HasRecordColumn("is_pwd") && reader["is_pwd"] != DBNull.Value && Convert.ToInt32(reader["is_pwd"]) == 1;
		_pwdIdNo.Text = HasRecordColumn("pwd_id_no") ? (Convert.ToString(reader["pwd_id_no"]) ?? string.Empty) : string.Empty;
		_isSenior.Checked = HasRecordColumn("is_senior") && reader["is_senior"] != DBNull.Value && Convert.ToInt32(reader["is_senior"]) == 1;
		_is4Ps.Checked = HasRecordColumn("is_4ps_beneficiary") && reader["is_4ps_beneficiary"] != DBNull.Value && Convert.ToInt32(reader["is_4ps_beneficiary"]) == 1;
		_isVoter.Checked = HasRecordColumn("is_registered_voter") && reader["is_registered_voter"] != DBNull.Value && Convert.ToInt32(reader["is_registered_voter"]) == 1;
		_voterPrecinctNo.Text = HasRecordColumn("voter_precinct_no") ? (Convert.ToString(reader["voter_precinct_no"]) ?? string.Empty) : string.Empty;
		_pwdIdNo.Enabled = _isPwd.Checked;
		_voterPrecinctNo.Enabled = _isVoter.Checked;

		_photoBytes = HasRecordColumn("photo") && reader["photo"] != DBNull.Value ? (byte[]?)reader["photo"] : null;
		_photoRemoved = false;

		_title.Text = "Resident Details";
		string fullName = string.Join(" ", new[]
		{
			_firstName.Text.Trim(),
			_middleName.Text.Trim(),
			_lastName.Text.Trim()
		}.Where(part => !string.IsNullOrWhiteSpace(part)));
		_subtitle.Text = string.IsNullOrWhiteSpace(fullName) ? $"Resident #{_residentId}" : fullName;
		UpdateStatusBadge(_status.Text);

		_originalPurokId = purokId;
		_originalHouseholdId = householdId;

		try
		{
			LoadLocationLookups(conn, barangayId, purokId, householdId);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("ResidentDetailsModal: unable to load location lookups.", ex);
			BindFallbackLocationLookups(barangayId, purokId, householdId);
		}
		_originalAddress = _addressPreview.Text;
	}

	private void LoadLocationLookups(MySqlConnection conn, int barangayId, int purokId, int? householdId)
	{
		_suppressLookupEvents = true;
		try
		{
			var barangays = LoadLookupItems(conn, "SELECT barangay_id, name FROM barangay ORDER BY name");
			BindCombo(_barangay, barangays, includeNone: false);
			SelectComboById(_barangay, barangayId);
			LoadPurokOptions(conn, barangayId, purokId);
			LoadHouseholdOptions(conn, barangayId, purokId, householdId);
		}
		finally
		{
			_suppressLookupEvents = false;
		}
	}

	private void BindFallbackLocationLookups(int barangayId, int purokId, int? householdId)
	{
		_suppressLookupEvents = true;
		try
		{
			int effectiveBarangayId = barangayId > 0 ? barangayId : SchemaDefaults.DefaultBarangayId;
			int effectivePurokId = purokId > 0 ? purokId : SchemaDefaults.DefaultPurokId;

			BindCombo(_barangay,
				new List<LookupItem> { new LookupItem(effectiveBarangayId, $"Barangay #{effectiveBarangayId}") },
				includeNone: false);
			SelectComboById(_barangay, effectiveBarangayId);

			BindCombo(_purok,
				new List<LookupItem> { new LookupItem(effectivePurokId, $"Purok #{effectivePurokId}") },
				includeNone: false);
			SelectComboById(_purok, effectivePurokId);

			List<LookupItem> households = new List<LookupItem>();
			if (householdId.HasValue && householdId.Value > 0)
			{
				households.Add(new LookupItem(householdId.Value, $"Household #{householdId.Value}"));
			}

			BindCombo(_household, households, includeNone: true);
			SelectComboById(_household, householdId);
		}
		finally
		{
			_suppressLookupEvents = false;
		}
	}

	private void LoadPurokOptions(MySqlConnection conn, int barangayId, int? selectedId)
	{
		var puroks = LoadLookupItems(conn,
			"SELECT purok_id, name FROM purok_sitio WHERE barangay_id=@barangayId ORDER BY name",
			new MySqlParameter("@barangayId", barangayId));
		BindCombo(_purok, puroks, includeNone: false);
		SelectComboById(_purok, selectedId ?? SchemaDefaults.DefaultPurokId);
	}

	private void LoadHouseholdOptions(MySqlConnection conn, int barangayId, int? purokId, int? selectedId)
	{
		const string sql = @"SELECT household_id,
                                    COALESCE(NULLIF(TRIM(CONCAT_WS(' ', house_no, street, subdivision)), ''), CONCAT('Household #', household_id)) AS label
                             FROM household
                             WHERE barangay_id=@barangayId
                               AND (@purokId IS NULL OR purok_id=@purokId)
                             ORDER BY household_id";
		var households = LoadLookupItems(conn, sql,
			new MySqlParameter("@barangayId", barangayId),
			new MySqlParameter("@purokId", (object?)purokId ?? DBNull.Value));
		BindCombo(_household, households, includeNone: true);
		SelectComboById(_household, selectedId);
	}

	private void LoadResidentDirectory(MySqlConnection conn)
	{
		_residentDirectory.Clear();
		using var cmd = new MySqlCommand(
			@"SELECT resident_id,
                     CONCAT(last_name, ', ', first_name,
                            CASE WHEN IFNULL(middle_name, '') = '' THEN '' ELSE CONCAT(' ', middle_name) END) AS full_name
              FROM resident
              WHERE IFNULL(is_deleted,0)=0
                AND resident_id <> @id
              ORDER BY last_name, first_name", conn);
		cmd.Parameters.AddWithValue("@id", _residentId);
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			int id = Convert.ToInt32(reader["resident_id"]);
			string name = Convert.ToString(reader["full_name"]) ?? $"Resident #{id}";
			if (!_residentDirectory.ContainsKey(id))
			{
				_residentDirectory.Add(id, name);
			}
		}
	}

	private void LoadRelationships(MySqlConnection conn)
	{
		_relationshipTable.Clear();
		_relationshipTable.Columns.Clear();
		_relationshipTable.Columns.Add("relationship_id", typeof(int));
		_relationshipTable.Columns.Add("related_resident_id", typeof(int));
		_relationshipTable.Columns.Add("relation_type", typeof(string));
		_relationshipTable.Columns.Add("notes", typeof(string));
		_relationshipTable.Columns.Add("related_name", typeof(string));

		if (!_relationshipTableAvailable)
		{
			_relationshipsHint.Text = "Relationship table is not available in this database.";
			_relationshipsGrid.DataSource = _relationshipTable.DefaultView;
			_relationshipAdd.Enabled = false;
			_relationshipEdit.Enabled = false;
			_relationshipRemove.Enabled = false;
			_relationshipSearch.Enabled = false;
			UpdateRelationshipsEmptyState();
			return;
		}

		_relationshipsHint.Text = "Manage resident relationship records.";
		_relationshipSearch.Enabled = true;

		using var cmd = new MySqlCommand(
			@"SELECT rr.relationship_id,
                     rr.related_resident_id,
                     rr.relation_type,
                     rr.notes,
                     CONCAT(r.last_name, ', ', r.first_name,
                            CASE WHEN IFNULL(r.middle_name,'') = '' THEN '' ELSE CONCAT(' ', r.middle_name) END) AS related_name
              FROM resident_relationship rr
              LEFT JOIN resident r ON r.resident_id = rr.related_resident_id
              WHERE rr.resident_id = @residentId
              ORDER BY rr.relationship_id", conn);
		cmd.Parameters.AddWithValue("@residentId", _residentId);
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			DataRow row = _relationshipTable.NewRow();
			row["relationship_id"] = reader["relationship_id"] == DBNull.Value ? DBNull.Value : reader["relationship_id"];
			row["related_resident_id"] = reader["related_resident_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["related_resident_id"]);
			row["relation_type"] = Convert.ToString(reader["relation_type"]) ?? "Other";
			row["notes"] = Convert.ToString(reader["notes"]) ?? string.Empty;
			row["related_name"] = Convert.ToString(reader["related_name"]) ?? string.Empty;
			_relationshipTable.Rows.Add(row);
		}

		_relationshipsGrid.DataSource = _relationshipTable.DefaultView;
		if (_relationshipsGrid.Columns["relationship_id"] != null)
		{
			_relationshipsGrid.Columns["relationship_id"].Visible = false;
		}
		if (_relationshipsGrid.Columns["related_resident_id"] != null)
		{
			_relationshipsGrid.Columns["related_resident_id"].Visible = false;
		}
		if (_relationshipsGrid.Columns["relation_type"] != null)
		{
			_relationshipsGrid.Columns["relation_type"].HeaderText = "Relation";
			_relationshipsGrid.Columns["relation_type"].FillWeight = 20F;
		}
		if (_relationshipsGrid.Columns["related_name"] != null)
		{
			_relationshipsGrid.Columns["related_name"].HeaderText = "Related Resident";
			_relationshipsGrid.Columns["related_name"].FillWeight = 40F;
		}
		if (_relationshipsGrid.Columns["notes"] != null)
		{
			_relationshipsGrid.Columns["notes"].HeaderText = "Notes";
			_relationshipsGrid.Columns["notes"].FillWeight = 40F;
		}

		ApplyRelationshipFilter();
	}

	private void LoadHistory(MySqlConnection conn)
	{
		DataTable table = new DataTable();
		table.Columns.Add("action_at", typeof(DateTime));
		table.Columns.Add("module", typeof(string));
		table.Columns.Add("action", typeof(string));
		table.Columns.Add("action_by", typeof(string));
		table.Columns.Add("details", typeof(string));

		bool hasActivity = TableExists(conn, "activity_log");
		bool hasTransfers = TableExists(conn, "resident_transfer_history");
		if (!hasActivity && !hasTransfers)
		{
			_historyTable = table;
			_historyGrid.DataSource = _historyTable.DefaultView;
			PopulateHistoryModuleFilter();
			return;
		}

		string sql = @"SELECT h.action_at,
                              h.module,
                              h.action,
                              h.details,
                              h.action_by
                       FROM (";

		List<string> unions = new List<string>();
		if (hasActivity)
		{
			unions.Add(@"SELECT l.action_at,
                                l.module,
                                l.action,
                                l.details,
                                COALESCE(u.username, '-') AS action_by
                         FROM activity_log l
                         LEFT JOIN user_account u ON u.user_id = l.action_by
                         WHERE l.resident_id = @rid");
		}
		if (hasTransfers)
		{
			unions.Add(@"SELECT t.transferred_at AS action_at,
                                'Residents' AS module,
                                'Transfer' AS action,
                                CONCAT('Address moved: ', COALESCE(t.old_address, '-'), ' -> ', COALESCE(t.new_address, '-')) AS details,
                                COALESCE(u.username, '-') AS action_by
                         FROM resident_transfer_history t
                         LEFT JOIN user_account u ON u.user_id = t.transferred_by_user_id
                         WHERE t.resident_id = @rid");
		}

		sql += string.Join(" UNION ALL ", unions);
		sql += @") h ORDER BY h.action_at DESC LIMIT 300";

		using var cmd = new MySqlCommand(sql, conn);
		cmd.Parameters.AddWithValue("@rid", _residentId);
		using var adapter = new MySqlDataAdapter(cmd);
		adapter.Fill(table);

		_historyTable = table;
		_historyGrid.DataSource = _historyTable.DefaultView;
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
			_historyGrid.Columns["action"].FillWeight = 14F;
		}
		if (_historyGrid.Columns["action_by"] != null)
		{
			_historyGrid.Columns["action_by"].HeaderText = "By";
			_historyGrid.Columns["action_by"].FillWeight = 12F;
		}
		if (_historyGrid.Columns["details"] != null)
		{
			_historyGrid.Columns["details"].HeaderText = "Details";
			_historyGrid.Columns["details"].FillWeight = 38F;
		}

		PopulateHistoryModuleFilter();
		ApplyHistoryFilter();
	}

	private void UpdateRelationshipsEmptyState()
	{
		int rowCount = _relationshipTable.DefaultView.Count;
		_relationshipsEmptyState.Visible = rowCount <= 0;
	}

	private void ApplyRelationshipFilter()
	{
		if (_relationshipTable.Columns.Count == 0)
		{
			_relationshipsGrid.DataSource = _relationshipTable.DefaultView;
			UpdateRelationshipsEmptyState();
			return;
		}

		DataView view = _relationshipTable.DefaultView;
		string keyword = EscapeLikeValue(_relationshipSearch.Text.Trim());
		if (string.IsNullOrWhiteSpace(keyword))
		{
			view.RowFilter = string.Empty;
		}
		else
		{
			view.RowFilter = $"related_name LIKE '%{keyword}%' OR relation_type LIKE '%{keyword}%' OR notes LIKE '%{keyword}%'";
		}

		_relationshipsGrid.DataSource = view;
		UpdateRelationshipsEmptyState();
	}

	private void PopulateHistoryModuleFilter()
	{
		string selected = _historyModuleFilter.SelectedItem?.ToString() ?? "All Modules";
		List<string> modules = new List<string> { "All Modules" };
		if (_historyTable.Columns.Contains("module"))
		{
			foreach (DataRow row in _historyTable.Rows)
			{
				string module = Convert.ToString(row["module"]) ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(module) &&
					!modules.Any(item => item.Equals(module, StringComparison.OrdinalIgnoreCase)))
				{
					modules.Add(module);
				}
			}
		}

		_historyModuleFilter.BeginUpdate();
		try
		{
			_historyModuleFilter.Items.Clear();
			_historyModuleFilter.Items.AddRange(modules.Cast<object>().ToArray());
			int index = _historyModuleFilter.FindStringExact(selected);
			_historyModuleFilter.SelectedIndex = index >= 0 ? index : 0;
		}
		finally
		{
			_historyModuleFilter.EndUpdate();
		}
	}

	private void ApplyHistoryFilter()
	{
		if (_historyTable.Columns.Count == 0)
		{
			_historyGrid.DataSource = _historyTable.DefaultView;
			return;
		}

		DataView view = _historyTable.DefaultView;
		List<string> filters = new List<string>();
		string selectedModule = _historyModuleFilter.SelectedItem?.ToString() ?? "All Modules";
		if (!string.IsNullOrWhiteSpace(selectedModule) &&
			!selectedModule.Equals("All Modules", StringComparison.OrdinalIgnoreCase))
		{
			filters.Add($"module = '{EscapeLikeValue(selectedModule)}'");
		}

		string keyword = EscapeLikeValue(_historySearch.Text.Trim());
		if (!string.IsNullOrWhiteSpace(keyword))
		{
			filters.Add(
				$"(action LIKE '%{keyword}%' OR details LIKE '%{keyword}%' OR action_by LIKE '%{keyword}%' OR module LIKE '%{keyword}%')");
		}

		view.RowFilter = filters.Count == 0 ? string.Empty : string.Join(" AND ", filters);
		_historyGrid.DataSource = view;
	}

	private static string EscapeLikeValue(string value)
	{
		return value
			.Replace("[", "[[]", StringComparison.Ordinal)
			.Replace("%", "[%]", StringComparison.Ordinal)
			.Replace("*", "[*]", StringComparison.Ordinal)
			.Replace("'", "''", StringComparison.Ordinal);
	}

	private void UpdateResidentRecord(MySqlConnection conn, MySqlTransaction tx)
	{
		int barangayId = GetSelectedLookupId(_barangay) ?? SchemaDefaults.DefaultBarangayId;
		int purokId = GetSelectedLookupId(_purok) ?? SchemaDefaults.DefaultPurokId;
		int? householdId = GetSelectedLookupId(_household);

		List<string> assignments = new List<string>();
		Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		void AddAssignment(string column, string parameter, object value)
		{
			if (!HasResidentColumn(column))
			{
				return;
			}

			assignments.Add($"{column} = {parameter}");
			values[parameter] = value;
		}

		AddAssignment("barangay_id", "@barangayId", barangayId);
		AddAssignment("purok_id", "@purokId", purokId);
		AddAssignment("household_id", "@householdId", householdId.HasValue ? householdId.Value : (object)DBNull.Value);
		AddAssignment("first_name", "@firstName", _firstName.Text.Trim());
		AddAssignment("middle_name", "@middleName", _middleName.Text.Trim());
		AddAssignment("last_name", "@lastName", _lastName.Text.Trim());
		AddAssignment("sex", "@sex", NormalizeSex(_sex.Text));
		AddAssignment("birth_date", "@birthDate", _birthDate.Value.Date);
		AddAssignment("civil_status", "@civilStatus", NormalizeCivilStatus(_civilStatus.Text));
		AddAssignment("contact_no", "@contactNo", _contact.Text.Trim());
		AddAssignment("email", "@email", _email.Text.Trim());
		AddAssignment("status", "@status", NormalizeStatus(_status.Text));
		AddAssignment("birth_place", "@birthPlace", NullIfWhiteSpace(_birthPlace.Text));
		AddAssignment("citizenship", "@citizenship", NullIfWhiteSpace(_citizenship.Text));
		AddAssignment("religion", "@religion", NullIfWhiteSpace(_religion.Text));
		AddAssignment("occupation", "@occupation", NullIfWhiteSpace(_occupation.Text));
		AddAssignment("employer", "@employer", NullIfWhiteSpace(_employer.Text));
		AddAssignment("education_level", "@educationLevel", (_education.Text ?? string.Empty).Trim() == "(None)" ? DBNull.Value : NullIfWhiteSpace(_education.Text));
		AddAssignment("is_pwd", "@isPwd", _isPwd.Checked ? 1 : 0);
		AddAssignment("pwd_id_no", "@pwdIdNo", _isPwd.Checked ? NullIfWhiteSpace(_pwdIdNo.Text) : DBNull.Value);
		AddAssignment("is_senior", "@isSenior", _isSenior.Checked ? 1 : 0);
		AddAssignment("is_4ps_beneficiary", "@is4Ps", _is4Ps.Checked ? 1 : 0);
		AddAssignment("is_registered_voter", "@isRegisteredVoter", _isVoter.Checked ? 1 : 0);
		AddAssignment("voter_precinct_no", "@voterPrecinctNo", _isVoter.Checked ? NullIfWhiteSpace(_voterPrecinctNo.Text) : DBNull.Value);
		AddAssignment("photo", "@photo", _photoRemoved ? DBNull.Value : (object?)_photoBytes ?? DBNull.Value);

		if (assignments.Count == 0)
		{
			throw new InvalidOperationException("No writable resident columns were found.");
		}

		string sql = $"UPDATE resident SET {string.Join(", ", assignments)} WHERE resident_id = @residentId";
		using var cmd = new MySqlCommand(sql, conn, tx);
		foreach (KeyValuePair<string, object> entry in values)
		{
			cmd.Parameters.AddWithValue(entry.Key, entry.Value);
		}

		cmd.Parameters.AddWithValue("@residentId", _residentId);
		cmd.ExecuteNonQuery();
	}

	private void SaveRelationships(MySqlConnection conn, MySqlTransaction tx)
	{
		if (!_relationshipTableAvailable)
		{
			return;
		}

		using (var delete = new MySqlCommand("DELETE FROM resident_relationship WHERE resident_id=@residentId", conn, tx))
		{
			delete.Parameters.AddWithValue("@residentId", _residentId);
			delete.ExecuteNonQuery();
		}

		foreach (DataRow row in _relationshipTable.Rows)
		{
			if (row.RowState == DataRowState.Deleted)
			{
				continue;
			}

			if (row["related_resident_id"] == DBNull.Value)
			{
				continue;
			}

			int relatedResidentId = Convert.ToInt32(row["related_resident_id"]);
			if (relatedResidentId <= 0 || relatedResidentId == _residentId)
			{
				continue;
			}

			string relationType = Convert.ToString(row["relation_type"]) ?? "Other";
			string notes = Convert.ToString(row["notes"]) ?? string.Empty;

			using var insert = new MySqlCommand(
				@"INSERT INTO resident_relationship
                  (resident_id, related_resident_id, relation_type, notes)
                  VALUES
                  (@residentId, @relatedResidentId, @relationType, @notes)", conn, tx);
			insert.Parameters.AddWithValue("@residentId", _residentId);
			insert.Parameters.AddWithValue("@relatedResidentId", relatedResidentId);
			insert.Parameters.AddWithValue("@relationType", relationType);
			insert.Parameters.AddWithValue("@notes", NullIfWhiteSpace(notes));
			insert.ExecuteNonQuery();
		}
	}

	private void InsertTransferHistoryIfNeeded(MySqlConnection conn, MySqlTransaction tx, ResidentLocationSnapshot beforeLocation)
	{
		if (!TableExists(conn, "resident_transfer_history"))
		{
			return;
		}

		int newPurokId = GetSelectedLookupId(_purok) ?? SchemaDefaults.DefaultPurokId;
		int? newHouseholdId = GetSelectedLookupId(_household);
		if (beforeLocation.PurokId == newPurokId && beforeLocation.HouseholdId == newHouseholdId)
		{
			return;
		}

		string newAddress = _addressPreview.Text.Trim();
		using var cmd = new MySqlCommand(
			@"INSERT INTO resident_transfer_history
                (resident_id, old_purok_id, old_household_id, old_address,
                 new_purok_id, new_household_id, new_address, transfer_reason,
                 transferred_by_user_id, transferred_at)
              VALUES
                (@residentId, @oldPurokId, @oldHouseholdId, @oldAddress,
                 @newPurokId, @newHouseholdId, @newAddress, @reason,
                 @userId, NOW())", conn, tx);
		cmd.Parameters.AddWithValue("@residentId", _residentId);
		cmd.Parameters.AddWithValue("@oldPurokId", beforeLocation.PurokId.HasValue ? beforeLocation.PurokId.Value : (object)DBNull.Value);
		cmd.Parameters.AddWithValue("@oldHouseholdId", beforeLocation.HouseholdId.HasValue ? beforeLocation.HouseholdId.Value : (object)DBNull.Value);
		cmd.Parameters.AddWithValue("@oldAddress", string.IsNullOrWhiteSpace(beforeLocation.Address) ? DBNull.Value : beforeLocation.Address);
		cmd.Parameters.AddWithValue("@newPurokId", newPurokId);
		cmd.Parameters.AddWithValue("@newHouseholdId", newHouseholdId.HasValue ? newHouseholdId.Value : (object)DBNull.Value);
		cmd.Parameters.AddWithValue("@newAddress", string.IsNullOrWhiteSpace(newAddress) ? DBNull.Value : newAddress);
		cmd.Parameters.AddWithValue("@reason", "Profile location updated");
		cmd.Parameters.AddWithValue("@userId", UserSession.UserId > 0 ? UserSession.UserId : (object)DBNull.Value);
		cmd.ExecuteNonQuery();
	}

	private ResidentLocationSnapshot ReadCurrentLocation(MySqlConnection conn, MySqlTransaction tx, int residentId)
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
              WHERE r.resident_id = @residentId
              LIMIT 1
              FOR UPDATE", conn, tx);
		cmd.Parameters.AddWithValue("@residentId", residentId);
		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
		{
			return new ResidentLocationSnapshot();
		}

		int? purokId = reader["purok_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["purok_id"]);
		int? householdId = reader["household_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["household_id"]);
		string purokName = Convert.ToString(reader["purok_name"]) ?? string.Empty;
		string houseNo = Convert.ToString(reader["house_no"]) ?? string.Empty;
		string street = Convert.ToString(reader["street"]) ?? string.Empty;
		string subdivision = Convert.ToString(reader["subdivision"]) ?? string.Empty;

		string address = string.Join(", ", new[]
		{
			string.Join(" ", new[] { houseNo, street, subdivision }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim(),
			purokName.Trim()
		}.Where(part => !string.IsNullOrWhiteSpace(part)));

		return new ResidentLocationSnapshot
		{
			PurokId = purokId,
			HouseholdId = householdId,
			Address = address
		};
	}

	private void LogResidentUpdate(MySqlConnection conn, MySqlTransaction tx)
	{
		if (!TableExists(conn, "activity_log"))
		{
			return;
		}

		using var cmd = new MySqlCommand(
			@"INSERT INTO activity_log
              (resident_id, module, action, details, action_by)
              VALUES
              (@residentId, 'Residents', 'Updated', 'Profile updated via Resident Details modal', @actionBy)", conn, tx);
		cmd.Parameters.AddWithValue("@residentId", _residentId);
		cmd.Parameters.AddWithValue("@actionBy", UserSession.UserId > 0 ? UserSession.UserId : (object)DBNull.Value);
		cmd.ExecuteNonQuery();
	}

	private void UpdateStatusBadge(string status)
	{
		string text = NormalizeStatus(status);
		switch (text)
		{
			case "ACTIVE":
				_statusBadge.Text = "ACTIVE";
				_statusBadge.BackColor = Color.FromArgb(210, 245, 220);
				_statusBadge.ForeColor = Color.FromArgb(0, 100, 40);
				break;
			case "DECEASED":
				_statusBadge.Text = "DECEASED";
				_statusBadge.BackColor = Color.FromArgb(254, 226, 226);
				_statusBadge.ForeColor = Color.FromArgb(153, 27, 27);
				break;
			default:
				_statusBadge.Text = "MOVED OUT";
				_statusBadge.BackColor = Color.FromArgb(235, 235, 235);
				_statusBadge.ForeColor = UiTheme.Slate700;
				break;
		}
	}
	private static MySqlConnection OpenConnection()
	{
		MySqlConnection conn = DBConnection.GetConnection();
		conn.Open();
		try
		{
			SchemaBootstrap.EnsureCoreDefaults(conn);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("ResidentDetailsModal: skipped core defaults bootstrap.", ex);
		}
		return conn;
	}

	private static List<LookupItem> LoadLookupItems(MySqlConnection conn, string sql, params MySqlParameter[] parameters)
	{
		List<LookupItem> items = new List<LookupItem>();
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
		List<LookupItem> source = includeNone
			? new List<LookupItem> { new LookupItem(0, "(None)") }
			: new List<LookupItem>();
		source.AddRange(items);

		comboBox.DataSource = null;
		comboBox.DisplayMember = nameof(LookupItem.Name);
		comboBox.ValueMember = nameof(LookupItem.Id);
		comboBox.DataSource = source;
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

	private static string NormalizeSex(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "M";
		}

		string text = value.Trim();
		if (text.Equals("F", StringComparison.OrdinalIgnoreCase) || text.Equals("Female", StringComparison.OrdinalIgnoreCase))
		{
			return "F";
		}

		return "M";
	}

	private static string NormalizeCivilStatus(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Single";
		}

		string text = value.Trim();
		if (text.Equals("Married", StringComparison.OrdinalIgnoreCase))
		{
			return "Married";
		}
		if (text.Equals("Widowed", StringComparison.OrdinalIgnoreCase))
		{
			return "Widowed";
		}
		if (text.Equals("Separated", StringComparison.OrdinalIgnoreCase))
		{
			return "Separated";
		}

		return "Single";
	}

	private static string NormalizeStatus(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "ACTIVE";
		}

		string text = value.Trim();
		if (text.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) || text.Equals("Active", StringComparison.OrdinalIgnoreCase))
		{
			return "ACTIVE";
		}
		if (text.Equals("DECEASED", StringComparison.OrdinalIgnoreCase) || text.Equals("Deceased", StringComparison.OrdinalIgnoreCase))
		{
			return "DECEASED";
		}
		return "MOVED_OUT";
	}

	private static object NullIfWhiteSpace(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
	}

	private static bool EnsureResidentRelationshipSchema(MySqlConnection conn)
	{
		if (TableExists(conn, "resident_relationship"))
		{
			return true;
		}

		if (!TableExists(conn, "resident"))
		{
			return false;
		}

		using var cmd = new MySqlCommand(
			@"CREATE TABLE resident_relationship (
                relationship_id INT AUTO_INCREMENT PRIMARY KEY,
                resident_id INT NOT NULL,
                related_resident_id INT NOT NULL,
                relation_type ENUM('Parent','Child','Spouse','Guardian','Sibling','Other') NOT NULL DEFAULT 'Other',
                notes VARCHAR(255) NULL,
                INDEX idx_relationship_resident (resident_id),
                INDEX idx_relationship_related (related_resident_id),
                FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE CASCADE,
                FOREIGN KEY (related_resident_id) REFERENCES resident(resident_id) ON DELETE CASCADE
              )", conn);
		cmd.ExecuteNonQuery();
		return true;
	}

	private static void EnsureTransferHistorySchema(MySqlConnection conn)
	{
		if (TableExists(conn, "resident_transfer_history"))
		{
			return;
		}

		using var cmd = new MySqlCommand(
			@"CREATE TABLE resident_transfer_history (
                transfer_id BIGINT AUTO_INCREMENT PRIMARY KEY,
                resident_id INT NOT NULL,
                old_purok_id INT NULL,
                old_household_id INT NULL,
                old_address VARCHAR(255) NULL,
                new_purok_id INT NULL,
                new_household_id INT NULL,
                new_address VARCHAR(255) NULL,
                transfer_reason VARCHAR(255) NULL,
                transferred_by_user_id INT NULL,
                transferred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_transfer_history_resident (resident_id, transferred_at)
              )", conn);
		cmd.ExecuteNonQuery();
	}

	private static void EnsureActivityLogSchema(MySqlConnection conn)
	{
		if (TableExists(conn, "activity_log"))
		{
			return;
		}

		using var cmd = new MySqlCommand(
			@"CREATE TABLE activity_log (
                log_id INT AUTO_INCREMENT PRIMARY KEY,
                resident_id INT NOT NULL,
                module VARCHAR(40) NOT NULL,
                action VARCHAR(50) NOT NULL,
                details VARCHAR(255) NULL,
                action_by INT NULL,
                action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_activity_resident (resident_id),
                INDEX idx_activity_module (module)
              )", conn);
		cmd.ExecuteNonQuery();
	}

	private static void EnsureResidentProfileColumns(MySqlConnection conn)
	{
		AddColumnIfMissing(conn, "resident", "birth_place", "VARCHAR(150) NULL");
		AddColumnIfMissing(conn, "resident", "citizenship", "VARCHAR(100) NULL");
		AddColumnIfMissing(conn, "resident", "religion", "VARCHAR(100) NULL");
		AddColumnIfMissing(conn, "resident", "email", "VARCHAR(150) NULL");
		AddColumnIfMissing(conn, "resident", "occupation", "VARCHAR(150) NULL");
		AddColumnIfMissing(conn, "resident", "employer", "VARCHAR(150) NULL");
		AddColumnIfMissing(conn, "resident", "education_level", "VARCHAR(100) NULL");
		AddColumnIfMissing(conn, "resident", "is_pwd", "TINYINT(1) NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "pwd_id_no", "VARCHAR(100) NULL");
		AddColumnIfMissing(conn, "resident", "is_senior", "TINYINT(1) NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_4ps_beneficiary", "TINYINT(1) NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "is_registered_voter", "TINYINT(1) NOT NULL DEFAULT 0");
		AddColumnIfMissing(conn, "resident", "voter_precinct_no", "VARCHAR(50) NULL");
		AddColumnIfMissing(conn, "resident", "photo", "LONGBLOB NULL");
	}

	private static void AddColumnIfMissing(MySqlConnection conn, string table, string column, string definition)
	{
		if (ColumnExists(conn, table, column))
		{
			return;
		}

		using var cmd = new MySqlCommand($"ALTER TABLE {table} ADD COLUMN {column} {definition};", conn);
		cmd.ExecuteNonQuery();
	}

	private static bool ColumnExists(MySqlConnection conn, string table, string column)
	{
		using var cmd = new MySqlCommand(
			@"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table
                AND COLUMN_NAME = @column", conn);
		cmd.Parameters.AddWithValue("@table", table);
		cmd.Parameters.AddWithValue("@column", column);
		return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
	}

	private static bool TableExists(MySqlConnection conn, string table)
	{
		using var cmd = new MySqlCommand(
			@"SELECT COUNT(*)
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @table", conn);
		cmd.Parameters.AddWithValue("@table", table);
		return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
	}

	private sealed class ResidentLocationSnapshot
	{
		public int? PurokId { get; init; }
		public int? HouseholdId { get; init; }
		public string Address { get; init; } = string.Empty;
	}

	private sealed class LookupItem
	{
		public LookupItem(int id, string name)
		{
			Id = id;
			Name = name;
		}

		public int Id { get; }
		public string Name { get; }
		public override string ToString() => Name;
	}

	private sealed class RelationshipEditorDialog : Form
	{
		private static readonly string[] RelationTypes = { "Parent", "Child", "Spouse", "Guardian", "Sibling", "Other" };

		private readonly ComboBox _resident = new ComboBox();
		private readonly ComboBox _relationType = new ComboBox();
		private readonly TextBox _notes = new TextBox();
		private readonly Button _save = new Button();
		private readonly Button _cancel = new Button();

		public int RelatedResidentId => _resident.SelectedItem is LookupItem item ? item.Id : 0;
		public string RelationType => _relationType.SelectedItem?.ToString() ?? "Other";
		public string Notes => _notes.Text.Trim();

		public RelationshipEditorDialog(
			Dictionary<int, string> residentOptions,
			int? selectedResidentId = null,
			string? relationType = null,
			string? notes = null)
		{
			Text = "Relationship";
			StartPosition = FormStartPosition.CenterParent;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			ClientSize = new Size(520, 220);
			Font = UiTheme.BodyFont;

			TableLayoutPanel root = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(16),
				ColumnCount = 2,
				RowCount = 4
			};
			root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
			root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			_relationType.DropDownStyle = ComboBoxStyle.DropDownList;
			_relationType.Items.AddRange(RelationTypes);

			_resident.DropDownStyle = ComboBoxStyle.DropDownList;
			List<LookupItem> options = residentOptions
				.OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
				.Select(pair => new LookupItem(pair.Key, pair.Value))
				.ToList();
			_resident.DataSource = options;
			_resident.DisplayMember = nameof(LookupItem.Name);
			_resident.ValueMember = nameof(LookupItem.Id);

			_notes.MaxLength = 255;

			UiTheme.StyleComboBoxes(_resident, _relationType);
			UiTheme.StyleTextBox(_notes);
			UiTheme.StylePrimaryButton(_save);
			UiTheme.StyleSecondaryButton(_cancel);

			root.Controls.Add(CreateDialogLabel("Related Resident"), 0, 0);
			root.Controls.Add(_resident, 1, 0);
			root.Controls.Add(CreateDialogLabel("Relation Type"), 0, 1);
			root.Controls.Add(_relationType, 1, 1);
			root.Controls.Add(CreateDialogLabel("Notes"), 0, 2);
			root.Controls.Add(_notes, 1, 2);

			FlowLayoutPanel actions = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.RightToLeft,
				WrapContents = false,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink
			};
			_save.Text = "Save";
			_cancel.Text = "Cancel";
			_save.Size = new Size(100, UiTheme.StandardButtonHeight);
			_cancel.Size = new Size(100, UiTheme.StandardButtonHeight);
			_cancel.Margin = new Padding(0, 0, 8, 0);
			_save.Click += Save_Click;
			_cancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
			actions.Controls.Add(_save);
			actions.Controls.Add(_cancel);
			root.Controls.Add(actions, 1, 3);

			Controls.Add(root);

			if (selectedResidentId.HasValue)
			{
				_resident.SelectedValue = selectedResidentId.Value;
			}
			if (!string.IsNullOrWhiteSpace(relationType))
			{
				_relationType.SelectedItem = RelationTypes.FirstOrDefault(item => item.Equals(relationType, StringComparison.OrdinalIgnoreCase)) ?? "Other";
			}
			else
			{
				_relationType.SelectedItem = "Other";
			}
			_notes.Text = notes ?? string.Empty;

			AcceptButton = _save;
			CancelButton = _cancel;
		}

		private void Save_Click(object? sender, EventArgs e)
		{
			if (RelatedResidentId <= 0)
			{
				ControllerDialogs.Warning("Select a related resident.");
				return;
			}

			DialogResult = DialogResult.OK;
		}

		private static Label CreateDialogLabel(string text)
		{
			return new Label
			{
				Text = text,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(0, 3, 8, 3),
				Font = UiTheme.LabelFont,
				ForeColor = UiTheme.Slate700
			};
		}
	}

	private void TryEnsureSchema(Action ensureAction, string label)
	{
		try
		{
			ensureAction();
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning($"ResidentDetailsModal: skipped {label}.", ex);
		}
	}

	private bool TryEnsureRelationshipSchema(MySqlConnection conn)
	{
		try
		{
			return EnsureResidentRelationshipSchema(conn);
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("ResidentDetailsModal: skipped resident_relationship schema.", ex);
			return TableExists(conn, "resident_relationship");
		}
	}

	private void RefreshResidentColumnMetadata(MySqlConnection conn)
	{
		_residentColumns.Clear();
		try
		{
			using var cmd = new MySqlCommand(
				@"SELECT COLUMN_NAME
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'resident';", conn);
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				string name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
				if (!string.IsNullOrWhiteSpace(name))
				{
					_residentColumns.Add(name);
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("ResidentDetailsModal: could not load resident column metadata.", ex);
		}
	}

	private bool HasResidentColumn(string columnName)
	{
		return _residentColumns.Contains(columnName);
	}
}
