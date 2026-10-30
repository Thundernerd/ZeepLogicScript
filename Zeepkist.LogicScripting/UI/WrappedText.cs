using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// How tall a piece of text is when it wraps to a width, so the rect it's drawn in fits all of its lines.
/// </summary>
internal static class WrappedText
{
    /// <summary>
    /// The height of <paramref name="text"/> wrapped to <paramref name="width"/>, never less than
    /// <paramref name="minHeight"/>, so a short or empty text still takes a whole line.
    /// </summary>
    public static float GetHeight(ImGui gui, string text, in ImTextSettings settings, float width, float minHeight)
    {
        var bounds = new Vector2(Mathf.Max(1, width), 0);
        return Mathf.Max(minHeight, gui.MeasureTextSize(text, in settings, bounds).y);
    }
}
