using System;
using Godot;

// A separate bus keeps user volume independent of each track's fades and mix level.
public partial class MusicSettings : Node
{
    public const string BusName = "Music";
    public static MusicSettings Instance { get; private set; }
    public double Volume { get; private set; } = 1;
    public event Action<double> VolumeChanged;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        if (AudioServer.GetBusIndex(BusName) < 0)
        {
            AudioServer.AddBus();
            AudioServer.SetBusName(AudioServer.BusCount - 1, BusName);
            AudioServer.SetBusSend(AudioServer.GetBusIndex(BusName), "Master");
        }
        LoadPreference();
    }

    public void LoadPreference()
    {
        var config = new ConfigFile();
        double volume = 1;
        if (config.Load(GameLocalization.SettingsPath) == Error.Ok)
        {
            var stored = config.GetValue("audio", "volume", 1.0);
            if (stored.VariantType is Variant.Type.Float or Variant.Type.Int) volume = stored.AsDouble();
        }
        Apply(volume);
    }

    public Error SetVolume(double volume)
    {
        Apply(volume);
        var config = new ConfigFile();
        config.Load(GameLocalization.SettingsPath);
        config.SetValue("audio", "volume", Volume);
        return config.Save(GameLocalization.SettingsPath);
    }

    private void Apply(double volume)
    {
        Volume = double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : 1;
        int bus = AudioServer.GetBusIndex(BusName);
        AudioServer.SetBusMute(bus, Volume == 0);
        AudioServer.SetBusVolumeDb(bus, Volume > 0 ? Mathf.LinearToDb((float)Volume) : -80);
        VolumeChanged?.Invoke(Volume);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }
}
