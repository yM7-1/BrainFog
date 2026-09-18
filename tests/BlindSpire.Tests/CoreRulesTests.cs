using BlindSpire.Core;
using BlindSpire.Core.Reveal;
using Xunit;

namespace BlindSpire.Tests;

public class InstanceIdsTests
{
    [Fact]
    public void NewId_HasPrefixAndIsValid()
    {
        var id = InstanceIds.NewId();
        Assert.StartsWith(InstanceIds.Prefix, id);
        Assert.True(InstanceIds.IsValid(id));
    }

    [Fact]
    public void NewId_IsUnique()
    {
        Assert.NotEqual(InstanceIds.NewId(), InstanceIds.NewId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BlindSpire.")]
    [InlineData("Other.a")]
    public void IsValid_RejectsBadIds(string? id)
    {
        Assert.False(InstanceIds.IsValid(id));
    }
}

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
