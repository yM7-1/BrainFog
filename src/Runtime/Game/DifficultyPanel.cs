using BrainFog.Core.Options;
using BrainFog.Core.Text;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace BrainFog.Game;

/// <summary>
/// In-run cognition modifier panel (draggable, collapsible, dockable):
/// - drag the title bar to move it anywhere (position persisted)
/// - click the title bar (no drag) or the arrow to collapse/expand
/// - the ◀/▶ button docks the panel to the nearer screen edge; a small edge
///   tab brings it back (state persisted)
/// - unified blur ratio slider (0–100%, 1% steps) with live re-apply
/// - selection-screen reveal count (none / 1 / 2 / 3 / all)
/// - shop &amp; event card faces on/off
/// - same-name reveal on/off (off = only the played copy is revealed)
/// - reveal every card face for the run
/// - HP/gold snapshot mode on/off (default: live values, garbled)
/// - HP/gold numbers readable on/off (default: garbled like all text)
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
    private Label _tip = null!;
    private Control? _tipAnchor;
    private OptionButton _selection = null!;
    private CheckButton _shopEvent = null!;
    private CheckButton _snapshotStatus = null!;
    private CheckButton _statusNumbers = null!;
    private CheckButton _ownedRelics = null!;
    private CheckButton _mapRoutes = null!;
    private Label _sectionText = null!;
    private Label _blurLabel = null!;
    private HSlider _blurSlider = null!;
    private Label _saltLabel = null!;
    private OptionButton _saltMode = null!;
    private Label _memoryLabel = null!;
    private OptionButton _memoryMode = null!;
    private Label _badNLabel = null!;
    private SpinBox _badN = null!;
    private Label _intentLabel = null!;
    private OptionButton _intentMode = null!;
    private Button _reset = null!;
    private Button _collapse = null!;
    private Button _dock = null!;
    private Button _tab = null!;

    private string _currentHintKey = "panel_hint_default";
    private string _lastLocale = string.Empty;
    private double _localeTimer;
    private bool _placed;
    private bool _applying;
    private bool _dragging;
    private bool _blurDirty;
    private bool _blurPending;
    private double _blurSaveTimer;
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
        if (_blurPending)
        {
            // Coalesce slider drags: re-apply at most once per frame.
            _blurPending = false;
            Game.TextBlurService.ReapplyAllText(DifficultyRuntime.TextBlurPercent);
        }
        if (_blurDirty)
        {
            // Debounce the config write while the slider is being dragged.
            _blurSaveTimer += delta;
            if (_blurSaveTimer >= 0.4)
            {
                SaveBlur();
            }
        }
        Visible = true; // also usable on the main title screen (user change 2026-09-21)
        UpdateDockVisuals();
        if (!DifficultyRuntime.PanelDocked && !_placed && _panel.Size.Y > 1f)
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
        _sectionText = Section();
        _body.AddChild(_sectionText);

        _blurLabel = new Label();
        _body.AddChild(_blurLabel);

        _blurSlider = new HSlider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            FocusMode = Control.FocusModeEnum.None,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _blurSlider.ValueChanged += OnBlurChanged;
        _blurSlider.DragEnded += _ => SaveBlur();
        _body.AddChild(_blurSlider);
        BindHint(_blurSlider, "panel_hint_blur");

        _saltLabel = new Label();
        _body.AddChild(_saltLabel);
        _saltMode = new OptionButton { FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _saltMode.AddItem(string.Empty);
        _saltMode.AddItem(string.Empty);
        _saltMode.ItemSelected += OnSaltModeSelected;
        _body.AddChild(_saltMode);
        BindHint(_saltMode, "panel_hint_salt");

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

        _memoryLabel = new Label();
        _body.AddChild(_memoryLabel);
        _memoryMode = new OptionButton { FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        for (var i = 0; i < 4; i++)
        {
            _memoryMode.AddItem(string.Empty);
        }
        _memoryMode.ItemSelected += OnMemoryModeSelected;
        _body.AddChild(_memoryMode);
        _memoryMode.MouseEntered += () => ShowTip(_memoryMode, MemoryHintKey());
        _memoryMode.MouseExited += HideTip;

        _badNLabel = new Label();
        _body.AddChild(_badNLabel);
        _badN = new SpinBox
        {
            MinValue = 1,
            MaxValue = 99,
            Step = 1,
            FocusMode = Control.FocusModeEnum.None,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _badN.ValueChanged += OnBadNChanged;
        _body.AddChild(_badN);
        BindHint(_badN, "panel_hint_memory_bad");

        _sectionPerception = Section();
        _body.AddChild(_sectionPerception);

        _snapshotStatus = new CheckButton();
        _snapshotStatus.Toggled += OnSnapshotStatusToggled;
        _body.AddChild(_snapshotStatus);
        BindHint(_snapshotStatus, "panel_hint_amnesia_status");

        _statusNumbers = new CheckButton();
        _statusNumbers.Toggled += OnStatusNumbersToggled;
        _body.AddChild(_statusNumbers);
        BindHint(_statusNumbers, "panel_hint_status_numbers");

        _ownedRelics = new CheckButton();
        _ownedRelics.Toggled += OnOwnedRelicsToggled;
        _body.AddChild(_ownedRelics);
        BindHint(_ownedRelics, "panel_hint_owned_relics");

        _mapRoutes = new CheckButton();
        _mapRoutes.Toggled += OnMapRoutesToggled;
        _body.AddChild(_mapRoutes);
        BindHint(_mapRoutes, "panel_hint_map_routes");

        _intentLabel = new Label();
        _body.AddChild(_intentLabel);
        _intentMode = new OptionButton { FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        for (var i = 0; i < 3; i++)
        {
            _intentMode.AddItem(string.Empty);
        }
        _intentMode.ItemSelected += OnIntentModeSelected;
        _body.AddChild(_intentMode);
        BindHint(_intentMode, "panel_hint_intent");

        _body.AddChild(Divider());

        _reset = new Button
        {
            Text = "Reset",
            FocusMode = Control.FocusModeEnum.None,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _reset.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        _reset.Pressed += OnReset;
        _body.AddChild(_reset);
        BindHint(_reset, "panel_hint_reset");

        _body.AddChild(Divider());

        root.AddChild(_body);
        _panel.AddChild(root);
        AddChild(_panel);

        // Option description shown as a floating card next to the hovered
        // control (user change 2026-09-21; replaces the bottom hint line).
        _tip = new Label
        {
            Name = "BrainFogTip",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 10,
        };
        _tip.AddThemeFontSizeOverride("font_size", 16);
        _tip.AddThemeColorOverride("font_color", new Color(0.96f, 0.94f, 0.86f));
        _tip.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.045f, 0.055f, 0.08f, 0.97f),
            BorderColor = new Color(0.62f, 0.55f, 0.36f, 0.85f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12f,
            ContentMarginRight = 12f,
            ContentMarginTop = 8f,
            ContentMarginBottom = 8f,
            ShadowColor = new Color(0f, 0f, 0f, 0.45f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0f, 2f),
        });
        _tip.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        AddChild(_tip);

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
                     _selectionLabel, _selection, _shopEvent,
                     _snapshotStatus, _statusNumbers, _ownedRelics, _mapRoutes, _tip,
                     _sectionText, _blurLabel, _saltLabel, _saltMode,
                     _memoryLabel, _memoryMode, _badNLabel, _badN,
                     _intentLabel, _intentMode, _reset,
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
        _sectionText.Text = T("panel_section_text", zh ? "文字" : "Text");
        _sectionCognition.Text = T("panel_section_cognition", zh ? "认知" : "Cognition");
        _sectionPerception.Text = T("panel_section_perception", zh ? "感知" : "Perception");

        _saltLabel.Text = T("panel_salt_label", zh ? "乱码模式" : "Blur mode");
        var saltOptions = zh
            ? new[] { "固定混乱", "混乱混乱" }
            : new[] { "Fixed chaos", "Chaos chaos" };
        var saltKeys = new[] { "panel_salt_0", "panel_salt_1" };
        for (var i = 0; i < saltOptions.Length; i++)
        {
            _saltMode.SetItemText(i, T(saltKeys[i], saltOptions[i]));
        }

        _selectionLabel.Text = T("panel_selection_label", zh ? "卡牌奖励" : "Card rewards");
        var options = zh
            ? new[] { "不揭示", "随机揭示1张", "随机揭示2张", "随机揭示3张", "卡牌奖励全部揭示" }
            : new[] { "None", "Random 1", "Random 2", "Random 3", "Reveal all reward cards" };
        var keys = new[] { "panel_selection_0", "panel_selection_1", "panel_selection_2", "panel_selection_3", "panel_selection_all" };
        for (var i = 0; i < options.Length; i++)
        {
            _selection.SetItemText(i, T(keys[i], options[i]));
        }

        _shopEvent.Text = T("panel_shop_event", zh ? "商店/事件卡面揭露" : "Reveal shop/event cards");

        _memoryLabel.Text = T("panel_memory_label", zh ? "卡牌记忆设置" : "Card memory");
        var memoryOptions = zh
            ? new[] { "通晓万物", "好记性", "坏记性", "歪比巴卜" }
            : new[] { "Omniscience", "Good memory", "Bad memory", "Nonsense" };
        var memoryKeys = new[] { "panel_memory_0", "panel_memory_1", "panel_memory_2", "panel_memory_3" };
        for (var i = 0; i < memoryOptions.Length; i++)
        {
            _memoryMode.SetItemText(i, T(memoryKeys[i], memoryOptions[i]));
        }
        _badNLabel.Text = T("panel_memory_n", zh ? "n =（卡牌上手n次未打出则失忆）" : "n = (forget after n unplayed draws)");

        _snapshotStatus.Text = T("panel_amnesia_status", zh ? "血量/金币失忆模式" : "HP/gold amnesia mode");
        _statusNumbers.Text = T("panel_status_numbers", zh ? "血量数/金币数恢复正常显示" : "Readable HP/gold numbers");
        _ownedRelics.Text = T("panel_owned_relics", zh ? "显示已拥有遗物" : "Show owned relics");
        _mapRoutes.Text = T("panel_map_routes", zh ? "显示地图所有路线" : "Show all map routes");

        _intentLabel.Text = T("panel_intent_label", zh ? "可见敌人意图" : "Enemy intents");
        var intentOptions = zh
            ? new[] { "不可见", "仅第一回合可见", "可见所有意图" }
            : new[] { "Hidden", "First round only", "Always visible" };
        var intentKeys = new[] { "panel_intent_0", "panel_intent_1", "panel_intent_2" };
        for (var i = 0; i < intentOptions.Length; i++)
        {
            _intentMode.SetItemText(i, T(intentKeys[i], intentOptions[i]));
        }

        _reset.Text = T("panel_reset", zh ? "重置为默认" : "Reset to defaults");
        _dock.TooltipText = T("panel_dock_tooltip", zh ? "缩进到屏幕边缘（点边缘小按钮恢复）" : "Dock to the screen edge (click the edge tab to restore)");
        _tab.TooltipText = T("panel_tab_tooltip", zh ? "显示认知修改器" : "Show the cognition modifier");
        UpdateBlurLabel();
        RefreshTip();

        // Re-apply the locale font (no-op for Latin; swaps in the CJK font when
        // the player switches to Chinese at runtime).
        foreach (var control in new Control[]
                 {
                     _title, _sectionText, _sectionCognition, _sectionPerception, _selectionLabel,
                     _selection, _shopEvent, _snapshotStatus, _statusNumbers, _ownedRelics, _mapRoutes,
                     _tip, _dock, _tab, _blurLabel, _saltLabel, _saltMode,
                     _memoryLabel, _memoryMode, _badNLabel, _badN, _intentLabel, _intentMode, _reset,
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
        control.MouseEntered += () => ShowTip(control, hintKey);
        control.MouseExited += HideTip;
    }

    /// <summary>Shows the description card next to the hovered control.</summary>
    private void ShowTip(Control anchor, string hintKey)
    {
        try
        {
            _currentHintKey = hintKey;
            _tipAnchor = anchor;
            _tip.Text = ResolveHintText(hintKey);
            _tip.Visible = true;
            _tip.ResetSize();
            PlaceTip(anchor);
        }
        catch (Exception ex)
        {
            PatchGuard.Run("DifficultyPanel.Tip", () => throw ex);
        }
    }

    private void HideTip()
    {
        _tip.Visible = false;
        _tipAnchor = null;
    }

    /// <summary>Re-shows the visible tip after a language change.</summary>
    private void RefreshTip()
    {
        if (_tip.Visible && _tipAnchor is { } anchor && GodotObject.IsInstanceValid(anchor))
        {
            ShowTip(anchor, _currentHintKey);
        }
    }

    private void PlaceTip(Control anchor)
    {
        var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        var size = _tip.Size;
        var x = _panel.Position.X + _panel.Size.X + 10f;
        if (x + size.X > viewportSize.X - 8f)
        {
            x = _panel.Position.X - size.X - 10f;
        }
        x = Math.Max(8f, x);
        var anchorY = anchor.GetGlobalRect().Position.Y;
        var y = Math.Clamp(anchorY, 8f, Math.Max(8f, viewportSize.Y - size.Y - 8f));
        _tip.Position = new Vector2(x, y);
    }

    private string ResolveHintText(string hintKey)
    {
        var zh = ModLocalization.IsChinese;
        var fallback = hintKey switch
        {
            "panel_hint_selection" => zh ? "奖励界面随机揭示部分卡面（每次奖励固定）" : "Randomly reveal some card faces in reward screens (fixed per reward)",
            "panel_hint_shop_event" => zh ? "商店与事件获得的卡牌显示真实牌面" : "Show real card faces for cards from shops and events",
            "panel_hint_memory_good" => zh ? "好记性：打出一张后本局永久揭示卡面；同名卡（含奖励/商店/事件）一并揭示" : "Good memory: playing a card reveals it for the run, including its copies in rewards/shops/events",
            "panel_hint_memory_bad" => zh ? "坏记性：某张卡每 n 次上手却未被打出，就变回未揭示状态" : "Bad memory: a copy drawn n times without being played reverts to unknown",
            "panel_hint_memory_omniscient" => zh ? "通晓万物：全部卡牌全局揭示" : "Omniscience: every card is revealed everywhere",
            "panel_hint_memory_nonsense" => zh ? "歪比巴卜：卡牌永不揭示" : "Nonsense: cards are never revealed",
            "panel_hint_salt" => zh ? "固定混乱：乱码不随重进变化；混乱混乱：每次重进游戏重新随机" : "Fixed: garbling never changes; Chaos: re-rolled on every launch",
            "panel_hint_amnesia_status" => zh ? "开启后血量与金币停留在上次休息时（灰显标注）" : "HP and gold stay at the values from your last rest (shown gray)",
            "panel_hint_status_numbers" => zh ? "开启后血量数与金币数不再乱码（实时/失忆模式不受影响）" : "HP and gold numbers are no longer garbled (live/amnesia mode unchanged)",
            "panel_hint_blur" => zh ? "所有文本的乱码程度（0% 完全可读，豁免项除外）" : "Garbling level for all text (0% readable, exemptions aside)",
            "panel_hint_owned_relics" => zh ? "库存与检视中显示已拥有遗物" : "Show owned relics in inventory and inspect screens",
            "panel_hint_map_routes" => zh ? "地图显示全部节点与路线" : "Show every map node and route",
            "panel_hint_intent" => zh ? "敌人意图：不可见 / 仅第一回合 / 每回合可见" : "Enemy intents: hidden / first round only / every round",
            "panel_hint_reset" => zh ? "把全部修改器选项恢复为默认值" : "Restore every modifier option to its default",
            _ => zh ? "拖动标题栏可移动 · 修改即时生效" : "Drag the title bar to move · changes apply instantly",
        };
        return T(hintKey, fallback);
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
            _snapshotStatus.ButtonPressed = settings.SnapshotStatus;
            _statusNumbers.ButtonPressed = settings.ReadableStatusNumbers;
            _blurSlider.Value = settings.TextBlurPercent;
            _saltMode.Selected = BlurSaltModeRules.ToIndex(settings.SaltMode);
            _memoryMode.Selected = CardMemoryModeRules.ToIndex(settings.MemoryMode);
            _badN.Value = settings.BadMemoryThreshold;
            UpdateBadNVisibility();
            _ownedRelics.ButtonPressed = settings.ShowOwnedRelics;
            _mapRoutes.ButtonPressed = settings.ShowAllMapRoutes;
            _intentMode.Selected = IntentVisibilityRules.ToIndex(settings.IntentMode);
            UpdateBlurLabel();
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

    private void OnSaltModeSelected(long index)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.SaltMode = BlurSaltModeRules.FromIndex((int)index);
        BlurSalt.PerLaunch = DifficultyRuntime.Current.SaltMode == BlurSaltMode.PerLaunch;
        _blurPending = true; // re-apply with the new salt
        DifficultyRuntime.Save();
    }

    private void OnMemoryModeSelected(long index)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.MemoryMode = CardMemoryModeRules.FromIndex((int)index);
        UpdateBadNVisibility();
        DifficultyRuntime.NotifyChanged();
    }

    private void OnBadNChanged(double value)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.BadMemoryThreshold = DifficultySettings.ClampBadMemoryThreshold((int)value);
        DifficultyRuntime.Save();
    }

    private void OnIntentModeSelected(long index)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.IntentMode = IntentVisibilityRules.FromIndex((int)index);
        DifficultyRuntime.NotifyChanged();
    }

    private void OnReset()
    {
        DifficultyRuntime.Current.ApplyDefaults();
        BlurSalt.PerLaunch = true;
        ApplyFromSettings();
        _blurPending = true;
        DifficultyRuntime.NotifyChanged();
    }

    private void UpdateBadNVisibility()
    {
        var bad = DifficultyRuntime.Current.MemoryMode == CardMemoryMode.BadMemory;
        _badNLabel.Visible = bad;
        _badN.Visible = bad;
    }

    private static string MemoryHintKey() => DifficultyRuntime.Current.MemoryMode switch
    {
        CardMemoryMode.Omniscient => "panel_hint_memory_omniscient",
        CardMemoryMode.BadMemory => "panel_hint_memory_bad",
        CardMemoryMode.Nonsense => "panel_hint_memory_nonsense",
        _ => "panel_hint_memory_good",
    };

    private void OnSnapshotStatusToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.SnapshotStatus = pressed;
        DifficultyRuntime.NotifyChanged();
    }

    private void OnStatusNumbersToggled(bool pressed)
    {
        if (_applying)
        {
            return;
        }
        DifficultyRuntime.Current.ReadableStatusNumbers = pressed;
        _blurPending = true; // restore / re-blur the HP and gold numbers
        DifficultyRuntime.NotifyChanged();
    }

    private void OnBlurChanged(double value)
    {
        if (_applying)
        {
            return;
        }
        var percent = DifficultySettings.ClampBlurPercent((int)value);
        DifficultyRuntime.Current.TextBlurPercent = percent;
        UpdateBlurLabel();
        _blurDirty = true;
        _blurSaveTimer = 0;
        _blurPending = true;
    }

    private void UpdateBlurLabel()
    {
        var zh = ModLocalization.IsChinese;
        var label = T("panel_blur_label", zh ? "乱码百分比" : "Blur percentage");
        var percent = DifficultyRuntime.TextBlurPercent;
        _blurLabel.Text = zh ? $"{label}：{percent}%" : $"{label}: {percent}%";
    }

    private void SaveBlur()
    {
        _blurDirty = false;
        DifficultyRuntime.Save();
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

    private void ToggleCollapsed()
    {
        HideTip();
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
        HideTip();
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
