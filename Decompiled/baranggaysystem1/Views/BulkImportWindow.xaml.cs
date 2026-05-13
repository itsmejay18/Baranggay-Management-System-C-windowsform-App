using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using baranggaysystem1.Services;

namespace baranggaysystem1.Views;

/// <summary>
/// Bulk import dialog for importing residents from CSV files.
/// Provides file selection, validation preview, and import execution.
/// </summary>
public partial class BulkImportWindow : Window
{
    private string? _selectedFilePath;
    private List<BulkImportRow>? _validRows;

    public int ImportedCount { get; private set; }

    public BulkImportWindow()
    {
        InitializeComponent();
    }

    private void SelectFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select CSV File for Import",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedFilePath = dialog.FileName;
            filePathLabel.Text = _selectedFilePath;
            ParseFile();
        }
    }

    private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder to save the template"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            try
            {
                string path = BulkImportService.GenerateTemplate(dialog.SelectedPath);
                MessageBox.Show(
                    $"Template saved to:\n{path}",
                    "Template Generated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error generating template: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void ParseFile()
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath)) return;

        try
        {
            var result = BulkImportService.ParseCsv(_selectedFilePath);

            emptyStateLabel.Visibility = Visibility.Collapsed;
            importResultsPanel.Visibility = Visibility.Collapsed;
            parseResultsPanel.Visibility = Visibility.Visible;

            if (result.IsSuccess)
            {
                _validRows = result.Rows;
                parseStatusLabel.Text = $"File parsed: {result.TotalLinesRead} row(s) read";
                parseStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));

                validCountLabel.Text = $"{result.Rows.Count} valid";
                errorCountLabel.Text = $"{result.Errors.Count} error(s)";

                importButton.IsEnabled = result.Rows.Count > 0;

                if (result.Errors.Count > 0)
                {
                    errorsHeader.Visibility = Visibility.Visible;
                    errorsList.Visibility = Visibility.Visible;
                    errorsList.Items.Clear();
                    foreach (var error in result.Errors)
                    {
                        errorsList.Items.Add(
                            $"Line {error.LineNumber}: {string.Join("; ", error.Errors)}");
                    }
                }
                else
                {
                    errorsHeader.Visibility = Visibility.Collapsed;
                    errorsList.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _validRows = null;
                parseStatusLabel.Text = result.Message;
                parseStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
                importButton.IsEnabled = false;
                validCountLabel.Text = "0 valid";
                errorCountLabel.Text = "Parse failed";
            }
        }
        catch (Exception ex)
        {
            parseResultsPanel.Visibility = Visibility.Visible;
            parseStatusLabel.Text = $"Error: {ex.Message}";
            parseStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
            importButton.IsEnabled = false;
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_validRows == null || _validRows.Count == 0)
        {
            MessageBox.Show("No valid rows to import.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Import {_validRows.Count} resident(s) into the system?\n\n" +
            (skipDuplicatesCheck.IsChecked == true ? "Duplicates will be skipped." : "Duplicates will NOT be checked."),
            "Confirm Import",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            importButton.IsEnabled = false;
            var result = BulkImportService.Import(_validRows, skipDuplicatesCheck.IsChecked == true);

            parseResultsPanel.Visibility = Visibility.Collapsed;
            importResultsPanel.Visibility = Visibility.Visible;

            if (result.IsSuccess)
            {
                ImportedCount = result.InsertedCount;
                importStatusLabel.Text = "Import Successful ✓";
                importStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));

                string details = $"{result.InsertedCount} resident(s) added.";
                if (result.SkippedCount > 0)
                    details += $"\n{result.SkippedCount} duplicate(s) skipped.";
                if (result.DuplicateNames.Count > 0 && result.DuplicateNames.Count <= 10)
                    details += $"\n\nSkipped: {string.Join(", ", result.DuplicateNames)}";

                importDetailsLabel.Text = details;
            }
            else
            {
                importStatusLabel.Text = "Import Failed";
                importStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
                importDetailsLabel.Text = result.Message;
            }
        }
        catch (Exception ex)
        {
            importResultsPanel.Visibility = Visibility.Visible;
            importStatusLabel.Text = "Import Error";
            importDetailsLabel.Text = ex.Message;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = ImportedCount > 0;
        Close();
    }
}
