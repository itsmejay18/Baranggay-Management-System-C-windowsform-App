using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace baranggaysystem1.Models;

public class AyudaBeneficiaryDraft : ObservableObject
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int PersistedReleaseId
	{
		get
		{
			return _persistedReleaseId;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_persistedReleaseId, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PersistedReleaseId);
				_persistedReleaseId = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PersistedReleaseId);
			}
		}
	}

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
	public string ContactNo
	{
		get
		{
			return _contactNo;
		}
		[MemberNotNull("_contactNo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_contactNo, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ContactNo);
				_contactNo = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ContactNo);
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
				OnAmountChanged(value);
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Amount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnAmountChanged(decimal value)
	{
		((ObservableObject)this).OnPropertyChanged("AmountDisplay");
	}
}
