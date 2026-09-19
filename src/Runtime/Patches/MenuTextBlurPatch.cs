using BrainFog.Core.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;

namespace BrainFog.Patches;

/// <summary>
/// Title-screen and pause-menu option text renders as a fixed 75% blur
/// (user change 2026-09-19). The Settings entry stays readable so the
/// settings screen remains reachable; the settings screen itself is untouched.
/// Text is re-derived from the localized source each refresh, so repeated
/// refreshes stay deterministic and never double-blur.
/// </summary>
[HarmonyPatch(typeof(NMainMenuTextButton), "RefreshLabel")]
internal static class MainMenuTextBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMainMenuTextButton __instance)
    {
        try
        {
            if (ModRuntime.Disabled || IsSettingsButton(__instance))
            {
                return;
            }
            Blur(__instance.label);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("MenuBlur.Main", () => throw ex);
        }
    }

    private static bool IsSettingsButton(NMainMenuTextButton button)
    {
        for (var node = button.GetParent(); node != null; node = node.GetParent())
        {
            if (node is NMainMenu menu && ReferenceEquals(menu._settingsButton, button))
            {
                return true;
            }
        }
        return false;
    }

    private static void Blur(Label? label) =>
        Game.TextBlurService.BlurNode(label, TextBlurPercents.Menu);
}

[HarmonyPatch(typeof(NPauseMenu), "RefreshLabels")]
internal static class PauseMenuTextBlurPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPauseMenu __instance)
    {
        try
        {
            if (ModRuntime.Disabled)
            {
                return;
            }

            foreach (var button in __instance.Buttons)
            {
                if (button == null || ReferenceEquals(button, __instance._settingsButton))
                {
                    continue;
                }
                var label = button.GetNodeOrNull<Label>("Label");
                Game.TextBlurService.BlurNode(label, TextBlurPercents.Menu);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("MenuBlur.Pause", () => throw ex);
        }
    }
}
