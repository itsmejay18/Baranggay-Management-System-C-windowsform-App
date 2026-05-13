using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using baranggaysystem1.Models;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class ResidentClassificationWindow : Window
{
	private sealed class ColorOption
	{
		public string Label { get; }

		public string Hex { get; }

		public ColorOption(string label, string hex)
		{
			Label = label;
			Hex = hex;
		}

		public override string ToString()
		{
			return Label;
		}
	}

	private static readonly ColorOption[] ColorOptions = new ColorOption[8]
	{
		new ColorOption("Blue", "#2563EB"),
		new ColorOption("Cyan", "#0891B2"),
		new ColorOption("Green", "#16A34A"),
		new ColorOption("Amber", "#CA8A04"),
		new ColorOption("Orange", "#EA580C"),
		new ColorOption("Rose", "#DB2777"),
		new ColorOption("Purple", "#7C3AED"),
		new ColorOption("Slate", "#475569")
	};

	private readonly ResidentClassificationRecord _source;











	internal ResidentClassificationRecord Draft { get; private set; }

	internal ResidentClassificationWindow(ResidentClassificationRecord? existingRecord = null)
	{
		InitializeComponent();
		_source = ((existingRecord != null) ? Clone(existingRecord) : new ResidentClassificationRecord());
		Draft = Clone(_source);
		typeComboBox.ItemsSource = new string[2] { "Tag", "Category" };
		statusComboBox.ItemsSource = new string[2] { "Active", "Archived" };
		colorComboBox.ItemsSource = ColorOptions;
		PopulateForm();
	}

	private void PopulateForm()
	{
		bool flag = _source.ClassificationId > 0;
		base.Title = (flag ? "Edit Tag or Category" : "New Tag or Category");
		eyebrowText.Text = (flag ? "EDIT CLASSIFICATION" : "NEW CLASSIFICATION");
		headerTitleText.Text = (flag ? ("Update " + _source.Name) : "Create a resident classification");
		headerSubtitleText.Text = (flag ? "Adjust the label, description, color, and active state." : "Add a reusable tag or category for resident record organization.");
		saveButton.Content = (flag ? "Save Changes" : "Create");
		typeComboBox.SelectedItem = (string.Equals(_source.ClassificationType, "CATEGORY", StringComparison.OrdinalIgnoreCase) ? "Category" : "Tag");
		statusComboBox.SelectedItem = (string.Equals(_source.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) ? "Archived" : "Active");
		nameTextBox.Text = _source.Name;
		descriptionTextBox.Text = _source.Description;
		ColorOption colorOption = ColorOptions.FirstOrDefault((ColorOption option) => string.Equals(option.Hex, _source.ColorHex, StringComparison.OrdinalIgnoreCase)) ?? ColorOptions[0];
		colorComboBox.SelectedItem = colorOption;
		UpdateColorPreview(colorOption.Hex);
		typeComboBox.IsEnabled = !_source.IsSystem;
		systemNotice.Visibility = ((!_source.IsSystem) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (colorComboBox.SelectedItem is ColorOption colorOption)
		{
			UpdateColorPreview(colorOption.Hex);
		}
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		string text = nameTextBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			DialogService.Instance.ShowWarning("Name is required.", "Tags & Categories");
			return;
		}
		Draft = new ResidentClassificationRecord
		{
			ClassificationId = _source.ClassificationId,
			BarangayId = _source.BarangayId,
			ClassificationType = GetTypeValue(),
			ClassificationKey = _source.ClassificationKey,
			Name = text,
			Description = descriptionTextBox.Text.Trim(),
			ColorHex = ((colorComboBox.SelectedItem is ColorOption colorOption) ? colorOption.Hex : _source.ColorHex),
			Status = GetStatusValue(),
			IsSystem = _source.IsSystem,
			SortOrder = _source.SortOrder,
			UsageCount = _source.UsageCount
		};
		base.DialogResult = true;
	}

	private string GetTypeValue()
	{
		if (!string.Equals(Convert.ToString(typeComboBox.SelectedItem) ?? typeComboBox.Text, "Category", StringComparison.OrdinalIgnoreCase))
		{
			return "TAG";
		}
		return "CATEGORY";
	}

	private string GetStatusValue()
	{
		if (!string.Equals(Convert.ToString(statusComboBox.SelectedItem) ?? statusComboBox.Text, "Archived", StringComparison.OrdinalIgnoreCase))
		{
			return "ACTIVE";
		}
		return "ARCHIVED";
	}

	private void UpdateColorPreview(string hex)
	{
		try
		{
			colorPreview.Background = (Brush)new BrushConverter().ConvertFromString(hex);
		}
		catch
		{
			colorPreview.Background = Brushes.DodgerBlue;
		}
	}

	private static ResidentClassificationRecord Clone(ResidentClassificationRecord source)
	{
		return new ResidentClassificationRecord
		{
			ClassificationId = source.ClassificationId,
			BarangayId = source.BarangayId,
			ClassificationType = source.ClassificationType,
			ClassificationKey = source.ClassificationKey,
			Name = source.Name,
			Description = source.Description,
			ColorHex = source.ColorHex,
			Status = source.Status,
			IsSystem = source.IsSystem,
			SortOrder = source.SortOrder,
			UsageCount = source.UsageCount,
			CreatedAtDisplay = source.CreatedAtDisplay
		};
	}}
