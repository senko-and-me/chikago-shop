using Godot;

internal sealed class StorySpeakerFocus
{
    private const float SpeakingScale = 1.06f;
    private const double TransitionSeconds = 0.4;
    private readonly Node _owner;
    private readonly (string Speaker, Control Portrait)[] _portraits;
    private string _speaker;
    private Tween _transition;

    public StorySpeakerFocus(Node owner, params (string Speaker, Control Portrait)[] portraits)
    {
        _owner = owner;
        _portraits = portraits;
        foreach (var (_, portrait) in portraits)
        {
            portrait.TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps;
            portrait.PivotOffset = portrait.Size / 2;
            portrait.Resized += () => portrait.PivotOffset = portrait.Size / 2;
        }
    }

    public void SetSpeaker(string speaker)
    {
        if (_speaker == speaker) return;
        _speaker = speaker;
        if (_transition?.IsValid() == true) _transition.Kill();
        _transition = _owner.CreateTween().SetParallel()
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        foreach (var (name, portrait) in _portraits)
        {
            Vector2 scale = Vector2.One * (speaker == name ? SpeakingScale : 1);
            _transition.TweenProperty(portrait, "scale", scale, TransitionSeconds);
        }
    }
}
