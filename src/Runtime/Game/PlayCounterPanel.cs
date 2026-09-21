using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog.Game;

/// <summary>
/// Top-right play-count leaderboard (user change 2026-09-21, optional):
/// shows every copy that has been played at least once, sorted by play count
/// descending, labelled with its stable copy number ("打击1 ×3"). Collapsible;
/// only visible while the option is on and a run is in progress.
/// </summary>
internal sealed partial class PlayCounterPanel : CanvasLayer
{
    private static readonly Color Accent = new(0.93f, 0.79f, 0.42f);
    private static readonly Color AccentBright = new(1f, 0.90f, 0.60f);
    private static readonly Color RowColor = new(0.90f, 0.91f, 0.94f);
    private static readonly Color CountColor = new(0.98f, 0.86f, 0.52f);
    private static readonly Color MutedColor = new(0.62f, 0.65f, 0.70f);

    private const double LocalePollSeconds = 0.5;

    private PanelContainer _panel = null!;
    private VBoxContainer _body = null!;
    private VBoxContainer _rows = null!;
    private ScrollContainer _scroll = null!;
    private Label _title = null!;
    private Button _collapse = null!;

    private bool _dirty = true;
    private string _lastLocale = string.Empty;
    private double _localeTimer;

    public override void _Ready()
    {
        try
        {
            Layer = 94;
            Build();
            Localize();
            ApplyCollapsed();
            PlayCounterTracker.Changed += MarkDirty;
        }
        catch (System.Exception ex)
        {
            PatchGuard.Run("PlayCounterPanel.Ready", () => throw ex);
        }
    }

    public override void _ExitTree()
    {
        PlayCounterTracker.Changed -= MarkDirty;
    }

    public override void _Process(double delta)
    {
        var show = !ModRuntime.Disabled
            && DifficultyRuntime.Current.PlayCounter
            && IsRunInProgress();
        Visible = show;
        if (!show)
        {
            return;
        }

        _localeTimer += delta;
        if (_localeTimer >= LocalePollSeconds)
        {
            _localeTimer = 0;
            if (ModLocalization.CurrentLanguageCode != _lastLocale)
            {
                Localize();
            }
        }

        if (_dirty)
        {
            _dirty = false;
            RefreshRows();
        }
    }

    private void MarkDirty() => _dirty = true;

    private static bool IsRunInProgress()
    {
        try
        {
            return RunManager.Instance is { IsInProgress: true };
        }
        catch
        {
            return false;
        }
    }

    private void Build()
    {
        _panel = new PanelContainer
        {
            Name = "BrainFogPlayCounterRoot",
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(232f, 0f),
        };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.055f, 0.065f, 0.09f, 0.90f),
            BorderColor = new Color(0.55f, 0.50f, 0.34f, 0.60f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 12f,
            ContentMarginRight = 12f,
            ContentMarginTop = 8f,
            ContentMarginBottom = 10f,
            ShadowColor = new Color(0f, 0f, 0f, 0.35f),
            ShadowSize = 8,
            ShadowOffset = new Vector2(0f, 3f),
        });
        _panel.AnchorLeft = 1f;
        _panel.AnchorRight = 1f;
        _panel.AnchorTop = 0f;
        _panel.AnchorBottom = 0f;
        _panel.OffsetLeft = -244f;
        _panel.OffsetRight = -12f;
        _panel.OffsetTop = 92f;
        _panel.OffsetBottom = 92f;

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        root.AddChild(BuildHeader());

        _body = new VBoxContainer { Name = "BrainFogPlayCounterBody" };
        _body.AddThemeConstantOverride("separation", 4);

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 3);
        _scroll.AddChild(_rows);
        _body.AddChild(_scroll);

        root.AddChild(_body);
        _panel.AddChild(root);
        AddChild(_panel);
    }

    private Control BuildHeader()
    {
        var header = new HBoxContainer { Name = "BrainFogPlayCounterHeader" };

        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 16);
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
            CustomMinimumSize = new Vector2(26f, 0f),
        };
        _collapse.AddThemeFontSizeOverride("font_size", 14);
        _collapse.AddThemeColorOverride("font_color", Accent);
        _collapse.AddThemeColorOverride("font_hover_color", AccentBright);
        _collapse.AddThemeColorOverride("font_pressed_color", AccentBright);
        _collapse.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        _collapse.Pressed += ToggleCollapsed;
        header.AddChild(_collapse);

        return header;
    }

    private void Localize()
    {
        _lastLocale = ModLocalization.CurrentLanguageCode;
        var zh = ModLocalization.IsChinese;
        _title.Text = ModLocalization.Panel("panel_counter_title", zh ? "出牌计数" : "Play counts");
        _collapse.TooltipText = ModLocalization.Panel(
            "panel_counter_collapse", zh ? "收起 / 展开榜单" : "Collapse / expand the leaderboard");
        foreach (var control in new Control[] { _title, _collapse })
        {
            control.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        }
        MarkDirty();
    }

    private void ApplyCollapsed()
    {
        var collapsed = DifficultyRuntime.CounterCollapsed;
        _body.Visible = !collapsed;
        _collapse.Text = collapsed ? "▸" : "▾";
    }

    private void ToggleCollapsed()
    {
        var collapsed = _body.Visible;
        _body.Visible = !collapsed;
        _collapse.Text = collapsed ? "▸" : "▾";
        DifficultyRuntime.SetCounterCollapsed(collapsed);
    }

    private void RefreshRows()
    {
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }

        var rows = PlayCounterTracker.GetRows();
        if (rows.Count == 0)
        {
            _rows.AddChild(BuildEmptyLabel());
        }
        else
        {
            foreach (var row in rows)
            {
                _rows.AddChild(BuildRow(row));
            }
        }
        ResizeScroll();
    }

    private Label BuildEmptyLabel()
    {
        var zh = ModLocalization.IsChinese;
        var label = new Label
        {
            Text = ModLocalization.Panel("panel_counter_empty", zh ? "暂无出牌记录" : "No plays yet"),
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", MutedColor);
        label.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        return label;
    }

    private Control BuildRow(PlayCounterTracker.Row row)
    {
        var line = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

        var name = new Label
        {
            Text = row.Label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        name.AddThemeFontSizeOverride("font_size", 14);
        name.AddThemeColorOverride("font_color", RowColor);
        name.ApplyLocaleFontSubstitution(FontType.Regular, "font");

        var count = new Label { Text = $"×{row.Count}" };
        count.AddThemeFontSizeOverride("font_size", 14);
        count.AddThemeColorOverride("font_color", CountColor);
        count.ApplyLocaleFontSubstitution(FontType.Regular, "font");

        line.AddChild(name);
        line.AddChild(count);
        return line;
    }

    /// <summary>Grows with the content up to ~42% of the screen, then scrolls.</summary>
    private void ResizeScroll()
    {
        var viewport = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        var content = _rows.GetCombinedMinimumSize().Y;
        var max = Mathf.Max(80f, viewport.Y * 0.42f);
        _scroll.CustomMinimumSize = new Vector2(0f, Mathf.Min(content, max));
    }
}
