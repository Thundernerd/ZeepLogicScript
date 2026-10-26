using BepInEx;
using HarmonyLib;
using TNRD.ZeepLogicScript.Spike;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;

namespace TNRD.ZeepLogicScript
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK", "2.7.5")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();

            SpikeEscapeWindow escapeWindow = new(Logger);
            UIApi.AddZeepGUIDrawer(escapeWindow);
            UIApi.AddToolbarDrawer(new SpikeToolbarDrawer(Logger, escapeWindow));
            LevelEditorApi.EnteredLevelEditor += () => SpikeBlocks.CheckGenerateNewBlockSignature(Logger);

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
