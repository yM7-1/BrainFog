using BrainFog.Core.Text;
using Godot;

namespace BrainFog.Game;

/// <summary>
/// Shared, idempotent label blur. The last blurred output is stored in node
/// metadata, so repeated calls (timer sweep + per-screen patches) never
/// double-blur and re-blur automatically once the game writes new text.
/// </summary>
internal static class TextBlurService
{
    public const string OutputMeta = "BrainFogBlurOutput";

    /// <summary>Blurs the node's text in place. Returns true when the text
    /// changed (drives the sweep backoff).</summary>
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
            node.SetMeta(OutputMeta, blurred);
            Write(node, blurred);
            return true;
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Node", () => throw ex);
            return false;
        }
    }

    /// <summary>Marks a node whose text was blurred at the source, so the
    /// timer sweep leaves it alone.</summary>
    public static void Mark(CanvasItem? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }
        var current = Read(node);
        if (!string.IsNullOrEmpty(current))
        {
            node.SetMeta(OutputMeta, current);
        }
    }

    /// <summary>Marks a node with the blur output that is about to be applied
    /// (source-side blur in a SetTextAutoSize prefix).</summary>
    public static void MarkOutput(CanvasItem? node, string output)
    {
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            node.SetMeta(OutputMeta, output);
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
