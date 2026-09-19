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

    public static void BlurNode(CanvasItem? node, int percent)
    {
        try
        {
            if (node == null || !GodotObject.IsInstanceValid(node))
            {
                return;
            }
            var current = Read(node);
            if (string.IsNullOrEmpty(current))
            {
                return;
            }
            if (node.HasMeta(OutputMeta) && node.GetMeta(OutputMeta).AsString() == current)
            {
                return; // unchanged since our last blur
            }

            var blurred = EventTextBlurrer.Blur(current, percent);
            node.SetMeta(OutputMeta, blurred);
            Write(node, blurred);
        }
        catch (Exception ex)
        {
            PatchGuard.Run("TextBlur.Node", () => throw ex);
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
