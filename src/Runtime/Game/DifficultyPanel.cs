using BrainFog.Core.Options;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace BrainFog.Game;

/// <summary>
/// In-run cognition modifier panel (draggable, collapsible, dockable):
/// - drag the title bar to move it anywhere (position persisted)
/// - click the title bar (no drag) or the arrow to collapse/expand
/// - the ◀/▶ button docks the panel to the nearer screen edge; a small edge
///   tab brings it back (state persisted)
/// - selection-screen reveal count (none / 1 / 2 / 3 / all)
/// - shop &amp; event card faces on/off
/// - same-name reveal on/off (off = only the played copy is revealed)
/// - reveal every card face for the run
/// All labels live under a "BrainFog*" named root, so the global text blur
/// keeps the control readable.
/// </summary>
internal sealed partial class DifficultyPanel : CanvasLayer
{
    private static readonly Color Accent = new(0.93f, 0.79f, 0.42f);
    private static readonly Color AccentBright = new(1f, 0.90f, 0.60f);
    private static readonly Color SectionColor = new(0.66f, 0.70f, 0.78f);
    private static readonly Color HintColor = new(0.62f, 0.65f, 0.70f);

    private const float ClickThresholdPx = 8f;
    private const double LocalePollSeconds = 0.5;

    private PanelContainer _panel = null!;
    private VBoxContainer _body = null!;
    private Label _title = null!;
    private Label _sectionCognition = null!;
    private Label _sectionPerception = null!;
    private Label _selectionLabel = null!;
    private Label _hint = null!;
    private OptionButton _selection = null!;
    private CheckButton _shopEvent = null!;
    private CheckButton _sameName = null!;
    private CheckButton _revealAll = null!;
    private CheckButton _liveStatus = null!;
    private CheckButton _ownedRelics = null!;
    private CheckButton _mapRoutes = null!;
    private CheckButton _intents = null!;
    private Button _collapse = null!;
    private Button _dock = null!;
    private Button _tab = null!;

    private string _currentHintKey = "panel_hint_default";
    private string _lastLocale = string.Empty;
    private double _localeTimer;
    private bool _placed;
    private bool _applying;
    private bool _dragging;
    private float _dragDistance;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        try
        {
            Layer = 95;
            Build();
            Localize();
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
        PollLocale(delta);
        var runActive = MegaCrit.Sts2.Core.Nodes.NRun.Instance is { } run && GodotObject.IsInstanceValid(run);
        Visible = runActive;
        UpdateDockVisuals();
        if (runActive && !DifficultyRuntime.PanelDocked && !_placed && _panel.Size.Y > 1f)
        {
            Place();
            _placed = true;
        }
    }

    /// <summary>Panel visible unless docked; the edge tab is the reverse.</summary>
    private void UpdateDockVisuals()
    {
        var docked = DifficultyRuntime.PanelDocked;
        _panel.Visible = !docked;
        _tab.Visible = docked;
        if (docked)
        {
            PlaceTab();
        }
    }

    private void PlaceTab()
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        var height = Math.Max(1f, _tab.Size.Y);
        var y = Math.Clamp(
            DifficultyRuntime.PanelDockY * viewportSize.Y - height * 0.5f,
            8f,
            Math.Max(8f, viewportSize.Y - height - 8f));
        var x = DifficultyRuntime.PanelDockSide == 1 ? viewportSize.X - _tab.Size.X : 0f;
        _tab.Position = new Vector2(x, y);
        _tab.Text = DifficultyRuntime.PanelDockSide == 1 ? "◀" : "▶";
    }

    /// <summary>The panel follows the game language (zhs -> Chinese, anything
    /// else -> English), including a language change made in settings.</summary>
    private void PollLocale(double delta)
    {
        _localeTimer += delta;
        if (_localeTimer < LocalePollSeconds)
        {
            return;
        }
        _localeTimer = 0;
        var locale = ModLocalization.CurrentLanguageCode;
        if (locale != _lastLocale)
        {
            Localize();
        }
    }

    private static string T(string key, string fallback) => ModLocalization.Panel(key, fallback);

    private void Build()
    {
        _panel = new PanelContainer
        {
            Name = "BrainFogDifficultyRoot",
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(252f, 0f),
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.055f, 0.065f, 0.09f, 0.93f),
            BorderColor = new Color(0.55f, 0.50f, 0.34f, 0.65f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 12f,
            ShadowColor = new Color(0f, 0f, 0f, 0.35f),
            ShadowSize = 8,
            ShadowOffset = new Vector2(0f, 3f),
        });

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);

        root.AddChild(BuildHeader());

        _body = new VBoxContainer { Name = "BrainFogBody" };
        _body.AddThemeConstantOverride("separation", 7);

        _body.AddChild(Divider());
        _sectionCognition = Section();
        _body.AddChild(_sectionCognition);

        _selectionLabel = new Label();
        _body.AddChild(_selectionLabel);
        _selection = new OptionButton { FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        for (var i = 0; i < 5; i++)
        {
            _selection.AddItem(string.Empty);
        }
        _selection.ItemSelected += OnSelectionSelected;
        _body.AddChild(_selection);
        BindHint(_selection, "panel_hint_selection");

        _shopEvent = new CheckButton();
        _shopEvent.Toggled += OnShopEventToggled;
        _body.AddChild(_shopEvent);
        BindHint(_shopEvent, "panel_hint_shop_event");

        _sameName = new CheckButton();
        _sameName.Toggled += OnSameNameToggled;
        _body.AddChild(_sameName);
        BindHint(_sameName, "panel_hint_same_name");

        _revealAll = new CheckButton();
        _revealAll.Toggled += OnRevealAllToggled;
        _body.AddChild(_revealAll);
        BindHint(_revealAll, "panel_hint_reveal_all");

        _sectionPerception = Section();
        _body.AddChild(_sectionPerception);

        _liveStatus = new CheckButton();
        _liveStatus.Toggled += OnLiveStatusToggled;
        _body.AddChild(_liveStatus);
        BindHint(_liveStatus, "panel_hint_live_status");

        _ownedRelics = new CheckButton();
        _ownedRelics.Toggled += OnOwnedRelicsToggled;
        _body.AddChild(_ownedRelics);
        BindHint(_ownedRelics, "panel_hint_owned_relics");

        _mapRoutes = new CheckButton();
        _mapRoutes.Toggled += OnMapRoutesToggled;
        _body.AddChild(_mapRoutes);
        BindHint(_mapRoutes, "panel_hint_map_routes");

        _intents = new CheckButton();
        _intents.Toggled += OnIntentsToggled;
        _body.AddChild(_intents);
        BindHint(_intents, "panel_hint_intents");

        _body.AddChild(Divider());

        _hint = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _hint.AddThemeFontSizeOverride("font_size", 11);
        _hint.AddThemeColorOverride("font_color", HintColor);
        _body.AddChild(_hint);

        root.AddChild(_body);
        _panel.AddChild(root);
        AddChild(_panel);

        _tab = new Button
        {
            Name = "BrainFogDockTab",
            Text = "▶",
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(26f, 64f),
            Visible = false,
        };
        _tab.AddThemeFontSizeOverride("font_size", 14);
        _tab.AddThemeColorOverride("font_color", Accent);
        _tab.AddThemeColorOverride("font_hover_color", AccentBright);
        _tab.AddThemeColorOverride("font_pressed_color", AccentBright);
        _tab.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.055f, 0.065f, 0.09f, 0.85f),
            BorderColor = new Color(0.55f, 0.50f, 0.34f, 0.55f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        });
        _tab.Pressed += UndockPanel;
        _tab.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        AddChild(_tab);

        foreach (var control in new Control[]
                 {
                     _selectionLabel, _selection, _shopEvent, _sameName, _revealAll,
                     _liveStatus, _ownedRelics, _mapRoutes, _intents, _hint,
                 })
        {
            control.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        }
    }

    /// <summary>Applies all localized strings for the current game language.
    /// Chinese for zhs; every other language uses the English table.</summary>
    private void Localize()
    {
        _lastLocale = ModLocalization.CurrentLanguageCode;
        var zh = ModLocalization.IsChinese;

        _title.Text = T("panel_title", zh ? "认知修改器" : "Cognition Modifier");
        _sectionCognition.Text = T("panel_section_cognition", zh ? "认知" : "Cognition");
        _sectionPerception.Text = T("panel_section_perception", zh ? "感知" : "Perception");
        _selectionLabel.Text = T("panel_selection_label", zh ? "选卡界面揭露" : "Reveal in card rewards");

        var options = zh
            ? new[] { "不揭露", "随机1张", "随机2张", "随机3张", "全部" }
            : new[] { "None", "Random 1", "Random 2", "Random 3", "All" };
        var keys = new[] { "panel_selection_0", "panel_selection_1", "panel_selection_2", "panel_selection_3", "panel_selection_all" };
        for (var i = 0; i < options.Length; i++)
        {
            _selection.SetItemText(i, T(keys[i], options[i]));
        }

        _shopEvent.Text = T("panel_shop_event", zh ? "商店/事件卡面揭露" : "Reveal shop/event cards");
        _sameName.Text = T("panel_same_name", zh ? "同名卡全部揭露" : "Reveal all copies");
        _revealAll.Text = T("panel_reveal_all", zh ? "卡牌本局全部揭示" : "Reveal all cards this run");
        _liveStatus.Text = T("panel_live_status", zh ? "显示实时血量/金币" : "Show live HP/gold");
        _ownedRelics.Text = T("panel_owned_relics", zh ? "显示已拥有遗物" : "Show owned relics");
        _mapRoutes.Text = T("panel_map_routes", zh ? "显示地图所有路线" : "Show all map routes");
        _intents.Text = T("panel_intents", zh ? "可见敌人意图" : "Show enemy intents");
        _dock.TooltipText = T("panel_dock_tooltip", zh ? "缩进到屏幕边缘（点边缘小按钮恢复）" : "Dock to the screen edge (click the edge tab to restore)");
        _tab.TooltipText = T("panel_tab_tooltip", zh ? "显示认知修改器" : "Show the cognition modifier");
        ShowHint(_currentHintKey);

        // Re-apply the locale font (no-op for Latin; swaps in the CJK font when
        // the player switches to Chinese at runtime).
        foreach (var control in new Control[]
                 {
                     _title, _sectionCognition, _sectionPerception, _selectionLabel,
                     _selection, _shopEvent, _sameName, _revealAll, _liveStatus,
                     _ownedRelics, _mapRoutes, _intents, _hint, _dock, _tab,
                 })
        {
            control.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        }
    }

    private Control BuildHeader()
    {
        var header = new HBoxContainer
        {
            Name = "BrainFogHeader",
            MouseFilter = Control.MouseFilterEnum.Stop,
            MouseDefaultCursorShape = Control.CursorShape.Move,
        };
        header.GuiInput += OnHeaderInput;

        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 18);
        _title.AddThemeColorOverride("font_color", Accent);
        _title.MouseFilter = Control.MouseFilterEnum.Ignore;
        _title.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        header.AddChild(_title);

        header.AddChild(new Control
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });

        _collapse = new Button
        {
            Text = "▾",
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(28f, 0f),
        };
        _collapse.AddThemeFontSizeOverride("font_size", 16);
        _collapse.AddThemeColorOverride("font_color", Accent);
        _collapse.AddThemeColorOverride("font_hover_color", AccentBright);
        _collapse.AddThemeColorOverride("font_pressed_color", AccentBright);
        _collapse.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        _collapse.Pressed += ToggleCollapsed;
        header.AddChild(_collapse);

        _dock = new Button
        {
            Text = "▶",
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(24f, 0f),
        };
        _dock.AddThemeFontSizeOverride("font_size", 13);
        _dock.AddThemeColorOverride("font_color", HintColor);
        _dock.AddThemeColorOverride("font_hover_color", AccentBright);
        _dock.AddThemeColorOverride("font_pressed_color", AccentBright);
        _dock.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        _dock.Pressed += DockPanel;
        header.AddChild(_dock);

        return header;
    }

    private static Label Section()
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", SectionColor);
        label.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        return label;
    }

    private static Control Divider() => new HSeparator
    {
        Modulate = new Color(1f, 1f, 1f, 0.14f),
    };

    private void BindHint(Control control, string hintKey)
    {
        control.MouseEntered += () => ShowHint(hintKey);
        control.MouseExited += () => ShowHint("panel_hint_default");
    }

    private void ShowHint(string hintKey)
    {
        _currentHintKey = hintKey;
        var zh = ModLocalization.IsChinese;
        var fallback = hintKey switch
        {
            "panel_hint_selection" => zh ? "奖励界面随机揭露部分卡面（每次奖励固定）" : "Randomly reveal some card faces in reward screens (fixed per reward)",
            "panel_hint_shop_event" => zh ? "商店与事件获得的卡牌显示真实牌面" : "Show real card faces for cards from shops and events",
            "panel_hint_same_name" => zh ? "打出或升级一张后，同名卡全部揭示" : "After playing or upgrading one, reveal every copy of that card",
            "panel_hint_reveal_all" => zh ? "本局所有卡牌直接显示真实牌面（文字仍按规则乱码）" : "Every card shows its real face for this run (text stays garbled)",
            "panel_hint_live_status" => zh ? "顶栏显示真实血量与金币" : "Show true HP and gold in the top bar",
            "panel_hint_owned_relics" => zh ? "库存与检视中显示已拥有遗物" : "Show owned relics in inventory and inspect screens",
            "panel_hint_map_routes" => zh ? "地图显示全部节点与路线" : "Show every map node and route",
            "panel_hint_intents" => zh ? "敌人意图每回合持续可见" : "Enemy intents stay visible every turn",
            _ => zh ? "拖动标题栏可移动 · 修改即时生效" : "Drag the title bar to move · changes apply instantly",
        };
        _hint.Text = T(hintKey, fallback);
    }

    private void OnHeaderInput(InputEvent @event)
    {
        try
        {
            switch (@event)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                    if (button.Pressed)
                    {
                        _dragging = true;
                        _dragDistance = 0f;
                        _dragOffset = _panel.Position - _panel.GetGlobalMousePosition();
                    }
                    else if (_dragging)
                    {
                        EndDrag();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("DifficultyPanel.Drag", () => throw ex);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_dragging || ModRuntime.Disabled)
        {
            return;
        }
        try
        {
            switch (@event)
            {
                case InputEventMouseMotion motion:
                    _dragDistance += motion.Relative.Length();
                    MovePanelTo(_panel.GetGlobalMousePosition() + _dragOffset);
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                    EndDrag();
                    break;
            }
        }
        catch (Exception ex)
        {
            PatchGuard.Run("DifficultyPanel.DragMove", () => throw ex);
        }
    }

    private void EndDrag()
    {
        _dragging = false;
        if (_dragDistance < ClickThresholdPx)
        {
            // A click on the arrow button is already handled by its Pressed
            // signal; anywhere else on the title bar toggles the body.
            if (!IsPointOnCollapseButton())
            {
                ToggleCollapsed();
            }
        }
        else
        {
            PersistPosition();
        }
    }

    private bool IsPointOnCollapseButton()
    {
        if (!GodotObject.IsInstanceValid(_collapse))
        {
            return false;
        }
        var local = _collapse.GetGlobalTransform().AffineInverse() * _panel.GetGlobalMousePosition();
        return new Rect2(Vector2.Zero, _collapse.Size).HasPoint(local);
    }

    private void MovePanelTo(Vector2 target)
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        var maxX = Math.Max(0f, viewportSize.X - _panel.Size.X);
        var maxY = Math.Max(0f, viewportSize.Y - _panel.Size.Y);
        _panel.Position = new Vector2(
            Math.Clamp(target.X, 0f, maxX),
            Math.Clamp(target.Y, 0f, maxY));
        UpdateDockButton();
    }

    /// <summary>The dock button points at the edge the panel would dock to.</summary>
    private void UpdateDockButton()
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        var centerX = _panel.Position.X + _panel.Size.X * 0.5f;
        _dock.Text = centerX < viewportSize.X * 0.5f ? "◀" : "▶";
    }

    private void PersistPosition()
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        DifficultyRuntime.SetPanelPosition(
            _panel.Position.X / Math.Max(1f, viewportSize.X),
            _panel.Position.Y / Math.Max(1f, viewportSize.Y));
    }

    private void Place()
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        if (DifficultyRuntime.HasPanelPosition)
        {
            MovePanelTo(new Vector2(
                DifficultyRuntime.PanelPositionX * viewportSize.X,
                DifficultyRuntime.PanelPositionY * viewportSize.Y));
            return;
        }
        _panel.Position = new Vector2(12f, Math.Max(12f, viewportSize.Y * 0.5f - _panel.Size.Y * 0.5f));
        UpdateDockButton();
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
            _revealAll.ButtonPressed = settings.RevealAllCards;
            _liveStatus.ButtonPressed = settings.ShowLiveStatus;
            _ownedRelics.ButtonPressed = settings.ShowOwnedRelics;
            _mapRoutes.ButtonPressed = settings.ShowAllMapRoutes;
            _intents.ButtonPressed = settings.ShowEnemyIntents;
            _body.Visible = !DifficultyRuntime.PanelCollapsed;
            _collapse.Text = DifficultyRuntime.PanelCollapsed ? "▸" : "▾";
            UpdateDockVisuals();
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

    private void OnRevealAllToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.RevealAllCards = pressed;
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

    private void ToggleCollapsed()
    {
        var collapsed = _body.Visible;
        _body.Visible = !collapsed;
        _collapse.Text = collapsed ? "▸" : "▾";
        ResizePanel();
        DifficultyRuntime.SetPanelCollapsed(collapsed);
    }

    /// <summary>Docks the panel to the nearer screen edge (the edge tab
    /// restores it); state and vertical position are persisted.</summary>
    private void DockPanel()
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        var centerX = _panel.Position.X + _panel.Size.X * 0.5f;
        var side = centerX < viewportSize.X * 0.5f ? 0 : 1;
        var normalizedY = Math.Clamp(
            (_panel.Position.Y + _panel.Size.Y * 0.5f) / Math.Max(1f, viewportSize.Y),
            0f,
            1f);
        DifficultyRuntime.SetPanelDocked(true, side, normalizedY);
        UpdateDockVisuals();
    }

    private void UndockPanel()
    {
        DifficultyRuntime.SetPanelDocked(false, DifficultyRuntime.PanelDockSide, DifficultyRuntime.PanelDockY);
        UpdateDockVisuals();
        if (!_placed)
        {
            Place();
            _placed = true;
        }
    }

    private void ResizePanel()
    {
        // Re-fit after the visibility change has been laid out, then re-clamp.
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(_panel))
            {
                return;
            }
            _panel.Size = _panel.GetCombinedMinimumSize();
            ReclampPosition();
        }).CallDeferred();
    }

    private void ReclampPosition()
    {
        if (_placed)
        {
            MovePanelTo(_panel.Position);
        }
    }
}
