using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptCompilerTests
{
    [Fact]
    public void PowerDisplayOfOneAndOne_SharesOneGeneratorBetweenBothLogicInputs()
    {
        var result = ScriptCompiler.Compile("power_display(1 & 1)", 0);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [BlockManifest.Generator, BlockManifest.Logic, BlockManifest.PowerDisplay],
            result.Blocks.Select(b => b.Type));

        var generator = Assert.Single(result.Blocks[0].Settings);
        Assert.Equal(BlockManifest.GeneratorPowerKey, generator.Key);
        Assert.Equal(SettingKind.Text, generator.Kind);
        Assert.Equal("1", generator.TextValue);

        var logic = Assert.Single(result.Blocks[1].Settings);
        Assert.Equal(BlockManifest.LogicOperatorKey, logic.Key);
        Assert.Equal(BlockManifest.LogicOperatorAnd, logic.IntValue);

        Assert.Equal(
            ["0.0 -> 1.0", "0.0 -> 1.1", "1.3 -> 2.0"],
            result.Wires.Select(w => $"{w.From}.{w.FromConnector} -> {w.To}.{w.ToConnector}"));
    }

    [Fact]
    public void DistinctLiterals_GetOneGeneratorEach()
    {
        var result = ScriptCompiler.Compile("a = 1 & 2\nb = 2 & 1\npower_display(a & b)", 0);

        Assert.True(result.Succeeded);
        var powers = result.Blocks
            .Where(b => b.Type == BlockManifest.Generator)
            .Select(b => b.Settings[0].TextValue);
        Assert.Equal(["1", "2"], powers);
    }

    [Fact]
    public void Names_ReferToTheBlockTheyWereDefinedAs()
    {
        var result = ScriptCompiler.Compile("x = 1 & 1\n\npower_display(x)\npower_display(x)", 0);

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.Blocks.Count);
        Assert.Equal(2, result.Wires.Count(w => w.From == 1 && w.FromConnector == 3));
    }

    [Fact]
    public void CallArguments_WireIntoInputsInConnectorOrder()
    {
        var result = ScriptCompiler.Compile("power_display(logic(1, 0))", 0);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["0.0 -> 2.0", "1.0 -> 2.1", "2.3 -> 3.0"],
            result.Wires.Select(w => $"{w.From}.{w.FromConnector} -> {w.To}.{w.ToConnector}"));
    }

    [Fact]
    public void Parentheses_Group()
    {
        var result = ScriptCompiler.Compile("power_display((1 & (1)))", 0);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Blocks.Count);
    }

    [Fact]
    public void EmptyScript_HasNoBlocks()
    {
        var result = ScriptCompiler.Compile("\n\n", 0);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Blocks);
    }

    [Theory]
    [InlineData("power_display(y)", 1, 15, "'y' isn't defined")]
    [InlineData("x = 1\nx = 1", 2, 1, "'x' is already defined")]
    [InlineData("logic = 1", 1, 1, "'logic' is a Block Function and can't be used as a name")]
    [InlineData("x = power_display", 1, 5, "'power_display' is a Block Function; call it with ( )")]
    [InlineData("lamp(1)", 1, 1, "'lamp' isn't a Block Function")]
    [InlineData("power_display(1, 1)", 1, 1, "'power_display' takes at most 1 inputs, got 2")]
    [InlineData("x = 1 &", 1, 8, "Expected a value, found the end of the Script")]
    [InlineData("power_display(1", 1, 14, "'(' isn't closed")]
    [InlineData("power_display(1 2)", 1, 17, "Expected ',' or ')', found '2'")]
    [InlineData("x = 1 1", 1, 7, "Expected the end of the line, found '1'")]
    [InlineData("x = 1$", 1, 6, "Unexpected character '$'")]
    [InlineData("x = 99999999999999999999", 1, 5, "99999999999999999999 doesn't fit in a Signal")]
    [InlineData("1 & 1", 1, 3, "A value on its own line does nothing; give it a name or pass it to a call")]
    public void Errors_HaveLineAndColumn(string text, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile(text, 0);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Blocks);
        var error = Assert.Single(result.Errors);
        Assert.Equal((line, column, message), (error.Line, error.Column, error.Message));
    }

    [Fact]
    public void Errors_FromEveryStageAreCollectedTogether()
    {
        var result = ScriptCompiler.Compile("a = 1 $ 2\nb = logic(1, 2, op: maybe)\npower_display(c\n", 0);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Blocks);
        Assert.Equal(
            [
                "1:7: Unexpected character '$'",
                "1:9: Expected the end of the line, found '2'",
                "2:21: 'maybe' isn't an option of op; use and, or, nand, nor, xor, xnor",
                "3:14: '(' isn't closed"
            ],
            result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void Errors_AreAllCollectedInOrder()
    {
        var result = ScriptCompiler.Compile("power_display(b)\nx = (\npower_display(a)", 0);

        Assert.Equal(["1:15", "2:5", "3:15"], result.Errors.Select(e => $"{e.Line}:{e.Column}"));
    }
}
