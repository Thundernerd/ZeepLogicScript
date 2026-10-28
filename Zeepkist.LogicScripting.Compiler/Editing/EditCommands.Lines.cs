using System;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public static partial class EditCommands
{
    /// <summary>
    /// Swaps the selected lines with the line above (<paramref name="direction"/> -1) or below (+1); the caret and
    /// selection move along with them.
    /// </summary>
    public static TextSnapshot MoveLines(TextSnapshot snapshot, int direction)
    {
        var text = snapshot.Text;
        var lineStarts = ScriptText.GetLineStarts(text);
        GetSelectedLines(snapshot, lineStarts, out var first, out var last);
        if (direction < 0 ? first == 0 : last == lineStarts.Count - 1)
            return snapshot;

        var blockStart = lineStarts[first];
        var blockEnd = GetLineEnd(lineStarts, text.Length, last);
        var block = text.Substring(blockStart, blockEnd - blockStart);
        if (direction < 0)
        {
            var aboveStart = lineStarts[first - 1];
            var above = text.Substring(aboveStart, blockStart - 1 - aboveStart);
            var moved = text.Substring(0, aboveStart) + block + "\n" + above + text.Substring(blockEnd);
            return new TextSnapshot(moved, snapshot.Caret - above.Length - 1, snapshot.Selection);
        }
        else
        {
            var belowStart = blockEnd + 1;
            var belowEnd = GetLineEnd(lineStarts, text.Length, last + 1);
            var below = text.Substring(belowStart, belowEnd - belowStart);
            var moved = text.Substring(0, blockStart) + below + "\n" + block + text.Substring(belowEnd);

            // A selection that ends at the start of the line below ends past the text once that line goes above it
            var caret = Math.Min(snapshot.Caret + below.Length + 1, moved.Length);
            var anchor = Math.Min(GetAnchor(snapshot) + below.Length + 1, moved.Length);
            return new TextSnapshot(moved, caret, anchor - caret);
        }
    }

    /// <summary>
    /// Copies the selection to right after itself and selects the copy; without a selection, copies the caret's line
    /// to below it and puts the caret on the copy.
    /// </summary>
    public static TextSnapshot Duplicate(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        if (snapshot.Selection != 0)
        {
            var end = GetSelectionEnd(snapshot);
            var copy = text.Substring(GetSelectionStart(snapshot), end - GetSelectionStart(snapshot));
            return new TextSnapshot(text.Insert(end, copy), snapshot.Caret + copy.Length, snapshot.Selection);
        }

        var lineStart = GetLineStart(text, snapshot.Caret);
        var lineEnd = GetLineEnd(text, snapshot.Caret);
        var line = text.Substring(lineStart, lineEnd - lineStart);
        return new TextSnapshot(text.Insert(lineEnd, "\n" + line), snapshot.Caret + line.Length + 1);
    }

    /// <summary>
    /// Deletes the selected lines with their line break; the caret keeps its column on the line that takes their
    /// place.
    /// </summary>
    public static TextSnapshot DeleteLines(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var lineStarts = ScriptText.GetLineStarts(text);
        GetSelectedLines(snapshot, lineStarts, out var first, out var last);
        var column = snapshot.Caret - lineStarts[GetLine(lineStarts, snapshot.Caret)];

        var start = lineStarts[first];
        var end = GetLineEnd(lineStarts, text.Length, last);
        if (end < text.Length)
            end++;
        else if (start > 0)
            start--;

        var deleted = text.Remove(start, end - start);
        var lineStart = GetLineStart(deleted, start);
        var caret = Math.Min(lineStart + column, GetLineEnd(deleted, lineStart));
        return new TextSnapshot(deleted, caret);
    }
}
