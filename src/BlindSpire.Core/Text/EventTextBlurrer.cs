using System.Text;

namespace BlindSpire.Core.Text;

/// <summary>
/// Deterministic 75% blur for event text (spec 0.03 e): the same input always
/// produces the same output ("固定不重掷"), whitespace and BBCode tags survive.
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
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '[')
            {
                // Preserve BBCode tags verbatim so rich text still renders.
                var close = text.IndexOf(']', i);
                if (close >= 0)
                {
                    sb.Append(text, i, close - i + 1);
                    i = close;
                    continue;
                }
            }

            if (char.IsWhiteSpace(ch))
            {
                sb.Append(ch);
                continue;
            }

            sb.Append(rng.Next(100) < BlurPercent ? Glyphs[rng.Next(Glyphs.Length)] : ch);
        }
        return sb.ToString();
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
