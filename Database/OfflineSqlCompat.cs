using Microsoft.Data.Sqlite;
using System;
using System.Globalization;
using System.Linq;

namespace baranggaysystem1.Database
{
    internal static class OfflineSqlCompat
    {
        public static void RegisterFunctions(SqliteConnection conn)
        {
            conn.CreateFunction<string>("NOW", () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            conn.CreateFunction<string>("CURDATE", () => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            conn.CreateFunction<string?, string?, string>("CONCAT", (a, b) => string.Concat(a ?? string.Empty, b ?? string.Empty));
            conn.CreateFunction<string?, string?, string?, string>("CONCAT", (a, b, c) => string.Concat(a ?? string.Empty, b ?? string.Empty, c ?? string.Empty));
            conn.CreateFunction<string?, string?, string?, string?, string>("CONCAT", (a, b, c, d) =>
                string.Concat(a ?? string.Empty, b ?? string.Empty, c ?? string.Empty, d ?? string.Empty));

            conn.CreateFunction<string?, string?, string?, string?, string>("CONCAT_WS", (sep, a, b, c) =>
                JoinWs(sep, a, b, c));
            conn.CreateFunction<string?, string?, string?, string?, string?, string>("CONCAT_WS", (sep, a, b, c, d) =>
                JoinWs(sep, a, b, c, d));

            conn.CreateFunction<object?, object?, object?, object?>("IF", (condition, whenTrue, whenFalse) =>
                ToBool(condition) ? whenTrue : whenFalse);

            conn.CreateFunction<string?, string?, string?, long>("TIMESTAMPDIFF", (unit, startText, endText) =>
            {
                if (!DateTime.TryParse(startText, out DateTime start) || !DateTime.TryParse(endText, out DateTime end))
                {
                    return 0;
                }

                string u = (unit ?? string.Empty).Trim().ToUpperInvariant();
                if (u == "YEAR")
                {
                    int years = end.Year - start.Year;
                    if (end < start.AddYears(years))
                    {
                        years--;
                    }

                    return years;
                }

                if (u == "MONTH")
                {
                    int months = (end.Year - start.Year) * 12 + (end.Month - start.Month);
                    if (end < start.AddMonths(months))
                    {
                        months--;
                    }

                    return months;
                }

                if (u == "DAY")
                {
                    return (long)(end - start).TotalDays;
                }

                return (long)(end - start).TotalSeconds;
            });

            conn.CreateFunction<string?, string?, string>("DATE_FORMAT", (dateText, mysqlFormat) =>
            {
                if (!DateTime.TryParse(dateText, out DateTime dt))
                {
                    return string.Empty;
                }

                string format = ConvertMySqlDateFormat(mysqlFormat ?? "%Y-%m-%d");
                return dt.ToString(format, CultureInfo.InvariantCulture);
            });
        }

        public static string NormalizeSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            string normalized = sql.Replace("`", string.Empty, StringComparison.Ordinal);
            normalized = normalized.Replace("NOW()", "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase);
            normalized = normalized.Replace("CURDATE()", "date('now')", StringComparison.OrdinalIgnoreCase);
            normalized = normalized.Replace("IFNULL", "COALESCE", StringComparison.OrdinalIgnoreCase);
            return normalized;
        }

        private static string JoinWs(string? separator, params string?[] values)
        {
            string sep = separator ?? string.Empty;
            return string.Join(sep, values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()));
        }

        private static bool ToBool(object? condition)
        {
            if (condition == null || condition == DBNull.Value)
            {
                return false;
            }

            if (condition is bool b)
            {
                return b;
            }

            if (condition is byte by)
            {
                return by != 0;
            }

            if (condition is short s)
            {
                return s != 0;
            }

            if (condition is int i)
            {
                return i != 0;
            }

            if (condition is long l)
            {
                return l != 0;
            }

            if (condition is string text)
            {
                return !string.IsNullOrWhiteSpace(text) &&
                       !string.Equals(text.Trim(), "0", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(text.Trim(), "false", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private static string ConvertMySqlDateFormat(string mysqlFormat)
        {
            return mysqlFormat
                .Replace("%Y", "yyyy", StringComparison.Ordinal)
                .Replace("%y", "yy", StringComparison.Ordinal)
                .Replace("%m", "MM", StringComparison.Ordinal)
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
}
