using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BlindSpire.Patches;

/// <summary>
/// After the game renders a card, re-apply the BlindSpire fog if the instance is unknown.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
internal static class NCardFogPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        Game.CardFogRenderer.Apply(__instance);
    }
}
