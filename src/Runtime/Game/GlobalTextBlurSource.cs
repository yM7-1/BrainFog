using BrainFog.Core.Text;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

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
/// The ratio comes from the cognition modifier slider (unified 0–100%); context
/// is classified by ancestor type names via <see cref="GlobalTextBlurRules"/>
/// (cards, hover tips, menus, dialogue, intents, settings, the compendium and
/// mod-owned labels are left to their own patches).
/// </summary>
internal static class GlobalTextBlurSource
{
    private const int MaxAncestorHops = 24;
    private const string PendingHookMeta = "BrainFogPendingTreeEntered";

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

            if (label.HasMeta(TextBlurService.InputMeta)
                && label.GetMeta(TextBlurService.InputMeta).AsString() == text
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

            if (!ShouldBlur(label))
            {
                SyncReadableStatusNumber(label, text);
                return;
            }

            if (!label.IsInsideTree())
            {
                // Context is unknown until the node is parented; blur once it
                // enters the tree (rare: labels configured before AddChild).
                SchedulePending(label);
                return;
            }

            var percent = DifficultyRuntime.TextBlurPercent;
            var blurred = EventTextBlurrer.Blur(text, percent, BlurSalt.Current);
            label.SetMeta(TextBlurService.InputMeta, text);
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

    /// <summary>True when the catch-all rule covers this label (another patch
    /// owns it or it must stay readable otherwise).</summary>
    public static bool ShouldBlur(CanvasItem label)
    {
        if (DifficultyRuntime.Current.ReadableStatusNumbers && IsStatusNumberLabel(label))
        {
            return false; // panel option: HP/gold numbers stay readable
        }

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

        return GlobalTextBlurRules.ShouldBlur(types, modOwned);
    }

    /// <summary>True for the HP/gold value labels the panel's readable-status
    /// option covers: the top-bar HP/gold numbers and the local player's combat
    /// health-bar number (block numbers and enemy bars stay garbled).</summary>
    public static bool IsStatusNumberLabel(CanvasItem label)
    {
        var name = label.Name.ToString();
        if (!GlobalTextBlurRules.IsStatusNumberName(name))
        {
            return false;
        }

        var hp = name.Contains("HpLabel", StringComparison.Ordinal);
        var types = new List<string>(8);
        var parent = label.GetParent();
        for (var i = 0; parent != null && i < MaxAncestorHops; i++, parent = parent.GetParent())
        {
            types.Add(parent.GetType().Name);
            if (hp && parent is NHealthBar bar
                && bar._creature is { IsPlayer: true } creature
                && LocalContext.IsMe(creature))
            {
                return true;
            }
        }

        return GlobalTextBlurRules.IsStatusNumberLabel(types, name, localPlayerHealthBar: false);
    }

    /// <summary>While a status-number label stays readable, keep its stored
    /// original in sync so re-enabling the blur garbles the current value
    /// immediately instead of waiting for the next game write.</summary>
    private static void SyncReadableStatusNumber(CanvasItem label, string text)
    {
        if (!DifficultyRuntime.Current.ReadableStatusNumbers
            || !GlobalTextBlurRules.IsStatusNumberName(label.Name.ToString())
            || !IsStatusNumberLabel(label))
        {
            return;
        }
        label.SetMeta(TextBlurService.InputMeta, text);
        label.SetMeta(TextBlurService.OutputMeta, text);
    }

    private static bool IsModOwned(StringName name) =>
        name.ToString().StartsWith("BrainFog", StringComparison.Ordinal);

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
            if (ShouldBlur(label))
            {
                TextBlurService.BlurNode(label, DifficultyRuntime.TextBlurPercent);
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("GlobalTextBlur.Pending", () => throw ex);
        }
    }
}
