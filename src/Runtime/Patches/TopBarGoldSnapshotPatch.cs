using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BlindSpire.Patches;

/// <summary>
/// Top-bar gold shows the stale snapshot: only deductions refresh it,
/// gains are not shown (spec 0.02 #3, 0.03 c).
/// </summary>
[HarmonyPatch(typeof(NTopBarGold))]
internal static class TopBarGoldSnapshotPatch
{
    [HarmonyPatch("Initialize")]
    [HarmonyPrefix]
    private static void InitializePrefix(Player player)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }
        Game.SnapshotDisplay.InitGold(player.Gold);
    }

    [HarmonyPatch("UpdateGold")]
    [HarmonyPrefix]
    private static bool UpdateGoldPrefix(NTopBarGold __instance)
    {
        if (ModRuntime.Disabled || __instance._player is not { } player)
        {
            return true;
        }

        if (Game.SnapshotDisplay.Snapshot.ShouldRefreshGold(player.Gold))
        {
            Game.SnapshotDisplay.OnGoldChanged(player.Gold);
            return true;
        }
        return false;
    }
}
