using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Every step a scenario can use, by the name it's written with. A new step is a class in Steps/ and a line here.
/// </summary>
internal static class StepList
{
    public const string SetText = "set-text";
    public const string End = "end";

    /// <summary>
    /// Whether the tab is the Libraries tab.
    /// </summary>
    private static readonly Dictionary<string, bool> _tabs = new()
    {
        ["scripts"] = false,
        ["libraries"] = true,
    };

    /// <summary>
    /// Whether the libraries.json warning shows.
    /// </summary>
    private static readonly Dictionary<string, bool> _onOff = new()
    {
        ["on"] = true,
        ["off"] = false,
    };

    private static readonly Dictionary<string, Func<string, string, ScenarioStep>> _steps = new()
    {
        ["open-window"] = (step, argument) => None(step, argument, new OpenWindowStep()),
        ["close-window"] = (step, argument) => None(step, argument, new CloseWindowStep()),
        ["new-script"] = (step, argument) => new NewScriptStep(StepArgument.Text(step, argument, "a name")),
        ["select-script"] = (step, argument) => new SelectScriptStep(StepArgument.Text(step, argument, "a name")),
        ["new-library"] = (step, argument) => new NewLibraryStep(StepArgument.Text(step, argument, "a name")),
        ["select-library"] = (step, argument) => new SelectLibraryStep(StepArgument.Text(step, argument, "a name")),
        ["open-delete"] = (step, argument) => new OpenDeleteStep(StepArgument.Text(step, argument, "a name")),
        ["show-tab"] = (step, argument) => new ShowTabStep(StepArgument.Choice(step, argument, _tabs)),
        ["open-new"] = (step, argument) => None(step, argument, new OpenNewStep()),
        ["open-rename"] = (step, argument) => new OpenRenameStep(StepArgument.Text(step, argument, "a name")),
        ["type-name"] = (step, argument) => new TypeNameStep(StepArgument.Text(step, argument, "the name to type")),
        ["press-prompt"] = (step, argument) => new PressPromptStep(StepArgument.Choice(step, argument, PromptNames.Buttons)),
        ["select-group"] = (step, argument) => new SelectGroupStep(StepArgument.Text(step, argument, "a name")),
        ["expect-list-result"] = (step, argument) => new ExpectListResultStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-prompt"] = (step, argument) => new ExpectPromptStep(StepArgument.Choice(step, argument, PromptNames.Prompts)),
        ["expect-footer-in-view"] = (step, argument) => None(step, argument, new ExpectFooterInViewStep()),
        ["set-window-height"] = (step, argument) => new SetWindowHeightStep(StepArgument.Pixels(step, argument)),
        ["library-read-failed"] = (step, argument) => new LibraryReadFailedStep(StepArgument.Choice(step, argument, _onOff)),
        ["compile"] = (step, argument) => None(step, argument, new CompileStep()),
        ["expect-text"] = (step, argument) => new ExpectTextStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-result"] = (step, argument) => new ExpectResultStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-no-errors"] = (step, argument) => None(step, argument, new ExpectNoErrorsStep()),
        ["expect-errors"] = (step, argument) => new ExpectErrorsStep(StepArgument.Count(step, argument)),
        ["expect-error"] = ExpectError,
        ["expect-blocks"] = (step, argument) => new ExpectBlocksStep(StepArgument.Count(step, argument)),
        ["caret-at-start"] = (step, argument) => None(step, argument, new PlaceCaretStep(atEnd: false)),
        ["caret-at-end"] = (step, argument) => None(step, argument, new PlaceCaretStep(atEnd: true)),
        ["complete"] = (step, argument) => None(step, argument, new CompleteStep()),
        ["expect-completion"] = (step, argument) => ExpectCompletion(StepArgument.Text(step, argument, "a row's label, and optionally its detail")),
        ["expect-call-help"] = (step, argument) => new ExpectCallHelpStep(StepArgument.Text(step, argument, "the text to look for")),
        ["expect-call-help-active"] = (step, argument) => new ExpectCallHelpActiveStep(StepArgument.Text(step, argument, "the highlighted input's or Setting's label")),
        ["expect-call-help-no-active"] = (step, argument) => None(step, argument, new ExpectCallHelpActiveStep(null)),
        ["expect-no-completion"] = (step, argument) => None(step, argument, new ExpectNoCompletionStep()),
        ["expect-no-call-help"] = (step, argument) => None(step, argument, new ExpectNoCallHelpStep()),
        ["press"] = (step, argument) => new PressStep(StepArgument.Keys(step, argument, KeysStep.MaxKeysPerFrame)),
        ["type"] = (step, argument) => new TypeStep(StepArgument.Text(step, argument, "the text to type")),
        ["place-block"] = PlaceBlock,
        ["insert-selected"] = (step, argument) => None(step, argument, new InsertSelectedStep()),
        ["undo"] = (step, argument) => None(step, argument, new UndoStep()),
        ["redo"] = (step, argument) => None(step, argument, new RedoStep()),
        ["place-start"] = (step, argument) => None(step, argument, new PlaceStartStep()),
        ["test-mode"] = (step, argument) => None(step, argument, new TestModeStep()),
        ["back-to-editor"] = (step, argument) => None(step, argument, new BackToEditorStep()),
        ["wait"] = (step, argument) => new WaitStep(StepArgument.Seconds(step, argument)),
        ["screenshot"] = (step, argument) => new ScreenshotStep(StepArgument.FileName(step, argument)),
        ["expect-no-exceptions"] = (step, argument) => None(step, argument, new ExpectNoExceptionsStep()),
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

    /// <summary>
    /// A label has no spaces, so the rest of the line is the detail.
    /// </summary>
    private static ScenarioStep ExpectCompletion(string argument)
    {
        var space = argument.IndexOf(' ');
        return space < 0
            ? new ExpectCompletionStep(argument, null)
            : new ExpectCompletionStep(argument.Substring(0, space), argument.Substring(space + 1).Trim());
    }

    /// <summary>
    /// <c>&lt;line&gt;:&lt;column&gt; &lt;text&gt;</c>, where the text is the rest of the line.
    /// </summary>
    private static ScenarioStep ExpectError(string step, string argument)
    {
        var what = $"{step} takes a line and column, such as 2:5, then the text to look for";
        var space = argument.IndexOf(' ');
        var text = space < 0 ? string.Empty : argument.Substring(space + 1).Trim();
        if (text.Length == 0)
            throw new ScenarioFormatException(what);

        var written = argument.Substring(0, space);
        var position = written.Split(':');
        if (position.Length != 2 || !StepArgument.TryPositive(position[0], out var line) || !StepArgument.TryPositive(position[1], out var column))
            throw new ScenarioFormatException($"{what}, not '{written}'");

        return new ExpectErrorStep(new Position(line, column), text);
    }

    /// <summary>
    /// <c>&lt;function&gt;</c>, or <c>&lt;function&gt; as &lt;name&gt;</c> to remember the block by the name.
    /// </summary>
    private static ScenarioStep PlaceBlock(string step, string argument)
    {
        var what = "a Block Function's name, and optionally 'as <name>'";
        var words = StepArgument.Text(step, argument, what).Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return new PlaceBlockStep(words[0]);

        if (words[1] != "as")
            throw new ScenarioFormatException($"{step} takes {what}, not '{argument}'");

        var name = StepArgument.FileName($"{step} ... as", string.Join(" ", words.Skip(2)));
        return new PlaceBlockStep(words[0], name);
    }

    private static ScenarioStep None(string step, string argument, ScenarioStep created)
    {
        StepArgument.None(step, argument);
        return created;
    }
}
