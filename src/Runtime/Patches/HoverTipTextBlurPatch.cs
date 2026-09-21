using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace BrainFog.Patches;

/// <summary>
/// Hover tips follow the unified blur ratio (user change 2026-09-21): titles
/// and descriptions are garbled like all other text.
/// Exempt: enemy intent tooltips (attack numbers/actions stay readable) and the
/// settings screens / card compendium.
/// Since 2026-09-21 (supplement) the act-boss icon and map legend tips are no
/// longer exempt while "show all map routes" is on: their restored descriptions
/// follow the text module's blur percentage too.
/// </summary>
[HarmonyPatch(typeof(NHoverTipSet), "Init")]
internal static class HoverTipTextBlurPatch
{
    /// <summary>Intent hover-tip titles (eng + zhs) of the "intents" table.</summary>
    private static readonly string[] IntentTitles =
    {
        "Aggressive", "Empower", "Malicious", "Death Blow", "Strategic", "Defensive",
        "Cowardly", "Heal", "Sleeping", "Stunned", "Summon",
        "攻势", "强化", "恶意", "濒死一击", "策略", "守势", "懦弱", "回复", "沉睡", "击晕", "召唤",
    };

    /// <summary>True for hover tips built from the intent tables (enemy intent numbers).</summary>
    private static bool IsIntentHoverTip(NHoverTipSet set)
    {
        var owner = set._owner;
        if (owner == null || !GodotObject.IsInstanceValid(owner))
        {
            return false;
        }
        return owner.GetType().Name == "NIntent";
    }

    private static bool LooksLikeIntentTip(NHoverTipSet set)
    {
        var container = set._textHoverTipContainer;
        if (container == null)
        {
            return false;
        }
        foreach (var child in container.GetChildren())
        {
            if (child is not Control tip)
            {
                continue;
            }
            var title = tip.GetNodeOrNull<Label>("%Title");
            var name = title?.Text;
            if (!string.IsNullOrEmpty(name) && IntentTitles.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    [HarmonyPostfix]
    private static void Postfix(NHoverTipSet __instance)
    {
        try
        {
            if (ModRuntime.Disabled || __instance._textHoverTipContainer == null
                || !GodotObject.IsInstanceValid(__instance._textHoverTipContainer))
            {
                return;
            }

            // Intent numbers stay readable (user rule 2026-09-20): "visible
            // enemy intents" must not show garbled attack values.
            if (IsIntentHoverTip(__instance) || LooksLikeIntentTip(__instance))
            {
                return;
            }

            if (IsExempt(__instance))
            {
                return;
            }

            var percent = Game.DifficultyRuntime.TextBlurPercent;
            foreach (var child in __instance._textHoverTipContainer.GetChildren())
            {
                if (child is not Control tip)
                {
                    continue;
                }
                Game.TextBlurService.BlurNode(tip.GetNodeOrNull<Label>("%Title"), percent);
                Game.TextBlurService.BlurNode(tip.GetNodeOrNull<RichTextLabel>("%Description"), percent);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("HoverTips.Blur", () => throw ex);
        }
    }

    private static bool IsExempt(NHoverTipSet set)
    {
        for (Node? node = set._owner; node != null; node = node.GetParent())
        {
            var typeName = node.GetType().Name;
            if (typeName is "NSettingsScreen" or "NCardLibrary")
            {
                return true;
            }
        }
        return false;
    }
}
