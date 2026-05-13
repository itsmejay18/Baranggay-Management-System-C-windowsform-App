using System;

namespace baranggaysystem1;

internal sealed record BackupRunInfo(long? BackupRunId, DateTime StartedAt, DateTime? EndedAt, BackupRunState State, string? FilePath, long? FileSizeBytes, string? ErrorMessage, BackupMode Mode = BackupMode.Full, DateTime? BaselineStartedAt = null, string? TargetDescription = null);
