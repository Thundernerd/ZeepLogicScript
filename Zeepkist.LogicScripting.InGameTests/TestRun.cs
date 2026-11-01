using System;
using System.Collections;
using System.IO;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Blocks;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeepSDK.LevelEditor;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Walks from the start screen to a new level in the level editor the way a player does, takes a screenshot of it and
/// quits. Each wait gives up after <see cref="WaitSeconds"/>, so a changed menu fails the run instead of hanging it.
/// </summary>
internal sealed class TestRun : IDisposable
{
    private const float WaitSeconds = 120;

    private readonly ManualLogSource _logger;
    private readonly RunResults _results;
    private readonly string _folder;
    private int _scenesLoaded;
    private bool _enteredEditor;

    public TestRun(ManualLogSource logger, RunResults results, string folder)
    {
        _logger = logger;
        _results = results;
        _folder = folder;
        SceneManager.sceneLoaded += OnSceneLoaded;
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
    }

    public IEnumerator Run()
    {
        // BepInEx flushes its log file every couple of seconds, and the copies it skipped are logged before plugins load
        yield return new WaitForSecondsRealtime(3);
        var problem = BuildCheck.Problem(_folder);
        Step("right build", problem == null, problem ?? "");
        if (_results.Failed)
        {
            yield return Finish();
            yield break;
        }

        PressAnyKeyToStartGame startScreen = null;
        yield return WaitFor(() => startScreen = Find<PressAnyKeyToStartGame>(), "the start screen");
        yield return WaitUntil(() => startScreen.disableThisIntroPanel.activeInHierarchy && PlayerManager.Instance != null, "press any key");
        if (_results.Failed)
        {
            yield return Finish();
            yield break;
        }

        // The main menu shows its one-time warning panels unless the session already did, which leaves settings alone
        PlayerManager.Instance.modPanelHasAlreadyBeenDisplayed = true;
        PlayerManager.Instance.epilepsyPanelHasAlreadyBeenDisplayed = true;
        startScreen.LoadNextLevel();
        Step("start screen", true);

        LevelEditorUI menu = null;
        yield return WaitFor(() => menu = Find<LevelEditorUI>(), "the main menu");
        if (_results.Failed)
        {
            yield return Finish();
            yield break;
        }

        // Let the menu finish opening, as a player can't click it any sooner
        yield return new WaitForSecondsRealtime(2);
        var blocksBefore = LevelChanges.Count;
        PressLevelEditorButton(menu);
        Step("main menu", true);

        yield return WaitUntil(() => _enteredEditor && LevelChanges.Count > blocksBefore, "the level editor");
        if (!_results.Failed)
        {
            // The editor's own UI fades in after the level is ready
            yield return new WaitForSecondsRealtime(2);
            yield return Screenshot("editor");
            Step("editor reached", true);
        }

        yield return Finish();
    }

    private static T Find<T>()
        where T : Object
    {
        return Object.FindObjectOfType<T>(true);
    }

    /// <summary>
    /// Clicks the main menu's Level Editor button, or calls what it calls when no button is wired to it.
    /// </summary>
    private void PressLevelEditorButton(LevelEditorUI menu)
    {
        foreach (var button in Object.FindObjectsOfType<GenericButton>(true))
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == menu
                    && button.onClick.GetPersistentMethodName(i) == nameof(LevelEditorUI.StartLevelEditor))
                {
                    button.onClick.Invoke();
                    return;
                }
            }
        }

        _logger.LogWarning("[ZLS-TEST] no Level Editor button found, starting the editor directly");
        menu.StartLevelEditor();
    }

    /// <summary>
    /// Looks for an object once now and once after each scene load, rather than searching the scene every frame.
    /// </summary>
    private IEnumerator WaitFor(Func<Object> find, string what)
    {
        var checkedAt = -1;
        return WaitUntil(
            () =>
            {
                if (checkedAt == _scenesLoaded)
                    return false;

                checkedAt = _scenesLoaded;
                return find() != null;
            },
            what);
    }

    private IEnumerator WaitUntil(Func<bool> condition, string what)
    {
        if (_results.Failed)
            yield break;

        var giveUpAt = Time.realtimeSinceStartup + WaitSeconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > giveUpAt)
            {
                Step(what, false, $"not reached after {WaitSeconds} seconds");
                yield return Screenshot("failure");
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator Screenshot(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(_results.ScreenshotPath(name), texture.EncodeToPNG());
        Object.Destroy(texture);
        _logger.LogInfo($"[ZLS-TEST] screenshot {name}");
    }

    private void Step(string name, bool passed, string detail = "")
    {
        _results.Add(name, passed, detail);
        var marker = $"[ZLS-TEST] {name} {(passed ? "PASS" : "FAIL")} {detail}".TrimEnd();
        if (passed)
            _logger.LogInfo(marker);
        else
            _logger.LogError(marker);
    }

    private IEnumerator Finish()
    {
        _results.Finish();
        _logger.LogInfo($"[ZLS-TEST] run {_results.RunId} {(_results.Passed ? "passed" : "failed")}");

        // Quitting this way skips the editor's unsaved-level prompt
        yield return null;
        Application.Quit();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _scenesLoaded++;
    }

    private void OnEnteredLevelEditor()
    {
        _enteredEditor = true;
    }
}
