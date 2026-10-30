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
    private const string InputsLabel = "inputs";
    private const string OutputsLabel = "outputs";
    private const string SettingsLabel = "settings";

    /// <summary>
    /// The most of the box's width a name column takes, so the values beside it still have room.
    /// </summary>
    private const float MaxNameColumn = 0.45f;

    private readonly EditorAssist _assist;
    private readonly List<(string Text, Color32 Color, ImRect Rect, bool Wrap)> _pieces = [];
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
    /// Under the line whose top left is <paramref name="linePosition"/>, or above it when there's no room below;
    /// always inside <paramref name="bounds"/>, so it never covers the level.
    /// </summary>
    public void Draw(ImGui gui, HoverInfo info, Vector2 linePosition, float lineHeight, ImRect bounds)
    {
        var padding = gui.Style.Menu.Padding;
        var maxTextWidth = Mathf.Min(MaxWidth, bounds.W) - padding.Horizontal;
        if (!ReferenceEquals(_laidOut, info) || _laidOutWidth != maxTextWidth)
        {
            _laidOut = info;
            _laidOutWidth = maxTextWidth;
            _size = Layout(gui, info, maxTextWidth, lineHeight);
        }

        var width = _size.x + padding.Horizontal;
        var height = _size.y + padding.Vertical;
        var lineBottom = linePosition.y - lineHeight;
        var y = lineBottom - height >= bounds.Y ? lineBottom - height : linePosition.y;
        var rect = ImRectUtility.Clamp(bounds, new ImRect(linePosition.x - padding.Left, y, width, height));

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

        var hasName = info.Function != null || info.Argument != null || info.Definition != null;
        if (info.Errors.Count > 0 && hasName)
            layout.AddGap(gui.Style.Layout.Spacing);

        if (info.Function is { } entry)
        {
            AddEntry(gui, layout, entry, palette, text, name);
        }
        else if (info.Argument is { } argument)
        {
            layout.AddRows([(argument.Name, argument.Value)], name, text, 0);
        }
        else if (info.Definition != null)
        {
            layout.AddRows([($"{info.DefinitionLine}:", info.Definition)], WindowColors.Muted, text, 0);
        }

        return layout.Size;
    }

    private static void AddEntry(ImGui gui, PieceLayout layout, BlockReferenceEntry entry, SyntaxPalette palette, Color32 text, Color32 name)
    {
        var indent = gui.Style.Layout.Indent;
        layout.AddLine(entry.Function, palette.Get(HighlightKind.Function), 0);
        if (entry.Aliases.Length > 0)
            layout.AddLine($"alias {entry.Aliases}", WindowColors.Muted, 0);

        List<(string, string)> parts = [(InputsLabel, entry.Inputs)];
        if (entry.Outputs != null)
            parts.Add((OutputsLabel, entry.Outputs));

        layout.AddRows(parts, WindowColors.Muted, text, 0);
        if (entry.Outputs == null)
        {
            layout.AddLine(OutputsLabel, WindowColors.Muted, 0);
            layout.AddRows(ToPairs(entry.ModeOutputs), name, text, indent);
        }

        if (entry.Settings.Count > 0)
        {
            layout.AddLine(SettingsLabel, WindowColors.Muted, 0);
            layout.AddRows(ToPairs(entry.Settings), name, text, indent);
        }
    }

    private static List<(string, string)> ToPairs(IReadOnlyList<BlockReferenceRow> rows)
    {
        List<(string, string)> pairs = [];
        foreach (var row in rows)
        {
            pairs.Add((row.Name, row.Value));
        }

        return pairs;
    }
}
