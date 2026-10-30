using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The box that tells about the name or error underline the mouse rests on: the Reference entry of a Block Function,
/// the row of a named argument, the line that defines a name, and an underline's message. It takes no clicks or keys.
/// </summary>
internal sealed partial class HoverPopup
{
    private const float MaxWidth = 480;
    private const double RestSeconds = 0.5;

    private readonly EditorAssist _assist;
    private readonly List<TextPiece> _pieces = [];
    private Vector2 _mousePosition;
    private double _restStart;
    private bool _keyPressed;
    private (string Text, IReadOnlyList<ErrorUnderline> Errors, int? Index) _found;
    private HoverInfo _info;
    private HoverInfo _laidOut;
    private float _laidOutWidth;
    private Vector2 _size;

    public HoverPopup(EditorAssist assist)
    {
        _assist = assist;
    }

    /// <summary>
    /// Call every frame: whether the mouse has rested on the editor long enough since it last moved or scrolled, with
    /// no key or button pressed since.
    /// </summary>
    public bool IsResting(ImGui gui, bool hovered)
    {
        var mouse = gui.Input.MousePosition;
        var time = gui.Input.Time;
        if (mouse != _mousePosition || Input.mouseScrollDelta != Vector2.zero)
        {
            _mousePosition = mouse;
            _restStart = time;
            _keyPressed = false;
        }

        _keyPressed |= Input.anyKeyDown;
        return hovered && !_keyPressed && time - _restStart >= RestSeconds;
    }

    /// <summary>
    /// Read again only when the text, its errors or the character change.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="index">The character the mouse is on; null when it's on none.</param>
    /// <param name="errors"></param>
    public HoverInfo Find(string text, int? index, IReadOnlyList<ErrorUnderline> errors)
    {
        if (ReferenceEquals(_found.Text, text) && ReferenceEquals(_found.Errors, errors) && _found.Index == index)
            return _info;

        _found = (text, errors, index);
        _info = index is { } at ? HoverInfo.At(text, at, errors, _assist.FindPlacedBlock) : null;
        return _info;
    }

    /// <summary>
    /// Under the line whose top left is <paramref name="linePosition"/>, or above it when there's no room below, as
    /// <see cref="PopupPlacement"/> picks; always inside <paramref name="window"/>, so it never covers the level.
    /// </summary>
    public void Draw(ImGui gui, HoverInfo info, Vector2 linePosition, float lineHeight, ImRect editor, ImRect window)
    {
        var padding = gui.Style.Menu.Padding;
        var maxTextWidth = Mathf.Min(MaxWidth, window.W) - padding.Horizontal;
        if (!ReferenceEquals(_laidOut, info) || _laidOutWidth != maxTextWidth)
        {
            _laidOut = info;
            _laidOutWidth = maxTextWidth;
            _size = Layout(gui, info, maxTextWidth, lineHeight);
        }

        var width = _size.x + padding.Horizontal;
        var height = _size.y + padding.Vertical;
        var below = linePosition.y - lineHeight - height;
        var y = PopupPlacement.Choose(below, linePosition.y, height, (editor.Y, editor.Top), (window.Y, window.Top)) ?? linePosition.y;
        var rect = ImRectUtility.Clamp(window, new ImRect(linePosition.x - padding.Left, y, width, height));

        gui.BeginPopup();
        gui.Box(rect, in gui.Style.Menu.Box);
        var line = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0);
        var wrapped = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0, wrap: true);
        var origin = new Vector2(rect.X + padding.Left, rect.Top - padding.Top);
        foreach (var piece in _pieces)
        {
            var pieceRect = piece.Rect;
            pieceRect.Position += origin;
            var settings = piece.Wrap ? wrapped : line;
            gui.Text(piece.Text, in settings, piece.Color, pieceRect);
        }

        gui.EndPopup();
    }

    /// <summary>
    /// The error messages first, then what the name is, laid out like the Reference tab. Fills <see cref="_pieces"/>
    /// with rects relative to the top left and returns the size they take.
    /// </summary>
    private Vector2 Layout(ImGui gui, HoverInfo info, float maxWidth, float lineHeight)
    {
        _pieces.Clear();
        var palette = SyntaxPalette.For(gui.Style.Menu.Box.BackColor, gui.Style.Text.Color);
        var text = gui.Style.Text.Color;
        var name = palette.Get(HighlightKind.Keyword);
        var layout = new PieceLayout(gui, _pieces, maxWidth, lineHeight);

        foreach (var error in info.Errors)
        {
            layout.AddLine(error, palette.Error, 0);
        }

        if (info.Errors.Count > 0 && info.HasName)
            layout.AddGap(gui.Style.Layout.Spacing);

        if (info.Function is { } entry)
        {
            AddEntry(gui, layout, entry.Layout, palette, text, name);
        }
        else if (info.Argument is { } argument)
        {
            layout.AddRows([new ReferenceRow(argument.Name, argument.Value)], name, text, 0);
        }
        else if (info.Definition != null)
        {
            layout.AddRows([new ReferenceRow($"{info.DefinitionLine}:", info.Definition)], WindowColors.Muted, text, 0);
        }

        return layout.Size;
    }

    private static void AddEntry(ImGui gui, PieceLayout layout, ReferenceEntryLayout entry, SyntaxPalette palette, Color32 text, Color32 name)
    {
        layout.AddLine(entry.Function, palette.Get(HighlightKind.Function), 0);
        if (entry.Note.Length > 0)
            layout.AddLine(entry.Note, WindowColors.Muted, 0);

        layout.AddRows(entry.Rows, WindowColors.Muted, text, 0);
        foreach (var group in entry.Groups)
        {
            layout.AddLine(group.Heading, WindowColors.Muted, 0);
            layout.AddRows(group.Rows, name, text, gui.Style.Layout.Indent);
        }
    }
}
