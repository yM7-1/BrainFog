using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlindSpire.Game;

/// <summary>
/// Enemy placeholder rectangle whose breathing pulse follows the enemy's own
/// spine animation phase (spec 0.01 4.1: 跟随敌人原模型的呼吸动画).
/// </summary>
internal sealed partial class BreathingBox : ColorRect
{
    public NCreature? Creature;

    public override void _Process(double delta)
    {
        if (Creature == null || !IsInstanceValid(Creature))
        {
            return;
        }

        var phase = 0f;
        using var track = Creature.SpineAnimation.GetCurrentTrack(0);
        if (track != null)
        {
            var duration = track.GetAnimationDuration();
            if (duration > 0.001f)
            {
                phase = track.GetTrackTime() % duration / duration;
            }
        }

        var alpha = 0.55f + 0.35f * Mathf.Sin(phase * Mathf.Tau);
        Modulate = new Color(1f, 1f, 1f, alpha);
    }
}
