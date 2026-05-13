namespace baranggaysystem1;

public sealed class SidebarBehaviorSettings
{
	public int MinExpandedWidth { get; set; } = 220;

	public int AutoHideDelayMs { get; set; } = 1000;

	public int LeftEdgePixels { get; set; } = 10;

	public int AnimationStep { get; set; } = 30;

	public static SidebarBehaviorSettings CreateDefault()
	{
		return new SidebarBehaviorSettings();
	}
}
