namespace BrainFog.Core.Map;

/// <summary>
/// Pure map-fog visibility rules (spec 0.01 3.3): visited nodes/paths and the
/// next travelable row stay visible, everything else is fogged.
/// </summary>
public static class MapFogRules
{
    public static bool IsFrontierNode(bool isTraveled, bool isTravelable) =>
        isTraveled || isTravelable;

    /// <summary>A path segment is visible when it starts at a visited node and
    /// both of its endpoints are on the frontier.</summary>
    public static bool IsPathVisible(bool fromTraveled, bool fromVisible, bool toVisible) =>
        fromTraveled && fromVisible && toVisible;
}
