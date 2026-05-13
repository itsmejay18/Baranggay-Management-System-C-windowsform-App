using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

internal partial class HouseholdCertificateViewModel : ObservableObject
{
	private readonly HouseholdRepository _repository = new HouseholdRepository();

	private readonly int _barangayId;

	private readonly int _householdId;

	[ObservableProperty]
	private string _householdLabel = string.Empty;

	[ObservableProperty]
	private string _householdAddress = string.Empty;

	[ObservableProperty]
	private string _purokLabel = string.Empty;

	[ObservableProperty]
	private string _memberSummary = string.Empty;

	[ObservableProperty]
	private string _purpose = "For household verification and barangay record purposes.";

	[ObservableProperty]
	private string _presentedTo = "Any concerned party";

	[ObservableProperty]
	private bool _includeMemberRoster = true;

	[ObservableProperty]
	private DateTime _issuedDate = DateTime.Today;

	[ObservableProperty]
	private bool _isGenerating;

	public Action<bool>? CloseAction { get; set; }

	public HouseholdCertificateViewModel(int householdId)
	{
		_householdId = householdId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		LoadHouseholdSummary();
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke(obj: false);
	}

	[RelayCommand]
	private async Task GenerateAsync()
	{
		if (!Permissions.CanIssueCertificates)
		{
			DialogService.Instance.ShowWarning("You do not have permission to generate household certificates.");
			return;
		}
		if (string.IsNullOrWhiteSpace(Purpose))
		{
			DialogService.Instance.ShowWarning("Please enter the purpose of the household certificate.");
			return;
		}
		if (IssuedDate.Date > DateTime.Today)
		{
			DialogService.Instance.ShowWarning("Issued date cannot be in the future.");
			return;
		}
		try
		{
			IsGenerating = true;
			string text = await Task.Run(() => HouseholdCertificateService.GeneratePdf(_householdId, new HouseholdCertificateRequest
			{
				Purpose = Purpose.Trim(),
				PresentedTo = PresentedTo.Trim(),
				IncludeMemberRoster = IncludeMemberRoster,
				IssuedDate = IssuedDate,
				GeneratedBy = (string.IsNullOrWhiteSpace(UserSession.Username) ? "Barangay Staff" : UserSession.Username)
			}));
			HouseholdCertificateService.TryOpenGeneratedFile(text);
			DialogService.Instance.ShowInfo("Household certificate generated successfully.\n\nSaved to:\n" + text);
			CloseAction?.Invoke(obj: true);
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Household certificate generation failed.", ex);
			DialogService.Instance.ShowError(ex.Message, "Household Certificate");
		}
		finally
		{
			IsGenerating = false;
		}
	}

	private void LoadHouseholdSummary()
	{
		HouseholdDetailsDto details = _repository.GetDetails(_householdId, _barangayId);
		if (details == null)
		{
			throw new InvalidOperationException("Selected household could not be loaded.");
		}
		HouseholdLabel = $"Household #{details.HouseholdId}";
		HouseholdAddress = (string.IsNullOrWhiteSpace(details.FullAddress) ? $"Household #{details.HouseholdId}" : details.FullAddress);
		PurokLabel = (string.IsNullOrWhiteSpace(details.PurokName) ? "Purok not set" : details.PurokName);
		MemberSummary = $"{details.MemberCount} member(s) | Seniors: {details.SeniorCount} | PWD: {details.PwdCount} | 4Ps: {details.FourPsCount}";
	}
}
