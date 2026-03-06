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

            string json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<SidebarBehaviorSettings>(json);
            return Normalize(settings);
        }
        catch
        {
            return SidebarBehaviorSettings.CreateDefault();
        }
    }

    public static void Save(SidebarBehaviorSettings settings)
    {
        var normalized = Normalize(settings);
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(normalized, options);
        File.WriteAllText(FilePath, json);
    }

    private static SidebarBehaviorSettings Normalize(SidebarBehaviorSettings? settings)
    {
        var value = settings ?? SidebarBehaviorSettings.CreateDefault();
        value.MinExpandedWidth = Math.Clamp(value.MinExpandedWidth, 120, 420);
        value.AutoHideDelayMs = Math.Clamp(value.AutoHideDelayMs, 300, 5000);
        value.LeftEdgePixels = Math.Clamp(value.LeftEdgePixels, 2, 50);
        value.AnimationStep = Math.Clamp(value.AnimationStep, 8, 80);
        return value;
    }
}

