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
using ZeepSDK.Utilities;
using Object = UnityEngine.Object;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Walks from the start screen to a new level in the level editor the way a player does, takes a screenshot of it, runs
/// the scenarios, each on a new level of its own, and quits. Each wait gives up after <see cref="WaitSeconds"/>, so a
/// changed menu fails the run instead of hanging it.
/// </summary>
internal sealed class TestRun : IDisposable
{
    // Starts every line this plugin logs, so a run's lines can be picked out of the BepInEx log
    public const string Marker = "[ZLS-TEST]";
    private const float WaitSeconds = 120;

    private readonly ManualLogSource _logger;
    private readonly RunResults _results;
    private readonly ExceptionLog _exceptions;
    private readonly RunRequest _request;
    private readonly string _folder;
    private readonly GameEvents _game = new();

    // Set once a step of the walk failed, which ends the run, unlike a failed step of a scenario
    private bool _walkFailed;

    // The scenario a walk to a new level is for, so the results list its steps under it
    private string _walkScenario = string.Empty;

    public TestRun(ManualLogSource logger, RunResults results, ExceptionLog exceptions, RunRequest request, string folder)
    {
        _logger = logger;
        _results = results;
        _exceptions = exceptions;
        _request = request;
        _folder = folder;
        Application.logMessageReceived += OnLogMessage;
    }

    public void Dispose()
    {
        Application.logMessageReceived -= OnLogMessage;
        _game.Dispose();
    }

    /// <summary>
    /// Reads every scenario first, so one that can't be read fails without the game starting it. An exception anywhere
    /// in the walk fails the run, and still quits the game, instead of leaving the run script to time out.
    /// </summary>
    public IEnumerator Run()
    {
        List<Scenario> scenarios = [];
        Exception error = null;
        try
        {
            scenarios = ReadScenarios();
        }
        catch (Exception exception)
        {
            error = exception;
        }

        if (error == null && scenarios.Count > 0)
            yield return GuardedRoutine.Run(Walk(), exception => error = exception);

        if (error != null)
        {
            _logger.LogError(error);
            Step(RunStep.Fail("no errors", $"{error.GetType().Name}: {error.Message}"));
            yield return Screenshot("failure");
        }
        else if (scenarios.Count > 0 && !_walkFailed)
        {
            yield return RunScenarios(scenarios);
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
    /// A failed step stops its scenario with a screenshot, and the run goes on with the next, on a new level. The walk
    /// already brought the first one to a new level.
    /// </summary>
    private IEnumerator RunScenarios(List<Scenario> scenarios)
    {
        var window = ((LogicScripting.Plugin)Chainloader.PluginInfos[LogicScripting.MyPluginInfo.PLUGIN_GUID].Instance).ScriptWindow;
        var driver = new ScriptWindow.Driver(window);
        for (var i = 0; i < scenarios.Count; i++)
        {
            var scenario = scenarios[i];
            _logger.LogInfo($"{Marker} scenario {scenario.Name}");
            _exceptions.StartScenario(scenario.Name);
            if (i > 0)
            {
                yield return GuardedRoutine.Run(NewLevel(scenario.Name), WalkFailed);
                _walkScenario = string.Empty;
                if (_walkFailed)
                    yield break;
            }

            var context = new StepContext(scenario.Name, driver, _game, _exceptions, _logger, Screenshot);
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
        var buildCheck = BuildCheck.Run(_folder);
        Step(buildCheck);
        _walkFailed = !buildCheck.Passed;
        if (_walkFailed)
            yield break;

        PressAnyKeyToStartGame startScreen = null;
        yield return WaitFor(() => startScreen = Find<PressAnyKeyToStartGame>(), "the start screen");
        yield return WaitUntil(() => startScreen.disableThisIntroPanel.activeInHierarchy && PlayerManager.Instance != null, "press any key");
        if (_walkFailed)
            yield break;

        // Skips the main menu's one-time warning panels for this session, without changing settings
        PlayerManager.Instance.modPanelHasAlreadyBeenDisplayed = true;
        PlayerManager.Instance.epilepsyPanelHasAlreadyBeenDisplayed = true;
        startScreen.LoadNextLevel();
        Step(RunStep.Pass("start screen"));

        yield return EnterEditor();
        if (_walkFailed)
            yield break;

        yield return Screenshot("editor");
        Step(RunStep.Pass("editor reached"));
    }

    /// <summary>
    /// From the main menu, or on the way to it, to a new level in the editor.
    /// </summary>
    private IEnumerator EnterEditor()
    {
        LevelEditorUI menu = null;
        yield return WaitFor(() => menu = Find<LevelEditorUI>(), "the main menu");
        if (_walkFailed)
            yield break;

        // Let the menu finish opening, as a player can't click it any sooner
        yield return new WaitForSecondsRealtime(2);
        var entriesBefore = _game.EditorEntries;
        var blocksBefore = LevelChanges.Count;
        PressLevelEditorButton(menu);
        Step(RunStep.Pass("main menu"));

        yield return WaitUntil(() => _game.EditorEntries > entriesBefore && LevelChanges.Count > blocksBefore, "the level editor");
        if (_walkFailed)
            yield break;

        // The editor's own UI fades in after the level is ready
        yield return new WaitForSecondsRealtime(2);
    }

    /// <summary>
    /// Takes the game to a new level in the editor the way a player starts one: out of test mode if the last scenario
    /// stopped there, back to the main menu without saving, and into the editor again.
    /// </summary>
    private IEnumerator NewLevel(string scenario)
    {
        _walkScenario = scenario;
        if (TestModeControls.IsActive)
        {
            var loadsBefore = _game.LevelLoads;
            yield return TestModeControls.Leave();
            yield return WaitUntil(() => TestModeControls.IsLevelBack(_game, loadsBefore), "the level editor, from test mode");
            if (_walkFailed)
                yield break;
        }

        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
        {
            WalkFailed("new level", "not in the level editor, where a new level starts from");
            yield return Screenshot($"{scenario}-failure");
            yield break;
        }

        central.pause.QuitToMenu();
        if (central.unsavedContentPopup.IsOpen)
            central.unsavedContentPopup.Yes();

        yield return EnterEditor();
        if (!_walkFailed)
            Step(RunStep.Pass("new level"));
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
                if (checkedAt == _game.ScenesLoaded)
                    return false;

                checkedAt = _game.ScenesLoaded;
                return find() != null;
            },
            what);
    }

    private IEnumerator WaitUntil(Func<bool> condition, string what)
    {
        if (_walkFailed)
            yield break;

        var giveUpAt = Time.realtimeSinceStartup + WaitSeconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > giveUpAt)
            {
                WalkFailed(what, $"not reached after {WaitSeconds} seconds");
                yield return Screenshot(_walkScenario.Length > 0 ? $"{_walkScenario}-failure" : "failure");
                yield break;
            }

            yield return null;
        }
    }

    private void WalkFailed(Exception exception)
    {
        _logger.LogError(exception);
        WalkFailed("new level", $"{exception.GetType().Name}: {exception.Message}");
    }

    private void WalkFailed(string what, string detail)
    {
        _walkFailed = true;
        Step(RunStep.Fail(what, detail));
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
        if (step.Scenario.Length == 0)
            step.Scenario = _walkScenario;

        _results.Add(step);
        var scenario = step.Scenario.Length > 0 ? $"{step.Scenario}:{step.Line} " : string.Empty;
        var expected = step.Expected == null ? string.Empty : $" (expected: {step.Expected}, actual: {step.Actual})";
        var line = $"{Marker} {scenario}{step.Name} {(step.Passed ? "PASS" : "FAIL")} {step.Detail}{expected}".TrimEnd();
        if (step.Passed)
            _logger.LogInfo(line);
        else
            _logger.LogError(line);
    }

    private void OnLogMessage(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
            _exceptions.Record(message, stackTrace);
    }

    private IEnumerator Finish()
    {
        // What's logged from here on is the game quitting, which the run script reads from the log once it has quit
        Application.logMessageReceived -= OnLogMessage;
        _results.Finish();
        _logger.LogInfo($"{Marker} run {_results.RunId} {(_results.Passed ? "passed" : "failed")}");

        // Quitting this way skips the editor's unsaved-level prompt
        yield return null;
        Application.Quit();
    }
}
