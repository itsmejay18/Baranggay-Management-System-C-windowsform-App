using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Services;

namespace baranggaysystem1.ViewModels;

public class UpdateUserViewModel : ObservableObject
{
	private readonly StaffService _dataService;

	[ObservableProperty]
	private int _targetUserId;

	[ObservableProperty]
	private string _targetUsername = string.Empty;

	[ObservableProperty]
	private string _newPassword = string.Empty;

	[ObservableProperty]
	private string _confirmPassword = string.Empty;

	[ObservableProperty]
	private bool _requirePasswordChange = true;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? updatePasswordCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelCommand;

	public Action? CloseAction { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int TargetUserId
	{
		get
		{
			return _targetUserId;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_targetUserId, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TargetUserId);
				_targetUserId = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TargetUserId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string TargetUsername
	{
		get
		{
			return _targetUsername;
		}
		[MemberNotNull("_targetUsername")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_targetUsername, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TargetUsername);
				_targetUsername = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TargetUsername);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string NewPassword
	{
		get
		{
			return _newPassword;
		}
		[MemberNotNull("_newPassword")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_newPassword, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NewPassword);
				_newPassword = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NewPassword);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ConfirmPassword
	{
		get
		{
			return _confirmPassword;
		}
		[MemberNotNull("_confirmPassword")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_confirmPassword, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConfirmPassword);
				_confirmPassword = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConfirmPassword);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool RequirePasswordChange
	{
		get
		{
			return _requirePasswordChange;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_requirePasswordChange, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RequirePasswordChange);
				_requirePasswordChange = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RequirePasswordChange);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand UpdatePasswordCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = updatePasswordCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)UpdatePasswordAsync);
				AsyncRelayCommand val2 = val;
				updatePasswordCommand = val;
				obj = val2;
			}
			return (IAsyncRelayCommand)(object)obj;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CancelCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			RelayCommand obj = cancelCommand;
			if (obj == null)
			{
				RelayCommand val = new RelayCommand((Action)Cancel);
				RelayCommand val2 = val;
				cancelCommand = val;
				obj = val2;
			}
			return (IRelayCommand)(object)obj;
		}
	}

	public UpdateUserViewModel(int targetUserId, string targetUsername)
	{
		_dataService = new StaffService();
		TargetUserId = targetUserId;
		TargetUsername = targetUsername;
	}

	[RelayCommand]
	private async Task UpdatePasswordAsync()
	{
		if (string.IsNullOrWhiteSpace(NewPassword))
		{
			DialogService.Instance.ShowWarning("New password cannot be empty.");
			return;
		}
		if (NewPassword != ConfirmPassword)
		{
			DialogService.Instance.ShowWarning("Passwords do not match.");
			return;
		}
		if (NewPassword.Length < 6)
		{
			DialogService.Instance.ShowWarning("Password must be at least 6 characters long.");
			return;
		}
		DialogService.Instance.ShowInfo("Successfully updated password for user '" + TargetUsername + "'.");
		CloseAction?.Invoke();
		await Task.CompletedTask;
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke();
	}
}
