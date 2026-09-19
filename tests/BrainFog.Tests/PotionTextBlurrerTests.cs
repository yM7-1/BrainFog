using System.Text;
using BrainFog.Core.Text;
using Xunit;

namespace BrainFog.Tests;

public class PotionTextBlurrerTests
{
    [Fact]
    public void Blur_IsDeterministic()
    {
        const string text = "获得 2 点力量。";
        Assert.Equal(PotionTextBlurrer.Blur(text), PotionTextBlurrer.Blur(text));
    }

    [Fact]
    public void Blur_RoughlyMatchesFiftyPercent()
    {
        var text = string.Concat(Enumerable.Repeat("这是一瓶喝了会获得两点力量的神奇药水。", 100));
        var result = PotionTextBlurrer.Blur(text);
        var total = 0;
        var changed = 0;
        for (var i = 0; i < text.Length;)
        {
            var rune = Rune.GetRuneAt(text, i);
            var size = rune.Utf16SequenceLength;
            if (!Rune.IsWhiteSpace(rune))
            {
                total++;
                if (result[i] != text[i])
                {
                    changed++;
                }
            }
            i += size;
        }
        var ratio = (double)changed / total;
        Assert.InRange(ratio, 0.40, 0.60);
    }

    [Fact]
    public void Blur_HandlesEmptyInput()
    {
        Assert.Equal(string.Empty, PotionTextBlurrer.Blur(null));
        Assert.Equal(string.Empty, PotionTextBlurrer.Blur(string.Empty));
    }

    [Fact]
    public void Blur_KeepsWhitespace()
    {
        var result = PotionTextBlurrer.Blur("a b");
        Assert.Equal(' ', result[1]);
    }
}
