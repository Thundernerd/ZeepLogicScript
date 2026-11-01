using System;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Splits a list tab's height between its scroll region and the part of the footer that grows, the prompts and result
/// line above New.
/// </summary>
internal static class FooterSplit
{
    public static float GetFooterHeight(float available, float wanted, float minListHeight)
    {
        return Math.Min(wanted, Math.Max(0, available - minListHeight));
    }
}
