using Godot;

public partial class MusicVolumeControl : PanelContainer
{
    [Export] public bool PlainStyle { get; set; }
    [Export] public bool ControlsSoundEffects { get; set; }
    private HSlider _slider;
    private Label _percentage;
    private MusicSettings _settings;
    private SoundEffects _effects;
    private bool _dragging;

    public override void _Ready()
    {
        _slider = GetNode<HSlider>("Content/Slider");
        _percentage = GetNode<Label>("Content/Header/Percentage");
        if (PlainStyle) ApplyPlainStyle();
        if (ControlsSoundEffects)
        {
            GetNode<Label>("Content/Header/Title").Text = "Громкость звуков";
            _effects = SoundEffects.Instance;
            UpdateValue(_effects.Volume);
            _effects.VolumeChanged += UpdateValue;
        }
        else
        {
            _settings = MusicSettings.Instance;
            UpdateValue(_settings.Volume);
            _settings.VolumeChanged += UpdateValue;
        }
        _slider.DragStarted += () => _dragging = true;
        _slider.DragEnded += changed =>
        {
            _dragging = false;
            if (changed) SaveVolume(_slider.Value);
        };
        _slider.ValueChanged += value =>
        {
            if (_dragging)
            {
                if (ControlsSoundEffects) _effects.PreviewVolume(value / 100);
                else _settings.PreviewVolume(value / 100);
            }
            else SaveVolume(value);
        };
    }

    private void SaveVolume(double value)
    {
        Error error = ControlsSoundEffects ? _effects.SetVolume(value / 100) : _settings.SetVolume(value / 100);
        if (error != Error.Ok) GD.PushWarning($"Audio volume could not be saved: {error}");
    }

    private void ApplyPlainStyle()
    {
        // Keep the scene-authored paper, ink track and illustrated handle in pause too.
        var paper = (StyleBox)GetThemeStylebox("panel").Duplicate();
        paper.ContentMarginLeft = 14; paper.ContentMarginRight = 14;
        paper.ContentMarginTop = 10; paper.ContentMarginBottom = 10;
        AddThemeStyleboxOverride("panel", paper);
        CustomMinimumSize = new Vector2(360, 0);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _slider.CustomMinimumSize = new Vector2(0, 28);
    }

    private void UpdateValue(double volume)
    {
        _slider.SetValueNoSignal(System.Math.Round(volume * 100));
        _percentage.Text = $"{System.Math.Round(volume * 100)}%";
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_settings)) _settings.VolumeChanged -= UpdateValue;
        if (GodotObject.IsInstanceValid(_effects)) _effects.VolumeChanged -= UpdateValue;
    }
}
