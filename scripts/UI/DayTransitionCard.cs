using System;
using System.Collections.Generic;
using System.Linq;
using ChikagoShop.Domain.Days;
using Godot;

// Selection only; effects and persistence are owned by the day controller/domain.
public partial class DayTransitionCard : CanvasLayer
{
    private Control _root = null!;
    private PanelContainer _panel = null!;
    private Label _route = null!;
    private Button _firstChoice = null!;
    private HBoxContainer _choices = null!;
    public IReadOnlyList<DayUpgrade> Options { get; private set; } = Array.Empty<DayUpgrade>();
    public event Action<DayUpgrade> Chosen;
    public event Action Skipped;
    private Button _skip = null!;
    private int _balance;
    private bool _ownsPause;
    public bool IsOpen => _root?.Visible ?? false;

    public override void _Ready()
    {
        Name = "DayTransitionCard";
        Layer = 120;
        ProcessMode = ProcessModeEnum.Always;
        _root = new Control { Name = "Root", Visible = false, Theme = GD.Load<Theme>("res://themes/game_theme.tres") };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color("101516"), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center = new CenterContainer { Name = "Center" };
        _root.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel = new PanelContainer { Name = "Card" };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("1b2122"), BorderColor = new Color("a88b58"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 3, BorderWidthBottom = 1,
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 24, ContentMarginBottom = 24,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        });
        center.AddChild(_panel);
        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 12);
        _panel.AddChild(content);
        _route = AddLabel(content, "Route", "", 22, "d7b67b");
        AddLabel(content, "Title", "Выберите улучшение", 30, "fff1d8");
        _choices = new HBoxContainer { Name = "Choices" };
        _choices.AddThemeConstantOverride("separation", 14);
        content.AddChild(_choices);
        _skip = new Button { Name = "Skip", Text = "Пропустить", CustomMinimumSize = new Vector2(0, 36) };
        content.AddChild(_skip);
        _skip.Pressed += Skip;
        _root.Resized += Layout;
        Layout();
    }

    private Button AddChoice(Node parent, DayUpgrade upgrade, string title, string amount, string description, string duration)
    {
        var panel = new PanelContainer { Name = upgrade.ToString(), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("252d2c"), BorderColor = new Color("756443"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 16, ContentMarginBottom = 16,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });
        parent.AddChild(panel);
        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);
        AddLabel(content, "Amount", amount, 38, "d7b67b");
        AddLabel(content, "Title", title, 22, "fff1d8").CustomMinimumSize = new Vector2(0, 52);
        AddLabel(content, "Description", description, 18, "d5ccbb").CustomMinimumSize = new Vector2(0, 72);
        AddLabel(content, "Duration", duration, 14, "a88b58").CustomMinimumSize = new Vector2(0, 34);
        int cost = DayUpgradeSession.Cost(upgrade);
        var button = new Button { Name = "Choose", Text = GameLocalization.Format("Купить за {0}$", cost),
            Disabled = _balance < cost, CustomMinimumSize = new Vector2(0, 44) };
        content.AddChild(button);
        button.Pressed += () => Choose(upgrade);
        return button;
    }

    private static Label AddLabel(Node parent, string name, string text, int size, string color)
    {
        var label = new Label { Name = name, Text = text, HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(color));
        parent.AddChild(label);
        return label;
    }

    private void Layout() => _panel.CustomMinimumSize = new Vector2(Mathf.Min(1080, Mathf.Max(280, _root.Size.X - 48)), 0);

    public void Open(int previousDay, int nextDay, IReadOnlyList<DayUpgrade> options, int balance)
    {
        if (IsOpen || previousDay is < 1 or > 6 || nextDay != previousDay + 1 || GetTree().Paused) return;
        Options = options.ToArray();
        _balance = balance;
        _firstChoice = _skip;
        foreach (Node child in _choices.GetChildren()) child.Free();
        foreach (var upgrade in Options)
        {
            var button = upgrade switch
            {
                DayUpgrade.Cash => AddChoice(_choices, upgrade, "Оборотные средства", "+50$", "Получить 50$ сразу. Деньги остаются на счёте.", "СРАЗУ"),
                DayUpgrade.Energy => AddChoice(_choices, upgrade, "Второе дыхание", "+1", "Добавить 1 энергию к следующему дню.", "ЭНЕРГИЯ · НА ОДИН ДЕНЬ"),
                DayUpgrade.Time => AddChoice(_choices, upgrade, "Длинная смена", "+30с", "Добавить 30 секунд к следующему дню.", "ВРЕМЯ · НА ОДИН ДЕНЬ"),
                DayUpgrade.Atelier => AddChoice(_choices, upgrade, "Мастерство портного", "↑", "Больше редкой одежды до конца игры.", "ОДЕЖДА · ДО КОНЦА ИГРЫ"),
                DayUpgrade.Music => AddChoice(_choices, upgrade, "Мастерство мастера", "↑", "Больше редких инструментов до конца игры.", "ИНСТРУМЕНТЫ · ДО КОНЦА ИГРЫ"),
                DayUpgrade.AtelierPrice => AddChoice(_choices, upgrade, "Имя на этикетке", "+20%", "Ещё +20% к базовой стоимости всей одежды.", "ОДЕЖДА · ДО КОНЦА ИГРЫ"),
                DayUpgrade.MusicPrice => AddChoice(_choices, upgrade, "Коллекционный звук", "+20%", "Ещё +20% к базовой стоимости всех инструментов.", "ИНСТРУМЕНТЫ · ДО КОНЦА ИГРЫ"),
                _ => throw new ArgumentOutOfRangeException(nameof(options))
            };
            if (_firstChoice == _skip && !button.Disabled) _firstChoice = button;
        }
        _route.Text = $"{previousDay:00}  →  {nextDay:00}  ·  " + GameLocalization.Format("Средства: {0}$", balance);
        _ownsPause = true;
        GetTree().Paused = true;
        _root.Show();
        _firstChoice.GrabFocus();
    }

    public void Choose(DayUpgrade upgrade)
    {
        if (IsOpen && Options.Contains(upgrade) && _balance >= DayUpgradeSession.Cost(upgrade)) Chosen?.Invoke(upgrade);
    }

    public void Skip()
    {
        if (IsOpen) Skipped?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        _root.Hide();
        if (_ownsPause) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen || @event is not InputEventKey { Pressed: true } key) return;
        DayUpgrade upgrade = key.Keycode switch
        {
            Key.Key1 or Key.Kp1 => Options[0],
            Key.Key2 or Key.Kp2 => Options[1],
            Key.Key3 or Key.Kp3 => Options[2],
            _ => DayUpgrade.None
        };
        if (upgrade != DayUpgrade.None || key.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            if (!key.Echo)
            {
                if (key.Keycode == Key.Escape) Skip();
                else Choose(upgrade);
            }
        }
    }

    public override void _ExitTree()
    {
        if (_ownsPause) GetTree().Paused = false;
    }
}
