using BepInEx;
using HarmonyLib;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.UI;
using ZeepSDK.LevelEditor;
using ZeepSDK.UI;

namespace TNRD.Zeepkist.LogicScripting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK", "2.7.5")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private StartupCheck _startupCheck;
    private ScriptWindow _scriptWindow;

    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        _startupCheck = new StartupCheck(Logger);
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;

        _scriptWindow = new ScriptWindow(Logger, _startupCheck);
        UIApi.AddZeepGUIDrawer(_scriptWindow);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;

        if (_scriptWindow != null)
        {
            UIApi.RemoveZeepGUIDrawer(_scriptWindow);
            _scriptWindow.ReleaseInput();
        }

        _harmony?.UnpatchSelf();
    }

    private void OnEnteredLevelEditor()
    {
        _startupCheck.RunOnce();
    }
}
