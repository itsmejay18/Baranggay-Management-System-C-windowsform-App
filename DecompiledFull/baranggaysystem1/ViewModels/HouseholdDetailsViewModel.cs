using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.helper;

namespace baranggaysystem1.ViewModels;

public class HouseholdDetailsViewModel : ObservableObject
{
	private readonly int? _householdId;

	private readonly int? _startResidentId;

	private readonly int _barangayId;

	private readonly ResidentHouseholdService _residentHouseholdService = new ResidentHouseholdService();

	[ObservableProperty]
	private string _householdNumber = "N/A";

	[ObservableProperty]
	private string _address = "No Address Set";

	[ObservableProperty]
	private string _purokName = "N/A";

	[ObservableProperty]
	private int _memberCount;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private bool _hasChanges;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<HouseholdMember?>? setAsHeadCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<HouseholdMember?>? removeMemberCommand;

	public ObservableCollection<HouseholdMember> Members { get; } = new ObservableCollection<HouseholdMember>();

	public int? HouseholdId => _householdId;

	public bool CanManageMembers
	{
		get
		{
			if (_householdId.HasValue)
			{
				return _householdId.Value > 0;
			}
			return false;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HouseholdNumber
	{
		get
		{
			return _householdNumber;
		}
		[MemberNotNull("_householdNumber")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_householdNumber, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HouseholdNumber);
				_householdNumber = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HouseholdNumber);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Address
	{
		get
		{
			return _address;
		}
		[MemberNotNull("_address")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_address, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Address);
				_address = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Address);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string PurokName
	{
		get
		{
			return _purokName;
		}
		[MemberNotNull("_purokName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_purokName, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PurokName);
				_purokName = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PurokName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int MemberCount
	{
		get
		{
			return _memberCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_memberCount, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MemberCount);
				_memberCount = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MemberCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isLoading, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsLoading);
				_isLoading = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsLoading);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HasChanges
	{
		get
		{
			return _hasChanges;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hasChanges, value))
			{
				((ObservableObject)this).OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasChanges);
				_hasChanges = value;
				((ObservableObject)this).OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasChanges);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<HouseholdMember?> SetAsHeadCommand => (IAsyncRelayCommand<HouseholdMember?>)(object)(setAsHeadCommand ?? (setAsHeadCommand = new AsyncRelayCommand<HouseholdMember>((Func<HouseholdMember, Task>)SetAsHead)));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<HouseholdMember?> RemoveMemberCommand => (IAsyncRelayCommand<HouseholdMember?>)(object)(removeMemberCommand ?? (removeMemberCommand = new AsyncRelayCommand<HouseholdMember>((Func<HouseholdMember, Task>)RemoveMember)));

	public HouseholdDetailsViewModel(int? householdId, int? startResidentId = null)
	{
		_householdId = householdId;
		_startResidentId = startResidentId;
		_barangayId = HouseholdRepository.ResolveBarangayId(UserSession.BarangayId);
		if (CanManageMembers)
		{
			ReloadAsync();
			return;
		}
		HouseholdNumber = "NEW";
		Address = "Create or select a household first before adding family members.";
		PurokName = "No household selected";
		MemberCount = 0;
	}

	public async Task ReloadAsync()
	{
		await LoadHouseholdData();
	}

	public void MarkChanged()
	{
		HasChanges = true;
	}

	private async Task LoadHouseholdData()
	{
		if (!CanManageMembers)
		{
			Members.Clear();
			MemberCount = 0;
			return;
		}
		try
		{
			IsLoading = true;
			Members.Clear();
			await EnsureResidentHouseholdSchemaAsync();
			DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n                    SELECT h.household_id,\n                           h.house_no,\n                           h.street,\n                           h.subdivision,\n                           COALESCE(p.name, '') AS purok_name\n                    FROM household h\n                    LEFT JOIN purok_sitio p ON p.purok_id = h.purok_id\n                    WHERE h.household_id = @id", delegate(MySqlCommand cmd)
			{
				cmd.Parameters.AddWithValue("@id", (object)_householdId.Value);
			});
			if (dataTable.Rows.Count > 0)
			{
				DataRow dataRow = dataTable.Rows[0];
				string text = dataRow["house_no"]?.ToString()?.Trim() ?? string.Empty;
				string street = dataRow["street"]?.ToString()?.Trim() ?? string.Empty;
				string subdivision = dataRow["subdivision"]?.ToString()?.Trim() ?? string.Empty;
				HouseholdNumber = (string.IsNullOrWhiteSpace(text) ? _householdId.Value.ToString() : text);
				PurokName = dataRow["purok_name"]?.ToString() ?? "N/A";
				Address = HouseholdRepository.BuildAddressLabel(text, street, subdivision, PurokName);
				if (string.IsNullOrWhiteSpace(Address))
				{
					Address = $"Household #{_householdId.Value}";
				}
			}
			else
			{
				HouseholdNumber = _householdId.Value.ToString();
				Address = "Household record not found.";
				PurokName = "N/A";
			}
			foreach (DataRow row in (await LoadMembersTableAsync()).Rows)
			{
				string text2 = row["status"]?.ToString() ?? "ACTIVE";
				Members.Add(new HouseholdMember
				{
					ResidentId = Convert.ToInt32(row["resident_id"]),
					FullName = $"{row["first_name"]} {row["last_name"]}".Trim(),
					Gender = ((row["sex"]?.ToString() == "M") ? "Male" : "Female"),
					Age = ((row["birth_date"] != DBNull.Value) ? CalculateAge(Convert.ToDateTime(row["birth_date"])) : 0),
					IsHead = Convert.ToBoolean((row["is_head_of_family"] == DBNull.Value) ? ((object)0) : row["is_head_of_family"]),
					Status = (string.IsNullOrWhiteSpace(text2) ? "ACTIVE" : text2),
					IsCurrentContext = (_startResidentId.HasValue && Convert.ToInt32(row["resident_id"]) == _startResidentId.Value)
				});
			}
			MemberCount = Members.Count;
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to load household data", ex);
			DialogService.Instance.ShowError("Error loading family members.");
		}
		finally
		{
			IsLoading = false;
		}
	}

	private int CalculateAge(DateTime dob)
	{
		int num = DateTime.Today.Year - dob.Year;
		if (dob.Date > DateTime.Today.AddYears(-num))
		{
			num--;
		}
		return num;
	}

	[RelayCommand]
	private async Task SetAsHead(HouseholdMember? member)
	{
		if (member == null || member.IsHead || !CanManageMembers)
		{
			return;
		}
		try
		{
			IsLoading = true;
			await EnsureResidentHouseholdSchemaAsync();
			await Task.Run(delegate
			{
				//IL_001a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Expected O, but got Unknown
				//IL_0060: Unknown result type (might be due to invalid IL or missing references)
				//IL_0066: Expected O, but got Unknown
				MySqlConnection connection = DBConnection.GetConnection();
				try
				{
					((DbConnection)(object)connection).Open();
					MySqlTransaction val = connection.BeginTransaction();
					try
					{
						MySqlCommand val2 = new MySqlCommand("UPDATE resident SET is_head_of_family = 0 WHERE household_id = @householdId", connection, val);
						try
						{
							val2.Parameters.AddWithValue("@householdId", (object)_householdId.Value);
							((DbCommand)(object)val2).ExecuteNonQuery();
						}
						finally
						{
							((IDisposable)val2)?.Dispose();
						}
						MySqlCommand val3 = new MySqlCommand("UPDATE resident SET is_head_of_family = 1 WHERE resident_id = @residentId", connection, val);
						try
						{
							val3.Parameters.AddWithValue("@residentId", (object)member.ResidentId);
							((DbCommand)(object)val3).ExecuteNonQuery();
						}
						finally
						{
							((IDisposable)val3)?.Dispose();
						}
						((DbTransaction)(object)val).Commit();
					}
					finally
					{
						((IDisposable)val)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)connection)?.Dispose();
				}
			});
			HasChanges = true;
			await LoadHouseholdData();
			DialogService.Instance.ShowInfo(member.FullName + " is now the Head of Family.");
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to update family head", ex);
			DialogService.Instance.ShowError("Unable to update the head of family.");
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task RemoveMember(HouseholdMember? member)
	{
		if (member == null || !CanManageMembers || !DialogService.Instance.Confirm("Remove " + member.FullName + " from this household?"))
		{
			return;
		}
		try
		{
			IsLoading = true;
			await Task.Run(delegate
			{
				_residentHouseholdService.RemoveResidentFromHousehold(member.ResidentId, _barangayId, "Removed from household details window.");
			});
			HasChanges = true;
			await LoadHouseholdData();
		}
		catch (Exception ex)
		{
			AppLogger.LogError("Failed to remove household member", ex);
			DialogService.Instance.ShowError(ex.Message, "Remove Member");
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task EnsureResidentHouseholdSchemaAsync()
	{
		try
		{
			await Task.Run(delegate
			{
				SchemaGuard.EnsureDatabaseReady();
			});
		}
		catch (Exception ex)
		{
			AppLogger.LogWarning("Unable to finish household schema compatibility checks before loading family members.", ex);
		}
	}

	private async Task<DataTable> LoadMembersTableAsync()
	{
		DataTable result = default(DataTable);
		Exception ex2 = default(Exception);
		int num;
		try
		{
			result = await LoadMembersTableCoreAsync(includeHeadOfFamily: true);
			return result;
		}
		catch (Exception ex) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			ex2 = ex;
			return IsMissingColumn(ex, "is_head_of_family");
		}).Invoke())
		{
			num = 1;
		}
		if (num != 1)
		{
			return result;
		}
		AppLogger.LogWarning("resident.is_head_of_family was not available while loading household members. Retrying with compatibility query.", ex2);
		return await LoadMembersTableCoreAsync(includeHeadOfFamily: false);
	}

	private Task<DataTable> LoadMembersTableCoreAsync(bool includeHeadOfFamily)
	{
		string value = (includeHeadOfFamily ? "IFNULL(is_head_of_family, 0) AS is_head_of_family" : "0 AS is_head_of_family");
		string value2 = (includeHeadOfFamily ? "is_head_of_family DESC, " : string.Empty);
		return DatabaseManagerAsync.LoadTableAsync($"\n                    SELECT resident_id,\n                           first_name,\n                           last_name,\n                           sex,\n                           birth_date,\n                           status,\n                           {value}\n                    FROM resident\n                    WHERE household_id = @id\n                      AND IFNULL(is_deleted,0) = 0\n                    ORDER BY {value2}last_name, first_name", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@id", (object)_householdId.Value);
		});
	}

	private static bool IsMissingColumn(Exception ex, string columnName)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			MySqlException ex3 = (MySqlException)(object)((ex2 is MySqlException) ? ex2 : null);
			if (ex3 != null && ex3.Number == 1054)
			{
				return true;
			}
			if ((ex2.Message ?? string.Empty).IndexOf(columnName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}
}
