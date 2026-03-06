using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using baranggaysystem1.helper;

namespace baranggaysystem1
{
    public enum ResidentsView
    {
        Profile,
        Blotter,
        Certificates,
        History
    }

    public enum CertificateAction
    {
        None,
        NewRequest,
        EditRequest,
        Approve,
        Issue,
        Print,
        Export,
        Cancel,
        Refresh
    }

    public partial class Residents : Form
    {
        private enum ResidentsModuleSection
        {
            ResidentsRegistry,
            Households,
            TagsAndCategories,
            DeceasedRegistry
        }

        private readonly ResidentsView _initialView;
        private readonly CertificateAction _certificateAction;
        private DynamicSidebarController? _sidebarController;
        private bool _embedLayoutRefreshPending;

        private readonly Panel _moduleNavHost = new Panel();
        private readonly TableLayoutPanel _moduleNavLayout = new TableLayoutPanel();
        private readonly Panel _moduleContentHost = new Panel();
        private readonly Panel _modulePlaceholderHost = new Panel();
        private readonly Label _modulePlaceholderTitle = new Label();
        private readonly Label _modulePlaceholderMessage = new Label();
        private readonly Button _moduleNavRegistryButton = new Button();
        private readonly Button _moduleNavHouseholdsButton = new Button();
        private readonly Button _moduleNavTagsButton = new Button();
        private readonly Button _moduleNavDeceasedButton = new Button();
        private readonly Dictionary<ResidentsModuleSection, Button> _moduleNavButtons = new Dictionary<ResidentsModuleSection, Button>();
        private readonly Font _moduleNavFontRegular = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        private readonly Font _moduleNavFontBold = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        private bool _moduleNavigationBuilt;
        private bool _suppressRouteSync;
        private ResidentsModuleSection _currentSection = ResidentsModuleSection.ResidentsRegistry;
        private string _currentRoute = "/residents";

        public string CurrentRoute => _currentRoute;

        public Residents(ResidentsView view = ResidentsView.Profile, CertificateAction certificateAction = CertificateAction.None)
        {
            InitializeComponent();
            _initialView = view;
            _certificateAction = certificateAction;
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            ConfigureDynamicSidebar();
            BuildResidentsModuleNavigation();
            WireResidentsRouting();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            ShowView(_initialView);
            if (_initialView == ResidentsView.Certificates && _certificateAction != CertificateAction.None)
            {
                ExecuteCertificateAction(_certificateAction);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || !IsHandleCreated)
            {
                return;
            }

            if (_currentSection == ResidentsModuleSection.ResidentsRegistry)
            {
                residentModuleControl.RefreshLayoutNow();
            }
        }

        private void ConfigureDynamicSidebar()
        {
            _sidebarController?.Dispose();
            _sidebarController = null;

            panelSidebar.Visible = false;
            panelSidebar.Enabled = false;
            panelSidebar.Dock = DockStyle.None;
            panelSidebar.Width = 0;
            panelSidebar.Padding = Padding.Empty;

            sidebarToggleButton.Visible = false;
            sidebarToggleButton.Enabled = false;

            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = Point.Empty;
            mainPanel.Padding = Padding.Empty;
        }

        private void BuildResidentsModuleNavigation()
        {
            if (_moduleNavigationBuilt)
            {
                return;
            }

            _moduleNavigationBuilt = true;

            _moduleNavHost.Dock = DockStyle.Top;
            _moduleNavHost.Height = 56;
            _moduleNavHost.Padding = new Padding(16, 10, 16, 8);
            _moduleNavHost.BackColor = Color.FromArgb(244, 246, 249);

            _moduleNavLayout.Dock = DockStyle.Fill;
            _moduleNavLayout.ColumnCount = 4;
            _moduleNavLayout.RowCount = 1;
            _moduleNavLayout.Margin = Padding.Empty;
            _moduleNavLayout.Padding = Padding.Empty;
            _moduleNavLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _moduleNavLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _moduleNavLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _moduleNavLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            _moduleNavLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ConfigureModuleNavButton(_moduleNavRegistryButton, "Residents Registry");
            ConfigureModuleNavButton(_moduleNavHouseholdsButton, "Households");
            ConfigureModuleNavButton(_moduleNavTagsButton, "Tags & Categories");
            ConfigureModuleNavButton(_moduleNavDeceasedButton, "Deceased Registry");

            _moduleNavRegistryButton.Click += (_, __) => NavigateToRoute("/residents");
            _moduleNavHouseholdsButton.Click += (_, __) => NavigateToRoute("/residents/households");
            _moduleNavTagsButton.Click += (_, __) => NavigateToRoute("/residents/tags");
            _moduleNavDeceasedButton.Click += (_, __) => NavigateToRoute("/residents/deceased");

            _moduleNavLayout.Controls.Add(_moduleNavRegistryButton, 0, 0);
            _moduleNavLayout.Controls.Add(_moduleNavHouseholdsButton, 1, 0);
            _moduleNavLayout.Controls.Add(_moduleNavTagsButton, 2, 0);
            _moduleNavLayout.Controls.Add(_moduleNavDeceasedButton, 3, 0);
            _moduleNavHost.Controls.Add(_moduleNavLayout);

            _moduleNavButtons[ResidentsModuleSection.ResidentsRegistry] = _moduleNavRegistryButton;
            _moduleNavButtons[ResidentsModuleSection.Households] = _moduleNavHouseholdsButton;
            _moduleNavButtons[ResidentsModuleSection.TagsAndCategories] = _moduleNavTagsButton;
            _moduleNavButtons[ResidentsModuleSection.DeceasedRegistry] = _moduleNavDeceasedButton;

            _moduleContentHost.Dock = DockStyle.Fill;
            _moduleContentHost.BackColor = Color.FromArgb(244, 246, 249);
            _moduleContentHost.Padding = Padding.Empty;

            BuildModulePlaceholderHost();

            mainPanel.SuspendLayout();
            try
            {
                mainPanel.Controls.Remove(sidebarToggleButton);
                mainPanel.Controls.Remove(residentModuleControl);

                residentModuleControl.Dock = DockStyle.Fill;
                residentModuleControl.Margin = Padding.Empty;
                residentModuleControl.Parent = null;

                _moduleContentHost.Controls.Clear();
                _moduleContentHost.Controls.Add(_modulePlaceholderHost);
                _moduleContentHost.Controls.Add(residentModuleControl);
                residentModuleControl.BringToFront();

                if (!mainPanel.Controls.Contains(_moduleContentHost))
                {
                    mainPanel.Controls.Add(_moduleContentHost);
                }

                if (!mainPanel.Controls.Contains(_moduleNavHost))
                {
                    mainPanel.Controls.Add(_moduleNavHost);
                }
            }
            finally
            {
                mainPanel.ResumeLayout(performLayout: true);
            }

            SetModuleSection(ResidentsModuleSection.ResidentsRegistry, updateRoute: false);
            UpdateCurrentRouteFromState();
        }

        private void BuildModulePlaceholderHost()
        {
            _modulePlaceholderHost.Dock = DockStyle.Fill;
            _modulePlaceholderHost.BackColor = Color.FromArgb(244, 246, 249);
            _modulePlaceholderHost.Padding = new Padding(20);
            _modulePlaceholderHost.Visible = false;

            var layout = new TableLayoutPanel
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

            var card = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(24),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.None
            };

            var stack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            _modulePlaceholderTitle.AutoSize = true;
            _modulePlaceholderTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point);
            _modulePlaceholderTitle.ForeColor = UiTheme.Slate900;
            _modulePlaceholderTitle.Margin = new Padding(0, 0, 0, 8);

            _modulePlaceholderMessage.AutoSize = true;
            _modulePlaceholderMessage.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            _modulePlaceholderMessage.ForeColor = UiTheme.Slate600;
            _modulePlaceholderMessage.MaximumSize = new Size(620, 0);
            _modulePlaceholderMessage.Margin = Padding.Empty;

            stack.Controls.Add(_modulePlaceholderTitle);
            stack.Controls.Add(_modulePlaceholderMessage);
            card.Controls.Add(stack);
            layout.Controls.Add(card, 0, 1);
            _modulePlaceholderHost.Controls.Add(layout);
        }

        private void WireResidentsRouting()
        {
            residentModuleControl.RouteChanged -= ResidentModuleControl_RouteChanged;
            residentModuleControl.RouteChanged += ResidentModuleControl_RouteChanged;
        }

        private void ResidentModuleControl_RouteChanged(object? sender, ResidentRouteChangedEventArgs e)
        {
            if (_suppressRouteSync || _currentSection != ResidentsModuleSection.ResidentsRegistry)
            {
                return;
            }

            UpdateCurrentRouteFromState(e.ResidentId, e.ProfileSegment);
        }

        private void ConfigureModuleNavButton(Button button, string text)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(0, 0, 8, 0);
            button.Padding = new Padding(10, 0, 10, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(206, 214, 224);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 245, 255);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(228, 238, 255);
            button.BackColor = Color.White;
            button.ForeColor = UiTheme.Slate700;
            button.Font = _moduleNavFontRegular;
            button.Height = 34;
            button.TabStop = true;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
        }

        private void SetModuleSection(ResidentsModuleSection section, bool updateRoute = true)
        {
            _currentSection = section;
            bool isRegistry = section == ResidentsModuleSection.ResidentsRegistry;

            residentModuleControl.Visible = isRegistry;
            _modulePlaceholderHost.Visible = !isRegistry;

            if (isRegistry)
            {
                residentModuleControl.BringToFront();
                residentModuleControl.RefreshLayoutNow();
            }
            else
            {
                ApplyPlaceholderContent(section);
                _modulePlaceholderHost.BringToFront();
            }

            UpdateModuleNavSelectionState();
            if (updateRoute)
            {
                UpdateCurrentRouteFromState();
            }
        }

        private void ApplyPlaceholderContent(ResidentsModuleSection section)
        {
            switch (section)
            {
                case ResidentsModuleSection.Households:
                    _modulePlaceholderTitle.Text = "Households";
                    _modulePlaceholderMessage.Text = "Manage households, family groupings, and address associations from this module.";
                    break;
                case ResidentsModuleSection.TagsAndCategories:
                    _modulePlaceholderTitle.Text = "Tags & Categories";
                    _modulePlaceholderMessage.Text = "Manage resident tags and category mappings for faster filtering and program targeting.";
                    break;
                case ResidentsModuleSection.DeceasedRegistry:
                    _modulePlaceholderTitle.Text = "Deceased Registry";
                    _modulePlaceholderMessage.Text = "Track deceased resident records and maintain status history in one dedicated registry.";
                    break;
                default:
                    _modulePlaceholderTitle.Text = "Residents Registry";
                    _modulePlaceholderMessage.Text = "Select a resident from the list to open the profile.";
                    break;
            }
        }

        private void UpdateModuleNavSelectionState()
        {
            foreach (var pair in _moduleNavButtons)
            {
                bool active = pair.Key == _currentSection;
                pair.Value.BackColor = active ? Color.FromArgb(225, 238, 255) : Color.White;
                pair.Value.ForeColor = active ? Color.FromArgb(24, 74, 178) : UiTheme.Slate700;
                pair.Value.FlatAppearance.BorderColor = active
                    ? Color.FromArgb(107, 149, 223)
                    : Color.FromArgb(206, 214, 224);
                pair.Value.Font = active ? _moduleNavFontBold : _moduleNavFontRegular;
            }
        }

        private static string ProfileSegmentFromView(ResidentsView view)
        {
            return view switch
            {
                ResidentsView.Blotter => "cases",
                ResidentsView.Certificates => "documents",
                ResidentsView.History => "activity",
                _ => "overview"
            };
        }

        private void UpdateCurrentRouteFromState(int? residentId = null, string? profileSegment = null)
        {
            if (_currentSection != ResidentsModuleSection.ResidentsRegistry)
            {
                _currentRoute = _currentSection switch
                {
                    ResidentsModuleSection.Households => "/residents/households",
                    ResidentsModuleSection.TagsAndCategories => "/residents/tags",
                    ResidentsModuleSection.DeceasedRegistry => "/residents/deceased",
                    _ => "/residents"
                };
                return;
            }

            int? targetResidentId = residentId ?? residentModuleControl.SelectedResidentId;
            if (!targetResidentId.HasValue)
            {
                _currentRoute = "/residents";
                return;
            }

            string segment = string.IsNullOrWhiteSpace(profileSegment)
                ? residentModuleControl.ActiveProfileRouteSegment
                : profileSegment.Trim().ToLowerInvariant();
            _currentRoute = string.Equals(segment, "overview", StringComparison.OrdinalIgnoreCase)
                ? $"/residents/{targetResidentId.Value}"
                : $"/residents/{targetResidentId.Value}/{segment}";
        }

        public void NavigateToRoute(string route)
        {
            string normalized = string.IsNullOrWhiteSpace(route) ? "/residents" : route.Trim();
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            string[] segments = normalized.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || !string.Equals(segments[0], "residents", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "/residents";
                segments = new[] { "residents" };
            }

            if (segments.Length == 1)
            {
                SetModuleSection(ResidentsModuleSection.ResidentsRegistry, updateRoute: false);
                _suppressRouteSync = true;
                try
                {
                    residentModuleControl.NavigateToProfileRoute("overview");
                }
                finally
                {
                    _suppressRouteSync = false;
                }
                UpdateCurrentRouteFromState();
                return;
            }

            string second = segments[1].Trim().ToLowerInvariant();
            switch (second)
            {
                case "households":
                    SetModuleSection(ResidentsModuleSection.Households);
                    return;
                case "tags":
                case "tags-categories":
                case "categories":
                    SetModuleSection(ResidentsModuleSection.TagsAndCategories);
                    return;
                case "deceased":
                case "deceased-registry":
                    SetModuleSection(ResidentsModuleSection.DeceasedRegistry);
                    return;
            }

            if (!int.TryParse(second, out int residentId))
            {
                SetModuleSection(ResidentsModuleSection.ResidentsRegistry);
                return;
            }

            string profileSegment = segments.Length >= 3 ? segments[2] : "overview";
            SetModuleSection(ResidentsModuleSection.ResidentsRegistry, updateRoute: false);

            _suppressRouteSync = true;
            try
            {
                bool selected = residentModuleControl.NavigateToResidentProfile(residentId, profileSegment);
                if (!selected)
                {
                    residentModuleControl.NavigateToProfileRoute("overview");
                }
            }
            finally
            {
                _suppressRouteSync = false;
            }

            UpdateCurrentRouteFromState();
        }

        public void ShowView(ResidentsView view)
        {
            SetModuleSection(ResidentsModuleSection.ResidentsRegistry, updateRoute: false);
            string profileSegment = ProfileSegmentFromView(view);

            _suppressRouteSync = true;
            try
            {
                residentModuleControl.NavigateToProfileRoute(profileSegment);
            }
            finally
            {
                _suppressRouteSync = false;
            }

            UpdateCurrentRouteFromState();
        }

        public void NavigateToResident(int residentId, ResidentsView view, int? certificateId = null, int? blotterId = null)
        {
            string profileSegment = ProfileSegmentFromView(view);
            SetModuleSection(ResidentsModuleSection.ResidentsRegistry, updateRoute: false);

            _suppressRouteSync = true;
            try
            {
                residentModuleControl.NavigateToResidentProfile(residentId, profileSegment);
            }
            finally
            {
                _suppressRouteSync = false;
            }

            if (view == ResidentsView.Certificates && certificateId.HasValue)
            {
                residentModuleControl.SelectCertificateById(certificateId.Value);
            }

            if (view == ResidentsView.Blotter && blotterId.HasValue)
            {
                residentModuleControl.SelectBlotterById(blotterId.Value);
            }

            UpdateCurrentRouteFromState();
        }

        public void ExecuteCertificateAction(CertificateAction action)
        {
            if (action == CertificateAction.None)
            {
                return;
            }

            SetModuleSection(ResidentsModuleSection.ResidentsRegistry, updateRoute: false);
            residentModuleControl.ShowCertificates();
            residentModuleControl.ExecuteCertificateAction(action);
            UpdateCurrentRouteFromState();
        }

        public void ConfigureForEmbeddedNavigation()
        {
            _sidebarController?.Dispose();
            _sidebarController = null;

            BuildResidentsModuleNavigation();

            SuspendLayout();
            panelSidebar.Visible = false;
            panelSidebar.Width = 0;
            sidebarToggleButton.Visible = false;
            sidebarToggleButton.Enabled = false;
            panelSidebar.Dock = DockStyle.None;
            mainPanel.Padding = Padding.Empty;
            mainPanel.Dock = DockStyle.Fill;
            _moduleNavHost.Dock = DockStyle.Top;
            _moduleContentHost.Dock = DockStyle.Fill;
            residentModuleControl.Margin = Padding.Empty;
            residentModuleControl.Dock = DockStyle.Fill;
            ResumeLayout(performLayout: true);
            PerformLayout();

            if (residentModuleControl.IsHandleCreated)
            {
                residentModuleControl.BeginInvoke(new Action(() =>
                {
                    PerformLayout();
                    residentModuleControl.RefreshLayoutNow();
                }));
            }
            else if (!_embedLayoutRefreshPending)
            {
                _embedLayoutRefreshPending = true;
                residentModuleControl.HandleCreated += ResidentModuleControl_HandleCreated;
            }
        }

        private void ResidentModuleControl_HandleCreated(object? sender, EventArgs e)
        {
            residentModuleControl.HandleCreated -= ResidentModuleControl_HandleCreated;
            _embedLayoutRefreshPending = false;
            residentModuleControl.RefreshLayoutNow();
        }

        private void OpenSidebarSettings()
        {
            if (!Permissions.CanOpenSettings)
            {
                ControllerDialogs.Warning("Only Admin users can open settings.");
                return;
            }

            using var form = new SidebarSettingsForm();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ConfigureDynamicSidebar();
            }
        }
    }
}
