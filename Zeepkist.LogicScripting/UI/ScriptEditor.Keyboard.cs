using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    /// <summary>
    /// Handles this frame's keys: undo and redo, Ctrl+Enter, then Imui's own text editing. Every edit is recorded in
    /// the history; a caret move without an edit ends the current typing step.
    /// </summary>
    private bool HandleKeyboard(ImGui gui, ref ImTextEditBuffer buffer, ImRect textRect, bool editable, out bool compileRequested)
    {
        compileRequested = false;
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

            var before = new TextSnapshot(text, _state.Caret, _state.Selection);
            if (!ImTextEdit.HandleKeyboardEvent(gui, in evt, ref _state, ref buffer, textRect, in _layout, true, editable, out var textChanged))
                continue;

            _state.BlinkTime = gui.Input.Time;
            gui.Input.UseKeyboardEvent(i);
            if (textChanged)
            {
                text = buffer.ToString();
                _history.Record(before, new TextSnapshot(text, _state.Caret, _state.Selection), gui.Input.Time);
                changed = true;

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
    /// Ctrl+Z undoes; Ctrl+Y and Ctrl+Shift+Z redo.
    /// </summary>
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
    /// Ctrl, or Cmd on macOS; not with Alt, because AltGr arrives as Ctrl+Alt and types characters.
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
