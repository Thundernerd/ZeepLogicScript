using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class CallTests
{
    [Theory]
    [InlineData("logic(1, 2, op: nor)", "logic[xmo=3](1, 2)")]
    [InlineData("math(1, 2, op: divide_scaled)", "math[xmo=27](1, 2)")]
    [InlineData("generator(power: 7)", "7")]
    [InlineData("generator(power: -7)", "-7")]
    [InlineData("digitizer(1, invert: true)", "digitizer[xt1=1](1)")]
    [InlineData("digitizer(1, invert: false)", "digitizer[xt1=0](1)")]
    public void NamedArguments_SetSettings(string call, string tree)
    {
        var result = ScriptCompiler.Compile($"power_display({call})", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(tree, BlockTree.RenderLast(result));
    }

    [Fact]
    public void OmittedSettings_AreLeftOut()
    {
        var result = ScriptCompiler.Compile("power_display(logic(1, 2))", 0);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Blocks[2].Settings);
    }

    [Theory]
    [InlineData("logic(b: 2, a: 1)", "logic(1, 2)")]
    [InlineData("logic(1, op_in: 2)", "logic(1, _, 2)")]
    [InlineData("logic(1, op: or, b: 2)", "logic[xmo=1](1, 2)")]
    public void NamedArguments_WireInputsByName(string call, string tree)
    {
        var result = ScriptCompiler.Compile($"power_display({call})", 0);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(tree, BlockTree.RenderLast(result));
    }

    [Theory]
    [InlineData("logic(1, 2, op: maybe)", 1, 31, "'maybe' isn't an option of op; use and, or, nand, nor, xor, xnor")]
    [InlineData("logic(1, 2, colour: 3)", 1, 27, "'logic' has no input or Setting 'colour'")]
    [InlineData("logic(1, 2, op: or, op: and)", 1, 35, "'op' is given twice")]
    [InlineData("logic(1, a: 2)", 1, 24, "Input 'a' is already wired by position")]
    [InlineData("logic(a: 1, 2)", 1, 27, "Positional inputs go before named arguments")]
    [InlineData("generator(power: 1 + 1)", 1, 32, "power takes a whole number, like power: 5")]
    [InlineData("digitizer(1, invert: 1)", 1, 36, "invert takes true or false")]
    [InlineData("logic(1, 2, op: 1)", 1, 31, "op takes a word: and, or, nand, nor, xor, xnor")]
    public void NamedArguments_Errors(string call, int line, int column, string message)
    {
        var result = ScriptCompiler.Compile($"power_display({call})", 0);

        var error = Assert.Single(result.Errors);
        Assert.Equal((line, column, message), (error.Line, error.Column, error.Message));
    }
}
