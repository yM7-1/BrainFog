using System.Text;

namespace BlindSpire.Core.Text;

/// <summary>
/// Deterministic 75% blur for event text (spec 0.03 e): the same input always
/// produces the same output ("固定不重掷"), whitespace and valid BBCode tags survive.
/// Blurring works per Unicode rune so surrogate pairs (emoji) never break.
/// </summary>
public static class EventTextBlurrer
{
    public const int BlurPercent = 75;

    private const string Glyphs = "▓▒░█◇◆＊※§#@%&";

    public static string Blur(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var rng = new Random(StableHash(text));
        var sb = new StringBuilder(text.Length);
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '[' && TryReadBbCodeTag(text, i, out var tagEnd))
            {
                sb.Append(text, i, tagEnd - i + 1);
                i = tagEnd + 1;
                continue;
            }

            var rune = Rune.GetRuneAt(text, i);
            var size = rune.Utf16SequenceLength;
            if (Rune.IsWhiteSpace(rune))
            {
                sb.Append(text, i, size);
            }
            else if (rng.Next(100) < BlurPercent)
            {
                sb.Append(Glyphs[rng.Next(Glyphs.Length)]);
            }
            else
            {
                sb.Append(text, i, size);
            }
            i += size;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns true when a real BBCode tag starts at <paramref name="start"/>.
    /// Plain bracketed words (e.g. "[攻击]") are NOT tags and stay blur-eligible.
    /// </summary>
    internal static bool TryReadBbCodeTag(string text, int start, out int end)
    {
        end = -1;
        if (start >= text.Length || text[start] != '[')
        {
            return false;
        }

        var i = start + 1;
        if (i < text.Length && text[i] == '/')
        {
            i++;
        }

        if (i >= text.Length || !char.IsAsciiLetter(text[i]))
        {
            return false;
        }

        while (i < text.Length && (char.IsAsciiLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '-'))
        {
            i++;
        }

        // Optional '=value' right after the tag name, e.g. [color=red].
        if (i < text.Length && text[i] == '=')
        {
            i++;
            var valueStart = i;
            while (i < text.Length && text[i] != ']' && text[i] != '[' && text[i] != '\n')
            {
                i++;
            }
            if (i == valueStart)
            {
                return false;
            }
        }

        // Optional space-separated parameters, e.g. [shake rate=5 level=10].
        while (i < text.Length && text[i] == ' ')
        {
            while (i < text.Length && text[i] == ' ')
            {
                i++;
            }
            if (i >= text.Length)
            {
                return false;
            }
            if (text[i] == ']')
            {
                break;
            }
            var valueStart = i;
            while (i < text.Length && text[i] != ']' && text[i] != '[' && text[i] != '\n' && text[i] != ' ')
            {
                i++;
            }
            if (i == valueStart)
            {
                return false;
            }
        }

        if (i >= text.Length || text[i] != ']')
        {
            return false;
        }

        end = i;
        return true;
    }

    public static int StableHash(string text)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619u;
            }
            return (int)hash;
        }
    }
}
