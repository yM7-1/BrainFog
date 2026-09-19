using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace BrainFog.Patches;

/// <summary>
/// The top-bar HP is a stale snapshot, so it renders gray and carries a tag +
/// hint explaining that it is the state at the last rest (user change 2026-09-19).
/// </summary>
[HarmonyPatch(typeof(NTopBarHp))]
internal static class HpSnapshotVisualPatch
{
    private const string TagNodeName = "BrainFogHpSnapshotTag";
    private const string HintNodeName = "BrainFogHpSnapshotHint";

    // Readable gray (user feedback: the first version was too small and too faint).
    private static readonly Color SnapshotGray = new(0.88f, 0.88f, 0.88f);
    private static readonly Color HintGray = new(0.82f, 0.82f, 0.82f);

    [HarmonyPatch("Initialize")]
    [HarmonyPostfix]
    private static void AfterInitialize(NTopBarHp __instance) => Decorate(__instance);

    [HarmonyPatch("UpdateHealth")]
    [HarmonyPrefix]
    private static void BeforeUpdateHealth(NTopBarHp __instance) => Decorate(__instance);

    [HarmonyPatch("UpdateHealth")]
    [HarmonyPostfix]
    private static void AfterUpdateHealth(NTopBarHp __instance) => Decorate(__instance);

    private static void Decorate(NTopBarHp bar)
    {
        try
        {
            if (ModRuntime.Disabled || bar._hpLabel == null || !GodotObject.IsInstanceValid(bar._hpLabel))
            {
                return;
            }

            bar._hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, SnapshotGray);
            bar._hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, new Color(0f, 0f, 0f, 0.6f));

            var tag = EnsureLabel(bar, TagNodeName, 15, SnapshotGray);
            var hint = EnsureLabel(bar, HintNodeName, 13, HintGray);
            tag.Text = Game.ModLocalization.HpSnapshotTag;
            hint.Text = Game.ModLocalization.HpSnapshotHint;

            var labelPosition = bar._hpLabel.Position;
            var labelHeight = bar._hpLabel.Size.Y;
            tag.Position = labelPosition + new Vector2(0f, labelHeight + 2f);
            hint.Position = tag.Position + new Vector2(0f, tag.GetMinimumSize().Y + 1f);
        }
        catch (Exception ex)
        {
            Game.PatchGuard.Run("HpSnapshot.Decorate", () => throw ex);
        }
    }

    private static Label EnsureLabel(NTopBarHp bar, string name, int fontSize, Color color)
    {
        if (bar.GetNodeOrNull<Label>(name) is { } existing && GodotObject.IsInstanceValid(existing))
        {
            return existing;
        }

        var label = new Label
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = color,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        label.AddThemeConstantOverride("outline_size", 4);
        label.ApplyLocaleFontSubstitution(FontType.Regular, ThemeConstants.Label.Font);
        bar.AddChild(label);
        return label;
    }
}
