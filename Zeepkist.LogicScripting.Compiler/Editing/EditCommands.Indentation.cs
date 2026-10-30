using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

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
            if (ScriptText.GetLineEnd(lineStarts, text.Length, line) > lineStarts[line])
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
                var lineEnd = ScriptText.GetLineEnd(lineStarts, text.Length, line);
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
    /// Replaces the selection with a line break and the indentation of the line it's on, up to the caret; after a
    /// Function's header, one level deeper for its body.
    /// </summary>
    public static TextSnapshot NewLine(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var start = GetSelectionStart(snapshot);
        var lineStart = GetLineStart(text, start);
        var indentEnd = Math.Min(SkipIndentation(text, lineStart), start);
        var indentation = text.Substring(lineStart, indentEnd - lineStart);
        var end = GetSelectionEnd(snapshot);
        var lineEnd = GetLineEnd(text, end);
        if (SkipIndentation(text, end) >= lineEnd && IsFunctionHeader(text, indentEnd, lineEnd))
            indentation += new string(' ', IndentSize);

        return ReplaceSelection(snapshot, "\n" + indentation);
    }

    /// <summary>
    /// Gives a line that is only <c>end</c>, with the caret right after it, the indentation of the Function header it
    /// closes; any other snapshot comes back as it was.
    /// </summary>
    public static TextSnapshot OutdentEnd(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var caret = snapshot.Caret;
        var lineStart = GetLineStart(text, caret);
        var indentEnd = SkipIndentation(text, lineStart);
        var keyword = FunctionDefinition.EndKeyword;
        if (snapshot.Selection != 0
            || caret != indentEnd + keyword.Length
            || GetLineEnd(text, caret) != caret
            || !StartsWithKeyword(text, indentEnd, caret, keyword))
        {
            return snapshot;
        }

        var above = lineStart;
        while (above > 0)
        {
            above = GetLineStart(text, above - 1);
            var aboveIndentEnd = SkipIndentation(text, above);
            var aboveEnd = GetLineEnd(text, above);
            if (StartsWithKeyword(text, aboveIndentEnd, aboveEnd, keyword))
                return snapshot;

            if (IsFunctionHeader(text, aboveIndentEnd, aboveEnd))
            {
                var indentation = text.Substring(above, aboveIndentEnd - above);
                return Apply(snapshot, [new Change(lineStart, indentEnd - lineStart, indentation)]);
            }
        }

        return snapshot;
    }

    /// <summary>
    /// <c>function = ...</c> is an assignment the compiler rejects, not a header.
    /// </summary>
    private static bool IsFunctionHeader(string text, int index, int end)
    {
        var keyword = FunctionDefinition.Keyword;
        if (!StartsWithKeyword(text, index, end, keyword))
            return false;

        var next = SkipIndentation(text, index + keyword.Length);
        return next >= end || text[next] != '=';
    }

    private static bool StartsWithKeyword(string text, int index, int end, string keyword)
    {
        var after = index + keyword.Length;
        return after <= end
            && string.CompareOrdinal(text, index, keyword, 0, keyword.Length) == 0
            && (after == end || !ScriptText.IsNamePart(text[after]));
    }
}
