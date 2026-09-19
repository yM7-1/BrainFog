using BrainFog.Core.Text;
using Xunit;

namespace BrainFog.Tests;

public class TextBlurPercentsTests
{
    [Fact]
    public void Default_IsSixty()
    {
        Assert.Equal(60, TextBlurPercents.Default);
    }

    [Fact]
    public void ExplicitPercents_AreWithinRange()
    {
        var percents = new[]
        {
            TextBlurPercents.Potion,
            TextBlurPercents.CardViewTips,
            TextBlurPercents.Event,
            TextBlurPercents.Menu,
            TextBlurPercents.UiDescription,
            TextBlurPercents.CardFaceText,
            TextBlurPercents.Dialogue,
        };
        foreach (var percent in percents)
        {
            Assert.InRange(percent, 1, 99);
        }
    }

    [Fact]
    public void ExplicitPercents_FollowTheUserRules()
    {
        Assert.Equal(50, TextBlurPercents.Potion);
        Assert.Equal(50, TextBlurPercents.CardViewTips);
        Assert.Equal(75, TextBlurPercents.Event);
        Assert.Equal(75, TextBlurPercents.Menu);
        Assert.Equal(70, TextBlurPercents.UiDescription);
        Assert.Equal(85, TextBlurPercents.CardFaceText);
        Assert.Equal(90, TextBlurPercents.Dialogue);
    }

    [Fact]
    public void PotionBlurrer_UsesTheSharedConstant()
    {
        Assert.Equal(TextBlurPercents.Potion, PotionTextBlurrer.BlurPercent);
    }
}
