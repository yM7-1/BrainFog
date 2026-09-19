using BrainFog.Core.Options;
using Godot;

namespace BrainFog.Game;

/// <summary>
/// Live difficulty options + persistence in the mod's own config file
/// (user://BrainFog/settings.cfg). No other mod's files are touched.
/// </summary>
internal static class DifficultyRuntime
{
    private const string SettingsPath = "user://BrainFog/settings.cfg";
    private const string SettingsDir = "user://BrainFog";
    private const string Section = "difficulty";
    private const string UiSection = "ui";

    public static DifficultySettings Current { get; } = new();

    /// <summary>Per-launch salt so random selection reveals differ between runs.</summary>
    public static int RevealSalt { get; } = Random.Shared.Next();

    public static bool PanelCollapsed { get; private set; }

    /// <summary>Dragged panel position, normalized to the viewport (0..1).</summary>
    public static bool HasPanelPosition { get; private set; }
    public static float PanelPositionX { get; private set; }
    public static float PanelPositionY { get; private set; }

    public static void Load()
    {
        try
        {
            var config = new ConfigFile();
            if (config.Load(SettingsPath) != Error.Ok)
            {
                return;
            }
            Current.SelectionReveal = DifficultySettings.ParseSelectionReveal(
                config.GetValue(Section, "selection_reveal", "none").AsString());
            Current.RevealShopAndEventCards = config.GetValue(Section, "reveal_shop_event", false).AsBool();
            Current.RevealSameNameCards = config.GetValue(Section, "reveal_same_name", true).AsBool();
            Current.ShowLiveStatus = config.GetValue(Section, "show_live_status", false).AsBool();
            Current.ShowOwnedRelics = config.GetValue(Section, "show_owned_relics", false).AsBool();
            Current.ShowAllMapRoutes = config.GetValue(Section, "show_map_routes", false).AsBool();
            Current.ShowEnemyIntents = config.GetValue(Section, "show_intents", false).AsBool();
            PanelCollapsed = config.GetValue(UiSection, "collapsed", false).AsBool();
            var posX = config.GetValue(UiSection, "panel_x", -1.0).AsDouble();
            var posY = config.GetValue(UiSection, "panel_y", -1.0).AsDouble();
            if (posX >= 0.0 && posX <= 1.0 && posY >= 0.0 && posY <= 1.0)
            {
                HasPanelPosition = true;
                PanelPositionX = (float)posX;
                PanelPositionY = (float)posY;
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("Difficulty.Load", () => throw ex);
        }
    }

    public static void Save() =>
        PatchGuard.Run("Difficulty.Save", () =>
        {
            Godot.DirAccess.MakeDirRecursiveAbsolute(SettingsDir);
            var config = new ConfigFile();
            config.SetValue(Section, "selection_reveal", DifficultySettings.ToStorage(Current.SelectionReveal));
            config.SetValue(Section, "reveal_shop_event", Current.RevealShopAndEventCards);
            config.SetValue(Section, "reveal_same_name", Current.RevealSameNameCards);
            config.SetValue(Section, "show_live_status", Current.ShowLiveStatus);
            config.SetValue(Section, "show_owned_relics", Current.ShowOwnedRelics);
            config.SetValue(Section, "show_map_routes", Current.ShowAllMapRoutes);
            config.SetValue(Section, "show_intents", Current.ShowEnemyIntents);
            config.SetValue(UiSection, "collapsed", PanelCollapsed);
            if (HasPanelPosition)
            {
                config.SetValue(UiSection, "panel_x", PanelPositionX);
                config.SetValue(UiSection, "panel_y", PanelPositionY);
            }
            config.Save(SettingsPath);
        });

    public static void SetPanelCollapsed(bool collapsed)
    {
        PanelCollapsed = collapsed;
        Save();
    }

    /// <summary>Persists the dragged panel position (normalized 0..1).</summary>
    public static void SetPanelPosition(float x, float y)
    {
        HasPanelPosition = true;
        PanelPositionX = Math.Clamp(x, 0f, 1f);
        PanelPositionY = Math.Clamp(y, 0f, 1f);
        Save();
    }

    /// <summary>Applies a changed option: refresh the affected live UI and persist.</summary>
    public static void NotifyChanged()
    {
        CardFogRenderer.RefreshAllLiveCards();
        DifficultyRefresh.ApplyAll();
        Save();
    }
}
