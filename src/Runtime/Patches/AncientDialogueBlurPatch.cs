using BrainFog.Core.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace BrainFog.Patches;

/// <summary>Ancient (先古之民) dialogue lines are garbled at 90% like the
/// Architect's (user change 2026-09-19).</summary>
[HarmonyPatch(typeof(NAncientDialogueLine), "_Ready")]
internal static class AncientDialogueBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NAncientDialogueLine __instance)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }
            var text = __instance.GetNodeOrNull<RichTextLabel>("%Text");
            Game.TextBlurService.BlurNode(text, Game.DifficultyRuntime.TextBlurPercent);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("AncientDialogue.Blur", () => throw ex);
        }
    }
}
