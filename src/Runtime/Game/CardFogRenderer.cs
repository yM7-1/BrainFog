using BlindSpire.Core.Reveal;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BlindSpire.Game;

/// <summary>
/// Applies the black-fog rule to an NCard: unknown instances show only the frame,
/// the interior is a black rectangle (spec 0.02 #1/#2, 0.03 b).
/// Acquisition contexts additionally show the "+" upgrade marker (spec 0.03 h).
/// </summary>
internal static class CardFogRenderer
{
    private const string FogNodeName = "BlindSpireFog";
    private const string HiddenPartMeta = "BlindSpireHiddenPart";
    private const string PlusNodeName = "BlindSpirePlusMarker";

    public static CardVisualRule ResolveRule(NCard card) =>
        PatchGuard.RunOr("CardFog.Resolve", () => ResolveRuleCore(card), CardVisualRule.FullFace);

    private static CardVisualRule ResolveRuleCore(NCard card)
    {
        if (ModRuntime.Disabled || card.Model is not CardModel model)
        {
            return CardVisualRule.FullFace;
        }

        var context = ResolveContext(card);
        var knowledge = ModRuntime.Tracker.GetKnowledge(CardInstanceRegistry.GetOrCreateId(model));
        return RevealRules.Resolve(context, knowledge);
    }

    public static void Apply(NCard card) =>
        PatchGuard.Run("CardFog.Apply", () => ApplyCore(card));

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
            return;
        }
        var rule = ResolveRule(card);
        var hide = rule != CardVisualRule.FullFace;
        if (hide)
        {
            HideFaceParts(card, fog);
            // Draw the fog where the frame sits, so the frame stays on top.
            var frameIndex = Mathf.Clamp(card._frame?.GetIndex() ?? 0, 0, Math.Max(0, card.GetChildCount() - 1));
            card.MoveChild(fog, frameIndex);
            fog.Visible = true;
            var upgraded = rule == CardVisualRule.RarityOnly
                && RevealRules.ShowsUpgradeMarker(card.Model?.IsUpgraded == true);
            UpdatePlusMarker(fog, upgraded);
        }
        else
        {
            RestoreFaceParts(card);
            fog.Visible = false;
        }
    }

    private static CardDisplayContext ResolveContext(NCard card)
    {
        var node = card.GetParent();
        while (node != null)
        {
            switch (node)
            {
                // The compendium is never fogged (spec 0.03 j).
                case NCardLibrary:
                    return CardDisplayContext.CardLibrary;
                // Acquisition screens: rarity border only (spec 0.02 #5, 0.03 h).
                case NCardRewardSelectionScreen:
                    return CardDisplayContext.Reward;
                case NMerchantInventory:
                    return CardDisplayContext.Shop;
                case NCardPileScreen:
                    return CardDisplayContext.PileView;
            }
            node = node.GetParent();
        }
        return CardDisplayContext.Other;
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
            Color = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        card.AddChild(fog);
        fog.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return fog;
    }

    private static void HideFaceParts(NCard card, ColorRect fog)
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
