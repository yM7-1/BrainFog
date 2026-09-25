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
    private const string DefaultsSection = "defaults";
    private const string UiSection = "ui";
    private const string ModSection = "mod";

    public static DifficultySettings Current { get; } = new();

    /// <summary>What the reset button restores (custom defaults or factory).</summary>
    public static DifficultySettings Defaults { get; } = new();

    /// <summary>Unified blur ratio for every garbled text (0–100).</summary>
    public static int TextBlurPercent => DifficultySettings.ClampBlurPercent(Current.TextBlurPercent);

    /// <summary>Config file path (BRAINFOG_DEBUG diagnostics).</summary>
    public static string SettingsFilePath => SettingsPath;

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

            var factory = new DifficultySettings();

            // Defaults the "reset" button restores: the player's saved custom
            // defaults when present (user change 2026-09-21), else factory.
            CustomDefaults = config.HasSectionKey(DefaultsSection, "memory_mode");
            if (CustomDefaults)
            {
                ReadInto(Defaults, config, DefaultsSection, factory);
            }

            // Pre-0.2.4 configs had per-context options (same-name/reveal-all/
            // intent toggle): start from the documented defaults instead of
            // carrying those obsolete values over (2026-09-21).
            if (!config.HasSectionKey(Section, "memory_mode"))
            {
                Current.CopyFrom(Defaults);
            }
            else
            {
                // "show_relics" (2026-09-21) replaced "show_owned_relics"; the
                // legacy value carries over so players who had it on keep relics
                // visible (now in every context).
                if (!config.HasSectionKey(Section, "show_relics"))
                {
                    config.SetValue(
                        Section,
                        "show_relics",
                        config.GetValue(Section, "show_owned_relics", Defaults.ShowRelics).AsBool());
                }
                ReadInto(Current, config, Section, Defaults);
            }
            BlurSalt.PerLaunch = Current.SaltMode == BlurSaltMode.PerLaunch;

            // "Disable BrainFog" option (0.3.7): persisted so the mod stays off
            // across launches until the player turns it back on.
            ModRuntime.SetUserDisabled(config.GetValue(ModSection, "disabled", false).AsBool());

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

    /// <summary>Reads one settings section; missing keys fall back to
    /// <paramref name="fallback"/> (the player's defaults or factory).</summary>
    private static void ReadInto(DifficultySettings target, ConfigFile config, string section, DifficultySettings fallback)
    {
        target.SelectionReveal = DifficultySettings.ParseSelectionReveal(
            config.GetValue(section, "selection_reveal", DifficultySettings.ToStorage(fallback.SelectionReveal)).AsString());
        target.RevealShopAndEventCards = config.GetValue(section, "reveal_shop_event", fallback.RevealShopAndEventCards).AsBool();
        target.TextBlurPercent = DifficultySettings.ClampBlurPercent(
            config.GetValue(section, "text_blur_percent", fallback.TextBlurPercent).AsInt32());
        target.SaltMode = BlurSaltModeRules.Parse(
            config.GetValue(section, "blur_salt_mode", BlurSaltModeRules.ToStorage(fallback.SaltMode)).AsString());
        target.MemoryMode = CardMemoryModeRules.Parse(
            config.GetValue(section, "memory_mode", CardMemoryModeRules.ToStorage(fallback.MemoryMode)).AsString());
        target.BadMemoryThreshold = DifficultySettings.ClampBadMemoryThreshold(
            config.GetValue(section, "bad_memory_n", fallback.BadMemoryThreshold).AsInt32());
        target.MemoryFade = config.GetValue(section, "memory_fade", fallback.MemoryFade).AsBool();
        target.SnapshotStatus = config.GetValue(section, "snapshot_status", fallback.SnapshotStatus).AsBool();
        target.ReadableStatusNumbers = config.GetValue(section, "readable_status_numbers", fallback.ReadableStatusNumbers).AsBool();
        target.ShowRelics = config.GetValue(section, "show_relics", fallback.ShowRelics).AsBool();
        target.ShowAllMapRoutes = config.GetValue(section, "show_map_routes", fallback.ShowAllMapRoutes).AsBool();
        target.IntentMode = IntentVisibilityRules.Parse(
            config.GetValue(section, "intent_visibility", IntentVisibilityRules.ToStorage(fallback.IntentMode)).AsString());
        target.EnemyModelsVisible = config.GetValue(section, "enemy_models_visible", fallback.EnemyModelsVisible).AsBool();
    }

    private static void WriteInto(ConfigFile config, string section, DifficultySettings settings)
    {
        config.SetValue(section, "selection_reveal", DifficultySettings.ToStorage(settings.SelectionReveal));
        config.SetValue(section, "reveal_shop_event", settings.RevealShopAndEventCards);
        config.SetValue(section, "text_blur_percent", settings.TextBlurPercent);
        config.SetValue(section, "blur_salt_mode", BlurSaltModeRules.ToStorage(settings.SaltMode));
        config.SetValue(section, "memory_mode", CardMemoryModeRules.ToStorage(settings.MemoryMode));
        config.SetValue(section, "bad_memory_n", settings.BadMemoryThreshold);
        config.SetValue(section, "memory_fade", settings.MemoryFade);
        config.SetValue(section, "snapshot_status", settings.SnapshotStatus);
        config.SetValue(section, "readable_status_numbers", settings.ReadableStatusNumbers);
        config.SetValue(section, "show_relics", settings.ShowRelics);
        config.SetValue(section, "show_map_routes", settings.ShowAllMapRoutes);
        config.SetValue(section, "intent_visibility", IntentVisibilityRules.ToStorage(settings.IntentMode));
        config.SetValue(section, "enemy_models_visible", settings.EnemyModelsVisible);
    }

    public static void Save() =>
        PatchGuard.Run("Difficulty.Save", () =>
        {
            Godot.DirAccess.MakeDirRecursiveAbsolute(SettingsDir);
            var config = new ConfigFile();
            WriteInto(config, Section, Current);
            if (CustomDefaults)
            {
                WriteInto(config, DefaultsSection, Defaults);
            }
            config.SetValue(ModSection, "disabled", ModRuntime.UserDisabled);
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

    /// <summary>Applies the reset target: the player's saved defaults when they
    /// pressed "set current as default", else the factory defaults.</summary>
    public static void ResetToDefaults()
    {
        Current.CopyFrom(Defaults);
        BlurSalt.PerLaunch = Current.SaltMode == BlurSaltMode.PerLaunch;
        NotifyChanged();
    }

    /// <summary>"Set current as default" (user change 2026-09-21): stores the
    /// current modifier settings as the defaults that reset restores; persisted
    /// in the mod's config and kept across launches.</summary>
    public static void SaveCurrentAsDefaults()
    {
        Defaults.CopyFrom(Current);
        CustomDefaults = true;
        Save();
    }

    /// <summary>True when the player has saved custom defaults.</summary>
    public static bool CustomDefaults { get; private set; }

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

    /// <summary>Panel option "disable BrainFog" (0.3.7): stops every mod effect
    /// and reverts what is already on screen to the vanilla presentation (text,
    /// cards, relics, enemy visuals, map, intents, status UI). Re-enabling
    /// re-applies the current options. Persisted across launches.</summary>
    public static void SetModDisabled(bool disabled)
    {
        if (ModRuntime.UserDisabled == disabled)
        {
            return;
        }
        ModRuntime.SetUserDisabled(disabled);
        Save();
        if (disabled)
        {
            TextBlurService.RestoreAll();
        }
        else
        {
            TextBlurService.ReapplyAllText(TextBlurPercent);
        }
        CardFogRenderer.RefreshAllLiveCards();
        DifficultyRefresh.ApplyAll();
    }

    /// <summary>Applies a changed option: refresh the affected live UI and persist.</summary>
    public static void NotifyChanged()
    {
        CardFogRenderer.RefreshAllLiveCards();
        DifficultyRefresh.ApplyAll();
        Save();
    }
}
