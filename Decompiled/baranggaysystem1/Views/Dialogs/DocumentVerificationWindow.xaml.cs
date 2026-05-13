using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using baranggaysystem1.helper;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;
using QRCoder;

namespace baranggaysystem1.Views.Dialogs;

public partial class DocumentVerificationWindow : Window
{
	private readonly CertificateRequestService _certificateRequestService = new CertificateRequestService();

	private readonly int? _requestId;

	private readonly string _initialLookup = string.Empty;

	private CertificateVerificationRecord? _currentRecord;


























	public DocumentVerificationWindow()
		: this(null, null)
	{
	}

	public DocumentVerificationWindow(int requestId)
		: this(requestId, null)
	{
	}

	public DocumentVerificationWindow(string lookup)
		: this(null, lookup)
	{
	}

	private DocumentVerificationWindow(int? requestId, string? lookup)
	{
		InitializeComponent();
		_requestId = requestId;
		_initialLookup = lookup?.Trim() ?? string.Empty;
		base.Loaded += DocumentVerificationWindow_Loaded;
	}

	private async void DocumentVerificationWindow_Loaded(object sender, RoutedEventArgs e)
	{
		if (_requestId.HasValue && _requestId.Value > 0)
		{
			await LoadByRequestIdAsync(_requestId.Value);
		}
		else if (!string.IsNullOrWhiteSpace(_initialLookup))
		{
			lookupBox.Text = _initialLookup;
			await VerifyLookupAsync();
		}
		else
		{
			lookupBox.Focus();
		}
	}

	private async void BtnVerify_Click(object sender, RoutedEventArgs e)
	{
		await VerifyLookupAsync();
	}

	private async void LookupBox_KeyDown(object sender, KeyEventArgs e)
	{
		if ((int)e.Key == 6)
		{
			e.Handled = true;
			await VerifyLookupAsync();
		}
	}

	private async Task LoadByRequestIdAsync(int requestId)
	{
		try
		{
			CertificateVerificationRecord certificateVerificationRecord = await _certificateRequestService.GetVerificationRecordAsync(requestId).ConfigureAwait(continueOnCapturedContext: true);
			ApplyRecord(certificateVerificationRecord, certificateVerificationRecord?.TrackingCode ?? $"Request #{requestId}");
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to load certificate verification details by request ID.", ex);
			ShowEmptyState("Failed to load verification details.", "The selected request could not be opened for verification.");
		}
	}

	private async Task VerifyLookupAsync()
	{
		string lookup = lookupBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(lookup))
		{
			DialogService.Instance.ShowWarning("Enter a tracking code, document number, verification token, or QR payload first.");
			lookupBox.Focus();
			return;
		}
		try
		{
			ApplyRecord(await _certificateRequestService.VerifyDocumentAsync(lookup).ConfigureAwait(continueOnCapturedContext: true), lookup);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Document verification lookup failed.", ex);
			ShowEmptyState("Verification lookup failed.", "The lookup could not be completed right now. Please try again.");
		}
	}

	private void ApplyRecord(CertificateVerificationRecord? record, string lookupLabel)
	{
		_currentRecord = record;
		btnCopyToken.IsEnabled = record?.HasVerificationToken ?? false;
		btnCopyPayload.IsEnabled = record != null && !string.IsNullOrWhiteSpace(record.VerificationPayload);
		if (record == null)
		{
			ApplyStateBadge("Not Found");
			ShowEmptyState("No matching document was found.", "Nothing in the certificate queue matches \"" + lookupLabel + "\". Use the exact tracking code, document number, verification token, or QR payload.");
			return;
		}
		emptyState.Visibility = Visibility.Collapsed;
		resultCard.Visibility = Visibility.Visible;
		resultSummaryText.Text = record.VerificationState + ": " + record.DocumentTypeName;
		resultMetaText.Text = $"Lookup matched {record.TrackingCode} for {record.ResidentName}. Verified at {DateTime.Now:MMM dd, yyyy hh:mm tt}.";
		trackingCodeText.Text = SafeText(record.TrackingCode, "Not assigned");
		documentNumberText.Text = SafeText(record.DocumentNo, "Not assigned");
		residentNameText.Text = SafeText(record.ResidentName, "Resident record unavailable");
		documentTypeText.Text = SafeText(record.DocumentTypeName, "Certificate");
		releasedOnText.Text = FormatDate(record.ReleasedAt, "Not yet released");
		expiresOnText.Text = FormatDate(record.ExpiresAt, "No expiry recorded");
		verificationTokenText.Text = SafeText(record.VerificationToken, "No token recorded");
		string value = (string.IsNullOrWhiteSpace(record.OrNumber) ? "No OR number" : ("OR " + record.OrNumber.Trim()));
		paymentMetaText.Text = $"{value} | PHP {record.Fee:N2}";
		purposeText.Text = SafeText(record.Purpose, "No purpose recorded");
		payloadText.Text = SafeText(record.VerificationPayload, "No QR payload available");
		qrHelpText.Text = (record.HasVerificationToken ? "This QR payload can be copied or scanned back into this verification screen." : "This record does not have a stored verification token yet, so the QR preview is unavailable.");
		stateSummaryText.Text = record.VerificationStateSummary;
		if (record.HasVerificationToken)
		{
			qrImage.Source = CreateQrCode(record.VerificationPayload);
			qrEmptyText.Visibility = Visibility.Collapsed;
		}
		else
		{
			qrImage.Source = null;
			qrEmptyText.Visibility = Visibility.Visible;
		}
		ApplyStateBadge(record.VerificationState);
	}

	private void ShowEmptyState(string title, string detail)
	{
		_currentRecord = null;
		resultCard.Visibility = Visibility.Collapsed;
		emptyState.Visibility = Visibility.Visible;
		emptyTitleText.Text = title;
		emptyMetaText.Text = detail;
		resultSummaryText.Text = "Verification details will appear here.";
		resultMetaText.Text = "Lookup metadata";
		qrImage.Source = null;
		qrEmptyText.Visibility = Visibility.Visible;
		btnCopyToken.IsEnabled = false;
		btnCopyPayload.IsEnabled = false;
	}

	private void ApplyStateBadge(string state)
	{
		string hex;
		string hex2;
		string hex3;
		switch (state)
		{
		case "Valid":
			hex = "#DCFCE7";
			hex2 = "#86EFAC";
			hex3 = "#15803D";
			break;
		case "Expired":
			hex = "#FEF3C7";
			hex2 = "#FCD34D";
			hex3 = "#B45309";
			break;
		case "Pending Release":
			hex = "#DBEAFE";
			hex2 = "#93C5FD";
			hex3 = "#1D4ED8";
			break;
		case "Not Found":
			hex = "#FEE2E2";
			hex2 = "#FCA5A5";
			hex3 = "#B91C1C";
			break;
		default:
			hex = "#E2E8F0";
			hex2 = "#CBD5E1";
			hex3 = "#475569";
			break;
		}
		verificationStateBadge.Background = CreateBrush(hex);
		verificationStateBadge.BorderBrush = CreateBrush(hex2);
		verificationStateText.Foreground = CreateBrush(hex3);
		verificationStateText.Text = state;
	}

	private void BtnCopyToken_Click(object sender, RoutedEventArgs e)
	{
		CertificateVerificationRecord? currentRecord = _currentRecord;
		if (currentRecord == null || !currentRecord.HasVerificationToken)
		{
			DialogService.Instance.ShowWarning("No verification token is available for this document yet.");
		}
		else
		{
			CopyText(_currentRecord.VerificationToken, "Verification token copied.");
		}
	}

	private void BtnCopyPayload_Click(object sender, RoutedEventArgs e)
	{
		if (_currentRecord == null || string.IsNullOrWhiteSpace(_currentRecord.VerificationPayload))
		{
			DialogService.Instance.ShowWarning("No QR payload is available to copy.");
		}
		else
		{
			CopyText(_currentRecord.VerificationPayload, "QR payload copied.");
		}
	}

	private static void CopyText(string value, string successMessage)
	{
		try
		{
			Clipboard.SetText(value);
			DialogService.Instance.ShowInfo(successMessage);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to copy verification text.", ex);
			DialogService.Instance.ShowError("The text could not be copied to the clipboard.");
		}
	}

	private static BitmapImage? CreateQrCode(string payload)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(payload))
		{
			return null;
		}
		QRCodeGenerator val = new QRCodeGenerator();
		try
		{
			QRCodeData val2 = val.CreateQrCode(payload, (QRCodeGenerator.ECCLevel)2, false, false, (QRCodeGenerator.EciMode)0, -1);
			try
			{
				using MemoryStream streamSource = new MemoryStream(new PngByteQRCode(val2).GetGraphic(20, true));
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.StreamSource = streamSource;
				bitmapImage.EndInit();
				((Freezable)bitmapImage).Freeze();
				return bitmapImage;
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static SolidColorBrush CreateBrush(string hex)
	{
		return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
	}

	private static string SafeText(string? value, string fallback)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return fallback;
	}

	private static string FormatDate(DateTime? value, string fallback)
	{
		if (!value.HasValue)
		{
			return fallback;
		}
		return value.Value.ToString("MMM dd, yyyy hh:mm tt", CultureInfo.InvariantCulture);
	}

	private void BtnClose_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}}
