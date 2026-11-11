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
    private readonly List<Button> _navigationButtons = new();
    private Control _choices = null!;
    private readonly List<PanelContainer> _cards = new();
    private ColorRect _shade = null!;
    private PokerCardBack _deck = null!;
    private DayCardDealAnimator _animation = null!;
    private Label _title = null!;
    private enum Phase { Closed, Dealing, Choosing, Leaving }
    private Phase _phase;
    private int _generation;
    private float _layoutScale = 1;
    public bool IsAnimating => _phase is Phase.Dealing or Phase.Leaving;
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
        MonochromePresentation.CoverExceptions(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _animation = new DayCardDealAnimator(this);
        _shade = new ColorRect { Name = "Shade", Color = new Color(0, 0, 0, 0.64f), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _deck = new PokerCardBack { Name = "Deck", IsDeck = true };
        _root.AddChild(_deck);
        var center = new CenterContainer { Name = "Center" };
        _root.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel = new PanelContainer { Name = "Card" };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 24, ContentMarginBottom = 24,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
        });
        center.AddChild(_panel);
        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 12);
        _panel.AddChild(content);
        _route = AddLabel(content, "Route", "", 22, "eeeeee");
        _title = AddLabel(content, "Title", "Выберите улучшение", 30, "ffffff");
        // Free layout lets the cards move without fighting a Container's sorting pass.
        _choices = new Control { Name = "Choices", MouseFilter = Control.MouseFilterEnum.Ignore };
        content.AddChild(_choices);
        _skip = new Button { Name = "Skip", Text = "Пропустить", CustomMinimumSize = new Vector2(0, 36) };
        content.AddChild(_skip);
        _skip.Pressed += Skip;
        _root.Resized += OnResize;
        Layout();
    }

    private Button AddChoice(Node parent, DayUpgrade upgrade, string title, string amount, string description, string duration)
    {
        var panel = new PanelContainer { Name = upgrade.ToString(), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("f3f1eb"), BorderColor = new Color("202020"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 16, ContentMarginBottom = 16,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });
        parent.AddChild(panel);
        _cards.Add(panel);
        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);
        AddLabel(content, "Amount", amount, 38, "202020");
        AddLabel(content, "Title", title, 22, "202020").CustomMinimumSize = new Vector2(0, 52);
        AddLabel(content, "Description", description, 18, "3c3c3c").CustomMinimumSize = new Vector2(0, 72);
        content.AddChild(new Control { Name = "Spacer", SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore });
        AddLabel(content, "Duration", duration, 14, "555555").CustomMinimumSize = new Vector2(0, 34);
        int cost = DayUpgradeSession.Cost(upgrade);
        var button = new Button { Name = "Choose", Text = GameLocalization.Format("Купить за {0}$", cost),
            Disabled = _balance < cost, CustomMinimumSize = new Vector2(0, 44) };
        content.AddChild(button);
        button.Pressed += () => Choose(upgrade);
        panel.AddChild(new PokerCardBack { Name = "Back", Visible = false });
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

    private void Layout()
    {
        _layoutScale = Mathf.Clamp(_root.Size.Y / 720, 0.6f, 1);
        _panel.CustomMinimumSize = new Vector2(Mathf.Min(1080 * _layoutScale, Mathf.Max(280, _root.Size.X - 48)), 0);
        var style = (StyleBoxFlat)_panel.GetThemeStylebox("panel");
        style.ContentMarginLeft = style.ContentMarginRight = style.ContentMarginTop = style.ContentMarginBottom = 24 * _layoutScale;
        _route.AddThemeFontSizeOverride("font_size", (int)(22 * _layoutScale));
        _title.AddThemeFontSizeOverride("font_size", (int)(30 * _layoutScale));
        _skip.CustomMinimumSize = new Vector2(0, 36 * _layoutScale);
        _choices.CustomMinimumSize = new Vector2(0, 460 * _layoutScale);
        _deck.Size = new Vector2(100, 144) * _layoutScale;
        _deck.Position = new Vector2(Mathf.Max(20, _root.Size.X * 0.04f), _root.Size.Y - _deck.Size.Y - 28);
        foreach (var card in _cards)
        {
            var content = card.GetNode<VBoxContainer>("Content");
            content.AddThemeConstantOverride("separation", (int)(8 * _layoutScale));
            foreach (var entry in new[] { ("Amount", 38, 0), ("Title", 22, 52), ("Description", 18, 72), ("Duration", 14, 34) })
            {
                var label = content.GetNode<Label>(entry.Item1);
                label.AddThemeFontSizeOverride("font_size", (int)(entry.Item2 * _layoutScale));
                label.CustomMinimumSize = new Vector2(0, entry.Item3 * _layoutScale);
            }
            var cardStyle = (StyleBoxFlat)card.GetThemeStylebox("panel");
            cardStyle.ContentMarginLeft = cardStyle.ContentMarginRight = 18 * _layoutScale;
            cardStyle.ContentMarginTop = cardStyle.ContentMarginBottom = 16 * _layoutScale;
            content.GetNode<Button>("Choose").CustomMinimumSize = new Vector2(0, 44 * _layoutScale);
        }
    }

    private void PlaceCards()
    {
        float gap = 20 * _layoutScale;
        float width = Mathf.Min(320 * _layoutScale, (_choices.Size.X - gap * (_cards.Count - 1)) / Mathf.Max(1, _cards.Count));
        float start = (_choices.Size.X - width * _cards.Count - gap * (_cards.Count - 1)) / 2;
        for (int i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            card.CustomMinimumSize = new Vector2(width, 460 * _layoutScale);
            card.Size = card.CustomMinimumSize;
            card.Position = new Vector2(start + i * (width + gap), 0);
            card.PivotOffset = card.Size / 2;
        }
    }

    private void OnResize()
    {
        Layout();
        if (IsOpen && _phase != Phase.Leaving) Reflow(_generation);
    }

    private async void Reflow(int generation)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || generation != _generation) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || generation != _generation || !IsOpen || _phase == Phase.Leaving) return;
        _animation.Cancel();
        PlaceCards();
        if (_phase == Phase.Dealing) StartDeal(generation);
    }

    private void LockActions(bool locked)
    {
        foreach (var button in _navigationButtons)
        {
            button.Disabled = locked || (button != _skip && _balance < DayUpgradeSession.Cost(Options[_navigationButtons.IndexOf(button)]));
            button.FocusMode = locked ? Control.FocusModeEnum.None : Control.FocusModeEnum.All;
        }
    }

    private void StartDeal(int generation)
    {
        _choices.Modulate = Colors.White;
        _animation.Deal(_cards, _deck, _shade, _panel, () =>
        {
            if (generation != _generation || !IsOpen) return;
            _phase = Phase.Choosing;
            LockActions(false);
        });
    }

    public void Open(int previousDay, int nextDay, IReadOnlyList<DayUpgrade> options, int balance)
    {
        if (IsOpen || previousDay is < 1 or > 6 || nextDay != previousDay + 1 || GetTree().Paused) return;
        Options = options.ToArray();
        _balance = balance;
        _navigationButtons.Clear();
        _cards.Clear();
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
            _navigationButtons.Add(button);
        }
        _navigationButtons.Add(_skip);
        _route.Text = $"{previousDay:00}  →  {nextDay:00}  ·  " + GameLocalization.Format("Средства: {0}$", balance);
        _ownsPause = true;
        _phase = Phase.Dealing;
        int generation = ++_generation;
        GetTree().Paused = true;
        _shade.Modulate = _panel.Modulate = _deck.Modulate = new Color(1, 1, 1, 0);
        _choices.Modulate = new Color(1, 1, 1, 0);
        _root.Show();
        LockActions(true);
        Layout();
        Reflow(generation);
        // Opening the screen with the mouse must not preselect a purchase.
        GetViewport().GuiReleaseFocus();
    }

    public void Choose(DayUpgrade upgrade)
    {
        if (_phase != Phase.Choosing || !Options.Contains(upgrade) || _balance < DayUpgradeSession.Cost(upgrade)) return;
        Leave(_cards[Options.ToList().IndexOf(upgrade)], () => Chosen?.Invoke(upgrade));
    }

    public void Skip()
    {
        if (_phase == Phase.Choosing) Leave(null, () => Skipped?.Invoke());
    }

    private void Leave(PanelContainer selected, Action action)
    {
        _phase = Phase.Leaving;
        LockActions(true);
        GetViewport().GuiReleaseFocus();
        int generation = _generation;
        _animation.Leave(_cards, selected, _deck, _shade, _panel, () =>
        {
            if (generation != _generation || !IsOpen) return;
            try { action(); }
            finally { if (generation == _generation) Close(); }
        });
    }

    public void Close()
    {
        if (!IsOpen) return;
        ++_generation;
        _animation.Cancel();
        _phase = Phase.Closed;
        _root.Hide();
        if (_ownsPause) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen || @event is not InputEventKey { Pressed: true } key) return;
        if (_phase != Phase.Choosing)
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (key.Keycode is Key.Left or Key.Right or Key.Up or Key.Down or Key.Tab)
        {
            ButtonFeedbackController.Instance?.UseKeyboardNavigation();
            var focused = GetViewport().GuiGetFocusOwner();
            var available = _navigationButtons.Where(button => !button.Disabled && button.IsVisibleInTree()).ToArray();
            int current = Array.IndexOf(available, focused);
            bool previous = key.Keycode is Key.Left or Key.Up || (key.Keycode == Key.Tab && key.ShiftPressed);
            if (available.Length > 0 && (current >= 0 || !key.Echo))
                available[current < 0 ? 0 : (current + (previous ? -1 : 1) + available.Length) % available.Length].GrabFocus();
            GetViewport().SetInputAsHandled();
            return;
        }
        DayUpgrade upgrade = key.Keycode switch
        {
            Key.Key1 or Key.Kp1 => Options.ElementAtOrDefault(0),
            Key.Key2 or Key.Kp2 => Options.ElementAtOrDefault(1),
            Key.Key3 or Key.Kp3 => Options.ElementAtOrDefault(2),
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
        ++_generation;
        _animation?.Cancel();
        if (_ownsPause) GetTree().Paused = false;
    }
}
