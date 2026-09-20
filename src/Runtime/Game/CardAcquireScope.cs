using Godot;

namespace BrainFog.Game;

/// <summary>
/// Marks card-selection screens that offer cards to acquire (event card
/// choices, generated choices from potions/relics/cards). Cards rendered inside
/// a marked screen follow the acquisition rule (rarity border only), even when
/// their model carries a pile reference (2026-09-21, closes IMPLEMENTATION-MAP
/// §2.3). The mark lives on the screen node, so its lifetime needs no async
/// bookkeeping.
/// </summary>
internal static class CardAcquireScope
{
    public const string Meta = "BrainFogAcquireScreen";

    public static void Mark(Node? screen)
    {
        if (screen != null && GodotObject.IsInstanceValid(screen))
        {
            screen.SetMeta(Meta, true);
        }
    }
}
