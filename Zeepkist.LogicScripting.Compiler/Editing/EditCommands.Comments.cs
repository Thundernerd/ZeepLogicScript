using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public static partial class EditCommands
{
    /// <summary>
    /// Uncomments the selected lines when every one that isn't blank starts with <c>#</c>; otherwise puts <c>#</c> in
    /// front of each of them, lined up at the least indented one. Blank lines are left alone.
    /// </summary>
    public static TextSnapshot ToggleComment(TextSnapshot snapshot)
    {
        var text = snapshot.Text;
        var lineStarts = ScriptText.GetLineStarts(text);
        GetSelectedLines(snapshot, lineStarts, out var first, out var last);

        List<int> lines = [];
        var commented = true;
        var column = int.MaxValue;
        for (var line = first; line <= last; line++)
        {
            var indentEnd = SkipIndentation(text, lineStarts[line]);
            if (indentEnd == ScriptText.GetLineEnd(lineStarts, text.Length, line))
                continue;

            lines.Add(line);
            column = Math.Min(column, indentEnd - lineStarts[line]);
            commented &= text[indentEnd] == '#';
        }

        List<Change> changes = [];
        foreach (var line in lines)
        {
            if (commented)
            {
                var hash = SkipIndentation(text, lineStarts[line]);
                var length = hash + 1 < text.Length && text[hash + 1] == ' ' ? 2 : 1;
                changes.Add(new Change(hash, length, string.Empty));
            }
            else
            {
                changes.Add(new Change(lineStarts[line] + column, 0, "# "));
            }
        }

        return Apply(snapshot, changes);
    }
}
