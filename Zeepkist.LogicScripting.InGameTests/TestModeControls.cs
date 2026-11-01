using System;
using System.Collections;
using System.Linq;
using ZeepSDK.LevelEditor;
using ZeepSDK.Utilities;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Goes into test mode and back the way a player does: the level editor's Test key, and Quit in the pause menu.
/// </summary>
internal static class TestModeControls
{
    public static bool IsActive => LevelEditorApi.IsTestingLevel;

    public static bool HasStart(LEV_LevelEditorCentral central)
    {
        return central.undoRedo.allBlocksDictionary.Values.Any(block => block != null && block.isStart);
    }

    /// <summary>
    /// Calls <see cref="LEV_TestMap.TestMap"/>, as the Test key does. It does nothing in a level without a start block.
    /// </summary>
    public static void Enter(LEV_LevelEditorCentral central)
    {
        central.testMap.TestMap();
    }

    /// <summary>
    /// Opens the pause menu and presses Quit, which goes back to the editor from a test level.
    /// </summary>
    public static IEnumerator Leave()
    {
        var pauseMenu = Object.FindObjectOfType<PauseMenuUI>(true)
            ?? throw new InvalidOperationException("no pause menu to quit test mode from");
        pauseMenu.Open(true);
        yield return null;
        pauseMenu.OnQuit();
    }

    /// <summary>
    /// Whether the editor has loaded a level since <paramref name="loadsBefore"/> and placed all its blocks, as it does
    /// coming back from test mode.
    /// </summary>
    public static bool IsLevelBack(GameEvents game, int loadsBefore)
    {
        // Only looked up once a load started, so the wait doesn't search the scene every frame before then
        return game.LevelLoads > loadsBefore
            && ComponentCache.Get<LEV_LevelEditorCentral>() is { } central
            && !central.saveload.isLoading;
    }
}
