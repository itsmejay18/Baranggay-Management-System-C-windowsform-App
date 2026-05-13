using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public class AyudaProgramViewModel : ObservableObject
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveProgramCommand;

	public ObservableCollection<string> Categories { get; } = new ObservableCollection<string> { "Financial Assistance", "Food Pack", "Medical Support", "Senior or PWD Support", "Emergency Relief", "Education Support" };

	public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string> { "ACTIVE", "PAUSED", "CLOSED" };

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string WindowTitle
	{
		get
		{
			return _windowTitle;
		}
		[MemberNotNull("_windowTitle")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_windowTitle, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WindowTitle);
				_windowTitle = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WindowTitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HeaderEyebrowText
	{
		get
		{
			return _headerEyebrowText;
		}
		[MemberNotNull("_headerEyebrowText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_headerEyebrowText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HeaderEyebrowText);
				_headerEyebrowText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HeaderEyebrowText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HeaderTitleText
	{
		get
		{
			return _headerTitleText;
		}
		[MemberNotNull("_headerTitleText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_headerTitleText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HeaderTitleText);
				_headerTitleText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HeaderTitleText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HeaderDescriptionText
	{
		get
		{
			return _headerDescriptionText;
		}
		[MemberNotNull("_headerDescriptionText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_headerDescriptionText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HeaderDescriptionText);
				_headerDescriptionText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HeaderDescriptionText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SaveButtonText
	{
		get
		{
			return _saveButtonText;
		}
		[MemberNotNull("_saveButtonText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_saveButtonText, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SaveButtonText);
				_saveButtonText = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SaveButtonText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProgramName
	{
		get
		{
			return _programName;
		}
		[MemberNotNull("_programName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_programName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProgramName);
				_programName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProgramName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Category
	{
		get
		{
			return _category;
		}
		[MemberNotNull("_category")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_category, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Category);
				_category = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Category);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public decimal AllocatedBudget
	{
		get
		{
			return _allocatedBudget;
		}
		set
		{
			if (!EqualityComparer<decimal>.Default.Equals(_allocatedBudget, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AllocatedBudget);
				_allocatedBudget = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AllocatedBudget);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Status
	{
		get
		{
			return _status;
		}
		[MemberNotNull("_status")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_status, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Status);
				_status = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Status);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public DateTime? StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			if (!EqualityComparer<DateTime?>.Default.Equals(_startDate, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StartDate);
				_startDate = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StartDate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public DateTime? EndDate
	{
		get
		{
			return _endDate;
		}
		set
		{
			if (!EqualityComparer<DateTime?>.Default.Equals(_endDate, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EndDate);
				_endDate = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EndDate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Notes
	{
		get
		{
			return _notes;
		}
		[MemberNotNull("_notes")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_notes, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Notes);
				_notes = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Notes);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsProcessing
	{
		get
		{
			return _isProcessing;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isProcessing, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsProcessing);
				_isProcessing = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsProcessing);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveProgramCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = saveProgramCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)SaveProgram);
				AsyncRelayCommand val2 = val;
				saveProgramCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
		}
	}

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
