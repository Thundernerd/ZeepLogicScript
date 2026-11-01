using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class OutputTests
{
    [Fact]
    public void DotAccess_OnAName_WiresFromThatOutput()
    {
        var result = ScriptCompiler.Compile("p = player_input()\npower_display(p.brake)\npower_display(p.horn)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(BlockManifest.PlayerInput, result.Blocks[0].Type);
        Assert.Equal(["0.1 -> 1.0", "0.6 -> 2.0"], result.Wires.Select(w => $"{w.From}.{w.FromConnector} -> {w.To}.{w.ToConnector}"));
    }

    [Fact]
    public void DotAccess_OnACall_WiresFromThatOutput()
    {
        var result = ScriptCompiler.Compile("power_display(demultiplexer(1, 2).ch3 & 1)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal("logic[xmo=0](demultiplexer(1, 2).ch3, 1)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Call_WithoutOutputs_StandsAlone()
    {
        var result = ScriptCompiler.Compile("death(1)", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal([BlockManifest.Generator, BlockManifest.Death], result.Blocks.Select(b => b.Type));
    }

    [Fact]
    public void Call_WithSeveralOutputs_StandsAlone()
    {
        var result = ScriptCompiler.Compile("player_input()", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Single(result.Blocks);
    }

    [Theory]
    [InlineData("p = player_input()\npower_display(p)", 2, 15, "'p' has several outputs. Pick one, like p.arms_up")]
    [InlineData("power_display(player_input())", 1, 15, "'player_input' has several outputs. Pick one, like player_input().arms_up")]
    [InlineData("p = player_input()\npower_display(p.nope)", 2, 17, "'player_input' has no output 'nope'. Use arms_up, brake, left, right, analog_left, analog_right, horn")]
    [InlineData("x = 1 & 1\npower_display(x.out)", 2, 17, "'x' has a single output. Use it without .out")]
    [InlineData("power_display(death(1))", 1, 15, "'death' has no output to use as a value")]
    [InlineData("x = death(1)", 1, 5, "'death' has no output to use as a value")]
    [InlineData("p = player_input()\np.brake", 2, 3, "A value on its own line does nothing. Give it a name or pass it to a call")]
    [InlineData("power_display(1.out)", 1, 16, "Only names and calls have outputs to pick with .")]
    public void Outputs_Errors(string text, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal((new Position(line, column), message), (error.Position, error.Message));
    }

    [Theory]
    [InlineData("t = simple_telemetry()\npower_display(t .speed)", 2, 17, "Write the dot right after 't', without a space")]
    [InlineData("t = simple_telemetry()\npower_display(t. speed)", 2, 16, "Write 'speed' right after the dot, without a space")]
    [InlineData("t = simple_telemetry()\npower_display(t . speed)", 2, 17, "Write the dot right after 't', without a space")]
    [InlineData("power_display(simple_telemetry() .speed)", 1, 34, "Write the dot right after ')', without a space")]
    [InlineData("power_display(simple_telemetry(). speed)", 1, 33, "Write 'speed' right after the dot, without a space")]
    [InlineData("t = simple_telemetry()\npower_display(t\n.speed)", 3, 1, "Write the dot right after 't', without a space")]
    [InlineData("t = simple_telemetry()\npower_display(t.\nspeed)", 2, 16, "Write 'speed' right after the dot, without a space")]
    [InlineData("t = simple_telemetry()\npower_display(t\t.speed)", 2, 17, "Write the dot right after 't', without a space")]
    [InlineData("t = simple_telemetry()\npower_display(t # speed\n.speed)", 3, 1, "Write the dot right after 't', without a space")]
    [InlineData("t = simple_telemetry()\npower_display((t) .speed)", 2, 19, "Write the dot right after ')', without a space")]
    [InlineData("function f(a) -> hi, lo\n    hi = a\n    lo = a\nend\npower_display(f(1) .hi)", 5, 20, "Write the dot right after ')', without a space")]
    public void DotAccess_WithASpace_IsAnErrorAtTheDot(string text, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal((new Position(line, column), message), (error.Position, error.Message));
    }

    [Theory]
    [InlineData("t = simple_telemetry()\npower_display(t.speed)")]
    [InlineData("power_display(simple_telemetry().speed)")]
    [InlineData("t = simple_telemetry()\npower_display((t).speed)")]
    [InlineData("function f(a) -> hi, lo\n    hi = a\n    lo = a\nend\npower_display(f(1).hi)")]
    public void DotAccess_TouchingBothSides_Compiles(string text)
    {
        var result = ScriptCompiler.Compile(text, 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Fact]
    public void DecimalNumber_KeepsItsOwnError()
    {
        var result = ScriptCompiler.Compile("power_display(0.5)", 0);

        Assert.Equal(["1:15: Only whole numbers are allowed"], result.Errors.Select(e => e.ToString()));
    }
}
