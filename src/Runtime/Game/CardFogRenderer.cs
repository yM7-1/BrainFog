using BrainFog.Core.Reveal;
using BrainFog.Core.Text;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BrainFog.Game;

/// <summary>
/// Applies the black-fog rule to an NCard: unknown instances show only the frame,
/// the interior is a black rectangle (spec 0.02 #1/#2, 0.03 b).
/// Acquisition contexts additionally show the "+" upgrade marker (spec 0.03 h).
/// </summary>
internal static class CardFogRenderer
{
    private const string FogNodeName = "BrainFogFog";
    private const string HiddenPartMeta = "BrainFogHiddenPart";
    private const string PlusNodeName = "BrainFogPlusMarker";
    private const string RuleMeta = "BrainFogRule";
    private const string BlurredTextMeta = "BrainFogBlurredText";

    public static CardVisualRule ResolveRule(NCard card) =>
        PatchGuard.RunOr("CardFog.Resolve", () => ResolveRuleCore(card), CardVisualRule.FullFace);

    private static CardVisualRule ResolveRuleCore(NCard card)
    {
        if (ModRuntime.Disabled || card.Model is not CardModel model)
        {
            return CardVisualRule.FullFace;
        }

        var context = ResolveContext(card, model);
        var knowledge = ModRuntime.Tracker.GetKnowledge(RevealKeys.Of(model));
        return RevealRules.Resolve(context, knowledge);
    }

    public static void Apply(NCard card) =>
        PatchGuard.Run("CardFog.Apply", () => ApplyCore(card));

    /// <summary>Re-applies the rule to every live NCard of this card definition
    /// (reveal covers all copies and can happen while a copy is on screen).</summary>
    public static void RefreshLiveCards(CardModel model) =>
        PatchGuard.Run("CardFog.RefreshLive", () => RefreshLiveCardsCore(model));

    private static void RefreshLiveCardsCore(CardModel model)
    {
        if (ModRuntime.Disabled || Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
        {
            return;
        }

        foreach (var node in tree.GetNodesInGroup(Patches.NCardGroupPatch.GroupName))
        {
            if (node is NCard card && card.Model is { } candidate && candidate.Id == model.Id)
            {
                ApplyCore(card);
            }
        }
    }

    private static void ApplyCore(NCard card)
    {
        if (!GodotObject.IsInstanceValid(card) || !card.IsNodeReady())
        {
            return;
        }

        var fog = GetOrCreateFog(card);
        if (card.Model == null)
        {
            // Pooled card without a model: clear any leftover state.
            RestoreFaceParts(card);
            fog.Visible = false;
            card.RemoveMeta(RuleMeta);
            return;
        }

        var rule = ResolveRule(card);
        var previous = card.HasMeta(RuleMeta) ? (CardVisualRule)card.GetMeta(RuleMeta).AsInt32() : (CardVisualRule?)null;
        if (previous == rule && rule != CardVisualRule.FullFace)
        {
            // Same rule: the game may have re-shown parts (UpdateVisuals); enforce cheaply.
            HideVisibleFaceParts(card);
            EnsureFogPlacement(card, fog);
            return;
        }

        card.SetMeta(RuleMeta, (int)rule);
        if (rule != CardVisualRule.FullFace)
        {
            HideFaceParts(card);
            if (rule == CardVisualRule.RarityOnly)
            {
                // Acquisition shows rarity only: hide the type-encoding frame/border.
                HidePart(card._frame);
                HidePart(card._portraitBorder);
            }
            EnsureFogPlacement(card, fog);
            fog.Visible = true;
            var upgraded = rule == CardVisualRule.RarityOnly
                && RevealRules.ShowsUpgradeMarker(card.Model?.IsUpgraded == true);
            UpdatePlusMarker(fog, upgraded);
        }
        else
        {
            RestoreFaceParts(card);
            fog.Visible = false;
            BlurFaceText(card);
        }
    }

    /// <summary>Revealed cards keep their art but every text on the face is
    /// garbled at 85% (user change 2026-09-19). The compendium stays clean.</summary>
    private static void BlurFaceText(NCard card)
    {
        if (IsCompendiumCard(card))
        {
            return;
        }
        foreach (var label in TextFaceParts(card))
        {
            BlurText(label);
        }
    }

    private static bool IsCompendiumCard(NCard card)
    {
        for (var node = card.GetParent(); node != null; node = node.GetParent())
        {
            if (node is NCardLibrary)
            {
                return true;
            }
        }
        return false;
    }

    private static void BlurText(CanvasItem? part)
    {
        if (part == null || !GodotObject.IsInstanceValid(part))
        {
            return;
        }

        var current = part switch
        {
            RichTextLabel rich => rich.Text,
            Label plain => plain.Text,
            _ => null,
        };
        if (string.IsNullOrEmpty(current))
        {
            return;
        }
        if (part.HasMeta(BlurredTextMeta) && part.GetMeta(BlurredTextMeta).AsString() == current)
        {
            return;
        }

        var blurred = EventTextBlurrer.Blur(current, TextBlurPercents.CardFaceText);
        part.SetMeta(BlurredTextMeta, blurred);
        switch (part)
        {
            case RichTextLabel rich:
                rich.Text = blurred;
                break;
            case Label plain:
                plain.Text = blurred;
                break;
        }
    }

    private static IEnumerable<CanvasItem?> TextFaceParts(NCard card)
    {
        yield return card._titleLabel;
        yield return card._descriptionLabel;
        yield return card._typeLabel;
        yield return card._energyLabel;
        yield return card._starLabel;
        yield return card._enchantmentLabel;
    }

    private static void EnsureFogPlacement(NCard card, ColorRect fog)
    {
        if (!GodotObject.IsInstanceValid(fog) || !fog.Visible)
        {
            if (GodotObject.IsInstanceValid(fog))
            {
                fog.Visible = true;
            }
        }
        var frameIndex = Mathf.Clamp(card._frame?.GetIndex() ?? 0, 0, Math.Max(0, card.GetChildCount() - 1));
        if (fog.GetIndex() != frameIndex)
        {
            card.MoveChild(fog, frameIndex);
        }
    }

    private static CardDisplayContext ResolveContext(NCard card, CardModel model)
    {
        var names = new List<string>();
        var ownedView = false;
        var forceAcquisition = false;
        var node = card.GetParent();
        while (node != null && names.Count < 16)
        {
            names.Add(node.GetType().Name);
            if (node is NInspectCardScreen inspect)
            {
                ownedView = true;
                forceAcquisition |= InspectSourceIsUnowned(inspect);
            }
            else if (node.GetType().Name == "NUpgradePreview")
            {
                ownedView = true;
            }
            node = node.GetParent();
        }
        return CardContextClassifier.Classify(names, model.Pile != null, ownedView, forceAcquisition);
    }

    /// <summary>The inspect screen can be opened from acquisition screens (card
    /// rewards); those source cards live in no pile and must stay masked.</summary>
    private static bool InspectSourceIsUnowned(NInspectCardScreen screen)
    {
        var cards = screen._cards;
        var index = screen._index;
        if (cards == null || index < 0 || index >= cards.Count)
        {
            return false;
        }
        return cards[index]?.Pile == null;
    }

    private static void UpdatePlusMarker(ColorRect fog, bool show)
    {
        var plus = fog.GetNodeOrNull<Label>(PlusNodeName);
        if (!show)
        {
            if (plus != null)
            {
                plus.Visible = false;
            }
            return;
        }

        if (plus == null)
        {
            plus = new Label
            {
                Name = PlusNodeName,
                Text = "+",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = Colors.White,
            };
            plus.ApplyLocaleFontSubstitution(FontType.Regular, "font");
            fog.AddChild(plus);
            plus.AnchorLeft = 1f;
            plus.AnchorRight = 1f;
            plus.OffsetLeft = -28f;
            plus.OffsetRight = -4f;
            plus.OffsetTop = 4f;
            plus.OffsetBottom = 32f;
        }
        plus.Visible = true;
    }

    private static ColorRect GetOrCreateFog(NCard card)
    {
        if (card.GetNodeOrNull<ColorRect>(FogNodeName) is { } existing)
        {
            return existing;
        }

        var fog = new ColorRect
        {
            Name = FogNodeName,
            Color = BrainFogTuning.CardFogColor,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        card.AddChild(fog);
        fog.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return fog;
    }

    private static void HideFaceParts(NCard card) => HideVisibleFaceParts(card);

    private static void HidePart(CanvasItem? part)
    {
        if (part != null && GodotObject.IsInstanceValid(part) && part.Visible)
        {
            part.SetMeta(HiddenPartMeta, true);
            part.Visible = false;
        }
    }

    private static void HideVisibleFaceParts(NCard card)
    {
        foreach (var part in FaceParts(card))
        {
            if (part != null && GodotObject.IsInstanceValid(part) && part.Visible)
            {
                part.SetMeta(HiddenPartMeta, true);
                part.Visible = false;
            }
        }
    }

    private static void RestoreFaceParts(NCard card)
    {
        foreach (var part in FaceParts(card))
        {
            if (part == null || !GodotObject.IsInstanceValid(part))
            {
                continue;
            }
            if (part.HasMeta(HiddenPartMeta))
            {
                part.Visible = true;
                part.RemoveMeta(HiddenPartMeta);
            }
        }
    }

    private static IEnumerable<CanvasItem?> FaceParts(NCard card)
    {
        // Frame/border are restored here too when RarityOnly hid them.
        yield return card._frame;
        yield return card._portraitBorder;
        yield return card._portrait;
        yield return card._ancientPortrait;
        yield return card._titleLabel;
        yield return card._descriptionLabel;
        yield return card._energyLabel;
        yield return card._energyIcon;
        yield return card._starLabel;
        yield return card._starIcon;
        yield return card._unplayableEnergyIcon;
        yield return card._unplayableStarIcon;
        yield return card._typeLabel;
        yield return card._typePlaque;
        yield return card._enchantmentTab;
        yield return card._enchantmentIcon;
        yield return card._enchantmentLabel;
    }
}
