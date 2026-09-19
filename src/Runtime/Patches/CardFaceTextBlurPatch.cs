using BrainFog.Core.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace BrainFog.Patches;

/// <summary>
/// Blurs revealed card-face text at the source (perf 2026-09-20).
///
/// The game's MegaLabel/MegaRichTextLabel only re-run the expensive BBCode
/// parse + font auto-size when the assigned string differs from the current
/// one. Blurring *after* the game wrote the real text (old approach) made every
/// UpdateVisuals see "real != blurred" and re-shape the text, then our post-pass
/// re-blurred it and re-shaped again — twice the text work on every card play,
/// which caused the audible/visual hitch. Blurring the incoming string in a
/// prefix keeps the label stable across repeated UpdateVisuals calls.
/// The compendium stays readable (0.03 j).
/// </summary>
[HarmonyPatch(typeof(MegaLabel), "SetTextAutoSize")]
internal static class CardLabelTextBlurPatch
{
    [HarmonyPrefix]
    private static void Prefix(MegaLabel __instance, ref string text) =>
        CardFaceTextBlur.Apply(__instance, ref text);
}

[HarmonyPatch(typeof(MegaRichTextLabel), "SetTextAutoSize")]
internal static class CardRichTextBlurPatch
{
    [HarmonyPrefix]
    private static void Prefix(MegaRichTextLabel __instance, ref string text) =>
        CardFaceTextBlur.Apply(__instance, ref text);
}

internal static class CardFaceTextBlur
{
    /// <summary>Positive cache only: a card that IS in the compendium stays
    /// there (compendium grids do not hand cards to other screens). Negative
    /// results are re-checked because pooled cards move between screens.</summary>
    private const string CompendiumMeta = "BrainFogCardBlurCompendium";

    /// <summary>Last input string, paired with TextBlurService.OutputMeta.
    /// Repeated UpdateVisuals passes with identical text skip the hash/RNG walk.</summary>
    private const string InputMeta = "BrainFogCardBlurInput";

    public static void Apply(CanvasItem label, ref string text)
    {
        try
        {
            if (ModRuntime.Disabled || string.IsNullOrEmpty(text))
            {
                return;
            }

            var card = FindCard(label);
            if (card == null || IsCompendiumCard(card))
            {
                return;
            }

            if (label.HasMeta(InputMeta)
                && label.GetMeta(InputMeta).AsString() == text
                && label.HasMeta(Game.TextBlurService.OutputMeta))
            {
                // Same input as last time: reuse the memoized output so the
                // label text stays byte-identical and the game skips re-shaping.
                text = label.GetMeta(Game.TextBlurService.OutputMeta).AsString();
                return;
            }

            var blurred = EventTextBlurrer.Blur(text, TextBlurPercents.CardFaceText, BlurSalt.Current);
            label.SetMeta(InputMeta, text);
            Game.TextBlurService.MarkOutput(label, blurred);
            if (!string.Equals(blurred, text, StringComparison.Ordinal))
            {
                text = blurred;
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("CardFaceBlur.Prefix", () => throw ex);
        }
    }

    /// <summary>Nearest NCard ancestor (at most 3 hops in card.tscn).</summary>
    private static NCard? FindCard(CanvasItem label)
    {
        var parent = label.GetParent();
        for (var i = 0; i < 4 && parent != null; i++, parent = parent.GetParent())
        {
            if (parent is NCard card)
            {
                return card;
            }
        }
        return null;
    }

    private static bool IsCompendiumCard(NCard card)
    {
        if (card.HasMeta(CompendiumMeta))
        {
            return true; // positive result: never leaves the compendium
        }
        for (var node = card.GetParent(); node != null; node = node.GetParent())
        {
            if (node is NCardLibrary)
            {
                card.SetMeta(CompendiumMeta, true);
                return true;
            }
        }
        return false;
    }
}
