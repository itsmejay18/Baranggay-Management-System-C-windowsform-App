using System;
using System.Collections.Generic;

namespace baranggaysystem1.Services;

internal sealed class SystemLogSnapshot
{
	public IReadOnlyList<SystemLogEntry> Entries { get; }

	public int AuditCount { get; }

	public int ApplicationCount { get; }

	public int ErrorCount { get; }

	public int ActiveUsers { get; }

	public int ModuleCount { get; }

	public DateTime LoadedAt { get; }

	public SystemLogSnapshot(IReadOnlyList<SystemLogEntry> entries, int auditCount, int applicationCount, int errorCount, int activeUsers, int moduleCount)
	{
		Entries = entries;
		AuditCount = auditCount;
		ApplicationCount = applicationCount;
		ErrorCount = errorCount;
		ActiveUsers = activeUsers;
		ModuleCount = moduleCount;
		LoadedAt = DateTime.Now;
	}
}
