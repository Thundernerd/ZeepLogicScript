using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class DefinitionTests
{
    [Fact]
    public void Delay_CanReferToTheNameBeingDefined()
    {
        var result = ScriptCompiler.Compile("press = 1\nheld = delay(held ^ press, ticks: 1)\npower_display(held)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(
            [BlockManifest.Generator, BlockManifest.Logic, BlockManifest.Delay, BlockManifest.PowerDisplay],
            result.Blocks.Select(b => b.Type));

        // The Delay feeds its own XOR, which feeds the Delay
        Assert.Contains(result.Wires, w => (w.From, w.FromConnector, w.To, w.ToConnector) == (2, 1, 1, 0));
        Assert.Contains(result.Wires, w => (w.From, w.FromConnector, w.To, w.ToConnector) == (1, 3, 2, 0));
        Assert.Equal("ldl", Assert.Single(result.Blocks[2].Settings).Key);
    }

    [Fact]
    public void Delay_CanReferToALaterDefinition()
    {
        var result = ScriptCompiler.Compile("a = delay(b, ticks: 2)\nb = a | 1\np = player_input()\nc = delay(p.horn, ticks: 1)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Contains(result.Wires, w => (w.From, w.To, w.ToConnector) == (2, 0, 0));
    }

    [Fact]
    public void Delay_ForwardReferenceToAMultiOutputBlock_UsesDotAccess()
    {
        var result = ScriptCompiler.Compile("c = delay(p.horn, ticks: 1)\np = player_input()", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        var wire = Assert.Single(result.Wires);
        Assert.Equal((1, 6, 0, 0), (wire.From, wire.FromConnector, wire.To, wire.ToConnector));
    }

    [Fact]
    public void FailedDefinition_DoesNotReportItsUsesAgain()
    {
        var result = ScriptCompiler.Compile("x = nope & 1\npower_display(x)\ny = delay(x, ticks: 1)", 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal("'nope' isn't defined", error.Message);
    }

    [Fact]
    public void NamesAreCaseSensitive()
    {
        var result = ScriptCompiler.Compile("Logic = 1\nlogic_out = Logic & 1\npower_display(logic_out)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Theory]
    [InlineData("x = y & 1\ny = 1", 1, 5, "'y' is defined later, on line 2; only delay(...) can refer ahead")]
    [InlineData("x = x & 1", 1, 5, "'x' can't refer to itself outside delay(...)")]
    [InlineData("x = delay(x, ticks: 1) & x", 1, 26, "'x' can't refer to itself outside delay(...)")]
    [InlineData("x = delay(y, ticks: 1)", 1, 11, "'y' isn't defined")]
    [InlineData("x = delay(1)", 1, 5, "'delay' needs ticks:, like ticks: 1")]
    [InlineData("p = player_input()\nx = delay(p, ticks: 1)", 2, 11, "'p' has several outputs; pick one, like p.arms_up")]
    [InlineData("x = delay(p, ticks: 1)\np = player_input()", 1, 11, "'p' has several outputs; pick one, like p.arms_up")]
    [InlineData("x = 1\nx = delay(x, ticks: 1)", 2, 1, "'x' is already defined")]
    [InlineData("x = delay(nope(later), ticks: 1)\nlater = 1", 1, 11, "'nope' isn't a Block Function")]
    public void Definitions_Errors(string text, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal((line, column, message), (error.Line, error.Column, error.Message));
    }
}
