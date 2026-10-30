using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    /// <summary>
    /// Every edit is recorded in the history; a caret move without an edit ends the current typing step.
    /// <paramref name="typedName"/> is set when a character that can start a name was typed, and
    /// <paramref name="typedSeparator"/> when one that can come before an argument was.
    /// </summary>
    private bool HandleKeyboard(
        ImGui gui,
        ref ImTextEditBuffer buffer,
        ImRect textRect,
        bool editable,
        out bool compileRequested,
        out bool typedName,
        out bool typedSeparator)
    {
        compileRequested = false;
        typedName = false;
        typedSeparator = false;
        var changed = false;
        var text = Text;
        for (var i = 0; i < gui.Input.KeyboardEventsCount; i++)
        {
            ref readonly var evt = ref gui.Input.GetKeyboardEvent(i);
            if (evt.Type != ImKeyboardEventType.Down)
                continue;

            var command = IsCommand(evt);
            if (command && evt.Key is KeyCode.Return or KeyCode.KeypadEnter)
            {
                compileRequested = true;
                gui.Input.UseKeyboardEvent(i);
                continue;
            }

            // Unity sends the character of a key press as an event of its own; with Ctrl held it's a control
            // character (or the line break of Ctrl+Enter), which must not end up in the text
            if (evt.Key == KeyCode.None && (command || IsUnprintable(evt.Char)))
            {
                gui.Input.UseKeyboardEvent(i);
                continue;
            }

            if (editable && command && TryStepHistory(evt, out var snapshot))
            {
                text = snapshot.Text;
                buffer = new ImTextEditBuffer(text, gui.Arena, 0);
                _state.Caret = snapshot.Caret;
                _state.Selection = snapshot.Selection;
                _state.BlinkTime = gui.Input.Time;
                _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
                changed = true;
                gui.Input.UseKeyboardEvent(i);
                continue;
            }

            // Using the event clears it, and evt refers to it
            var typed = evt.Char;
            var before = new TextSnapshot(text, _state.Caret, _state.Selection);
            if (RunShortcut(in evt, before, buffer, editable) is { } after)
            {
                gui.Input.UseKeyboardEvent(i);
                _state.Caret = after.Caret;
                _state.Selection = after.Selection;
                _state.BlinkTime = gui.Input.Time;
                if (after.Text != text)
                {
                    text = after.Text;
                    buffer = new ImTextEditBuffer(text, gui.Arena, 0);

                    // Enter is typing, which the typing after it joins; every other shortcut is a step of its own
                    if (typed == '\n')
                        _history.Record(before, after, gui.Input.Time);
                    else
                        _history.RecordStep(before, after, gui.Input.Time);

                    _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
                    changed = true;
                }
                else if (!after.Equals(before))
                {
                    _history.BreakGroup();
                    ImTextEdit.ScrollToCaret(gui, _state, textRect, in _layout, buffer);
                }

                continue;
            }

            if (!ImTextEdit.HandleKeyboardEvent(gui, in evt, ref _state, ref buffer, textRect, in _layout, true, editable, out var textChanged))
                continue;

            _state.BlinkTime = gui.Input.Time;
            gui.Input.UseKeyboardEvent(i);
            if (textChanged)
            {
                text = buffer.ToString();
                typedName |= ScriptText.IsNameStart(typed);
                typedSeparator |= ScriptText.IsArgumentSeparator(typed);
                var typedText = new TextSnapshot(text, _state.Caret, _state.Selection);
                _history.Record(before, typedText, gui.Input.Time);
                changed = true;

                // A step of its own, so undo can take the outdent back and keep the typed end
                var outdented = EditCommands.OutdentEnd(typedText);
                if (outdented.Text != text)
                {
                    text = outdented.Text;
                    buffer = new ImTextEditBuffer(text, gui.Arena, 0);
                    _state.Caret = outdented.Caret;
                    _history.RecordStep(typedText, outdented, gui.Input.Time);
                }

                // The new text's layout is built next frame, so scrolling to the caret has to wait for it
                _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
            }
            else
            {
                _history.BreakGroup();
                ImTextEdit.ScrollToCaret(gui, _state, textRect, in _layout, buffer);
            }
        }

        Text = text;
        return changed;
    }

    /// <summary>
    /// The editing shortcuts Imui's text field doesn't have. On macOS, Cmd jumps to the ends of the Script and Option
    /// deletes words, as Imui's arrow keys do there. A Tab character on its own is swallowed: the Tab key indents.
    /// </summary>
    private static TextSnapshot? RunShortcut(in ImKeyboardEvent evt, TextSnapshot before, ImTextEditBuffer buffer, bool editable)
    {
        var shift = (evt.Modifiers & EventModifiers.Shift) != 0;
        var alt = (evt.Modifiers & EventModifiers.Alt) != 0;
        var controlOrCommand = (evt.Modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0;
        var command = IsCommand(evt);
        var mac = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;
        var wordDelete = editable && (mac ? alt && !controlOrCommand : command);
        return evt.Key switch
        {
            KeyCode.Home when command => EditCommands.MoveTo(before, 0, shift),
            KeyCode.End when command => EditCommands.MoveTo(before, before.Text.Length, shift),
            KeyCode.Home => EditCommands.Home(before, shift),
            KeyCode.End => EditCommands.End(before, shift),
            KeyCode.UpArrow when mac && command => EditCommands.MoveTo(before, 0, shift),
            KeyCode.DownArrow when mac && command => EditCommands.MoveTo(before, before.Text.Length, shift),
            KeyCode.UpArrow when editable && alt && !controlOrCommand && !shift => EditCommands.MoveLines(before, -1),
            KeyCode.DownArrow when editable && alt && !controlOrCommand && !shift => EditCommands.MoveLines(before, 1),
            KeyCode.Backspace when wordDelete => DeleteWord(before, buffer, -1),
            KeyCode.Delete when wordDelete => DeleteWord(before, buffer, 1),
            KeyCode.D when editable && command && !shift => EditCommands.Duplicate(before),
            KeyCode.K when editable && command && shift => EditCommands.DeleteLines(before),
            KeyCode.Slash or KeyCode.KeypadDivide when editable && command => EditCommands.ToggleComment(before),
            KeyCode.Tab when editable && !controlOrCommand && !alt => shift ? EditCommands.Outdent(before) : EditCommands.Indent(before),
            _ when evt.Char == '\t' => before,
            _ when editable && evt.Char == '\n' && !controlOrCommand => EditCommands.NewLine(before),
            _ => null,
        };
    }

    /// <summary>
    /// Deletes to the same word boundary Ctrl+Left/Right (Option on macOS) moves to.
    /// </summary>
    private static TextSnapshot DeleteWord(TextSnapshot before, ImTextEditBuffer buffer, int direction)
    {
        return EditCommands.DeleteTo(before, ImTextEdit.FindEndOfWordOrSpacesSequence(before.Caret, direction, buffer));
    }

    private bool TryStepHistory(in ImKeyboardEvent evt, out TextSnapshot snapshot)
    {
        var shift = (evt.Modifiers & EventModifiers.Shift) != 0;
        if (evt.Key == KeyCode.Z && !shift)
            return _history.TryUndo(out snapshot);

        if (evt.Key == KeyCode.Y || (evt.Key == KeyCode.Z && shift))
            return _history.TryRedo(out snapshot);

        snapshot = default;
        return false;
    }

    /// <summary>
    /// Not with Alt, because AltGr arrives as Ctrl+Alt and types characters.
    /// </summary>
    private static bool IsCommand(in ImKeyboardEvent evt)
    {
        return (evt.Modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0
            && (evt.Modifiers & EventModifiers.Alt) == 0;
    }

    private static bool IsUnprintable(char c)
    {
        return char.IsControl(c) && c != '\n' && c != '\t';
    }
}
