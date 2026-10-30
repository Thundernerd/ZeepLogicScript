namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Picks the side of a line a Script editor popup goes on. The editor comes first, so a popup doesn't cover the
/// window's toolbar or the error list; the window is the fallback when the editor is too short for it, so its clicks
/// still never reach the level.
/// </summary>
internal static class PopupPlacement
{
    /// <summary>
    /// The first of the two bottoms where a popup of <paramref name="height"/> fits inside <paramref name="editor"/>,
    /// or else the first where it fits inside <paramref name="window"/>; null when neither fits there either.
    /// </summary>
    public static float? Choose(float first, float second, float height, (float Bottom, float Top) editor, (float Bottom, float Top) window)
    {
        if (Fits(first, height, editor))
            return first;

        if (Fits(second, height, editor))
            return second;

        if (Fits(first, height, window))
            return first;

        return Fits(second, height, window) ? second : null;
    }

    private static bool Fits(float bottom, float height, (float Bottom, float Top) bounds)
    {
        return bottom >= bounds.Bottom && bottom + height <= bounds.Top;
    }
}
