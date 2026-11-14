using Godot;

public partial class MusicVolumeControl : PanelContainer
{
    [Export] public bool PlainStyle { get; set; }
    private HSlider _slider;
    private Label _percentage;
    private MusicSettings _settings;

    public override void _Ready()
    {
        _slider = GetNode<HSlider>("Content/Slider");
        _percentage = GetNode<Label>("Content/Header/Percentage");
        if (PlainStyle) ApplyPlainStyle();
        _settings = MusicSettings.Instance;
        UpdateValue(_settings.Volume);
        _settings.VolumeChanged += UpdateValue;
        _slider.ValueChanged += value =>
        {
            Error error = _settings.SetVolume(value / 100);
            if (error != Error.Ok) GD.PushWarning($"Music volume could not be saved: {error}");
        };
    }

    private void ApplyPlainStyle()
    {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.94f, 0.91f, 0.85f),
            ContentMarginLeft = 14, ContentMarginRight = 14,
            ContentMarginTop = 10, ContentMarginBottom = 10
        });
        CustomMinimumSize = new Vector2(360, 0);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        GetNode<Label>("Content/Header/Title").AddThemeFontSizeOverride("font_size", 20);
        _slider.CustomMinimumSize = new Vector2(0, 28);
        foreach (string icon in new[] { "grabber", "grabber_highlight", "grabber_disabled" })
            _slider.RemoveThemeIconOverride(icon);
    }

    private void UpdateValue(double volume)
    {
        _slider.SetValueNoSignal(System.Math.Round(volume * 100));
        _percentage.Text = $"{System.Math.Round(volume * 100)}%";
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_settings)) _settings.VolumeChanged -= UpdateValue;
    }
}
