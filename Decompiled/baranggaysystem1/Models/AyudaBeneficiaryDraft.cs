using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;

namespace baranggaysystem1.Models;

public partial class AyudaBeneficiaryDraft : ObservableObject
{
	[ObservableProperty]
	private int _persistedReleaseId;

	[ObservableProperty]
	private int _residentId;

	[ObservableProperty]
	private string _residentName = string.Empty;

	[ObservableProperty]
	private string _contactNo = string.Empty;

	[ObservableProperty]
	private decimal _amount;

	public string AmountDisplay => $"PHP {Amount:N2}";

}
