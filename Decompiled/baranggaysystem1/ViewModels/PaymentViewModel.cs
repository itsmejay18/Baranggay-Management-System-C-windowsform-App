using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public partial class PaymentViewModel : ObservableObject
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

	public ObservableCollection<string> PaymentMethods { get; } = new ObservableCollection<string> { "Cash", "GCash", "Bank" };

	public ObservableCollection<OfficialResidentOption> ResidentOptions { get; } = new ObservableCollection<OfficialResidentOption>();

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
}
