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
/// Has an undo and redo of its own, which never touch the level. Forked from Imui's <c>ImTextEdit.TextEdit</c> (MIT,
/// see THIRD_PARTY_NOTICES.md).
/// </summary>
internal sealed partial class ScriptEditor
{
    private readonly EditHistory _history = new();
    private readonly EditorAssist _assist;
    private readonly CompletionPopup _completion;
    private readonly CallHelpPopup _callHelp;
    private readonly HoverPopup _hover;
    private ImTextEditState _state;
    private ImTextLayout _layout = new() { Lines = new ImTextLine[64] };
    private IReadOnlyList<ErrorUnderline> _errors = [];
    private bool _focusRequested;
    private (TextSnapshot Before, TextSnapshot After)? _outdentedEnd;

    public ScriptEditor(string text, EditorAssist assist)
    {
        Text = text;
        _assist = assist;
        _completion = new CompletionPopup(assist);
        _callHelp = new CallHelpPopup(assist);
        _hover = new HoverPopup(assist);
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
    /// Whether Escape closed the completion list or hid the call's help this frame, which leaves the field focused.
    /// </summary>
    public bool TookEscape => _completion.TookEscape || _callHelp.TookEscape;

    /// <summary>
    /// Spans are character indices into <see cref="Text"/>.
    /// </summary>
    public void SetErrors(IReadOnlyList<ErrorUnderline> errors)
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
        JumpTo(ScriptText.GetIndex(Text, position));
    }

    private void JumpTo(int index)
    {
        _completion.Close();
        _state.Caret = index;
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
            DrawPopups(gui, textRect, textBounds, in buffer);
            completed = _completion.HandleMouse(gui);
        }

        DrawHover(gui, textRect, textBounds, in buffer, hovered && !gui.IsReadOnly);

        var jump = FindJump(gui, textRect, textBounds, in buffer, hovered && !gui.IsReadOnly);
        if (jump != null)
            DrawJumpUnderline(gui, jump, textRect, palette);

        if (!HandleJumpClick(gui, jump))
            HandleMouse(gui, ref buffer, textRect, selected, hovered);

        var changed = false;
        var typedName = false;
        var typedSeparator = false;
        if (selected)
        {
            ImTextEdit.DrawCaret(gui, _state.Caret, _state.BlinkTime, textRect, in _layout, in stateStyle, in buffer);
            if (completing)
            {
                completed ??= _completion.HandleKeyboard(gui);
                _callHelp.HandleKeyboard(_completion.TookEscape);
            }

            if (completed is { } accepted)
            {
                Replace(accepted.Text, accepted.Caret, gui.Input.Time);
                buffer = new ImTextEditBuffer(Text, gui.Arena, 0);
                changed = true;
            }

            changed |= HandleKeyboard(gui, ref buffer, textRect, !gui.IsReadOnly, out compileRequested, out typedName, out typedSeparator);
        }

        var focused = completing && gui.IsControlActive(Id);
        _completion.Update(GetSnapshot(), typedName, typedSeparator, focused);
        _callHelp.Update(GetSnapshot(), focused);

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
    /// The completion list at the word being completed and the call's help at the caret, kept inside the visible text,
    /// and inside the window so their clicks never reach the level.
    /// </summary>
    private void DrawPopups(ImGui gui, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer)
    {
        var bounds = GetPopupBounds(gui);
        if (_completion.WordStart is { } wordStart)
            _completion.Draw(gui, GetViewPosition(gui, wordStart, textRect, textBounds, in buffer), _layout.LineHeight, bounds);

        _callHelp.Draw(gui, GetViewPosition(gui, _state.Caret, textRect, textBounds, in buffer), _layout.LineHeight, bounds, _completion.Rect);
    }

    /// <summary>
    /// What the mouse rests on, at the start of the line under it; shown whether or not the field is focused.
    /// </summary>
    private void DrawHover(ImGui gui, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer, bool hovered)
    {
        if (!_hover.IsResting(gui, hovered))
            return;

        var index = FindCharacterAt(gui, gui.Input.MousePosition, textRect, textBounds, in buffer);
        if (_hover.Find(Text, index, _errors) is not { } info)
            return;

        var linePosition = GetViewPosition(gui, info.Span.Start, textRect, textBounds, in buffer);
        _hover.Draw(gui, info, linePosition, _layout.LineHeight, GetPopupBounds(gui));
    }

    /// <summary>
    /// The window, so a popup's clicks never reach the level.
    /// </summary>
    private static ImRect GetPopupBounds(ImGui gui)
    {
        return gui.WindowManager.TryGetDrawingWindowId(out var window)
            ? gui.WindowManager.GetWindowState(window).Rect
            : gui.Canvas.SafeScreenRect;
    }

    /// <summary>
    /// The character under <paramref name="mouse"/>, or the end of its line when it's just past it, where an error at
    /// the end of a line has its short underline; null over nothing else.
    /// </summary>
    private int? FindCharacterAt(ImGui gui, Vector2 mouse, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer)
    {
        if (!textBounds.Contains(mouse))
            return null;

        var caret = ImTextEdit.ViewToCaretPosition(mouse, gui.TextDrawer, textRect, in _layout, in buffer);
        var position = ImTextEdit.CaretToViewPosition(caret, gui.TextDrawer, textRect, in _layout, in buffer);
        if (mouse.y > position.y || mouse.y < position.y - _layout.LineHeight)
            return null;

        // The caret goes to the nearer side of a character, so a mouse left of it is on the character before
        if (mouse.x < position.x)
            return caret > 0 && Text[caret - 1] != '\n' ? caret - 1 : null;

        var isLineEnd = caret == Text.Length || Text[caret] == '\n';
        if (!isLineEnd)
            return caret;

        return mouse.x <= position.x + gui.TextDrawer.GetCharacterAdvance(' ', _layout.Size) ? caret : null;
    }

    private Vector2 GetViewPosition(ImGui gui, int index, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer)
    {
        var position = ImTextEdit.CaretToViewPosition(Mathf.Min(index, buffer.Length), gui.TextDrawer, textRect, in _layout, in buffer);
        position.x = Mathf.Clamp(position.x, textBounds.X, textBounds.Right);
        position.y = Mathf.Clamp(position.y, textBounds.Bottom + _layout.LineHeight, textBounds.Top);
        return position;
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
