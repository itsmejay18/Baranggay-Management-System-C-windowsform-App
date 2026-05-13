using System;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace baranggaysystem1.Database;

internal static class OfflineSqlCompat
{
	public static void RegisterFunctions(SqliteConnection conn)
	{
		conn.CreateFunction<string>("NOW", (Func<string>)(() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)), false);
		conn.CreateFunction<string>("CURDATE", (Func<string>)(() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), false);
		conn.CreateFunction<object, object, string>("CONCAT", (Func<object, object, string>)((object? a, object? b) => ConcatParts(a, b)), false);
		conn.CreateFunction<object, object, object, string>("CONCAT", (Func<object, object, object, string>)((object? a, object? b, object? c) => ConcatParts(a, b, c)), false);
		conn.CreateFunction<object, object, object, object, string>("CONCAT", (Func<object, object, object, object, string>)((object? a, object? b, object? c, object? d) => ConcatParts(a, b, c, d)), false);
		conn.CreateFunction<object, object, object, object, object, string>("CONCAT", (Func<object, object, object, object, object, string>)((object? a, object? b, object? c, object? d, object? e) => ConcatParts(a, b, c, d, e)), false);
		conn.CreateFunction<object, object, object, object, object, object, string>("CONCAT", (Func<object, object, object, object, object, object, string>)((object? a, object? b, object? c, object? d, object? e, object? f) => ConcatParts(a, b, c, d, e, f)), false);
		conn.CreateFunction<object, object, object, string>("LPAD", (Func<object, object, object, string>)delegate(object? value, object? lengthArg, object? padArg)
		{
			string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
			string text2 = Convert.ToString(padArg, CultureInfo.InvariantCulture) ?? " ";
			int num = Convert.ToInt32(lengthArg, CultureInfo.InvariantCulture);
			if (num <= 0)
			{
				return string.Empty;
			}
			if (text.Length >= num)
			{
				return text.Substring(0, num);
			}
			if (string.IsNullOrEmpty(text2))
			{
				return text;
			}
			int num2 = num - text.Length;
			string text3 = string.Concat(Enumerable.Repeat(text2, (num2 + text2.Length - 1) / text2.Length));
			if (text3.Length > num2)
			{
				text3 = text3.Substring(0, num2);
			}
			return text3 + text;
		}, false);
		conn.CreateFunction<string, object, string>("CONCAT_WS", (Func<string, object, string>)((string? sep, object? a) => JoinWs(sep, a)), false);
		conn.CreateFunction<string, object, object, string>("CONCAT_WS", (Func<string, object, object, string>)((string? sep, object? a, object? b) => JoinWs(sep, a, b)), false);
		conn.CreateFunction<string, object, object, object, string>("CONCAT_WS", (Func<string, object, object, object, string>)((string? sep, object? a, object? b, object? c) => JoinWs(sep, a, b, c)), false);
		conn.CreateFunction<string, object, object, object, object, string>("CONCAT_WS", (Func<string, object, object, object, object, string>)((string? sep, object? a, object? b, object? c, object? d) => JoinWs(sep, a, b, c, d)), false);
		conn.CreateFunction<string, object, object, object, object, object, string>("CONCAT_WS", (Func<string, object, object, object, object, object, string>)((string? sep, object? a, object? b, object? c, object? d, object? e) => JoinWs(sep, a, b, c, d, e)), false);
		conn.CreateFunction<object, object, object, object>("IF", (Func<object, object, object, object>)((object? condition, object? whenTrue, object? whenFalse) => (!ToBool(condition)) ? whenFalse : whenTrue), false);
		conn.CreateFunction<string, string, string, long>("TIMESTAMPDIFF", (Func<string, string, string, long>)delegate(string? unit, string? startText, string? endText)
		{
			if (!DateTime.TryParse(startText, out var result) || !DateTime.TryParse(endText, out var result2))
			{
				return 0L;
			}
			switch ((unit ?? string.Empty).Trim().ToUpperInvariant())
			{
			case "YEAR":
			{
				int num2 = result2.Year - result.Year;
				if (result2 < result.AddYears(num2))
				{
					num2--;
				}
				return num2;
			}
			case "MONTH":
			{
				int num = (result2.Year - result.Year) * 12 + (result2.Month - result.Month);
				if (result2 < result.AddMonths(num))
				{
					num--;
				}
				return num;
			}
			case "DAY":
				return (long)(result2 - result).TotalDays;
			default:
				return (long)(result2 - result).TotalSeconds;
			}
		}, false);
		conn.CreateFunction<string, string, string>("DATE_FORMAT", (Func<string, string, string>)delegate(string? dateText, string? mysqlFormat)
		{
			if (!DateTime.TryParse(dateText, out var result))
			{
				return string.Empty;
			}
			string text = ConvertMySqlDateFormat(mysqlFormat ?? "%Y-%m-%d");
			return result.ToString(text, CultureInfo.InvariantCulture);
		}, false);
	}

	public static string NormalizeSql(string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
		{
			return sql;
		}
		return NormalizeDateIntervalFunctions(sql.Replace("`", string.Empty, StringComparison.Ordinal).Replace("NOW()", "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase).Replace("CURDATE()", "date('now')", StringComparison.OrdinalIgnoreCase)
			.Replace("IFNULL", "COALESCE", StringComparison.OrdinalIgnoreCase));
	}

	private static string NormalizeDateIntervalFunctions(string sql)
	{
		return NormalizeDateIntervalFunction(NormalizeDateIntervalFunction(sql, "DATE_ADD", "+"), "DATE_SUB", "-");
	}

	private static string NormalizeDateIntervalFunction(string sql, string functionName, string sign)
	{
		int startIndex = 0;
		int functionIndex;
		int openParenIndex;
		int closeParenIndex;
		while (TryFindFunctionCall(sql, functionName, startIndex, out functionIndex, out openParenIndex, out closeParenIndex))
		{
			if (!TrySplitTopLevelArguments(sql.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1), out string first, out string second) || !TryParseIntervalPart(second, out string amountExpression, out string unit))
			{
				startIndex = closeParenIndex + 1;
				continue;
			}
			string text = $"DATETIME({first.Trim()}, '{sign}' || ({amountExpression.Trim()}) || ' {unit.ToLowerInvariant()}')";
			sql = sql.Substring(0, functionIndex) + text + sql.Substring(closeParenIndex + 1);
			startIndex = functionIndex + text.Length;
		}
		return sql;
	}

	private static bool TryFindFunctionCall(string sql, string functionName, int startIndex, out int functionIndex, out int openParenIndex, out int closeParenIndex)
	{
		functionIndex = -1;
		openParenIndex = -1;
		closeParenIndex = -1;
		for (int num = sql.IndexOf(functionName, startIndex, StringComparison.OrdinalIgnoreCase); num >= 0; num = sql.IndexOf(functionName, num + functionName.Length, StringComparison.OrdinalIgnoreCase))
		{
			int i;
			for (i = num + functionName.Length; i < sql.Length && char.IsWhiteSpace(sql[i]); i++)
			{
			}
			if (i < sql.Length && sql[i] == '(')
			{
				int num2 = 0;
				for (int j = i; j < sql.Length; j++)
				{
					if (sql[j] == '(')
					{
						num2++;
					}
					else if (sql[j] == ')')
					{
						num2--;
						if (num2 == 0)
						{
							functionIndex = num;
							openParenIndex = i;
							closeParenIndex = j;
							return true;
						}
					}
				}
				return false;
			}
		}
		return false;
	}

	private static bool TrySplitTopLevelArguments(string arguments, out string first, out string second)
	{
		first = string.Empty;
		second = string.Empty;
		int num = 0;
		for (int i = 0; i < arguments.Length; i++)
		{
			switch (arguments[i])
			{
			case '(':
				num++;
				break;
			case ')':
				num--;
				break;
			case ',':
				if (num == 0)
				{
					first = arguments.Substring(0, i);
					second = arguments.Substring(i + 1);
					return true;
				}
				break;
			}
		}
		return false;
	}

	private static bool TryParseIntervalPart(string intervalPart, out string amountExpression, out string unit)
	{
		amountExpression = string.Empty;
		unit = string.Empty;
		string text = intervalPart.Trim();
		if (!text.StartsWith("INTERVAL ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int num = 0;
		int num2 = text.Length - 1;
		while (num2 >= 0 && char.IsWhiteSpace(text[num2]))
		{
			num2--;
		}
		int num3 = num2;
		while (num3 >= 0 && char.IsLetter(text[num3]))
		{
			num3--;
		}
		unit = text.Substring(num3 + 1, num2 - num3).Trim();
		if (string.IsNullOrWhiteSpace(unit))
		{
			return false;
		}
		string text2 = text.Substring("INTERVAL ".Length, num3 + 1 - "INTERVAL ".Length).Trim();
		for (int i = 0; i < text2.Length; i++)
		{
			if (text2[i] == '(')
			{
				num++;
			}
			else if (text2[i] == ')')
			{
				num--;
			}
		}
		if (num != 0 || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		amountExpression = text2;
		return true;
	}

	private static string JoinWs(string? separator, params object?[] values)
	{
		return string.Join(separator ?? string.Empty, from v in values.Select(ConvertToText)
			where !string.IsNullOrWhiteSpace(v)
			select v.Trim());
	}

	private static string ConcatParts(params object?[] values)
	{
		return string.Concat(values.Select(ConvertToText));
	}

	private static string ConvertToText(object? value)
	{
		if (value == null || value == DBNull.Value)
		{
			return string.Empty;
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
	}

	private static bool ToBool(object? condition)
	{
		if (condition == null || condition == DBNull.Value)
		{
			return false;
		}
		if (condition is bool)
		{
			return (bool)condition;
		}
		if (condition is byte b)
		{
			return b != 0;
		}
		if (condition is short num)
		{
			return num != 0;
		}
		if (condition is int num2)
		{
			return num2 != 0;
		}
		if (condition is long num3)
		{
			return num3 != 0;
		}
		if (condition is string text)
		{
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text.Trim(), "0", StringComparison.OrdinalIgnoreCase))
			{
				return !string.Equals(text.Trim(), "false", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		return true;
	}

	private static string ConvertMySqlDateFormat(string mysqlFormat)
	{
		return mysqlFormat.Replace("%Y", "yyyy", StringComparison.Ordinal).Replace("%y", "yy", StringComparison.Ordinal).Replace("%m", "MM", StringComparison.Ordinal)
			.Replace("%c", "M", StringComparison.Ordinal)
			.Replace("%d", "dd", StringComparison.Ordinal)
			.Replace("%e", "d", StringComparison.Ordinal)
			.Replace("%H", "HH", StringComparison.Ordinal)
			.Replace("%h", "hh", StringComparison.Ordinal)
			.Replace("%i", "mm", StringComparison.Ordinal)
			.Replace("%s", "ss", StringComparison.Ordinal)
			.Replace("%p", "tt", StringComparison.Ordinal);
	}
}
