using BrainFog.Core.Text;
using Godot;

namespace BrainFog.Game;

/// <summary>
/// Shared, idempotent label blur. The original text and the last blurred output
/// are stored in node metadata, so repeated calls never double-blur, a game
/// rewrite is re-blurred automatically, and a blur-ratio change can re-apply
/// from the original (slider, 2026-09-21).
/// </summary>
internal static class TextBlurService
{
    public const string OutputMeta = "BrainFogBlurOutput";

    /// <summary>Original (ungarbled) text, kept so the ratio can change later.</summary>
    public const string InputMeta = "BrainFogBlurInput";

    /// <summary>Blurs the node's text in place at the given ratio. Returns true
    /// when the visible text changed.</summary>
    public static bool BlurNode(CanvasItem? node, int percent)
    {
        try
        {
            if (node == null || !GodotObject.IsInstanceValid(node))
            {
                return false;
            }
            var current = Read(node);
            if (string.IsNullOrEmpty(current))
            {
                return false;
            }
            if (node.HasMeta(OutputMeta) && node.GetMeta(OutputMeta).AsString() == current)
            {
                return false; // unchanged since our last blur
            }

            var blurred = EventTextBlurrer.Blur(current, percent, BlurSalt.Current);
            node.SetMeta(InputMeta, current);
            node.SetMeta(OutputMeta, blurred);
            if (string.Equals(blurred, current, StringComparison.Ordinal))
            {
                return false;
            }
            Write(node, blurred);
            return true;
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Node", () => throw ex);
            return false;
        }
    }

    /// <summary>Marks a node with the blur output that is about to be applied
    /// (source-side blur in a setter prefix).</summary>
    public static void MarkOutput(CanvasItem? node, string output)
    {
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            node.SetMeta(OutputMeta, output);
        }
    }

    /// <summary>Re-blurs one node from its stored original at a new ratio
    /// (no-op when the game owns the current text or no original is known).</summary>
    public static bool Reapply(CanvasItem? node, int percent)
    {
        try
        {
            if (node == null || !GodotObject.IsInstanceValid(node) || !node.HasMeta(InputMeta))
            {
                return false;
            }
            var original = node.GetMeta(InputMeta).AsString();
            if (string.IsNullOrEmpty(original))
            {
                return false;
            }
            if (node.HasMeta(OutputMeta) && Read(node) != node.GetMeta(OutputMeta).AsString())
            {
                return false; // the game wrote new text: leave it to the normal path
            }

            var blurred = EventTextBlurrer.Blur(original, percent, BlurSalt.Current);
            node.SetMeta(OutputMeta, blurred);
            if (!string.Equals(Read(node), blurred, StringComparison.Ordinal))
            {
                Write(node, blurred);
            }
            return true;
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Reapply", () => throw ex);
            return false;
        }
    }

    /// <summary>Restores a label's stored original text (it is exempt from the
    /// blur, e.g. readable HP/gold numbers after a panel toggle). Returns true
    /// when the visible text changed.</summary>
    public static bool Restore(CanvasItem? node)
    {
        try
        {
            if (node == null || !GodotObject.IsInstanceValid(node) || !node.HasMeta(InputMeta))
            {
                return false;
            }
            var original = node.GetMeta(InputMeta).AsString();
            if (string.IsNullOrEmpty(original) || Read(node) == original)
            {
                return false;
            }
            if (node.HasMeta(OutputMeta) && Read(node) != node.GetMeta(OutputMeta).AsString())
            {
                return false; // the game wrote new text: leave it to the normal path
            }

            Write(node, original);
            // Drop the memoized blur output: it described the blurred text, and
            // leaving it equal to the readable text made BlurNode treat the node
            // as "already blurred" forever (map-legend toggle round-trip, 0.3.7).
            // Reapply/ReapplyAllText read InputMeta, so re-blurring still works.
            node.RemoveMeta(OutputMeta);
            return true;
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Restore", () => throw ex);
            return false;
        }
    }

    /// <summary>Restores every label that carries a stored original to vanilla
    /// text (mod disabled, 0.3.7). Dedicated-patch contexts included: with the
    /// mod off, their patches skip too, so nothing re-blurs them.</summary>
    public static void RestoreAll() =>
        PatchGuard.Run("TextBlur.RestoreAll", () =>
        {
            if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            {
                return;
            }
            WalkRestore(tree.Root, 0);
        });

    private static void WalkRestore(Node node, int depth)
    {
        if (depth > 64)
        {
            return;
        }
        if (node is CanvasItem item && item.HasMeta(InputMeta))
        {
            Restore(item);
        }
        foreach (var child in node.GetChildren())
        {
            WalkRestore(child, depth + 1);
        }
    }

    /// <summary>Re-applies the blur ratio to every label that has a stored
    /// original (panel slider). Only the readable-status-number labels are
    /// restored to their original text (2026-09-21); everything else keeps its
    /// blur, including contexts owned by dedicated patches.</summary>
    public static void ReapplyAllText(int percent) =>
        PatchGuard.Run("TextBlur.ReapplyAll", () =>
        {
            if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            {
                return;
            }
            Walk(tree.Root, percent, 0);
        });

    private static void Walk(Node node, int percent, int depth)
    {
        if (depth > 64)
        {
            return;
        }
        if (node is CanvasItem item && item.HasMeta(InputMeta))
        {
            if (GlobalTextBlurSource.IsReadableStatusNumber(item))
            {
                Restore(item);
            }
            else
            {
                // Everything else that carries a stored original keeps its blur,
                // including contexts owned by dedicated patches (main menu,
                // cards, dialogue): those stay garbled, never restored.
                Reapply(item, percent);
            }
        }
        foreach (var child in node.GetChildren())
        {
            Walk(child, percent, depth + 1);
        }
    }

    public static string? Read(Node node) => node switch
    {
        RichTextLabel rich => rich.Text,
        Label plain => plain.Text,
        _ => null,
    };

    public static void Write(Node node, string text)
    {
        switch (node)
        {
            case RichTextLabel rich:
                rich.Text = text;
                break;
            case Label plain:
                plain.Text = text;
                break;
        }
    }
}
