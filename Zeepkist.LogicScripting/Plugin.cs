using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Storage;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;
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
    private ConfigEntry<KeyCode> _toggleKey;
    private ScriptWindow _scriptWindow;
    private ScriptToolbarDrawer _toolbarDrawer;
    private bool _returningFromTestMode;
    private bool _hintLogged;

    private void Awake()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        _startupCheck = new StartupCheck(Logger);
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
        LevelEditorApi.EnteredTestMode += OnEnteredTestMode;
        LevelEditorApi.LevelLoaded += OnLevelLoaded;

        // F12 is Steam's screenshot key; F9 is the only F key the game itself leaves free
        _toggleKey = Config.Bind(
            "Window",
            "Toggle key",
            KeyCode.F9,
            "Opens and closes the Logic Script window in the level editor. None turns the key off; the Logic Script menu in ZeepSDK's menu bar still works.");

        var storage = StorageApi.CreateModStorage(this);
        var drafts = new DraftStore(storage, Logger);
        _scriptWindow = new ScriptWindow(Logger, _startupCheck, drafts, new WindowStateStore(storage, Logger), _toggleKey);
        UIApi.AddZeepGUIDrawer(_scriptWindow);
        _toolbarDrawer = new ScriptToolbarDrawer(_scriptWindow);
        UIApi.AddToolbarDrawer(_toolbarDrawer);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnDestroy()
    {
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
        LevelEditorApi.EnteredTestMode -= OnEnteredTestMode;
        LevelEditorApi.LevelLoaded -= OnLevelLoaded;

        if (_toolbarDrawer != null)
            UIApi.RemoveToolbarDrawer(_toolbarDrawer);

        if (_scriptWindow != null)
        {
            UIApi.RemoveZeepGUIDrawer(_scriptWindow);
            _scriptWindow.ReleaseInput();
            _scriptWindow.SaveDrafts();
            _scriptWindow.SaveState();
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
        LogHintOnce();
    }

    /// <summary>
    /// ZeepSDK's menu bar is hidden until its own key is set, so the log is where a player finds out how to open the
    /// window.
    /// </summary>
    private void LogHintOnce()
    {
        if (_hintLogged)
            return;

        _hintLogged = true;
        var key = _toggleKey.Value;
        Logger.LogInfo(key == KeyCode.None
            ? "Open the Logic Script window from the Logic Script menu in ZeepSDK's menu bar, or set a toggle key in the mod's settings"
            : $"Press {key} in the level editor to open the Logic Script window (also in the Logic Script menu of ZeepSDK's menu bar)");
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
