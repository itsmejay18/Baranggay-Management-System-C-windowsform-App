using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace baranggaysystem1.helper;

internal sealed class DynamicSidebarController : IDisposable
{
    private readonly Form _hostForm;
    private readonly Panel _sidebarPanel;
    private readonly IconButton _toggleButton;
    private readonly IconButton[] _sidebarButtons;
    private readonly int _minExpandedWidth;
    private readonly int _autoHideDelayMs;
    private readonly int _leftEdgePixels;
    private readonly int _animationStep;
    private readonly System.Windows.Forms.Timer _autoTimer = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new();

    private int _expandedWidth;
    private int _targetWidth;
    private bool _isExpanded = true;
    private DateTime _leaveStartedUtc = DateTime.MinValue;
    private DateTime _animationStartedUtc = DateTime.MinValue;
    private int _animationFromWidth;
    private int _animationToWidth;
    private int _animationDurationMs = 180;
    private bool _disposed;

    public DynamicSidebarController(
        Form hostForm,
        Panel sidebarPanel,
        IconButton toggleButton,
        IEnumerable<IconButton> sidebarButtons,
        SidebarBehaviorSettings? settings = null)
    {
        _hostForm = hostForm ?? throw new ArgumentNullException(nameof(hostForm));
        _sidebarPanel = sidebarPanel ?? throw new ArgumentNullException(nameof(sidebarPanel));
        _toggleButton = toggleButton ?? throw new ArgumentNullException(nameof(toggleButton));
        _sidebarButtons = sidebarButtons?.ToArray() ?? Array.Empty<IconButton>();
        var value = settings ?? SidebarBehaviorSettings.CreateDefault();
        _minExpandedWidth = Math.Max(100, value.MinExpandedWidth);
        _autoHideDelayMs = Math.Max(300, value.AutoHideDelayMs);
        _leftEdgePixels = Math.Max(2, value.LeftEdgePixels);
        _animationStep = Math.Max(8, value.AnimationStep);
    }

    public void Initialize()
    {
        ThrowIfDisposed();

        // Always honor the configured expanded width so behavior is identical across forms.
        _expandedWidth = _minExpandedWidth;
        bool wasExpanded = _sidebarPanel.Visible && _sidebarPanel.Width > 0;
        _isExpanded = wasExpanded;
        _targetWidth = wasExpanded ? _expandedWidth : 0;

        foreach (var button in _sidebarButtons)
        {
            if (button.Tag is not string tag || string.IsNullOrWhiteSpace(tag))
            {
                button.Tag = button.Text;
            }
        }

        StyleToggleButton();
        _sidebarPanel.Width = _targetWidth;
        _sidebarPanel.Visible = _isExpanded;
        _sidebarPanel.Padding = _isExpanded ? new Padding(12, 20, 12, 12) : new Padding(0);
        ApplySidebarButtonState(_isExpanded);

        _autoTimer.Tick -= AutoTimerTick;
        _animationTimer.Tick -= AnimationTimerTick;
        _toggleButton.Click -= ToggleButtonClick;
        _hostForm.FormClosed -= HostFormClosed;

        _autoTimer.Interval = 120;
        _animationTimer.Interval = 15;

        _autoTimer.Tick += AutoTimerTick;
        _animationTimer.Tick += AnimationTimerTick;
        _toggleButton.Click += ToggleButtonClick;
        _hostForm.FormClosed += HostFormClosed;

        _autoTimer.Start();
    }

    private void AutoTimerTick(object? sender, EventArgs e)
    {
        if (_disposed || !_hostForm.Visible || _hostForm.IsDisposed)
        {
            return;
        }

        bool pointerInsideToggle = IsPointerInside(_toggleButton);
        bool pointerInsideSidebar = IsPointerInside(_sidebarPanel);
        bool pointerAtLeftEdge = IsPointerInLeftEdgeActivationZone();

        if (_isExpanded)
        {
            if (pointerInsideSidebar || pointerInsideToggle)
            {
                _leaveStartedUtc = DateTime.MinValue;
                return;
            }

            if (_leaveStartedUtc == DateTime.MinValue)
            {
                _leaveStartedUtc = DateTime.UtcNow;
                return;
            }

            if ((DateTime.UtcNow - _leaveStartedUtc).TotalMilliseconds >= _autoHideDelayMs)
            {
                BeginSidebarAnimation(false);
            }

            return;
        }

        if (pointerInsideSidebar || pointerInsideToggle || pointerAtLeftEdge)
        {
            BeginSidebarAnimation(true);
            _leaveStartedUtc = DateTime.MinValue;
        }
    }

    private void ToggleButtonClick(object? sender, EventArgs e)
    {
        _leaveStartedUtc = DateTime.MinValue;
        BeginSidebarAnimation(!_isExpanded);
    }

    private void BeginSidebarAnimation(bool expand)
    {
        int target = expand ? _expandedWidth : 0;
        if (_targetWidth == target && _animationTimer.Enabled)
        {
            return;
        }

        _targetWidth = target;
        _animationFromWidth = Math.Max(0, _sidebarPanel.Width);
        _animationToWidth = target;

        int distance = Math.Abs(_animationToWidth - _animationFromWidth);
        // Convert user speed setting to a stable px/sec velocity.
        int pixelsPerSecond = Math.Clamp(_animationStep * 40, 320, 2400);
        _animationDurationMs = distance == 0
            ? 1
            : Math.Max(120, (int)Math.Round(distance * 1000.0 / pixelsPerSecond));
        _animationStartedUtc = DateTime.UtcNow;

        if (expand)
        {
            _sidebarPanel.Visible = true;
            _sidebarPanel.Width = Math.Max(0, _sidebarPanel.Width);
            _sidebarPanel.Padding = new Padding(12, 20, 12, 12);
            ApplySidebarButtonState(true);
        }
        else
        {
            ApplySidebarButtonState(false);
        }

        _animationTimer.Start();
    }

    private void AnimationTimerTick(object? sender, EventArgs e)
    {
        if (_disposed || !_hostForm.Visible || _hostForm.IsDisposed)
        {
            _animationTimer.Stop();
            return;
        }

        double elapsedMs = (DateTime.UtcNow - _animationStartedUtc).TotalMilliseconds;
        double t = _animationDurationMs <= 0 ? 1.0 : Math.Clamp(elapsedMs / _animationDurationMs, 0.0, 1.0);
        double eased = EaseInOutCubic(t);
        int next = (int)Math.Round(_animationFromWidth + ((_animationToWidth - _animationFromWidth) * eased));
        next = Math.Clamp(next, 0, _expandedWidth);

        if (next > 0 && !_sidebarPanel.Visible)
        {
            _sidebarPanel.Visible = true;
        }

        _sidebarPanel.Width = next;
        _sidebarPanel.Padding = next == 0 ? new Padding(0) : new Padding(12, 20, 12, 12);

        if (t >= 1.0 || next == _targetWidth)
        {
            _animationTimer.Stop();
            _isExpanded = _targetWidth > 0;
            _sidebarPanel.Visible = _isExpanded;
            ApplySidebarButtonState(_isExpanded);
            return;
        }

        _isExpanded = next > 0;
    }

    private static double EaseInOutCubic(double t)
    {
        return t < 0.5
            ? 4 * t * t * t
            : 1 - Math.Pow(-2 * t + 2, 3) / 2;
    }

    private void ApplySidebarButtonState(bool expanded)
    {
        foreach (var button in _sidebarButtons)
        {
            string label = button.Tag as string ?? string.Empty;
            button.Text = expanded ? label : string.Empty;
            button.TextAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            button.ImageAlign = expanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            button.TextImageRelation = expanded ? TextImageRelation.ImageBeforeText : TextImageRelation.Overlay;
            button.Padding = expanded ? new Padding(4, 0, 0, 0) : Padding.Empty;
            button.IconSize = expanded ? 20 : 22;
        }
    }

    private bool IsPointerInside(Control control)
    {
        var mouse = Control.MousePosition;
        var screenRect = control.RectangleToScreen(control.ClientRectangle);
        return screenRect.Contains(mouse);
    }

    private bool IsPointerInLeftEdgeActivationZone()
    {
        var point = _hostForm.PointToClient(Control.MousePosition);
        return point.Y >= 0 && point.Y <= _hostForm.ClientSize.Height && point.X >= 0 && point.X <= _leftEdgePixels;
    }

    private void StyleToggleButton()
    {
        _toggleButton.FlatStyle = FlatStyle.Flat;
        _toggleButton.FlatAppearance.BorderSize = 0;
        _toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 15, 23, 42);
        _toggleButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 15, 23, 42);
        _toggleButton.BackColor = Color.Transparent;
        _toggleButton.IconChar = IconChar.Bars;
        _toggleButton.IconColor = UiTheme.Slate700;
        _toggleButton.IconFont = IconFont.Auto;
        _toggleButton.IconSize = 18;
        _toggleButton.Text = string.Empty;
        _toggleButton.Cursor = Cursors.Hand;
        _toggleButton.BringToFront();
    }

    private void HostFormClosed(object? sender, FormClosedEventArgs e)
    {
        Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DynamicSidebarController));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _autoTimer.Stop();
        _animationTimer.Stop();
        _autoTimer.Tick -= AutoTimerTick;
        _animationTimer.Tick -= AnimationTimerTick;
        _toggleButton.Click -= ToggleButtonClick;
        _hostForm.FormClosed -= HostFormClosed;
        _autoTimer.Dispose();
        _animationTimer.Dispose();
    }
}
