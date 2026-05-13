namespace baranggaysystem1.Database;

internal sealed class ConnectionTestResult
{
	public bool Success { get; }

	public bool DatabaseMissing { get; }

	public string Message { get; }

	private ConnectionTestResult(bool success, bool databaseMissing, string message)
	{
		Success = success;
		DatabaseMissing = databaseMissing;
		Message = message;
	}

	public static ConnectionTestResult Pass(string message, bool databaseMissing = false)
	{
		return new ConnectionTestResult(success: true, databaseMissing, message);
	}

	public static ConnectionTestResult Fail(string message)
	{
		return new ConnectionTestResult(success: false, databaseMissing: false, message);
	}
}
