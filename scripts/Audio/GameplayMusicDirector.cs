using System;
using Godot;

// Presentation-only observer: the card stays open through both deal and exit animation.
public partial class GameplayMusicDirector : Node
{
    [Export] public AudioStream DayStream { get; set; }
    [Export] public AudioStream UpgradeStream { get; set; }
    [Export] public float VolumeDb { get; set; } = -12.0206f;
    [Export] public float UpgradeCompensation { get; set; } = 2.7922467f;
    [Export] public double CrossfadeSeconds { get; set; } = 2.5;
    [Export] public double FadeInSeconds { get; set; } = 2.5;
    [Export] public NodePath UpgradeCardPath { get; set; } = new("../DayTransitionCard");

    public string CurrentCue { get; private set; } = "";
    private DayTransitionCard _card;
    private Voice _day, _upgrades;

    private sealed class Voice
    {
        public AudioStreamPlayer Player;
        public float From, Target;
        public double Elapsed, Duration;
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _day = CreateVoice("DayMusic", DayStream);
        _upgrades = CreateVoice("UpgradeMusic", UpgradeStream);
    }

    private Voice CreateVoice(string name, AudioStream stream)
    {
        var player = new AudioStreamPlayer { Name = name, Stream = stream, VolumeLinear = 0, Bus = MusicSettings.BusName };
        AddChild(player);
        return new Voice { Player = player };
    }

    public override void _Process(double delta)
    {
        // The card is created by the parent's Ready, after this node's Ready.
        // Observe actual visibility so restored pending choices and reset also work.
        if (_card == null || !GodotObject.IsInstanceValid(_card))
            _card = GetNodeOrNull<DayTransitionCard>(UpgradeCardPath);
        string wanted = _card?.IsOpen == true ? "upgrades" : "day";
        if (wanted != CurrentCue)
        {
            double duration = CurrentCue.Length == 0 ? FadeInSeconds : CrossfadeSeconds;
            CurrentCue = wanted;
            float level = Mathf.DbToLinear(VolumeDb);
            BeginRamp(_day, wanted == "day" ? level : 0, duration);
            BeginRamp(_upgrades, wanted == "upgrades" ? level * UpgradeCompensation : 0, duration);
        }
        Step(_day, delta);
        Step(_upgrades, delta);
    }

    private static void BeginRamp(Voice voice, float target, double duration)
    {
        voice.From = voice.Player.Playing ? voice.Player.VolumeLinear : 0;
        voice.Target = target;
        voice.Elapsed = 0;
        voice.Duration = Math.Max(0.001, duration);
        if (target > 0 && !voice.Player.Playing)
        {
            voice.Player.VolumeLinear = 0;
            voice.Player.Play();
        }
    }

    private static void Step(Voice voice, double delta)
    {
        if (!voice.Player.Playing) return;
        voice.Elapsed = Math.Min(voice.Duration, voice.Elapsed + delta);
        float t = (float)(voice.Elapsed / voice.Duration);
        float smooth = t * t * (3 - 2 * t);
        // Equal-power crossfade avoids a volume dip between equally loud tracks.
        voice.Player.VolumeLinear = Mathf.Sqrt(Mathf.Lerp(voice.From * voice.From, voice.Target * voice.Target, smooth));
        if (t >= 1 && voice.Target == 0) voice.Player.Stop();
    }

    public override void _ExitTree()
    {
        _day?.Player.Stop();
        _upgrades?.Player.Stop();
    }
}
