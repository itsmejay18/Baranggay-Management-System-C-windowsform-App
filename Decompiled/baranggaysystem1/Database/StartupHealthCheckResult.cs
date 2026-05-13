namespace baranggaysystem1.Database;

internal sealed record StartupHealthCheckResult(string CheckName, StartupHealthLevel Level, string Message);
