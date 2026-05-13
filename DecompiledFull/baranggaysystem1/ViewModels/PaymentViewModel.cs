using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public class PaymentViewModel : ObservableObject
{
	private readonly PaymentLedgerService _paymentLedgerService = new PaymentLedgerService();

	private readonly BarangayOfficialService _barangayOfficialService = new BarangayOfficialService();

	[ObservableProperty]
	private int _residentId;

	[ObservableProperty]
	private string _residentName = string.Empty;

	[ObservableProperty]
	private string _paymentMethod = "Cash";

	[ObservableProperty]
	private decimal _amount;

	[ObservableProperty]
	private string _orNumber = string.Empty;

	[ObservableProperty]
	private string _remarks = "Resident fee payment";

	[ObservableProperty]
	private bool _isProcessing;

	[ObservableProperty]
	private bool _showResidentPicker;

	[ObservableProperty]
	private OfficialResidentOption? _selectedResident;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? savePaymentCommand;

	public ObservableCollection<string> PaymentMethods { get; } = new ObservableCollection<string> { "Cash", "GCash", "Bank" };

	public ObservableCollection<OfficialResidentOption> ResidentOptions { get; } = new ObservableCollection<OfficialResidentOption>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ResidentId
	{
		get
		{
			return _residentId;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_residentId, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResidentId);
				_residentId = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResidentId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ResidentName
	{
		get
		{
			return _residentName;
		}
		[MemberNotNull("_residentName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_residentName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResidentName);
				_residentName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResidentName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string PaymentMethod
	{
		get
		{
			return _paymentMethod;
		}
		[MemberNotNull("_paymentMethod")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_paymentMethod, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PaymentMethod);
				_paymentMethod = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PaymentMethod);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public decimal Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (!EqualityComparer<decimal>.Default.Equals(_amount, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Amount);
				_amount = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Amount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string OrNumber
	{
		get
		{
			return _orNumber;
		}
		[MemberNotNull("_orNumber")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_orNumber, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OrNumber);
				_orNumber = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OrNumber);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Remarks
	{
		get
		{
			return _remarks;
		}
		[MemberNotNull("_remarks")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_remarks, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Remarks);
				_remarks = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Remarks);
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowResidentPicker
	{
		get
		{
			return _showResidentPicker;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showResidentPicker, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowResidentPicker);
				_showResidentPicker = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowResidentPicker);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public OfficialResidentOption? SelectedResident
	{
		get
		{
			return _selectedResident;
		}
		set
		{
			if (!EqualityComparer<OfficialResidentOption>.Default.Equals(_selectedResident, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedResident);
				_selectedResident = value;
				OnSelectedResidentChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedResident);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SavePaymentCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = savePaymentCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)SavePayment);
				AsyncRelayCommand val2 = val;
				savePaymentCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
		}
	}

	public event Action<bool?>? CloseRequested;

	public PaymentViewModel()
	{
		ShowResidentPicker = true;
		OrNumber = BuildOrNumber();
	}

	public PaymentViewModel(int residentId, string residentName, decimal initialAmount = 50.00m)
		: this()
	{
		ResidentId = residentId;
		ResidentName = residentName;
		Amount = initialAmount;
		ShowResidentPicker = residentId <= 0;
	}

	public async Task InitializeAsync()
	{
		if (!ShowResidentPicker)
		{
			return;
		}
		try
		{
			IsProcessing = true;
			ResidentOptions.Clear();
			foreach (OfficialResidentOption item in (await _barangayOfficialService.GetResidentOptionsAsync()).OrderBy((OfficialResidentOption option) => option.FullName))
			{
				ResidentOptions.Add(item);
			}
			if (ResidentId > 0)
			{
				SelectedResident = ResidentOptions.FirstOrDefault((OfficialResidentOption option) => option.ResidentId == ResidentId);
			}
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Payment resident picker failed to load.", ex);
			DialogService.Instance.ShowError("Could not load the resident list for payment entry.");
		}
		finally
		{
			IsProcessing = false;
		}
	}

	[RelayCommand]
	private async Task SavePayment()
	{
		if (ResidentId <= 0 || string.IsNullOrWhiteSpace(ResidentName))
		{
			DialogService.Instance.ShowWarning("Select a resident before recording payment.");
			return;
		}
		if (Amount <= 0m)
		{
			DialogService.Instance.ShowWarning("Please enter a valid payment amount.");
			return;
		}
		if (string.IsNullOrWhiteSpace(OrNumber))
		{
			DialogService.Instance.ShowWarning("Official receipt number is required.");
			return;
		}
		try
		{
			IsProcessing = true;
			string text = await _paymentLedgerService.RecordPaymentAsync(ResidentId, ResidentName, Amount, OrNumber, PaymentMethod, Remarks);
			DialogService.Instance.ShowInfo("Payment recorded successfully.\nOfficial Receipt: " + text);
			this.CloseRequested?.Invoke(true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Payment recording failed.", ex);
			DialogService.Instance.ShowError("Could not process the payment transaction.");
		}
		finally
		{
			IsProcessing = false;
		}
	}

	private static string BuildOrNumber()
	{
		return $"OR-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedResidentChanged(OfficialResidentOption? value)
	{
		if (value != null)
		{
			ResidentId = value.ResidentId;
			ResidentName = value.FullName;
		}
	}
}
