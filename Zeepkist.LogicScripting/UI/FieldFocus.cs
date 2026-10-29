using Imui.Core;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Whether a text field has focus, which is when the window blocks the editor's keyboard input.
/// </summary>
internal static class FieldFocus
{
    /// <summary>
    /// Call right after drawing the field. Escape gives up focus, so the player can go back to the level without
    /// clicking it, unless the field already used it, as the Script editor does to close its completion list.
    /// </summary>
    public static bool Update(ImGui gui, uint fieldId, bool escapeUsed = false)
    {
        if (!gui.IsControlActive(fieldId))
            return false;

        if (escapeUsed)
        {
            UseLevelEditorEscape();
            return true;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return true;

        gui.ResetActiveControl();
        return false;
    }

    /// <summary>
    /// Giving up focus unblocks the level editor's keyboard, which also drops this frame's presses, so the editor never
    /// sees that Escape. A field that keeps focus has to drop it itself, or the editor opens its pause menu.
    /// </summary>
    private static void UseLevelEditorEscape()
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            return;

        central.input.Escape.buttonDown = false;
        central.input.MenuPause.buttonDown = false;
    }
}
