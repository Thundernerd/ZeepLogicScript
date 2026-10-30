using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal static class WrappedText
{
    /// <summary>
    /// The height of <paramref name="text"/> wrapped to <paramref name="width"/>, so a short or empty text still takes
    /// <paramref name="minHeight"/>.
    /// </summary>
    public static float GetHeight(ImGui gui, string text, in ImTextSettings settings, float width, float minHeight)
    {
        // Imui doesn't wrap at all when the width is 0 or less, which would measure the text as one long line
        var bounds = new Vector2(Mathf.Max(1, width), 0);
        return Mathf.Max(minHeight, gui.MeasureTextSize(text, in settings, bounds).y);
    }
}
