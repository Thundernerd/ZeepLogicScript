using System.Collections.Generic;
using BepInEx.Configuration;
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
/// Has an undo and redo of its own, which never touch the level. Forked from Imui's <c>ImTextEdit.TextEdit</c> (MIT,
/// see THIRD_PARTY_NOTICES.md).
/// </summary>
internal sealed partial class ScriptEditor
{
    private readonly EditHistory _history = new();
    private readonly CompletionPopup _completion;
    private ImTextEditState _state;
    private ImTextLayout _layout = new() { Lines = new ImTextLine[64] };
    private IReadOnlyList<TextSpan> _errors = [];
    private bool _focusRequested;

    public ScriptEditor(string text, ConfigEntry<bool> completionWhileTyping)
    {
        Text = text;
        _completion = new CompletionPopup(completionWhileTyping);
    }

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
    /// Whether Escape closed the completion list this frame, which leaves the field focused.
    /// </summary>
    public bool TookEscape => _completion.TookEscape;

    /// <summary>
    /// Spans are character indices into <see cref="Text"/>.
    /// </summary>
    public void SetErrors(IReadOnlyList<TextSpan> errors)
    {
        _errors = errors ?? [];
    }

    /// <summary>
    /// Records exactly one undo step of its own.
    /// </summary>
    public void Replace(string text, int caret, double time)
    {
        _completion.Close();
        var before = GetSnapshot();
        Text = text ?? string.Empty;
        _state.Caret = Mathf.Clamp(caret, 0, Text.Length);
        _state.Selection = 0;
        _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
        _history.RecordStep(before, GetSnapshot(), time);
    }

    /// <summary>
    /// Also focuses the field.
    /// </summary>
    public void JumpTo(Position position)
    {
        _completion.Close();
        _state.Caret = ScriptText.GetIndex(Text, position);
        _state.Selection = 0;
        _state.Flag |= ImTextEditStateFlag.NeedScrollToCaret;
        _history.BreakGroup();
        _focusRequested = true;
    }

    /// <summary>
    /// Returns whether the text changed; <paramref name="compileRequested"/> is set when Ctrl+Enter was pressed in it.
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

        // The list takes its clicks and keys before the editor, which would otherwise lose focus or move the caret
        var completing = selected && !gui.IsReadOnly;
        TextSnapshot? completed = null;
        if (completing)
        {
            DrawCompletion(gui, textRect, textBounds, in buffer);
            completed = _completion.HandleMouse(gui);
        }

        HandleMouse(gui, ref buffer, textRect, selected, hovered);

        var changed = false;
        var typedName = false;
        if (selected)
        {
            ImTextEdit.DrawCaret(gui, _state.Caret, _state.BlinkTime, textRect, in _layout, in stateStyle, in buffer);
            if (completing)
                completed ??= _completion.HandleKeyboard(gui);

            if (completed is { } accepted)
            {
                Replace(accepted.Text, accepted.Caret, gui.Input.Time);
                buffer = new ImTextEditBuffer(Text, gui.Arena, 0);
                changed = true;
            }

            changed |= HandleKeyboard(gui, ref buffer, textRect, !gui.IsReadOnly, out compileRequested, out typedName);
        }

        _completion.Update(GetSnapshot(), typedName, completing && gui.IsControlActive(Id));

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

    /// <summary>
    /// At the word being completed, kept inside the visible text, and inside the window so its clicks never reach the
    /// level.
    /// </summary>
    private void DrawCompletion(ImGui gui, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer)
    {
        if (_completion.WordStart is not { } wordStart)
            return;

        var position = ImTextEdit.CaretToViewPosition(Mathf.Min(wordStart, buffer.Length), gui.TextDrawer, textRect, in _layout, in buffer);
        position.x = Mathf.Clamp(position.x, textBounds.X, textBounds.Right);
        position.y = Mathf.Clamp(position.y, textBounds.Bottom + _layout.LineHeight, textBounds.Top);
        var bounds = gui.WindowManager.TryGetDrawingWindowId(out var window)
            ? gui.WindowManager.GetWindowState(window).Rect
            : gui.Canvas.SafeScreenRect;

        _completion.Draw(gui, position, _layout.LineHeight, bounds);
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
