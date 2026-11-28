using System;
using System.Linq;
using Godot;

// Letter-reveal presentation shared by the prologue, family and quota dialogue UIs.
public partial class DialogueTypingAudio : Node
{
    [Export] public Godot.Collections.Array<AudioStream> Samples { get; set; } = new();
    [Export] public Godot.Collections.Array<float> StartOffsets { get; set; } = new();
    [Export] public float VolumeDb { get; set; } = -18;
    [Export] public double MinimumInterval { get; set; } = 0.035;
    [Export] public double SnippetSeconds { get; set; } = 0.065;
    private readonly RandomNumberGenerator _random = new();
    private AudioStreamPlayer[] _voices;
    private readonly double[] _ages = new double[2];
    private string _text = "";
    private int _visible, _lastSample = -1;
    private double _elapsed;
    private bool _voiced, _direction;
    internal event Action<int> SamplePlayed;
    internal int ActiveVoiceCount => _voices?.Count(voice => voice.Playing) ?? 0;

    internal static DialogueTypingAudio Attach(Node owner)
    {
        var audio = GD.Load<PackedScene>("res://scenes/Audio/DialogueTypingAudio.tscn").Instantiate<DialogueTypingAudio>();
        owner.AddChild(audio);
        return audio;
    }

    public override void _Ready()
    {
        _random.Randomize();
        _voices = new[] { GetNode<AudioStreamPlayer>("Voice1"), GetNode<AudioStreamPlayer>("Voice2") };
        foreach (var voice in _voices) voice.Bus = SoundEffects.BusName;
    }

    internal void BeginPage(string text, bool voiced, int alreadyVisible = 0)
    {
        Stop();
        _text = text;
        _voiced = voiced;
        _visible = Math.Clamp(alreadyVisible, 0, text.Length);
        _direction = text[.._visible].Count(character => character == '*') % 2 != 0;
        _elapsed = MinimumInterval;
    }

    internal void Advance(int visible, double delta)
    {
        _elapsed += Math.Max(0, delta);
        int end = Math.Clamp(visible, _visible, _text.Length);
        bool letter = false;
        for (int i = _visible; i < end; i++)
        {
            char character = _text[i];
            if (character == '*') _direction = !_direction;
            else if (!_direction && char.IsLetterOrDigit(character)) letter = true;
        }
        _visible = end;
        if (!_voiced || !letter || _elapsed < MinimumInterval || Samples.Count == 0) return;
        // One audible tick at most per update: dropped frames never create a burst.
        _elapsed = 0;
        int sample = _random.RandiRange(0, Samples.Count - (_lastSample >= 0 && Samples.Count > 1 ? 2 : 1));
        if (Samples.Count > 1 && _lastSample >= 0 && sample >= _lastSample) sample++;
        _lastSample = sample;
        int index = Array.FindIndex(_voices, voice => !voice.Playing);
        if (index < 0) index = _ages[0] >= _ages[1] ? 0 : 1;
        var player = _voices[index];
        player.Stop();
        player.Stream = Samples[sample];
        player.VolumeDb = VolumeDb;
        _ages[index] = 0;
        player.Play(sample < StartOffsets.Count ? StartOffsets[sample] : 0);
        SamplePlayed?.Invoke(sample);
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            if (!voice.Playing) continue;
            _ages[i] += delta;
            if (_ages[i] >= SnippetSeconds) voice.Stop();
            else
            {
                double fade = Math.Clamp((SnippetSeconds - _ages[i]) / 0.012, 0, 1);
                voice.VolumeDb = VolumeDb + Mathf.LinearToDb((float)Math.Max(0.0001, fade));
            }
        }
    }

    internal void Stop()
    {
        if (_voices != null) foreach (var voice in _voices) voice.Stop();
        _voiced = false;
    }

    public override void _ExitTree() => Stop();
}
