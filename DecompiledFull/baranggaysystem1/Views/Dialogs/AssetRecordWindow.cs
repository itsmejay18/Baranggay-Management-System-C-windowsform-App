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

public class AssetRecordWindow : Window, IComponentConnector
{
	internal TextBlock windowEyebrowText;

	internal TextBlock windowTitleText;

	internal TextBox assetNameTextBox;

	internal ComboBox categoryComboBox;

	internal TextBox assetTagTextBox;

	internal DatePicker acquisitionDatePicker;

	internal TextBox acquisitionCostTextBox;

	internal TextBox locationTextBox;

	internal TextBox custodianTextBox;

	internal ComboBox conditionComboBox;

	internal ComboBox lifecycleComboBox;

	internal TextBox notesTextBox;

	internal Button saveButton;

	private bool _contentLoaded;

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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/assetrecordwindow.xaml", UriKind.Relative);
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
			assetNameTextBox = (TextBox)target;
			break;
		case 4:
			categoryComboBox = (ComboBox)target;
			break;
		case 5:
			assetTagTextBox = (TextBox)target;
			break;
		case 6:
			acquisitionDatePicker = (DatePicker)target;
			break;
		case 7:
			acquisitionCostTextBox = (TextBox)target;
			break;
		case 8:
			locationTextBox = (TextBox)target;
			break;
		case 9:
			custodianTextBox = (TextBox)target;
			break;
		case 10:
			conditionComboBox = (ComboBox)target;
			break;
		case 11:
			lifecycleComboBox = (ComboBox)target;
			break;
		case 12:
			notesTextBox = (TextBox)target;
			break;
		case 13:
			saveButton = (Button)target;
			saveButton.Click += SaveButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
