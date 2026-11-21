#nullable enable
using System;
using System.Linq;
using ChikagoShop.Domain.Days;
using Godot;

// The pull-out notebook shows real objectives, today's active card bonuses and,
// only when the game implements any, negative effects. It does not change gameplay.
public partial class PulloutNotebook : Control
{
    [Export] public float ClosedX { get; set; } = 266f;
    [Export] public float HiddenX { get; set; } = 310f;
    [Export] public float OpenX { get; set; } = 14f;
    [Export] public float SlideSeconds { get; set; } = 0.34f;

    private TradeScene _game = null!;
    private TextureRect _page = null!;
    private Control _notes = null!;
    private Label _questsTitle = null!;
    private Label _questDetails = null!;
    private Label _buffsTitle = null!;
    private Label _buffDetails = null!;
    private Label _debuffsTitle = null!;
    private Label _debuffDetails = null!;
    private Tween? _slide;
    private bool _expanded;
    private float _targetX = float.NaN;
    public bool Expanded => _expanded;
    private bool _pinned;
    private double _leaveTime;
    private string _lastSignature = string.Empty;
    private Vector2 _lastViewportSize = Vector2.Zero;

    private void UpdateHudAnchor(bool force = false)
    {
        Vector2 viewport = GetViewportRect().Size;
        if (!force && viewport == _lastViewportSize)
            return;
        _lastViewportSize = viewport;
        Position = new Vector2(viewport.X - Size.X - 24f, 204f);
    }


    public override void _Ready()
    {
        _game = GetNode<TradeScene>("../../..");
        _page = GetNode<TextureRect>("Page");
        _notes = GetNode<Control>("Page/Notes");
        _questsTitle = GetNode<Label>("Page/Notes/QuestsTitle");
        _questDetails = GetNode<Label>("Page/Notes/QuestDetails");
        _buffsTitle = GetNode<Label>("Page/Notes/BuffsTitle");
        _buffDetails = GetNode<Label>("Page/Notes/BuffDetails");
        _debuffsTitle = GetNode<Label>("Page/Notes/DebuffsTitle");
        _debuffDetails = GetNode<Label>("Page/Notes/DebuffDetails");
        MouseFilter = MouseFilterEnum.Ignore;
        _page.MouseFilter = MouseFilterEnum.Stop;
        _page.Position = new Vector2(ClosedX, 0);
        _notes.Modulate = new Color(1, 1, 1, 0);
        _targetX = ClosedX;
        _page.GuiInput += OnPageInput;
        UpdateHudAnchor(force: true);
        RefreshNotes();
    }

    private void OnPageInput(InputEvent input)
    {
        if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;
        _pinned = !_pinned;
        SetExpanded(_pinned || _expanded);
        AcceptEvent();
    }

    public override void _Process(double delta)
    {
        UpdateHudAnchor();
        if (!IsVisibleInTree())
        {
            _pinned = false;
            SetExpanded(false, immediate: true);
            return;
        }

        RefreshNotes();
        Vector2 cursor = GetLocalMousePosition();
        Control? hovered = GetViewport().GuiGetHoveredControl();
        bool unobstructed = hovered == null || hovered == _page || _page.IsAncestorOf(hovered);
        bool wanted = _pinned || unobstructed && HoverBounds.HasPoint(cursor);
        _leaveTime = wanted ? 0 : _leaveTime + delta;
        if (wanted)
            SetExpanded(true);
        else if (_leaveTime > 0.22)
            SetExpanded(false);
    }

    public Rect2 HoverBounds
    {
        get
        {
            Rect2 bounds = _expanded ? _page.GetRect() : new Rect2(ClosedX, 0, 56, _page.Size.Y);
            float extraWidth = bounds.Size.X * 0.2f;
            return new Rect2(bounds.Position - new Vector2(extraWidth, 0), bounds.Size + new Vector2(extraWidth, 0));
        }
    }

    private void SetExpanded(bool expand, bool immediate = false, bool suppressed = false)
    {
        float targetX = expand ? OpenX : (suppressed ? HiddenX : ClosedX);
        if (!immediate && Mathf.IsEqualApprox(_targetX, targetX))
            return;
        _targetX = targetX;
        _expanded = expand;
        if (_slide?.IsValid() == true)
            _slide.Kill();
        _slide = null;
        if (immediate)
        {
            _page.Position = new Vector2(targetX, 0);
            _notes.Modulate = new Color(1, 1, 1, expand ? 1 : 0);
            return;
        }
        _slide = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _slide.TweenProperty(_page, "position:x", targetX, SlideSeconds);
        // Start with invisible writing while closed. As the page slides out,
        // fade the text in; when closing, fade back out.
        _slide.TweenProperty(_notes, "modulate:a", expand ? 1f : 0f, SlideSeconds);
    }

    private void RefreshNotes()
    {
        if (!_game.IsNodeReady() || _game.DayUpgrades == null)
            return;

        var config = EconomyConfigProvider.Load();
        int deadline = _game.QuotaStage == 0
            ? config.Quotas.Single(entry => entry.Id == "first").DeadlineDay
            : config.Days.TotalDays;
        int quota = _game.QuotaStage == 0 ? _game.FirstQuota : _game.SecondQuota;
        DayUpgrade selected = _game.DayUpgrades.Selected;
        bool english = GameLocalization.Language == "en";
        string signature = $"{english}|{quota}|{deadline}|{_game.Completed}|{_game.Failed}|{selected}";
        if (signature == _lastSignature)
            return;
        _lastSignature = signature;

        // The quota is the currently implemented objective. A standalone quest
        // system is planned but is NOT present in this build: don't invent quests.
        _questsTitle.Text = english ? "QUESTS" : "ЗАДАНИЯ";
        if (_game.Completed)
            _questDetails.Text = english ? "All debts paid" : "Все долги выплачены";
        else if (_game.Failed)
            _questDetails.Text = english ? "Quota overdue" : "Квота просрочена";
        else
            _questDetails.Text = english
                ? $"Pay mafia: ${quota}\nBy the end of day {deadline}"
                : $"Выплатить мафии: {quota}$\nДо конца {deadline}-го дня";

        _buffsTitle.Text = english ? "DAY BUFFS" : "БАФФЫ ДНЯ";
        string activeBuff = selected switch
        {
            DayUpgrade.Energy => english ? "+1 energy today" : "+1 энергия сегодня",
            DayUpgrade.Atelier => english ? "Rare clothing chance ↑" : "Шанс редкой одежды ↑",
            DayUpgrade.Music => english ? "Rare instruments chance ↑" : "Шанс редких инструментов ↑",
            DayUpgrade.AtelierPrice => english ? "+20% clothing prices" : "+20% к цене одежды",
            DayUpgrade.MusicPrice => english ? "+20% instrument prices" : "+20% к цене инструментов",
            // Cash is received immediately, not a lasting day buff.
            _ => english ? "None" : "Нет"
        };
        _buffDetails.Text = activeBuff;

        // There are no negative day cards in the current rules.
        _debuffsTitle.Hide();
        _debuffDetails.Hide();
    }

    public override void _ExitTree()
    {
        if (_slide?.IsValid() == true)
            _slide.Kill();
    }
}
