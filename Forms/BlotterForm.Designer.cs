namespace baranggaysystem1
{
    partial class BlotterForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.FlowLayoutPanel headerPanel;
        private System.Windows.Forms.FlowLayoutPanel headerTitleRow;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblStatusBadge;
        private System.Windows.Forms.Label lblSubHeader;
        private System.Windows.Forms.Label lblComplainant;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.TableLayoutPanel formTable;
        private System.Windows.Forms.Label lblRespondent;
        private System.Windows.Forms.Label lblSectionIncident;
        private System.Windows.Forms.Label lblIncidentType;
        private System.Windows.Forms.Label lblIncidentDate;
        private System.Windows.Forms.Label lblIncidentTime;
        private System.Windows.Forms.Label lblIncidentLocation;
        private System.Windows.Forms.Label lblDetails;
        private System.Windows.Forms.Label lblWitnesses;
        private System.Windows.Forms.Label lblSectionHandling;
        private System.Windows.Forms.Label lblActionTaken;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblResolution;
        private System.Windows.Forms.Panel respondentPanel;
        private System.Windows.Forms.FlowLayoutPanel respondentChoice;
        private System.Windows.Forms.RadioButton rbResident;
        private System.Windows.Forms.RadioButton rbOther;
        private System.Windows.Forms.TableLayoutPanel respondentFields;
        private System.Windows.Forms.ComboBox cmbRespondent;
        private System.Windows.Forms.TextBox txtRespondentOther;
        private System.Windows.Forms.TextBox txtIncidentType;
        private System.Windows.Forms.DateTimePicker dtpIncidentDate;
        private System.Windows.Forms.DateTimePicker dtpIncidentTime;
        private System.Windows.Forms.TextBox txtIncidentLocation;
        private System.Windows.Forms.TextBox txtIncidentDetails;
        private System.Windows.Forms.TextBox txtWitnesses;
        private System.Windows.Forms.TextBox txtActionTaken;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.TableLayoutPanel statusPanel;
        private System.Windows.Forms.TextBox txtResolution;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnUpdateStatus;
        private System.Windows.Forms.Button btnPrint;

        private System.Windows.Forms.GroupBox grpAiAnalysis;
        private System.Windows.Forms.Panel aiHeaderPanel;
        private System.Windows.Forms.TableLayoutPanel aiLayout;
        private System.Windows.Forms.TableLayoutPanel aiLeftLayout;
        private System.Windows.Forms.TableLayoutPanel aiRightLayout;
        private System.Windows.Forms.TableLayoutPanel aiRiskGrid;
        private System.Windows.Forms.FlowLayoutPanel aiRiskScorePanel;
        private System.Windows.Forms.TableLayoutPanel aiEntitiesGrid;
        private System.Windows.Forms.Button btnRunAiAnalysis;
        private System.Windows.Forms.Label lblAiMeta;
        private System.Windows.Forms.Label lblAiSummaryTitle;
        private System.Windows.Forms.TextBox txtAiSummary;
        private System.Windows.Forms.Label lblAiKeyPointsTitle;
        private System.Windows.Forms.ListBox lstAiKeyPoints;
        private System.Windows.Forms.Label lblAiCategory;
        private System.Windows.Forms.Label lblAiCategoryValue;
        private System.Windows.Forms.Label lblAiConfidence;
        private System.Windows.Forms.Label lblAiConfidenceValue;
        private System.Windows.Forms.Label lblAiRiskLevel;
        private System.Windows.Forms.Label lblAiRiskLevelValue;
        private System.Windows.Forms.Label lblAiRiskScore;
        private System.Windows.Forms.ProgressBar progressRiskScore;
        private System.Windows.Forms.Label lblAiRiskScoreValue;
        private System.Windows.Forms.Label lblAiRiskReasonsTitle;
        private System.Windows.Forms.ListBox lstAiRiskReasons;
        private System.Windows.Forms.Label lblAiEntitiesTitle;
        private System.Windows.Forms.Label lblAiPeople;
        private System.Windows.Forms.ListBox lstAiPeople;
        private System.Windows.Forms.Label lblAiPlaces;
        private System.Windows.Forms.ListBox lstAiPlaces;
        private System.Windows.Forms.Label lblAiDatesTimes;
        private System.Windows.Forms.ListBox lstAiDatesTimes;
        private System.Windows.Forms.Label lblAiItems;
        private System.Windows.Forms.ListBox lstAiItems;
        private System.Windows.Forms.Label lblAiNextActionTitle;
        private System.Windows.Forms.TextBox txtAiNextAction;

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
            headerPanel = new System.Windows.Forms.FlowLayoutPanel();
            headerTitleRow = new System.Windows.Forms.FlowLayoutPanel();
            lblHeader = new System.Windows.Forms.Label();
            lblStatusBadge = new System.Windows.Forms.Label();
            lblSubHeader = new System.Windows.Forms.Label();
            lblComplainant = new System.Windows.Forms.Label();
            mainLayout = new System.Windows.Forms.TableLayoutPanel();
            leftPanel = new System.Windows.Forms.Panel();
            rightPanel = new System.Windows.Forms.Panel();
            formTable = new System.Windows.Forms.TableLayoutPanel();
            lblRespondent = new System.Windows.Forms.Label();
            lblSectionIncident = new System.Windows.Forms.Label();
            respondentPanel = new System.Windows.Forms.Panel();
            respondentFields = new System.Windows.Forms.TableLayoutPanel();
            cmbRespondent = new System.Windows.Forms.ComboBox();
            txtRespondentOther = new System.Windows.Forms.TextBox();
            respondentChoice = new System.Windows.Forms.FlowLayoutPanel();
            rbResident = new System.Windows.Forms.RadioButton();
            rbOther = new System.Windows.Forms.RadioButton();
            lblIncidentType = new System.Windows.Forms.Label();
            txtIncidentType = new System.Windows.Forms.TextBox();
            lblIncidentDate = new System.Windows.Forms.Label();
            dtpIncidentDate = new System.Windows.Forms.DateTimePicker();
            lblIncidentTime = new System.Windows.Forms.Label();
            dtpIncidentTime = new System.Windows.Forms.DateTimePicker();
            lblIncidentLocation = new System.Windows.Forms.Label();
            txtIncidentLocation = new System.Windows.Forms.TextBox();
            lblDetails = new System.Windows.Forms.Label();
            txtIncidentDetails = new System.Windows.Forms.TextBox();
            lblWitnesses = new System.Windows.Forms.Label();
            txtWitnesses = new System.Windows.Forms.TextBox();
            lblSectionHandling = new System.Windows.Forms.Label();
            lblActionTaken = new System.Windows.Forms.Label();
            txtActionTaken = new System.Windows.Forms.TextBox();
            lblStatus = new System.Windows.Forms.Label();
            cmbStatus = new System.Windows.Forms.ComboBox();
            statusPanel = new System.Windows.Forms.TableLayoutPanel();
            lblResolution = new System.Windows.Forms.Label();
            txtResolution = new System.Windows.Forms.TextBox();
            buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            btnSave = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            btnUpdateStatus = new System.Windows.Forms.Button();
            btnPrint = new System.Windows.Forms.Button();
            grpAiAnalysis = new System.Windows.Forms.GroupBox();
            aiHeaderPanel = new System.Windows.Forms.Panel();
            aiLayout = new System.Windows.Forms.TableLayoutPanel();
            aiLeftLayout = new System.Windows.Forms.TableLayoutPanel();
            aiRightLayout = new System.Windows.Forms.TableLayoutPanel();
            aiRiskGrid = new System.Windows.Forms.TableLayoutPanel();
            aiRiskScorePanel = new System.Windows.Forms.FlowLayoutPanel();
            aiEntitiesGrid = new System.Windows.Forms.TableLayoutPanel();
            btnRunAiAnalysis = new System.Windows.Forms.Button();
            lblAiMeta = new System.Windows.Forms.Label();
            lblAiSummaryTitle = new System.Windows.Forms.Label();
            txtAiSummary = new System.Windows.Forms.TextBox();
            lblAiKeyPointsTitle = new System.Windows.Forms.Label();
            lstAiKeyPoints = new System.Windows.Forms.ListBox();
            lblAiCategory = new System.Windows.Forms.Label();
            lblAiCategoryValue = new System.Windows.Forms.Label();
            lblAiConfidence = new System.Windows.Forms.Label();
            lblAiConfidenceValue = new System.Windows.Forms.Label();
            lblAiRiskLevel = new System.Windows.Forms.Label();
            lblAiRiskLevelValue = new System.Windows.Forms.Label();
            lblAiRiskScore = new System.Windows.Forms.Label();
            progressRiskScore = new System.Windows.Forms.ProgressBar();
            lblAiRiskScoreValue = new System.Windows.Forms.Label();
            lblAiRiskReasonsTitle = new System.Windows.Forms.Label();
            lstAiRiskReasons = new System.Windows.Forms.ListBox();
            lblAiEntitiesTitle = new System.Windows.Forms.Label();
            lblAiPeople = new System.Windows.Forms.Label();
            lstAiPeople = new System.Windows.Forms.ListBox();
            lblAiPlaces = new System.Windows.Forms.Label();
            lstAiPlaces = new System.Windows.Forms.ListBox();
            lblAiDatesTimes = new System.Windows.Forms.Label();
            lstAiDatesTimes = new System.Windows.Forms.ListBox();
            lblAiItems = new System.Windows.Forms.Label();
            lstAiItems = new System.Windows.Forms.ListBox();
            lblAiNextActionTitle = new System.Windows.Forms.Label();
            txtAiNextAction = new System.Windows.Forms.TextBox();

            headerPanel.SuspendLayout();
            mainLayout.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            formTable.SuspendLayout();
            respondentPanel.SuspendLayout();
            respondentFields.SuspendLayout();
            respondentChoice.SuspendLayout();
            buttonPanel.SuspendLayout();
            grpAiAnalysis.SuspendLayout();
            aiHeaderPanel.SuspendLayout();
            aiLayout.SuspendLayout();
            aiLeftLayout.SuspendLayout();
            aiRightLayout.SuspendLayout();
            aiRiskGrid.SuspendLayout();
            aiRiskScorePanel.SuspendLayout();
            aiEntitiesGrid.SuspendLayout();
            SuspendLayout();

            headerPanel.AutoSize = false;
            headerPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            headerPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            headerPanel.Size = new System.Drawing.Size(560, 90);
            headerPanel.Name = "headerPanel";
            headerPanel.TabIndex = 0;

            headerTitleRow.AutoSize = true;
            headerTitleRow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            headerTitleRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            headerTitleRow.WrapContents = false;
            headerTitleRow.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            headerTitleRow.Name = "headerTitleRow";

            lblHeader.AutoSize = true;
            lblHeader.Text = "New Blotter Record";
            lblHeader.Name = "lblHeader";
            lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);

            lblStatusBadge.AutoSize = true;
            lblStatusBadge.Text = "Ongoing";
            lblStatusBadge.Name = "lblStatusBadge";
            lblStatusBadge.Margin = new System.Windows.Forms.Padding(12, 3, 0, 0);
            lblStatusBadge.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            lblStatusBadge.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            headerTitleRow.Controls.Add(lblHeader);
            headerTitleRow.Controls.Add(lblStatusBadge);

            lblSubHeader.AutoSize = true;
            lblSubHeader.Text = "Provide incident details and respondent information.";
            lblSubHeader.Name = "lblSubHeader";
            lblSubHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);

            lblComplainant.AutoSize = true;
            lblComplainant.Text = "Complainant:";
            lblComplainant.Name = "lblComplainant";
            lblComplainant.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);

            headerPanel.Controls.Add(headerTitleRow);
            headerPanel.Controls.Add(lblSubHeader);
            headerPanel.Controls.Add(lblComplainant);

            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62F));
            mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayout.Location = new System.Drawing.Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            leftPanel.AutoScroll = true;
            leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            leftPanel.Name = "leftPanel";
            leftPanel.Padding = new System.Windows.Forms.Padding(21, 24, 21, 24);

            rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new System.Windows.Forms.Padding(12, 24, 21, 24);

            formTable.AutoSize = false;
            formTable.Dock = System.Windows.Forms.DockStyle.Top;
            formTable.Name = "formTable";
            formTable.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            formTable.ColumnCount = 2;
            formTable.RowCount = 12;
            formTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            formTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            formTable.Size = new System.Drawing.Size(560, 500);

            lblRespondent.AutoSize = true;
            lblRespondent.Text = "Respondent";
            lblRespondent.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            lblSectionIncident.AutoSize = true;
            lblSectionIncident.Text = "Incident details";
            lblSectionIncident.Margin = new System.Windows.Forms.Padding(0, 16, 0, 4);

            lblSectionHandling.AutoSize = true;
            lblSectionHandling.Text = "Case handling";
            lblSectionHandling.Margin = new System.Windows.Forms.Padding(0, 16, 0, 4);

            respondentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            respondentPanel.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
            respondentPanel.MinimumSize = new System.Drawing.Size(0, 72);

            respondentChoice.AutoSize = true;
            respondentChoice.Dock = System.Windows.Forms.DockStyle.Top;
            respondentChoice.WrapContents = false;
            respondentChoice.Margin = new System.Windows.Forms.Padding(0);
            respondentChoice.Controls.Add(rbResident);
            respondentChoice.Controls.Add(rbOther);

            rbResident.AutoSize = true;
            rbResident.Checked = true;
            rbResident.TabStop = true;
            rbResident.Text = "Resident";
            rbResident.CheckedChanged += RespondentMode_CheckedChanged;

            rbOther.AutoSize = true;
            rbOther.Text = "Other";
            rbOther.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            rbOther.CheckedChanged += RespondentMode_CheckedChanged;

            respondentFields.AutoSize = false;
            respondentFields.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            respondentFields.ColumnCount = 1;
            respondentFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            respondentFields.RowCount = 2;
            respondentFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            respondentFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            respondentFields.Dock = System.Windows.Forms.DockStyle.Top;
            respondentFields.Location = new System.Drawing.Point(0, 28);
            respondentFields.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            respondentFields.Size = new System.Drawing.Size(760, 66);
            respondentFields.Controls.Add(cmbRespondent, 0, 0);
            respondentFields.Controls.Add(txtRespondentOther, 0, 1);

            cmbRespondent.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            cmbRespondent.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            cmbRespondent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbRespondent.Dock = System.Windows.Forms.DockStyle.Fill;
            cmbRespondent.Margin = new System.Windows.Forms.Padding(0);
            cmbRespondent.Name = "cmbRespondent";
            cmbRespondent.Size = new System.Drawing.Size(760, 28);

            txtRespondentOther.Dock = System.Windows.Forms.DockStyle.Fill;
            txtRespondentOther.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            txtRespondentOther.Name = "txtRespondentOther";
            txtRespondentOther.PlaceholderText = "Enter respondent name";
            txtRespondentOther.Size = new System.Drawing.Size(760, 27);

            respondentPanel.Controls.Add(respondentChoice);
            respondentPanel.Controls.Add(respondentFields);

            lblIncidentType.AutoSize = true;
            lblIncidentType.Text = "Incident type";
            lblIncidentType.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            txtIncidentType.Name = "txtIncidentType";
            txtIncidentType.Dock = System.Windows.Forms.DockStyle.Fill;
            txtIncidentType.Size = new System.Drawing.Size(760, 27);
            txtIncidentType.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblIncidentDate.AutoSize = true;
            lblIncidentDate.Text = "Incident date";
            lblIncidentDate.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            dtpIncidentDate.Name = "dtpIncidentDate";
            dtpIncidentDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            dtpIncidentDate.Dock = System.Windows.Forms.DockStyle.Fill;
            dtpIncidentDate.Size = new System.Drawing.Size(760, 27);
            dtpIncidentDate.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblIncidentTime.AutoSize = true;
            lblIncidentTime.Text = "Incident time";
            lblIncidentTime.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            dtpIncidentTime.Name = "dtpIncidentTime";
            dtpIncidentTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            dtpIncidentTime.ShowUpDown = true;
            dtpIncidentTime.Dock = System.Windows.Forms.DockStyle.Fill;
            dtpIncidentTime.Size = new System.Drawing.Size(760, 27);
            dtpIncidentTime.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblIncidentLocation.AutoSize = true;
            lblIncidentLocation.Text = "Location";
            lblIncidentLocation.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            txtIncidentLocation.Name = "txtIncidentLocation";
            txtIncidentLocation.Dock = System.Windows.Forms.DockStyle.Fill;
            txtIncidentLocation.Size = new System.Drawing.Size(760, 27);
            txtIncidentLocation.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblDetails.AutoSize = true;
            lblDetails.Text = "Details";
            lblDetails.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            txtIncidentDetails.Name = "txtIncidentDetails";
            txtIncidentDetails.Multiline = true;
            txtIncidentDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtIncidentDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            txtIncidentDetails.Size = new System.Drawing.Size(760, 90);
            txtIncidentDetails.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblWitnesses.AutoSize = true;
            lblWitnesses.Text = "Witnesses";
            lblWitnesses.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            txtWitnesses.Name = "txtWitnesses";
            txtWitnesses.Dock = System.Windows.Forms.DockStyle.Fill;
            txtWitnesses.Size = new System.Drawing.Size(760, 27);
            txtWitnesses.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblActionTaken.AutoSize = true;
            lblActionTaken.Text = "Action taken";
            lblActionTaken.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            txtActionTaken.Name = "txtActionTaken";
            txtActionTaken.Multiline = true;
            txtActionTaken.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtActionTaken.Dock = System.Windows.Forms.DockStyle.Fill;
            txtActionTaken.Size = new System.Drawing.Size(760, 60);
            txtActionTaken.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            lblStatus.AutoSize = true;
            lblStatus.Text = "Status";
            lblStatus.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            cmbStatus.Name = "cmbStatus";
            cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbStatus.Items.AddRange(new object[] { "Ongoing", "Settled", "Referred" });
            cmbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            cmbStatus.Size = new System.Drawing.Size(760, 28);
            cmbStatus.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            statusPanel.ColumnCount = 1;
            statusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            statusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            statusPanel.RowCount = 1;
            statusPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            statusPanel.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);
            statusPanel.Controls.Add(cmbStatus, 0, 0);

            lblResolution.AutoSize = true;
            lblResolution.Text = "Resolution details";
            lblResolution.Margin = new System.Windows.Forms.Padding(0, 10, 0, 4);

            txtResolution.Name = "txtResolution";
            txtResolution.Multiline = true;
            txtResolution.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtResolution.Dock = System.Windows.Forms.DockStyle.Fill;
            txtResolution.Size = new System.Drawing.Size(760, 60);
            txtResolution.Margin = new System.Windows.Forms.Padding(0, 8, 0, 8);

            formTable.Controls.Add(lblRespondent, 0, 0);
            formTable.Controls.Add(respondentPanel, 1, 0);
            formTable.Controls.Add(lblSectionIncident, 0, 1);
            formTable.SetColumnSpan(lblSectionIncident, 2);
            formTable.Controls.Add(lblIncidentType, 0, 2);
            formTable.Controls.Add(txtIncidentType, 1, 2);
            formTable.Controls.Add(lblIncidentDate, 0, 3);
            formTable.Controls.Add(dtpIncidentDate, 1, 3);
            formTable.Controls.Add(lblIncidentTime, 0, 4);
            formTable.Controls.Add(dtpIncidentTime, 1, 4);
            formTable.Controls.Add(lblIncidentLocation, 0, 5);
            formTable.Controls.Add(txtIncidentLocation, 1, 5);
            formTable.Controls.Add(lblDetails, 0, 6);
            formTable.Controls.Add(txtIncidentDetails, 1, 6);
            formTable.Controls.Add(lblWitnesses, 0, 7);
            formTable.Controls.Add(txtWitnesses, 1, 7);
            formTable.Controls.Add(lblSectionHandling, 0, 8);
            formTable.SetColumnSpan(lblSectionHandling, 2);
            formTable.Controls.Add(lblStatus, 0, 9);
            formTable.Controls.Add(statusPanel, 1, 9);
            formTable.Controls.Add(lblActionTaken, 0, 10);
            formTable.Controls.Add(txtActionTaken, 1, 10);
            formTable.Controls.Add(lblResolution, 0, 11);
            formTable.Controls.Add(txtResolution, 1, 11);

            buttonPanel.AutoSize = true;
            buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Padding = new System.Windows.Forms.Padding(0, 16, 0, 0);

            btnSave.AutoSize = true;
            btnSave.Text = "Save";
            btnSave.Name = "btnSave";
            btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 9, 0);
            btnSave.Click += ValidateAndClose;

            btnCancel.AutoSize = true;
            btnCancel.Text = "Cancel";
            btnCancel.Name = "btnCancel";
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Margin = new System.Windows.Forms.Padding(0);

            btnPrint.AutoSize = true;
            btnPrint.Text = "Print";
            btnPrint.Name = "btnPrint";
            btnPrint.Margin = new System.Windows.Forms.Padding(0, 0, 9, 0);
            btnPrint.Click += PrintBlotter_Click;

            btnUpdateStatus.AutoSize = true;
            btnUpdateStatus.Text = "Update Status";
            btnUpdateStatus.Name = "btnUpdateStatus";
            btnUpdateStatus.Margin = new System.Windows.Forms.Padding(0, 0, 9, 0);
            btnUpdateStatus.Click += UpdateStatus_Click;

            buttonPanel.Controls.Add(btnSave);
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnPrint);
            buttonPanel.Controls.Add(btnUpdateStatus);
            grpAiAnalysis.Dock = System.Windows.Forms.DockStyle.Fill;
            grpAiAnalysis.Location = new System.Drawing.Point(0, 0);
            grpAiAnalysis.Name = "grpAiAnalysis";
            grpAiAnalysis.Size = new System.Drawing.Size(320, 320);
            grpAiAnalysis.TabStop = false;
            grpAiAnalysis.Text = "AI Blotter & Case Assistant";

            aiHeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            aiHeaderPanel.Height = 44;
            aiHeaderPanel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 0);
            aiHeaderPanel.Name = "aiHeaderPanel";

            btnRunAiAnalysis.Dock = System.Windows.Forms.DockStyle.Right;
            btnRunAiAnalysis.Location = new System.Drawing.Point(0, 0);
            btnRunAiAnalysis.Name = "btnRunAiAnalysis";
            btnRunAiAnalysis.Size = new System.Drawing.Size(132, 34);
            btnRunAiAnalysis.Text = "Run AI Analysis";
            btnRunAiAnalysis.UseVisualStyleBackColor = true;
            btnRunAiAnalysis.Click += RunAiAnalysis_Click;

            lblAiMeta.AutoSize = true;
            lblAiMeta.Dock = System.Windows.Forms.DockStyle.Left;
            lblAiMeta.Location = new System.Drawing.Point(14, 30);
            lblAiMeta.Name = "lblAiMeta";
            lblAiMeta.Text = "Last AI run: - | Model: -";

            aiHeaderPanel.Controls.Add(btnRunAiAnalysis);
            aiHeaderPanel.Controls.Add(lblAiMeta);

            aiLayout.ColumnCount = 2;
            aiLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            aiLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            aiLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            aiLayout.Location = new System.Drawing.Point(3, 23);
            aiLayout.Name = "aiLayout";
            aiLayout.RowCount = 1;
            aiLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            aiLayout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 8);

            aiLeftLayout.ColumnCount = 1;
            aiLeftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            aiLeftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            aiLeftLayout.RowCount = 6;
            aiLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            aiLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            aiLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));

            aiRightLayout.ColumnCount = 1;
            aiRightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            aiRightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            aiRightLayout.RowCount = 5;
            aiRightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiRightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiRightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            aiRightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiRightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));

            aiRiskGrid.ColumnCount = 2;
            aiRiskGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            aiRiskGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
            aiRiskGrid.Dock = System.Windows.Forms.DockStyle.Top;
            aiRiskGrid.RowCount = 4;
            aiRiskGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiRiskGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiRiskGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiRiskGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            aiRiskScorePanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            aiRiskScorePanel.WrapContents = false;
            aiRiskScorePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            aiRiskScorePanel.Margin = new System.Windows.Forms.Padding(0);

            lblAiSummaryTitle.AutoSize = true;
            lblAiSummaryTitle.Location = new System.Drawing.Point(14, 63);
            lblAiSummaryTitle.Name = "lblAiSummaryTitle";
            lblAiSummaryTitle.Text = "Summary";

            txtAiSummary.Location = new System.Drawing.Point(14, 86);
            txtAiSummary.Name = "txtAiSummary";
            txtAiSummary.Multiline = true;
            txtAiSummary.ReadOnly = true;
            txtAiSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtAiSummary.Size = new System.Drawing.Size(260, 70);
            txtAiSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            txtAiSummary.Margin = new System.Windows.Forms.Padding(0, 4, 8, 8);

            lblAiKeyPointsTitle.AutoSize = true;
            lblAiKeyPointsTitle.Location = new System.Drawing.Point(14, 163);
            lblAiKeyPointsTitle.Name = "lblAiKeyPointsTitle";
            lblAiKeyPointsTitle.Text = "Key points";

            lstAiKeyPoints.FormattingEnabled = true;
            lstAiKeyPoints.ItemHeight = 20;
            lstAiKeyPoints.Location = new System.Drawing.Point(14, 186);
            lstAiKeyPoints.Name = "lstAiKeyPoints";
            lstAiKeyPoints.Size = new System.Drawing.Size(260, 84);
            lstAiKeyPoints.Dock = System.Windows.Forms.DockStyle.Fill;
            lstAiKeyPoints.Margin = new System.Windows.Forms.Padding(0, 4, 8, 8);

            lblAiCategory.AutoSize = true;
            lblAiCategory.Location = new System.Drawing.Point(290, 63);
            lblAiCategory.Name = "lblAiCategory";
            lblAiCategory.Text = "Category:";

            lblAiCategoryValue.AutoSize = true;
            lblAiCategoryValue.Location = new System.Drawing.Point(370, 63);
            lblAiCategoryValue.Name = "lblAiCategoryValue";
            lblAiCategoryValue.Text = "-";

            lblAiConfidence.AutoSize = true;
            lblAiConfidence.Location = new System.Drawing.Point(290, 86);
            lblAiConfidence.Name = "lblAiConfidence";
            lblAiConfidence.Text = "Confidence:";

            lblAiConfidenceValue.AutoSize = true;
            lblAiConfidenceValue.Location = new System.Drawing.Point(370, 86);
            lblAiConfidenceValue.Name = "lblAiConfidenceValue";
            lblAiConfidenceValue.Text = "-";

            lblAiRiskLevel.AutoSize = true;
            lblAiRiskLevel.Location = new System.Drawing.Point(290, 110);
            lblAiRiskLevel.Name = "lblAiRiskLevel";
            lblAiRiskLevel.Text = "Risk level:";

            lblAiRiskLevelValue.AutoSize = true;
            lblAiRiskLevelValue.Location = new System.Drawing.Point(370, 110);
            lblAiRiskLevelValue.Name = "lblAiRiskLevelValue";
            lblAiRiskLevelValue.Text = "-";

            lblAiRiskScore.AutoSize = true;
            lblAiRiskScore.Location = new System.Drawing.Point(290, 133);
            lblAiRiskScore.Name = "lblAiRiskScore";
            lblAiRiskScore.Text = "Risk score";

            progressRiskScore.Location = new System.Drawing.Point(370, 136);
            progressRiskScore.Name = "progressRiskScore";
            progressRiskScore.Size = new System.Drawing.Size(110, 14);

            lblAiRiskScoreValue.AutoSize = true;
            lblAiRiskScoreValue.Location = new System.Drawing.Point(370, 153);
            lblAiRiskScoreValue.Name = "lblAiRiskScoreValue";
            lblAiRiskScoreValue.Text = "0";

            lblAiRiskReasonsTitle.AutoSize = true;
            lblAiRiskReasonsTitle.Location = new System.Drawing.Point(290, 162);
            lblAiRiskReasonsTitle.Name = "lblAiRiskReasonsTitle";
            lblAiRiskReasonsTitle.Text = "Risk reasons";

            lstAiRiskReasons.FormattingEnabled = true;
            lstAiRiskReasons.ItemHeight = 20;
            lstAiRiskReasons.Location = new System.Drawing.Point(290, 186);
            lstAiRiskReasons.Name = "lstAiRiskReasons";
            lstAiRiskReasons.Size = new System.Drawing.Size(190, 64);
            lstAiRiskReasons.Dock = System.Windows.Forms.DockStyle.Fill;
            lstAiRiskReasons.Margin = new System.Windows.Forms.Padding(0, 4, 0, 8);

            lblAiEntitiesTitle.AutoSize = true;
            lblAiEntitiesTitle.Location = new System.Drawing.Point(290, 260);
            lblAiEntitiesTitle.Name = "lblAiEntitiesTitle";
            lblAiEntitiesTitle.Text = "Entities";

            lblAiPeople.AutoSize = true;
            lblAiPeople.Location = new System.Drawing.Point(290, 284);
            lblAiPeople.Name = "lblAiPeople";
            lblAiPeople.Text = "People";

            lstAiPeople.FormattingEnabled = true;
            lstAiPeople.ItemHeight = 20;
            lstAiPeople.Location = new System.Drawing.Point(290, 307);
            lstAiPeople.Name = "lstAiPeople";
            lstAiPeople.Size = new System.Drawing.Size(90, 44);
            lstAiPeople.Dock = System.Windows.Forms.DockStyle.Fill;
            lstAiPeople.Margin = new System.Windows.Forms.Padding(0, 4, 6, 6);

            lblAiPlaces.AutoSize = true;
            lblAiPlaces.Location = new System.Drawing.Point(385, 284);
            lblAiPlaces.Name = "lblAiPlaces";
            lblAiPlaces.Text = "Places";

            lstAiPlaces.FormattingEnabled = true;
            lstAiPlaces.ItemHeight = 20;
            lstAiPlaces.Location = new System.Drawing.Point(385, 307);
            lstAiPlaces.Name = "lstAiPlaces";
            lstAiPlaces.Size = new System.Drawing.Size(95, 44);
            lstAiPlaces.Dock = System.Windows.Forms.DockStyle.Fill;
            lstAiPlaces.Margin = new System.Windows.Forms.Padding(0, 4, 0, 6);

            lblAiDatesTimes.AutoSize = true;
            lblAiDatesTimes.Location = new System.Drawing.Point(290, 356);
            lblAiDatesTimes.Name = "lblAiDatesTimes";
            lblAiDatesTimes.Text = "Dates/Times";

            lstAiDatesTimes.FormattingEnabled = true;
            lstAiDatesTimes.ItemHeight = 20;
            lstAiDatesTimes.Location = new System.Drawing.Point(290, 379);
            lstAiDatesTimes.Name = "lstAiDatesTimes";
            lstAiDatesTimes.Size = new System.Drawing.Size(90, 44);
            lstAiDatesTimes.Dock = System.Windows.Forms.DockStyle.Fill;
            lstAiDatesTimes.Margin = new System.Windows.Forms.Padding(0, 4, 6, 0);

            lblAiItems.AutoSize = true;
            lblAiItems.Location = new System.Drawing.Point(385, 356);
            lblAiItems.Name = "lblAiItems";
            lblAiItems.Text = "Items";

            lstAiItems.FormattingEnabled = true;
            lstAiItems.ItemHeight = 20;
            lstAiItems.Location = new System.Drawing.Point(385, 379);
            lstAiItems.Name = "lstAiItems";
            lstAiItems.Size = new System.Drawing.Size(95, 44);
            lstAiItems.Dock = System.Windows.Forms.DockStyle.Fill;
            lstAiItems.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);

            lblAiNextActionTitle.AutoSize = true;
            lblAiNextActionTitle.Location = new System.Drawing.Point(14, 276);
            lblAiNextActionTitle.Name = "lblAiNextActionTitle";
            lblAiNextActionTitle.Text = "Recommended action";

            txtAiNextAction.Location = new System.Drawing.Point(14, 299);
            txtAiNextAction.Name = "txtAiNextAction";
            txtAiNextAction.Multiline = true;
            txtAiNextAction.ReadOnly = true;
            txtAiNextAction.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtAiNextAction.Size = new System.Drawing.Size(260, 50);
            txtAiNextAction.Dock = System.Windows.Forms.DockStyle.Fill;
            txtAiNextAction.Margin = new System.Windows.Forms.Padding(0, 4, 8, 0);

            aiLeftLayout.Controls.Add(lblAiSummaryTitle, 0, 0);
            aiLeftLayout.Controls.Add(txtAiSummary, 0, 1);
            aiLeftLayout.Controls.Add(lblAiKeyPointsTitle, 0, 2);
            aiLeftLayout.Controls.Add(lstAiKeyPoints, 0, 3);
            aiLeftLayout.Controls.Add(lblAiNextActionTitle, 0, 4);
            aiLeftLayout.Controls.Add(txtAiNextAction, 0, 5);

            aiRiskGrid.Controls.Add(lblAiCategory, 0, 0);
            aiRiskGrid.Controls.Add(lblAiCategoryValue, 1, 0);
            aiRiskGrid.Controls.Add(lblAiConfidence, 0, 1);
            aiRiskGrid.Controls.Add(lblAiConfidenceValue, 1, 1);
            aiRiskGrid.Controls.Add(lblAiRiskLevel, 0, 2);
            aiRiskGrid.Controls.Add(lblAiRiskLevelValue, 1, 2);
            aiRiskGrid.Controls.Add(lblAiRiskScore, 0, 3);
            aiRiskScorePanel.Controls.Add(progressRiskScore);
            aiRiskScorePanel.Controls.Add(lblAiRiskScoreValue);
            aiRiskGrid.Controls.Add(aiRiskScorePanel, 1, 3);

            aiEntitiesGrid.ColumnCount = 2;
            aiEntitiesGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            aiEntitiesGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            aiEntitiesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            aiEntitiesGrid.RowCount = 4;
            aiEntitiesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiEntitiesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            aiEntitiesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            aiEntitiesGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            aiEntitiesGrid.Controls.Add(lblAiPeople, 0, 0);
            aiEntitiesGrid.Controls.Add(lblAiPlaces, 1, 0);
            aiEntitiesGrid.Controls.Add(lstAiPeople, 0, 1);
            aiEntitiesGrid.Controls.Add(lstAiPlaces, 1, 1);
            aiEntitiesGrid.Controls.Add(lblAiDatesTimes, 0, 2);
            aiEntitiesGrid.Controls.Add(lblAiItems, 1, 2);
            aiEntitiesGrid.Controls.Add(lstAiDatesTimes, 0, 3);
            aiEntitiesGrid.Controls.Add(lstAiItems, 1, 3);

            aiRightLayout.Controls.Add(aiRiskGrid, 0, 0);
            aiRightLayout.Controls.Add(lblAiRiskReasonsTitle, 0, 1);
            aiRightLayout.Controls.Add(lstAiRiskReasons, 0, 2);
            aiRightLayout.Controls.Add(lblAiEntitiesTitle, 0, 3);
            aiRightLayout.Controls.Add(aiEntitiesGrid, 0, 4);

            aiLayout.Controls.Add(aiLeftLayout, 0, 0);
            aiLayout.Controls.Add(aiRightLayout, 1, 0);

            grpAiAnalysis.Controls.Add(aiLayout);
            grpAiAnalysis.Controls.Add(aiHeaderPanel);


            AcceptButton = btnSave;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = false;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(970, 1040);
            leftPanel.Controls.Add(buttonPanel);
            leftPanel.Controls.Add(formTable);
            leftPanel.Controls.Add(headerPanel);
            rightPanel.Controls.Add(grpAiAnalysis);
            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);
            Controls.Add(mainLayout);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MinimumSize = new System.Drawing.Size(988, 1080);
            Name = "BlotterForm";
            Padding = new System.Windows.Forms.Padding(21, 24, 21, 24);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "File Blotter";

            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            rightPanel.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            leftPanel.PerformLayout();
            mainLayout.ResumeLayout(false);
            formTable.ResumeLayout(false);
            formTable.PerformLayout();
            respondentPanel.ResumeLayout(false);
            respondentPanel.PerformLayout();
            respondentFields.ResumeLayout(false);
            respondentFields.PerformLayout();
            respondentChoice.ResumeLayout(false);
            respondentChoice.PerformLayout();
            buttonPanel.ResumeLayout(false);
            buttonPanel.PerformLayout();
            aiEntitiesGrid.ResumeLayout(false);
            aiEntitiesGrid.PerformLayout();
            aiRiskScorePanel.ResumeLayout(false);
            aiRiskScorePanel.PerformLayout();
            aiRiskGrid.ResumeLayout(false);
            aiRiskGrid.PerformLayout();
            aiRightLayout.ResumeLayout(false);
            aiRightLayout.PerformLayout();
            aiLeftLayout.ResumeLayout(false);
            aiLeftLayout.PerformLayout();
            aiLayout.ResumeLayout(false);
            aiHeaderPanel.ResumeLayout(false);
            aiHeaderPanel.PerformLayout();
            grpAiAnalysis.ResumeLayout(false);
            grpAiAnalysis.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
