using System.Collections.Generic;
using BepInEx.Configuration;
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
    private const int MaxRows = 8;
    private const float MaxWidth = 480;

    private readonly ConfigEntry<bool> _whileTyping;
    private CaretContext _context;
    private IReadOnlyList<CompletionItem> _items = [];
    private IReadOnlyList<CompletionItem> _measuredItems;
    private float _labelWidth;
    private float _detailWidth;
    private bool _open;
    private bool _invoked;
    private bool _openRequested;
    private int _selected;
    private int _firstRow;
    private ImRect _rect;
    private ImRect _rows;
    private float _rowHeight;

    public CompletionPopup(ConfigEntry<bool> whileTyping)
    {
        _whileTyping = whileTyping;
    }

    /// <summary>
    /// Whether Escape closed the list this frame, so it doesn't also take the editor's focus.
    /// </summary>
    public bool TookEscape { get; private set; }

    /// <summary>
    /// Where the word being completed starts while the list is open.
    /// </summary>
    public int? WordStart => _open ? _context.Word.Start : null;

    public void Close()
    {
        _open = false;
        _invoked = false;
        _rect = default;
    }

    /// <summary>
    /// Under the line of <paramref name="wordPosition"/>, the top left of the word being completed, or above it when
    /// there's no room below; always inside <paramref name="bounds"/>, so no click on it reaches the level.
    /// </summary>
    public void Draw(ImGui gui, Vector2 wordPosition, float lineHeight, ImRect bounds)
    {
        _rect = default;
        if (!_open)
            return;

        Measure(gui);
        var padding = gui.Style.Menu.Padding;
        var spacing = gui.Style.Layout.Spacing;
        _rowHeight = lineHeight + spacing;
        var rows = Mathf.Min(MaxRows, _items.Count);
        var width = Mathf.Min(MaxWidth, _labelWidth + (_detailWidth > 0 ? spacing * 4 + _detailWidth : 0) + (spacing * 2) + padding.Horizontal);
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
        for (var i = _firstRow; i < _firstRow + rows; i++)
        {
            var row = GetRowRect(i);
            ref readonly var itemStyle = ref i == _selected ? ref gui.Style.Menu.ItemActive : ref gui.Style.Menu.ItemNormal;
            var back = i == hovered && i != _selected ? itemStyle.Hovered.BackColor : itemStyle.Normal.BackColor;
            gui.Canvas.Rect(row, back, itemStyle.BorderRadius);

            var item = _items[i];
            var text = row.WithPadding(left: spacing, right: spacing);
            var label = text.TakeLeft(Mathf.Min(_labelWidth, text.W), out var detail);

            // The highlighted row's background can be the accent color, which the syntax colors aren't made for
            var kind = item.Kind == CompletionKind.Keyword ? HighlightKind.Keyword : HighlightKind.Function;
            var labelColor = i == _selected ? itemStyle.Normal.FrontColor : palette.Get(kind);
            var detailColor = i == _selected ? itemStyle.Normal.FrontColor : WindowColors.Muted;
            gui.Text(item.Label, in settings, labelColor, label);
            if (item.Detail.Length > 0)
                gui.Text(item.Detail, in detailSettings, detailColor, detail.WithPadding(left: spacing * 4));
        }

        gui.EndPopup();
    }

    /// <summary>
    /// Call before the editor handles the mouse. A click on a row accepts it, the wheel moves through the list, and
    /// every other event over the list is used up so the editor keeps focus. A click anywhere else closes the list.
    /// </summary>
    public TextSnapshot? HandleMouse(ImGui gui)
    {
        if (!_open || _rect.W <= 0)
            return null;

        ref readonly var evt = ref gui.Input.MouseEvent;
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
                    accepted = Accept(row);

                break;

            case ImMouseEventType.Scroll when evt.Delta.y != 0:
                Select(_selected + (evt.Delta.y > 0 ? 1 : -1), wrap: false);
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
        if (_open && Input.GetKeyDown(KeyCode.Escape))
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
                _openRequested = true;
                _invoked = true;
                gui.Input.UseKeyboardEvent(i);
                continue;
            }

            if (!_open || (evt.Modifiers & (EventModifiers.Control | EventModifiers.Command | EventModifiers.Alt | EventModifiers.Shift)) != 0)
                continue;

            switch (evt.Key)
            {
                case KeyCode.DownArrow:
                    Select(_selected + 1, wrap: true);
                    gui.Input.UseKeyboardEvent(i);
                    break;

                case KeyCode.UpArrow:
                    Select(_selected - 1, wrap: true);
                    gui.Input.UseKeyboardEvent(i);
                    break;

                case KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Tab:
                    gui.Input.UseKeyboardEvent(i);

                    // Unity sends the key's character as an event of its own, which would still type a line break or
                    // indent
                    UseCharacter(gui, evt.Key == KeyCode.Tab ? '\t' : '\n');
                    return Accept(_selected);
            }
        }

        return null;
    }

    /// <summary>
    /// Call after the editor handled its input, with the text and caret it ended with. Opens the list on Ctrl+Space,
    /// or when a name character was typed and completion while typing is on; closes it when the caret leaves the word,
    /// when nothing matches, or when the editor loses focus or selects text.
    /// </summary>
    public void Update(string text, int caret, int selection, bool typedName, bool focused)
    {
        var requested = _openRequested || (typedName && _whileTyping.Value);
        _openRequested = false;
        if (!focused || selection != 0)
        {
            Close();
            return;
        }

        if (!_open && !requested)
            return;

        if (_context == null || _context.Text != text || _context.Caret != caret)
        {
            var wordStart = _context?.Word.Start;
            _context = CaretContext.At(text, caret);
            _items = Completion.GetItems(_context);
            _selected = 0;
            _firstRow = 0;
            if (_open && wordStart != _context.Word.Start)
                Close();
        }

        if (requested)
            _open = true;

        if (_items.Count == 0 || (_context.Prefix.Length == 0 && !_invoked))
            Close();
    }

    private TextSnapshot? Accept(int index)
    {
        var item = _items[index];
        Close();
        return Completion.Accept(_context, item);
    }

    private void Select(int index, bool wrap)
    {
        var count = _items.Count;
        _selected = wrap ? (index + count) % count : Mathf.Clamp(index, 0, count - 1);
        if (_selected < _firstRow)
            _firstRow = _selected;
        else if (_selected >= _firstRow + MaxRows)
            _firstRow = _selected - MaxRows + 1;
    }

    private ImRect GetRowRect(int index)
    {
        var bottom = _rows.Top - ((index - _firstRow + 1) * _rowHeight);
        return new ImRect(_rows.X, bottom, _rows.W, _rowHeight);
    }

    private int GetRowAt(Vector2 position)
    {
        if (!_rect.Contains(position))
            return -1;

        var row = _firstRow + Mathf.FloorToInt((_rows.Top - position.y) / _rowHeight);
        return row >= _firstRow && row < Mathf.Min(_items.Count, _firstRow + MaxRows) ? row : -1;
    }

    /// <summary>
    /// The widest label and detail, measured again only when the items change.
    /// </summary>
    private void Measure(ImGui gui)
    {
        if (ReferenceEquals(_measuredItems, _items))
            return;

        _measuredItems = _items;
        _labelWidth = 0;
        _detailWidth = 0;
        foreach (var item in _items)
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
