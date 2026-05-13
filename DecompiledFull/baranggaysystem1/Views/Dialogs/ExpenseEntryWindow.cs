using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Models;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class ExpenseEntryWindow : Window, IComponentConnector
{
	internal TextBlock windowEyebrowText;

	internal TextBlock windowTitleText;

	internal DatePicker expenseDatePicker;

	internal ComboBox categoryComboBox;

	internal TextBox titleTextBox;

	internal TextBox payeeTextBox;

	internal TextBox amountTextBox;

	internal ComboBox paymentMethodComboBox;

	internal ComboBox statusComboBox;

	internal TextBox referenceTextBox;

	internal TextBox notesTextBox;

	internal Button saveButton;

	private bool _contentLoaded;

	public ExpenseEntryRecord Draft { get; private set; }

	public ExpenseEntryWindow(ExpenseEntryRecord? existingRecord = null)
	{
		InitializeComponent();
		categoryComboBox.ItemsSource = new string[8] { "Utilities", "Office Supplies", "Maintenance", "Programs and Events", "Transportation", "Repairs", "Emergency Response", "Other" };
		paymentMethodComboBox.ItemsSource = new string[4] { "Cash", "GCash", "Bank", "Petty Cash" };
		statusComboBox.ItemsSource = new string[3] { "POSTED", "PENDING", "CANCELLED" };
		Draft = ((existingRecord != null) ? Clone(existingRecord) : new ExpenseEntryRecord());
		expenseDatePicker.SelectedDate = ((Draft.ExpenseDate == default(DateTime)) ? DateTime.Today : Draft.ExpenseDate);
		categoryComboBox.Text = Draft.ExpenseCategory;
		titleTextBox.Text = Draft.ExpenseTitle;
		payeeTextBox.Text = Draft.PayeeName;
		amountTextBox.Text = ((Draft.Amount <= 0m) ? "0.00" : Draft.Amount.ToString("N2", CultureInfo.InvariantCulture));
		paymentMethodComboBox.Text = (string.IsNullOrWhiteSpace(Draft.PaymentMethod) ? "Cash" : Draft.PaymentMethod);
		statusComboBox.SelectedItem = (string.IsNullOrWhiteSpace(Draft.Status) ? "POSTED" : Draft.Status);
		referenceTextBox.Text = Draft.ReferenceNo;
		notesTextBox.Text = Draft.Notes;
		bool flag = Draft.ExpenseId > 0;
		base.Title = (flag ? "Edit Expense Entry" : "New Expense Entry");
		windowEyebrowText.Text = (flag ? "UPDATE EXPENSE ENTRY" : "NEW EXPENSE ENTRY");
		windowTitleText.Text = (flag ? "Edit barangay expense" : "Record a barangay expense");
		saveButton.Content = (flag ? "Update Expense" : "Save Expense");
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (!TryParseDecimal(amountTextBox.Text, out var value))
		{
			DialogService.Instance.ShowWarning("Enter a valid expense amount.");
			return;
		}
		Draft = new ExpenseEntryRecord
		{
			ExpenseId = Draft.ExpenseId,
			ExpenseDate = (expenseDatePicker.SelectedDate ?? DateTime.Today),
			ExpenseCategory = categoryComboBox.Text,
			ExpenseTitle = titleTextBox.Text,
			PayeeName = payeeTextBox.Text,
			Amount = value,
			PaymentMethod = (Convert.ToString(paymentMethodComboBox.SelectedItem) ?? paymentMethodComboBox.Text),
			Status = (Convert.ToString(statusComboBox.SelectedItem) ?? statusComboBox.Text),
			ReferenceNo = referenceTextBox.Text,
			Notes = notesTextBox.Text
		};
		base.DialogResult = true;
	}

	private static bool TryParseDecimal(string? text, out decimal value)
	{
		if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
		{
			return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
		}
		return true;
	}

	private static ExpenseEntryRecord Clone(ExpenseEntryRecord source)
	{
		return new ExpenseEntryRecord
		{
			ExpenseId = source.ExpenseId,
			ExpenseDate = source.ExpenseDate,
			ExpenseCategory = source.ExpenseCategory,
			ExpenseTitle = source.ExpenseTitle,
			PayeeName = source.PayeeName,
			Amount = source.Amount,
			PaymentMethod = source.PaymentMethod,
			Status = source.Status,
			ReferenceNo = source.ReferenceNo,
			Notes = source.Notes
		};
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/expenseentrywindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			windowEyebrowText = (TextBlock)target;
			break;
		case 2:
			windowTitleText = (TextBlock)target;
			break;
		case 3:
			expenseDatePicker = (DatePicker)target;
			break;
		case 4:
			categoryComboBox = (ComboBox)target;
			break;
		case 5:
			titleTextBox = (TextBox)target;
			break;
		case 6:
			payeeTextBox = (TextBox)target;
			break;
		case 7:
			amountTextBox = (TextBox)target;
			break;
		case 8:
			paymentMethodComboBox = (ComboBox)target;
			break;
		case 9:
			statusComboBox = (ComboBox)target;
			break;
		case 10:
			referenceTextBox = (TextBox)target;
			break;
		case 11:
			notesTextBox = (TextBox)target;
			break;
		case 12:
			saveButton = (Button)target;
			saveButton.Click += SaveButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
