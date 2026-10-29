using Imui.Core;
using Imui.Style;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// How deep Imui's stacks were before a drawer ran, so an exception partway through drawing can be undone: without it
/// the drawers after this one, and ZeepSDK's end of the frame, draw inside the scopes, window and clip rect it left
/// open. Reads Imui's private stacks, publicized like the game's.
/// </summary>
internal readonly struct GuiCheckpoint
{
    private readonly int _ids;
    private readonly int _readOnly;
    private readonly int _controlScopes;
    private readonly int _styles;
    private readonly ImStyleSheet _style;
    private readonly int _canvasSettings;
    private readonly int _layoutFrames;
    private readonly int _windows;

    private GuiCheckpoint(ImGui gui)
    {
        _ids = gui.idsStack.Count;
        _readOnly = gui.readOnlyStack.Count;
        _controlScopes = gui.controlScopesStack.Count;
        _styles = gui.styleStack.Count;
        _style = gui.Style;
        _canvasSettings = gui.Canvas.settingsStack.Count;
        _layoutFrames = gui.Layout.frames.Count;
        _windows = gui.WindowManager.drawingStack.Count;
    }

    public static GuiCheckpoint Take(ImGui gui)
    {
        return new GuiCheckpoint(gui);
    }

    /// <summary>
    /// Canvas settings are popped one by one rather than cut off, so the mesh drawn next uses the restored ones. A
    /// pushed style only knows its offset, not its type, so the whole style sheet is put back instead.
    /// </summary>
    public void Restore(ImGui gui)
    {
        while (gui.Canvas.settingsStack.Count > _canvasSettings)
            gui.Canvas.PopSettings();

        while (gui.Layout.frames.Count > _layoutFrames)
            gui.Layout.Pop();

        gui.idsStack.Count = _ids;
        gui.readOnlyStack.Count = _readOnly;
        gui.controlScopesStack.Count = _controlScopes;
        gui.styleStack.Count = _styles;
        gui.Style = _style;
        gui.WindowManager.drawingStack.Count = _windows;
    }
}
