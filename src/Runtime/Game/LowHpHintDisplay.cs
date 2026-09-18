using BlindSpire.Core.Status;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BlindSpire.Game;

/// <summary>
/// Low-HP warning: a red border around the player's creature node plus a status-bar
/// message, driven by TRUE hp (spec 0.02 #3, 0.03 d).
/// </summary>
internal static class LowHpHintDisplay
{
    private const string HintLabelName = "BlindSpireLowHpHint";
    private const string BorderName = "BlindSpireLowHpBorder";

    public static void Evaluate(Player player, int trueHp, int maxHp) =>
        PatchGuard.Run("LowHpHint.Evaluate", () => EvaluateCore(player, trueHp, maxHp));

    private static void EvaluateCore(Player player, int trueHp, int maxHp)
    {
        if (ModRuntime.Disabled)
        {
            return;
        }

        var low = LowHpHint.IsLow(trueHp, maxHp);
        UpdateStatusLabel(player, low);
        UpdateCreatureBorder(player, low);
    }

    private static void UpdateStatusLabel(Player player, bool low)
    {
        if (player.Creature == null)
        {
            return;
        }

        // The top-bar HP node is the natural status-bar anchor.
        var hpBar = SnapshotDisplay.HpBar;
        if (hpBar == null || !GodotObject.IsInstanceValid(hpBar))
        {
            return;
        }

        var hpLabel = hpBar._hpLabel;
        if (hpLabel == null || !GodotObject.IsInstanceValid(hpLabel))
        {
            return;
        }

        var label = hpBar.GetNodeOrNull<Label>(HintLabelName);
        if (label == null)
        {
            label = new Label
            {
                Name = HintLabelName,
                Text = LowHpHint.Message,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = Colors.Red,
            };
            label.ApplyLocaleFontSubstitution(FontType.Regular, "font");
            hpBar.AddChild(label);
        }
        label.Position = hpLabel.Position + new Vector2(0, 28);
        label.Visible = low;
    }

    private static void UpdateCreatureBorder(Player player, bool low)
    {
        var room = NCombatRoom.Instance;
        if (room == null || player.Creature == null)
        {
            return;
        }

        var node = room.GetCreatureNode(player.Creature);
        if (node == null)
        {
            return;
        }

        var border = node.GetNodeOrNull<Panel>(BorderName);
        if (border == null)
        {
            if (!low)
            {
                return;
            }
            border = new Panel
            {
                Name = BorderName,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            var style = new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                BorderColor = Colors.Red,
            };
            style.SetBorderWidthAll(3);
            border.AddThemeStyleboxOverride("panel", style);
            node.AddChild(border);
            border.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        }
        border.Visible = low;
    }
}
