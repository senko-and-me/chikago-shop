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
    private DayCardSceneTransition _sceneTransition = null!;
    private Label _title = null!;
    private const float CardHeight = 460;
    private enum Phase { Closed, Framing, Dealing, Choosing, Leaving }
    private Phase _phase;
    private int _generation;
    private float _layoutScale = 1;
    public bool IsAnimating => _phase is Phase.Framing or Phase.Dealing or Phase.Leaving;
    public IReadOnlyList<DayUpgrade> Options { get; private set; } = Array.Empty<DayUpgrade>();
    public event Action<DayUpgrade> Chosen;
    public event Action Skipped;
    private Button _skip = null!;
    private int _balance;
    private int _previousDay, _nextDay;
    private bool _ownsPause;
    private bool _instructionLocked;

    internal void SetInstructionLocked(bool locked)
    {
        if (_instructionLocked == locked) return;
        _instructionLocked = locked;
        if (_phase == Phase.Choosing) LockActions(locked);
    }
    public bool IsOpen => _root?.Visible ?? false;

    public override void _Notification(int what)
    {
        if (what != NotificationTranslationChanged || !IsNodeReady() || !IsOpen) return;
        _route.Text = $"{_previousDay:00}  →  {_nextDay:00}  ·  " + GameLocalization.Format("Средства: {0}$", _balance);
        for (int i = 0; i < _cards.Count; i++) PopulateChoice(_cards[i], Options[i]);
    }

    private DayUpgradeRules Rules => (GetParent() as TradeScene)?.DayUpgrades.Rules ?? DebugBalanceSettings.CardRules;

    private void PopulateChoice(PanelContainer panel, DayUpgrade upgrade)
    {
        var terms = Rules.For(upgrade);
        string amount = upgrade switch
        {
            DayUpgrade.Materials => GameLocalization.Format("−{0}$ / товар", terms.Amount),
            DayUpgrade.Patient => GameLocalization.Format("+{0} сек.", terms.Amount),
            DayUpgrade.Bargaining => $"≥ +{terms.Amount}%",
            DayUpgrade.Loyal => $"−{terms.Amount}%",
            _ => GameLocalization.Format("+{0}$", terms.Amount)
        };
        panel.GetNode<Label>("Content/Amount").Text = amount;
        panel.GetNode<Label>("Content/Title").Text = GameLocalization.Text(DebugBalanceSettings.CardName(upgrade));
        panel.GetNode<Label>("Content/Description").Text = Describe(upgrade, terms);
        panel.GetNode<Label>("Content/Duration").Text = GameLocalization.Text(upgrade == DayUpgrade.Cash ? "СРАЗУ" : "НА ОДИН ДЕНЬ");
        panel.GetNode<Button>("Content/Choose").Text = GameLocalization.Format("Купить за {0}$", Cost(upgrade));
    }

    internal static string Describe(DayUpgrade upgrade, CardTerms terms) => upgrade switch
    {
        DayUpgrade.Cash => GameLocalization.Format("Сразу получите {0}$ после покупки.", terms.Amount),
        DayUpgrade.Materials => GameLocalization.Format("−{0}$ к стоимости каждого создания.", terms.Amount),
        DayUpgrade.AtelierPrice => GameLocalization.Format("+{0}$ за каждую продажу одежды.", terms.Amount),
        DayUpgrade.MusicPrice => GameLocalization.Format("+{0}$ за каждую продажу инструмента.", terms.Amount),
        DayUpgrade.Patient => GameLocalization.Format("Новые покупатели ждут на {0} сек. дольше.", terms.Amount),
        DayUpgrade.Bargaining => GameLocalization.Format("Последняя смена покупателя даёт не меньше +{0}%, пока есть время.", terms.Amount),
        DayUpgrade.QualityMark => GameLocalization.Format("+{0}$ за каждый товар качества B, A или S.", terms.Amount),
        DayUpgrade.Loyal => GameLocalization.Format("После таймера: −{0}% или прежнее предложение, если оно ниже.", terms.Amount),
        DayUpgrade.QuickService => GameLocalization.Format("+{0}$ за каждую продажу вовремя.", terms.Amount),
        _ => ""
    };

    private int Cost(DayUpgrade upgrade) => (GetParent() as TradeScene)?.DayUpgrades.CostFor(upgrade)
        ?? DebugBalanceSettings.CardRules.Cost(upgrade);

    public override void _Ready()
    {
        _root = GetNode<Control>("Root");
        _panel = GetNode<PanelContainer>("Root/Center/Card");
        _route = GetNode<Label>("Root/Center/Card/Content/Route");
        _title = GetNode<Label>("Root/Center/Card/Content/Title");
        _choices = GetNode<Control>("Root/Center/Card/Content/Choices");
        _skip = GetNode<Button>("Root/Center/Card/Content/Skip");
        _shade = GetNode<ColorRect>("Root/Shade");
        _deck = GetNode<PokerCardBack>("Root/Deck");
        _animation = new DayCardDealAnimator(this);
        _sceneTransition = new DayCardSceneTransition(this);
        _skip.Pressed += Skip;
        _root.Resized += OnResize;
        Layout();
    }

    private Button AddChoice(Node parent, DayUpgrade upgrade)
    {
        var panel = GD.Load<PackedScene>("res://scenes/UI/DayUpgradeCard.tscn").Instantiate<PanelContainer>();
        panel.Name = upgrade.ToString();
        parent.AddChild(panel);
        _cards.Add(panel);
        PopulateChoice(panel, upgrade);
        int cost = Cost(upgrade);
        var button = panel.GetNode<Button>("Content/Choose");
        button.Disabled = _balance < cost;
        button.Pressed += () => Choose(upgrade);
        return button;
    }

    private void Layout()
    {
        _layoutScale = Mathf.Clamp(_root.Size.Y / 720, 0.6f, 1);
        _panel.CustomMinimumSize = new Vector2(Mathf.Min(1080 * _layoutScale, Mathf.Max(280, _root.Size.X - 48)), 0);
        var style = (StyleBoxFlat)_panel.GetThemeStylebox("panel");
        style.ContentMarginLeft = style.ContentMarginRight = style.ContentMarginTop = style.ContentMarginBottom = 24 * _layoutScale;
        _route.AddThemeFontSizeOverride("font_size", (int)(22 * _layoutScale));
        _title.AddThemeFontSizeOverride("font_size", (int)(30 * _layoutScale));
        _skip.CustomMinimumSize = new Vector2(252 * _layoutScale, 45 * _layoutScale);
        _choices.CustomMinimumSize = new Vector2(0, CardHeight * _layoutScale);
        _deck.Size = new Vector2(100, 144) * _layoutScale;
        _deck.Position = new Vector2(_deck.Position.X, _root.Size.Y - _deck.Size.Y - 28);
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
            var cardStyle = card.GetThemeStylebox("panel");
            cardStyle.ContentMarginLeft = cardStyle.ContentMarginRight = 30 * _layoutScale;
            cardStyle.ContentMarginTop = cardStyle.ContentMarginBottom = 26 * _layoutScale;
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
            card.CustomMinimumSize = new Vector2(width, CardHeight * _layoutScale);
            card.Size = card.CustomMinimumSize;
            card.Position = new Vector2(start + i * (width + gap), 0);
            card.PivotOffset = card.Size / 2;
        }
        if (_cards.Count > 0)
        {
            Vector2 firstCard = _root.GetGlobalTransformWithCanvas().AffineInverse() *
                (_choices.GetGlobalTransformWithCanvas() * new Vector2(start, 0));
            _deck.Position = new Vector2(Mathf.Max(0, (firstCard.X - _deck.Size.X) / 2), _deck.Position.Y);
        }
    }

    private void OnResize()
    {
        Layout();
        if (IsOpen && _phase == Phase.Framing) PrepareDeal(_generation);
        else if (IsOpen && _phase != Phase.Leaving) Reflow(_generation);
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
            button.Disabled = locked || (button != _skip && _balance < Cost(Options[_navigationButtons.IndexOf(button)]));
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

    private void PrepareDeal(int generation)
    {
        _sceneTransition.Begin(() =>
        {
            if (generation != _generation || !IsOpen) return;
            _phase = Phase.Dealing;
            Reflow(generation);
        });
    }

    public void Open(int previousDay, int nextDay, IReadOnlyList<DayUpgrade> options, int balance)
    {
        if (IsOpen || previousDay < 1 || nextDay != previousDay + 1 || GetTree().Paused) return;
        Options = options.Where(DayUpgradeRules.Available.Contains).Distinct().Take(3).ToArray();
        _previousDay = previousDay;
        _nextDay = nextDay;
        _balance = balance;
        _navigationButtons.Clear();
        _cards.Clear();
        foreach (Node child in _choices.GetChildren()) child.Free();
        foreach (var upgrade in Options)
        {
            var button = AddChoice(_choices, upgrade);
            _navigationButtons.Add(button);
        }
        _navigationButtons.Add(_skip);
        _route.Text = $"{previousDay:00}  →  {nextDay:00}  ·  " + GameLocalization.Format("Средства: {0}$", balance);
        _ownsPause = true;
        _phase = Phase.Framing;
        int generation = ++_generation;
        GetTree().Paused = true;
        _shade.Modulate = _panel.Modulate = _deck.Modulate = new Color(1, 1, 1, 0);
        _choices.Modulate = new Color(1, 1, 1, 0);
        _root.Show();
        LockActions(true);
        Layout();
        PrepareDeal(generation);
        // Opening the screen with the mouse must not preselect a purchase.
        GetViewport().GuiReleaseFocus();
    }

    public void Choose(DayUpgrade upgrade)
    {
        if (_phase != Phase.Choosing || _instructionLocked || !Options.Contains(upgrade) || _balance < Cost(upgrade)) return;
        Leave(_cards[Options.ToList().IndexOf(upgrade)], () => Chosen?.Invoke(upgrade));
    }

    public void Skip()
    {
        if (_phase == Phase.Choosing && !_instructionLocked) Leave(null, () => Skipped?.Invoke());
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
        _sceneTransition.Restore();
        _phase = Phase.Closed;
        _instructionLocked = false;
        _root.Hide();
        if (_ownsPause) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen || _instructionLocked || @event is not InputEventKey { Pressed: true } key) return;
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
        _sceneTransition?.Restore();
        if (_ownsPause) GetTree().Paused = false;
    }
}
