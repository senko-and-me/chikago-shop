using System;
using Godot;

// Contact-driven presentation only; polishing progress remains owned by its session.
public partial class PolishingAudio : Node
{
    [Export] public float VolumeDb { get; set; } = -12;
    [Export(PropertyHint.Range, "0.1,2,0.01")] public float PitchScale { get; set; } = 1;
    [Export] public float MinRate { get; set; } = 1.5f;
    [Export] public float MaxRate { get; set; } = 3;
    [Export] public float SlowSpeed { get; set; } = 60;
    [Export] public float FastSpeed { get; set; } = 600;
    [Export] public float MinimumMovement { get; set; } = 3;
    [Export] public double IdleSeconds { get; set; } = 0.08;
    [Export] public double AttackSeconds { get; set; } = 0.015;
    [Export] public double FadeSeconds { get; set; } = 0.06;

    public event Action<int> SamplePlayed;
    public int LastSample { get; private set; }
    public float CurrentRate { get; private set; }
    public int ActiveVoiceCount => (_voices[0]?.Player.Playing == true ? 1 : 0) +
        (_voices[1]?.Player.Playing == true ? 1 : 0);
    private readonly AudioStreamPlayer[] _samples = new AudioStreamPlayer[5];
    private readonly Voice[] _voices = new Voice[2];
    private readonly RandomNumberGenerator _random = new();
    private Node _owner;
    private Vector2 _anchor;
    private double _clock, _motionUntil, _nextSample, _movementSeconds;
    private double _lastSampleAt = double.NegativeInfinity;
    private bool _moving, _initialized;

    private sealed class Voice
    {
        public AudioStreamPlayer Player;
        public double Age, Duration, ReleaseAge;
        public float Gain, Envelope, ReleaseFrom;
        public bool Releasing;
    }

    internal void Initialize()
    {
        if (_initialized) return;
        ProcessMode = ProcessModeEnum.Always;
        _random.Randomize();
        for (int i = 0; i < _samples.Length; i++)
            _samples[i] = GetNode<AudioStreamPlayer>("Scrape" + (i + 1));
        for (int i = 0; i < _voices.Length; i++)
        {
            var player = new AudioStreamPlayer { Name = "Voice" + (i + 1), Bus = SoundEffects.BusName, VolumeLinear = 0 };
            AddChild(player);
            _voices[i] = new Voice { Player = player };
        }
        _initialized = true;
    }

    public void BeginContact(Node owner, Vector2 point)
    {
        if (!_initialized) return;
        if (_owner != null) EndContact(_owner);
        _owner = owner;
        _anchor = point;
        _movementSeconds = 0;
        _moving = false;
    }

    public void MoveContact(Node owner, Vector2 point, double seconds)
    {
        if (!_initialized || _owner != owner || !double.IsFinite(seconds) || seconds < 0) return;
        _movementSeconds += seconds;
        float distance = point.DistanceTo(_anchor);
        if (distance < Math.Max(0.1f, MinimumMovement)) return;
        float speed = distance / (float)Math.Max(0.001, _movementSeconds);
        float t = Mathf.Clamp((speed - SlowSpeed) / Math.Max(1, FastSpeed - SlowSpeed), 0, 1);
        float low = Math.Max(0.1f, MinRate), high = Math.Max(low, MaxRate);
        CurrentRate = Mathf.Lerp(low, high, t);
        // Keep one deadline across contour exits/reentries and pointer re-presses.
        _nextSample = _lastSampleAt + 1 / CurrentRate;
        _anchor = point;
        _movementSeconds = 0;
        _motionUntil = _clock + Math.Max(0.01, IdleSeconds);
        if (!_moving)
        {
            _moving = true;
            if (_clock >= _nextSample) StartSample();
        }
    }

    public void EndContact(Node owner)
    {
        if (_owner != owner) return;
        _owner = null;
        StopMotion();
    }

    private void StopMotion()
    {
        _moving = false;
        foreach (var voice in _voices)
        {
            if (voice == null || !voice.Player.Playing || voice.Releasing) continue;
            voice.Releasing = true;
            voice.ReleaseAge = 0;
            voice.ReleaseFrom = voice.Envelope;
        }
    }

    private void StartSample()
    {
        int index = _random.RandiRange(0, _samples.Length - (LastSample == 0 ? 1 : 2));
        if (LastSample != 0 && index >= LastSample - 1) index++;
        var sample = _samples[index];
        var voice = !_voices[0].Player.Playing ? _voices[0] : !_voices[1].Player.Playing ? _voices[1] :
            _voices[0].Age >= _voices[1].Age ? _voices[0] : _voices[1];
        voice.Player.Stop();
        voice.Player.Stream = sample.Stream;
        voice.Player.PitchScale = Math.Max(0.01f, PitchScale * sample.PitchScale);
        voice.Player.VolumeLinear = 0;
        voice.Gain = Mathf.DbToLinear(VolumeDb + sample.VolumeDb);
        voice.Age = voice.Envelope = 0;
        // Lower pitch lengthens playback. Fade before a third clip would need this voice.
        voice.Duration = Math.Min(sample.Stream.GetLength() / voice.Player.PitchScale,
            Math.Max(0.01, 2 / CurrentRate - Math.Max(0.001, AttackSeconds)));
        voice.Releasing = false;
        voice.Player.Play();
        LastSample = index + 1;
        _lastSampleAt = _clock;
        _nextSample = _clock + 1 / CurrentRate;
        SamplePlayed?.Invoke(LastSample);
    }

    public override void _Process(double delta)
    {
        if (!_initialized || !double.IsFinite(delta) || delta < 0) return;
        _clock += delta;
        if (_moving)
        {
            if (_clock >= _motionUntil || !GodotObject.IsInstanceValid(_owner)) StopMotion();
            else if (_clock >= _nextSample) StartSample();
        }
        float power = 0;
        foreach (var voice in _voices)
        {
            if (!voice.Player.Playing) { voice.Envelope = 0; continue; }
            voice.Age += delta;
            if (voice.Releasing)
            {
                voice.ReleaseAge += delta;
                voice.Envelope = voice.ReleaseFrom * (1 - (float)Math.Clamp(voice.ReleaseAge / Math.Max(0.001, FadeSeconds), 0, 1));
            }
            else
                voice.Envelope = (float)Math.Clamp(Math.Min(voice.Age / Math.Max(0.001, AttackSeconds),
                    (voice.Duration - voice.Age) / Math.Max(0.001, FadeSeconds)), 0, 1);
            if (voice.Age >= voice.Duration || voice.Releasing && voice.ReleaseAge >= Math.Max(0.001, FadeSeconds))
            {
                voice.Player.Stop();
                voice.Envelope = 0;
            }
            power += voice.Envelope * voice.Envelope;
        }
        float normalization = Mathf.Sqrt(Math.Max(1, power));
        foreach (var voice in _voices)
            if (voice.Player.Playing) voice.Player.VolumeLinear = voice.Gain * voice.Envelope / normalization;
    }

    public override void _ExitTree()
    {
        foreach (var voice in _voices) voice?.Player.Stop();
    }
}
