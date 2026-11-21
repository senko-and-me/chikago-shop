using Godot;

// Scene-owned cover. Only a locked -> unlocked transition creates animation work.
public partial class RoomCurtainPresentation : ColorRect
{
    [Export(PropertyHint.Range, "0.2,2,0.05")] public double RevealDuration { get; set; } = 0.85;
    private TextureRect _curtain;
    private Control _unlock;
    private Vector2 _restPosition, _restScale;
    private Color _restModulate, _restSelfModulate;
    private MouseFilterEnum _restMouseFilter;
    private Tween _reveal;
    private bool _initialized, _locked, _unlockVisible;
    public bool IsRevealing => _reveal?.IsRunning() == true;

    public override void _Ready()
    {
        _curtain = GetNode<TextureRect>("LockedCurtain");
        _unlock = GetNodeOrNull<Control>("UnlockPanel");
        _restPosition = _curtain.Position;
        _restScale = _curtain.Scale;
        _restModulate = Modulate;
        _restSelfModulate = SelfModulate;
        _restMouseFilter = MouseFilter;
        _unlockVisible = _unlock?.Visible == true;
        ClipContents = true;
    }

    public void SetLocked(bool locked)
    {
        if (_initialized && locked == _locked) return;
        bool animate = _initialized && _locked && !locked && IsVisibleInTree();
        _initialized = true;
        _locked = locked;
        StopReveal();
        _curtain.Position = _restPosition;
        _curtain.Scale = _restScale;
        Modulate = _restModulate;
        SelfModulate = _restSelfModulate;
        MouseFilter = locked ? _restMouseFilter : MouseFilterEnum.Ignore;
        if (_unlock != null) _unlock.Visible = locked && _unlockVisible;
        Visible = locked || animate;
        if (!animate) return;

        // Clip to the doorway; fade the black backing early so it cannot mask the reveal.
        _reveal = CreateTween().SetParallel().SetPauseMode(Tween.TweenPauseMode.Process);
        _reveal.TweenProperty(this, "self_modulate:a", 0f, RevealDuration * 0.3);
        _reveal.TweenProperty(_curtain, "scale:y", _restScale.Y * 0.06f, RevealDuration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _reveal.TweenProperty(_curtain, "position:y", _restPosition.Y - _curtain.Size.Y * 0.12f,
            RevealDuration).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _reveal.TweenProperty(this, "modulate:a", 0f, RevealDuration * 0.7)
            .SetDelay(RevealDuration * 0.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _reveal.Chain().TweenCallback(Callable.From(() => { Hide(); _reveal = null; }));
    }

    private void StopReveal()
    {
        _reveal?.Kill();
        _reveal = null;
    }

    public override void _ExitTree() => StopReveal();
}
