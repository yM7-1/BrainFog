using BlindSpire.Core.Status;

namespace BlindSpire.Game;

/// <summary>
/// Game-side holder for the displayed snapshot and its refresh triggers.
/// HP/gold labels are poked directly on explicit refreshes (rest), otherwise
/// live updates are suppressed (spec 0.02 #3, 0.03 c).
/// </summary>
internal static class SnapshotDisplay
{
    public static StatusSnapshot Snapshot { get; } = new();

    public static MegaCrit.sts2.Core.Nodes.TopBar.NTopBarHp? HpBar => _hpBar;

    private static MegaCrit.sts2.Core.Nodes.TopBar.NTopBarHp? _hpBar;
    private static MegaCrit.sts2.Core.Nodes.TopBar.NTopBarGold? _goldBar;
    private static bool _hpRefreshRequested;

    public static void AttachHpBar(MegaCrit.sts2.Core.Nodes.TopBar.NTopBarHp bar) => _hpBar = bar;

    public static void AttachGoldBar(MegaCrit.sts2.Core.Nodes.TopBar.NTopBarGold bar) => _goldBar = bar;

    public static void InitHp(int hp, int maxHp) =>
        PatchGuard.Run("Snapshot.InitHp", () =>
        {
            Snapshot.RefreshHp(hp, maxHp);
            _hpRefreshRequested = true;
        });

    public static void InitGold(int gold) =>
        PatchGuard.Run("Snapshot.InitGold", () => Snapshot.RefreshGold(gold));

    public static bool ConsumeHpRefresh()
    {
        var requested = _hpRefreshRequested;
        _hpRefreshRequested = false;
        return requested;
    }

    public static void OnHpChanged(int hp, int maxHp) => Snapshot.RefreshHp(hp, maxHp);

    /// <summary>Rest trigger: show and snapshot the true HP now (spec 0.02 #3).</summary>
    public static void RefreshHpNow(int hp, int maxHp) =>
        PatchGuard.Run("Snapshot.RefreshHp", () => RefreshHpNowCore(hp, maxHp));

    private static void RefreshHpNowCore(int hp, int maxHp)
    {
        Snapshot.RefreshHp(hp, maxHp);
        if (_hpBar != null && Godot.GodotObject.IsInstanceValid(_hpBar)
            && _hpBar._hpLabel != null && Godot.GodotObject.IsInstanceValid(_hpBar._hpLabel))
        {
            _hpBar._hpLabel.SetTextAutoSize($"{hp}/{maxHp}");
        }
    }

    /// <summary>Rest trigger: show and snapshot the true gold now (spec 0.02 #3).</summary>
    public static void RefreshGoldNow(int gold) =>
        PatchGuard.Run("Snapshot.RefreshGold", () => RefreshGoldNowCore(gold));

    private static void RefreshGoldNowCore(int gold)
    {
        Snapshot.RefreshGold(gold);
        if (_goldBar != null && Godot.GodotObject.IsInstanceValid(_goldBar)
            && _goldBar._goldLabel != null && Godot.GodotObject.IsInstanceValid(_goldBar._goldLabel))
        {
            _goldBar._currentGold = gold;
            _goldBar._goldLabel.SetTextAutoSize($"{gold}");
        }
    }

    public static void OnGoldChanged(int gold)
    {
        if (Snapshot.ShouldRefreshGold(gold))
        {
            Snapshot.RefreshGold(gold);
        }
    }
}
