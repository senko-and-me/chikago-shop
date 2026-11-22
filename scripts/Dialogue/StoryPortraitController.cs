#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using ChikagoShop.Domain.Dialogue;
using Godot;

// Plays any pair of portrait slots with the same reveal clock as the dialogue.
internal sealed class StoryPortraitController
{
    public const string Castello = "Кастелло";
    public const string Frankie = "Френки";
    private const string StatePath = "res://dialogue/portrait_states.json";

    private sealed record Frames(string Closed, string? Open);
    private static readonly Lazy<Dictionary<string, Dictionary<string, Frames>>> States = new(() =>
        JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Frames>>>(
            FileAccess.GetFileAsString(StatePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);

    private readonly Slot _left;
    private readonly Slot _right;
    private DialogueLine? _line;

    public StoryPortraitController(TextureRect left, string leftSpeaker, TextureRect right, string rightSpeaker)
    {
        _left = new Slot(left, leftSpeaker, States.Value["left"]);
        _right = new Slot(right, rightSpeaker, States.Value["right"]);
    }

    public void SetLine(DialogueLine line) => _line = line;

    public void ShowSpeech(double? elapsed, bool lineComplete = false)
    {
        if (_line == null) return;
        // Alternate the mouth while letters appear; pauses turn it off immediately.
        bool open = elapsed is double seconds && seconds % 0.25 < 0.145;
        _left.Show(lineComplete ? _line.LeftPortraitAfter ?? _line.LeftPortrait : _line.LeftPortrait,
            open && _line.Speaker == _left.Speaker);
        _right.Show(lineComplete ? _line.RightPortraitAfter ?? _line.RightPortrait : _line.RightPortrait,
            open && _line.Speaker == _right.Speaker);
    }

    private sealed class Slot
    {
        private readonly TextureRect _portrait;
        private readonly Texture2D _original;
        private readonly Dictionary<string, (Texture2D Closed, Texture2D Open)> _textures = new();
        public string Speaker { get; }

        public Slot(TextureRect portrait, string speaker, Dictionary<string, Frames> states)
        {
            _portrait = portrait;
            _original = portrait.Texture;
            Speaker = speaker;
            foreach (var (state, frame) in states)
            {
                var closed = GD.Load<Texture2D>(frame.Closed);
                var open = string.IsNullOrEmpty(frame.Open) ? closed : GD.Load<Texture2D>(frame.Open);
                _textures[state] = (closed, open);
            }
        }

        public void Show(string? state, bool speaking)
        {
            var texture = state != null && _textures.TryGetValue(state, out var frames)
                ? speaking ? frames.Open : frames.Closed
                : _original;
            if (_portrait.Texture != texture) _portrait.Texture = texture;
        }
    }
}
