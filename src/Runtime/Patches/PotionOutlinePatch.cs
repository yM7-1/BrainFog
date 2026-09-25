using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace BrainFog.Patches;

/// <summary>Potions show only the outline (spec 0.01 4.3 / 0.02 #9).
/// Mod off restores the potion art (0.3.7).</summary>
[HarmonyPatch(typeof(NPotion), "Reload")]
internal static class PotionOutlinePatch
{
    [HarmonyPostfix]
    private static void Postfix(NPotion __instance) => PotionOutlines.Apply(__instance);
}

internal static class PotionOutlines
{
    /// <summary>Marks the image this mod hid (mod-off sweep restores it).</summary>
    public const string HiddenMeta = "BrainFogPotionImageHidden";

    public static void Apply(NPotion potion)
    {
        if (!GodotObject.IsInstanceValid(potion) || potion.Image is not { } image
            || !GodotObject.IsInstanceValid(image))
        {
            return;
        }

        if (ModRuntime.Disabled)
        {
            if (image.HasMeta(HiddenMeta))
            {
                image.Visible = true;
                image.RemoveMeta(HiddenMeta);
            }
            return;
        }

        if (image.Visible)
        {
            image.SetMeta(HiddenMeta, true);
            image.Visible = false;
        }
    }
}
