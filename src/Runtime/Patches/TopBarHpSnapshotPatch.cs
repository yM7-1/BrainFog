using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BlindSpire.Patches;

/// <summary>
/// Top-bar HP shows the stale snapshot: live changes are suppressed unless a
/// refresh trigger (rest) requested one (spec 0.02 #3, 0.03 c).
/// Low-HP warning is driven by TRUE hp on every change (spec 0.03 d).
/// </summary>
[HarmonyPatch(typeof(NTopBarHp))]
internal static class TopBarHpSnapshotPatch
{
    [HarmonyPatch("Initialize")]
    [HarmonyPrefix]
    private static void InitializePrefix(NTopBarHp __instance, Player player)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }
        Game.SnapshotDisplay.AttachHpBar(__instance);
        Game.SnapshotDisplay.InitHp(player.Creature.CurrentHp, player.Creature.MaxHp);
    }

    [HarmonyPatch("UpdateHealth")]
    [HarmonyPrefix]
    private static bool UpdateHealthPrefix(NTopBarHp __instance)
    {
        if (ModRuntime.Disabled || __instance._player is not { } player)
        {
            return true;
        }

        var creature = player.Creature;
        Game.LowHpHintDisplay.Evaluate(player, creature.CurrentHp, creature.MaxHp);

        if (Game.SnapshotDisplay.ConsumeHpRefresh())
        {
            Game.SnapshotDisplay.OnHpChanged(creature.CurrentHp, creature.MaxHp);
            return true;
        }
        return false;
    }
}
