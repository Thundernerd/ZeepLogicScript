using System;
using BepInEx.Logging;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// The checks and failure handling every change the mod makes to the open level shares.
/// </summary>
internal static class LevelEdit
{
    public const string GrabbingMessage = "Let go of the grabbed blocks first";

    /// <summary>
    /// The level editor, or null with the reason it can't take a change now. The editor doesn't register undo steps,
    /// selection steps included, while blocks are grabbed.
    /// </summary>
    public static LEV_LevelEditorCentral GetCentral(out string blocker)
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            blocker = "Open the level editor first";
        else if (central.gizmos.isGrabbing)
            blocker = GrabbingMessage;
        else
            blocker = null;

        return blocker == null ? central : null;
    }

    /// <summary>
    /// Runs a <see cref="BlockGroupEdits"/> call and returns how many blocks it changed, or null when it threw. A batch
    /// that throws leaves the level unchanged (ADR 0007), so the exception only goes to the log.
    /// </summary>
    public static int? TryEditGroup(ManualLogSource logger, string description, Func<int> edit)
    {
        try
        {
            return edit();
        }
        catch (Exception e)
        {
            logger.LogError($"{description} failed: {e}");
            return null;
        }
    }
}
