using System.Reflection;
using System.Text.Json;
using Xunit;

namespace BlindSpire.Tests;

/// <summary>
/// Release-shape regressions: the manifest must satisfy the game's loader and the
/// shipped mod assembly must contain the Core logic (no separate Core.dll needed).
/// </summary>
public class ReleaseShapeTests
{
    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BlindSpire.json")))
            {
                dir = dir.Parent;
            }
            Assert.True(dir != null, "repo root with BlindSpire.json not found");
            return dir!.FullName;
        }
    }

    [Fact]
    public void Manifest_HasLoaderRequiredFields()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "BlindSpire.json")));
        var root = doc.RootElement;

        Assert.Equal("BlindSpire", root.GetProperty("id").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("min_game_version").GetString()));
        Assert.True(root.GetProperty("has_dll").GetBoolean());
        Assert.True(root.GetProperty("affects_gameplay").GetBoolean());

        var deps = root.GetProperty("dependencies");
        Assert.Contains(deps.EnumerateArray(),
            d => d.GetProperty("id").GetString() == "STS2-RitsuLib");
    }

    [Fact]
    public void ShippedModAssembly_ContainsCoreTypes()
    {
        var dll = Path.Combine(RepoRoot, ".godot", "mono", "temp", "bin", "Release", "BlindSpire.dll");
        Assert.True(File.Exists(dll), $"mod assembly not found at {dll}; run tools/check.sh (build first)");

        using var ctx = new MetadataLoadContext(
            new PathAssemblyResolver(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll")));
        var assembly = ctx.LoadFromAssemblyPath(dll);

        Assert.NotNull(assembly.GetType("BlindSpire.Core.Reveal.CardRevealTracker"));
        Assert.NotNull(assembly.GetType("BlindSpire.Core.Text.EventTextBlurrer"));
        Assert.NotNull(assembly.GetType("BlindSpire.Entry"));
    }
}
