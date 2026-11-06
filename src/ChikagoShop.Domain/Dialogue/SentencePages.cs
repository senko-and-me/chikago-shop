using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChikagoShop.Domain.Dialogue;

public static class SentencePages
{
    // Keep closing quotes, stage-direction markers and whitespace with the sentence.
    public static int End(string text, int start, Func<string, bool> fits)
    {
        int selected = start;
        foreach (Match match in Regex.Matches(text[start..], """[.!?…]+[»”"')\]*]*(?=\s|$)"""))
        {
            int end = start + match.Index + match.Length;
            if (!text[start..end].Any(char.IsLetterOrDigit)) continue;
            while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
            if (!fits(text[start..end])) return selected == start ? end : selected;
            selected = end;
        }
        if (selected < text.Length && fits(text[start..])) return text.Length;
        return selected == start ? text.Length : selected;
    }
}
