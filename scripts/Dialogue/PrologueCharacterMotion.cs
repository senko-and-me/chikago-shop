using Godot;

// Prologue blocking establishes the cast on opening and cues departure from the original dialogue.
public partial class PrologueCharacterMotion : Control
{
    [Export(PropertyHint.Range, "0.2,2,0.05")] public double ArrivalDuration { get; set; } = 0.7;
    [Export(PropertyHint.Range, "0.2,2,0.05")] public double DepartureDuration { get; set; } = 0.6;
    private TextureRect _mafia, _customer;
    private float _mafiaLeft, _customerLeft;
    private Tween _arrival, _departure;
    private int _firstMafiaLine = -1;
    private bool _entranceStarted;
    private bool _departed, _stopped;
    public float ArrivalProgress { get; private set; }
    public float DepartureProgress { get; private set; }
    public bool IsDeparting => _departure?.IsRunning() == true;
    public bool IsMoving => _arrival?.IsRunning() == true || IsDeparting;

    public override void _Ready()
    {
        _mafia = GetNode<TextureRect>("Castello");
        _customer = GetNode<TextureRect>("Customer");
        _mafiaLeft = _mafia.OffsetLeft;
        _customerLeft = _customer.OffsetLeft;
        _mafia.Hide();
        Resized += Apply;
        Apply();
    }

    public void SyncLine(int index, string speaker)
    {
        if (_stopped) return;
        if (!_entranceStarted)
        {
            _entranceStarted = true;
            _mafia.Show();
            _arrival = CreateTween();
            _arrival.TweenMethod(Callable.From<float>(value => { ArrivalProgress = value; Apply(); }),
                0f, 1f, ArrivalDuration).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            _arrival.TweenCallback(Callable.From(() => _arrival = null));
        }
        if (_firstMafiaLine < 0 && speaker == StoryPortraitController.Castello) _firstMafiaLine = index;
        // She remains for his offer to pay, then leaves as Frankie turns to him.
        if (_firstMafiaLine >= 0 && index > _firstMafiaLine && !_departed)
        {
            _departed = true;
            _departure = CreateTween();
            _departure.TweenMethod(Callable.From<float>(value => { DepartureProgress = value; Apply(); }),
                0f, 1f, DepartureDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            _departure.TweenCallback(Callable.From(() => { _customer.Hide(); _departure = null; }));
        }
    }

    private void Apply()
    {
        if (_mafia == null) return;
        Place(_mafia, _mafiaLeft, (ArrivalProgress - 1) * Size.X * 0.14f, ArrivalProgress);
        Place(_customer, _customerLeft, -DepartureProgress * Size.X * 0.20f, 1 - DepartureProgress);
    }

    private void Place(TextureRect portrait, float left, float shift, float alpha)
    {
        portrait.Position = new Vector2(portrait.AnchorLeft * Size.X + left + shift, portrait.Position.Y);
        Color color = portrait.Modulate;
        color.A = alpha * alpha * (3 - 2 * alpha);
        portrait.Modulate = color;
    }

    public void Stop()
    {
        _stopped = true;
        _arrival?.Kill(); _departure?.Kill();
        _arrival = null; _departure = null;
    }

    public override void _ExitTree() => Stop();
}
