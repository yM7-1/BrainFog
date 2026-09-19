using BrainFog.Core.Text;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace BrainFog.Game;

/// <summary>
/// Periodic scene sweep that garbles remaining UI text (user rule 2026-09-19:
/// all unmentioned prompts/descriptions default to 60%). Contexts with their own
/// rule are skipped: card faces (blurred at the source by CardFaceTextBlurPatch),
/// hover tips (own policy), settings and the compendium (stay readable), and
/// mod-owned labels.
///
/// Perf (2026-09-20): the sweep interval backs off while nothing changes, and
/// VFX/particle subtrees are skipped entirely, so card-play effects spawning
/// and despawning no longer cost a full-tree walk.
/// </summary>
internal sealed partial class GlobalTextBlurDriver : Node
{
    private const double BaseIntervalSeconds = 0.2;
    private const double MaxIntervalSeconds = 0.4;
    private const int MaxDepth = 64;

    /// <summary>Subtrees that never contain game UI text: VFX, particles, spine
    /// trails. Damage numbers / "Blocked" / heal numbers live here and must stay
    /// readable (spec 1.3: hit VFX preserved), and skipping them keeps the sweep
    /// cheap while card-play effects spawn and despawn (perf 2026-09-20).</summary>
    private static readonly string[] SkipTypeMarkers =
    {
        "Vfx", "Particles", "Trail", "Spark", "Glow", "Smoke", "Flipbook",
    };

    private enum Context
    {
        Default,
        UiDescription,
        Skip,
    }

    private double _timer = BaseIntervalSeconds;
    private double _interval = BaseIntervalSeconds;

    public override void _Process(double delta)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }
            _timer += delta;
            if (_timer < _interval)
            {
                return;
            }
            _timer = 0;

            var root = GetTree()?.Root;
            if (root == null)
            {
                return;
            }

            // Adaptive backoff: when a sweep blurred nothing, the scene was
            // already covered and the next sweep can wait longer; any actual
            // text change snaps back to the fast interval (perf 2026-09-20).
            var changed = Sweep(root, Context.Default, 0);
            _interval = changed
                ? BaseIntervalSeconds
                : Math.Min(_interval * 1.5, MaxIntervalSeconds);
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Sweep", () => throw ex);
        }
    }

    /// <summary>Walks the tree, blurring text that changed. Returns true when at
    /// least one node was actually re-blurred this pass.</summary>
    private static bool Sweep(Node node, Context context, int depth)
    {
        if (depth > MaxDepth)
        {
            return false;
        }

        var next = RefineContext(node, context);
        if (next == Context.Skip)
        {
            return false;
        }

        if (node is CanvasItem canvas && !canvas.IsVisibleInTree())
        {
            return false; // handled once it becomes visible
        }

        if (node is Label or RichTextLabel)
        {
            // Top-bar HP/gold values stay readable (user change 2026-09-19):
            // only their description texts are garbled.
            if (IsTopBarValueLabel(node))
            {
                return false;
            }
            var percent = next == Context.UiDescription
                ? TextBlurPercents.UiDescription
                : TextBlurPercents.Default;
            return TextBlurService.BlurNode(node as CanvasItem, percent);
        }

        var changed = false;
        foreach (var child in node.GetChildren())
        {
            changed |= Sweep(child, next, depth + 1);
        }
        return changed;
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

        // VFX/particle subtrees never contain UI text: skipping them keeps the
        // sweep cheap while card-play effects spawn and despawn (perf 2026-09-20).
        foreach (var marker in SkipTypeMarkers)
        {
            if (typeName.Contains(marker, StringComparison.Ordinal))
            {
                return Context.Skip;
            }
        }

        // Enemy intents stay readable (user rule 2026-09-20): the "visible
        // enemy intents" modifier must not show garbled attack numbers.
        if (typeName == "NIntent")
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
