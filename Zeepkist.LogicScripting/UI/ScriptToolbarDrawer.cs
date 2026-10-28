using Imui.Controls;
using Imui.Core;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The "Logic Script" menu in ZeepSDK's menu bar, which opens and closes the window like its key does.
/// </summary>
internal sealed class ScriptToolbarDrawer : IZeepToolbarDrawer
{
    private readonly ScriptWindow _window;

    public ScriptToolbarDrawer(ScriptWindow window)
    {
        _window = window;
    }

    public string MenuTitle => "Logic Script";

    public void DrawMenuItems(ImGui gui)
    {
        // The window only shows in the editor, outside test mode
        var available = LevelEditorApi.IsInLevelEditor && !LevelEditorApi.IsTestingLevel;
        gui.BeginReadOnly(!available);
        if (gui.Menu(_window.IsOpen ? "Close Script window" : "Open Script window") && available)
            _window.SetOpen(!_window.IsOpen);

        gui.EndReadOnly();
        if (!available)
            gui.TooltipAtLastControl("Only in the level editor");
    }
}
