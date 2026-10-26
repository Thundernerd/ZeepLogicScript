using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;

namespace TNRD.ZeepLogicScript.Spike
{
    internal class SpikeToolbarDrawer : IZeepToolbarDrawer
    {
        private readonly ManualLogSource _logger;
        private readonly SpikeEscapeWindow _escapeWindow;

        public SpikeToolbarDrawer(ManualLogSource logger, SpikeEscapeWindow escapeWindow)
        {
            _logger = logger;
            _escapeWindow = escapeWindow;
        }

        public string MenuTitle => "Logic Script";

        public void DrawMenuItems(ImGui gui)
        {
            if (!LevelEditorApi.IsInLevelEditor)
            {
                gui.Text("Open the level editor");
                return;
            }

            if (gui.Menu("Spike: place test group"))
                BatchCreateSpike.PlaceGroup(_logger);
            if (gui.Menu("Spike: recompile test group"))
                BatchCreateSpike.RecompileGroup(_logger);
            if (gui.Menu("Spike: inspect groups"))
                SpikeInspector.Inspect(_logger);
            if (gui.Menu("Spike: place collision test"))
                BatchCreateSpike.PlaceCollisionTest(_logger);
            if (gui.Menu("Spike: Escape test window"))
                _escapeWindow.Toggle();
            if (gui.Menu("Spike: check private method"))
                SpikeBlocks.CheckGenerateNewBlockSignature(_logger);
        }
    }
}
