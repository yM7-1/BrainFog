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

            if (label.HasMeta(Game.TextBlurService.InputMeta)
                && label.GetMeta(Game.TextBlurService.InputMeta).AsString() == text
                && label.HasMeta(Game.TextBlurService.OutputMeta))
            {
                // Same input as last time: reuse the memoized output so the
                // label text stays byte-identical and the game skips re-shaping.
                text = label.GetMeta(Game.TextBlurService.OutputMeta).AsString();
                return;
            }

            if (label.HasMeta(Game.TextBlurService.OutputMeta)
                && label.GetMeta(Game.TextBlurService.OutputMeta).AsString() == text)
            {
                return; // already our output (re-apply / repeated render)
            }

            var blurred = EventTextBlurrer.Blur(text, Game.DifficultyRuntime.TextBlurPercent, BlurSalt.Current);
            label.SetMeta(Game.TextBlurService.InputMeta, text);
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

    /// <summary>Re-blurs a live card's face text from its current text (0.3.9):
    /// labels created while the mod was off carry no stored original, so the
    /// metadata-based re-apply cannot see them and the kill-switch re-enable
    /// left readable card text behind. Labels that already have a stored
    /// original (blurred before or by the source patch) are skipped.
    /// Returns how many labels were re-fed through the blur.</summary>
    public static int ReapplyCardText(NCard card)
    {
        try
        {
            if (ModRuntime.Disabled || card == null || !GodotObject.IsInstanceValid(card)
                || IsCompendiumCard(card))
            {
                return 0;
            }

            var changed = 0;
            changed += ReapplyLabel(card._titleLabel) ? 1 : 0;
            changed += ReapplyLabel(card._descriptionLabel) ? 1 : 0;
            changed += ReapplyLabel(card._energyLabel) ? 1 : 0;
            changed += ReapplyLabel(card._starLabel) ? 1 : 0;
            changed += ReapplyLabel(card._typeLabel) ? 1 : 0;
            changed += ReapplyLabel(card._enchantmentLabel) ? 1 : 0;
            return changed;
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("CardFaceBlur.Reapply", () => throw ex);
            return 0;
        }
    }

    /// <summary>Re-feeds one label without a stored original through the patched
    /// setter so the source blur runs on its current text.</summary>
    private static bool ReapplyLabel(CanvasItem? label)
    {
        if (label == null || !GodotObject.IsInstanceValid(label) || !label.IsInsideTree()
            || label.HasMeta(Game.TextBlurService.InputMeta))
        {
            return false;
        }
        var text = Game.TextBlurService.Read(label);
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        switch (label)
        {
            case MegaLabel mega:
                mega.SetTextAutoSize(text);
                return true;
            case MegaRichTextLabel rich:
                rich.SetTextAutoSize(text);
                return true;
            default:
                return false;
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
