using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A piece of a popup's text in one color, laid out with its rect relative to the popup's top left.
/// </summary>
internal readonly struct TextPiece
{
    public TextPiece(string text, Color32 color, ImRect rect, bool wrap = false)
    {
        Text = text;
        Color = color;
        Rect = rect;
        Wrap = wrap;
    }

    public string Text { get; }

    public Color32 Color { get; }

    public ImRect Rect { get; }

    /// <summary>
    /// Whether it wraps inside <see cref="Rect"/> instead of staying on one line.
    /// </summary>
    public bool Wrap { get; }
}
