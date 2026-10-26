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

        public SpikeToolbarDrawer(ManualLogSource logger)
        {
            _logger = logger;
        }

        public string MenuTitle => "Logic Script";

        public void DrawMenuItems(ImGui gui)
        {
            if (!LevelEditorApi.IsInLevelEditor)
            {
                gui.Text("Open the level editor");
                return;
            }

            if (gui.Menu("Spike: place a & b"))
                BatchCreateSpike.Run(_logger);
        }
    }
}
