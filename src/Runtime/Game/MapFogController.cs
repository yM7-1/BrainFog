using BrainFog.Core.Map;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BrainFog.Game;

/// <summary>
/// Map fog (spec 0.01 3.3 / 0.02 #9): only visited points/paths and the next
/// travelable row stay visible. The original drawing tools are never touched
/// (user change 2026-09-21, 0.3.3).
/// </summary>
internal static class MapFogController
{
    public static void Apply(NMapScreen screen) =>
        PatchGuard.Run("MapFog.Apply", () => ApplyCore(screen));

    private static void ApplyCore(NMapScreen screen)
    {
        if (ModRuntime.Disabled || screen._mapPointDictionary is not { } points)
        {
            return;
        }

        if (DifficultyRuntime.Current.ShowAllMapRoutes)
        {
            RevealEverything(screen, points);
            return;
        }

        var visible = new HashSet<MapCoord>();
        foreach (var pair in points)
        {
            if (pair.Value == null || !GodotObject.IsInstanceValid(pair.Value))
            {
                continue;
            }
            var show = MapFogRules.IsFrontierNode(
                pair.Value.State == MapPointState.Traveled,
                pair.Value.State == MapPointState.Travelable);
            if (show)
            {
                visible.Add(pair.Key);
            }
            pair.Value.Visible = show;
        }

        if (screen._paths is { } paths)
        {
            foreach (var pair in paths)
            {
                var from = pair.Key.Item1;
                var to = pair.Key.Item2;
                var show = visible.Contains(from)
                    && visible.Contains(to)
                    && points.TryGetValue(from, out var fromNode)
                    && MapFogRules.IsPathVisible(
                        fromTraveled: fromNode.State == MapPointState.Traveled,
                        fromVisible: visible.Contains(from),
                        toVisible: visible.Contains(to));
                foreach (var segment in pair.Value)
                {
                    if (segment != null && GodotObject.IsInstanceValid(segment))
                    {
                        segment.Visible = show;
                    }
                }
            }
        }

        HideSpecial(screen._bossPointNode);
        HideSpecial(screen._secondBossPointNode);
        HideSpecial(screen._startingPointNode);
    }

    /// <summary>Difficulty option: show every node and route.</summary>
    private static void RevealEverything(NMapScreen screen, System.Collections.Generic.Dictionary<MapCoord, NMapPoint> points)
    {
        foreach (var pair in points)
        {
            if (pair.Value != null && GodotObject.IsInstanceValid(pair.Value))
            {
                pair.Value.Visible = true;
            }
        }

        if (screen._paths is { } paths)
        {
            foreach (var pair in paths)
            {
                foreach (var segment in pair.Value)
                {
                    if (segment != null && GodotObject.IsInstanceValid(segment))
                    {
                        segment.Visible = true;
                    }
                }
            }
        }

        ShowSpecial(screen._bossPointNode);
        ShowSpecial(screen._secondBossPointNode);
        ShowSpecial(screen._startingPointNode);
    }

    private static void ShowSpecial(NMapPoint? node)
    {
        if (node != null && GodotObject.IsInstanceValid(node))
        {
            node.Visible = true;
        }
    }

    private static void HideSpecial(NMapPoint? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }
        node.Visible = MapFogRules.IsFrontierNode(
            node.State == MapPointState.Traveled,
            node.State == MapPointState.Travelable);
    }
}
