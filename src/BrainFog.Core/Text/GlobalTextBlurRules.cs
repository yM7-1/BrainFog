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
/// / blocked) are garbled; all other VFX text stays readable.
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
