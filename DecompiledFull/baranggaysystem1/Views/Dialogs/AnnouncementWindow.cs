using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class AnnouncementWindow : Window, IComponentConnector
{
	private readonly AnnouncementService _service = new AnnouncementService();

	private readonly AnnouncementRecord? _existingRecord;

	private bool _isSaving;

	internal TextBlock eyebrowText;

	internal TextBlock headerTitleText;

	internal TextBlock headerSubtitleText;

	internal TextBox txtTitle;

	internal ComboBox priorityCombo;

	internal ComboBox statusCombo;

	internal CheckBox chkPinned;

	internal TextBox txtBody;

	internal Button btnConfirm;

	private bool _contentLoaded;

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
			IsPinned = (chkPinned.IsChecked == true),
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
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.9.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/announcementwindow.xaml", UriKind.Relative);
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
			eyebrowText = (TextBlock)target;
			break;
		case 2:
			headerTitleText = (TextBlock)target;
			break;
		case 3:
			headerSubtitleText = (TextBlock)target;
			break;
		case 4:
			txtTitle = (TextBox)target;
			break;
		case 5:
			priorityCombo = (ComboBox)target;
			break;
		case 6:
			statusCombo = (ComboBox)target;
			break;
		case 7:
			chkPinned = (CheckBox)target;
			break;
		case 8:
			txtBody = (TextBox)target;
			break;
		case 9:
			btnConfirm = (Button)target;
			btnConfirm.Click += BtnConfirm_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
