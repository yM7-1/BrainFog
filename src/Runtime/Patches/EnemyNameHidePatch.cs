using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BrainFog.Patches;

/// <summary>Enemy nameplates never show their name (user change 2026-09-19).
/// The HP bar and power glyphs stay readable on hover. Mod off restores the
/// vanilla nameplates (0.3.7).</summary>
[HarmonyPatch(typeof(NCreatureStateDisplay), "SetCreature")]
internal static class EnemyNameHidePatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreatureStateDisplay __instance, Creature creature) =>
        EnemyNameMask.Apply(__instance, creature);
}

internal static class EnemyNameMask
{
    /// <summary>Marks a nameplate this mod hid (mod-off sweep restores it).</summary>
    public const string HiddenMeta = "BrainFogNameHidden";

    public static void Apply(NCreatureStateDisplay display, Creature? creature)
    {
        try
        {
            if (!GodotObject.IsInstanceValid(display))
            {
                return;
            }
            var label = display._nameplateLabel;
            if (label == null || !GodotObject.IsInstanceValid(label))
            {
                return;
            }

            if (!ModRuntime.Disabled && creature is { IsEnemy: true })
            {
                label.SetMeta(HiddenMeta, true);
                label.Visible = false;
                return;
            }

            // Mod-off sweep (creature may be unknown then): restore what we hid.
            if (ModRuntime.Disabled && label.HasMeta(HiddenMeta))
            {
                label.Visible = true;
                label.RemoveMeta(HiddenMeta);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("EnemyName.Hide", () => throw ex);
        }
    }
}
