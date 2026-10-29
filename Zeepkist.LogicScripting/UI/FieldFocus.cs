using Imui.Core;
using UnityEngine;

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

        if (escapeUsed || !Input.GetKeyDown(KeyCode.Escape))
            return true;

        gui.ResetActiveControl();
        return false;
    }
}
