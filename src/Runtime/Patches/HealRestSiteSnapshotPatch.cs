using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace BrainFog.Patches;

/// <summary>
/// Rest refreshes the HP and gold snapshots (spec 0.02 #3).
/// Smith's reveal is handled by the upgrade patch (spec 0.01 3.2 / 0.03 a).
/// </summary>
[HarmonyPatch(typeof(HealRestSiteOption), "OnSelect")]
internal static class HealRestSiteSnapshotPatch
{
    [HarmonyPostfix]
    private static void Postfix(HealRestSiteOption __instance)
    {
        if (ModRuntime.Disabled || __instance.Owner is not { } player)
        {
            return;
        }

        Game.SnapshotDisplay.RefreshHpNow(player.Creature.CurrentHp, player.Creature.MaxHp);
        Game.SnapshotDisplay.RefreshGoldNow(player.Gold);
    }
}
