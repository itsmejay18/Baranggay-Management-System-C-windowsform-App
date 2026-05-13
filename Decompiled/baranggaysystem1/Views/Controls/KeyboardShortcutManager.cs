using System;
using System.Windows;
using System.Windows.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Global keyboard shortcut manager for the application.
/// Provides quick access to common actions for power users.
/// 
/// Shortcuts:
///   Ctrl+K       → Global Search
///   Ctrl+N       → New Resident
///   Ctrl+Shift+N → New Certificate Request
///   Ctrl+L       → Lock Session
///   Ctrl+Shift+I → Bulk Import
///   Ctrl+Shift+E → Export (context-dependent)
///   F1           → Ellie AI Assistant
///   F5           → Refresh current page
///   Ctrl+,       → Settings
///   Ctrl+Shift+P → Command Palette (quick navigation)
///   Escape       → Close current dialog/popup
/// </summary>
internal static class KeyboardShortcutManager
{
    private static bool _initialized;

    /// <summary>
    /// Fired when a shortcut triggers navigation to a route.
    /// </summary>
    public static event Action<string>? NavigateRequested;

    /// <summary>
    /// Fired when a shortcut triggers opening a dialog.
    /// </summary>
    public static event Action<string>? DialogRequested;

    /// <summary>
    /// Fired when refresh is requested.
    /// </summary>
    public static event Action? RefreshRequested;

    /// <summary>
    /// Initialize keyboard shortcuts on the main window.
    /// Call once after MainWindow is loaded.
    /// </summary>
    public static void Initialize(Window mainWindow)
    {
        if (_initialized || mainWindow == null) return;
        _initialized = true;

        // Register input bindings
        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("GlobalSearch")),
            Key.K, ModifierKeys.Control));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("NewResident")),
            Key.N, ModifierKeys.Control));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("NewCertificate")),
            Key.N, ModifierKeys.Control | ModifierKeys.Shift));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("LockSession")),
            Key.L, ModifierKeys.Control));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("BulkImport")),
            Key.I, ModifierKeys.Control | ModifierKeys.Shift));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("Export")),
            Key.E, ModifierKeys.Control | ModifierKeys.Shift));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("EllieAssistant")),
            Key.F1, ModifierKeys.None));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => RefreshRequested?.Invoke()),
            Key.F5, ModifierKeys.None));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => NavigateRequested?.Invoke("Settings")),
            Key.OemComma, ModifierKeys.Control));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("CommandPalette")),
            Key.P, ModifierKeys.Control | ModifierKeys.Shift));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => NavigateRequested?.Invoke("Dashboard")),
            Key.D, ModifierKeys.Control));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => NavigateRequested?.Invoke("ResidentWorkspace")),
            Key.R, ModifierKeys.Control | ModifierKeys.Shift));

        mainWindow.InputBindings.Add(new KeyBinding(
            new RelayShortcutCommand(() => DialogRequested?.Invoke("ChangePassword")),
            Key.F12, ModifierKeys.None));
    }

    /// <summary>
    /// Get a formatted list of all shortcuts for display in help/tooltip.
    /// </summary>
    public static string GetShortcutHelp()
    {
        return @"Keyboard Shortcuts:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Ctrl+K              Global Search
Ctrl+N              New Resident
Ctrl+Shift+N        New Certificate Request
Ctrl+L              Lock Session
Ctrl+Shift+I        Bulk Import
Ctrl+Shift+E        Export
Ctrl+D              Dashboard
Ctrl+Shift+R        Residents
Ctrl+Shift+P        Command Palette
Ctrl+,              Settings
F1                  AI Assistant (Ellie)
F5                  Refresh Page
F12                 Change Password
Esc                 Close Dialog";
    }
}

/// <summary>
/// Simple ICommand implementation for keyboard shortcuts.
/// </summary>
internal sealed class RelayShortcutCommand : ICommand
{
    private readonly Action _execute;

    public RelayShortcutCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
