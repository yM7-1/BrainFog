using BrainFog.Core.Options;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BrainFog.Patches;

/// <summary>
/// Upgrades (smith, events, relics) reveal the true face even if never played
/// (spec 0.03 a), following the same memory mode as play reveals (user change
/// 2026-09-21): whole definition in "good memory", only that copy in "bad
/// memory", nothing in "omniscient"/"nonsense".
/// Preview clones and save deserialization upgrades (no pile) never count.
/// Repeated upgrades are a cheap no-op; visuals refresh deferred one frame.
/// </summary>
[HarmonyPatch(typeof(CardModel), "set_CurrentUpgradeLevel")]
internal static class CardModelUpgradeRevealPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance)
    {
        if (ModRuntime.Disabled || __instance.CurrentUpgradeLevel <= 0)
        {
            return;
        }

        if (__instance.Pile == null)
        {
            return; // preview clone / not part of the run deck
        }

        if (!RevealCore(__instance))
        {
            return;
        }

        var model = __instance;
        Godot.Callable.From(() => Game.CardFogRenderer.RefreshLiveCards(model)).CallDeferred();
    }

    /// <summary>Returns true when new knowledge was recorded.</summary>
    private static bool RevealCore(CardModel card)
    {
        switch (Game.DifficultyRuntime.Current.MemoryMode)
        {
            case CardMemoryMode.Omniscient:
            case CardMemoryMode.Nonsense:
                return false;
            case CardMemoryMode.BadMemory:
            {
                var id = Game.CardInstanceRegistry.GetOrCreateId(card);
                if (string.IsNullOrEmpty(id) || !ModRuntime.Tracker.RevealInstanceByUpgrade(id))
                {
                    return false;
                }
                Game.RevealPersistence.OnInstanceRevealed(card, id);
                return true;
            }
            default:
            {
                var key = Game.RevealKeys.Of(card);
                if (!ModRuntime.Tracker.RevealByUpgrade(key))
                {
                    return false;
                }
                Game.RevealPersistence.OnRevealed(key);
                return true;
            }
        }
    }
}
