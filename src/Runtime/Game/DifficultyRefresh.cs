using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.sts2.Core.Nodes.TopBar;

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
                Patches.MapLegendTextBlurPatch.Refresh(map);
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
        if (node is TextureRect icon && icon.HasMeta(Patches.RelicMasking.RewardIconMeta))
        {
            Patches.RelicMasking.ApplyRewardIcon(icon);
        }

        switch (node)
        {
            case NCreature creature:
                EnemyVisualMask.Apply(creature);
                if (ModRuntime.Disabled)
                {
                    LowHpHintDisplay.HideBorder(creature);
                }
                break;
            case NCreatureStateDisplay nameplate:
                Patches.EnemyNameMask.Apply(nameplate, null);
                break;
            case NRelic relic:
                Patches.RelicMasking.Apply(relic);
                break;
            case NRewardButton reward:
                Patches.RelicMasking.ApplyRewardButton(reward);
                break;
            case NInspectRelicScreen inspect:
                Patches.RelicMasking.ApplyInspect(inspect);
                break;
            case NPotion potion:
                Patches.PotionOutlines.Apply(potion);
                break;
            case NEventOptionButton option:
                Patches.EventChoiceIcons.ApplyTo(option);
                break;
            case NTopBarBossIcon bossIcon:
                Patches.BossIconHidePatch.Refresh(bossIcon);
                break;
            case NTopBarHp topBarHp:
                if (ModRuntime.Disabled)
                {
                    LowHpHintDisplay.HideLabel(topBarHp);
                }
                else
                {
                    // First re-attach after an earlier disabled launch (0.3.7):
                    // seed the snapshot from the live player instead of a value
                    // from a previous run.
                    var hadHpBar = SnapshotDisplay.HpBar != null;
                    SnapshotDisplay.AttachHpBar(topBarHp);
                    if (!hadHpBar && topBarHp._player is { } hpPlayer)
                    {
                        SnapshotDisplay.InitHp(hpPlayer.Creature.CurrentHp, hpPlayer.Creature.MaxHp);
                    }
                }
                break;
            case NTopBarGold topBarGold:
                if (!ModRuntime.Disabled)
                {
                    var hadGoldBar = SnapshotDisplay.GoldBarAttached;
                    SnapshotDisplay.AttachGoldBar(topBarGold);
                    if (!hadGoldBar && topBarGold._player is { } goldPlayer)
                    {
                        SnapshotDisplay.InitGold(goldPlayer.Gold);
                    }
                }
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
        if (ModRuntime.Disabled)
        {
            // Mod off: vanilla shows the intent the game decided to show.
            intent.Visible = true;
            return;
        }

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
