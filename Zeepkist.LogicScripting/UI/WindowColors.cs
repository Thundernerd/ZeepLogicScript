using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The few colors the window adds to the ZeepSDK theme.
/// </summary>
internal static class WindowColors
{
    public static readonly Color32 Error = new(230, 90, 90, 255);
    public static readonly Color32 Muted = new(160, 160, 160, 255);

    /// <summary>
    /// Laid over every other row of a table, light enough to show on any theme's background.
    /// </summary>
    public static readonly Color32 RowStripe = new(255, 255, 255, 10);
}
