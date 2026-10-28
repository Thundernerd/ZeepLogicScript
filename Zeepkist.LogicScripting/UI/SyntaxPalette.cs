using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The editor's colours for each <see cref="HighlightKind"/>, the error underline and the line numbers, picked to read
/// on the theme's light or dark field background.
/// </summary>
internal readonly struct SyntaxPalette
{
    private readonly bool _isDark;
    private readonly Color32 _text;

    private SyntaxPalette(bool isDark, Color32 text, Color32 lineNumber)
    {
        _isDark = isDark;
        _text = text;
        LineNumber = lineNumber;
    }

    public Color32 LineNumber { get; }
    public Color32 Error => _isDark ? new Color32(240, 80, 80, 255) : new Color32(210, 30, 30, 255);

    public static SyntaxPalette For(Color32 background, Color32 text)
    {
        return new SyntaxPalette(((Color)background).grayscale < 0.5f, text, Color32.Lerp(text, background, 0.5f));
    }

    /// <summary>
    /// The colour for a kind; names, punctuation and text the lexer gave no kind keep the theme's text colour.
    /// </summary>
    public Color32 Get(HighlightKind? kind)
    {
        return kind switch
        {
            HighlightKind.Function => _isDark ? new Color32(220, 200, 120, 255) : new Color32(150, 100, 0, 255),
            HighlightKind.Keyword => _isDark ? new Color32(110, 165, 230, 255) : new Color32(20, 80, 200, 255),
            HighlightKind.Number => _isDark ? new Color32(170, 210, 150, 255) : new Color32(20, 120, 60, 255),
            HighlightKind.String => _isDark ? new Color32(220, 150, 110, 255) : new Color32(170, 60, 20, 255),
            HighlightKind.Operator => _isDark ? new Color32(200, 160, 220, 255) : new Color32(130, 40, 150, 255),
            HighlightKind.Comment => _isDark ? new Color32(120, 150, 110, 255) : new Color32(90, 120, 80, 255),
            _ => _text
        };
    }
}
