using System.IO;
using BepInEx;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Drives the game from the start screen into the level editor and records what it finds, for tools/ingame-test.sh.
/// It does nothing in normal play: a run needs both the script's command-line flag and a run.json next to this DLL.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("Zeepkist.LogicScripting")]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private TestRun _run;

    private void Awake()
    {
        var folder = Path.GetDirectoryName(Info.Location);
        if (!RunRequest.TryRead(folder, System.Environment.GetCommandLineArgs(), out var request))
            return;

        Logger.LogInfo($"[ZLS-TEST] run {request.RunId} starting");
        var results = new RunResults(request.RunId, Path.Combine(folder, "out"));
        _run = new TestRun(Logger, results, folder);
        StartCoroutine(_run.Run());
    }

    private void OnDestroy()
    {
        _run?.Dispose();
    }
}
