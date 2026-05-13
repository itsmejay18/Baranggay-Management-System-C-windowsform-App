using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace baranggaysystem1.Database;

internal sealed class StartupHealthReport
{
	private readonly List<StartupHealthCheckResult> _results = new List<StartupHealthCheckResult>();

	public DateTime CheckedAt { get; } = DateTime.Now;

	public IReadOnlyList<StartupHealthCheckResult> Results => _results;

	public bool HasWarnings => _results.Any((StartupHealthCheckResult r) => r.Level == StartupHealthLevel.Warning);

	public bool HasCriticalIssues => _results.Any((StartupHealthCheckResult r) => r.Level == StartupHealthLevel.Critical);

	public bool IsHealthy
	{
		get
		{
			if (!HasWarnings)
			{
				return !HasCriticalIssues;
			}
			return false;
		}
	}

	public void Add(string checkName, StartupHealthLevel level, string message)
	{
		_results.Add(new StartupHealthCheckResult(checkName, level, message));
	}

	public string ToMultilineText(bool includeOk = true)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (StartupHealthCheckResult result in _results)
		{
			if (includeOk || result.Level != StartupHealthLevel.Ok)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 3, stringBuilder2);
				handler.AppendLiteral("- [");
				handler.AppendFormatted(result.Level);
				handler.AppendLiteral("] ");
				handler.AppendFormatted(result.CheckName);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(result.Message);
				stringBuilder2.AppendLine(ref handler);
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}
}
