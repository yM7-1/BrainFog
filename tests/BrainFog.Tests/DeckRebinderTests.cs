using BrainFog.Core.Reveal;
using Xunit;

namespace BrainFog.Tests;

public class DeckRebinderTests
{
    private const string Id1 = "BrainFog.11111111111111111111111111111111";
    private const string Id2 = "BrainFog.22222222222222222222222222222222";
    private const string Id3 = "BrainFog.33333333333333333333333333333333";
    private const string Id4 = "BrainFog.44444444444444444444444444444444";

    [Fact]
    public void ExactOrder_BindsEverySlot()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "B", "C" },
            new[] { Id1, Id2, Id3 },
            new[] { "A", "B", "C" });

        Assert.Equal(new[] { (0, Id1), (1, Id2), (2, Id3) }, ToTuples(bindings));
    }

    [Fact]
    public void AppendedDeckCard_SkipsTheNewSlot()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "B", "C", "D" },
            new[] { Id1, Id2, Id3 },
            new[] { "A", "B", "C" });

        Assert.Equal(new[] { (0, Id1), (1, Id2), (2, Id3) }, ToTuples(bindings));
    }

    [Fact]
    public void InsertedDeckCard_ResyncsByKey()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "X", "B", "C" },
            new[] { Id1, Id2, Id3 },
            new[] { "A", "B", "C" });

        Assert.Equal(new[] { (0, Id1), (2, Id2), (3, Id3) }, ToTuples(bindings));
    }

    [Fact]
    public void RemovedDeckCard_SkipsTheSavedSlot()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "C" },
            new[] { Id1, Id2, Id3 },
            new[] { "A", "B", "C" });

        Assert.Equal(new[] { (0, Id1), (1, Id3) }, ToTuples(bindings));
    }

    [Fact]
    public void DuplicateDefinitions_MatchInOrder()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "A", "B" },
            new[] { Id1, Id2 },
            new[] { "A", "A" });

        Assert.Equal(new[] { (0, Id1), (1, Id2) }, ToTuples(bindings));
    }

    [Fact]
    public void LegacySave_NoKeys_UsesIndexAlignment()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "B" },
            new[] { Id1, Id2 },
            savedKeys: null);

        Assert.Equal(new[] { (0, Id1), (1, Id2) }, ToTuples(bindings));
    }

    [Fact]
    public void LegacySave_MismatchedKeyCount_UsesIndexAlignment()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "B" },
            new[] { Id1, Id2 },
            new[] { "A" });

        Assert.Equal(new[] { (0, Id1), (1, Id2) }, ToTuples(bindings));
    }

    [Fact]
    public void DuplicateIds_AreDropped()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "B" },
            new[] { Id1, Id1 },
            new[] { "A", "B" });

        Assert.Empty(bindings);
    }

    [Fact]
    public void InvalidIds_AreDropped()
    {
        var bindings = DeckRebinder.Align(
            new[] { "A", "B", "C" },
            new[] { "not-an-id", Id2, Id3 },
            new[] { "A", "B", "C" });

        Assert.Equal(new[] { (1, Id2), (2, Id3) }, ToTuples(bindings));
    }

    [Fact]
    public void EmptyInputs_ReturnNothing()
    {
        Assert.Empty(DeckRebinder.Align(Array.Empty<string>(), new[] { Id1 }, new[] { "A" }));
        Assert.Empty(DeckRebinder.Align(new[] { "A" }, Array.Empty<string>(), Array.Empty<string>()));
    }

    [Fact]
    public void UnrelatedDecks_DoNotMisbind()
    {
        var bindings = DeckRebinder.Align(
            new[] { "X", "Y" },
            new[] { Id1, Id2, Id3, Id4 },
            new[] { "A", "B", "C", "D" });

        Assert.Empty(bindings);
    }

    private static (int, string)[] ToTuples(IReadOnlyList<DeckRebinder.Binding> bindings)
    {
        var result = new (int, string)[bindings.Count];
        for (var i = 0; i < bindings.Count; i++)
        {
            result[i] = (bindings[i].DeckIndex, bindings[i].InstanceId);
        }
        return result;
    }
}
