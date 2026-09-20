using BrainFog.Core.Options;
using BrainFog.Core.Text;
using Xunit;

namespace BrainFog.Tests;

public class TextBlurPercentsTests
{
    [Fact]
    public void Default_IsFifty()
    {
        Assert.Equal(50, TextBlurPercents.Default);
        Assert.Equal(TextBlurPercents.Default, new DifficultySettings().TextBlurPercent);
    }

    [Theory]
    [InlineData(-20, 0)]
    [InlineData(0, 0)]
    [InlineData(37, 37)]
    [InlineData(100, 100)]
    [InlineData(140, 100)]
    public void ClampBlurPercent_ClampsToSliderRange(int input, int expected)
    {
        Assert.Equal(expected, DifficultySettings.ClampBlurPercent(input));
    }
}
