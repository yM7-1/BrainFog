using BlindSpire.Core.Reveal;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace BlindSpire.Game;

/// <summary>
/// Applies the black-fog rule to an NCard: unknown instances show only the frame,
/// the interior is a black rectangle (spec 0.02 #1/#2, 0.03 b).
/// </summary>
internal static class CardFogRenderer
{
    private const string FogNodeName = "BlindSpireFog";
    private const string HiddenMeta = "BlindSpireHiddenParts";

    public static bool ShouldFog(NCard card)
    {
        if (ModRuntime.Disabled || card.Model is not CardModel model)
        {
            return false;
        }

        var context = ResolveContext(card);
        var knowledge = ModRuntime.Tracker.GetKnowledge(CardInstanceRegistry.GetOrCreateId(model));
        return RevealRules.Resolve(context, knowledge) == CardVisualRule.BlackFog;
    }

    public static void Apply(NCard card)
    {
        if (!GodotObject.IsInstanceValid(card) || !card.IsNodeReady())
        {
            return;
        }

        var fog = GetOrCreateFog(card);
        if (ShouldFog(card))
        {
            HideFaceParts(card, fog);
            // Draw the fog where the frame sits, so the frame stays on top.
            var frameIndex = Mathf.Clamp(card._frame?.GetIndex() ?? 0, 0, Math.Max(0, card.GetChildCount() - 1));
            card.MoveChild(fog, frameIndex);
            fog.Visible = true;
        }
        else
        {
            RestoreFaceParts(card, fog);
            fog.Visible = false;
        }
    }

    private static CardDisplayContext ResolveContext(NCard card)
    {
        // The compendium is never fogged (spec 0.03 j).
        var node = card.GetParent();
        while (node != null)
        {
            if (node is NCardLibrary)
            {
                return CardDisplayContext.CardLibrary;
            }
            node = node.GetParent();
        }
        return CardDisplayContext.Other;
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
        var hidden = new Godot.Collections.Array();
        foreach (var part in FaceParts(card))
        {
            if (part != null && GodotObject.IsInstanceValid(part) && part.Visible)
            {
                part.Visible = false;
                hidden.Add(part.GetPath());
            }
        }
        fog.SetMeta(HiddenMeta, hidden);
    }

    private static void RestoreFaceParts(NCard card, ColorRect fog)
    {
        if (!fog.HasMeta(HiddenMeta))
        {
            return;
        }

        if (fog.GetMeta(HiddenMeta).AsGodotArray() is { } hidden)
        {
            foreach (var variant in hidden)
            {
                var path = variant.AsNodePath();
                if (card.GetNodeOrNull<CanvasItem>(path) is { } node)
                {
                    node.Visible = true;
                }
            }
        }
        fog.RemoveMeta(HiddenMeta);
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
