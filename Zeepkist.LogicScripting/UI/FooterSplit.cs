using System;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Splits a list tab's height between its scroll region and the footer pinned under it.
/// </summary>
internal static class FooterSplit
{
    /// <summary>
    /// The footer gets the height it wants, but never so much that the list is left less than
    /// <paramref name="minListHeight"/>.
    /// </summary>
    public static float GetFooterHeight(float available, float wanted, float minListHeight)
    {
        return Math.Min(wanted, Math.Max(0, available - minListHeight));
    }
}
