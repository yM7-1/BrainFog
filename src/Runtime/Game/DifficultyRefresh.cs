using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BrainFog.Game;

/// <summary>
/// Applies difficulty-option changes to already-rendered UI: relics, health
/// bars, the top bar and (if open) the map screen. One bounded tree walk per
/// toggle click — cheap and immediate.
/// </summary>
internal static class DifficultyRefresh
{
    private const int MaxDepth = 64;

    public static void ApplyAll() =>
        PatchGuard.Run("Difficulty.RefreshAll", () =>
        {
            SnapshotDisplay.RefreshTopBar();

            if (NMapScreen.Instance is { } map && GodotObject.IsInstanceValid(map))
            {
                MapFogController.Apply(map);
            }

            if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            {
                return;
            }
            Walk(tree.Root, 0);
        });

    private static void Walk(Node node, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }
        switch (node)
        {
            case NRelic relic:
                Patches.RelicMasking.Apply(relic);
                break;
            case NHealthBar bar:
                bar.RefreshValues();
                break;
            case NBossMapPoint boss:
                Patches.BossMapPointMaskPatch.Refresh(boss);
                break;
            case NIntent intent:
                RefreshIntent(intent);
                break;
        }
        foreach (var child in node.GetChildren())
        {
            Walk(child, depth + 1);
        }
    }

    private static void RefreshIntent(NIntent intent)
    {
        switch (DifficultyRuntime.Current.IntentMode)
        {
            case Core.Options.IntentVisibility.All:
                intent.Visible = true;
                return;
            case Core.Options.IntentVisibility.Hidden:
                intent.Visible = false;
                return;
        }

        var node = intent.GetParent();
        while (node != null && node is not NCreature)
        {
            node = node.GetParent();
        }
        if (node is NCreature { Entity: { } entity })
        {
            intent.Visible = IntentGate.ShouldShow(entity, entity.CombatState?.RoundNumber ?? 1);
        }
    }
}
