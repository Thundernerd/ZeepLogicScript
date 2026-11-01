using System;
using System.Linq;
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
    [InlineData("wait 1", typeof(WaitStep))]
    [InlineData("screenshot compiled", typeof(ScreenshotStep))]
    [InlineData("log hello", typeof(LogStep))]
    public void EachStep_ParsesToItsOwnClass(string line, Type type)
    {
        var scenario = ScenarioParser.Parse("steps", line, out var error);

        Assert.Null(error);
        Assert.IsType(type, Assert.Single(scenario.Steps));
    }

    [Fact]
    public void TheStepList_HoldsExactlyTheDocumentedSteps()
    {
        string[] documented =
        [
            "open-window", "close-window", "new-script", "select-script", "set-text", "compile",
            "expect-result", "expect-no-errors", "expect-errors", "expect-blocks",
            "undo", "redo", "wait", "screenshot", "log",
        ];

        Assert.Equal(documented.OrderBy(n => n), StepList.Names.OrderBy(n => n));
    }
}
