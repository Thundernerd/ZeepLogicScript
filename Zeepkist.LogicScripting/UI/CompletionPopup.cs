using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Script editor's completion list. The editor stays focused while it's open: the list takes its keys and clicks
/// before the editor sees them, and only while it's open.
/// </summary>
internal sealed class CompletionPopup
{
    private const float MaxWidth = 480;

    /// <summary>
    /// The gap between a label and its detail, in layout spacings.
    /// </summary>
    private const float DetailGap = 4;

    private readonly CompletionSession _session;
    private readonly EditorAssist _assist;
    private IReadOnlyList<CompletionItem> _measuredItems;
    private float _labelWidth;
    private float _detailWidth;
    private bool _pressAccepted;
    private ImRect _rect;
    private ImRect _rows;
    private float _rowHeight;

    public CompletionPopup(EditorAssist assist)
    {
        _assist = assist;
        _session = new CompletionSession(assist.FindPlacedBlock);
    }

    /// <summary>
    /// Whether Escape closed the list this frame, so it doesn't also take the editor's focus.
    /// </summary>
    public bool TookEscape { get; private set; }

    /// <inheritdoc cref="CompletionSession.WordStart"/>
    public int? WordStart => _session.WordStart;

    public void Close()
    {
        _session.Close();
    }

    /// <summary>
    /// Under the line of <paramref name="wordPosition"/>, the top left of the word being completed, or above it when
    /// there's no room below; always inside <paramref name="bounds"/>, so no click on it reaches the level.
    /// </summary>
    public void Draw(ImGui gui, Vector2 wordPosition, float lineHeight, ImRect bounds)
    {
        _rect = default;
        if (!_session.IsOpen)
            return;

        Measure(gui);
        var padding = gui.Style.Menu.Padding;
        var spacing = gui.Style.Layout.Spacing;
        _rowHeight = lineHeight + spacing;
        var rows = _session.VisibleRows;
        var width = Mathf.Min(MaxWidth, _labelWidth + (_detailWidth > 0 ? (spacing * DetailGap) + _detailWidth : 0) + (spacing * 2) + padding.Horizontal);
        var height = (rows * _rowHeight) + padding.Vertical;
        var lineBottom = wordPosition.y - lineHeight;
        var y = lineBottom - height >= bounds.Y ? lineBottom - height : wordPosition.y;
        _rect = ImRectUtility.Clamp(bounds, new ImRect(wordPosition.x - padding.Left - spacing, y, width, height));
        _rows = _rect.WithPadding(padding);

        gui.BeginPopup();
        gui.Box(_rect, in gui.Style.Menu.Box);
        gui.RegisterControl(gui.GetControlId("completion"), _rect);

        var palette = SyntaxPalette.For(gui.Style.Menu.Box.BackColor, gui.Style.Text.Color);
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        var detailSettings = new ImTextSettings(gui.Style.Layout.TextSize, 1, 0.5f, overflow: ImTextOverflow.Ellipsis);
        var hovered = GetRowAt(gui.Input.MousePosition);
        var selected = _session.Selected;
        for (var i = _session.FirstRow; i < _session.FirstRow + rows; i++)
        {
            var row = GetRowRect(i);
            ref readonly var itemStyle = ref i == selected ? ref gui.Style.Menu.ItemActive : ref gui.Style.Menu.ItemNormal;
            var back = i == hovered && i != selected ? itemStyle.Hovered.BackColor : itemStyle.Normal.BackColor;
            gui.Canvas.Rect(row, back, itemStyle.BorderRadius);

            var item = _session.Items[i];
            var text = row.WithPadding(left: spacing, right: spacing);
            var label = text.TakeLeft(Mathf.Min(_labelWidth, text.W), out var detail);

            // The highlighted row's background can be the accent color, which the syntax colors aren't made for
            var kind = item.Kind switch
            {
                CompletionKind.Keyword => HighlightKind.Keyword,
                CompletionKind.Name or CompletionKind.Argument => HighlightKind.Name,
                _ => HighlightKind.Function
            };
            var labelColor = i == selected ? itemStyle.Normal.FrontColor : palette.Get(kind);
            var detailColor = i == selected ? itemStyle.Normal.FrontColor : WindowColors.Muted;
            gui.Text(item.Label, in settings, labelColor, label);
            if (item.Detail.Length > 0)
                gui.Text(item.Detail, in detailSettings, detailColor, detail.WithPadding(left: spacing * DetailGap));
        }

        gui.EndPopup();
    }

    /// <summary>
    /// Call before the editor handles the mouse. A click on a row accepts it, the wheel moves through the list, and
    /// every other event over the list is used up so the editor keeps focus. A click anywhere else closes the list.
    /// </summary>
    public TextSnapshot? HandleMouse(ImGui gui)
    {
        ref readonly var evt = ref gui.Input.MouseEvent;

        // The list closes on the press that accepts a row, so the drag and release after it would reach the editor
        // and move its caret
        if (_pressAccepted)
        {
            if (evt.Type is ImMouseEventType.Up or ImMouseEventType.Click or ImMouseEventType.Down)
                _pressAccepted = false;

            if (evt.Type is ImMouseEventType.BeginDrag or ImMouseEventType.Drag or ImMouseEventType.Up or ImMouseEventType.Click)
            {
                gui.Input.UseMouseEvent();
                return null;
            }
        }

        if (!_session.IsOpen || _rect.W <= 0)
            return null;

        if (!_rect.Contains(gui.Input.MousePosition))
        {
            if (evt.Type == ImMouseEventType.Down)
                Close();

            return null;
        }

        TextSnapshot? accepted = null;
        switch (evt.Type)
        {
            case ImMouseEventType.Down when evt.LeftButton:
                var row = GetRowAt(gui.Input.MousePosition);
                if (row >= 0)
                {
                    accepted = _session.Accept(row);
                    _pressAccepted = true;
                }

                break;

            case ImMouseEventType.Scroll when evt.Delta.y != 0:
                _session.Select(_session.Selected + (evt.Delta.y > 0 ? 1 : -1), wrap: false);
                break;
        }

        if (evt.Type != ImMouseEventType.None)
            gui.Input.UseMouseEvent();

        return accepted;
    }

    /// <summary>
    /// Call before the editor handles the keyboard. Ctrl+Space asks for the list; while it's open, Up and Down move
    /// through it, Enter and Tab accept and Escape closes it.
    /// </summary>
    public TextSnapshot? HandleKeyboard(ImGui gui)
    {
        TookEscape = false;

        // The same check as the one that unfocuses the editor, which Imui's own Escape event arrives a frame after
        if (_session.IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            TookEscape = true;
            return null;
        }

        for (var i = 0; i < gui.Input.KeyboardEventsCount; i++)
        {
            ref readonly var evt = ref gui.Input.GetKeyboardEvent(i);
            if (evt.Type != ImKeyboardEventType.Down)
                continue;

            var control = (evt.Modifiers & EventModifiers.Control) != 0;
            if (evt.Key == KeyCode.Space && control)
            {
                _session.Request();
                gui.Input.UseKeyboardEvent(i);
                continue;
            }

            if (!_session.IsOpen || (evt.Modifiers & (EventModifiers.Control | EventModifiers.Command | EventModifiers.Alt | EventModifiers.Shift)) != 0)
                continue;

            switch (evt.Key)
            {
                case KeyCode.DownArrow:
                    _session.Select(_session.Selected + 1, wrap: true);
                    gui.Input.UseKeyboardEvent(i);
                    break;

                case KeyCode.UpArrow:
                    _session.Select(_session.Selected - 1, wrap: true);
                    gui.Input.UseKeyboardEvent(i);
                    break;

                case KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Tab:
                    // Using the event clears it, and evt refers to it
                    var character = evt.Key == KeyCode.Tab ? '\t' : '\n';
                    gui.Input.UseKeyboardEvent(i);

                    // Unity sends the key's character as an event of its own, which would still type a line break or
                    // indent
                    UseCharacter(gui, character);
                    return _session.Accept(_session.Selected);
            }
        }

        return null;
    }

    /// <summary>
    /// Call after the editor handled its input, with the text and caret it ended with.
    /// </summary>
    public void Update(TextSnapshot snapshot, bool typedName, bool focused)
    {
        TookEscape &= focused;
        _session.Update(snapshot, typedName, _assist.CompletionWhileTyping, focused);
    }

    private ImRect GetRowRect(int index)
    {
        var bottom = _rows.Top - ((index - _session.FirstRow + 1) * _rowHeight);
        return new ImRect(_rows.X, bottom, _rows.W, _rowHeight);
    }

    private int GetRowAt(Vector2 position)
    {
        if (!_rect.Contains(position))
            return -1;

        var row = _session.FirstRow + Mathf.FloorToInt((_rows.Top - position.y) / _rowHeight);
        return row >= _session.FirstRow && row < _session.FirstRow + _session.VisibleRows ? row : -1;
    }

    private void Measure(ImGui gui)
    {
        if (ReferenceEquals(_measuredItems, _session.Items))
            return;

        _measuredItems = _session.Items;
        _labelWidth = 0;
        _detailWidth = 0;
        foreach (var item in _session.Items)
        {
            _labelWidth = Mathf.Max(_labelWidth, gui.MeasureTextSize(item.Label).x);
            _detailWidth = Mathf.Max(_detailWidth, gui.MeasureTextSize(item.Detail).x);
        }
    }

    private static void UseCharacter(ImGui gui, char character)
    {
        for (var i = 0; i < gui.Input.KeyboardEventsCount; i++)
        {
            ref readonly var evt = ref gui.Input.GetKeyboardEvent(i);
            if (evt.Key == KeyCode.None && evt.Char == character)
                gui.Input.UseKeyboardEvent(i);
        }
    }
}
