using System;
using System.Linq;
using Godot;

// Presentation of public gameplay state; actions retain existing TradeScene button bindings.
public partial class ProgressPanel : Control
{
    [Export] public double SlideSeconds { get; set; } = 0.32;
    [Export] public double StaggerSeconds { get; set; } = 0.065;
    [Export] public float ClosedX { get; set; } = -350;
    [Export] public float OpenX { get; set; } = 22;
    public bool Expanded { get; private set; }
    public bool Pinned { get; private set; }
    public bool HoveringHotspot { get; private set; }
    public bool IsAnimating => _slide?.IsRunning() == true;
    private TradeScene _game;
    private Button _toggle;
    private Control _content;
    private TextureRect[] _sticks;
    private Label[] _captions;
    private Tween _slide;
    private double _leaveTime;
    private bool _wasVisible = true;
    private string _lastText;
    private Vector2? _pointer;

    public override void _Ready()
    {
        _game = GetNode<TradeScene>("../../..");
        _toggle = GetNode<Button>("../ProgressToggleButton");
        _content = GetNode<Control>("Content");
        _sticks = new[] { "Day", "Quota", "Deadline" }.Select(name => GetNode<TextureRect>("Visual/" + name)).ToArray();
        _captions = _sticks.Select(stick => stick.GetNode<Label>("Caption")).ToArray();
        _toggle.Pressed += () => { Pinned = !Pinned; SetExpanded(Pinned); };
        _toggle.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                _toggle.CallDeferred(Control.MethodName.ReleaseFocus);
        };
        _content.Hide();
        GetWindow().MouseExited += OnPointerLeft;
        RefreshCaptions();
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseMotion motion) _pointer = motion.Position;
    }

    private void OnPointerLeft() => _pointer = new Vector2(-1000, -1000);

    public override void _Process(double delta)
    {
        RefreshCaptions();
        if (!IsVisibleInTree())
        {
            if (_wasVisible) { Pinned = false; SetExpanded(false, immediate: true); }
            _wasVisible = false;
            return;
        }
        _wasVisible = true;
        Size = new Vector2(Expanded ? 384 : 46, 208);
        Vector2 pointer = _pointer.HasValue ? GetGlobalTransformWithCanvas().AffineInverse() * _pointer.Value : GetLocalMousePosition();
        bool onPack = new Rect2(0, 0, 46, 208).HasPoint(pointer);
        bool onOpen = Expanded && new Rect2(0, 0, 384, 208).HasPoint(pointer);
        Control focused = GetViewport().GuiGetFocusOwner();
        HoveringHotspot = onPack || onOpen;
        bool wanted = HoveringHotspot || Pinned || _toggle.HasFocus() ||
            focused != null && (focused == _content || _content.IsAncestorOf(focused)) || _game.Failed || _game.Completed;
        _leaveTime = wanted ? 0 : _leaveTime + delta;
        if (wanted) SetExpanded(true);
        else if (_leaveTime >= 0.18) SetExpanded(false);
    }

    private void RefreshCaptions()
    {
        if (_captions == null) return;
        var config = EconomyConfigProvider.Load();
        int deadline = _game.QuotaStage == 0 ? config.Quotas.Single(quota => quota.Id == "first").DeadlineDay : config.Days.TotalDays;
        int days = Math.Max(0, deadline - _game.CurrentDay + 1);
        string day = GameLocalization.Format("День {0}", _game.CurrentDay);
        string quota = _game.Completed ? GameLocalization.Text("Все квоты выплачены") :
            GameLocalization.Format("Следующая квота: {0}$", _game.QuotaStage == 0 ? _game.FirstQuota : _game.SecondQuota);
        string key = days % 10 == 1 && days % 100 != 11 ? "До квоты: {0} день" :
            days % 10 is >= 2 and <= 4 && days % 100 is not (>= 12 and <= 14) ? "До квоты: {0} дня" : "До квоты: {0} дней";
        string remaining = _game.Completed ? GameLocalization.Text("Долг погашен") : GameLocalization.Format(key, days);
        string signature = day + "|" + quota + "|" + remaining;
        if (_lastText == signature) return;
        _lastText = signature;
        string[] texts = { day, quota, remaining };
        for (int i = 0; i < texts.Length; i++)
        {
            _captions[i].Text = texts[i];
            _captions[i].AutoTranslateMode = AutoTranslateModeEnum.Disabled;
            var font = _captions[i].GetThemeFont("font");
            int size = 21;
            while (size > 15 && font.GetStringSize(texts[i], fontSize: size).X > 258) size--;
            _captions[i].AddThemeFontSizeOverride("font_size", size);
        }
    }

    private void SetExpanded(bool expanded, bool immediate = false)
    {
        if (Expanded == expanded && !immediate) return;
        Expanded = expanded;
        if (_slide?.IsValid() == true) _slide.Kill();
        if (immediate)
        {
            foreach (var stick in _sticks) stick.Position = new Vector2(expanded ? OpenX : ClosedX, stick.Position.Y);
            _content.Hide();
            _slide = null;
            return;
        }
        _content.Hide();
        _slide = CreateTween().SetParallel().SetPauseMode(Tween.TweenPauseMode.Stop)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        for (int i = 0; i < _sticks.Length; i++)
            _slide.TweenProperty(_sticks[i], "position:x", expanded ? OpenX : ClosedX, SlideSeconds)
                .SetDelay((expanded ? i : _sticks.Length - 1 - i) * StaggerSeconds);
        _slide.Chain().TweenCallback(Callable.From(() => { _slide = null; _content.Hide(); }));
    }

    public override void _ExitTree()
    {
        GetWindow().MouseExited -= OnPointerLeft;
        if (_slide?.IsValid() == true) _slide.Kill();
    }
}
