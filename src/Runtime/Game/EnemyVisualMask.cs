using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlindSpire.Game;

/// <summary>
/// Hides enemy models and replaces them with a breathing rectangle placeholder
/// (spec 0.01 4.1 / 0.02 #9). Own character and own summons stay fully visible.
/// Hit VFX live in the combat VFX container, so they are unaffected.
/// </summary>
internal static class EnemyVisualMask
{
    private const string BoxName = "BlindSpireEnemyBox";

    public static void Apply(NCreature node)
    {
        if (!GodotObject.IsInstanceValid(node) || node.Entity is not { } entity)
        {
            return;
        }

        var isOurs = entity.IsPlayer || (entity.PetOwner != null && LocalContext.IsMe(entity.PetOwner));
        if (ModRuntime.Disabled || isOurs)
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
            existing.Visible = true;
            return;
        }

        var box = new BreathingBox
        {
            Name = BoxName,
            Creature = node,
            Color = new Color(0.06f, 0.06f, 0.09f, 0.85f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        node.AddChild(box);
        box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
    }

    /// <summary>Restores the real model (own creatures, disabled mod, cleanup).</summary>
    public static void Restore(NCreature node)
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
        }
    }
}
