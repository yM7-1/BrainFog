using BrainFog.Core.Options;
using BrainFog.Core.Reveal;
using BrainFog.Core.Text;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
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
    private const string DimNodeName = "BrainFogDim";
    private const string HiddenPartMeta = "BrainFogHiddenPart";
    private const string PlusNodeName = "BrainFogPlusMarker";
    private const string RuleMeta = "BrainFogRule";
    private const string ContextMeta = "BrainFogContext";
    private const string ContextParentMeta = "BrainFogContextParent";

    public static CardVisualRule ResolveRule(NCard card) =>
        PatchGuard.RunOr("CardFog.Resolve", () => ResolveRuleCore(card), CardVisualRule.FullFace);

    private static CardVisualRule ResolveRuleCore(NCard card)
    {
        if (ModRuntime.Disabled || card.Model is not CardModel model)
        {
            return CardVisualRule.FullFace;
        }

        var context = ResolveContextCached(card, model);
        var settings = DifficultyRuntime.Current;
        var knowledge = settings.MemoryMode switch
        {
            CardMemoryMode.Omniscient => CardKnowledge.Revealed,
            CardMemoryMode.Nonsense => CardKnowledge.Unknown,
            // "Bad memory" tracks copies; "good memory" the whole definition.
            CardMemoryMode.BadMemory => ModRuntime.Tracker.GetInstanceKnowledge(
                CardInstanceRegistry.TryGetId(model)),
            _ => ModRuntime.Tracker.GetDefinitionKnowledge(RevealKeys.Of(model)),
        };
        var slotRevealed = context == CardDisplayContext.Reward && IsRewardSlotRevealed(card, settings);
        return RevealRules.Resolve(context, knowledge, settings, slotRevealed);
    }

    /// <summary>
    /// Context depends only on the ancestor chain; cache it per card and
    /// recompute only when the card is re-parented (pooled cards move screens).
    /// Perf: avoids a 16-node walk on every UpdateVisuals (card-play hitch fix).
    /// </summary>
    private static CardDisplayContext ResolveContextCached(NCard card, CardModel model)
    {
        var parentId = (long)(card.GetParent()?.GetInstanceId() ?? 0UL);
        if (card.HasMeta(ContextMeta) && card.GetMeta(ContextParentMeta).AsInt64() == parentId)
        {
            return (CardDisplayContext)card.GetMeta(ContextMeta).AsInt32();
        }

        var context = ResolveContext(card, model);
        card.SetMeta(ContextMeta, (int)context);
        card.SetMeta(ContextParentMeta, parentId);
        return context;
    }

    /// <summary>Random (but stable per offer) reward-slot reveal pick.</summary>
    private static bool IsRewardSlotRevealed(NCard card, DifficultySettings settings)
    {
        if (settings.SelectionReveal == SelectionRevealOption.None)
        {
            return false;
        }

        var holder = FindHolder(card);
        var row = holder?.GetParent();
        if (holder == null || row == null)
        {
            // Single-card presentation (e.g. inspect view): only "all" reveals.
            return settings.SelectionReveal == SelectionRevealOption.All;
        }

        var keys = new List<string>();
        var myIndex = -1;
        foreach (var sibling in row.GetChildren())
        {
            if (sibling is not NCardHolder siblingHolder)
            {
                continue;
            }
            if (ReferenceEquals(siblingHolder, holder))
            {
                myIndex = keys.Count;
            }
            keys.Add(siblingHolder.CardNode?.Model?.Id.ToString() ?? "?");
        }
        return SelectionRevealPlanner.IsRevealed(
            keys, settings.SelectionReveal, DifficultyRuntime.RevealSalt, myIndex);
    }

    private static NCardHolder? FindHolder(NCard card)
    {
        for (var node = card.GetParent(); node != null; node = node.GetParent())
        {
            if (node is NCardHolder holder)
            {
                return holder;
            }
        }
        return null;
    }

    public static void Apply(NCard card) =>
        PatchGuard.Run("CardFog.Apply", () => ApplyCore(card));

    /// <summary>Re-applies the rule to every live NCard of this card definition
    /// (reveal covers all copies and can happen while a copy is on screen).</summary>
    public static void RefreshLiveCards(CardModel model) =>
        PatchGuard.Run("CardFog.RefreshLive", () => RefreshLiveCardsCore(model));

    private static void RefreshLiveCardsCore(CardModel model)
    {
        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
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

    /// <summary>Re-applies the rule to every live card (difficulty option changed).</summary>
    public static void RefreshAllLiveCards() =>
        PatchGuard.Run("CardFog.RefreshAll", () => RefreshAllLiveCardsCore());

    /// <summary>Re-blurs face text of live cards that never stored an original
    /// (created while the mod was off; kill-switch re-enable, 0.3.9).</summary>
    public static void ReapplyTextOnAllLiveCards() =>
        PatchGuard.Run("CardFog.ReapplyTextAll", () =>
        {
            if (ModRuntime.Disabled || Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            {
                return;
            }

            var labels = 0;
            foreach (var node in tree.GetNodesInGroup(Patches.NCardGroupPatch.GroupName))
            {
                if (node is NCard card)
                {
                    labels += Patches.CardFaceTextBlur.ReapplyCardText(card);
                }
            }
            if (labels > 0)
            {
                MegaCrit.Sts2.Core.Logging.Log.Info(
                    $"[BrainFog][CardText] re-blurred {labels} label(s) on live cards");
            }
        });

    private static void RefreshAllLiveCardsCore()
    {
        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
        {
            return;
        }

        foreach (var node in tree.GetNodesInGroup(Patches.NCardGroupPatch.GroupName))
        {
            if (node is NCard card)
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
            if (card.GetNodeOrNull<ColorRect>(DimNodeName) is { Visible: true } leftoverDim)
            {
                leftoverDim.Visible = false;
            }
            card.RemoveMeta(RuleMeta);
            return;
        }

        var rule = ResolveRule(card);
        UpdateDimOverlay(card, rule, card.Model);
        var previous = card.HasMeta(RuleMeta) ? (CardVisualRule)card.GetMeta(RuleMeta).AsInt32() : (CardVisualRule?)null;
        if (previous == rule)
        {
            // Same rule: the game may have re-shown face parts (UpdateVisuals).
            // Card text is blurred at the source (CardFaceTextBlurPatch), so no
            // text re-work is needed here (perf 2026-09-20).
            if (rule == CardVisualRule.FullFace)
            {
                return;
            }
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
            // Text blur happens at the source (CardFaceTextBlurPatch); the game
            // re-renders the labels right after this, and the prefix blurs them.
        }
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
            if (node.HasMeta(CardAcquireScope.Meta))
            {
                // Acquisition screen (event/generated card choices): rarity only.
                forceAcquisition = true;
            }
            else if (node is NInspectCardScreen inspect)
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

    /// <summary>
    /// "About to be forgotten" hint (user change 2026-09-21): in bad-memory
    /// mode, a revealed hand copy that would revert to unknown if it is not
    /// played gets a dark overlay on its face. The node is only created when
    /// the hint is needed.
    /// </summary>
    private static void UpdateDimOverlay(NCard card, CardVisualRule rule, CardModel model)
    {
        var show = rule == CardVisualRule.FullFace
            && !ModRuntime.Disabled
            && Game.BadMemoryTracker.ShouldDim(model);
        var dim = card.GetNodeOrNull<ColorRect>(DimNodeName);
        if (!show)
        {
            if (dim != null && dim.Visible)
            {
                dim.Visible = false;
            }
            return;
        }

        dim ??= CreateDim(card);
        dim.Visible = true;
        var last = card.GetChildCount() - 1;
        if (last >= 0 && dim.GetIndex() != last)
        {
            card.MoveChild(dim, last); // keep the hint above every face part
        }
    }

    private static ColorRect CreateDim(NCard card)
    {
        var dim = new ColorRect
        {
            Name = DimNodeName,
            Color = BrainFogTuning.CardDimColor,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        card.AddChild(dim);
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return dim;
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
