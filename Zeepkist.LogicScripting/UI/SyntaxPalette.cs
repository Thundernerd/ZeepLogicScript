using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal readonly struct SyntaxPalette
{
    private static readonly Dictionary<HighlightKind, Color32> _dark = new()
    {
        [HighlightKind.Function] = new Color32(220, 200, 120, 255),
        [HighlightKind.Keyword] = new Color32(110, 165, 230, 255),
        [HighlightKind.Number] = new Color32(170, 210, 150, 255),
        [HighlightKind.String] = new Color32(220, 150, 110, 255),
        [HighlightKind.Operator] = new Color32(200, 160, 220, 255),
        [HighlightKind.Comment] = new Color32(120, 150, 110, 255)
    };

    private static readonly Dictionary<HighlightKind, Color32> _light = new()
    {
        [HighlightKind.Function] = new Color32(150, 100, 0, 255),
        [HighlightKind.Keyword] = new Color32(20, 80, 200, 255),
        [HighlightKind.Number] = new Color32(20, 120, 60, 255),
        [HighlightKind.String] = new Color32(170, 60, 20, 255),
        [HighlightKind.Operator] = new Color32(130, 40, 150, 255),
        [HighlightKind.Comment] = new Color32(90, 120, 80, 255)
    };

    private readonly Dictionary<HighlightKind, Color32> _colors;
    private readonly Color32 _text;

    private SyntaxPalette(Dictionary<HighlightKind, Color32> colors, Color32 text, Color32 lineNumber, Color32 error)
    {
        _colors = colors;
        _text = text;
        LineNumber = lineNumber;
        Error = error;
    }

    public Color32 LineNumber { get; }
    public Color32 Error { get; }

    public static SyntaxPalette For(Color32 background, Color32 text)
    {
        var lineNumber = Color32.Lerp(text, background, 0.5f);
        return ((Color)background).grayscale < 0.5f
            ? new SyntaxPalette(_dark, text, lineNumber, new Color32(240, 80, 80, 255))
            : new SyntaxPalette(_light, text, lineNumber, new Color32(210, 30, 30, 255));
    }

    /// <summary>
    /// Names, punctuation and text the lexer gave no kind keep the theme's text color.
    /// </summary>
    public Color32 Get(HighlightKind? kind)
    {
        return kind != null && _colors.TryGetValue(kind.Value, out var color) ? color : _text;
    }
}
