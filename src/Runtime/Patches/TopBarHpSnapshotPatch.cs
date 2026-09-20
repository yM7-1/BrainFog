using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BrainFog.Patches;

/// <summary>
/// Snapshot mode (panel option, default off): top-bar HP suppresses live
/// changes and shows the last rest value. Default (off) shows live HP.
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
    private static bool UpdateHealthPrefix(NTopBarHp __instance) =>
        Game.PatchGuard.RunOr("TopBarHp.UpdateHealth", () => UpdateHealthPrefixCore(__instance), true);

    private static bool UpdateHealthPrefixCore(NTopBarHp __instance)
    {
        if (ModRuntime.Disabled || __instance._player is not { } player)
        {
            return true;
        }

        var creature = player.Creature;
        Game.LowHpHintDisplay.Evaluate(player, creature.CurrentHp, creature.MaxHp);

        // Keep the snapshot data fresh on rest in both modes; only suppress the
        // label update while snapshot mode is on.
        if (Game.SnapshotDisplay.ConsumeHpRefresh())
        {
            Game.SnapshotDisplay.OnHpChanged(creature.CurrentHp, creature.MaxHp);
            return true;
        }
        return !Game.DifficultyRuntime.Current.SnapshotStatus;
    }
}
