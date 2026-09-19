using BrainFog.Core.Text;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace BrainFog.Game;

/// <summary>
/// Periodic scene sweep that garbles remaining UI text (user rule 2026-09-19:
/// all unmentioned prompts/descriptions default to 60%). Contexts with their own
/// rule are skipped: card faces (85% via CardFogRenderer), hover tips (own
/// policy), settings and the compendium (stay readable), and mod-owned labels.
/// </summary>
internal sealed partial class GlobalTextBlurDriver : Node
{
    private const double SweepIntervalSeconds = 0.2;
    private const int MaxDepth = 64;

    private enum Context
    {
        Default,
        UiDescription,
        Skip,
    }

    private double _timer = SweepIntervalSeconds;

    public override void _Process(double delta)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }
            _timer += delta;
            if (_timer < SweepIntervalSeconds)
            {
                return;
            }
            _timer = 0;

            var root = GetTree()?.Root;
            if (root != null)
            {
                Sweep(root, Context.Default, 0);
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Sweep", () => throw ex);
        }
    }

    private static void Sweep(Node node, Context context, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        var next = RefineContext(node, context);
        if (next == Context.Skip)
        {
            return;
        }

        if (node is CanvasItem canvas && !canvas.IsVisibleInTree())
        {
            return; // handled once it becomes visible
        }

        if (node is Label or RichTextLabel)
        {
            // Top-bar HP/gold values stay readable (user change 2026-09-19):
            // only their description texts are garbled.
            if (IsTopBarValueLabel(node))
            {
                return;
            }
            var percent = next == Context.UiDescription
                ? TextBlurPercents.UiDescription
                : TextBlurPercents.Default;
            TextBlurService.BlurNode(node as CanvasItem, percent);
            return;
        }

        foreach (var child in node.GetChildren())
        {
            Sweep(child, next, depth + 1);
        }
    }

    private static bool IsTopBarValueLabel(Node node)
    {
        var name = node.Name.ToString();
        if (name is not ("HpLabel" or "GoldLabel"))
        {
            return false;
        }
        var parent = node.GetParent();
        for (var i = 0; i < 4 && parent != null; i++, parent = parent.GetParent())
        {
            var parentType = parent.GetType().Name;
            if (parentType is "NTopBarHp" or "NTopBarGold")
            {
                return true;
            }
        }
        return false;
    }

    private static Context RefineContext(Node node, Context inherited)
    {
        var name = node.Name.ToString();
        if (name.StartsWith("BrainFog", StringComparison.Ordinal))
        {
            return Context.Skip;
        }

        var typeName = node.GetType().Name;
        if (typeName.Contains("Settings", StringComparison.Ordinal)
            || node is NCardLibrary
            || node is NHoverTipSet
            || node is NCard)
        {
            return Context.Skip;
        }

        if (typeName.StartsWith("NTopBar", StringComparison.Ordinal)
            || node is MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen
            || typeName.StartsWith("NMap", StringComparison.Ordinal))
        {
            return Context.UiDescription;
        }

        return inherited;
    }
}
