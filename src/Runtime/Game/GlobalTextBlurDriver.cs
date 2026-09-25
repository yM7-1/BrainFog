using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace BrainFog.Game;

/// <summary>
/// Fallback scene sweep for text controls that do not go through the Mega label
/// setters (plain <see cref="Label"/>/<see cref="RichTextLabel"/>; rare).
/// General UI text is blurred at the source by
/// <see cref="Patches.GlobalMegaLabelTextBlurPatch"/> (2026-09-21), so this
/// sweep skips every Mega widget subtree and only visits containers/plain
/// labels. Contexts and percentages come from <see cref="GlobalTextBlurSource"/>,
/// the same classifier the source patch uses.
///
/// VFX/particle subtrees are pruned: their text stays readable (only the
/// damage/heal/blocked number VFX are garbled, and those are Mega labels
/// handled at the source).
/// </summary>
internal sealed partial class GlobalTextBlurDriver : Node
{
    private const double BaseIntervalSeconds = 0.2;
    private const double MaxIntervalSeconds = 0.4;
    private const int MaxDepth = 64;

    private static readonly string[] SkipTypeMarkers =
    {
        "Vfx", "Particles", "Trail", "Spark", "Glow", "Smoke", "Flipbook",
    };

    // Perf (0.3.8): the driver sweeps the whole tree every 0.2–0.4s, so the
    // name-prefix check and the type-marker scan are cached per name/type
    // instead of allocating a string per node per sweep.
    private static readonly Dictionary<Type, bool> SkipTypeCache = new();
    private static readonly Dictionary<StringName, bool> ModNameCache = new();

    private static bool IsSkippedType(Type type)
    {
        if (SkipTypeCache.TryGetValue(type, out var skip))
        {
            return skip;
        }
        var typeName = type.Name;
        skip = false;
        foreach (var marker in SkipTypeMarkers)
        {
            if (typeName.Contains(marker, StringComparison.Ordinal))
            {
                skip = true;
                break;
            }
        }
        SkipTypeCache[type] = skip;
        return skip;
    }

    private static bool IsModOwnedName(StringName name)
    {
        if (ModNameCache.TryGetValue(name, out var owned))
        {
            return owned;
        }
        owned = name.ToString().StartsWith("BrainFog", StringComparison.Ordinal);
        ModNameCache[name] = owned;
        return owned;
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
            var changed = Sweep(root, 0);
            _interval = changed
                ? BaseIntervalSeconds
                : Math.Min(_interval * 1.5, MaxIntervalSeconds);
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Sweep", () => throw ex);
        }
    }

    /// <summary>Walks the tree, blurring plain-label text that changed. Returns
    /// true when at least one node was actually re-blurred this pass.</summary>
    private static bool Sweep(Node node, int depth)
    {
        if (depth > MaxDepth)
        {
            return false;
        }

        // Mega widgets are covered at the source (GlobalTextBlurPatch).
        if (node is MegaLabel or MegaRichTextLabel)
        {
            return false;
        }

        if (IsModOwnedName(node.Name))
        {
            return false;
        }

        if (IsSkippedType(node.GetType()))
        {
            return false;
        }

        if (node is CanvasItem canvas && !canvas.IsVisibleInTree())
        {
            return false; // handled once it becomes visible
        }

        if (node is Label or RichTextLabel)
        {
            if (!GlobalTextBlurSource.ShouldBlur((CanvasItem)node))
            {
                return false; // owned by another patch or deliberately readable
            }
            return TextBlurService.BlurNode((CanvasItem)node, DifficultyRuntime.TextBlurPercent);
        }

        var changed = false;
        var childCount = node.GetChildCount();
        for (var i = 0; i < childCount; i++)
        {
            changed |= Sweep(node.GetChild(i), depth + 1);
        }
        return changed;
    }
}
