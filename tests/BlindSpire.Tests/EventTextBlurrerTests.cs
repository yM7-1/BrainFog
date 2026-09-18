using BlindSpire.Core.Text;
using Xunit;

namespace BlindSpire.Tests;

public class EventTextBlurrerTests
{
    [Fact]
    public void Blur_IsDeterministic()
    {
        const string text = "你走进了一个阴暗的房间，四周弥漫着雾气。";
        Assert.Equal(EventTextBlurrer.Blur(text), EventTextBlurrer.Blur(text));
    }

    [Fact]
    public void Blur_KeepsWhitespaceAndNewlines()
    {
        var result = EventTextBlurrer.Blur("a b\nc");
        Assert.Equal(' ', result[1]);
        Assert.Equal('\n', result[3]);
    }

    [Fact]
    public void Blur_PreservesBbCodeTags()
    {
        const string text = "[b]危险[/b]的[color=red]选择[/color]";
        var result = EventTextBlurrer.Blur(text);
        Assert.Contains("[b]", result);
        Assert.Contains("[/b]", result);
        Assert.Contains("[color=red]", result);
        Assert.Contains("[/color]", result);
    }

    [Fact]
    public void Blur_RoughlyMatchesConfiguredRatio()
    {
        var text = string.Concat(Enumerable.Repeat("这是一个用于测试模糊比例的中文句子。", 100));
        var result = EventTextBlurrer.Blur(text);
        var total = text.Count(c => !char.IsWhiteSpace(c));
        var changed = text.Where((c, i) => !char.IsWhiteSpace(c) && result[i] != c).Count();
        var ratio = (double)changed / total;
        Assert.InRange(ratio, 0.65, 0.85);
    }

    [Fact]
    public void Blur_HandlesEmptyInput()
    {
        Assert.Equal(string.Empty, EventTextBlurrer.Blur(null));
        Assert.Equal(string.Empty, EventTextBlurrer.Blur(string.Empty));
    }
}
