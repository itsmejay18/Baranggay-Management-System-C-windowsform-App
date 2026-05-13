using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public partial class AnnouncementWindow : Window
{
	private readonly AnnouncementService _service = new AnnouncementService();

	private readonly AnnouncementRecord? _existingRecord;

	private bool _isSaving;









	public AnnouncementWindow()
		: this(null)
	{
	}

	public AnnouncementWindow(AnnouncementRecord? record)
	{
		InitializeComponent();
		_existingRecord = record;
		ConfigureOptions();
		PopulateForm();
	}

	private void ConfigureOptions()
	{
		priorityCombo.ItemsSource = new string[3] { "Low", "Normal", "High" };
		statusCombo.ItemsSource = new string[3] { "Draft", "Published", "Archived" };
	}

	private void PopulateForm()
	{
		if (_existingRecord == null)
		{
			priorityCombo.SelectedItem = "Normal";
			statusCombo.SelectedItem = "Published";
			chkPinned.IsChecked = false;
			return;
		}
		base.Title = "Edit Announcement";
		eyebrowText.Text = "EDIT ANNOUNCEMENT";
		headerTitleText.Text = "Update this barangay announcement";
		headerSubtitleText.Text = "Save changes to the message, urgency, and dashboard visibility.";
		btnConfirm.Content = "Save Changes";
		txtTitle.Text = _existingRecord.Title;
		txtBody.Text = _existingRecord.Body;
		priorityCombo.SelectedItem = _existingRecord.Priority;
		statusCombo.SelectedItem = _existingRecord.Status;
		chkPinned.IsChecked = _existingRecord.IsPinned;
	}

	private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
	{
		await SaveAsync();
	}

	private async Task SaveAsync()
	{
		if (_isSaving)
		{
			return;
		}
		AnnouncementRecord record = new AnnouncementRecord
		{
			AnnouncementId = (_existingRecord?.AnnouncementId ?? 0),
			Title = txtTitle.Text,
			Body = txtBody.Text,
			Priority = GetSelectedValue(priorityCombo, "Normal"),
			Status = GetSelectedValue(statusCombo, "Published"),
			IsPinned = chkPinned.IsChecked.GetValueOrDefault(),
			CreatedAt = _existingRecord?.CreatedAt
		};
		try
		{
			_isSaving = true;
			btnConfirm.IsEnabled = false;
			btnConfirm.Content = "Saving...";
			if (_existingRecord != null)
			{
				await _service.UpdateAnnouncementAsync(record);
			}
			else
			{
				await _service.CreateAnnouncementAsync(record);
			}
			base.DialogResult = true;
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError(ex.Message, "Announcement");
		}
		finally
		{
			_isSaving = false;
			btnConfirm.IsEnabled = true;
			btnConfirm.Content = ((_existingRecord == null) ? "Save Announcement" : "Save Changes");
		}
	}

	private static string GetSelectedValue(ComboBox comboBox, string fallback)
	{
		string text = comboBox.SelectedItem?.ToString()?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return fallback;
	}}
