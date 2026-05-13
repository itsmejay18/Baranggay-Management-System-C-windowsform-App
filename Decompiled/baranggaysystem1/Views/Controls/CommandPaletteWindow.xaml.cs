using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// VS Code-style command palette for quick navigation and actions.
/// Triggered by Ctrl+Shift+P. Provides fuzzy search over all available
/// pages, actions, and dialogs.
/// </summary>
public partial class CommandPaletteWindow : Window
{
    private readonly List<CommandPaletteItem> _allItems;

    /// <summary>
    /// The selected command's action identifier.
    /// </summary>
    public string? SelectedAction { get; private set; }

    /// <summary>
    /// Whether the selection is a navigation route or a dialog action.
    /// </summary>
    public CommandPaletteActionType SelectedType { get; private set; }

    public CommandPaletteWindow()
    {
        InitializeComponent();
        _allItems = BuildCommandList();
        resultsList.ItemsSource = _allItems;

        Loaded += (s, e) =>
        {
            searchBox.Focus();
        };

        // Close on Escape
        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };

        // Close on deactivation
        Deactivated += (s, e) =>
        {
            if (IsVisible)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = searchBox.Text?.Trim().ToLowerInvariant() ?? "";

        if (string.IsNullOrEmpty(query))
        {
            resultsList.ItemsSource = _allItems;
        }
        else
        {
            var filtered = _allItems
                .Where(item =>
                    item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            resultsList.ItemsSource = filtered;
        }

        if (resultsList.Items.Count > 0)
            resultsList.SelectedIndex = 0;
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (resultsList.SelectedIndex < resultsList.Items.Count - 1)
                    resultsList.SelectedIndex++;
                e.Handled = true;
                break;

            case Key.Up:
                if (resultsList.SelectedIndex > 0)
                    resultsList.SelectedIndex--;
                e.Handled = true;
                break;

            case Key.Enter:
                ExecuteSelected();
                e.Handled = true;
                break;
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Just visual selection, no action
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteSelected();
    }

    private void ExecuteSelected()
    {
        if (resultsList.SelectedItem is CommandPaletteItem item)
        {
            SelectedAction = item.Action;
            SelectedType = item.Type;
            DialogResult = true;
            Close();
        }
    }

    private static List<CommandPaletteItem> BuildCommandList()
    {
        return new List<CommandPaletteItem>
        {
            // Navigation
            new("🏠", "Home", "Go to home landing page", "Home", CommandPaletteActionType.Navigate, "", new[] { "home", "landing" }),
            new("📊", "Dashboard", "View KPIs and notifications", "DashboardNotifications", CommandPaletteActionType.Navigate, "Ctrl+D", new[] { "dashboard", "kpi", "stats" }),
            new("👥", "Resident Records", "Manage resident profiles", "ResidentWorkspace", CommandPaletteActionType.Navigate, "Ctrl+Shift+R", new[] { "residents", "people", "profiles" }),
            new("🏘️", "Households", "Manage household records", "Households", CommandPaletteActionType.Navigate, "", new[] { "household", "family", "address" }),
            new("📋", "Clearances", "Certificate request queue", "Clearances", CommandPaletteActionType.Navigate, "", new[] { "clearance", "certificate", "document" }),
            new("🛡️", "Permits", "Permits queue", "Permits", CommandPaletteActionType.Navigate, "", new[] { "permit", "business" }),
            new("⚖️", "Blotter Cases", "Case management", "ResidentCases", CommandPaletteActionType.Navigate, "", new[] { "blotter", "case", "dispute", "complaint" }),
            new("💰", "Payments", "Payment ledger", "ResidentPayments", CommandPaletteActionType.Navigate, "", new[] { "payment", "fee", "collection" }),
            new("🤝", "Ayuda Assistance", "Aid program management", "Ayuda", CommandPaletteActionType.Navigate, "", new[] { "ayuda", "aid", "assistance", "4ps" }),
            new("📦", "Finance Operations", "Expenses, inventory, assets", "Collections", CommandPaletteActionType.Navigate, "", new[] { "finance", "expense", "inventory", "asset" }),
            new("📢", "Announcements & Projects", "Manage announcements", "GovernanceRegistry", CommandPaletteActionType.Navigate, "", new[] { "announcement", "project", "news" }),
            new("⭐", "Barangay Officials", "Official records", "Officials", CommandPaletteActionType.Navigate, "", new[] { "official", "captain", "kagawad" }),
            new("👤", "Staff & Users", "User account management", "StaffUsers", CommandPaletteActionType.Navigate, "", new[] { "staff", "user", "account" }),
            new("🔑", "Roles & Permissions", "RBAC management", "RolePermissions", CommandPaletteActionType.Navigate, "", new[] { "role", "permission", "access" }),
            new("📝", "System Logs", "View audit trail", "SystemLogs", CommandPaletteActionType.Navigate, "", new[] { "log", "audit", "history" }),
            new("✉️", "Notification Outbox", "View sent notifications", "NotificationOutbox", CommandPaletteActionType.Navigate, "", new[] { "notification", "email", "sms", "outbox" }),
            new("⚙️", "Settings", "System configuration", "Settings", CommandPaletteActionType.Navigate, "Ctrl+,", new[] { "settings", "config", "branding" }),
            new("📈", "Reports", "Analytics and reports", "Reports", CommandPaletteActionType.Navigate, "", new[] { "report", "analytics", "chart", "trend" }),

            // Actions
            new("🔍", "Global Search", "Search across all modules", "GlobalSearch", CommandPaletteActionType.Dialog, "Ctrl+K", new[] { "search", "find", "lookup" }),
            new("🤖", "AI Assistant (Ellie)", "Ask Ellie for help", "EllieAssistant", CommandPaletteActionType.Dialog, "F1", new[] { "ai", "ellie", "help", "assistant" }),
            new("➕", "New Resident", "Add a new resident", "NewResident", CommandPaletteActionType.Dialog, "Ctrl+N", new[] { "new", "add", "create", "resident" }),
            new("📄", "New Certificate Request", "Create certificate request", "NewCertificate", CommandPaletteActionType.Dialog, "Ctrl+Shift+N", new[] { "new", "certificate", "clearance", "request" }),
            new("📥", "Bulk Import", "Import residents from CSV", "BulkImport", CommandPaletteActionType.Dialog, "Ctrl+Shift+I", new[] { "import", "csv", "bulk", "upload" }),
            new("📤", "Export Data", "Export current view", "Export", CommandPaletteActionType.Dialog, "Ctrl+Shift+E", new[] { "export", "download", "csv", "excel" }),
            new("🔒", "Lock Session", "Lock the screen", "LockSession", CommandPaletteActionType.Dialog, "Ctrl+L", new[] { "lock", "secure", "away" }),
            new("🔑", "Change Password", "Update your password", "ChangePassword", CommandPaletteActionType.Dialog, "F12", new[] { "password", "change", "security" }),
            new("🔔", "Notification Settings", "Configure email/SMS", "NotificationSettings", CommandPaletteActionType.Dialog, "", new[] { "notification", "smtp", "sms", "email", "settings" }),
            new("❓", "Security Questions", "Set up recovery questions", "SecurityQuestions", CommandPaletteActionType.Dialog, "", new[] { "security", "question", "recovery" }),
            new("🔄", "Refresh", "Reload current page", "Refresh", CommandPaletteActionType.Dialog, "F5", new[] { "refresh", "reload", "update" }),
        };
    }
}

/// <summary>
/// A single item in the command palette.
/// </summary>
public sealed class CommandPaletteItem
{
    public string Icon { get; }
    public string Title { get; }
    public string Description { get; }
    public string Action { get; }
    public CommandPaletteActionType Type { get; }
    public string Shortcut { get; }
    public string[] Keywords { get; }

    public CommandPaletteItem(string icon, string title, string description, string action,
        CommandPaletteActionType type, string shortcut, string[] keywords)
    {
        Icon = icon;
        Title = title;
        Description = description;
        Action = action;
        Type = type;
        Shortcut = shortcut;
        Keywords = keywords;
    }
}

public enum CommandPaletteActionType
{
    Navigate,
    Dialog
}
