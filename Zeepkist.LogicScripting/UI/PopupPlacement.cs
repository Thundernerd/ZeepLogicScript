namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Picks the side of a line a Script editor popup goes on. The editor comes first, so a popup doesn't cover the
/// window's toolbar or the error list; the window is the fallback when the editor is too short for it, so its clicks
/// still never reach the level.
/// </summary>
internal static class PopupPlacement
{
    /// <summary>
    /// The bottom a popup of <paramref name="height"/> goes at; null when neither fits inside the window.
    /// </summary>
    public static float? Choose(float preferred, float fallback, float height, (float Bottom, float Top) editor, (float Bottom, float Top) window)
    {
        if (Fits(preferred, height, editor))
            return preferred;

        if (Fits(fallback, height, editor))
            return fallback;

        if (Fits(preferred, height, window))
            return preferred;

        return Fits(fallback, height, window) ? fallback : null;
    }

    private static bool Fits(float bottom, float height, (float Bottom, float Top) bounds)
    {
        return bottom >= bounds.Bottom && bottom + height <= bounds.Top;
    }
}
