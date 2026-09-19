using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BrainFog.Patches;

/// <summary>
/// The game re-applies card visuals (cost/star/enchantment visibility) after Reload;
/// re-fog unknown instances so no face information leaks (spec 0.02 #1/#2).
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateVisuals")]
internal static class NCardFogUpdatePatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        Game.CardFogRenderer.Apply(__instance);
    }
}
