using System;
using System.Linq;
using System.Text.RegularExpressions;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using TNRD.Zeepkist.LogicScripting.UI;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class StepListTests
{
    [Theory]
    [InlineData("open-window", typeof(OpenWindowStep))]
    [InlineData("close-window", typeof(CloseWindowStep))]
    [InlineData("new-script Lights", typeof(NewScriptStep))]
    [InlineData("select-script Lights", typeof(SelectScriptStep))]
    [InlineData("new-library timing", typeof(NewLibraryStep))]
    [InlineData("select-library timing", typeof(SelectLibraryStep))]
    [InlineData("open-delete Lights", typeof(OpenDeleteStep))]
    [InlineData("show-tab libraries", typeof(ShowTabStep))]
    [InlineData("open-new", typeof(OpenNewStep))]
    [InlineData("open-rename Lights", typeof(OpenRenameStep))]
    [InlineData("type-name Lights", typeof(TypeNameStep))]
    [InlineData("press-prompt keep-blocks", typeof(PressPromptStep))]
    [InlineData("select-group Lights", typeof(SelectGroupStep))]
    [InlineData("expect-list-result Deleted", typeof(ExpectListResultStep))]
    [InlineData("expect-prompt none", typeof(ExpectPromptStep))]
    [InlineData("expect-footer-in-view", typeof(ExpectFooterInViewStep))]
    [InlineData("set-window-height 300", typeof(SetWindowHeightStep))]
    [InlineData("library-read-failed on", typeof(LibraryReadFailedStep))]
    [InlineData("library-read-failed off", typeof(LibraryReadFailedStep))]
    [InlineData("set-text\na = 1\nend", typeof(SetTextStep))]
    [InlineData("compile", typeof(CompileStep))]
    [InlineData("expect-text a = 1", typeof(ExpectTextStep))]
    [InlineData("expect-result Placed", typeof(ExpectResultStep))]
    [InlineData("expect-no-errors", typeof(ExpectNoErrorsStep))]
    [InlineData("expect-errors 2", typeof(ExpectErrorsStep))]
    [InlineData("expect-error 1:5 isn't defined", typeof(ExpectErrorStep))]
    [InlineData("expect-blocks 3", typeof(ExpectBlocksStep))]
    [InlineData("caret-at-start", typeof(PlaceCaretStep))]
    [InlineData("caret-at-end", typeof(PlaceCaretStep))]
    [InlineData("complete", typeof(CompleteStep))]
    [InlineData("expect-completion t", typeof(ExpectCompletionStep))]
    [InlineData("expect-call-help logic(", typeof(ExpectCallHelpStep))]
    [InlineData("expect-call-help-active op:", typeof(ExpectCallHelpActiveStep))]
    [InlineData("expect-call-help-no-active", typeof(ExpectCallHelpActiveStep))]
    [InlineData("expect-no-completion", typeof(ExpectNoCompletionStep))]
    [InlineData("expect-no-call-help", typeof(ExpectNoCallHelpStep))]
    [InlineData("press ctrl+space", typeof(PressStep))]
    [InlineData("type b = a", typeof(TypeStep))]
    [InlineData("place-block lamp", typeof(PlaceBlockStep))]
    [InlineData("place-block lamp as lamp", typeof(PlaceBlockStep))]
    [InlineData("insert-selected", typeof(InsertSelectedStep))]
    [InlineData("undo", typeof(UndoStep))]
    [InlineData("redo", typeof(RedoStep))]
    [InlineData("place-start", typeof(PlaceStartStep))]
    [InlineData("test-mode", typeof(TestModeStep))]
    [InlineData("back-to-editor", typeof(BackToEditorStep))]
    [InlineData("wait 1", typeof(WaitStep))]
    [InlineData("screenshot compiled", typeof(ScreenshotStep))]
    [InlineData("log hello", typeof(LogStep))]
    [InlineData("expect-no-exceptions", typeof(ExpectNoExceptionsStep))]
    public void EachStep_ParsesToItsOwnClass(string line, Type type)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(error);
        Assert.IsType(type, Assert.Single(scenario.Steps));
    }

    [Theory]
    [InlineData("place-start now")]
    [InlineData("test-mode 5")]
    [InlineData("back-to-editor now")]
    public void TheTestModeSteps_TakeNothingAfterThem(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains("takes nothing after it", error);
    }

    [Theory]
    [InlineData("expect-completion a", "a", null)]
    [InlineData("expect-completion a logic", "a", "logic")]
    [InlineData("expect-completion a placed block", "a", "placed block")]
    public void ExpectCompletion_ALabel_ThenTheDetail(string line, string label, string detail)
    {
        var step = Assert.IsType<ExpectCompletionStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(label, step.Label);
        Assert.Equal(detail, step.Detail);
    }

    [Theory]
    [InlineData("expect-call-help-active op:", "op:")]
    [InlineData("expect-call-help-no-active", null)]
    public void ExpectCallHelpActive_ReadsTheLabel(string line, string label)
    {
        var step = Assert.IsType<ExpectCallHelpActiveStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(label, step.Label);
    }

    [Theory]
    [InlineData("press enter", "Return", '\n', (int)KeyModifiers.None)]
    [InlineData("press ctrl+space", "Space", ' ', (int)KeyModifiers.Control)]
    [InlineData("press ctrl+/", "Slash", '/', (int)KeyModifiers.Control)]
    [InlineData("press Shift+A", "A", 'A', (int)KeyModifiers.Shift)]
    [InlineData("press ctrl+shift+k", "K", 'K', (int)(KeyModifiers.Control | KeyModifiers.Shift))]
    [InlineData("press escape", "Escape", '\0', (int)KeyModifiers.None)]
    [InlineData("press 7", "Alpha7", '7', (int)KeyModifiers.None)]
    public void Press_ReadsTheKeyAndItsModifiers(string line, string key, char character, int modifiers)
    {
        var step = Assert.IsType<PressStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        var press = Assert.Single(step.Keys);
        Assert.Equal(key, press.Key);
        Assert.Equal(character, press.Character);
        Assert.Equal((KeyModifiers)modifiers, press.Modifiers);
    }

    [Fact]
    public void Press_SeveralKeys_KeepsTheirOrder()
    {
        var step = Assert.IsType<PressStep>(Assert.Single(ScenarioParser.Parse("steps", "press  escape enter", out _).Steps));

        Assert.Equal(["Escape", "Return"], step.Keys.Select(k => k.Key));
    }

    [Theory]
    [InlineData("press", "press takes 1 to 8 keys, such as enter or ctrl+space")]
    [InlineData("press a b c d e f g h i", "press takes 1 to 8 keys, such as enter or ctrl+space, not 9")]
    [InlineData("press ctrl+", "press doesn't know the key 'ctrl+'")]
    [InlineData("press hyper+a", "press doesn't know the key 'hyper+a'")]
    [InlineData("press (", "press doesn't know the key '('")]
    public void Press_WithoutKnownKeys_Fails(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Theory]
    [InlineData('a', "A", (int)KeyModifiers.None)]
    [InlineData('A', "A", (int)KeyModifiers.Shift)]
    [InlineData(' ', "Space", (int)KeyModifiers.None)]
    [InlineData('=', "Equals", (int)KeyModifiers.None)]
    [InlineData('(', "None", (int)KeyModifiers.None)]
    public void Type_SendsTheKeyForEachCharacter(char character, string key, int modifiers)
    {
        var press = KeyNames.ForCharacter(character);

        Assert.Equal(key, press.Key);
        Assert.Equal(character, press.Character);
        Assert.Equal((KeyModifiers)modifiers, press.Modifiers);
    }

    [Fact]
    public void ExpectError_APosition_ThenTheText()
    {
        var step = Assert.IsType<ExpectErrorStep>(Assert.Single(ScenarioParser.Parse("steps", "expect-error 12:3 'nope' isn't defined", out _).Steps));

        Assert.Equal((12, 3), (step.Position.Line, step.Position.Column));
        Assert.Equal("'nope' isn't defined", step.Text);
    }

    [Theory]
    [InlineData("expect-error 1:5")]
    [InlineData("expect-error nope")]
    [InlineData("expect-error 1 text")]
    [InlineData("expect-error 0:1 text")]
    [InlineData("expect-error 1:x text")]
    [InlineData("expect-error 1:2:3 text")]
    public void ExpectError_NeedsAOneBasedPosition_AndText(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains("takes a line and column", error);
    }

    [Theory]
    [InlineData("show-tab scripts", false)]
    [InlineData("show-tab libraries", true)]
    public void ShowTab_ReadsTheTab(string line, bool libraries)
    {
        var step = Assert.IsType<ShowTabStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(libraries, step.Libraries);
    }

    [Theory]
    [InlineData("library-read-failed on", true)]
    [InlineData("library-read-failed off", false)]
    public void LibraryReadFailed_ReadsOnOrOff(string line, bool failed)
    {
        var step = Assert.IsType<LibraryReadFailedStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(failed, step.Failed);
    }

    [Theory]
    [InlineData("library-read-failed", "library-read-failed takes on or off")]
    [InlineData("library-read-failed yes", "library-read-failed takes on or off, not 'yes'")]
    public void LibraryReadFailed_WithoutOnOrOff_Fails(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Theory]
    [InlineData("press-prompt ok", (int)PromptButton.Ok)]
    [InlineData("press-prompt cancel", (int)PromptButton.Cancel)]
    [InlineData("press-prompt delete", (int)PromptButton.Delete)]
    [InlineData("press-prompt keep-blocks", (int)PromptButton.KeepBlocks)]
    public void PressPrompt_ReadsTheButton(string line, int button)
    {
        var step = Assert.IsType<PressPromptStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((PromptButton)button, step.Button);
    }

    [Theory]
    [InlineData("expect-prompt none", (int)ListPrompt.None)]
    [InlineData("expect-prompt new", (int)ListPrompt.New)]
    [InlineData("expect-prompt rename", (int)ListPrompt.Rename)]
    [InlineData("expect-prompt delete", (int)ListPrompt.Delete)]
    public void ExpectPrompt_ReadsThePrompt(string line, int prompt)
    {
        var step = Assert.IsType<ExpectPromptStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal((ListPrompt)prompt, step.Prompt);
    }

    [Theory]
    [InlineData("show-tab", "show-tab takes scripts or libraries")]
    [InlineData("show-tab blocks", "show-tab takes scripts or libraries, not 'blocks'")]
    [InlineData("press-prompt yes", "press-prompt takes ok, cancel, delete or keep-blocks, not 'yes'")]
    [InlineData("expect-prompt open", "expect-prompt takes none, new, rename or delete, not 'open'")]
    [InlineData("set-window-height 0", "set-window-height takes a whole number of pixels above 0, not '0'")]
    [InlineData("set-window-height tall", "set-window-height takes a whole number of pixels above 0, not 'tall'")]
    [InlineData("open-new now", "open-new takes nothing after it")]
    [InlineData("expect-footer-in-view now", "expect-footer-in-view takes nothing after it")]
    public void TheFooterSteps_WithAWrongArgument_Fail(string line, string error)
    {
        ScenarioParser.Parse("steps", line, out var actual);

        Assert.Equal($"line 1: {error}", actual);
    }

    [Fact]
    public void SetWindowHeight_ReadsThePixels()
    {
        var step = Assert.IsType<SetWindowHeightStep>(Assert.Single(ScenarioParser.Parse("steps", "set-window-height 300", out _).Steps));

        Assert.Equal(300, step.Height);
    }

    [Theory]
    [InlineData("place-block lamp", "lamp", null)]
    [InlineData("place-block lamp as lamp_1", "lamp", "lamp_1")]
    [InlineData("place-block simple_telemetry as speed-gauge", "simple_telemetry", "speed-gauge")]
    public void PlaceBlock_AFunction_ThenOptionallyAName(string line, string function, string name)
    {
        var step = Assert.IsType<PlaceBlockStep>(Assert.Single(ScenarioParser.Parse("steps", line, out _).Steps));

        Assert.Equal(function, step.Function);
        Assert.Equal(name, step.Name);
    }

    [Theory]
    [InlineData("place-block lamp as")]
    [InlineData("place-block lamp as a}b")]
    [InlineData("place-block lamp as two words")]
    public void PlaceBlock_TakesANameOfLettersDigitsDashesAndUnderscores(string line)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(scenario);
        Assert.Contains("takes a name of letters", error);
    }

    [Fact]
    public void TheStepList_HoldsExactlyTheStepsTheDocsList()
    {
        var documented = DocsTable("### Steps").Select(step => step.Split(' ')[0]);

        Assert.Equal(documented.OrderBy(n => n), StepList.Names.OrderBy(n => n));
    }

    [Fact]
    public void TheDocs_ListExactlyTheRunScriptsOptions()
    {
        var script = RepoFiles.Read("tools/ingame-test.sh");
        var options = Regex.Matches(script, @"^\s*((?:-[-\w]+)(?: \| -[-\w]+)*)\)", RegexOptions.Multiline)
            .SelectMany(match => match.Groups[1].Value.Split(" | "));
        var documented = DocsTable("### Options").SelectMany(option => option.Split(", ")).Select(option => option.Split(' ')[0]);

        Assert.Equal(documented.OrderBy(o => o), options.Where(o => o != "-*").OrderBy(o => o));
    }

    [Theory]
    [InlineData("smoke.zlstest")]
    [InlineData("test-mode.zlstest")]
    [InlineData("keys.zlstest")]
    [InlineData("libraries.zlstest")]
    [InlineData("list-footer.zlstest")]
    public void TheShippedScenarios_CanBeRead(string file)
    {
        var text = RepoFiles.Read($"Zeepkist.LogicScripting.InGameTests/Scenarios/{file}");

        var scenario = ScenarioParser.Parse(file, text, out var error);

        Assert.Null(error);
        Assert.NotEmpty(scenario.Steps);
    }

    /// <summary>
    /// The first column of the table under <paramref name="heading"/> in docs/ingame-tests.md, without its backticks.
    /// </summary>
    private static string[] DocsTable(string heading)
    {
        var docs = RepoFiles.Read("docs/ingame-tests.md").Replace("\r\n", "\n");
        var section = docs.Substring(docs.IndexOf(heading + "\n", StringComparison.Ordinal));
        return [.. section.Split('\n')
            .SkipWhile(line => !line.StartsWith('|'))
            .TakeWhile(line => line.StartsWith('|'))
            .Skip(2)
            .Select(line => line.Split('|')[1].Trim().Replace("`", string.Empty))];
    }
}
