using System;
using System.Collections.Generic;

namespace baranggaysystem1;

internal sealed class RepeatRespondentBatch
{
	public Dictionary<int, RepeatRespondentCounts> ByResidentId { get; } = new Dictionary<int, RepeatRespondentCounts>();

	public Dictionary<string, RepeatRespondentCounts> ByNameAll { get; } = new Dictionary<string, RepeatRespondentCounts>(StringComparer.Ordinal);

	public Dictionary<string, RepeatRespondentCounts> ByNameNullIdOnly { get; } = new Dictionary<string, RepeatRespondentCounts>(StringComparer.Ordinal);
}
