using BlindSpire.Core.Status;

namespace BlindSpire.Game;

/// <summary>
/// Game-side holder for the displayed snapshot and its refresh requests.
/// HP refreshes only when a trigger (rest) requests it (candidate-010 wires rest).
/// Gold refreshes on deduction only.
/// </summary>
internal static class SnapshotDisplay
{
    public static StatusSnapshot Snapshot { get; } = new();

    private static bool _hpRefreshRequested;

    public static void InitHp(int hp, int maxHp)
    {
        Snapshot.RefreshHp(hp, maxHp);
        _hpRefreshRequested = true;
    }

    public static void InitGold(int gold) => Snapshot.RefreshGold(gold);

    /// <summary>Rest (or another trigger) asks the next HP change to be shown and snapshotted.</summary>
    public static void RequestHpRefresh() => _hpRefreshRequested = true;

    public static bool ConsumeHpRefresh()
    {
        var requested = _hpRefreshRequested;
        _hpRefreshRequested = false;
        return requested;
    }

    public static void OnHpChanged(int hp, int maxHp) => Snapshot.RefreshHp(hp, maxHp);

    public static void OnGoldChanged(int gold)
    {
        if (Snapshot.ShouldRefreshGold(gold))
        {
            Snapshot.RefreshGold(gold);
        }
    }
}
