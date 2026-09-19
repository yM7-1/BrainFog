using BrainFog.Core.Map;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BrainFog.Game;

/// <summary>
/// Map fog (spec 0.01 3.3 / 0.02 #9): only visited points/paths and the next
/// travelable row stay visible; the drawing tools are disabled.
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

        if (screen._drawingTools != null && GodotObject.IsInstanceValid(screen._drawingTools))
        {
            screen._drawingTools.Visible = false;
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
