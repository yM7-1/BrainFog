using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BrainFog.Game;

/// <summary>
/// Hides enemy models and replaces them with a breathing rectangle placeholder
/// (spec 0.01 4.1 / 0.02 #9). Own character and own summons stay fully visible.
/// Hit VFX live in the combat VFX container, so they are unaffected.
/// </summary>
internal static class EnemyVisualMask
{
    private const string BoxName = "BrainFogEnemyBox";

    public static void Apply(NCreature node) =>
        PatchGuard.Run("EnemyMask.Apply", () => ApplyCore(node));

    private static void ApplyCore(NCreature node)
    {
        if (!GodotObject.IsInstanceValid(node) || node.Entity is not { } entity)
        {
            return;
        }

        var isOwnPet = entity.PetOwner != null && LocalContext.IsMe(entity.PetOwner);
        if (!Core.Reveal.MaskEligibility.ShouldMask(ModRuntime.Disabled, entity.IsPlayer, isOwnPet))
        {
            Restore(node);
            return;
        }

        if (GodotObject.IsInstanceValid(node.Body))
        {
            node.Body.Visible = false;
        }

        if (node.GetNodeOrNull<BreathingBox>(BoxName) is { } existing)
        {
            existing.Creature = node;
            existing.SetProcess(true);
            existing.Visible = true;
            return;
        }

        var box = new BreathingBox
        {
            Name = BoxName,
            Creature = node,
            Color = BrainFogTuning.EnemyBoxColor,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        node.AddChild(box);
        box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Restores the real model (own creatures, disabled mod, cleanup).</summary>
    public static void Restore(NCreature node) =>
        PatchGuard.Run("EnemyMask.Restore", () => RestoreCore(node));

    private static void RestoreCore(NCreature node)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        if (node.Body != null && GodotObject.IsInstanceValid(node.Body))
        {
            node.Body.Visible = true;
        }

        var box = node.GetNodeOrNull<BreathingBox>(BoxName);
        if (box != null && GodotObject.IsInstanceValid(box))
        {
            box.Visible = false;
            box.SetProcess(false);
        }
    }
}
