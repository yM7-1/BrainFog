using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace BrainFog.Tests;

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
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BrainFog.json")))
            {
                dir = dir.Parent;
            }
            Assert.True(dir != null, "repo root with BrainFog.json not found");
            return dir!.FullName;
        }
    }

    [Fact]
    public void Manifest_HasLoaderRequiredFields()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot, "BrainFog.json")));
        var root = doc.RootElement;

        Assert.Equal("BrainFog", root.GetProperty("id").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("min_game_version").GetString()));
        Assert.True(root.GetProperty("has_dll").GetBoolean());
        Assert.True(root.GetProperty("affects_gameplay").GetBoolean());

        var deps = root.GetProperty("dependencies");
        Assert.Contains(deps.EnumerateArray(),
            d => d.GetProperty("id").GetString() == "STS2-RitsuLib");
    }

    [Fact]
    public void Version_IsConsistentAcrossReleaseFiles()
    {
        var root = RepoRoot;

        var manifest = JsonDocument
            .Parse(File.ReadAllText(Path.Combine(root, "BrainFog.json")))
            .RootElement.GetProperty("version").GetString();
        var workshopManifest = JsonDocument
            .Parse(File.ReadAllText(Path.Combine(root, "packaging", "workshop", "content", "BrainFog", "BrainFog.json")))
            .RootElement.GetProperty("version").GetString();
        var csprojMatch = Regex.Match(
            File.ReadAllText(Path.Combine(root, "BrainFog.csproj")),
            "<Version>([^<]+)</Version>");
        var changelogMatch = Regex.Match(
            File.ReadAllText(Path.Combine(root, "CHANGELOG.md")),
            @"^##\s+(\d+\.\d+\.\d+)",
            RegexOptions.Multiline);

        Assert.True(csprojMatch.Success, "BrainFog.csproj <Version> not found");
        Assert.True(changelogMatch.Success, "CHANGELOG.md top version heading not found");
        Assert.False(string.IsNullOrWhiteSpace(manifest));

        Assert.Equal(manifest, csprojMatch.Groups[1].Value);
        Assert.Equal(manifest, workshopManifest);
        Assert.Equal(manifest, changelogMatch.Groups[1].Value);
    }

    [Fact]
    public void ShippedModAssembly_ContainsCoreTypes()
    {
        var dll = Path.Combine(RepoRoot, ".godot", "mono", "temp", "bin", "Release", "BrainFog.dll");
        Assert.True(File.Exists(dll), $"mod assembly not found at {dll}; run tools/check.sh (build first)");

        using var ctx = new MetadataLoadContext(
            new PathAssemblyResolver(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll")));
        var assembly = ctx.LoadFromAssemblyPath(dll);

        Assert.NotNull(assembly.GetType("BrainFog.Core.Reveal.CardRevealTracker"));
        Assert.NotNull(assembly.GetType("BrainFog.Core.Text.EventTextBlurrer"));
        Assert.NotNull(assembly.GetType("BrainFog.Entry"));
    }
}
