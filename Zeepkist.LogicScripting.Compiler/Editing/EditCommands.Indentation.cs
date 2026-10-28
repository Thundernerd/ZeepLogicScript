using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public static partial class EditCommands
{
    /// <summary>
    /// Indents every selected line that isn't empty when the selection spans a line break; otherwise replaces the
    /// selection with spaces up to the next indentation level.
    /// </summary>
    public static TextSnapshot Indent(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var start = GetSelectionStart(snapshot);
        if (text.IndexOf('\n', start, GetSelectionEnd(snapshot) - start) < 0)
        {
            var column = start - GetLineStart(text, start);
            return ReplaceSelection(snapshot, new string(' ', IndentSize - (column % IndentSize)));
        }

        var lineStarts = ScriptText.GetLineStarts(text);
        GetSelectedLines(snapshot, lineStarts, out var first, out var last);
        List<Change> changes = [];
        for (var line = first; line <= last; line++)
        {
            if (GetLineEnd(lineStarts, text.Length, line) > lineStarts[line])
                changes.Add(new Change(lineStarts[line], 0, new string(' ', IndentSize)));
        }

        return Apply(snapshot, changes);
    }

    /// <summary>
    /// Takes one indentation level, a tab or up to <see cref="IndentSize"/> spaces, off every selected line.
    /// </summary>
    public static TextSnapshot Outdent(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var lineStarts = ScriptText.GetLineStarts(text);
        GetSelectedLines(snapshot, lineStarts, out var first, out var last);
        List<Change> changes = [];
        for (var line = first; line <= last; line++)
        {
            var lineStart = lineStarts[line];
            var length = 0;
            if (lineStart < text.Length && text[lineStart] == '\t')
            {
                length = 1;
            }
            else
            {
                var lineEnd = GetLineEnd(lineStarts, text.Length, line);
                while (length < IndentSize && lineStart + length < lineEnd && text[lineStart + length] == ' ')
                {
                    length++;
                }
            }

            if (length > 0)
                changes.Add(new Change(lineStart, length, string.Empty));
        }

        return Apply(snapshot, changes);
    }

    /// <summary>
    /// Replaces the selection with a line break and the indentation of the line it's on, up to the caret.
    /// </summary>
    public static TextSnapshot NewLine(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var start = GetSelectionStart(snapshot);
        var lineStart = GetLineStart(text, start);
        var indentEnd = Math.Min(SkipIndentation(text, lineStart), start);
        return ReplaceSelection(snapshot, "\n" + text.Substring(lineStart, indentEnd - lineStart));
    }
}
