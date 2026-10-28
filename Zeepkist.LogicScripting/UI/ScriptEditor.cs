using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using Imui.Rendering;
using Imui.Style;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Script editor: a multi-line text field with a line-number gutter, colours from the compiler's lexer, error
/// underlines and an undo and redo of its own, which never touch the level. Forked from Imui's
/// <c>ImTextEdit.TextEdit</c> (MIT, see THIRD_PARTY_NOTICES.md).
/// </summary>
internal sealed partial class ScriptEditor
{
    private readonly EditHistory _history = new();
    private ImTextEditState _state;
    private ImTextLayout _layout = new() { Lines = new ImTextLine[64] };
    private IReadOnlyList<TextSpan> _errors = [];
    private bool _focusRequested;

    public string Text { get; private set; } = string.Empty;

    /// <summary>
    /// The caret as it was when the field last had it; clicking a button takes focus but keeps it.
    /// </summary>
    public int Caret => _state.Caret;

    /// <summary>
    /// The field's control id, valid after the first <see cref="Draw"/>.
    /// </summary>
    public uint Id { get; private set; }

    /// <summary>
    /// The spans to underline as errors; character indices into <see cref="Text"/>.
    /// </summary>
    public void SetErrors(IReadOnlyList<TextSpan> errors)
    {
        _errors = errors ?? [];
    }

    /// <summary>
    /// Replaces the text as exactly one undo step of its own, as Insert selected does.
    /// </summary>
    public void Replace(string text, int caret, double time)
    {
        var before = GetSnapshot();
        Text = text ?? string.Empty;
        _state.Caret = Mathf.Clamp(caret, 0, Text.Length);
        _state.Selection = 0;
        _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
        _history.RecordStep(before, GetSnapshot(), time);
    }

    /// <summary>
    /// Puts the caret at a line and column, scrolls to it and focuses the field.
    /// </summary>
    public void JumpTo(Position position)
    {
        _state.Caret = ScriptText.GetIndex(Text, position);
        _state.Selection = 0;
        _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
        _history.BreakGroup();
        _focusRequested = true;
    }

    /// <summary>
    /// Draws the editor and handles its input. Returns whether the text changed; <paramref name="compileRequested"/>
    /// is set when Ctrl+Enter was pressed in it.
    /// </summary>
    public bool Draw(ImGui gui, ImRect rect, out bool compileRequested)
    {
        compileRequested = false;
        Id = gui.GetNextControlId();
        if (_focusRequested)
        {
            gui.SetActiveControl(Id);
            _state.BlinkTime = gui.Input.Time;
            _focusRequested = false;
        }

        ref readonly var style = ref gui.Style.TextEdit;
        var selected = gui.IsControlActive(Id);
        var hovered = gui.IsControlHovered(Id);
        ref readonly var stateStyle = ref selected ? ref style.Selected : ref style.Normal;
        var textSize = gui.Style.Layout.TextSize;
        var palette = SyntaxPalette.For(stateStyle.Box.BackColor, stateStyle.Box.FrontColor);

        gui.Box(rect, in stateStyle.Box);

        var gutterWidth = GetGutterWidth(gui, textSize);
        var textArea = new ImRect(rect.X + gutterWidth, rect.Y, rect.W - gutterWidth, rect.H);
        var textBounds = textArea.WithPadding((ImPadding)gui.Style.Layout.InnerSpacing + style.Padding);

        gui.Canvas.PushRectMask(rect, stateStyle.Box.BorderRadius);
        gui.Layout.Push(ImAxis.Vertical, textBounds, ImLayoutFlag.Root);
        gui.BeginScrollable();

        var buffer = new ImTextEditBuffer(Text, gui.Arena, 0);
        gui.TextDrawer.FillLayout(buffer, textBounds.W, textBounds.H, 0, 0, textSize, false, ImTextOverflow.Overflow, ref _layout);
        var textRect = gui.Layout.AddRect(_layout.Width, _layout.Height);

        if ((_state.Flag & ImTextEditStateFlag.NeedScrollToCaret) != 0)
        {
            _state.Flag &= ~ImTextEditStateFlag.NeedScrollToCaret;
            textRect.Position += ImTextEdit.ScrollToCaret(gui, _state, textRect, in _layout, buffer);
        }

        _state.Caret = Mathf.Clamp(_state.Caret, 0, buffer.Length);

        if (selected)
            ImTextEdit.DrawSelection(gui, _state.Caret, _state.Selection, textRect, in _layout, in stateStyle, in buffer);

        DrawText(gui, textRect, textBounds, palette);
        DrawErrors(gui, textRect, palette);
        HandleMouse(gui, ref buffer, textRect, selected, hovered);

        var changed = false;
        if (selected)
        {
            ImTextEdit.DrawCaret(gui, _state.Caret, _state.BlinkTime, textRect, in _layout, in stateStyle, in buffer);
            changed = HandleKeyboard(gui, ref buffer, textRect, !gui.IsReadOnly, out compileRequested);
        }

        gui.RegisterControl(Id, rect);
        gui.EndScrollable();
        gui.Layout.Pop();

        DrawGutter(gui, rect, gutterWidth, textRect, textBounds, palette);
        gui.Canvas.PopRectMask();

        return changed;
    }

    private void HandleMouse(ImGui gui, ref ImTextEditBuffer buffer, ImRect textRect, bool selected, bool hovered)
    {
        ref readonly var evt = ref gui.Input.MouseEvent;
        switch (evt.Type)
        {
            case ImMouseEventType.Down when evt.LeftButton && selected && hovered && evt.Count > 1:
                MoveCaretToMouse(gui, ref buffer, textRect);
                if (evt.Count == 2)
                    ImTextEdit.SelectWordOnTheSameLineAtCaret(ref _state, in _layout, buffer);
                else
                    ImTextEdit.SelectLineAtCaret(ref _state, in _layout, buffer);

                break;

            case ImMouseEventType.Down or ImMouseEventType.BeginDrag when evt.LeftButton && hovered:
                if (!selected)
                    gui.SetActiveControl(Id, ImControlFlag.Draggable);

                MoveCaretToMouse(gui, ref buffer, textRect);
                gui.Input.UseMouseEvent();
                break;

            case ImMouseEventType.Drag when selected:
                var caret = ImTextEdit.ViewToCaretPosition(gui.Input.MousePosition, gui.TextDrawer, textRect, in _layout, in buffer);
                _state.Selection -= caret - _state.Caret;
                _state.Caret = caret;
                _state.BlinkTime = gui.Input.Time;
                gui.Input.UseMouseEvent();
                ImTextEdit.ScrollToCaret(gui, _state, textRect, in _layout, buffer);
                break;

            case ImMouseEventType.Down when selected && !hovered:
                gui.ResetActiveControl();
                break;
        }
    }

    private void MoveCaretToMouse(ImGui gui, ref ImTextEditBuffer buffer, ImRect textRect)
    {
        _state.Selection = 0;
        _state.Caret = ImTextEdit.ViewToCaretPosition(gui.Input.MousePosition, gui.TextDrawer, textRect, in _layout, in buffer);
        _state.BlinkTime = gui.Input.Time;
        _history.BreakGroup();
    }

    private TextSnapshot GetSnapshot()
    {
        return new TextSnapshot(Text, _state.Caret, _state.Selection);
    }
}
