using System;

namespace baranggaysystem1;

public sealed class ResidentRouteChangedEventArgs : EventArgs
{
    public ResidentRouteChangedEventArgs(int? residentId, string profileSegment)
    {
        ResidentId = residentId;
        ProfileSegment = string.IsNullOrWhiteSpace(profileSegment) ? "overview" : profileSegment.Trim().ToLowerInvariant();
    }

    public int? ResidentId { get; }

    public string ProfileSegment { get; }
}
