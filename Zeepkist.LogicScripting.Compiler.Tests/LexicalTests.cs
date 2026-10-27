using System.Linq;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class LexicalTests
{
    [Fact]
    public void Comments_RunToTheEndOfTheLine()
    {
        var result = ScriptCompiler.Compile("# a whole line\nx = 1 & 2 # after a statement\npower_display(x) #", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal("logic[xmo=0](1, 2)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Statements_ContinueOnTheNextLineInsideBrackets()
    {
        var result = ScriptCompiler.Compile("x = logic(\n    1, # first\n    2,\n    op: or\n)\npower_display((x\n& 3))", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal("logic[xmo=0](logic[xmo=1](1, 2), 3)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void ErrorsOnContinuedLines_KeepTheirOwnLine()
    {
        var result = ScriptCompiler.Compile("x = logic(\n  1,\n  nope)\ny = nope2", 0);

        Assert.Equal(["3:3", "4:5"], result.Errors.Select(e => $"{e.Position}"));
    }

    [Fact]
    public void UnclosedBracket_IsReportedAtTheBracket_AndLaterLinesAreStillChecked()
    {
        var result = ScriptCompiler.Compile("x = (1 & 2\npower_display(nope)", 0);

        Assert.Equal(
            ["1:5 '(' isn't closed", "2:15 'nope' isn't defined"],
            result.Errors.Select(e => $"{e.Position} {e.Message}"));
    }

    [Fact]
    public void UnclosedBracket_IsNotClosedByAStrayBracketAfterALaterDefinition()
    {
        var result = ScriptCompiler.Compile("x = logic(1\ny = nope\nz = logic(1, 2))", 0);

        Assert.Equal(
            ["1:10: '(' isn't closed", "2:5: 'nope' isn't defined", "3:16: Expected the end of the line, found ')'"],
            result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void NewLineOutsideBrackets_EndsTheStatement()
    {
        var result = ScriptCompiler.Compile("x = 1 &\n2", 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "1:8: Expected a value, found the end of the line; to continue on the next line, put the expression in ( )",
            error.ToString());
    }

    [Fact]
    public void DanglingOperator_StillChecksTheLinesAfterTheContinuation()
    {
        var result = ScriptCompiler.Compile("x = 1 &\n2\ny = nope", 0);

        Assert.Equal(["1:8", "3:5"], result.Errors.Select(e => $"{e.Position}"));
    }

    [Theory]
    [InlineData("x = 1.5", 1, 5, "Only whole numbers are allowed")]
    [InlineData("x = 1)", 1, 6, "Expected the end of the line, found ')'")]
    public void Lexical_Errors(string text, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal((line, column, message), (error.Position.Line, error.Position.Column, error.Message));
    }
}
