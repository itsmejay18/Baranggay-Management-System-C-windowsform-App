using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace baranggaysystem1.Views.Controls;

/// <summary>
/// Print preview window for certificates and documents.
/// Allows users to preview, zoom, print, or save as PDF before committing.
/// </summary>
public partial class PrintPreviewWindow : Window
{
    private double _zoomLevel = 1.0;
    private FlowDocument? _document;
    private UIElement? _visualContent;

    public PrintPreviewWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Set the document content to preview using a FlowDocument.
    /// </summary>
    public void SetDocument(FlowDocument document)
    {
        _document = document;
        documentContent.Children.Clear();

        var reader = new FlowDocumentScrollViewer
        {
            Document = document,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            IsToolBarVisible = false,
            MinHeight = 800
        };

        documentContent.Children.Add(reader);
        previewPlaceholder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Set the document content using a visual element (e.g., a rendered certificate).
    /// </summary>
    public void SetVisualContent(UIElement content)
    {
        _visualContent = content;
        documentContent.Children.Clear();
        documentContent.Children.Add(content);
        previewPlaceholder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Set content from a pre-built StackPanel of elements.
    /// </summary>
    public void SetContent(StackPanel content)
    {
        _visualContent = content;
        documentContent.Children.Clear();

        foreach (UIElement child in content.Children)
        {
            // We need to detach from parent first
        }

        documentContent.Children.Add(content);
        previewPlaceholder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Build a certificate preview from data.
    /// </summary>
    public void BuildCertificatePreview(string certificateType, string residentName,
        string purpose, string documentNo, string barangayName, string officialName,
        DateTime issuedDate)
    {
        documentContent.Children.Clear();
        previewPlaceholder.Visibility = Visibility.Collapsed;

        var content = new StackPanel { Margin = new Thickness(20) };

        // Header
        content.Children.Add(new TextBlock
        {
            Text = "Republic of the Philippines",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.Black
        });
        content.Children.Add(new TextBlock
        {
            Text = barangayName,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4),
            Foreground = Brushes.Black
        });
        content.Children.Add(new TextBlock
        {
            Text = "OFFICE OF THE BARANGAY CAPTAIN",
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.DarkGray,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Separator
        content.Children.Add(new Border
        {
            Height = 2,
            Background = Brushes.DarkBlue,
            Margin = new Thickness(0, 0, 0, 30)
        });

        // Certificate title
        content.Children.Add(new TextBlock
        {
            Text = certificateType.ToUpperInvariant(),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30),
            Foreground = Brushes.Black
        });

        // Body
        content.Children.Add(new TextBlock
        {
            Text = "TO WHOM IT MAY CONCERN:",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = Brushes.Black
        });

        content.Children.Add(new TextBlock
        {
            Text = $"This is to certify that {residentName.ToUpperInvariant()}, " +
                   $"is a bonafide resident of this barangay.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 22,
            Margin = new Thickness(0, 0, 0, 16),
            Foreground = Brushes.Black
        });

        if (!string.IsNullOrWhiteSpace(purpose))
        {
            content.Children.Add(new TextBlock
            {
                Text = $"This certification is being issued upon the request of the above-named " +
                       $"person for {purpose} purposes.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
                Margin = new Thickness(0, 0, 0, 16),
                Foreground = Brushes.Black
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = $"Issued this {issuedDate:MMMM dd, yyyy} at the Barangay Hall.",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 50),
            Foreground = Brushes.Black
        });

        // Signature area
        content.Children.Add(new TextBlock
        {
            Text = officialName.ToUpperInvariant(),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 40, 40, 4),
            Foreground = Brushes.Black
        });
        content.Children.Add(new TextBlock
        {
            Text = "Barangay Captain",
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 40, 20),
            Foreground = Brushes.DarkGray
        });

        // Document number
        if (!string.IsNullOrWhiteSpace(documentNo))
        {
            content.Children.Add(new TextBlock
            {
                Text = $"Doc No: {documentNo}",
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 20, 0, 0)
            });
        }

        documentContent.Children.Add(content);
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(previewContainer, "Barangay Document");
                MessageBox.Show("Document sent to printer.", "Print",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Print failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SavePdfButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "XPS Document (*.xps)|*.xps",
                DefaultExt = ".xps",
                FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                // Use XPS for WPF (PDF requires third-party library)
                using var package = System.IO.Packaging.Package.Open(
                    dialog.FileName, System.IO.FileMode.Create);
                using var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(
                    package, System.IO.Packaging.CompressionOption.Maximum);
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                writer.Write(previewContainer);

                MessageBox.Show($"Document saved to:\n{dialog.FileName}", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Min(_zoomLevel + 0.1, 2.0);
        ApplyZoom();
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Max(_zoomLevel - 0.1, 0.5);
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        previewContainer.LayoutTransform = new ScaleTransform(_zoomLevel, _zoomLevel);
        zoomLabel.Text = $"{(int)(_zoomLevel * 100)}%";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
