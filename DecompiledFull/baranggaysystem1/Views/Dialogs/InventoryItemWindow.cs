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

public class InventoryItemWindow : Window, IComponentConnector
{
	internal TextBlock windowEyebrowText;

	internal TextBlock windowTitleText;

	internal TextBox itemNameTextBox;

	internal ComboBox categoryComboBox;

	internal ComboBox unitComboBox;

	internal TextBox quantityTextBox;

	internal TextBox reorderLevelTextBox;

	internal TextBox unitCostTextBox;

	internal TextBox locationTextBox;

	internal ComboBox statusComboBox;

	internal DatePicker lastRestockedDatePicker;

	internal TextBox notesTextBox;

	internal Button saveButton;

	private bool _contentLoaded;

	public InventoryItemRecord Draft { get; private set; }

	public InventoryItemWindow(InventoryItemRecord? existingRecord = null)
	{
		InitializeComponent();
		categoryComboBox.ItemsSource = new string[7] { "Office Supplies", "Cleaning Supplies", "Medical Supplies", "Disaster Response", "Maintenance Materials", "IT Equipment", "Other" };
		unitComboBox.ItemsSource = new string[7] { "pcs", "box", "pack", "ream", "set", "liter", "kg" };
		statusComboBox.ItemsSource = new string[2] { "ACTIVE", "ARCHIVED" };
		Draft = ((existingRecord != null) ? Clone(existingRecord) : new InventoryItemRecord());
		itemNameTextBox.Text = Draft.ItemName;
		categoryComboBox.Text = Draft.Category;
		unitComboBox.Text = (string.IsNullOrWhiteSpace(Draft.Unit) ? "pcs" : Draft.Unit);
		quantityTextBox.Text = Draft.QuantityOnHand.ToString("N2", CultureInfo.InvariantCulture);
		reorderLevelTextBox.Text = Draft.ReorderLevel.ToString("N2", CultureInfo.InvariantCulture);
		unitCostTextBox.Text = Draft.UnitCost.ToString("N2", CultureInfo.InvariantCulture);
		locationTextBox.Text = Draft.Location;
		statusComboBox.SelectedItem = (string.IsNullOrWhiteSpace(Draft.ItemStatus) ? "ACTIVE" : Draft.ItemStatus);
		lastRestockedDatePicker.SelectedDate = Draft.LastRestockedAt;
		notesTextBox.Text = Draft.Notes;
		bool flag = Draft.ItemId > 0;
		base.Title = (flag ? "Edit Inventory Item" : "New Inventory Item");
		windowEyebrowText.Text = (flag ? "UPDATE INVENTORY ITEM" : "NEW INVENTORY ITEM");
		windowTitleText.Text = (flag ? "Update stock record" : "Add inventory stock item");
		saveButton.Content = (flag ? "Update Item" : "Save Item");
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (!TryParseDecimal(quantityTextBox.Text, out var value))
		{
			DialogService.Instance.ShowWarning("Enter a valid quantity on hand.");
			return;
		}
		if (!TryParseDecimal(reorderLevelTextBox.Text, out var value2))
		{
			DialogService.Instance.ShowWarning("Enter a valid reorder level.");
			return;
		}
		if (!TryParseDecimal(unitCostTextBox.Text, out var value3))
		{
			DialogService.Instance.ShowWarning("Enter a valid unit cost.");
			return;
		}
		Draft = new InventoryItemRecord
		{
			ItemId = Draft.ItemId,
			ItemName = itemNameTextBox.Text,
			Category = categoryComboBox.Text,
			Unit = unitComboBox.Text,
			QuantityOnHand = value,
			ReorderLevel = value2,
			UnitCost = value3,
			Location = locationTextBox.Text,
			ItemStatus = (Convert.ToString(statusComboBox.SelectedItem) ?? statusComboBox.Text),
			LastRestockedAt = lastRestockedDatePicker.SelectedDate,
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

	private static InventoryItemRecord Clone(InventoryItemRecord source)
	{
		return new InventoryItemRecord
		{
			ItemId = source.ItemId,
			ItemName = source.ItemName,
			Category = source.Category,
			Unit = source.Unit,
			QuantityOnHand = source.QuantityOnHand,
			ReorderLevel = source.ReorderLevel,
			UnitCost = source.UnitCost,
			Location = source.Location,
			ItemStatus = source.ItemStatus,
			LastRestockedAt = source.LastRestockedAt,
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
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/inventoryitemwindow.xaml", UriKind.Relative);
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
			itemNameTextBox = (TextBox)target;
			break;
		case 4:
			categoryComboBox = (ComboBox)target;
			break;
		case 5:
			unitComboBox = (ComboBox)target;
			break;
		case 6:
			quantityTextBox = (TextBox)target;
			break;
		case 7:
			reorderLevelTextBox = (TextBox)target;
			break;
		case 8:
			unitCostTextBox = (TextBox)target;
			break;
		case 9:
			locationTextBox = (TextBox)target;
			break;
		case 10:
			statusComboBox = (ComboBox)target;
			break;
		case 11:
			lastRestockedDatePicker = (DatePicker)target;
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
