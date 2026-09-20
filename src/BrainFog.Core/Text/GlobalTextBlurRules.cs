namespace BrainFog.Core.Text;

/// <summary>
/// Catch-all blur policy for text that has no dedicated rule (user rule
/// 2026-09-19: unspecified text defaults to 60%). Pure classification over the
/// label's ancestor type names (nearest first), so it is unit-testable without
/// Godot.
///
/// Contexts with their own patch (cards, hover tips, events, menus, dialogue,
/// intents, settings, compendium, mod-owned UI) return null = leave the text to
/// that patch. Top bar / map descriptions use 70%. Combat number VFX (damage /
/// heal / blocked) are garbled like everything else (user change 2026-09-21);
/// all other VFX text stays readable.
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
        "NEventLayout", "NEventOptionButton",
        "NCard", "NHoverTipSet", "NCardLibrary",
        "NIntent",
        "NMainMenuTextButton", "NPauseMenuButton",
        "NMerchantDialogue", "NAncientDialogueLine",
    };

    private static readonly string[] CombatNumberVfx =
    {
        "NDamageNumVfx", "NHealNumVfx", "NDamageBlockedVfx",
    };

    /// <summary>Percent to blur at, or null when this label is handled elsewhere.</summary>
    public static int? ResolvePercent(
        IReadOnlyList<string> ancestorTypeNames,
        bool modOwned,
        bool topBarValueLabel)
    {
        if (modOwned || topBarValueLabel)
        {
            return null;
        }

        foreach (var typeName in ancestorTypeNames)
        {
            if (OwnedContexts.Contains(typeName, StringComparer.Ordinal))
            {
                return null;
            }
            if (CombatNumberVfx.Contains(typeName, StringComparer.Ordinal))
            {
                return TextBlurPercents.Default;
            }
            if (typeName.Contains("Settings", StringComparison.Ordinal))
            {
                return null;
            }
            if (IsVfxMarker(typeName))
            {
                return null;
            }
            if (typeName.StartsWith("NTopBar", StringComparison.Ordinal)
                || typeName.StartsWith("NMap", StringComparison.Ordinal))
            {
                return TextBlurPercents.UiDescription;
            }
        }

        return TextBlurPercents.Default;
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
