using Godot;

public partial class MusicVolumeControl : PanelContainer
{
    private HSlider _slider;
    private Label _percentage;
    private MusicSettings _settings;

    public override void _Ready()
    {
        _slider = GetNode<HSlider>("Content/Slider");
        _percentage = GetNode<Label>("Content/Header/Percentage");
        _settings = MusicSettings.Instance;
        UpdateValue(_settings.Volume);
        _settings.VolumeChanged += UpdateValue;
        _slider.ValueChanged += value =>
        {
            Error error = _settings.SetVolume(value / 100);
            if (error != Error.Ok) GD.PushWarning($"Music volume could not be saved: {error}");
        };
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
