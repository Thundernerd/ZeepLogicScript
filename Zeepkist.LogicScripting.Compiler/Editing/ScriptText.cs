using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Converts between the compiler's line and column and the editor's character index.
/// </summary>
public static class ScriptText
{
    /// <summary>
    /// The character index of a one-based line and column, kept on that line: a column past its end gives the end of
    /// the line, and a line past the last gives the end of the text.
    /// </summary>
    public static int GetIndex(string text, Position position)
    {
        text ??= string.Empty;
        return GetIndex(GetLineStarts(text), text.Length, position);
    }

    /// <summary>
    /// What to underline for each error: the token that starts where the error is, or else the one character there.
    /// At the end of a line it's empty. The text is lexed once for all of them.
    /// </summary>
    public static List<TextSpan> GetErrorSpans(string text, IEnumerable<Position> positions)
    {
        text ??= string.Empty;
        var lineStarts = GetLineStarts(text);
        var tokens = ScriptHighlighter.Highlight(text);
        List<TextSpan> spans = [];
        foreach (var position in positions)
        {
            spans.Add(GetErrorSpan(text, tokens, GetIndex(lineStarts, text.Length, position)));
        }

        return spans;
    }

    private static TextSpan GetErrorSpan(string text, IReadOnlyList<HighlightSpan> tokens, int index)
    {
        var low = 0;
        var high = tokens.Count;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (tokens[middle].Start < index)
                low = middle + 1;
            else
                high = middle;
        }

        if (low < tokens.Count && tokens[low].Start == index && tokens[low].Kind != HighlightKind.Comment)
            return new TextSpan(index, tokens[low].Length);

        var length = index < text.Length && text[index] != '\n' ? 1 : 0;
        return new TextSpan(index, length);
    }

    internal static List<int> GetLineStarts(string text)
    {
        List<int> starts = [0];
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                starts.Add(i + 1);
        }

        return starts;
    }

    internal static int GetIndex(List<int> lineStarts, int textLength, Position position)
    {
        var line = position.Line - 1;
        if (line < 0)
            return 0;

        if (line >= lineStarts.Count)
            return textLength;

        var lineEnd = line + 1 < lineStarts.Count ? lineStarts[line + 1] - 1 : textLength;
        return Math.Min(lineStarts[line] + Math.Max(0, position.Column - 1), lineEnd);
    }
}
