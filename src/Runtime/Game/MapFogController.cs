using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace BlindSpire.Game;

/// <summary>
/// Map fog (spec 0.01 3.3 / 0.02 #9): only visited points/paths and the next
/// travelable row stay visible; the drawing tools are disabled.
/// </summary>
internal static class MapFogController
{
    public static void Apply(NMapScreen screen)
    {
        if (ModRuntime.Disabled || screen._mapPointDictionary is not { } points)
        {
            return;
        }

        var visible = new HashSet<MapCoord>();
        foreach (var pair in points)
        {
            var show = IsFrontier(pair.Value.State);
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
                    && fromNode.State == MapPointState.Traveled;
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

    private static bool IsFrontier(MapPointState state) =>
        state is MapPointState.Traveled or MapPointState.Travelable;

    private static void HideSpecial(NMapPoint? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }
        node.Visible = IsFrontier(node.State);
    }
}
