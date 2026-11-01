using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeepSDK.LevelEditor;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Walks from the start screen to a new level in the level editor the way a player does, takes a screenshot of it, runs
/// the scenarios and quits. Each wait gives up after <see cref="WaitSeconds"/>, so a changed menu fails the run instead of hanging it.
/// </summary>
internal sealed class TestRun : IDisposable
{
    // Starts every line this plugin logs, so a run's lines can be picked out of the BepInEx log
    public const string Marker = "[ZLS-TEST]";
    private const float WaitSeconds = 120;

    private readonly ManualLogSource _logger;
    private readonly RunResults _results;
    private readonly RunRequest _request;
    private readonly string _folder;
    private int _scenesLoaded;
    private bool _enteredEditor;

    public TestRun(ManualLogSource logger, RunResults results, RunRequest request, string folder)
    {
        _logger = logger;
        _results = results;
        _request = request;
        _folder = folder;
        SceneManager.sceneLoaded += OnSceneLoaded;
        LevelEditorApi.EnteredLevelEditor += OnEnteredLevelEditor;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LevelEditorApi.EnteredLevelEditor -= OnEnteredLevelEditor;
    }

    /// <summary>
    /// Reads every scenario first, so one that can't be read fails without the game starting it. An exception anywhere
    /// in the walk fails the run, and still quits the game, instead of leaving the run script to time out.
    /// </summary>
    public IEnumerator Run()
    {
        var scenarios = ReadScenarios();
        if (scenarios.Count > 0)
        {
            Exception error = null;
            yield return GuardedRoutine.Run(Walk(), exception => error = exception);
            if (error != null)
            {
                _logger.LogError(error);
                Step(RunStep.Fail("no errors", $"{error.GetType().Name}: {error.Message}"));
                yield return Screenshot("failure");
            }
            else if (!_results.AnyStepFailed)
            {
                yield return RunScenarios(scenarios);
            }
        }

        yield return Finish();
    }

    private List<Scenario> ReadScenarios()
    {
        List<Scenario> scenarios = [];
        foreach (var file in _request.Scenarios)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var scenario = ScenarioParser.Parse(name, File.ReadAllText(Path.Combine(_folder, "scenarios", file)), out var error);
            if (scenario == null)
                Step(new RunStep { Scenario = name, Name = "read scenario", Detail = error });
            else
                scenarios.Add(scenario);
        }

        return scenarios;
    }

    /// <summary>
    /// A failed step stops its scenario with a screenshot, and the run goes on with the next.
    /// </summary>
    private IEnumerator RunScenarios(List<Scenario> scenarios)
    {
        var window = ((LogicScripting.Plugin)Chainloader.PluginInfos[LogicScripting.MyPluginInfo.PLUGIN_GUID].Instance).ScriptWindow;
        var driver = new ScriptWindow.Driver(window);
        foreach (var scenario in scenarios)
        {
            _logger.LogInfo($"{Marker} scenario {scenario.Name}");
            var context = new StepContext(scenario.Name, driver, _logger, Screenshot);
            foreach (var step in scenario.Steps)
            {
                context.Reset();
                var startedAt = Time.realtimeSinceStartup;
                yield return GuardedRoutine.Run(step.Run(context), exception =>
                {
                    _logger.LogError(exception);
                    context.Fail($"{exception.GetType().Name}: {exception.Message}");
                });

                Step(new RunStep
                {
                    Scenario = scenario.Name,
                    Line = step.Line,
                    Name = step.Source,
                    Passed = !context.Failed,
                    Detail = context.Detail,
                    Expected = context.Expected,
                    Actual = context.Actual,
                    Seconds = Math.Round(Time.realtimeSinceStartup - startedAt, 2),
                });

                if (context.Failed)
                {
                    yield return context.Screenshot("failure");
                    break;
                }
            }
        }
    }

    private IEnumerator Walk()
    {
        // BepInEx flushes its log file every couple of seconds, and the copies it skipped are logged before plugins load
        yield return new WaitForSecondsRealtime(3);
        Step(BuildCheck.Run(_folder));
        if (_results.AnyStepFailed)
            yield break;

        PressAnyKeyToStartGame startScreen = null;
        yield return WaitFor(() => startScreen = Find<PressAnyKeyToStartGame>(), "the start screen");
        yield return WaitUntil(() => startScreen.disableThisIntroPanel.activeInHierarchy && PlayerManager.Instance != null, "press any key");
        if (_results.AnyStepFailed)
            yield break;

        // Skips the main menu's one-time warning panels for this session, without changing settings
        PlayerManager.Instance.modPanelHasAlreadyBeenDisplayed = true;
        PlayerManager.Instance.epilepsyPanelHasAlreadyBeenDisplayed = true;
        startScreen.LoadNextLevel();
        Step(RunStep.Pass("start screen"));

        LevelEditorUI menu = null;
        yield return WaitFor(() => menu = Find<LevelEditorUI>(), "the main menu");
        if (_results.AnyStepFailed)
            yield break;

        // Let the menu finish opening, as a player can't click it any sooner
        yield return new WaitForSecondsRealtime(2);
        var blocksBefore = LevelChanges.Count;
        PressLevelEditorButton(menu);
        Step(RunStep.Pass("main menu"));

        yield return WaitUntil(() => _enteredEditor && LevelChanges.Count > blocksBefore, "the level editor");
        if (_results.AnyStepFailed)
            yield break;

        // The editor's own UI fades in after the level is ready
        yield return new WaitForSecondsRealtime(2);
        yield return Screenshot("editor");
        Step(RunStep.Pass("editor reached"));
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

        _logger.LogWarning($"{Marker} no Level Editor button found, starting the editor directly");
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
        if (_results.AnyStepFailed)
            yield break;

        var giveUpAt = Time.realtimeSinceStartup + WaitSeconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > giveUpAt)
            {
                Step(RunStep.Fail(what, $"not reached after {WaitSeconds} seconds"));
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
        _logger.LogInfo($"{Marker} screenshot {name}");
    }

    private void Step(RunStep step)
    {
        _results.Add(step);
        var scenario = step.Scenario.Length > 0 ? $"{step.Scenario}:{step.Line} " : string.Empty;
        var expected = step.Expected == null ? string.Empty : $" (expected: {step.Expected}, actual: {step.Actual})";
        var line = $"{Marker} {scenario}{step.Name} {(step.Passed ? "PASS" : "FAIL")} {step.Detail}{expected}".TrimEnd();
        if (step.Passed)
            _logger.LogInfo(line);
        else
            _logger.LogError(line);
    }

    private IEnumerator Finish()
    {
        _results.Finish();
        _logger.LogInfo($"{Marker} run {_results.RunId} {(_results.Passed ? "passed" : "failed")}");

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
