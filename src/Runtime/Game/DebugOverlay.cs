using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace BrainFog.Game;

/// <summary>
/// BRAINFOG_DEBUG=1 only: a small F9-toggled overlay with live mod state,
/// used to verify rules in-game without reading logs.
/// </summary>
internal sealed partial class DebugOverlay : CanvasLayer
{
    private Label _label = null!;
    private double _timer = 1.0;

    public override void _Ready()
    {
        Layer = 200;
        _label = new Label
        {
            Position = new Vector2(16f, 96f),
            Modulate = new Color(1f, 0.9f, 0.25f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        AddChild(_label);
        UpdateText();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.F9 })
        {
            Visible = !Visible;
        }
    }

    public override void _Process(double delta)
    {
        _timer += delta;
        if (_timer < 0.5)
        {
            return;
        }
        _timer = 0;
        UpdateText();
    }

    private void UpdateText()
    {
        var snapshot = SnapshotDisplay.Snapshot;
        _label.Text =
            $"[BrainFog] F9 toggle\n" +
            $"disabled={ModRuntime.Disabled} revealed={ModRuntime.Tracker.RevealedCount}\n" +
            $"hp={snapshot.Hp?.ToString() ?? "-"}/{snapshot.MaxHp?.ToString() ?? "-"} gold={snapshot.Gold?.ToString() ?? "-"}";
    }
}
