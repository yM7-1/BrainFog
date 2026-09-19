using System.Text;
using BrainFog.Core.Text;
using Xunit;

namespace BrainFog.Tests;

public class EventTextBlurrerTests
{
    [Fact]
    public void Blur_IsDeterministic()
    {
        const string text = "你走进了一个阴暗的房间，四周弥漫着雾气。";
        Assert.Equal(EventTextBlurrer.Blur(text), EventTextBlurrer.Blur(text));
    }

    [Fact]
    public void Blur_SameSaltAndText_AreStable()
    {
        const string text = "你走进了一个阴暗的房间，四周弥漫着雾气。";
        Assert.Equal(
            EventTextBlurrer.Blur(text, 75, 12345),
            EventTextBlurrer.Blur(text, 75, 12345));
    }

    [Fact]
    public void Blur_DifferentSalt_RollsADifferentPattern()
    {
        const string text = "你走进了一个阴暗的房间，四周弥漫着雾气，远处传来低沉的钟声。";
        Assert.NotEqual(
            EventTextBlurrer.Blur(text, 75, 1),
            EventTextBlurrer.Blur(text, 75, 2));
    }

    [Fact]
    public void Blur_SaltedBlur_KeepsWhitespaceAndRatio()
    {
        var text = string.Concat(Enumerable.Repeat("这是一个用于测试模糊比例的中文句子。", 100));
        var result = EventTextBlurrer.Blur(text, 75, unchecked((int)0xDEADBEEF));
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
            else
            {
                Assert.Equal(text[i], result[i]);
            }
            i += size;
        }
        Assert.InRange((double)changed / total, 0.65, 0.85);
    }

    [Fact]
    public void Blur_SaltedBlur_PreservesBbCodeAndImgPaths()
    {
        const string text = "[b]危险[/b]的[color=red]选择[/color] [img]res://a/b.png[/img]";
        var result = EventTextBlurrer.Blur(text, 75, 777);
        Assert.Contains("[b]", result);
        Assert.Contains("[/b]", result);
        Assert.Contains("[color=red]", result);
        Assert.Contains("[img]res://a/b.png[/img]", result);
    }

    [Fact]
    public void Blur_SameSalt_ProducesByteIdenticalOutput()
    {
        // Card-face blur relies on this: repeated UpdateVisuals with the same
        // source text must yield the identical string so the game's own
        // "text unchanged" check skips re-shaping (perf 2026-09-20).
        const string text = "造成 6 点伤害。获得 6 点格挡。";
        Assert.Equal(
            EventTextBlurrer.Blur(text, 85, 4242),
            EventTextBlurrer.Blur(text, 85, 4242));
    }

    [Fact]
    public void BlurSalt_For_IsStablePerText()
    {
        Assert.Equal(BlurSalt.For("同一句话"), BlurSalt.For("同一句话"));
        Assert.NotEqual(BlurSalt.For("第一句话"), BlurSalt.For("第二句话"));
    }

    [Fact]
    public void Blur_KeepsWhitespaceAndNewlines()
    {
        var result = EventTextBlurrer.Blur("a b\nc");
        Assert.Equal(' ', result[1]);
        Assert.Equal('\n', result[3]);
    }

    [Fact]
    public void Blur_PreservesRealBbCodeTags()
    {
        const string text = "[b]危险[/b]的[color=red]选择[/color]";
        var result = EventTextBlurrer.Blur(text);
        Assert.Contains("[b]", result);
        Assert.Contains("[/b]", result);
        Assert.Contains("[color=red]", result);
        Assert.Contains("[/color]", result);
    }

    [Theory]
    [InlineData("[b]", true)]
    [InlineData("[/b]", true)]
    [InlineData("[color=red]", true)]
    [InlineData("[shake rate=5]", true)]
    [InlineData("[攻击]", false)]
    [InlineData("[", false)]
    [InlineData("[b", false)]
    [InlineData("[=x]", false)]
    [InlineData("[123]", false)]
    public void TryReadBbCodeTag_DistinguishesTagsFromLiteralBrackets(string text, bool expected)
    {
        Assert.Equal(expected, EventTextBlurrer.TryReadBbCodeTag(text, 0, out _));
    }

    [Fact]
    public void Blur_PreservesImgResourcePath()
    {
        const string text = "获得[img]res://images/icons/potion.png[/img]效果";
        var result = EventTextBlurrer.Blur(text);
        Assert.Contains("[img]res://images/icons/potion.png[/img]", result);
    }

    [Fact]
    public void Blur_PreservesImgPathAtAnyRatio()
    {
        const string text = "[img]res://a/b/c.png[/img]";
        foreach (var percent in new[] { 50, 60, 75, 85, 90, 100 })
        {
            var result = EventTextBlurrer.Blur(text, percent);
            Assert.Contains("res://a/b/c.png", result);
            Assert.Contains("[img]", result);
            Assert.Contains("[/img]", result);
        }
    }

    [Fact]
    public void IsImgTag_DistinguishesImgFromOtherTags()
    {
        Assert.True(EventTextBlurrer.TryReadBbCodeTag("[img]", 0, out var end) && EventTextBlurrer.IsImgTag("[img]", 0, end));
        Assert.True(EventTextBlurrer.TryReadBbCodeTag("[img=64]", 0, out var end2) && EventTextBlurrer.IsImgTag("[img=64]", 0, end2));
        Assert.False(EventTextBlurrer.TryReadBbCodeTag("[/img]", 0, out var end3) && EventTextBlurrer.IsImgTag("[/img]", 0, end3));
        Assert.False(EventTextBlurrer.TryReadBbCodeTag("[b]", 0, out var end4) && EventTextBlurrer.IsImgTag("[b]", 0, end4));
        Assert.False(EventTextBlurrer.TryReadBbCodeTag("[image]", 0, out var end5) && EventTextBlurrer.IsImgTag("[image]", 0, end5));
    }

    [Fact]
    public void Blur_LiteralBracketedWords_AreStillBlurred()
    {
        const string text = "[攻击攻击攻击攻击攻击攻击攻击攻击]";
        var result = EventTextBlurrer.Blur(text);
        Assert.DoesNotContain("攻击攻击攻击攻击攻击攻击攻击攻击", result);
    }

    [Fact]
    public void Blur_EmojiSurviveAsWholeRunes()
    {
        const string text = "😀😀😀😀😀😀😀😀😀😀";
        var result = EventTextBlurrer.Blur(text);
        Assert.True(IsWellFormedUtf16(result), "blur produced unpaired surrogates");
    }

    [Fact]
    public void Blur_FamilyEmojiWithJoinersStaysWellFormed()
    {
        const string text = "👨‍👩‍👧‍👦 你好 👩‍💻";
        var result = EventTextBlurrer.Blur(text);
        Assert.True(IsWellFormedUtf16(result), "blur produced unpaired surrogates");
    }

    [Fact]
    public void Blur_RoughlyMatchesConfiguredRatio()
    {
        var text = string.Concat(Enumerable.Repeat("这是一个用于测试模糊比例的中文句子。", 100));
        var result = EventTextBlurrer.Blur(text);
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
        Assert.InRange(ratio, 0.65, 0.85);
    }

    [Fact]
    public void Blur_HandlesEmptyInput()
    {
        Assert.Equal(string.Empty, EventTextBlurrer.Blur(null));
        Assert.Equal(string.Empty, EventTextBlurrer.Blur(string.Empty));
    }

    private static bool IsWellFormedUtf16(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]))
            {
                if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                {
                    return false;
                }
                i++;
            }
            else if (char.IsLowSurrogate(text[i]))
            {
                return false;
            }
        }
        return true;
    }
}
