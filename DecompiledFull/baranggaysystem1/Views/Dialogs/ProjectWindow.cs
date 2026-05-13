using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using baranggaysystem1.Services;
using baranggaysystem1.ViewModels;

namespace baranggaysystem1.Views.Dialogs;

public class ProjectWindow : Window, IComponentConnector
{
	private readonly ProjectService _service = new ProjectService();

	private readonly ProjectRecord? _existingRecord;

	private bool _isSaving;

	internal TextBlock eyebrowText;

	internal TextBlock headerTitleText;

	internal TextBlock headerSubtitleText;

	internal ComboBox typeCombo;

	internal ComboBox statusCombo;

	internal TextBox txtName;

	internal TextBox txtBudget;

	internal ComboBox outcomeStatusCombo;

	internal DatePicker startDatePicker;

	internal DatePicker endDatePicker;

	internal TextBox txtLead;

	internal TextBox txtAttendanceTarget;

	internal TextBox txtAttendanceCount;

	internal DatePicker activityDatePicker;

	internal TextBox txtOutcomeSummary;

	internal TextBox txtRemarks;

	internal Button btnConfirm;

	private bool _contentLoaded;

	public ProjectWindow()
		: this(null)
	{
	}

	public ProjectWindow(ProjectRecord? record)
	{
		InitializeComponent();
		_existingRecord = record;
		ConfigureOptions();
		PopulateForm();
	}

	private void ConfigureOptions()
	{
		typeCombo.ItemsSource = new string[2] { "Project", "Program" };
		statusCombo.ItemsSource = new string[4] { "Planned", "Ongoing", "On hold", "Completed" };
		outcomeStatusCombo.ItemsSource = new string[4] { "Pending", "In progress", "Needs follow-up", "Achieved" };
	}

	private void PopulateForm()
	{
		if (_existingRecord == null)
		{
			typeCombo.SelectedItem = "Project";
			statusCombo.SelectedItem = "Planned";
			outcomeStatusCombo.SelectedItem = "Pending";
			txtBudget.Text = "0.00";
			txtAttendanceTarget.Text = "0";
			txtAttendanceCount.Text = "0";
			return;
		}
		base.Title = "Edit Project / Program";
		eyebrowText.Text = "EDIT RECORD";
		headerTitleText.Text = "Update this community initiative";
		headerSubtitleText.Text = "Save the current status, attendance, dates, ownership, and outcome details.";
		btnConfirm.Content = "Save Changes";
		typeCombo.SelectedItem = _existingRecord.RecordType;
		txtName.Text = _existingRecord.Name;
		statusCombo.SelectedItem = _existingRecord.Status;
		outcomeStatusCombo.SelectedItem = _existingRecord.OutcomeStatus;
		txtBudget.Text = _existingRecord.Budget.ToString("N2", CultureInfo.InvariantCulture);
		startDatePicker.SelectedDate = _existingRecord.StartDate;
		endDatePicker.SelectedDate = _existingRecord.EndDate;
		txtLead.Text = _existingRecord.Lead;
		txtAttendanceTarget.Text = _existingRecord.AttendanceTarget.ToString(CultureInfo.InvariantCulture);
		txtAttendanceCount.Text = _existingRecord.AttendanceCount.ToString(CultureInfo.InvariantCulture);
		activityDatePicker.SelectedDate = _existingRecord.LastActivityDate;
		txtOutcomeSummary.Text = _existingRecord.OutcomeSummary;
		txtRemarks.Text = _existingRecord.Remarks;
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
		decimal budget = ParseBudget(txtBudget.Text);
		int attendanceTarget = ParseWholeNumber(txtAttendanceTarget.Text, "target attendance");
		int attendanceCount = ParseWholeNumber(txtAttendanceCount.Text, "actual attendance");
		ProjectRecord record = new ProjectRecord
		{
			ProjectId = (_existingRecord?.ProjectId ?? 0),
			RecordType = GetSelectedValue(typeCombo, "Project"),
			Name = txtName.Text,
			Status = GetSelectedValue(statusCombo, "Planned"),
			Budget = budget,
			StartDate = startDatePicker.SelectedDate,
			EndDate = endDatePicker.SelectedDate,
			Lead = txtLead.Text,
			AttendanceTarget = attendanceTarget,
			AttendanceCount = attendanceCount,
			LastActivityDate = activityDatePicker.SelectedDate,
			OutcomeStatus = GetSelectedValue(outcomeStatusCombo, "Pending"),
			OutcomeSummary = txtOutcomeSummary.Text,
			Remarks = txtRemarks.Text,
			CreatedAt = _existingRecord?.CreatedAt
		};
		try
		{
			_isSaving = true;
			btnConfirm.IsEnabled = false;
			btnConfirm.Content = "Saving...";
			if (_existingRecord != null)
			{
				await _service.UpdateProjectAsync(record);
			}
			else
			{
				await _service.CreateProjectAsync(record);
			}
			base.DialogResult = true;
		}
		catch (Exception ex)
		{
			DialogService.Instance.ShowError(ex.Message, "Project");
		}
		finally
		{
			_isSaving = false;
			btnConfirm.IsEnabled = true;
			btnConfirm.Content = ((_existingRecord == null) ? "Save Record" : "Save Changes");
		}
	}

	private static decimal ParseBudget(string? value)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0m;
		}
		if (decimal.TryParse(text, NumberStyles.Currency, CultureInfo.CurrentCulture, out var result))
		{
			return result;
		}
		if (decimal.TryParse(text, NumberStyles.Currency, CultureInfo.InvariantCulture, out var result2))
		{
			return result2;
		}
		throw new InvalidOperationException("Enter a valid budget amount.");
	}

	private static int ParseWholeNumber(string? value, string fieldLabel)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var result) && result >= 0)
		{
			return result;
		}
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2) && result2 >= 0)
		{
			return result2;
		}
		throw new InvalidOperationException("Enter a valid non-negative " + fieldLabel + ".");
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
			Uri resourceLocator = new Uri("/baranggaysystem1;V3.0.0;component/views/dialogs/projectwindow.xaml", UriKind.Relative);
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
			typeCombo = (ComboBox)target;
			break;
		case 5:
			statusCombo = (ComboBox)target;
			break;
		case 6:
			txtName = (TextBox)target;
			break;
		case 7:
			txtBudget = (TextBox)target;
			break;
		case 8:
			outcomeStatusCombo = (ComboBox)target;
			break;
		case 9:
			startDatePicker = (DatePicker)target;
			break;
		case 10:
			endDatePicker = (DatePicker)target;
			break;
		case 11:
			txtLead = (TextBox)target;
			break;
		case 12:
			txtAttendanceTarget = (TextBox)target;
			break;
		case 13:
			txtAttendanceCount = (TextBox)target;
			break;
		case 14:
			activityDatePicker = (DatePicker)target;
			break;
		case 15:
			txtOutcomeSummary = (TextBox)target;
			break;
		case 16:
			txtRemarks = (TextBox)target;
			break;
		case 17:
			btnConfirm = (Button)target;
			btnConfirm.Click += BtnConfirm_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
