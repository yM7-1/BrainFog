using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;

namespace BrainFog.Patches;

/// <summary>
/// Relics are invisible by default (spec 0.01 3.1). The "show relics" option
/// (user change 2026-09-21; formerly "show owned relics") restores every relic
/// display: owned inventory/inspect, rewards, shops, chests and run history.
/// The compendium is always visible (0.03 j).
/// </summary>
internal static class RelicMasking
{
    public const string RewardIconMeta = "BrainFogRelicIcon";
    public const string RewardLabelMeta = "BrainFogRelicLabel";

    // Mod off (user option 0.3.7 / multiplayer): no masking at all, so the
    // regular "show" paths double as the restore path.
    private static bool ShowAll => ModRuntime.Disabled || Game.DifficultyRuntime.Current.ShowRelics;

    public static void Apply(NRelic relic)
    {
        if (!GodotObject.IsInstanceValid(relic) || IsCompendiumEntry(relic))
        {
            return;
        }

        var show = ShowAll;
        SetVisible(relic.Icon, show);
        SetVisible(relic.Outline, show);
    }

    /// <summary>Relic reward icons carry their own TextureRect (no NRelic).</summary>
    public static void ApplyRewardIcon(TextureRect icon)
    {
        if (!GodotObject.IsInstanceValid(icon))
        {
            return;
        }

        icon.SetMeta(RewardIconMeta, true);
        icon.Visible = ShowAll;
    }

    /// <summary>Reward rows print the relic title; while masked it becomes
    /// "unknown relic", while shown the original title comes back.</summary>
    public static void ApplyRewardLabel(MegaRichTextLabel label)
    {
        if (!GodotObject.IsInstanceValid(label))
        {
            return;
        }

        if (ShowAll)
        {
            if (label.HasMeta(RewardLabelMeta))
            {
                label.Text = label.GetMeta(RewardLabelMeta).AsString();
                label.RemoveMeta(RewardLabelMeta);
            }
            return;
        }

        var unknown = Game.ModLocalization.UnknownRelic;
        if (label.Text != unknown)
        {
            label.SetMeta(RewardLabelMeta, label.Text);
        }
        label.Text = unknown;
    }

    /// <summary>Relic reward button: icon plus title label.</summary>
    public static void ApplyRewardButton(NRewardButton button)
    {
        if (button.Reward is RelicReward && button._label != null)
        {
            ApplyRewardLabel(button._label);
        }
    }

    /// <summary>Inspect screen: fogged by default, fully restored when shown.</summary>
    public static void ApplyInspect(NInspectRelicScreen screen)
    {
        if (!GodotObject.IsInstanceValid(screen))
        {
            return;
        }

        if (ShowAll)
        {
            if (screen._relicImage != null && GodotObject.IsInstanceValid(screen._relicImage))
            {
                screen._relicImage.SelfModulate = Colors.White;
            }
            Show(screen._nameLabel);
            Show(screen._description);
            Show(screen._flavor);
            Show(screen._rarityLabel);
            return;
        }

        if (screen._relicImage != null && GodotObject.IsInstanceValid(screen._relicImage))
        {
            screen._relicImage.SelfModulate = Colors.Black;
        }
        Hide(screen._nameLabel);
        Hide(screen._description);
        Hide(screen._flavor);
        Hide(screen._rarityLabel);
    }

    private static bool IsCompendiumEntry(NRelic relic)
    {
        for (var node = relic.GetParent(); node != null; node = node.GetParent())
        {
            if (node.GetType().Name.StartsWith("NRelicCollection", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static void SetVisible(CanvasItem? item, bool visible)
    {
        if (item != null && GodotObject.IsInstanceValid(item))
        {
            item.Visible = visible;
        }
    }

    private static void Hide(CanvasItem? node) => SetVisible(node, false);

    private static void Show(CanvasItem? node) => SetVisible(node, true);
}

[HarmonyPatch(typeof(NRelic), "Reload")]
internal static class RelicHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(NRelic __instance) => RelicMasking.Apply(__instance);
}

/// <summary>Relic rewards carry their own TextureRect (does not use NRelic).</summary>
[HarmonyPatch(typeof(RelicReward), "CreateIcon")]
internal static class RelicRewardHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(TextureRect __result) => RelicMasking.ApplyRewardIcon(__result);
}

/// <summary>Relic reward rows print the relic title as their label; replace it
/// with "unknown relic" while masked (leak fix 2026-09-19).</summary>
[HarmonyPatch(typeof(NRewardButton), "Reload")]
internal static class RelicRewardLabelPatch
{
    [HarmonyPostfix]
    private static void Postfix(NRewardButton __instance)
    {
        try
        {
            if (__instance.Reward is RelicReward && __instance._label != null)
            {
                RelicMasking.ApplyRewardLabel(__instance._label);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("RelicReward.Label", () => throw ex);
        }
    }
}

/// <summary>Relic inspect screen is fogged by default; "show relics"
/// restores the real display.</summary>
[HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
internal static class InspectRelicFogPatch
{
    [HarmonyPostfix]
    private static void Postfix(NInspectRelicScreen __instance) => RelicMasking.ApplyInspect(__instance);
}
