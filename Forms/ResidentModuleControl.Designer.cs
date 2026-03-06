namespace baranggaysystem1
{
    partial class ResidentModuleControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel _listPanel;
        private System.Windows.Forms.TableLayoutPanel tableLeftRoot;
        private System.Windows.Forms.Panel panelLeftPagerHost;
        private System.Windows.Forms.Panel panelRightRoot;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.TableLayoutPanel tableBody;
        private System.Windows.Forms.Panel panelProfileDetails;
        private System.Windows.Forms.Panel panelTabBarHost;
        private System.Windows.Forms.FlowLayoutPanel _actionsPanel;
        private System.Windows.Forms.FlowLayoutPanel _searchPanel;
        private System.Windows.Forms.TextBox _searchBox;
        private System.Windows.Forms.Button _searchClear;
        private System.Windows.Forms.DataGridView dgvResidents;
        private System.Windows.Forms.Panel datapanel;

        private System.Windows.Forms.Panel _residentHeader;
        private System.Windows.Forms.Label _residentHeaderName;
        private System.Windows.Forms.Label _residentHeaderMeta;
        private System.Windows.Forms.Label _residentHeaderStatus;

        private System.Windows.Forms.TabControl _residentTabs;
        private System.Windows.Forms.TabPage _tabProfile;
        private System.Windows.Forms.TabPage _tabBlotter;
        private System.Windows.Forms.TabPage _tabCertificates;
        private System.Windows.Forms.TabPage _tabHistory;

        private System.Windows.Forms.Panel profileContainer;
        private System.Windows.Forms.Label profileHeader;
        private System.Windows.Forms.TableLayoutPanel profileBody;
        private System.Windows.Forms.TableLayoutPanel profileInfoTable;
        private System.Windows.Forms.FlowLayoutPanel profilePhotoPanel;
        private System.Windows.Forms.FlowLayoutPanel profilePhotoButtons;
        private System.Windows.Forms.FlowLayoutPanel profileActions;
        private System.Windows.Forms.Label lblFirstName;
        private System.Windows.Forms.Label lblMiddleName;
        private System.Windows.Forms.Label lblLastName;
        private System.Windows.Forms.Label lblGender;
        private System.Windows.Forms.Label lblBirthDate;
        private System.Windows.Forms.Label lblCivilStatus;
        private System.Windows.Forms.Label lblContact;
        private System.Windows.Forms.Label lblStatus;

        private System.Windows.Forms.Label _detailMessage;
        private System.Windows.Forms.TextBox _editFirstName;
        private System.Windows.Forms.TextBox _editMiddleName;
        private System.Windows.Forms.TextBox _editLastName;
        private System.Windows.Forms.TextBox _editGender;
        private System.Windows.Forms.DateTimePicker _editDob;
        private System.Windows.Forms.TextBox _editCivil;
        private System.Windows.Forms.TextBox _editContact;
        private System.Windows.Forms.TextBox _editStatus;
        private System.Windows.Forms.PictureBox _residentPhoto;
        private System.Windows.Forms.Label _residentPhotoCaption;
        private System.Windows.Forms.Button _residentPhotoUpload;
        private System.Windows.Forms.Button _residentPhotoRemove;
        private System.Windows.Forms.Button _residentQuickEdit;

        private System.Windows.Forms.Panel blotterContainer;
        private System.Windows.Forms.Label blotterTitle;
        private System.Windows.Forms.FlowLayoutPanel blotterActions;
        private System.Windows.Forms.Panel blotterGridPanel;

        private System.Windows.Forms.Panel certContainer;
        private System.Windows.Forms.Label certTitle;
        private System.Windows.Forms.FlowLayoutPanel certActions;
        private System.Windows.Forms.FlowLayoutPanel certFilters;
        private System.Windows.Forms.FlowLayoutPanel certSummary;
        private System.Windows.Forms.TableLayoutPanel certBody;
        private System.Windows.Forms.Panel certGridPanel;
        private System.Windows.Forms.Panel certDetailsPanel;
        private System.Windows.Forms.Label certDetailsHeader;
        private System.Windows.Forms.Label certDataHeader;
        private System.Windows.Forms.TableLayoutPanel certSummaryTable;
        private System.Windows.Forms.TableLayoutPanel certDetailTable;
        private System.Windows.Forms.Label _certFilterFromLabel;
        private System.Windows.Forms.Label _certFilterToLabel;

        private System.Windows.Forms.Panel historyContainer;
        private System.Windows.Forms.Label historyTitle;
        private System.Windows.Forms.FlowLayoutPanel historyFilters;
        private System.Windows.Forms.FlowLayoutPanel historySummaryPanel;
        private System.Windows.Forms.Panel historyBody;
        private System.Windows.Forms.Label _historyFilterFromLabel;
        private System.Windows.Forms.Label _historyFilterToLabel;
        private System.Windows.Forms.Label _historyFilterQuickLabel;
        private System.Windows.Forms.Button _historyQuickToday;
        private System.Windows.Forms.Button _historyQuickWeek;
        private System.Windows.Forms.Button _historyQuickMonth;
        private System.Windows.Forms.Button _historyExport;
        private System.Windows.Forms.SplitContainer historySplit;
        private System.Windows.Forms.Panel historyListPanel;
        private System.Windows.Forms.Panel historyDetailPanel;
        private System.Windows.Forms.Label historyDetailTitle;
        private System.Windows.Forms.TableLayoutPanel historyDetailTable;
        private System.Windows.Forms.Label historyDetailDateLabel;
        private System.Windows.Forms.Label historyDetailDateValue;
        private System.Windows.Forms.Label historyDetailModuleLabel;
        private System.Windows.Forms.Label historyDetailModuleValue;
        private System.Windows.Forms.Label historyDetailActionLabel;
        private System.Windows.Forms.Label historyDetailActionValue;
        private System.Windows.Forms.Label historyDetailByLabel;
        private System.Windows.Forms.Label historyDetailByValue;
        private System.Windows.Forms.Label historyDetailDetailsLabel;
        private System.Windows.Forms.TextBox historyDetailDetails;
        private System.Windows.Forms.Label historyDetailEmpty;
        private System.Windows.Forms.Panel historySummaryCardTotal;
        private System.Windows.Forms.Label historySummaryTotalValue;
        private System.Windows.Forms.Label historySummaryTotalLabel;
        private System.Windows.Forms.Panel historySummaryCardResidents;
        private System.Windows.Forms.Label historySummaryResidentsValue;
        private System.Windows.Forms.Label historySummaryResidentsLabel;
        private System.Windows.Forms.Panel historySummaryCardBlotter;
        private System.Windows.Forms.Label historySummaryBlotterValue;
        private System.Windows.Forms.Label historySummaryBlotterLabel;
        private System.Windows.Forms.Panel historySummaryCardCertificates;
        private System.Windows.Forms.Label historySummaryCertificatesValue;
        private System.Windows.Forms.Label historySummaryCertificatesLabel;

        private System.Windows.Forms.Button add;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            contentPanel = new Panel();
            splitMain = new SplitContainer();
            tableLeftRoot = new TableLayoutPanel();
            panelLeftPagerHost = new Panel();
            panelRightRoot = new Panel();
            panelHeader = new Panel();
            tableBody = new TableLayoutPanel();
            panelProfileDetails = new Panel();
            panelTabBarHost = new Panel();
            datapanel = new Panel();
            _residentTabs = new TabControl();
            _tabProfile = new TabPage();
            profileContainer = new Panel();
            profileActions = new FlowLayoutPanel();
            add = new Button();
            button3 = new Button();
            _residentQuickEdit = new Button();
            profileBody = new TableLayoutPanel();
            profileInfoTable = new TableLayoutPanel();
            lblFirstName = new Label();
            _editFirstName = new TextBox();
            lblMiddleName = new Label();
            _editMiddleName = new TextBox();
            lblLastName = new Label();
            _editLastName = new TextBox();
            lblGender = new Label();
            _editGender = new TextBox();
            lblBirthDate = new Label();
            _editDob = new DateTimePicker();
            lblCivilStatus = new Label();
            _editCivil = new TextBox();
            lblContact = new Label();
            _editContact = new TextBox();
            lblStatus = new Label();
            _editStatus = new TextBox();
            profilePhotoPanel = new FlowLayoutPanel();
            _residentPhotoCaption = new Label();
            _residentPhoto = new PictureBox();
            profilePhotoButtons = new FlowLayoutPanel();
            _residentPhotoUpload = new Button();
            _residentPhotoRemove = new Button();
            _detailMessage = new Label();
            profileHeader = new Label();
            _tabBlotter = new TabPage();
            blotterContainer = new Panel();
            blotterGridPanel = new Panel();
            blotterActions = new FlowLayoutPanel();
            blotterTitle = new Label();
            _tabCertificates = new TabPage();
            certContainer = new Panel();
            certBody = new TableLayoutPanel();
            certGridPanel = new Panel();
            certDetailsPanel = new Panel();
            certSummary = new FlowLayoutPanel();
            certFilters = new FlowLayoutPanel();
            _certFilterFromLabel = new Label();
            _certFilterToLabel = new Label();
            certActions = new FlowLayoutPanel();
            certTitle = new Label();
            _tabHistory = new TabPage();
            historyContainer = new Panel();
            historyBody = new Panel();
            historySummaryPanel = new FlowLayoutPanel();
            historyFilters = new FlowLayoutPanel();
            _historySearchBox = new TextBox();
            _historyFilterModule = new ComboBox();
            _historyFilterFrom = new DateTimePicker();
            _historyFilterTo = new DateTimePicker();
            _historyFilterClear = new Button();
            _historyFilterFromLabel = new Label();
            _historyFilterToLabel = new Label();
            _historyFilterQuickLabel = new Label();
            _historyQuickToday = new Button();
            _historyQuickWeek = new Button();
            _historyQuickMonth = new Button();
            _historyExport = new Button();
            historySplit = new SplitContainer();
            historyListPanel = new Panel();
            historyDetailPanel = new Panel();
            historyDetailTitle = new Label();
            historyDetailTable = new TableLayoutPanel();
            historyDetailDateLabel = new Label();
            historyDetailDateValue = new Label();
            historyDetailModuleLabel = new Label();
            historyDetailModuleValue = new Label();
            historyDetailActionLabel = new Label();
            historyDetailActionValue = new Label();
            historyDetailByLabel = new Label();
            historyDetailByValue = new Label();
            historyDetailDetailsLabel = new Label();
            historyDetailDetails = new TextBox();
            historyDetailEmpty = new Label();
            historySummaryCardTotal = new Panel();
            historySummaryTotalValue = new Label();
            historySummaryTotalLabel = new Label();
            historySummaryCardResidents = new Panel();
            historySummaryResidentsValue = new Label();
            historySummaryResidentsLabel = new Label();
            historySummaryCardBlotter = new Panel();
            historySummaryBlotterValue = new Label();
            historySummaryBlotterLabel = new Label();
            historySummaryCardCertificates = new Panel();
            historySummaryCertificatesValue = new Label();
            historySummaryCertificatesLabel = new Label();
            historyTitle = new Label();
            _residentHeader = new Panel();
            _residentHeaderName = new Label();
            _residentHeaderStatus = new Label();
            _residentHeaderMeta = new Label();
            _listPanel = new Panel();
            dgvResidents = new DataGridView();
            _searchPanel = new FlowLayoutPanel();
            _searchBox = new TextBox();
            _searchClear = new Button();
            _actionsPanel = new FlowLayoutPanel();
            button1 = new Button();
            certDetailsHeader = new Label();
            certDataHeader = new Label();
            certSummaryTable = new TableLayoutPanel();
            certDetailTable = new TableLayoutPanel();
            button2 = new Button();
            contentPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            tableLeftRoot.SuspendLayout();
            panelLeftPagerHost.SuspendLayout();
            panelRightRoot.SuspendLayout();
            panelHeader.SuspendLayout();
            tableBody.SuspendLayout();
            panelProfileDetails.SuspendLayout();
            datapanel.SuspendLayout();
            _residentTabs.SuspendLayout();
            _tabProfile.SuspendLayout();
            profileContainer.SuspendLayout();
            profileActions.SuspendLayout();
            profileBody.SuspendLayout();
            profileInfoTable.SuspendLayout();
            profilePhotoPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_residentPhoto).BeginInit();
            profilePhotoButtons.SuspendLayout();
            _tabBlotter.SuspendLayout();
            blotterContainer.SuspendLayout();
            _tabCertificates.SuspendLayout();
            certContainer.SuspendLayout();
            certBody.SuspendLayout();
            certFilters.SuspendLayout();
            _tabHistory.SuspendLayout();
            historyContainer.SuspendLayout();
            historyFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)historySplit).BeginInit();
            historySplit.Panel1.SuspendLayout();
            historySplit.Panel2.SuspendLayout();
            _residentHeader.SuspendLayout();
            _listPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResidents).BeginInit();
            _searchPanel.SuspendLayout();
            _actionsPanel.SuspendLayout();
            panelTabBarHost.SuspendLayout();
            SuspendLayout();
            // 
            // contentPanel
            // 
            contentPanel.Controls.Add(splitMain);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(0, 0);
            contentPanel.Name = "contentPanel";
            contentPanel.Padding = new Padding(16);
            contentPanel.Size = new Size(1060, 648);
            contentPanel.TabIndex = 0;
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.IsSplitterFixed = false;
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Vertical;
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(_listPanel);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(panelRightRoot);
            splitMain.Panel1MinSize = 280;
            splitMain.Panel2MinSize = 700;
            splitMain.Size = new Size(1028, 616);
            splitMain.SplitterDistance = 320;
            splitMain.SplitterWidth = 6;
            splitMain.TabIndex = 0;
            // 
            // tableLeftRoot
            // 
            tableLeftRoot.ColumnCount = 1;
            tableLeftRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLeftRoot.Controls.Add(dgvResidents, 0, 0);
            tableLeftRoot.Controls.Add(panelLeftPagerHost, 0, 1);
            tableLeftRoot.Controls.Add(_residentStatusPanel, 0, 2);
            tableLeftRoot.Dock = DockStyle.Fill;
            tableLeftRoot.Margin = Padding.Empty;
            tableLeftRoot.Name = "tableLeftRoot";
            tableLeftRoot.Padding = new Padding(12);
            tableLeftRoot.RowCount = 3;
            tableLeftRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLeftRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tableLeftRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLeftRoot.Size = new Size(320, 616);
            tableLeftRoot.TabIndex = 0;
            // 
            // panelLeftPagerHost
            // 
            panelLeftPagerHost.Dock = DockStyle.Fill;
            panelLeftPagerHost.Margin = new Padding(0, 0, 0, 0);
            panelLeftPagerHost.Name = "panelLeftPagerHost";
            panelLeftPagerHost.Size = new Size(320, 45);
            panelLeftPagerHost.TabIndex = 2;
            // 
            // panelRightRoot
            // 
            panelRightRoot.Controls.Add(tableBody);
            panelRightRoot.Dock = DockStyle.Fill;
            panelRightRoot.Margin = Padding.Empty;
            panelRightRoot.Name = "panelRightRoot";
            panelRightRoot.Padding = new Padding(16);
            panelRightRoot.Size = new Size(702, 616);
            panelRightRoot.TabIndex = 0;
            // 
            // panelHeader
            // 
            panelHeader.Controls.Add(_residentHeader);
            panelHeader.Dock = DockStyle.Fill;
            panelHeader.Margin = new Padding(0, 0, 0, 12);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(667, 98);
            panelHeader.TabIndex = 0;
            panelHeader.BackColor = Color.White;
            panelHeader.BorderStyle = BorderStyle.None;
            // 
            // tableBody
            // 
            tableBody.ColumnCount = 1;
            tableBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableBody.Controls.Add(panelHeader, 0, 0);
            tableBody.Controls.Add(panelProfileDetails, 0, 1);
            tableBody.Controls.Add(panelTabBarHost, 0, 2);
            tableBody.Controls.Add(datapanel, 0, 3);
            tableBody.Controls.Add(_residentBottomSummaryHost, 0, 4);
            tableBody.Dock = DockStyle.Fill;
            tableBody.Margin = Padding.Empty;
            tableBody.Name = "tableBody";
            tableBody.RowCount = 5;
            tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            tableBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            tableBody.Size = new Size(670, 584);
            tableBody.TabIndex = 0;
            // 
            // panelProfileDetails
            // 
            panelProfileDetails.BackColor = Color.White;
            panelProfileDetails.BorderStyle = BorderStyle.None;
            panelProfileDetails.Controls.Add(profileContainer);
            panelProfileDetails.Dock = DockStyle.Fill;
            panelProfileDetails.Margin = new Padding(0, 0, 0, 12);
            panelProfileDetails.MinimumSize = new Size(0, 200);
            panelProfileDetails.Name = "panelProfileDetails";
            panelProfileDetails.Padding = new Padding(0);
            panelProfileDetails.Size = new Size(670, 1);
            panelProfileDetails.TabIndex = 1;
            panelProfileDetails.Visible = false;
            // 
            // panelTabBarHost
            // 
            panelTabBarHost.Dock = DockStyle.Fill;
            panelTabBarHost.Margin = Padding.Empty;
            panelTabBarHost.Name = "panelTabBarHost";
            panelTabBarHost.Size = new Size(670, 45);
            panelTabBarHost.TabIndex = 2;
            // 
            // datapanel
            // 
            datapanel.Controls.Add(_residentTabs);
            datapanel.Dock = DockStyle.Fill;
            datapanel.Margin = Padding.Empty;
            datapanel.Name = "datapanel";
            datapanel.Padding = new Padding(0);
            datapanel.Size = new Size(670, 429);
            datapanel.TabIndex = 3;
            // 
            // _residentTabs
            // 
            _residentTabs.Controls.Add(_tabProfile);
            _residentTabs.Controls.Add(_tabBlotter);
            _residentTabs.Controls.Add(_tabCertificates);
            _residentTabs.Controls.Add(_tabHistory);
            _residentTabs.Dock = DockStyle.Fill;
            _residentTabs.Location = new Point(0, 0);
            _residentTabs.Name = "_residentTabs";
            _residentTabs.SelectedIndex = 2;
            _residentTabs.Size = new Size(670, 429);
            _residentTabs.TabIndex = 0;
            // 
            // _tabProfile
            // 
            _tabProfile.Location = new Point(4, 29);
            _tabProfile.Name = "_tabProfile";
            _tabProfile.Padding = new Padding(16, 12, 16, 16);
            _tabProfile.Size = new Size(442, 471);
            _tabProfile.TabIndex = 0;
            _tabProfile.Text = "Profile";
            // 
            // profileContainer
            // 
            profileContainer.AutoScroll = false;
            profileContainer.Controls.Add(profileActions);
            profileContainer.Controls.Add(profileBody);
            profileContainer.Controls.Add(_detailMessage);
            profileContainer.Controls.Add(profileHeader);
            profileContainer.Dock = DockStyle.Fill;
            profileContainer.Name = "profileContainer";
            profileContainer.Size = new Size(636, 186);
            profileContainer.TabIndex = 0;
            // 
            // profileActions
            // 
            profileActions.AutoSize = true;
            profileActions.Controls.Add(add);
            profileActions.Controls.Add(button3);
            profileActions.Controls.Add(_residentQuickEdit);
            profileActions.Dock = DockStyle.Top;
            profileActions.Location = new Point(0, 318);
            profileActions.Name = "profileActions";
            profileActions.Padding = new Padding(0, 12, 0, 0);
            profileActions.Size = new Size(410, 48);
            profileActions.TabIndex = 0;
            // 
            // add
            // 
            add.AutoSize = true;
            add.Location = new Point(3, 15);
            add.Name = "add";
            add.Size = new Size(75, 30);
            add.TabIndex = 0;
            add.Text = "Add";
            // 
            // button3
            // 
            button3.AutoSize = true;
            button3.Location = new Point(84, 15);
            button3.Name = "button3";
            button3.Size = new Size(75, 30);
            button3.TabIndex = 1;
            button3.Text = "Delete";
            // 
            // _residentQuickEdit
            // 
            _residentQuickEdit.Location = new Point(165, 15);
            _residentQuickEdit.Name = "_residentQuickEdit";
            _residentQuickEdit.Size = new Size(75, 30);
            _residentQuickEdit.TabIndex = 2;
            _residentQuickEdit.Text = "Edit Profile";
            // 
            // profileBody
            // 
            profileBody.AutoSize = true;
            profileBody.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            profileBody.ColumnCount = 2;
            profileBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            profileBody.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            profileBody.Controls.Add(profileInfoTable, 0, 0);
            profileBody.Controls.Add(profilePhotoPanel, 1, 0);
            profileBody.Dock = DockStyle.Top;
            profileBody.Location = new Point(0, 48);
            profileBody.Name = "profileBody";
            profileBody.RowStyles.Add(new RowStyle());
            profileBody.Size = new Size(410, 270);
            profileBody.TabIndex = 1;
            // 
            // profileInfoTable
            // 
            profileInfoTable.AutoSize = true;
            profileInfoTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            profileInfoTable.ColumnCount = 2;
            profileInfoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            profileInfoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            profileInfoTable.Controls.Add(lblFirstName, 0, 0);
            profileInfoTable.Controls.Add(_editFirstName, 1, 0);
            profileInfoTable.Controls.Add(lblMiddleName, 0, 1);
            profileInfoTable.Controls.Add(_editMiddleName, 1, 1);
            profileInfoTable.Controls.Add(lblLastName, 0, 2);
            profileInfoTable.Controls.Add(_editLastName, 1, 2);
            profileInfoTable.Controls.Add(lblGender, 0, 3);
            profileInfoTable.Controls.Add(_editGender, 1, 3);
            profileInfoTable.Controls.Add(lblBirthDate, 0, 4);
            profileInfoTable.Controls.Add(_editDob, 1, 4);
            profileInfoTable.Controls.Add(lblCivilStatus, 0, 5);
            profileInfoTable.Controls.Add(_editCivil, 1, 5);
            profileInfoTable.Controls.Add(lblContact, 0, 6);
            profileInfoTable.Controls.Add(_editContact, 1, 6);
            profileInfoTable.Controls.Add(lblStatus, 0, 7);
            profileInfoTable.Controls.Add(_editStatus, 1, 7);
            profileInfoTable.Dock = DockStyle.Top;
            profileInfoTable.Location = new Point(3, 3);
            profileInfoTable.Name = "profileInfoTable";
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.RowStyles.Add(new RowStyle());
            profileInfoTable.Size = new Size(164, 264);
            profileInfoTable.TabIndex = 0;
            // 
            // lblFirstName
            // 
            lblFirstName.AutoSize = true;
            lblFirstName.ForeColor = Color.FromArgb(45, 45, 45);
            lblFirstName.Location = new Point(3, 0);
            lblFirstName.Name = "lblFirstName";
            lblFirstName.Size = new Size(77, 20);
            lblFirstName.TabIndex = 0;
            lblFirstName.Text = "First name";
            // 
            // _editFirstName
            // 
            _editFirstName.Location = new Point(143, 3);
            _editFirstName.Name = "_editFirstName";
            _editFirstName.Size = new Size(18, 27);
            _editFirstName.TabIndex = 1;
            // 
            // lblMiddleName
            // 
            lblMiddleName.AutoSize = true;
            lblMiddleName.ForeColor = Color.FromArgb(45, 45, 45);
            lblMiddleName.Location = new Point(3, 33);
            lblMiddleName.Name = "lblMiddleName";
            lblMiddleName.Size = new Size(97, 20);
            lblMiddleName.TabIndex = 2;
            lblMiddleName.Text = "Middle name";
            // 
            // _editMiddleName
            // 
            _editMiddleName.Location = new Point(143, 36);
            _editMiddleName.Name = "_editMiddleName";
            _editMiddleName.Size = new Size(18, 27);
            _editMiddleName.TabIndex = 3;
            // 
            // lblLastName
            // 
            lblLastName.AutoSize = true;
            lblLastName.ForeColor = Color.FromArgb(45, 45, 45);
            lblLastName.Location = new Point(3, 66);
            lblLastName.Name = "lblLastName";
            lblLastName.Size = new Size(76, 20);
            lblLastName.TabIndex = 4;
            lblLastName.Text = "Last name";
            // 
            // _editLastName
            // 
            _editLastName.Location = new Point(143, 69);
            _editLastName.Name = "_editLastName";
            _editLastName.Size = new Size(18, 27);
            _editLastName.TabIndex = 5;
            // 
            // lblGender
            // 
            lblGender.AutoSize = true;
            lblGender.ForeColor = Color.FromArgb(45, 45, 45);
            lblGender.Location = new Point(3, 99);
            lblGender.Name = "lblGender";
            lblGender.Size = new Size(57, 20);
            lblGender.TabIndex = 6;
            lblGender.Text = "Gender";
            // 
            // _editGender
            // 
            _editGender.Location = new Point(143, 102);
            _editGender.Name = "_editGender";
            _editGender.Size = new Size(18, 27);
            _editGender.TabIndex = 7;
            // 
            // lblBirthDate
            // 
            lblBirthDate.AutoSize = true;
            lblBirthDate.ForeColor = Color.FromArgb(45, 45, 45);
            lblBirthDate.Location = new Point(3, 132);
            lblBirthDate.Name = "lblBirthDate";
            lblBirthDate.Size = new Size(74, 20);
            lblBirthDate.TabIndex = 8;
            lblBirthDate.Text = "Birth date";
            // 
            // _editDob
            // 
            _editDob.Location = new Point(143, 135);
            _editDob.Name = "_editDob";
            _editDob.Size = new Size(18, 27);
            _editDob.TabIndex = 9;
            // 
            // lblCivilStatus
            // 
            lblCivilStatus.AutoSize = true;
            lblCivilStatus.ForeColor = Color.FromArgb(45, 45, 45);
            lblCivilStatus.Location = new Point(3, 165);
            lblCivilStatus.Name = "lblCivilStatus";
            lblCivilStatus.Size = new Size(79, 20);
            lblCivilStatus.TabIndex = 10;
            lblCivilStatus.Text = "Civil status";
            // 
            // _editCivil
            // 
            _editCivil.Location = new Point(143, 168);
            _editCivil.Name = "_editCivil";
            _editCivil.Size = new Size(18, 27);
            _editCivil.TabIndex = 11;
            // 
            // lblContact
            // 
            lblContact.AutoSize = true;
            lblContact.ForeColor = Color.FromArgb(45, 45, 45);
            lblContact.Location = new Point(3, 198);
            lblContact.Name = "lblContact";
            lblContact.Size = new Size(84, 20);
            lblContact.TabIndex = 12;
            lblContact.Text = "Contact no.";
            // 
            // _editContact
            // 
            _editContact.Location = new Point(143, 201);
            _editContact.Name = "_editContact";
            _editContact.Size = new Size(18, 27);
            _editContact.TabIndex = 13;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.FromArgb(45, 45, 45);
            lblStatus.Location = new Point(3, 231);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(49, 20);
            lblStatus.TabIndex = 14;
            lblStatus.Text = "Status";
            // 
            // _editStatus
            // 
            _editStatus.Location = new Point(143, 234);
            _editStatus.Name = "_editStatus";
            _editStatus.Size = new Size(18, 27);
            _editStatus.TabIndex = 15;
            // 
            // profilePhotoPanel
            // 
            profilePhotoPanel.AutoSize = true;
            profilePhotoPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            profilePhotoPanel.Controls.Add(_residentPhotoCaption);
            profilePhotoPanel.Controls.Add(_residentPhoto);
            profilePhotoPanel.Controls.Add(profilePhotoButtons);
            profilePhotoPanel.Dock = DockStyle.Top;
            profilePhotoPanel.FlowDirection = FlowDirection.TopDown;
            profilePhotoPanel.Location = new Point(170, 0);
            profilePhotoPanel.Margin = new Padding(0);
            profilePhotoPanel.Name = "profilePhotoPanel";
            profilePhotoPanel.Size = new Size(240, 184);
            profilePhotoPanel.TabIndex = 1;
            // 
            // _residentPhotoCaption
            // 
            _residentPhotoCaption.Location = new Point(3, 0);
            _residentPhotoCaption.Name = "_residentPhotoCaption";
            _residentPhotoCaption.Size = new Size(100, 23);
            _residentPhotoCaption.TabIndex = 0;
            // 
            // _residentPhoto
            // 
            _residentPhoto.Location = new Point(3, 26);
            _residentPhoto.Name = "_residentPhoto";
            _residentPhoto.Size = new Size(160, 120);
            _residentPhoto.TabIndex = 1;
            _residentPhoto.TabStop = false;
            // 
            // profilePhotoButtons
            // 
            profilePhotoButtons.AutoSize = true;
            profilePhotoButtons.Controls.Add(_residentPhotoUpload);
            profilePhotoButtons.Controls.Add(_residentPhotoRemove);
            profilePhotoButtons.Location = new Point(3, 152);
            profilePhotoButtons.Name = "profilePhotoButtons";
            profilePhotoButtons.Size = new Size(162, 29);
            profilePhotoButtons.TabIndex = 2;
            // 
            // _residentPhotoUpload
            // 
            _residentPhotoUpload.Location = new Point(3, 3);
            _residentPhotoUpload.Name = "_residentPhotoUpload";
            _residentPhotoUpload.Size = new Size(75, 23);
            _residentPhotoUpload.TabIndex = 0;
            // 
            // _residentPhotoRemove
            // 
            _residentPhotoRemove.Location = new Point(84, 3);
            _residentPhotoRemove.Name = "_residentPhotoRemove";
            _residentPhotoRemove.Size = new Size(75, 23);
            _residentPhotoRemove.TabIndex = 1;
            // 
            // _detailMessage
            // 
            _detailMessage.AutoSize = true;
            _detailMessage.Dock = DockStyle.Top;
            _detailMessage.Location = new Point(0, 28);
            _detailMessage.Name = "_detailMessage";
            _detailMessage.Size = new Size(145, 20);
            _detailMessage.TabIndex = 2;
            _detailMessage.Text = "No resident selected";
            // 
            // profileHeader
            // 
            profileHeader.AutoSize = true;
            profileHeader.Dock = DockStyle.Top;
            profileHeader.Font = new Font("Century Gothic", 14F, FontStyle.Bold);
            profileHeader.ForeColor = Color.FromArgb(18, 18, 18);
            profileHeader.Location = new Point(0, 0);
            profileHeader.Margin = new Padding(0, 0, 0, 10);
            profileHeader.Name = "profileHeader";
            profileHeader.Size = new Size(187, 28);
            profileHeader.TabIndex = 3;
            profileHeader.Text = "Resident Details";
            // 
            // _tabBlotter
            // 
            _tabBlotter.Controls.Add(blotterContainer);
            _tabBlotter.Location = new Point(4, 29);
            _tabBlotter.Name = "_tabBlotter";
            _tabBlotter.Size = new Size(442, 471);
            _tabBlotter.TabIndex = 1;
            _tabBlotter.Text = "Blotter";
            // 
            // blotterContainer
            // 
            blotterContainer.Controls.Add(blotterGridPanel);
            blotterContainer.Controls.Add(blotterActions);
            blotterContainer.Controls.Add(blotterTitle);
            blotterContainer.Dock = DockStyle.Fill;
            blotterContainer.Location = new Point(0, 0);
            blotterContainer.Name = "blotterContainer";
            blotterContainer.Padding = new Padding(16, 12, 16, 16);
            blotterContainer.Size = new Size(442, 471);
            blotterContainer.TabIndex = 0;
            // 
            // blotterGridPanel
            // 
            blotterGridPanel.Dock = DockStyle.Fill;
            blotterGridPanel.Location = new Point(16, 48);
            blotterGridPanel.Name = "blotterGridPanel";
            blotterGridPanel.Padding = new Padding(0, 8, 0, 0);
            blotterGridPanel.Size = new Size(410, 407);
            blotterGridPanel.TabIndex = 0;
            // 
            // blotterActions
            // 
            blotterActions.AutoSize = false;
            blotterActions.Dock = DockStyle.Top;
            blotterActions.Location = new Point(16, 32);
            blotterActions.MinimumSize = new Size(0, 44);
            blotterActions.Name = "blotterActions";
            blotterActions.Padding = new Padding(0, 8, 0, 8);
            blotterActions.Size = new Size(410, 44);
            blotterActions.TabIndex = 1;
            blotterActions.WrapContents = false;
            // 
            // blotterTitle
            // 
            blotterTitle.AutoSize = true;
            blotterTitle.Dock = DockStyle.Top;
            blotterTitle.Location = new Point(16, 12);
            blotterTitle.Name = "blotterTitle";
            blotterTitle.Size = new Size(111, 20);
            blotterTitle.TabIndex = 2;
            blotterTitle.Text = "Blotter Records";
            // 
            // _tabCertificates
            // 
            _tabCertificates.Controls.Add(certContainer);
            _tabCertificates.Location = new Point(4, 29);
            _tabCertificates.Name = "_tabCertificates";
            _tabCertificates.Size = new Size(442, 471);
            _tabCertificates.TabIndex = 2;
            _tabCertificates.Text = "Certificates";
            // 
            // certContainer
            // 
            certContainer.Controls.Add(certBody);
            certContainer.Controls.Add(certFilters);
            certContainer.Dock = DockStyle.Fill;
            certContainer.Name = "certContainer";
            certContainer.Padding = new Padding(16);
            certContainer.Size = new Size(442, 471);
            certContainer.TabIndex = 0;
            // 
            // certBody
            // 
            certBody.ColumnCount = 1;
            certBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            certBody.Controls.Add(certGridPanel, 0, 0);
            certBody.Dock = DockStyle.Fill;
            certBody.Margin = Padding.Empty;
            certBody.Name = "certBody";
            certBody.RowCount = 1;
            certBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            certBody.Size = new Size(410, 399);
            certBody.TabIndex = 0;
            // 
            // certGridPanel
            // 
            certGridPanel.Dock = DockStyle.Fill;
            certGridPanel.Margin = Padding.Empty;
            certGridPanel.Name = "certGridPanel";
            certGridPanel.Padding = new Padding(0);
            certGridPanel.Size = new Size(410, 399);
            certGridPanel.TabIndex = 0;
            // 
            // certDetailsPanel
            // 
            certDetailsPanel.AutoScroll = true;
            certDetailsPanel.Dock = DockStyle.Fill;
            certDetailsPanel.Location = new Point(228, 3);
            certDetailsPanel.Name = "certDetailsPanel";
            certDetailsPanel.Size = new Size(179, 394);
            certDetailsPanel.TabIndex = 1;
            certDetailsPanel.Visible = false;
            // 
            // certSummary
            // 
            certSummary.AutoSize = true;
            certSummary.Dock = DockStyle.Top;
            certSummary.Location = new Point(16, 55);
            certSummary.Name = "certSummary";
            certSummary.Size = new Size(410, 0);
            certSummary.TabIndex = 1;
            // 
            // certFilters
            // 
            certFilters.AutoSize = false;
            certFilters.Controls.Add(_certFilterFromLabel);
            certFilters.Controls.Add(_certFilterToLabel);
            certFilters.Dock = DockStyle.Bottom;
            certFilters.MinimumSize = new Size(0, 40);
            certFilters.Name = "certFilters";
            certFilters.Padding = new Padding(0, 4, 0, 4);
            certFilters.Size = new Size(410, 40);
            certFilters.TabIndex = 2;
            // 
            // _certFilterFromLabel
            // 
            _certFilterFromLabel.Location = new Point(3, 0);
            _certFilterFromLabel.Name = "_certFilterFromLabel";
            _certFilterFromLabel.Size = new Size(100, 23);
            _certFilterFromLabel.TabIndex = 0;
            // 
            // _certFilterToLabel
            // 
            _certFilterToLabel.Location = new Point(109, 0);
            _certFilterToLabel.Name = "_certFilterToLabel";
            _certFilterToLabel.Size = new Size(100, 23);
            _certFilterToLabel.TabIndex = 1;
            // 
            // certActions
            // 
            certActions.AutoSize = true;
            certActions.Dock = DockStyle.Top;
            certActions.Location = new Point(16, 32);
            certActions.Name = "certActions";
            certActions.Size = new Size(410, 0);
            certActions.TabIndex = 3;
            // 
            // certTitle
            // 
            certTitle.AutoSize = true;
            certTitle.Dock = DockStyle.Top;
            certTitle.Location = new Point(16, 12);
            certTitle.Name = "certTitle";
            certTitle.Size = new Size(83, 20);
            certTitle.TabIndex = 4;
            certTitle.Text = "Certificates";
            certTitle.Visible = false;
            // 
            // _tabHistory
            // 
            _tabHistory.Controls.Add(historyContainer);
            _tabHistory.Location = new Point(4, 29);
            _tabHistory.Name = "_tabHistory";
            _tabHistory.Size = new Size(442, 471);
            _tabHistory.TabIndex = 3;
            _tabHistory.Text = "History";
            // 
            // historyContainer
            // 
            historyContainer.Controls.Add(historyBody);
            historyContainer.Controls.Add(historySummaryPanel);
            historyContainer.Controls.Add(historyFilters);
            historyContainer.Controls.Add(historyTitle);
            historyContainer.Dock = DockStyle.Fill;
            historyContainer.Location = new Point(0, 0);
            historyContainer.Name = "historyContainer";
            historyContainer.Padding = new Padding(16, 32, 16, 16);
            historyContainer.Size = new Size(442, 471);
            historyContainer.TabIndex = 0;
            // 
            // historyBody
            // 
            historyBody.Controls.Add(historySplit);
            historyBody.Dock = DockStyle.Fill;
            historyBody.Location = new Point(16, 143);
            historyBody.Name = "historyBody";
            historyBody.Size = new Size(410, 312);
            historyBody.TabIndex = 0;
            // 
            // historySummaryPanel
            // 
            historySummaryPanel.AutoSize = true;
            historySummaryPanel.Dock = DockStyle.Top;
            historySummaryPanel.FlowDirection = FlowDirection.LeftToRight;
            historySummaryPanel.Location = new Point(16, 91);
            historySummaryPanel.Name = "historySummaryPanel";
            historySummaryPanel.Size = new Size(410, 52);
            historySummaryPanel.TabIndex = 1;
            historySummaryPanel.WrapContents = true;
            historySummaryPanel.Controls.Add(historySummaryCardTotal);
            historySummaryPanel.Controls.Add(historySummaryCardResidents);
            historySummaryPanel.Controls.Add(historySummaryCardBlotter);
            historySummaryPanel.Controls.Add(historySummaryCardCertificates);
            // 
            // historyFilters
            // 
            historyFilters.AutoSize = true;
            historyFilters.Controls.Add(_historySearchBox);
            historyFilters.Controls.Add(_historyFilterModule);
            historyFilters.Controls.Add(_historyFilterFromLabel);
            historyFilters.Controls.Add(_historyFilterFrom);
            historyFilters.Controls.Add(_historyFilterToLabel);
            historyFilters.Controls.Add(_historyFilterTo);
            historyFilters.Controls.Add(_historyFilterQuickLabel);
            historyFilters.Controls.Add(_historyQuickToday);
            historyFilters.Controls.Add(_historyQuickWeek);
            historyFilters.Controls.Add(_historyQuickMonth);
            historyFilters.Controls.Add(_historyFilterClear);
            historyFilters.Controls.Add(_historyExport);
            historyFilters.Dock = DockStyle.Top;
            historyFilters.FlowDirection = FlowDirection.LeftToRight;
            historyFilters.Location = new Point(16, 52);
            historyFilters.Name = "historyFilters";
            historyFilters.Padding = new Padding(0, 6, 0, 6);
            historyFilters.Size = new Size(410, 39);
            historyFilters.TabIndex = 2;
            historyFilters.WrapContents = true;
            // 
            // _historySearchBox
            // 
            _historySearchBox.Location = new Point(3, 9);
            _historySearchBox.Name = "_historySearchBox";
            _historySearchBox.Size = new Size(160, 27);
            _historySearchBox.TabIndex = 0;
            // 
            // _historyFilterModule
            // 
            _historyFilterModule.DropDownStyle = ComboBoxStyle.DropDownList;
            _historyFilterModule.FormattingEnabled = true;
            _historyFilterModule.Location = new Point(169, 9);
            _historyFilterModule.Name = "_historyFilterModule";
            _historyFilterModule.Size = new Size(120, 28);
            _historyFilterModule.TabIndex = 1;
            // 
            // _historyFilterFromLabel
            // 
            _historyFilterFromLabel.AutoSize = true;
            _historyFilterFromLabel.Location = new Point(295, 12);
            _historyFilterFromLabel.Name = "_historyFilterFromLabel";
            _historyFilterFromLabel.Size = new Size(43, 20);
            _historyFilterFromLabel.TabIndex = 2;
            _historyFilterFromLabel.Text = "From";
            // 
            // _historyFilterFrom
            // 
            _historyFilterFrom.Format = DateTimePickerFormat.Short;
            _historyFilterFrom.Location = new Point(344, 9);
            _historyFilterFrom.Name = "_historyFilterFrom";
            _historyFilterFrom.Size = new Size(110, 27);
            _historyFilterFrom.TabIndex = 3;
            // 
            // _historyFilterToLabel
            // 
            _historyFilterToLabel.AutoSize = true;
            _historyFilterToLabel.Location = new Point(460, 12);
            _historyFilterToLabel.Name = "_historyFilterToLabel";
            _historyFilterToLabel.Size = new Size(25, 20);
            _historyFilterToLabel.TabIndex = 4;
            _historyFilterToLabel.Text = "To";
            // 
            // _historyFilterTo
            // 
            _historyFilterTo.Format = DateTimePickerFormat.Short;
            _historyFilterTo.Location = new Point(491, 9);
            _historyFilterTo.Name = "_historyFilterTo";
            _historyFilterTo.Size = new Size(110, 27);
            _historyFilterTo.TabIndex = 5;
            // 
            // _historyFilterQuickLabel
            // 
            _historyFilterQuickLabel.AutoSize = true;
            _historyFilterQuickLabel.Location = new Point(607, 12);
            _historyFilterQuickLabel.Name = "_historyFilterQuickLabel";
            _historyFilterQuickLabel.Size = new Size(47, 20);
            _historyFilterQuickLabel.TabIndex = 6;
            _historyFilterQuickLabel.Text = "Quick";
            // 
            // _historyQuickToday
            // 
            _historyQuickToday.Location = new Point(660, 7);
            _historyQuickToday.Name = "_historyQuickToday";
            _historyQuickToday.Size = new Size(62, 30);
            _historyQuickToday.TabIndex = 7;
            _historyQuickToday.Text = "Today";
            _historyQuickToday.UseVisualStyleBackColor = true;
            // 
            // _historyQuickWeek
            // 
            _historyQuickWeek.Location = new Point(728, 7);
            _historyQuickWeek.Name = "_historyQuickWeek";
            _historyQuickWeek.Size = new Size(62, 30);
            _historyQuickWeek.TabIndex = 8;
            _historyQuickWeek.Text = "7d";
            _historyQuickWeek.UseVisualStyleBackColor = true;
            // 
            // _historyQuickMonth
            // 
            _historyQuickMonth.Location = new Point(796, 7);
            _historyQuickMonth.Name = "_historyQuickMonth";
            _historyQuickMonth.Size = new Size(62, 30);
            _historyQuickMonth.TabIndex = 9;
            _historyQuickMonth.Text = "30d";
            _historyQuickMonth.UseVisualStyleBackColor = true;
            // 
            // _historyFilterClear
            // 
            _historyFilterClear.Location = new Point(864, 7);
            _historyFilterClear.Name = "_historyFilterClear";
            _historyFilterClear.Size = new Size(62, 30);
            _historyFilterClear.TabIndex = 10;
            _historyFilterClear.Text = "Clear";
            _historyFilterClear.UseVisualStyleBackColor = true;
            // 
            // _historyExport
            // 
            _historyExport.Location = new Point(932, 7);
            _historyExport.Name = "_historyExport";
            _historyExport.Size = new Size(72, 30);
            _historyExport.TabIndex = 11;
            _historyExport.Text = "Export";
            _historyExport.UseVisualStyleBackColor = true;
            // 
            // historySplit
            // 
            historySplit.Dock = DockStyle.Fill;
            historySplit.FixedPanel = FixedPanel.Panel2;
            historySplit.Location = new Point(0, 0);
            historySplit.Name = "historySplit";
            historySplit.Panel1MinSize = 180;
            historySplit.Panel2MinSize = 160;
            historySplit.Panel1.Controls.Add(historyListPanel);
            historySplit.Panel2.Controls.Add(historyDetailPanel);
            historySplit.Size = new Size(410, 312);
            historySplit.SplitterDistance = 235;
            historySplit.TabIndex = 0;
            // 
            // historyListPanel
            // 
            historyListPanel.Dock = DockStyle.Fill;
            historyListPanel.Location = new Point(0, 0);
            historyListPanel.Name = "historyListPanel";
            historyListPanel.Padding = new Padding(0, 8, 8, 0);
            historyListPanel.Size = new Size(235, 312);
            historyListPanel.TabIndex = 0;
            // 
            // historyDetailPanel
            // 
            historyDetailPanel.Controls.Add(historyDetailTable);
            historyDetailPanel.Controls.Add(historyDetailTitle);
            historyDetailPanel.Controls.Add(historyDetailEmpty);
            historyDetailPanel.Dock = DockStyle.Fill;
            historyDetailPanel.Location = new Point(0, 0);
            historyDetailPanel.Name = "historyDetailPanel";
            historyDetailPanel.Padding = new Padding(10, 8, 10, 8);
            historyDetailPanel.Size = new Size(171, 312);
            historyDetailPanel.TabIndex = 0;
            // 
            // historyDetailTitle
            // 
            historyDetailTitle.AutoSize = true;
            historyDetailTitle.Dock = DockStyle.Top;
            historyDetailTitle.Location = new Point(10, 8);
            historyDetailTitle.Name = "historyDetailTitle";
            historyDetailTitle.Size = new Size(52, 20);
            historyDetailTitle.TabIndex = 0;
            historyDetailTitle.Text = "Details";
            // 
            // historyDetailTable
            // 
            historyDetailTable.ColumnCount = 2;
            historyDetailTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            historyDetailTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            historyDetailTable.Controls.Add(historyDetailDateLabel, 0, 0);
            historyDetailTable.Controls.Add(historyDetailDateValue, 1, 0);
            historyDetailTable.Controls.Add(historyDetailModuleLabel, 0, 1);
            historyDetailTable.Controls.Add(historyDetailModuleValue, 1, 1);
            historyDetailTable.Controls.Add(historyDetailActionLabel, 0, 2);
            historyDetailTable.Controls.Add(historyDetailActionValue, 1, 2);
            historyDetailTable.Controls.Add(historyDetailByLabel, 0, 3);
            historyDetailTable.Controls.Add(historyDetailByValue, 1, 3);
            historyDetailTable.Controls.Add(historyDetailDetailsLabel, 0, 4);
            historyDetailTable.Controls.Add(historyDetailDetails, 0, 5);
            historyDetailTable.Dock = DockStyle.Top;
            historyDetailTable.Location = new Point(10, 28);
            historyDetailTable.Name = "historyDetailTable";
            historyDetailTable.RowCount = 6;
            historyDetailTable.RowStyles.Add(new RowStyle());
            historyDetailTable.RowStyles.Add(new RowStyle());
            historyDetailTable.RowStyles.Add(new RowStyle());
            historyDetailTable.RowStyles.Add(new RowStyle());
            historyDetailTable.RowStyles.Add(new RowStyle());
            historyDetailTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            historyDetailTable.Size = new Size(151, 188);
            historyDetailTable.TabIndex = 1;
            historyDetailTable.SetColumnSpan(historyDetailDetails, 2);
            // 
            // historyDetailDateLabel
            // 
            historyDetailDateLabel.AutoSize = true;
            historyDetailDateLabel.Location = new Point(3, 0);
            historyDetailDateLabel.Name = "historyDetailDateLabel";
            historyDetailDateLabel.Size = new Size(42, 20);
            historyDetailDateLabel.TabIndex = 0;
            historyDetailDateLabel.Text = "Date";
            // 
            // historyDetailDateValue
            // 
            historyDetailDateValue.AutoSize = true;
            historyDetailDateValue.Location = new Point(73, 0);
            historyDetailDateValue.Name = "historyDetailDateValue";
            historyDetailDateValue.Size = new Size(12, 20);
            historyDetailDateValue.TabIndex = 1;
            historyDetailDateValue.Text = "-";
            // 
            // historyDetailModuleLabel
            // 
            historyDetailModuleLabel.AutoSize = true;
            historyDetailModuleLabel.Location = new Point(3, 20);
            historyDetailModuleLabel.Name = "historyDetailModuleLabel";
            historyDetailModuleLabel.Size = new Size(59, 20);
            historyDetailModuleLabel.TabIndex = 2;
            historyDetailModuleLabel.Text = "Module";
            // 
            // historyDetailModuleValue
            // 
            historyDetailModuleValue.AutoSize = true;
            historyDetailModuleValue.Location = new Point(73, 20);
            historyDetailModuleValue.Name = "historyDetailModuleValue";
            historyDetailModuleValue.Size = new Size(12, 20);
            historyDetailModuleValue.TabIndex = 3;
            historyDetailModuleValue.Text = "-";
            // 
            // historyDetailActionLabel
            // 
            historyDetailActionLabel.AutoSize = true;
            historyDetailActionLabel.Location = new Point(3, 40);
            historyDetailActionLabel.Name = "historyDetailActionLabel";
            historyDetailActionLabel.Size = new Size(49, 20);
            historyDetailActionLabel.TabIndex = 4;
            historyDetailActionLabel.Text = "Action";
            // 
            // historyDetailActionValue
            // 
            historyDetailActionValue.AutoSize = true;
            historyDetailActionValue.Location = new Point(73, 40);
            historyDetailActionValue.Name = "historyDetailActionValue";
            historyDetailActionValue.Size = new Size(12, 20);
            historyDetailActionValue.TabIndex = 5;
            historyDetailActionValue.Text = "-";
            // 
            // historyDetailByLabel
            // 
            historyDetailByLabel.AutoSize = true;
            historyDetailByLabel.Location = new Point(3, 60);
            historyDetailByLabel.Name = "historyDetailByLabel";
            historyDetailByLabel.Size = new Size(24, 20);
            historyDetailByLabel.TabIndex = 6;
            historyDetailByLabel.Text = "By";
            // 
            // historyDetailByValue
            // 
            historyDetailByValue.AutoSize = true;
            historyDetailByValue.Location = new Point(73, 60);
            historyDetailByValue.Name = "historyDetailByValue";
            historyDetailByValue.Size = new Size(12, 20);
            historyDetailByValue.TabIndex = 7;
            historyDetailByValue.Text = "-";
            // 
            // historyDetailDetailsLabel
            // 
            historyDetailDetailsLabel.AutoSize = true;
            historyDetailDetailsLabel.Location = new Point(3, 80);
            historyDetailDetailsLabel.Name = "historyDetailDetailsLabel";
            historyDetailDetailsLabel.Size = new Size(54, 20);
            historyDetailDetailsLabel.TabIndex = 8;
            historyDetailDetailsLabel.Text = "Details";
            // 
            // historyDetailDetails
            // 
            historyDetailDetails.Location = new Point(3, 103);
            historyDetailDetails.Multiline = true;
            historyDetailDetails.Name = "historyDetailDetails";
            historyDetailDetails.ReadOnly = true;
            historyDetailDetails.ScrollBars = ScrollBars.Vertical;
            historyDetailDetails.Size = new Size(145, 82);
            historyDetailDetails.TabIndex = 9;
            // 
            // historyDetailEmpty
            // 
            historyDetailEmpty.AutoSize = true;
            historyDetailEmpty.Location = new Point(10, 220);
            historyDetailEmpty.Name = "historyDetailEmpty";
            historyDetailEmpty.Size = new Size(146, 20);
            historyDetailEmpty.TabIndex = 2;
            historyDetailEmpty.Text = "Select a history item.";
            // 
            // historySummaryCardTotal
            // 
            historySummaryCardTotal.BorderStyle = BorderStyle.FixedSingle;
            historySummaryCardTotal.Controls.Add(historySummaryTotalValue);
            historySummaryCardTotal.Controls.Add(historySummaryTotalLabel);
            historySummaryCardTotal.Margin = new Padding(0, 0, 8, 0);
            historySummaryCardTotal.Name = "historySummaryCardTotal";
            historySummaryCardTotal.Size = new Size(96, 52);
            historySummaryCardTotal.TabIndex = 0;
            // 
            // historySummaryTotalValue
            // 
            historySummaryTotalValue.AutoSize = true;
            historySummaryTotalValue.Location = new Point(10, 6);
            historySummaryTotalValue.Name = "historySummaryTotalValue";
            historySummaryTotalValue.Size = new Size(13, 20);
            historySummaryTotalValue.TabIndex = 0;
            historySummaryTotalValue.Text = "0";
            // 
            // historySummaryTotalLabel
            // 
            historySummaryTotalLabel.AutoSize = true;
            historySummaryTotalLabel.Location = new Point(10, 26);
            historySummaryTotalLabel.Name = "historySummaryTotalLabel";
            historySummaryTotalLabel.Size = new Size(40, 20);
            historySummaryTotalLabel.TabIndex = 1;
            historySummaryTotalLabel.Text = "Total";
            // 
            // historySummaryCardResidents
            // 
            historySummaryCardResidents.BorderStyle = BorderStyle.FixedSingle;
            historySummaryCardResidents.Controls.Add(historySummaryResidentsValue);
            historySummaryCardResidents.Controls.Add(historySummaryResidentsLabel);
            historySummaryCardResidents.Margin = new Padding(0, 0, 8, 0);
            historySummaryCardResidents.Name = "historySummaryCardResidents";
            historySummaryCardResidents.Size = new Size(96, 52);
            historySummaryCardResidents.TabIndex = 1;
            // 
            // historySummaryResidentsValue
            // 
            historySummaryResidentsValue.AutoSize = true;
            historySummaryResidentsValue.Location = new Point(10, 6);
            historySummaryResidentsValue.Name = "historySummaryResidentsValue";
            historySummaryResidentsValue.Size = new Size(13, 20);
            historySummaryResidentsValue.TabIndex = 0;
            historySummaryResidentsValue.Text = "0";
            // 
            // historySummaryResidentsLabel
            // 
            historySummaryResidentsLabel.AutoSize = true;
            historySummaryResidentsLabel.Location = new Point(10, 26);
            historySummaryResidentsLabel.Name = "historySummaryResidentsLabel";
            historySummaryResidentsLabel.Size = new Size(66, 20);
            historySummaryResidentsLabel.TabIndex = 1;
            historySummaryResidentsLabel.Text = "Residents";
            // 
            // historySummaryCardBlotter
            // 
            historySummaryCardBlotter.BorderStyle = BorderStyle.FixedSingle;
            historySummaryCardBlotter.Controls.Add(historySummaryBlotterValue);
            historySummaryCardBlotter.Controls.Add(historySummaryBlotterLabel);
            historySummaryCardBlotter.Margin = new Padding(0, 0, 8, 0);
            historySummaryCardBlotter.Name = "historySummaryCardBlotter";
            historySummaryCardBlotter.Size = new Size(96, 52);
            historySummaryCardBlotter.TabIndex = 2;
            // 
            // historySummaryBlotterValue
            // 
            historySummaryBlotterValue.AutoSize = true;
            historySummaryBlotterValue.Location = new Point(10, 6);
            historySummaryBlotterValue.Name = "historySummaryBlotterValue";
            historySummaryBlotterValue.Size = new Size(13, 20);
            historySummaryBlotterValue.TabIndex = 0;
            historySummaryBlotterValue.Text = "0";
            // 
            // historySummaryBlotterLabel
            // 
            historySummaryBlotterLabel.AutoSize = true;
            historySummaryBlotterLabel.Location = new Point(10, 26);
            historySummaryBlotterLabel.Name = "historySummaryBlotterLabel";
            historySummaryBlotterLabel.Size = new Size(49, 20);
            historySummaryBlotterLabel.TabIndex = 1;
            historySummaryBlotterLabel.Text = "Blotter";
            // 
            // historySummaryCardCertificates
            // 
            historySummaryCardCertificates.BorderStyle = BorderStyle.FixedSingle;
            historySummaryCardCertificates.Controls.Add(historySummaryCertificatesValue);
            historySummaryCardCertificates.Controls.Add(historySummaryCertificatesLabel);
            historySummaryCardCertificates.Margin = new Padding(0, 0, 0, 0);
            historySummaryCardCertificates.Name = "historySummaryCardCertificates";
            historySummaryCardCertificates.Size = new Size(96, 52);
            historySummaryCardCertificates.TabIndex = 3;
            // 
            // historySummaryCertificatesValue
            // 
            historySummaryCertificatesValue.AutoSize = true;
            historySummaryCertificatesValue.Location = new Point(10, 6);
            historySummaryCertificatesValue.Name = "historySummaryCertificatesValue";
            historySummaryCertificatesValue.Size = new Size(13, 20);
            historySummaryCertificatesValue.TabIndex = 0;
            historySummaryCertificatesValue.Text = "0";
            // 
            // historySummaryCertificatesLabel
            // 
            historySummaryCertificatesLabel.AutoSize = true;
            historySummaryCertificatesLabel.Location = new Point(10, 26);
            historySummaryCertificatesLabel.Name = "historySummaryCertificatesLabel";
            historySummaryCertificatesLabel.Size = new Size(73, 20);
            historySummaryCertificatesLabel.TabIndex = 1;
            historySummaryCertificatesLabel.Text = "Certificates";
            // 
            // historyTitle
            // 
            historyTitle.AutoSize = true;
            historyTitle.Dock = DockStyle.Top;
            historyTitle.Location = new Point(16, 32);
            historyTitle.Name = "historyTitle";
            historyTitle.Size = new Size(109, 20);
            historyTitle.TabIndex = 3;
            historyTitle.Text = "Activity History";
            // 
            // _residentHeader
            // 
            _residentHeader.AutoSize = false;
            _residentHeader.AutoSizeMode = AutoSizeMode.GrowOnly;
            _residentHeader.Controls.Add(_residentHeaderName);
            _residentHeader.Controls.Add(_residentHeaderStatus);
            _residentHeader.Controls.Add(_residentHeaderMeta);
            _residentHeader.Dock = DockStyle.Fill;
            _residentHeader.Location = new Point(0, 0);
            _residentHeader.Margin = Padding.Empty;
            _residentHeader.Name = "_residentHeader";
            _residentHeader.Padding = new Padding(16);
            _residentHeader.Size = new Size(667, 98);
            _residentHeader.TabIndex = 1;
            // 
            // _residentHeaderName
            // 
            _residentHeaderName.AutoSize = true;
            _residentHeaderName.Location = new Point(16, 12);
            _residentHeaderName.Name = "_residentHeaderName";
            _residentHeaderName.Size = new Size(145, 20);
            _residentHeaderName.TabIndex = 0;
            _residentHeaderName.Text = "No resident selected";
            // 
            // _residentHeaderStatus
            // 
            _residentHeaderStatus.AutoSize = true;
            _residentHeaderStatus.Location = new Point(200, 12);
            _residentHeaderStatus.Name = "_residentHeaderStatus";
            _residentHeaderStatus.Size = new Size(0, 20);
            _residentHeaderStatus.TabIndex = 1;
            // 
            // _residentHeaderMeta
            // 
            _residentHeaderMeta.AutoSize = true;
            _residentHeaderMeta.Location = new Point(16, 38);
            _residentHeaderMeta.Name = "_residentHeaderMeta";
            _residentHeaderMeta.Size = new Size(221, 20);
            _residentHeaderMeta.TabIndex = 2;
            _residentHeaderMeta.Text = "Select a resident to view details.";
            // 
            // _listPanel
            // 
            _listPanel.Controls.Add(tableLeftRoot);
            _listPanel.Dock = DockStyle.Fill;
            _listPanel.Margin = Padding.Empty;
            _listPanel.Name = "_listPanel";
            _listPanel.Padding = Padding.Empty;
            _listPanel.Size = new Size(320, 616);
            _listPanel.TabIndex = 0;
            // 
            // dgvResidents
            // 
            dgvResidents.ColumnHeadersHeight = 29;
            dgvResidents.Dock = DockStyle.Fill;
            dgvResidents.Margin = Padding.Empty;
            dgvResidents.Name = "dgvResidents";
            dgvResidents.RowHeadersWidth = 51;
            dgvResidents.Size = new Size(320, 458);
            dgvResidents.TabIndex = 3;
            // 
            // _searchPanel
            // 
            _searchPanel.AutoSize = false;
            _searchPanel.Controls.Add(_searchBox);
            _searchPanel.Controls.Add(_searchClear);
            _searchPanel.Dock = DockStyle.Fill;
            _searchPanel.Margin = Padding.Empty;
            _searchPanel.Name = "_searchPanel";
            _searchPanel.Padding = new Padding(0, 12, 0, 12);
            _searchPanel.Size = new Size(320, 60);
            _searchPanel.TabIndex = 1;
            _searchPanel.WrapContents = false;
            // 
            // _searchBox
            // 
            _searchBox.Location = new Point(3, 11);
            _searchBox.Name = "_searchBox";
            _searchBox.Size = new Size(100, 27);
            _searchBox.TabIndex = 0;
            // 
            // _searchClear
            // 
            _searchClear.Location = new Point(109, 11);
            _searchClear.Name = "_searchClear";
            _searchClear.Size = new Size(75, 23);
            _searchClear.TabIndex = 1;
            // 
            // _actionsPanel
            // 
            _actionsPanel.AutoSize = false;
            _actionsPanel.Controls.Add(button1);
            _actionsPanel.Dock = DockStyle.Fill;
            _actionsPanel.Margin = Padding.Empty;
            _actionsPanel.Name = "_actionsPanel";
            _actionsPanel.Padding = new Padding(0, 8, 0, 8);
            _actionsPanel.Size = new Size(320, 50);
            _actionsPanel.TabIndex = 2;
            _actionsPanel.WrapContents = false;
            // 
            // button1
            // 
            button1.Location = new Point(3, 15);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            // 
            // certDetailsHeader
            // 
            certDetailsHeader.Location = new Point(0, 0);
            certDetailsHeader.Name = "certDetailsHeader";
            certDetailsHeader.Size = new Size(100, 23);
            certDetailsHeader.TabIndex = 0;
            // 
            // certDataHeader
            // 
            certDataHeader.Location = new Point(0, 0);
            certDataHeader.Name = "certDataHeader";
            certDataHeader.Size = new Size(100, 23);
            certDataHeader.TabIndex = 0;
            // 
            // certSummaryTable
            // 
            certSummaryTable.Location = new Point(0, 0);
            certSummaryTable.Name = "certSummaryTable";
            certSummaryTable.Size = new Size(200, 100);
            certSummaryTable.TabIndex = 0;
            // 
            // certDetailTable
            // 
            certDetailTable.Location = new Point(0, 0);
            certDetailTable.Name = "certDetailTable";
            certDetailTable.Size = new Size(200, 100);
            certDetailTable.TabIndex = 0;
            // 
            // button2
            // 
            button2.Location = new Point(0, 0);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 0;
            // 
            // ResidentModuleControl
            // 
            Controls.Add(contentPanel);
            Name = "ResidentModuleControl";
            Size = new Size(1060, 648);
            contentPanel.ResumeLayout(false);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            tableLeftRoot.ResumeLayout(false);
            panelLeftPagerHost.ResumeLayout(false);
            panelRightRoot.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            tableBody.ResumeLayout(false);
            panelProfileDetails.ResumeLayout(false);
            datapanel.ResumeLayout(false);
            datapanel.PerformLayout();
            _residentTabs.ResumeLayout(false);
            _tabProfile.ResumeLayout(false);
            profileContainer.ResumeLayout(false);
            profileContainer.PerformLayout();
            profileActions.ResumeLayout(false);
            profileActions.PerformLayout();
            profileBody.ResumeLayout(false);
            profileBody.PerformLayout();
            profileInfoTable.ResumeLayout(false);
            profileInfoTable.PerformLayout();
            profilePhotoPanel.ResumeLayout(false);
            profilePhotoPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_residentPhoto).EndInit();
            profilePhotoButtons.ResumeLayout(false);
            _tabBlotter.ResumeLayout(false);
            blotterContainer.ResumeLayout(false);
            blotterContainer.PerformLayout();
            _tabCertificates.ResumeLayout(false);
            certContainer.ResumeLayout(false);
            certContainer.PerformLayout();
            certBody.ResumeLayout(false);
            certFilters.ResumeLayout(false);
            _tabHistory.ResumeLayout(false);
            historyContainer.ResumeLayout(false);
            historyContainer.PerformLayout();
            historyFilters.ResumeLayout(false);
            historyFilters.PerformLayout();
            historySplit.Panel1.ResumeLayout(false);
            historySplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)historySplit).EndInit();
            historySplit.ResumeLayout(false);
            _residentHeader.ResumeLayout(false);
            _residentHeader.PerformLayout();
            _listPanel.ResumeLayout(false);
            _listPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResidents).EndInit();
            _searchPanel.ResumeLayout(false);
            _searchPanel.PerformLayout();
            _actionsPanel.ResumeLayout(false);
            panelTabBarHost.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}

