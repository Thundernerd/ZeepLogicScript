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
    /// What to underline for an error: the token that starts where the error is, or else the one character there.
    /// At the end of a line it's empty.
    /// </summary>
    public static TextSpan GetErrorSpan(string text, Position position)
    {
        text ??= string.Empty;
        var index = GetIndex(text, position);
        foreach (var span in ScriptHighlighter.Highlight(text))
        {
            if (span.Start == index && span.Kind != HighlightKind.Comment)
                return new TextSpan(span.Start, span.Length);

            if (span.Start > index)
                break;
        }

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
