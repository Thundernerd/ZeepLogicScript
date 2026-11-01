using System;
using System.Linq;
using System.Text.RegularExpressions;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class StepListTests
{
    [Theory]
    [InlineData("open-window", typeof(OpenWindowStep))]
    [InlineData("close-window", typeof(CloseWindowStep))]
    [InlineData("new-script Lights", typeof(NewScriptStep))]
    [InlineData("select-script Lights", typeof(SelectScriptStep))]
    [InlineData("set-text\na = 1\nend", typeof(SetTextStep))]
    [InlineData("compile", typeof(CompileStep))]
    [InlineData("expect-result Placed", typeof(ExpectResultStep))]
    [InlineData("expect-no-errors", typeof(ExpectNoErrorsStep))]
    [InlineData("expect-errors 2", typeof(ExpectErrorsStep))]
    [InlineData("expect-blocks 3", typeof(ExpectBlocksStep))]
    [InlineData("undo", typeof(UndoStep))]
    [InlineData("redo", typeof(RedoStep))]
    [InlineData("place-start", typeof(PlaceStartStep))]
    [InlineData("test-mode", typeof(TestModeStep))]
    [InlineData("back-to-editor", typeof(BackToEditorStep))]
    [InlineData("wait 1", typeof(WaitStep))]
    [InlineData("screenshot compiled", typeof(ScreenshotStep))]
    [InlineData("log hello", typeof(LogStep))]
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
