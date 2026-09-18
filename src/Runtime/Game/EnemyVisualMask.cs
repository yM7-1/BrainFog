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
        if (ModRuntime.Disabled || node.Entity is not { } entity)
        {
            return;
        }

        var isOurs = entity.IsPlayer || (entity.PetOwner != null && LocalContext.IsMe(entity.PetOwner));
        if (isOurs)
        {
            return;
        }

        node.Body.Visible = false;

        var box = node.GetNodeOrNull<ColorRect>(BoxName);
        if (box == null)
        {
            box = new ColorRect
            {
                Name = BoxName,
                Color = new Color(0.06f, 0.06f, 0.09f, 0.85f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            node.AddChild(box);
            box.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var tween = box.CreateTween().SetLoops();
            tween.TweenProperty(box, "modulate:a", 1.0f, 1.3f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(box, "modulate:a", 0.55f, 1.3f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
        box.Visible = true;
    }
}
