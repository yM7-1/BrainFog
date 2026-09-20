using BrainFog.Core.Options;
using BrainFog.Core.Text;
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

    /// <summary>Unified blur ratio for every garbled text (0–100).</summary>
    public static int TextBlurPercent => DifficultySettings.ClampBlurPercent(Current.TextBlurPercent);

    /// <summary>Per-launch salt so random selection reveals differ between runs.</summary>
    public static int RevealSalt { get; } = Random.Shared.Next();

    public static bool PanelCollapsed { get; private set; }

    /// <summary>Panel docked to a screen edge (a small edge tab brings it back).</summary>
    public static bool PanelDocked { get; private set; }

    /// <summary>0 = left edge, 1 = right edge.</summary>
    public static int PanelDockSide { get; private set; }

    /// <summary>Docked tab position, normalized to the viewport (0..1).</summary>
    public static float PanelDockY { get; private set; } = 0.5f;

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

            // Pre-0.2.4 configs had per-context options (same-name/reveal-all/
            // intent toggle): start from the documented defaults instead of
            // carrying those obsolete values over (2026-09-21).
            if (!config.HasSectionKey(Section, "memory_mode"))
            {
                Current.ApplyDefaults();
                BlurSalt.PerLaunch = true;
            }
            else
            {
                Current.SelectionReveal = DifficultySettings.ParseSelectionReveal(
                    config.GetValue(Section, "selection_reveal", "none").AsString());
                Current.RevealShopAndEventCards = config.GetValue(Section, "reveal_shop_event", false).AsBool();
                Current.TextBlurPercent = DifficultySettings.ClampBlurPercent(
                    config.GetValue(Section, "text_blur_percent", TextBlurPercents.Default).AsInt32());
                Current.SaltMode = BlurSaltModeRules.Parse(
                    config.GetValue(Section, "blur_salt_mode", "per-launch").AsString());
                Current.MemoryMode = CardMemoryModeRules.Parse(
                    config.GetValue(Section, "memory_mode", "bad").AsString());
                Current.BadMemoryThreshold = DifficultySettings.ClampBadMemoryThreshold(
                    config.GetValue(Section, "bad_memory_n", 1).AsInt32());
                Current.SnapshotStatus = config.GetValue(Section, "snapshot_status", false).AsBool();
                Current.ShowOwnedRelics = config.GetValue(Section, "show_owned_relics", false).AsBool();
                Current.ShowAllMapRoutes = config.GetValue(Section, "show_map_routes", false).AsBool();
                Current.IntentMode = IntentVisibilityRules.Parse(
                    config.GetValue(Section, "intent_visibility", "hidden").AsString());
                BlurSalt.PerLaunch = Current.SaltMode == BlurSaltMode.PerLaunch;
            }

            PanelCollapsed = config.GetValue(UiSection, "collapsed", false).AsBool();
            PanelDocked = config.GetValue(UiSection, "docked", false).AsBool();
            PanelDockSide = config.GetValue(UiSection, "dock_side", 0).AsInt32() == 1 ? 1 : 0;
            PanelDockY = Math.Clamp((float)config.GetValue(UiSection, "dock_y", 0.5).AsDouble(), 0f, 1f);
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
            config.SetValue(Section, "text_blur_percent", Current.TextBlurPercent);
            config.SetValue(Section, "blur_salt_mode", BlurSaltModeRules.ToStorage(Current.SaltMode));
            config.SetValue(Section, "memory_mode", CardMemoryModeRules.ToStorage(Current.MemoryMode));
            config.SetValue(Section, "bad_memory_n", Current.BadMemoryThreshold);
            config.SetValue(Section, "snapshot_status", Current.SnapshotStatus);
            config.SetValue(Section, "show_owned_relics", Current.ShowOwnedRelics);
            config.SetValue(Section, "show_map_routes", Current.ShowAllMapRoutes);
            config.SetValue(Section, "intent_visibility", IntentVisibilityRules.ToStorage(Current.IntentMode));
            config.SetValue(UiSection, "collapsed", PanelCollapsed);
            config.SetValue(UiSection, "docked", PanelDocked);
            config.SetValue(UiSection, "dock_side", PanelDockSide);
            config.SetValue(UiSection, "dock_y", PanelDockY);
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

    /// <summary>Docks the panel to a screen edge (the edge tab restores it).</summary>
    public static void SetPanelDocked(bool docked, int side, float normalizedY)
    {
        PanelDocked = docked;
        PanelDockSide = side == 1 ? 1 : 0;
        PanelDockY = Math.Clamp(normalizedY, 0f, 1f);
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
