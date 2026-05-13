using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using baranggaysystem1.Models;
using baranggaysystem1.Services;

namespace baranggaysystem1.ViewModels;

public class StaffDetailsViewModel : ObservableObject
{
	private readonly StaffProfileDetails? _originalRecord;

	private readonly StaffService _dataService;

	[ObservableProperty]
	private string _title;

	[ObservableProperty]
	private string _username = string.Empty;

	[ObservableProperty]
	private string _firstName = string.Empty;

	[ObservableProperty]
	private string _middleName = string.Empty;

	[ObservableProperty]
	private string _lastName = string.Empty;

	[ObservableProperty]
	private string _email = string.Empty;

	[ObservableProperty]
	private string _contactNumber = string.Empty;

	[ObservableProperty]
	private string _position = string.Empty;

	[ObservableProperty]
	private string _department = string.Empty;

	[ObservableProperty]
	private string _roleName = "Standard";

	[ObservableProperty]
	private bool _isActive = true;

	[ObservableProperty]
	private bool _isNewUser;

	[ObservableProperty]
	private string _temporaryPassword = string.Empty;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? saveCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelCommand;

	public Action? CloseAction { get; set; }

	public List<string> RoleOptions { get; } = new List<string> { "Super Admin", "Admin", "Standard", "Viewer" };

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Title
	{
		get
		{
			return _title;
		}
		[MemberNotNull("_title")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_title, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Title);
				_title = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Title);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Username
	{
		get
		{
			return _username;
		}
		[MemberNotNull("_username")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_username, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Username);
				_username = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Username);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string FirstName
	{
		get
		{
			return _firstName;
		}
		[MemberNotNull("_firstName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_firstName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FirstName);
				_firstName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FirstName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string MiddleName
	{
		get
		{
			return _middleName;
		}
		[MemberNotNull("_middleName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_middleName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MiddleName);
				_middleName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MiddleName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastName
	{
		get
		{
			return _lastName;
		}
		[MemberNotNull("_lastName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastName);
				_lastName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Email
	{
		get
		{
			return _email;
		}
		[MemberNotNull("_email")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_email, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Email);
				_email = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Email);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ContactNumber
	{
		get
		{
			return _contactNumber;
		}
		[MemberNotNull("_contactNumber")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_contactNumber, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ContactNumber);
				_contactNumber = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ContactNumber);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Position
	{
		get
		{
			return _position;
		}
		[MemberNotNull("_position")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_position, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Position);
				_position = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Position);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Department
	{
		get
		{
			return _department;
		}
		[MemberNotNull("_department")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_department, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Department);
				_department = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Department);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string RoleName
	{
		get
		{
			return _roleName;
		}
		[MemberNotNull("_roleName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_roleName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RoleName);
				_roleName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RoleName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isActive, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsActive);
				_isActive = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsActive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsNewUser
	{
		get
		{
			return _isNewUser;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isNewUser, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsNewUser);
				_isNewUser = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsNewUser);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string TemporaryPassword
	{
		get
		{
			return _temporaryPassword;
		}
		[MemberNotNull("_temporaryPassword")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_temporaryPassword, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TemporaryPassword);
				_temporaryPassword = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TemporaryPassword);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SaveCommand
	{
		get
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Expected O, but got Unknown
			//IL_0023: Expected O, but got Unknown
			AsyncRelayCommand obj = saveCommand;
			if (obj == null)
			{
				AsyncRelayCommand val = new AsyncRelayCommand((Func<Task>)SaveAsync);
				AsyncRelayCommand val2 = val;
				saveCommand = val;
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

	public StaffDetailsViewModel(StaffProfileDetails? existingRecord = null)
	{
		_dataService = new StaffService();
		if (existingRecord != null)
		{
			Title = "Edit Staff Profile";
			IsNewUser = false;
			_originalRecord = existingRecord;
			Username = existingRecord.Username;
			FirstName = existingRecord.FirstName;
			MiddleName = existingRecord.MiddleName;
			LastName = existingRecord.LastName;
			Email = existingRecord.Email;
			ContactNumber = existingRecord.ContactNumber;
			Position = existingRecord.Position;
			Department = existingRecord.Department;
			RoleName = existingRecord.RoleName;
			IsActive = existingRecord.IsActive;
		}
		else
		{
			Title = "Register New Staff";
			IsNewUser = true;
			_originalRecord = null;
			TemporaryPassword = GenerateTemporaryPassword();
		}
	}

	private string GenerateTemporaryPassword()
	{
		return "Setup_" + new Random().Next(1000, 9999);
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
		{
			DialogService.Instance.ShowWarning("Username, First Name, and Last Name are required.");
			return;
		}
		StaffProfileDetails staffProfileDetails = new StaffProfileDetails
		{
			UserId = (_originalRecord?.UserId ?? 0),
			Username = Username.Trim(),
			FirstName = FirstName.Trim(),
			MiddleName = MiddleName.Trim(),
			LastName = LastName.Trim(),
			Email = Email.Trim(),
			ContactNumber = ContactNumber.Trim(),
			Position = Position.Trim(),
			Department = Department.Trim(),
			RoleName = RoleName,
			IsActive = IsActive
		};
		DialogService.Instance.ShowInfo("Successfully saved staff profile for " + staffProfileDetails.Username + ".");
		CloseAction?.Invoke();
		await Task.CompletedTask;
	}

	[RelayCommand]
	private void Cancel()
	{
		CloseAction?.Invoke();
	}
}
