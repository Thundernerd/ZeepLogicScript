using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// The editor's own undo and redo, separate from the level editor's. Typing characters in a row is one step, and so
/// is deleting them one by one; a pause, a typed line break or a caret move starts a new step. Any other edit, like a
/// paste or Insert selected, is exactly one step that the typing around it never joins.
/// </summary>
public sealed partial class EditHistory
{
    /// <summary>
    /// How many steps undo can go back.
    /// </summary>
    public const int MaxSteps = 500;

    /// <summary>
    /// The longest pause, in seconds, that still keeps typing in the same step.
    /// </summary>
    public const double GroupSeconds = 1;

    private readonly LinkedList<Step> _undo = new();
    private readonly Stack<Step> _redo = new();
    private bool _groupBroken;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Records one edit, joining the newest step when both are typing (or both deleting) that continues where it left
    /// off; a typed line break starts a new step that the typing after it joins. Drops the steps redo could go to. An
    /// edit that leaves the text as it was isn't recorded.
    /// </summary>
    public void Record(TextSnapshot before, TextSnapshot after, double time)
    {
        if (before.Text == after.Text)
            return;

        _redo.Clear();
        var kind = GetKind(before, after);
        var last = _undo.Last?.Value;
        if (!_groupBroken
            && last != null
            && kind != StepKind.Other
            && !IsLineBreakTyped(kind, before, after)
            && last.Kind == kind
            && last.After.Equals(before)
            && time - last.Time <= GroupSeconds)
        {
            last.After = after;
            last.Time = time;
            return;
        }

        _undo.AddLast(new Step(kind, before, after, time));
        if (_undo.Count > MaxSteps)
            _undo.RemoveFirst();

        _groupBroken = false;
    }

    /// <summary>
    /// Makes the next edit start a new step; call when the caret moves without editing.
    /// </summary>
    public void BreakGroup()
    {
        _groupBroken = true;
    }

    /// <summary>
    /// Steps back: <paramref name="snapshot"/> is the text as it was before the newest step.
    /// </summary>
    public bool TryUndo(out TextSnapshot snapshot)
    {
        if (_undo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        var step = _undo.Last.Value;
        _undo.RemoveLast();
        _redo.Push(step);
        _groupBroken = true;
        snapshot = step.Before;
        return true;
    }

    /// <summary>
    /// Steps forward again: <paramref name="snapshot"/> is the text as it was after the undone step.
    /// </summary>
    public bool TryRedo(out TextSnapshot snapshot)
    {
        if (_redo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        var step = _redo.Pop();
        _undo.AddLast(step);
        _groupBroken = true;
        snapshot = step.After;
        return true;
    }

    private static bool IsLineBreakTyped(StepKind kind, TextSnapshot before, TextSnapshot after)
    {
        return kind == StepKind.Typing && after.Text[before.Caret] == '\n';
    }

    /// <summary>
    /// Typing is one character inserted at the caret, or a line break with the indentation the editor keeps after
    /// it; deleting is one character removed next to the caret. Either one only when nothing was selected.
    /// </summary>
    private static StepKind GetKind(TextSnapshot before, TextSnapshot after)
    {
        if (before.Selection != 0)
            return StepKind.Other;

        var caret = before.Caret;
        if (after.Text.Length == before.Text.Length + 1
            && after.Caret == caret + 1
            && IsSameExcept(after.Text, before.Text, caret))
        {
            return StepKind.Typing;
        }

        if (IsIndentedLineBreak(before, after))
            return StepKind.Typing;

        if (after.Text.Length == before.Text.Length - 1
            && (after.Caret == caret - 1 || after.Caret == caret)
            && IsSameExcept(before.Text, after.Text, after.Caret))
        {
            return StepKind.Deleting;
        }

        return StepKind.Other;
    }

    private static bool IsIndentedLineBreak(TextSnapshot before, TextSnapshot after)
    {
        var caret = before.Caret;
        var added = after.Text.Length - before.Text.Length;
        if (added < 2 || after.Caret != caret + added || after.Text[caret] != '\n')
            return false;

        for (var i = caret + 1; i < after.Caret; i++)
        {
            if (after.Text[i] is not (' ' or '\t'))
                return false;
        }

        return string.CompareOrdinal(after.Text, 0, before.Text, 0, caret) == 0
            && string.CompareOrdinal(after.Text, after.Caret, before.Text, caret, before.Text.Length - caret) == 0;
    }

    /// <summary>
    /// Whether <paramref name="longer"/> is <paramref name="shorter"/> with one character added at
    /// <paramref name="index"/>.
    /// </summary>
    private static bool IsSameExcept(string longer, string shorter, int index)
    {
        return index >= 0
            && index < longer.Length
            && string.CompareOrdinal(longer, 0, shorter, 0, index) == 0
            && string.CompareOrdinal(longer, index + 1, shorter, index, shorter.Length - index) == 0;
    }
}
