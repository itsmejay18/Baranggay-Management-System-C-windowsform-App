using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace baranggaysystem1.Database;

internal enum StartupHealthLevel
{
    Ok = 0,
    Warning = 1,
    Critical = 2
}

internal sealed record StartupHealthCheckResult(
    string CheckName,
    StartupHealthLevel Level,
    string Message);

internal sealed class StartupHealthReport
{
    private readonly List<StartupHealthCheckResult> _results = new List<StartupHealthCheckResult>();

    public DateTime CheckedAt { get; } = DateTime.Now;
    public IReadOnlyList<StartupHealthCheckResult> Results => _results;
    public bool HasWarnings => _results.Any(r => r.Level == StartupHealthLevel.Warning);
    public bool HasCriticalIssues => _results.Any(r => r.Level == StartupHealthLevel.Critical);
    public bool IsHealthy => !HasWarnings && !HasCriticalIssues;

    public void Add(string checkName, StartupHealthLevel level, string message)
    {
        _results.Add(new StartupHealthCheckResult(checkName, level, message));
    }

    public string ToMultilineText(bool includeOk = true)
    {
        var sb = new StringBuilder();
        foreach (StartupHealthCheckResult result in _results)
        {
            if (!includeOk && result.Level == StartupHealthLevel.Ok)
            {
                continue;
            }

            sb.AppendLine($"- [{result.Level}] {result.CheckName}: {result.Message}");
        }

        return sb.ToString().TrimEnd();
    }
}

