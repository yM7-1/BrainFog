using System.Reflection;
using Xunit;

namespace BlindSpire.Tests;

/// <summary>
/// Audits that the game assembly still contains every symbol BlindSpire patches.
/// Add new patch targets to <see cref="PatchTargets"/>; the test verifies them all.
/// Target game: STS2 v0.111.0.
/// </summary>
public class PatchTargetAuditTests
{
    private static readonly string Sts2DataDir =
        Environment.GetEnvironmentVariable("STS2_DATA_DIR")
        ?? "/mnt/d/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64";

    private enum Kind
    {
        Field,
        Property,
        Method,
        StaticMethod,
    }

    private sealed record MemberTarget(string Name, Kind Kind);

    private static readonly Dictionary<string, MemberTarget[]> PatchTargets = new()
    {
        ["MegaCrit.Sts2.Core.Nodes.Cards.NCard"] = new[]
        {
            new MemberTarget("_portrait", Kind.Field),
            new MemberTarget("_frame", Kind.Field),
            new MemberTarget("_titleLabel", Kind.Field),
            new MemberTarget("_descriptionLabel", Kind.Field),
            new MemberTarget("_energyLabel", Kind.Field),
            new MemberTarget("_typeLabel", Kind.Field),
            new MemberTarget("_starLabel", Kind.Field),
            new MemberTarget("_enchantmentLabel", Kind.Field),
            new MemberTarget("_enchantmentTab", Kind.Field),
            new MemberTarget("_ancientPortrait", Kind.Field),
            new MemberTarget("_energyIcon", Kind.Field),
            new MemberTarget("_starIcon", Kind.Field),
            new MemberTarget("_unplayableEnergyIcon", Kind.Field),
            new MemberTarget("_unplayableStarIcon", Kind.Field),
            new MemberTarget("_typePlaque", Kind.Field),
            new MemberTarget("_enchantmentIcon", Kind.Field),
            new MemberTarget("CardHighlight", Kind.Property),
            new MemberTarget("Reload", Kind.Method),
            new MemberTarget("UpdateVisuals", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Models.CardModel"] = new[]
        {
            new MemberTarget("OnPlayWrapper", Kind.Method),
            new MemberTarget("ToSerializable", Kind.Method),
            new MemberTarget("FromSerializable", Kind.StaticMethod),
            new MemberTarget("set_CurrentUpgradeLevel", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Saves.Runs.SerializableCard"] = new[]
        {
            new MemberTarget("Props", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar"] = new[]
        {
            new MemberTarget("Hp", Kind.Property),
            new MemberTarget("Gold", Kind.Property),
        },
        ["MegaCrit.sts2.Core.Nodes.TopBar.NTopBarHp"] = new[]
        {
            new MemberTarget("Initialize", Kind.Method),
            new MemberTarget("UpdateHealth", Kind.Method),
            new MemberTarget("_player", Kind.Field),
            new MemberTarget("_hpLabel", Kind.Field),
        },
        ["MegaCrit.sts2.Core.Nodes.TopBar.NTopBarGold"] = new[]
        {
            new MemberTarget("Initialize", Kind.Method),
            new MemberTarget("UpdateGold", Kind.Method),
            new MemberTarget("_player", Kind.Field),
            new MemberTarget("_goldLabel", Kind.Field),
            new MemberTarget("_currentGold", Kind.Field),
        },
        ["MegaCrit.Sts2.Core.Entities.RestSite.HealRestSiteOption"] = new[]
        {
            new MemberTarget("OnSelect", Kind.Method),
            new MemberTarget("Owner", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.Events.NEventLayout"] = new[]
        {
            new MemberTarget("SetTitle", Kind.Method),
            new MemberTarget("SetDescription", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Nodes.Events.NEventOptionButton"] = new[]
        {
            new MemberTarget("_Ready", Kind.Method),
            new MemberTarget("RefreshVotes", Kind.Method),
            new MemberTarget("FlashConfirmation", Kind.Method),
            new MemberTarget("_label", Kind.Field),
        },
        ["MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardRewardSelectionScreen"] = Array.Empty<MemberTarget>(),
        ["MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory"] = Array.Empty<MemberTarget>(),
        ["MegaCrit.Sts2.Core.Nodes.Potions.NPotion"] = new[]
        {
            new MemberTarget("Reload", Kind.Method),
            new MemberTarget("Image", Kind.Property),
            new MemberTarget("Outline", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.Relics.NRelic"] = new[]
        {
            new MemberTarget("Reload", Kind.Method),
            new MemberTarget("Icon", Kind.Property),
            new MemberTarget("Outline", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.Relics.NRelicBasicHolder"] = new[]
        {
            new MemberTarget("OnFocus", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Nodes.Relics.NRelicInventoryHolder"] = new[]
        {
            new MemberTarget("OnFocus", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Rewards.RelicReward"] = new[]
        {
            new MemberTarget("CreateIcon", Kind.Method),
            new MemberTarget("ExtraHoverTips", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Rewards.SpecialCardReward"] = new[]
        {
            new MemberTarget("Description", Kind.Property),
            new MemberTarget("ExtraHoverTips", Kind.Property),
            new MemberTarget("_card", Kind.Field),
        },
        ["MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens.NInspectRelicScreen"] = new[]
        {
            new MemberTarget("UpdateRelicDisplay", Kind.Method),
            new MemberTarget("_relicImage", Kind.Field),
            new MemberTarget("_nameLabel", Kind.Field),
            new MemberTarget("_description", Kind.Field),
            new MemberTarget("_flavor", Kind.Field),
            new MemberTarget("_rarityLabel", Kind.Field),
        },
        ["MegaCrit.Sts2.Core.Nodes.Cards.NCardGrid"] = new[]
        {
            new MemberTarget("SetCards", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Nodes.Screens.NDeckViewScreen"] = Array.Empty<MemberTarget>(),
        ["MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen"] = new[]
        {
            new MemberTarget("Open", Kind.Method),
            new MemberTarget("RecalculateTravelability", Kind.Method),
            new MemberTarget("ProcessMouseDrawingEvent", Kind.Method),
            new MemberTarget("OnDrawingToolsHotkeyPressed", Kind.Method),
            new MemberTarget("OnMapDrawingButtonPressed", Kind.Method),
            new MemberTarget("OnMapErasingButtonPressed", Kind.Method),
            new MemberTarget("_mapPointDictionary", Kind.Field),
            new MemberTarget("_paths", Kind.Field),
            new MemberTarget("_drawingTools", Kind.Field),
            new MemberTarget("_bossPointNode", Kind.Field),
            new MemberTarget("_secondBossPointNode", Kind.Field),
            new MemberTarget("_startingPointNode", Kind.Field),
        },
        ["MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint"] = new[]
        {
            new MemberTarget("State", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom"] = new[]
        {
            new MemberTarget("_Ready", Kind.Method),
            new MemberTarget("CreatureNodes", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.Combat.NCreatureVisuals"] = new[]
        {
            new MemberTarget("UpdatePhobiaMode", Kind.Method),
            new MemberTarget("SetUpSkin", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Map.MapCoord"] = Array.Empty<MemberTarget>(),
        ["MegaCrit.Sts2.Core.Map.MapPointState"] = Array.Empty<MemberTarget>(),
        ["MegaCrit.Sts2.Core.Nodes.Cards.Holders.NCardHolder"] = new[]
        {
            new MemberTarget("CreateHoverTips", Kind.Method),
            new MemberTarget("CardNode", Kind.Property),
        },
        ["MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder"] = new[]
        {
            new MemberTarget("UpdateCard", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Nodes.Combat.NCreature"] = new[]
        {
            new MemberTarget("Body", Kind.Property),
            new MemberTarget("Visuals", Kind.Property),
            new MemberTarget("Entity", Kind.Property),
            new MemberTarget("_Ready", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Nodes.Combat.NIntent"] = new[]
        {
            new MemberTarget("UpdateIntent", Kind.Method),
        },
        ["MegaCrit.Sts2.Core.Runs.RunManager"] = new[]
        {
            new MemberTarget("InitializeRunLobby", Kind.Method),
        },
    };

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

    [Fact]
    public void GameAssembly_IsPresent()
    {
        Assert.True(File.Exists(Path.Combine(Sts2DataDir, "sts2.dll")),
            $"sts2.dll not found under {Sts2DataDir}");
    }

    [Fact]
    public void AllPatchTargets_Exist()
    {
        using var ctx = CreateContext();
        var sts2 = ctx.LoadFromAssemblyName(new AssemblyName("sts2"));
        var failures = new List<string>();

        foreach (var (typeName, members) in PatchTargets)
        {
            var type = sts2.GetType(typeName);
            if (type == null)
            {
                failures.Add($"type {typeName}");
                continue;
            }

            foreach (var member in members)
            {
                var flags = BindingFlags.Public | BindingFlags.NonPublic
                    | (member.Kind == Kind.StaticMethod ? BindingFlags.Static : BindingFlags.Instance);
                var exists = member.Kind switch
                {
                    Kind.Field => type.GetField(member.Name, flags) != null,
                    Kind.Property => type.GetProperty(member.Name, flags) != null,
                    _ => type.GetMethod(member.Name, flags) != null,
                };
                if (!exists)
                {
                    failures.Add($"{typeName}.{member.Name}");
                }
            }
        }

        Assert.True(failures.Count == 0, "Missing patch targets: " + string.Join(", ", failures));
    }
}
