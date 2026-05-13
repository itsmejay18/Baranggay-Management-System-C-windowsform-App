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

public partial class AssetRecordWindow : Window
{












	public AssetRecord Draft { get; private set; }

	public AssetRecordWindow(AssetRecord? existingRecord = null)
	{
		InitializeComponent();
		categoryComboBox.ItemsSource = new string[8] { "Furniture", "Office Equipment", "IT Equipment", "Vehicle", "Emergency Equipment", "Appliance", "Building Fixture", "Other" };
		conditionComboBox.ItemsSource = new string[5] { "EXCELLENT", "GOOD", "FAIR", "NEEDS REPAIR", "UNSERVICEABLE" };
		lifecycleComboBox.ItemsSource = new string[4] { "ACTIVE", "IN REPAIR", "STORED", "DISPOSED" };
		Draft = ((existingRecord != null) ? Clone(existingRecord) : new AssetRecord());
		assetNameTextBox.Text = Draft.AssetName;
		categoryComboBox.Text = Draft.AssetCategory;
		assetTagTextBox.Text = Draft.AssetTag;
		acquisitionDatePicker.SelectedDate = Draft.AcquisitionDate;
		acquisitionCostTextBox.Text = Draft.AcquisitionCost.ToString("N2", CultureInfo.InvariantCulture);
		locationTextBox.Text = Draft.AssignedLocation;
		custodianTextBox.Text = Draft.CustodianName;
		conditionComboBox.SelectedItem = (string.IsNullOrWhiteSpace(Draft.ConditionStatus) ? "GOOD" : Draft.ConditionStatus);
		lifecycleComboBox.SelectedItem = (string.IsNullOrWhiteSpace(Draft.LifecycleStatus) ? "ACTIVE" : Draft.LifecycleStatus);
		notesTextBox.Text = Draft.Notes;
		bool flag = Draft.AssetId > 0;
		base.Title = (flag ? "Edit Asset Record" : "New Asset Record");
		windowEyebrowText.Text = (flag ? "UPDATE ASSET RECORD" : "NEW ASSET RECORD");
		windowTitleText.Text = (flag ? "Update barangay asset" : "Register barangay asset");
		saveButton.Content = (flag ? "Update Asset" : "Save Asset");
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (!TryParseDecimal(acquisitionCostTextBox.Text, out var value))
		{
			DialogService.Instance.ShowWarning("Enter a valid acquisition cost.");
			return;
		}
		Draft = new AssetRecord
		{
			AssetId = Draft.AssetId,
			AssetName = assetNameTextBox.Text,
			AssetCategory = categoryComboBox.Text,
			AssetTag = assetTagTextBox.Text,
			AcquisitionDate = acquisitionDatePicker.SelectedDate,
			AcquisitionCost = value,
			AssignedLocation = locationTextBox.Text,
			CustodianName = custodianTextBox.Text,
			ConditionStatus = (Convert.ToString(conditionComboBox.SelectedItem) ?? conditionComboBox.Text),
			LifecycleStatus = (Convert.ToString(lifecycleComboBox.SelectedItem) ?? lifecycleComboBox.Text),
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

	private static AssetRecord Clone(AssetRecord source)
	{
		return new AssetRecord
		{
			AssetId = source.AssetId,
			AssetName = source.AssetName,
			AssetCategory = source.AssetCategory,
			AssetTag = source.AssetTag,
			AcquisitionDate = source.AcquisitionDate,
			AcquisitionCost = source.AcquisitionCost,
			AssignedLocation = source.AssignedLocation,
			CustodianName = source.CustodianName,
			ConditionStatus = source.ConditionStatus,
			LifecycleStatus = source.LifecycleStatus,
			Notes = source.Notes
		};
	}}
