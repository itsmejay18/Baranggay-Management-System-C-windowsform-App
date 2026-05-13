using System;
using System.Collections.Generic;
using System.Globalization;

namespace baranggaysystem1.helper;

public static class FormatHelper
{
	public static string FormatResidentName(string? firstName, string? middleName, string? lastName, string? suffix)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(firstName))
		{
			list.Add(firstName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(middleName))
		{
			list.Add(middleName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(lastName))
		{
			list.Add(lastName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(suffix))
		{
			list.Add(suffix.Trim());
		}
		if (list.Count <= 0)
		{
			return "—";
		}
		return string.Join(" ", list);
	}

	public static string FormatAge(int? age)
	{
		if (!age.HasValue || age.Value < 0)
		{
			return "—";
		}
		return age.Value.ToString(CultureInfo.InvariantCulture);
	}

	public static int? ComputeAge(DateTime? birthDate)
	{
		if (!birthDate.HasValue)
		{
			return null;
		}
		DateTime today = DateTime.Today;
		int num = today.Year - birthDate.Value.Year;
		if (birthDate.Value.Date > today.AddYears(-num))
		{
			num--;
		}
		return num;
	}

	public static string FormatDateTime(DateTime? value)
	{
		if (!value.HasValue)
		{
			return "—";
		}
		return value.Value.ToString("MMM dd, yyyy \nhh:mm tt", CultureInfo.InvariantCulture);
	}

	public static string FormatDate(DateTime? value)
	{
		if (!value.HasValue)
		{
			return "—";
		}
		return value.Value.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
	}

	public static string FormatCoordinates(decimal? latitude, decimal? longitude)
	{
		if (!latitude.HasValue || !longitude.HasValue)
		{
			return "—";
		}
		return latitude.Value.ToString("F6", CultureInfo.InvariantCulture) + ", " + longitude.Value.ToString("F6", CultureInfo.InvariantCulture);
	}

	public static string FormatHouseholdAddress(string? houseNo, string? street, string? subdivision, string? addressNote, string? purokName)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(houseNo))
		{
			list.Add(houseNo.Trim());
		}
		if (!string.IsNullOrWhiteSpace(street))
		{
			list.Add(street.Trim());
		}
		if (!string.IsNullOrWhiteSpace(subdivision))
		{
			list.Add(subdivision.Trim());
		}
		if (!string.IsNullOrWhiteSpace(addressNote))
		{
			list.Add(addressNote.Trim());
		}
		if (!string.IsNullOrWhiteSpace(purokName))
		{
			list.Add(purokName.Trim());
		}
		if (list.Count <= 0)
		{
			return "—";
		}
		return string.Join(", ", list);
	}

	public static string Fallback(string? value, string fallback = "—")
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return fallback;
	}
}
