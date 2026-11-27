using Godot;

// Castello enters once when his conversation with Frankie begins.
public partial class PrologueCharacterMotion : Control
{
    [Export(PropertyHint.Range, "0.2,2,0.05")] public double ArrivalDuration { get; set; } = 0.7;
    private TextureRect _mafia;
    private float _mafiaLeft;
    private Tween _arrival;
    private bool _entranceStarted;
    private bool _stopped;
    public float ArrivalProgress { get; private set; }
    public bool IsMoving => _arrival?.IsRunning() == true;

    public override void _Ready()
    {
        _mafia = GetNode<TextureRect>("Castello");
        _mafiaLeft = _mafia.OffsetLeft;
        _mafia.Hide();
        Resized += Apply;
        Apply();
    }

    public void SyncLine()
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
    }

    private void Apply()
    {
        if (_mafia == null) return;
        Place(_mafia, _mafiaLeft, (ArrivalProgress - 1) * Size.X * 0.14f, ArrivalProgress);
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
        _arrival?.Kill();
        _arrival = null;
    }

    public override void _ExitTree() => Stop();
}
