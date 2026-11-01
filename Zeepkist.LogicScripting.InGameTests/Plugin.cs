using System.IO;
using BepInEx;
using HarmonyLib;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Drives the game from the start screen into the level editor, runs the scenarios there and records what it finds,
/// for tools/ingame-test.sh.
/// It does nothing in normal play: a run needs both the script's command-line flag and a run.json next to this DLL.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LogicScripting.MyPluginInfo.PLUGIN_GUID)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private TestRun _run;

    private void Awake()
    {
        var folder = Path.GetDirectoryName(Info.Location);
        if (!RunRequest.TryRead(folder, System.Environment.GetCommandLineArgs(), out var request))
            return;

        Logger.LogInfo($"{TestRun.Marker} run {request.RunId} starting");
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        var exceptions = new ExceptionLog();
        var results = new RunResults(request.RunId, Path.Combine(folder, "out"), exceptions.Logged);
        _run = new TestRun(Logger, results, exceptions, request, folder);
        StartCoroutine(_run.Run());
    }

    private void OnDestroy()
    {
        _run?.Dispose();
        _harmony?.UnpatchSelf();
    }
}
