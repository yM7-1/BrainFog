using BrainFog.Core;
using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class MultiplayerGuardTests
{
    [Fact]
    public void Multiplayer_DisablesMod()
    {
        Assert.True(MultiplayerGuard.ShouldDisable(isMultiplayer: true));
        Assert.False(MultiplayerGuard.ShouldDisable(isMultiplayer: false));
    }

    [Fact]
    public void DisabledReason_IsPresent()
    {
        Assert.False(string.IsNullOrWhiteSpace(MultiplayerGuard.DisabledReason));
    }
}
