using BrainFog.Core.Text;
using Godot;

namespace BrainFog.Game;

/// <summary>
/// Source-level blur for general UI text (perf + latency, 2026-09-21).
///
/// Game UI text is written through <c>MegaLabel.SetTextAutoSize</c> /
/// <c>MegaRichTextLabel.SetTextAutoSize</c>. Rewriting the incoming string in a
/// prefix (like the card-face patch) makes the label stable across repeated
/// renders and removes the old 0.2s scene-sweep latency. The sweep is kept only
/// as a fallback for the rare plain <c>Label</c>/<c>RichTextLabel</c> controls.
///
/// Context is classified by ancestor type names via <see cref="GlobalTextBlurRules"/>:
/// cards, hover tips, events, menus, dialogue, intents, settings, the compendium
/// and mod-owned labels are left to their own patches. Combat number VFX are
/// garbled; other VFX text stays readable.
/// </summary>
internal static class GlobalTextBlurSource
{
    private const int MaxAncestorHops = 24;
    private const string PendingHookMeta = "BrainFogPendingTreeEntered";

    /// <summary>Last real input, paired with the output metadata: repeated
    /// renders with identical text skip the hash/RNG walk (perf, like the
    /// card-face patch).</summary>
    private const string InputMeta = "BrainFogGlobalBlurInput";

    /// <summary>Rewrites the incoming text with its blurred form when the
    /// label's context is covered by the catch-all rule.</summary>
    public static void Apply(CanvasItem label, ref string text)
    {
        try
        {
            if (ModRuntime.Disabled || string.IsNullOrEmpty(text))
            {
                return;
            }

            if (label.HasMeta(TextBlurService.OutputMeta)
                && label.GetMeta(TextBlurService.OutputMeta).AsString() == text)
            {
                return; // already blurred (source or fallback)
            }

            if (label.HasMeta(InputMeta)
                && label.GetMeta(InputMeta).AsString() == text
                && label.HasMeta(TextBlurService.OutputMeta))
            {
                // Same input as last time: reuse the memoized output so the
                // label stays byte-identical and the game skips re-shaping.
                var memo = label.GetMeta(TextBlurService.OutputMeta).AsString();
                if (!string.Equals(memo, text, StringComparison.Ordinal))
                {
                    text = memo;
                }
                return;
            }

            var percent = ResolvePercent(label);
            if (percent == null)
            {
                return;
            }

            if (!label.IsInsideTree())
            {
                // Context is unknown until the node is parented; blur once it
                // enters the tree (rare: labels configured before AddChild).
                SchedulePending(label);
                return;
            }

            var blurred = EventTextBlurrer.Blur(text, percent.Value, BlurSalt.Current);
            label.SetMeta(InputMeta, text);
            TextBlurService.MarkOutput(label, blurred);
            if (!string.Equals(blurred, text, StringComparison.Ordinal))
            {
                text = blurred;
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("GlobalTextBlur.Source", () => throw ex);
        }
    }

    /// <summary>Percent for this label, or null when another patch owns it
    /// (or it must stay readable).</summary>
    public static int? ResolvePercent(CanvasItem label)
    {
        var modOwned = IsModOwned(label.Name);
        var types = new List<string>(8);
        var parent = label.GetParent();
        for (var i = 0; parent != null && i < MaxAncestorHops; i++, parent = parent.GetParent())
        {
            if (!modOwned && IsModOwned(parent.Name))
            {
                modOwned = true;
            }
            types.Add(parent.GetType().Name);
        }

        return GlobalTextBlurRules.ResolvePercent(types, modOwned, IsTopBarValueLabel(label));
    }

    private static bool IsModOwned(StringName name) =>
        name.ToString().StartsWith("BrainFog", StringComparison.Ordinal);

    /// <summary>Top-bar HP/gold numbers stay readable (user change 2026-09-19);
    /// only their description texts are garbled.</summary>
    private static bool IsTopBarValueLabel(CanvasItem label)
    {
        if (label.Name.ToString() is not ("HpLabel" or "GoldLabel"))
        {
            return false;
        }
        var parent = label.GetParent();
        for (var i = 0; i < 4 && parent != null; i++, parent = parent.GetParent())
        {
            var typeName = parent.GetType().Name;
            if (typeName is "NTopBarHp" or "NTopBarGold")
            {
                return true;
            }
        }
        return false;
    }

    private static void SchedulePending(CanvasItem label)
    {
        if (label.HasMeta(PendingHookMeta))
        {
            return;
        }
        label.SetMeta(PendingHookMeta, true);
        label.Connect(
            Node.SignalName.TreeEntered,
            Callable.From(() => OnTreeEntered(label)),
            (uint)GodotObject.ConnectFlags.OneShot);
    }

    private static void OnTreeEntered(CanvasItem label)
    {
        try
        {
            if (!GodotObject.IsInstanceValid(label))
            {
                return;
            }
            label.RemoveMeta(PendingHookMeta);
            var percent = ResolvePercent(label);
            if (percent != null)
            {
                TextBlurService.BlurNode(label, percent.Value);
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("GlobalTextBlur.Pending", () => throw ex);
        }
    }
}
