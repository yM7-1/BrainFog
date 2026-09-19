using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BrainFog.Patches;

/// <summary>
/// The map boss point shows "?" instead of the boss art (user change
/// 2026-09-19; font slightly smaller than the original icon) and never
/// reveals the boss via hover. The node stays clickable for act completion.
/// </summary>
[HarmonyPatch(typeof(NBossMapPoint))]
internal static class BossMapPointMaskPatch
{
    private const string QuestionNodeName = "BrainFogBossQuestion";

    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void AfterReady(NBossMapPoint __instance) => Mask(__instance);

    [HarmonyPatch("RefreshColorInstantly")]
    [HarmonyPostfix]
    private static void AfterRefreshColor(NBossMapPoint __instance) => Mask(__instance);

    [HarmonyPatch("OnFocus")]
    [HarmonyPostfix]
    private static void AfterFocus(NBossMapPoint __instance) =>
        Game.PatchGuard.Run("BossMapPoint.Tip", () =>
        {
            if (!ModRuntime.Disabled)
            {
                NHoverTipSet.Remove(__instance);
            }
        });

    private static void Mask(NBossMapPoint point)
    {
        try
        {
            if (ModRuntime.Disabled || !GodotObject.IsInstanceValid(point))
            {
                return;
            }

            Hide(point._spineSprite);
            Hide(point._placeholderImage);
            Hide(point._placeholderOutline);

            if (point.GetNodeOrNull<Label>(QuestionNodeName) == null)
            {
                var question = new Label
                {
                    Name = QuestionNodeName,
                    Text = "?",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Modulate = new Color(0.92f, 0.92f, 0.92f),
                };
                question.AddThemeFontSizeOverride("font_size", 64);
                question.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
                question.AddThemeConstantOverride("outline_size", 6);
                question.ApplyLocaleFontSubstitution(FontType.Regular, "font");
                point.AddChild(question);
                question.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            }
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("BossMapPoint.Mask", () => throw ex);
        }
    }

    private static void Hide(Node? node)
    {
        if (node is CanvasItem item && GodotObject.IsInstanceValid(item))
        {
            item.Visible = false;
        }
    }
}
