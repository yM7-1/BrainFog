using System.Reflection;
using Xunit;

namespace BlindSpire.Tests;

/// <summary>
/// Audits that the game assembly still contains the symbols BlindSpire patches.
/// Guards against game-version drift (target game: STS2 v0.111.0).
/// </summary>
public class PatchTargetAuditTests
{
    private static readonly string Sts2DataDir =
        Environment.GetEnvironmentVariable("STS2_DATA_DIR")
        ?? "/mnt/d/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64";

    private static MetadataLoadContext CreateContext()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assemblies = new List<string>();
        var candidates = Directory.GetFiles(runtimeDir, "*.dll")
            .Concat(Directory.GetFiles(Sts2DataDir, "*.dll"));
        foreach (var path in candidates)
        {
            var name = Path.GetFileName(path);
            if (name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (seen.Add(name))
            {
                assemblies.Add(path);
            }
        }
        return new MetadataLoadContext(new PathAssemblyResolver(assemblies));
    }

    private static Type RequireType(MetadataLoadContext ctx, string fullName)
    {
        var type = ctx.LoadFromAssemblyName(new AssemblyName("sts2")).GetType(fullName);
        Assert.True(type != null, $"Missing type: {fullName}");
        return type!;
    }

    [Fact]
    public void GameAssembly_IsPresent()
    {
        Assert.True(File.Exists(Path.Combine(Sts2DataDir, "sts2.dll")),
            $"sts2.dll not found under {Sts2DataDir}");
    }

    [Fact]
    public void NCard_HasFaceAndFrameMembers()
    {
        using var ctx = CreateContext();
        var type = RequireType(ctx, "MegaCrit.Sts2.Core.Nodes.Cards.NCard");
        foreach (var field in new[] { "_portrait", "_frame", "_titleLabel", "_descriptionLabel", "_energyLabel" })
        {
            Assert.True(type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic) != null,
                $"NCard.{field} missing");
        }
    }

    [Fact]
    public void CardModel_HasPlayWrapperHook()
    {
        using var ctx = CreateContext();
        var type = RequireType(ctx, "MegaCrit.Sts2.Core.Models.CardModel");
        Assert.True(type.GetMethod("OnPlayWrapper", BindingFlags.Instance | BindingFlags.Public) != null,
            "CardModel.OnPlayWrapper missing");
        Assert.True(type.GetMethod("ToSerializable", BindingFlags.Instance | BindingFlags.Public) != null,
            "CardModel.ToSerializable missing");
        Assert.True(type.GetMethod("FromSerializable", BindingFlags.Static | BindingFlags.Public) != null,
            "CardModel.FromSerializable missing");
        var serializable = RequireType(ctx, "MegaCrit.Sts2.Core.Saves.Runs.SerializableCard");
        Assert.True(serializable.GetProperty("Props", BindingFlags.Instance | BindingFlags.Public) != null,
            "SerializableCard.Props missing");
    }

    [Fact]
    public void TopBar_HasHealthAndGold()
    {
        using var ctx = CreateContext();
        var type = RequireType(ctx, "MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar");
        Assert.True(type.GetProperty("Hp", BindingFlags.Instance | BindingFlags.Public) != null, "NTopBar.Hp missing");
        Assert.True(type.GetProperty("Gold", BindingFlags.Instance | BindingFlags.Public) != null, "NTopBar.Gold missing");
    }

    [Fact]
    public void CombatVisuals_HavePatchTargets()
    {
        using var ctx = CreateContext();
        RequireType(ctx, "MegaCrit.Sts2.Core.Nodes.Combat.NCreature");
        RequireType(ctx, "MegaCrit.Sts2.Core.Nodes.Combat.NIntent");
    }
}
