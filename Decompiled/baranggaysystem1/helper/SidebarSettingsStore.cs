using System;
using System.IO;
using System.Text.Json;

namespace baranggaysystem1.helper;

internal static class SidebarSettingsStore
{
	private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "sidebar.settings.json");

	public static SidebarBehaviorSettings Load()
	{
		try
		{
			if (!File.Exists(FilePath))
			{
				return SidebarBehaviorSettings.CreateDefault();
			}
			return Normalize(JsonSerializer.Deserialize<SidebarBehaviorSettings>(File.ReadAllText(FilePath)));
		}
		catch
		{
			return SidebarBehaviorSettings.CreateDefault();
		}
	}

	public static void Save(SidebarBehaviorSettings settings)
	{
		SidebarBehaviorSettings value = Normalize(settings);
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		string contents = JsonSerializer.Serialize(value, options);
		File.WriteAllText(FilePath, contents);
	}

	private static SidebarBehaviorSettings Normalize(SidebarBehaviorSettings? settings)
	{
		SidebarBehaviorSettings? obj = settings ?? SidebarBehaviorSettings.CreateDefault();
		obj.MinExpandedWidth = Math.Clamp(obj.MinExpandedWidth, 120, 420);
		obj.AutoHideDelayMs = Math.Clamp(obj.AutoHideDelayMs, 300, 5000);
		obj.LeftEdgePixels = Math.Clamp(obj.LeftEdgePixels, 2, 50);
		obj.AnimationStep = Math.Clamp(obj.AnimationStep, 8, 80);
		return obj;
	}
}
