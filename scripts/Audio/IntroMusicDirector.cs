using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

// Scene-owned music: overlapping voices keep even rapid dialogue changes smooth.
public partial class IntroMusicDirector : Node
{
    [Export] public float MenuVolumeDb { get; set; } = -6;
    [Export] public float IntroVolumeDb { get; set; } = -12.0206f;
    [Export] public float ThoughtfulVolumeDb { get; set; } = -10;
    [Export] public float SinisterVolumeDb { get; set; } = -8;
    // Match source RMS to the menu reference before applying the stage fader.
    [Export] public float ThoughtfulCompensation { get; set; } = 2.7922467f;
    [Export] public float SinisterCompensation { get; set; } = 1.4395282f;
    [Export] public AudioStream ThoughtfulStream { get; set; } = GD.Load<AudioStream>("res://audio/music/thoughtful_loop.wav");
    [Export] public AudioStream SinisterStream { get; set; } = GD.Load<AudioStream>("res://audio/music/sinister_loop.wav");
    [Export] public double IntroFadeSeconds { get; set; } = 4;
    [Export] public double CrossfadeSeconds { get; set; } = 2.5;
    [Export] public double ExitFadeSeconds { get; set; } = 1.5;

    public string CurrentCue { get; private set; } = "menu";
    public bool IsSilent => _voices.All(v => !v.Player.Playing);
    private readonly List<Voice> _voices = new();
    private Voice _menu, _intro, _thoughtful, _sinister;
    private TaskCompletionSource<bool> _silence;

    private sealed class Voice
    {
        public AudioStreamPlayer Player;
        public float Compensation = 1;
        public float From, Target;
        public double Elapsed, Duration;
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _menu = new Voice { Player = GetParent().GetNode<AudioStreamPlayer>("MenuMusic") };
        _menu.Player.VolumeDb = MenuVolumeDb;
        _menu.Target = _menu.Player.VolumeLinear;
        _menu.From = _menu.Target;
        _menu.Duration = _menu.Elapsed = 1;
        _voices.Add(_menu);
        _intro = AddVoice("IntroBaseMusic", _menu.Player.Stream);
        _thoughtful = AddVoice("ThoughtfulMusic", ThoughtfulStream);
        _sinister = AddVoice("SinisterMusic", SinisterStream);
        _thoughtful.Compensation = ThoughtfulCompensation;
        _sinister.Compensation = SinisterCompensation;
    }

    private Voice AddVoice(string name, AudioStream stream)
    {
        var player = new AudioStreamPlayer { Name = name, Stream = stream, VolumeLinear = 0 };
        AddChild(player);
        var voice = new Voice { Player = player };
        _voices.Add(voice);
        return voice;
    }

    public void PlayMenu()
    {
        if (CurrentCue == "menu" && _menu.Player.Playing) return;
        CancelSilence();
        CurrentCue = "menu";
        Crossfade(_menu, MenuVolumeDb, 1);
    }

    public void BeginIntro()
    {
        CancelSilence();
        CurrentCue = "base";
        _intro.Player.Stop();
        _intro.Player.VolumeDb = -54;
        _intro.Player.Play();
        foreach (var voice in _voices)
            Ramp(voice, voice == _intro ? Mathf.DbToLinear(IntroVolumeDb) : 0,
                voice == _intro ? IntroFadeSeconds : 0.6);
    }

    public void ApplyIntroCue(string cue)
    {
        if (cue == CurrentCue || CurrentCue is "menu" or "silence") return;
        if (cue != "thoughtful" && cue != "sinister") return;
        CurrentCue = cue;
        Crossfade(cue == "thoughtful" ? _thoughtful : _sinister,
            cue == "thoughtful" ? ThoughtfulVolumeDb : SinisterVolumeDb, CrossfadeSeconds);
    }

    private void Crossfade(Voice incoming, float referenceDb, double duration)
    {
        foreach (var voice in _voices)
            Ramp(voice, voice == incoming ? Mathf.DbToLinear(referenceDb) * voice.Compensation : 0, duration);
    }

    private static void Ramp(Voice voice, float target, double duration)
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

    public Task<bool> FadeToSilence()
    {
        if (_silence != null) return _silence.Task;
        CurrentCue = "silence";
        foreach (var voice in _voices) Ramp(voice, 0, ExitFadeSeconds);
        _silence = new TaskCompletionSource<bool>();
        if (IsSilent) CompleteSilence();
        return _silence?.Task ?? Task.FromResult(true);
    }

    public override void _Process(double delta)
    {
        foreach (var voice in _voices)
        {
            if (!voice.Player.Playing) continue;
            voice.Elapsed = Math.Min(voice.Duration, voice.Elapsed + delta);
            float t = (float)(voice.Elapsed / Math.Max(0.001, voice.Duration));
            float smooth = t * t * (3 - 2 * t);
            voice.Player.VolumeLinear = Mathf.Lerp(voice.From, voice.Target, smooth);
            if (t >= 1 && voice.Target == 0) voice.Player.Stop();
        }
        if (_silence != null && IsSilent) CompleteSilence();
    }

    private void CompleteSilence()
    {
        var completion = _silence;
        _silence = null;
        completion?.TrySetResult(true);
    }

    private void CancelSilence()
    {
        var completion = _silence;
        _silence = null;
        completion?.TrySetResult(false);
    }

    public override void _ExitTree() => CancelSilence();
}
