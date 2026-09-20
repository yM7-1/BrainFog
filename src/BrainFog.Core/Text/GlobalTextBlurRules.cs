namespace BrainFog.Core.Text;

/// <summary>
/// Catch-all policy for text that has no dedicated patch: everything that is
/// not deliberately exempt is garbled at the unified ratio (user change
/// 2026-09-21). Pure classification over the label's ancestor type names
/// (nearest first), so it is unit-testable without Godot.
///
/// Exemptions: enemy intents (numbers and tooltips stay readable), the settings
/// screen, the card compendium, the mod's own UI, and contexts with a dedicated
/// patch (cards, hover tips, menus, dialogue). Combat number VFX (damage / heal
/// / blocked) are garbled; all other VFX text stays readable. The top-bar HP and
/// gold value labels are exempt while the panel's "readable status numbers"
/// option is on (see <see cref="IsStatusNumberLabel"/>).
/// </summary>
public static class GlobalTextBlurRules
{
    private static readonly string[] VfxMarkers =
    {
        "Vfx", "Particles", "Trail", "Spark", "Glow", "Smoke", "Flipbook",
    };

    private static readonly string[] OwnedContexts =
    {
        // Owned by dedicated patches or deliberately readable.
        "NCard", "NHoverTipSet", "NCardLibrary",
        "NIntent",
        "NMainMenuTextButton", "NPauseMenuButton",
        "NMerchantDialogue", "NAncientDialogueLine",
    };

    private static readonly string[] CombatNumberVfx =
    {
        "NDamageNumVfx", "NHealNumVfx", "NDamageBlockedVfx",
    };

    private const string TopBarHpContext = "NTopBarHp";
    private const string TopBarGoldContext = "NTopBarGold";

    /// <summary>True when this label is covered by the catch-all blur rule.</summary>
    public static bool ShouldBlur(IReadOnlyList<string> ancestorTypeNames, bool modOwned)
    {
        if (modOwned)
        {
            return false;
        }

        foreach (var typeName in ancestorTypeNames)
        {
            if (OwnedContexts.Contains(typeName, StringComparer.Ordinal))
            {
                return false;
            }
            if (CombatNumberVfx.Contains(typeName, StringComparer.Ordinal))
            {
                return true;
            }
            if (typeName.Contains("Settings", StringComparison.Ordinal))
            {
                return false;
            }
            if (IsVfxMarker(typeName))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when the label name looks like a HP/gold value label
    /// ("HpLabel" / "GoldLabel"); cheap gate for the readable-numbers option.</summary>
    public static bool IsStatusNumberName(string? labelName) =>
        labelName is { Length: > 0 }
        && (labelName.Contains("HpLabel", StringComparison.Ordinal)
            || labelName.Contains("GoldLabel", StringComparison.Ordinal));

    /// <summary>True when the label is one of the top-bar HP/gold value labels
    /// covered by the "readable status numbers" option (2026-09-21). The local
    /// player's combat health-bar number is decided by the caller
    /// (<paramref name="localPlayerHealthBar"/>) because it needs the creature.</summary>
    public static bool IsStatusNumberLabel(
        IReadOnlyList<string> ancestorTypeNames,
        string? labelName,
        bool localPlayerHealthBar)
    {
        if (!IsStatusNumberName(labelName))
        {
            return false;
        }
        var hp = labelName!.Contains("HpLabel", StringComparison.Ordinal);
        if (hp && localPlayerHealthBar)
        {
            return true;
        }
        foreach (var typeName in ancestorTypeNames)
        {
            if (hp && typeName == TopBarHpContext)
            {
                return true;
            }
            if (!hp && typeName == TopBarGoldContext)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsVfxMarker(string typeName)
    {
        foreach (var marker in VfxMarkers)
        {
            if (typeName.Contains(marker, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}
