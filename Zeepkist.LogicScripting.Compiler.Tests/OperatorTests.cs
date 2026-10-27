using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class OperatorTests
{
    [Theory]
    [InlineData("&", 0)]
    [InlineData("|", 1)]
    [InlineData("^", 4)]
    public void LogicOperators_PlaceALogicBlockWithTheirOperator(string op, int stored)
    {
        var result = ScriptCompiler.Compile($"power_display(1 {op} 2)", 0);

        Assert.True(result.Succeeded);
        var logic = result.Blocks[2];
        Assert.Equal(BlockManifest.Logic, logic.Type);
        Assert.Equal(["xmo=" + stored], logic.Settings.Select(Describe));
        Assert.Equal(["0.0 -> 2.0", "1.0 -> 2.1", "2.3 -> 3.0"], result.Wires.Select(Describe));
    }

    [Theory]
    [InlineData("+", 0)]
    [InlineData("-", 1)]
    [InlineData("*", 2)]
    [InlineData("/", 3)]
    [InlineData("%", 4)]
    [InlineData("<", 5)]
    [InlineData(">", 6)]
    [InlineData("<=", 7)]
    [InlineData(">=", 8)]
    [InlineData("==", 9)]
    [InlineData("!=", 10)]
    public void MathOperators_PlaceAMathBlockWithTheirOperator(string op, int stored)
    {
        var result = ScriptCompiler.Compile($"power_display(1 {op} 2)", 0);

        Assert.True(result.Succeeded);
        var math = result.Blocks[2];
        Assert.Equal(BlockManifest.Math, math.Type);
        Assert.Equal(["xmo=" + stored], math.Settings.Select(Describe));
        Assert.Equal(["0.0 -> 2.0", "1.0 -> 2.1", "2.3 -> 3.0"], result.Wires.Select(Describe));
    }

    [Theory]
    [InlineData("1 + 2 * 3", "math[xmo=0](1, math[xmo=2](2, 3))")]
    [InlineData("1 * 2 + 3", "math[xmo=0](math[xmo=2](1, 2), 3)")]
    [InlineData("1 - 2 - 3", "math[xmo=1](math[xmo=1](1, 2), 3)")]
    [InlineData("(1 - 2) * 3", "math[xmo=2](math[xmo=1](1, 2), 3)")]
    [InlineData("1 < 2 == 3", "math[xmo=9](math[xmo=5](1, 2), 3)")]
    [InlineData("1 == 2 & 3", "logic[xmo=0](math[xmo=9](1, 2), 3)")]
    [InlineData("1 | 2 ^ 3 & 4", "logic[xmo=1](1, logic[xmo=4](2, logic[xmo=0](3, 4)))")]
    [InlineData("1 + 2 > 3 - 4", "math[xmo=6](math[xmo=0](1, 2), math[xmo=1](3, 4))")]
    public void Operators_FollowCPrecedenceWithOneBlockEach(string expression, string tree)
    {
        var result = ScriptCompiler.Compile($"power_display({expression})", 0);

        Assert.True(result.Succeeded);
        Assert.Equal(tree, BlockTree.RenderLast(result));
    }

    [Fact]
    public void Not_PlacesAnInvertingDigitizer()
    {
        var result = ScriptCompiler.Compile("power_display(!(1 & 2))", 0);

        Assert.True(result.Succeeded);
        Assert.Equal("digitizer[xt1=1](logic[xmo=0](1, 2))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Negation_IsZeroMinusTheValue()
    {
        var result = ScriptCompiler.Compile("x = 1 & 2\npower_display(-x)", 0);

        Assert.True(result.Succeeded);
        Assert.Equal("math[xmo=1](0, logic[xmo=0](1, 2))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void NegativeLiteral_IsOneGenerator()
    {
        var result = ScriptCompiler.Compile("power_display(-5 + 1)", 0);

        Assert.True(result.Succeeded);
        Assert.Equal("math[xmo=0](-5, 1)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void UnaryOperators_BindTighterThanBinaryOnes()
    {
        var result = ScriptCompiler.Compile("x = 1\npower_display(!x & -x * 2)", 0);

        Assert.True(result.Succeeded);
        Assert.Equal("logic[xmo=0](digitizer[xt1=1](1), math[xmo=2](math[xmo=1](0, 1), 2))", BlockTree.RenderLast(result));
    }

    private static string Describe(Setting setting)
    {
        return setting.Kind == SettingKind.Int ? $"{setting.Key}={setting.IntValue}" : $"{setting.Key}=\"{setting.TextValue}\"";
    }

    private static string Describe(CompiledWire wire)
    {
        return $"{wire.From}.{wire.FromConnector} -> {wire.To}.{wire.ToConnector}";
    }
}
