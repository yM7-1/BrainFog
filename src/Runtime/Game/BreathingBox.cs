using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlindSpire.Game;

/// <summary>
/// Enemy placeholder rectangle whose breathing pulse follows the enemy's own
/// spine animation phase (spec 0.01 4.1: 跟随敌人原模型的呼吸动画).
/// </summary>
internal sealed partial class BreathingBox : ColorRect
{
    private const double ProbeIntervalSeconds = 0.5;

    public NCreature? Creature;

    private float _elapsed;
    private float _duration;
    private double _probeTimer;

    public override void _Process(double delta)
    {
        try
        {
            ProcessCore(delta);
        }
        catch (Exception ex)
        {
            PatchGuard.Run("BreathingBox.Process", () => throw ex);
        }
    }

    private void ProcessCore(double delta)
    {
        if (Creature == null || !IsInstanceValid(Creature))
        {
            SetProcess(false);
            return;
        }
        if (Creature.Entity is { IsDead: true })
        {
            Visible = false;
            SetProcess(false);
            return;
        }

        _elapsed += (float)delta;
        _probeTimer += delta;
        if (_probeTimer >= ProbeIntervalSeconds)
        {
            _probeTimer = 0;
            using var track = Creature.SpineAnimation.GetCurrentTrack(0);
            if (track != null)
            {
                var duration = track.GetAnimationDuration();
                if (duration > 0.001f)
                {
                    _duration = duration;
                }
            }
        }

        var phase = _duration > 0.001f ? _elapsed % _duration / _duration : 0f;
        var mid = (BlindSpireTuning.EnemyBoxAlphaMin + BlindSpireTuning.EnemyBoxAlphaMax) * 0.5f;
        var amp = (BlindSpireTuning.EnemyBoxAlphaMax - BlindSpireTuning.EnemyBoxAlphaMin) * 0.5f;
        var alpha = mid + amp * Mathf.Sin(phase * Mathf.Tau);
        Modulate = new Color(1f, 1f, 1f, alpha);
    }
}
