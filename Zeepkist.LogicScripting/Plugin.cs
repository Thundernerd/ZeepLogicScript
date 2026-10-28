using BepInEx;
using HarmonyLib;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Storage;
using TNRD.Zeepkist.LogicScripting.UI;
using ZeepSDK.LevelEditor;
using ZeepSDK.Storage;
using ZeepSDK.UI;

namespace TNRD.Zeepkist.LogicScripting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK", "2.7.5")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private StartupCheck _startupCheck;
    private ScriptWindow _scriptWindow;
    private bool _returningFromTestMode;

    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        _startupCheck = new StartupCheck(Logger);
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
        LevelEditorApi.EnteredTestMode += OnEnteredTestMode;
        LevelEditorApi.LevelLoaded += OnLevelLoaded;

        var drafts = new DraftStore(StorageApi.CreateModStorage(this), Logger);
        _scriptWindow = new ScriptWindow(Logger, _startupCheck, drafts);
        UIApi.AddZeepGUIDrawer(_scriptWindow);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
        LevelEditorApi.EnteredTestMode -= OnEnteredTestMode;
        LevelEditorApi.LevelLoaded -= OnLevelLoaded;

        if (_scriptWindow != null)
        {
            UIApi.RemoveZeepGUIDrawer(_scriptWindow);
            _scriptWindow.ReleaseInput();
            _scriptWindow.SaveDrafts();
        }

        _harmony?.UnpatchSelf();
    }

    /// <summary>
    /// The window keeps its Scripts through test mode, which reloads the same level; coming from the main menu opens
    /// another level, or a new one.
    /// </summary>
    private void OnEnteredLevelEditor()
    {
        _startupCheck.RunOnce();
        if (!_returningFromTestMode)
            _scriptWindow.ForgetScripts();

        _returningFromTestMode = false;
    }

    private void OnEnteredTestMode()
    {
        _returningFromTestMode = true;
    }

    private void OnLevelLoaded()
    {
        _scriptWindow.ForgetScripts();
    }
}
