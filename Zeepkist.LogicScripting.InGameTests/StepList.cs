using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Every step a scenario can use, by the name it's written with. A new step is a class in Steps/ and a line here.
/// </summary>
internal static class StepList
{
    public const string SetText = "set-text";
    public const string End = "end";

    private static readonly Dictionary<string, Func<string, string, ScenarioStep>> _steps = new()
    {
        ["open-window"] = (step, argument) => None(step, argument, new OpenWindowStep()),
        ["close-window"] = (step, argument) => None(step, argument, new CloseWindowStep()),
        ["new-script"] = (step, argument) => new NewScriptStep(StepArgument.Text(step, argument, "a name")),
        ["select-script"] = (step, argument) => new SelectScriptStep(StepArgument.Text(step, argument, "a name")),
        ["open-delete"] = (step, argument) => new OpenDeleteStep(StepArgument.Text(step, argument, "a name")),
        ["compile"] = (step, argument) => None(step, argument, new CompileStep()),
        ["expect-text"] = (step, argument) => new ExpectTextStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-result"] = (step, argument) => new ExpectResultStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-no-errors"] = (step, argument) => None(step, argument, new ExpectNoErrorsStep()),
        ["expect-errors"] = (step, argument) => new ExpectErrorsStep(StepArgument.Count(step, argument)),
        ["expect-blocks"] = (step, argument) => new ExpectBlocksStep(StepArgument.Count(step, argument)),
        ["undo"] = (step, argument) => None(step, argument, new UndoStep()),
        ["redo"] = (step, argument) => None(step, argument, new RedoStep()),
        ["place-start"] = (step, argument) => None(step, argument, new PlaceStartStep()),
        ["test-mode"] = (step, argument) => None(step, argument, new TestModeStep()),
        ["back-to-editor"] = (step, argument) => None(step, argument, new BackToEditorStep()),
        ["wait"] = (step, argument) => new WaitStep(StepArgument.Seconds(step, argument)),
        ["screenshot"] = (step, argument) => new ScreenshotStep(StepArgument.FileName(step, argument)),
        ["log"] = (step, argument) => new LogStep(StepArgument.Text(step, argument, "the text to log")),
    };

    /// <summary>
    /// Includes <see cref="SetText"/>, which <see cref="ScenarioParser"/> reads itself as it spans several lines.
    /// </summary>
    public static IEnumerable<string> Names => [.. _steps.Keys, SetText];

    /// <summary>
    /// Returns null for an unknown step.
    /// </summary>
    public static ScenarioStep Create(string step, string argument)
    {
        return _steps.TryGetValue(step, out var create) ? create(step, argument) : null;
    }

    private static ScenarioStep None(string step, string argument, ScenarioStep created)
    {
        StepArgument.None(step, argument);
        return created;
    }
}
