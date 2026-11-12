using System;
using System.Text.Json;

namespace ChikagoShop.Domain.Dialogue;

public sealed record DialogueLine(string Speaker, string Text, string? MusicCue = null);

/// <summary>A finite, ordered dialogue. Content and presentation are independent.</summary>
public sealed class DialogueSession
{
    private readonly DialogueLine[] _lines;
    public int Index { get; private set; }
    public int Count => _lines.Length;
    public bool IsComplete => Index == Count;
    public DialogueLine? Current => IsComplete ? null : _lines[Index];

    private DialogueSession(DialogueLine[] lines) => _lines = lines;

    public static DialogueSession FromJson(string json)
    {
        var lines = JsonSerializer.Deserialize<DialogueLine[]>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (lines == null || lines.Length == 0) throw new ArgumentException("Dialogue must contain lines.", nameof(json));
        foreach (var line in lines)
            if (line == null || line.Speaker == null || string.IsNullOrWhiteSpace(line.Text))
                throw new ArgumentException("Every line needs text and a speaker field (empty for narration).", nameof(json));
        return new DialogueSession(lines);
    }

    /// <returns>True only when this step finishes the dialogue.</returns>
    public bool Advance()
    {
        if (IsComplete) return false;
        Index++;
        return IsComplete;
    }
}
