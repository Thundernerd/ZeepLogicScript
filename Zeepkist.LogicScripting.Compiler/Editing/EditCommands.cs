using System;
using System.Collections.Generic;
using System.Text;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The Script editor's own editing shortcuts, as changes from one <see cref="TextSnapshot"/> to the next. A line is a
/// line of the text, up to its line break; the editor doesn't wrap.
/// </summary>
public static partial class EditCommands
{
    /// <summary>
    /// The spaces one indentation level is.
    /// </summary>
    public const int IndentSize = 4;

    /// <summary>
    /// Puts the caret at <paramref name="index"/>; with <paramref name="select"/> the selection stretches from where
    /// it started to there.
    /// </summary>
    public static TextSnapshot MoveTo(TextSnapshot snapshot, int index, bool select)
    {
        index = Math.Max(0, Math.Min(index, snapshot.Text.Length));
        var selection = select ? GetAnchor(snapshot) - index : 0;
        return new TextSnapshot(snapshot.Text, index, selection);
    }

    /// <summary>
    /// Moves to the line's first character that isn't a space, or to its start when the caret is already there.
    /// </summary>
    public static TextSnapshot Home(TextSnapshot snapshot, bool select)
    {
        var lineStart = GetLineStart(snapshot.Text, snapshot.Caret);
        var indentEnd = SkipIndentation(snapshot.Text, lineStart);
        return MoveTo(snapshot, snapshot.Caret == indentEnd ? lineStart : indentEnd, select);
    }

    public static TextSnapshot End(TextSnapshot snapshot, bool select)
    {
        return MoveTo(snapshot, GetLineEnd(snapshot.Text, snapshot.Caret), select);
    }

    /// <summary>
    /// Deletes from the caret to <paramref name="boundary"/>, or only the selection when there is one.
    /// </summary>
    public static TextSnapshot DeleteTo(TextSnapshot snapshot, int boundary)
    {
        if (snapshot.Selection != 0)
            return ReplaceSelection(snapshot, string.Empty);

        boundary = Math.Max(0, Math.Min(boundary, snapshot.Text.Length));
        var start = Math.Min(snapshot.Caret, boundary);
        var end = Math.Max(snapshot.Caret, boundary);
        return new TextSnapshot(snapshot.Text.Remove(start, end - start), start);
    }

    private static TextSnapshot ReplaceSelection(TextSnapshot snapshot, string text)
    {
        var start = GetSelectionStart(snapshot);
        var end = GetSelectionEnd(snapshot);
        var replaced = snapshot.Text.Remove(start, end - start).Insert(start, text);
        return new TextSnapshot(replaced, start + text.Length);
    }

    /// <summary>
    /// Makes every change at once; the caret and the selection's other end keep their place in the text around them.
    /// </summary>
    private static TextSnapshot Apply(TextSnapshot snapshot, List<Change> changes)
    {
        var text = snapshot.Text;
        var builder = new StringBuilder(text.Length);
        var copied = 0;
        foreach (var change in changes)
        {
            builder.Append(text, copied, change.Index - copied).Append(change.Text);
            copied = change.Index + change.Length;
        }

        builder.Append(text, copied, text.Length - copied);
        var caret = Map(text, changes, snapshot.Caret);
        var anchor = Map(text, changes, GetAnchor(snapshot));
        return new TextSnapshot(builder.ToString(), caret, anchor - caret);
    }

    /// <summary>
    /// Where <paramref name="index"/> ends up after <paramref name="changes"/>. Text inserted at the start of a line goes
    /// after an index there, so a selection of whole lines takes in their new indentation.
    /// </summary>
    private static int Map(string text, List<Change> changes, int index)
    {
        var shift = 0;
        foreach (var change in changes)
        {
            if (index < change.Index || (index == change.Index && change.Length == 0 && IsLineStart(text, index)))
                break;

            if (index < change.Index + change.Length)
                return change.Index + shift;

            shift += change.Text.Length - change.Length;
        }

        return index + shift;
    }

    /// <summary>
    /// The lines the selection touches, or the caret's line. A selection that ends at the start of a line leaves that
    /// line out.
    /// </summary>
    private static void GetSelectedLines(TextSnapshot snapshot, List<int> lineStarts, out int first, out int last)
    {
        var start = GetSelectionStart(snapshot);
        var end = GetSelectionEnd(snapshot);
        first = GetLine(lineStarts, start);
        last = GetLine(lineStarts, end);
        if (last > first && lineStarts[last] == end)
            last--;
    }

    private static int GetLine(List<int> lineStarts, int index)
    {
        var found = lineStarts.BinarySearch(index);
        return found >= 0 ? found : ~found - 1;
    }

    private static int GetLineEnd(List<int> lineStarts, int textLength, int line)
    {
        return line + 1 < lineStarts.Count ? lineStarts[line + 1] - 1 : textLength;
    }

    private static int GetLineStart(string text, int index)
    {
        return index == 0 ? 0 : text.LastIndexOf('\n', index - 1) + 1;
    }

    private static int GetLineEnd(string text, int index)
    {
        var lineBreak = text.IndexOf('\n', index);
        return lineBreak < 0 ? text.Length : lineBreak;
    }

    private static bool IsLineStart(string text, int index)
    {
        return index == 0 || text[index - 1] == '\n';
    }

    private static int SkipIndentation(string text, int index)
    {
        while (index < text.Length && text[index] is ' ' or '\t')
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// The end of the selection that stays put while the caret moves.
    /// </summary>
    private static int GetAnchor(TextSnapshot snapshot)
    {
        return snapshot.Caret + snapshot.Selection;
    }

    private static int GetSelectionStart(TextSnapshot snapshot)
    {
        return Math.Min(snapshot.Caret, GetAnchor(snapshot));
    }

    private static int GetSelectionEnd(TextSnapshot snapshot)
    {
        return Math.Max(snapshot.Caret, GetAnchor(snapshot));
    }
}
