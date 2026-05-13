using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Models;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class ExpenseEntryWindow : Window
{











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
	}}
