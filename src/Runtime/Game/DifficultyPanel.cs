using BrainFog.Core.Options;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace BrainFog.Game;

/// <summary>
/// Left-side in-run cognition modifier (renamed from "difficulty adjuster" 2026-09-20):
/// - selection-screen reveal count (none / 1 / 2 / 3 / all)
/// - shop &amp; event card faces on/off
/// - same-name reveal on/off (off = only the played copy is revealed)
/// All labels live under a "BrainFog*" named root, so the global text sweep
/// keeps the control readable.
/// </summary>
internal sealed partial class DifficultyPanel : CanvasLayer
{
    private PanelContainer _panel = null!;
    private VBoxContainer _body = null!;
    private OptionButton _selection = null!;
    private CheckButton _shopEvent = null!;
    private CheckButton _sameName = null!;
    private CheckButton _liveStatus = null!;
    private CheckButton _ownedRelics = null!;
    private CheckButton _mapRoutes = null!;
    private CheckButton _intents = null!;
    private Button _collapse = null!;

    private bool _placed;
    private bool _applying;

    public override void _Ready()
    {
        try
        {
            Layer = 95;
            Build();
            ApplyFromSettings();
        }
        catch (Exception ex)
        {
            PatchGuard.Run("DifficultyPanel.Ready", () => throw ex);
        }
    }

    public override void _Process(double delta)
    {
        if (ModRuntime.Disabled)
        {
            Visible = false;
            return;
        }
        var runActive = MegaCrit.Sts2.Core.Nodes.NRun.Instance is { } run && GodotObject.IsInstanceValid(run);
        Visible = runActive;
        if (runActive && !_placed && _panel.Size.Y > 1f)
        {
            Place();
            _placed = true;
        }
    }

    private void Build()
    {
        _panel = new PanelContainer
        {
            Name = "BrainFogDifficultyRoot",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.85f),
            BorderColor = new Color(0.55f, 0.55f, 0.58f, 0.9f),
            ContentMarginLeft = 12f,
            ContentMarginRight = 12f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        });

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);

        var header = new HBoxContainer();
        var title = new Label { Text = "认知修改器" };
        title.AddThemeFontSizeOverride("font_size", 17);
        header.AddChild(title);
        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        header.AddChild(spacer);
        _collapse = new Button
        {
            Text = "◀",
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = "收起/展开",
        };
        _collapse.Pressed += OnCollapsePressed;
        header.AddChild(_collapse);
        root.AddChild(header);

        _body = new VBoxContainer();
        var selectionLabel = new Label { Text = "选卡界面揭露" };
        _body.AddChild(selectionLabel);

        _selection = new OptionButton { FocusMode = Control.FocusModeEnum.None };
        foreach (var item in new[] { "不揭露", "随机1张", "随机2张", "随机3张", "全部" })
        {
            _selection.AddItem(item);
        }
        _selection.ItemSelected += OnSelectionSelected;
        _body.AddChild(_selection);

        _shopEvent = new CheckButton { Text = "商店/事件卡面揭露" };
        _shopEvent.Toggled += OnShopEventToggled;
        _body.AddChild(_shopEvent);

        _sameName = new CheckButton { Text = "同名卡全部揭露" };
        _sameName.Toggled += OnSameNameToggled;
        _body.AddChild(_sameName);

        _liveStatus = new CheckButton { Text = "显示实时血量/金币" };
        _liveStatus.Toggled += OnLiveStatusToggled;
        _body.AddChild(_liveStatus);

        _ownedRelics = new CheckButton { Text = "显示已拥有遗物" };
        _ownedRelics.Toggled += OnOwnedRelicsToggled;
        _body.AddChild(_ownedRelics);

        _mapRoutes = new CheckButton { Text = "显示地图所有路线" };
        _mapRoutes.Toggled += OnMapRoutesToggled;
        _body.AddChild(_mapRoutes);

        _intents = new CheckButton { Text = "可见敌人意图" };
        _intents.Toggled += OnIntentsToggled;
        _body.AddChild(_intents);

        root.AddChild(_body);
        _panel.AddChild(root);
        AddChild(_panel);

        foreach (var control in new Control[]
                 {
                     title, selectionLabel, _selection, _shopEvent, _sameName,
                     _liveStatus, _ownedRelics, _mapRoutes, _intents, _collapse,
                 })
        {
            control.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        }
    }

    private void Place()
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        _panel.Position = new Vector2(12f, Math.Max(12f, viewportSize.Y * 0.5f - _panel.Size.Y * 0.5f));
    }

    private void ApplyFromSettings()
    {
        _applying = true;
        try
        {
            var settings = DifficultyRuntime.Current;
            _selection.Selected = DifficultySettings.ToIndex(settings.SelectionReveal);
            _shopEvent.ButtonPressed = settings.RevealShopAndEventCards;
            _sameName.ButtonPressed = settings.RevealSameNameCards;
            _liveStatus.ButtonPressed = settings.ShowLiveStatus;
            _ownedRelics.ButtonPressed = settings.ShowOwnedRelics;
            _mapRoutes.ButtonPressed = settings.ShowAllMapRoutes;
            _intents.ButtonPressed = settings.ShowEnemyIntents;
            _body.Visible = !DifficultyRuntime.PanelCollapsed;
            _collapse.Text = DifficultyRuntime.PanelCollapsed ? "▶" : "◀";
        }
        finally
        {
            _applying = false;
        }
    }

    private void OnSelectionSelected(long index)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.SelectionReveal = DifficultySettings.FromIndex((int)index);
        DifficultyRuntime.NotifyChanged();
    }

    private void OnShopEventToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.RevealShopAndEventCards = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnSameNameToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.RevealSameNameCards = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnLiveStatusToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.ShowLiveStatus = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnOwnedRelicsToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.ShowOwnedRelics = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnMapRoutesToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.ShowAllMapRoutes = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnIntentsToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.ShowEnemyIntents = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnCollapsePressed()
    {
        var collapsed = _body.Visible;
        _body.Visible = !collapsed;
        _collapse.Text = collapsed ? "▶" : "◀";
        DifficultyRuntime.SetPanelCollapsed(collapsed);
    }
}
