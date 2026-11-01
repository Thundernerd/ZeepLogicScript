using System.Linq;
using TNRD.Zeepkist.LogicScripting.InGameTests;
using TNRD.Zeepkist.LogicScripting.InGameTests.Steps;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Tests;

public class ScenarioParserTests
{
    [Fact]
    public void Steps_KeepTheirLineAndText()
    {
        var scenario = Parse("# a comment\n\nopen-window\n  compile  \nexpect-blocks 2\n");

        Assert.Equal("smoke", scenario.Name);
        Assert.Equal([3, 4, 5], scenario.Steps.Select(s => s.Line));
        Assert.Equal(["open-window", "compile", "expect-blocks 2"], scenario.Steps.Select(s => s.Source));
        Assert.IsType<ExpectBlocksStep>(scenario.Steps[2]);
        Assert.Equal(2, ((ExpectBlocksStep)scenario.Steps[2]).Count);
    }

    [Fact]
    public void SetText_TakesTheLinesUpToEnd()
    {
        var scenario = Parse("set-text\npower_display(1 & 1)\n\n# not a step comment\nend\ncompile");

        var setText = Assert.IsType<SetTextStep>(scenario.Steps[0]);
        Assert.Equal("power_display(1 & 1)\n\n# not a step comment", setText.Text);
        Assert.Equal(1, setText.Line);
        Assert.Equal(6, scenario.Steps[1].Line);
    }

    [Fact]
    public void SetText_Indented_TakesTheIndentationOffAndKeepsAFunctionsEnd()
    {
        var scenario = Parse("set-text\n    function f(x) -> y\n        y = x\n    end\n\n    f(1)\nend");

        var setText = Assert.IsType<SetTextStep>(scenario.Steps[0]);
        Assert.Equal("function f(x) -> y\n    y = x\nend\n\nf(1)", setText.Text);
        Assert.Single(scenario.Steps);
    }

    [Fact]
    public void SetText_WindowsLineEnds_AreReadAsNewlines()
    {
        var scenario = Parse("set-text\r\na = 1\r\nb = 2\r\nend\r\ncompile\r\n");

        Assert.Equal("a = 1\nb = 2", ((SetTextStep)scenario.Steps[0]).Text);
        Assert.Equal("compile", scenario.Steps[1].Source);
    }

    [Fact]
    public void SetText_WithoutEnd_FailsAtItsLine()
    {
        Assert.Equal("line 2: set-text has no end", ParseError("open-window\nset-text\na = 1"));
    }

    [Fact]
    public void EndWithoutSetText_Fails()
    {
        Assert.Equal("line 2: end without set-text", ParseError("open-window\nend"));
    }

    [Fact]
    public void AnUnknownStep_FailsAtItsLine()
    {
        Assert.Equal("line 3: unknown step 'compiel'", ParseError("open-window\n\ncompiel"));
    }

    [Theory]
    [InlineData("open-window now", "line 1: open-window takes nothing after it")]
    [InlineData("expect-blocks two", "line 1: expect-blocks takes a whole number of 0 or more, not 'two'")]
    [InlineData("expect-blocks -1", "line 1: expect-blocks takes a whole number of 0 or more, not '-1'")]
    [InlineData("expect-blocks", "line 1: expect-blocks takes a whole number of 0 or more")]
    [InlineData("new-script", "line 1: new-script takes a name")]
    [InlineData("expect-result", "line 1: expect-result takes the text to look for")]
    [InlineData("wait", "line 1: wait takes a number of seconds above 0")]
    [InlineData("wait 0", "line 1: wait takes a number of seconds above 0, not '0'")]
    [InlineData("wait soon", "line 1: wait takes a number of seconds above 0, not 'soon'")]
    [InlineData("screenshot a/b", "line 1: screenshot takes a name of letters, digits, '-' and '_', not 'a/b'")]
    public void AMalformedLine_FailsAtItsLine(string line, string error)
    {
        Assert.Equal(error, ParseError(line));
    }

    [Fact]
    public void AScenarioWithoutSteps_Fails()
    {
        Assert.Equal("no steps", ParseError("# only a comment\n"));
    }

    [Fact]
    public void Wait_ReadsDecimalsWithAPoint()
    {
        var wait = Assert.IsType<WaitStep>(Parse("wait 0.5").Steps[0]);

        Assert.Equal(0.5f, wait.Seconds);
    }

    [Fact]
    public void ArgumentText_IsTheRestOfTheLine()
    {
        var scenario = Parse("new-script  Lamps and doors \nexpect-result Placed 3 blocks\nlog  hello there");

        Assert.Equal("Lamps and doors", ((NewScriptStep)scenario.Steps[0]).Name);
        Assert.Equal("Placed 3 blocks", ((ExpectResultStep)scenario.Steps[1]).Text);
        Assert.Equal("hello there", ((LogStep)scenario.Steps[2]).Text);
    }

    private static Scenario Parse(string text)
    {
        var scenario = ScenarioParser.Parse("smoke", text, out var error);
        Assert.Null(error);
        return scenario;
    }

    private static string ParseError(string text)
    {
        var scenario = ScenarioParser.Parse("smoke", text, out var error);
        Assert.Null(scenario);
        return error;
    }
}
