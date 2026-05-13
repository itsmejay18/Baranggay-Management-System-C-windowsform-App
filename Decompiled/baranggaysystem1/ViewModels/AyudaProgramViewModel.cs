using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class AyudaProgramViewModel : ObservableObject
{
	private readonly AyudaService _ayudaService = new AyudaService();

	private readonly int? _programId;

	[ObservableProperty]
	private string _windowTitle = "Create Ayuda Program";

	[ObservableProperty]
	private string _headerEyebrowText = "NEW AYUDA BUDGET";

	[ObservableProperty]
	private string _headerTitleText = "Set up a barangay ayuda program";

	[ObservableProperty]
	private string _headerDescriptionText = "Define the program name, category, and total available budget before releasing assistance.";

	[ObservableProperty]
	private string _saveButtonText = "Save Budget Program";

	[ObservableProperty]
	private string _programName = string.Empty;

	[ObservableProperty]
	private string _category = "Financial Assistance";

	[ObservableProperty]
	private decimal _allocatedBudget = 10000m;

	[ObservableProperty]
	private string _status = "ACTIVE";

	[ObservableProperty]
	private DateTime? _startDate = DateTime.Today;

	[ObservableProperty]
	private DateTime? _endDate;

	[ObservableProperty]
	private string _notes = string.Empty;

	[ObservableProperty]
	private bool _isProcessing;

	public ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "Financial Assistance", "Food Pack", "Medical Support", "Senior or PWD Support", "Emergency Relief", "Education Support" };

	public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string> { "ACTIVE", "PAUSED", "CLOSED" };

	public event Action<bool?>? CloseRequested;

	public AyudaProgramViewModel(int? programId = null)
	{
		_programId = programId;
		ApplyModeText();
	}

	public async Task InitializeAsync()
	{
		if (!_programId.HasValue || _programId.Value <= 0)
		{
			return;
		}
		try
		{
			IsProcessing = true;
			AyudaProgramRecord ayudaProgramRecord = await _ayudaService.GetProgramAsync(_programId.Value);
			if (ayudaProgramRecord == null)
			{
				DialogService.Instance.ShowWarning("The selected ayuda budget could not be found.");
				this.CloseRequested?.Invoke(false);
				return;
			}
			ProgramName = ayudaProgramRecord.ProgramName;
			Category = (string.IsNullOrWhiteSpace(ayudaProgramRecord.Category) ? Category : ayudaProgramRecord.Category);
			AllocatedBudget = ayudaProgramRecord.AllocatedBudget;
			Status = ayudaProgramRecord.Status;
			StartDate = ayudaProgramRecord.StartDate;
			EndDate = ayudaProgramRecord.EndDate;
			Notes = ayudaProgramRecord.Notes;
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda program editor failed to initialize.", ex);
			DialogService.Instance.ShowError("Could not load the selected ayuda budget.");
			this.CloseRequested?.Invoke(false);
		}
		finally
		{
			IsProcessing = false;
		}
	}

	[RelayCommand]
	private async Task SaveProgram()
	{
		if (string.IsNullOrWhiteSpace(ProgramName))
		{
			DialogService.Instance.ShowWarning("Program name is required.");
			return;
		}
		if (AllocatedBudget <= 0m)
		{
			DialogService.Instance.ShowWarning("Allocated budget must be greater than zero.");
			return;
		}
		if (StartDate.HasValue && EndDate.HasValue && EndDate.Value.Date < StartDate.Value.Date)
		{
			DialogService.Instance.ShowWarning("End date cannot be earlier than the start date.");
			return;
		}
		try
		{
			IsProcessing = true;
			await _ayudaService.SaveProgramAsync(new AyudaProgramRecord
			{
				ProgramId = _programId.GetValueOrDefault(),
				ProgramName = ProgramName,
				Category = Category,
				AllocatedBudget = AllocatedBudget,
				Status = Status,
				StartDate = StartDate,
				EndDate = EndDate,
				Notes = Notes
			});
			string value = (_programId.HasValue ? "updated" : "created");
			DialogService.Instance.ShowInfo($"Ayuda budget {value} for {ProgramName}.");
			this.CloseRequested?.Invoke(true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Ayuda program save failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Ayuda Program");
		}
		finally
		{
			IsProcessing = false;
		}
	}

	private void ApplyModeText()
	{
		if (_programId.HasValue && _programId.Value > 0)
		{
			WindowTitle = "Edit Ayuda Program";
			HeaderEyebrowText = "UPDATE AYUDA BUDGET";
			HeaderTitleText = "Adjust the selected ayuda program";
			HeaderDescriptionText = "Update the assistance budget, schedule, notes, or operating status for this program.";
			SaveButtonText = "Save Program Changes";
		}
	}
}
