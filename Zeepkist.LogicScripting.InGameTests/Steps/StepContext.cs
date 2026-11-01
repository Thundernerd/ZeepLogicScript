using System;
using System.Collections;
using BepInEx.Logging;
using TNRD.Zeepkist.LogicScripting.UI;
using UnityEngine;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

/// <summary>
/// What a step works with, and where it leaves its outcome. One per scenario, reset before each step.
/// </summary>
internal sealed class StepContext
{
    private readonly Func<string, IEnumerator> _screenshot;

    public StepContext(
        string scenario,
        ScriptWindow.Driver window,
        GameEvents game,
        ExceptionLog exceptions,
        ManualLogSource logger,
        Func<string, IEnumerator> screenshot)
    {
        Scenario = scenario;
        Window = window;
        Game = game;
        Exceptions = exceptions;
        Logger = logger;
        _screenshot = screenshot;
    }

    public string Scenario { get; }
    public ScriptWindow.Driver Window { get; }
    public GameEvents Game { get; }
    public ExceptionLog Exceptions { get; }
    public ManualLogSource Logger { get; }

    /// <summary>
    /// They last the scenario.
    /// </summary>
    public PlacedBlockNames PlacedBlocks { get; } = new();

    public bool Failed { get; private set; }

    /// <summary>
    /// What the step found, such as a result line, shown whether it passed or not.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    public string Expected { get; private set; }
    public string Actual { get; private set; }

    public void Reset()
    {
        Failed = false;
        Detail = string.Empty;
        Expected = null;
        Actual = null;
    }

    /// <summary>
    /// For a check that passed, so the results show what it compared.
    /// </summary>
    public void Pass(string detail, string expected, string actual)
    {
        Detail = detail;
        Expected = expected;
        Actual = actual;
    }

    public void Fail(string detail, string expected = null, string actual = null)
    {
        Failed = true;
        Detail = detail;
        Expected = expected;
        Actual = actual;
    }

    /// <summary>
    /// For a step on an entry of the shown tab's list that has none by the name.
    /// </summary>
    public void FailNoEntry(string name)
    {
        var list = Window.ShownList;
        Fail($"no {list.Noun} named \"{name}\" on the shown tab", name, string.Join(", ", list.Names));
    }

    /// <summary>
    /// Saved as <c>&lt;scenario&gt;-&lt;name&gt;.png</c> in the run's results.
    /// </summary>
    public IEnumerator Screenshot(string name)
    {
        return _screenshot($"{Scenario}-{name}");
    }

    /// <summary>
    /// Waits until <paramref name="condition"/> holds or <paramref name="seconds"/> pass, whichever is first, so the
    /// step checks again afterwards.
    /// </summary>
    public IEnumerator WaitUntil(Func<bool> condition, float seconds)
    {
        var giveUpAt = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < giveUpAt)
        {
            yield return null;
        }
    }

    /// <summary>
    /// The level editor, or null after failing the step when the game isn't in it.
    /// </summary>
    public LEV_LevelEditorCentral RequireEditor()
    {
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        if (central == null)
            Fail("not in the level editor");

        return central;
    }

    /// <summary>
    /// Fails the step when the window isn't open, as a player can't use its buttons then. Returns whether it is.
    /// </summary>
    public bool RequireOpenWindow()
    {
        if (!Window.IsOpen)
            Fail("the window isn't open: add open-window first");

        return Window.IsOpen;
    }

    /// <summary>
    /// Fails the step when no field in the window has focus, as keys typed then go to the level editor. Returns whether
    /// one has.
    /// </summary>
    public bool RequireFocusedField()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.IsFieldFocused)
            Fail("no field is focused: add caret-at-start or caret-at-end first");

        return Window.IsFieldFocused;
    }

    /// <summary>
    /// Like <see cref="RequireOpenWindow"/>, with a Script selected too.
    /// </summary>
    public bool RequireScript()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.HasScript)
            Fail("no Script selected: add new-script or select-script first");

        return Window.HasScript;
    }

    /// <summary>
    /// Like <see cref="RequireOpenWindow"/>, with a Script or a Library selected too, for a step on the editor's text.
    /// </summary>
    public bool RequireText()
    {
        if (!RequireOpenWindow())
            return false;

        if (!Window.HasText)
            Fail("no Script or Library selected: add new-script, select-script, new-library or select-library first");

        return Window.HasText;
    }
}
