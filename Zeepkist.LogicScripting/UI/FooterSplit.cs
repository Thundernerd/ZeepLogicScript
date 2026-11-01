using System;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Splits a list tab's height between its scroll region and the parts around it that grow: the prompts and result line
/// above New, and a warning pinned above the list.
/// </summary>
internal static class FooterSplit
{
    public static float GetFooterHeight(float available, float wanted, float minListHeight)
    {
        return Math.Min(wanted, Math.Max(0, available - minListHeight));
    }

    /// <summary>
    /// Like <see cref="GetFooterHeight"/>, but a warning gets at least <paramref name="minHeight"/>, which the window's
    /// minimum height makes room for. <paramref name="minRestHeight"/> is what the list and its footer need under it.
    /// </summary>
    public static float GetWarningHeight(float available, float wanted, float minRestHeight, float minHeight)
    {
        return Math.Min(wanted, Math.Max(minHeight, available - minRestHeight));
    }
}
