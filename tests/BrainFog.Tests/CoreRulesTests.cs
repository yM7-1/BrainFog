using BrainFog.Core;
using BrainFog.Core.Reveal;
using BrainFog.Core.Runs;
using Xunit;

namespace BrainFog.Tests;

public class MultiplayerGuardTests
{
    [Fact]
    public void Multiplayer_NoLongerDisablesMod()
    {
        Assert.True(MultiplayerGuard.IsActiveInSession(isMultiplayer: true));
        Assert.True(MultiplayerGuard.IsActiveInSession(isMultiplayer: false));
    }

    [Fact]
    public void MemoryFade_IsSinglePlayerOnly()
    {
        Assert.False(MultiplayerGuard.MemoryFadeAllowed(isMultiplayer: true));
        Assert.True(MultiplayerGuard.MemoryFadeAllowed(isMultiplayer: false));
    }

    [Fact]
    public void Notices_ArePresent()
    {
        Assert.False(string.IsNullOrWhiteSpace(MultiplayerGuard.MultiplayerNotice));
        Assert.False(string.IsNullOrWhiteSpace(MultiplayerGuard.MultiplayerSetupNotice));
    }
}

public class RunModeRulesTests
{
    [Theory]
    [InlineData("Blitz", RunModeKind.Blitz)]
    [InlineData("blitz", RunModeKind.Blitz)]
    [InlineData("Dread", RunModeKind.Dread)]
    [InlineData("Standard", RunModeKind.Standard)]
    [InlineData("Daily", RunModeKind.Daily)]
    [InlineData("Custom", RunModeKind.Custom)]
    [InlineData("None", RunModeKind.Unknown)]
    [InlineData("future-mode", RunModeKind.Unknown)]
    [InlineData(null, RunModeKind.Unknown)]
    public void Parse_ByGameModeName(string? name, RunModeKind expected) =>
        Assert.Equal(expected, RunModeRules.Parse(name));

    [Fact]
    public void Blitz_HasNoActMapAndNoGold()
    {
        Assert.False(RunModeRules.HasActMap(RunModeKind.Blitz));
        Assert.False(RunModeRules.HasGold(RunModeKind.Blitz));
        Assert.True(RunModeRules.HasActMap(RunModeKind.Standard));
        Assert.True(RunModeRules.HasGold(RunModeKind.Dread));
    }

    [Fact]
    public void Dread_UsesDominion()
    {
        Assert.True(RunModeRules.UsesDominion(RunModeKind.Dread));
        Assert.False(RunModeRules.UsesDominion(RunModeKind.Standard));
    }

    [Fact]
    public void MemoryFade_HasASinglePlayerGate()
    {
        Assert.False(RunModeRules.MemoryFadeAllowed(isMultiplayer: true));
        Assert.True(RunModeRules.MemoryFadeAllowed(isMultiplayer: false));
    }
}

public class ScopeRulesTests
{
    [Theory]
    [InlineData(false, true, false, false)] // single-player: never remote
    [InlineData(true, false, false, false)] // unknown owner: keep local scope
    [InlineData(true, true, true, false)]   // local card
    [InlineData(true, true, false, true)]   // teammate card
    public void IsRemoteCard_OnlyInMultiplayerAndWhenOwnedBySomeoneElse(
        bool multiplayer, bool hasOwner, bool isLocalOwner, bool expected) =>
        Assert.Equal(expected, ScopeRules.IsRemoteCard(multiplayer, hasOwner, isLocalOwner));

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, true)]
    public void IsRemotePlayer_OnlyInMultiplayer(bool multiplayer, bool hasPlayer, bool isLocal, bool expected) =>
        Assert.Equal(expected, ScopeRules.IsRemotePlayer(multiplayer, hasPlayer, isLocal));
}
