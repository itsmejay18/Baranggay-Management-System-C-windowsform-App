using System;

namespace baranggaysystem1;

internal readonly record struct SlaEvaluation(SlaState State, string Stage, DateTime? DueDate, int? DaysRemaining, int? DaysOverdue)
{
	internal bool Applies => State != SlaState.NotApplicable;
}
