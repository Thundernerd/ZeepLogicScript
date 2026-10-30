using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class DepthLimitTests
{
    // Inside power_display(...), whose call and argument take the first two levels
    private const int Deepest = Parser.MaxDepth - 2;

    [Theory]
    [MemberData(nameof(Shapes))]
    public void AtTheLimit_Compiles_OnASmallStack(string open, string close)
    {
        var result = SmallStack.Run(() => ScriptCompiler.Compile(Nest(open, close, Deepest), 0));

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void PastTheLimit_IsAnError(string open, string close)
    {
        var text = Nest(open, close, Deepest + 1) + "\npower_display(nope)";
        var result = SmallStack.Run(() => ScriptCompiler.Compile(text, 0));

        Assert.Equal(2, result.Errors.Count);
        Assert.EndsWith($"This nests more than {Parser.MaxDepth} deep. Split it into definitions", result.Errors[0].ToString());
        Assert.Equal(2, result.Errors[1].Position.Line);
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void FarPastTheLimit_IsAnError(string open, string close)
    {
        var result = SmallStack.Run(() => ScriptCompiler.Compile(Nest(open, close, 100_000), 0));

        Assert.Contains("nests more than", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void FunctionsAtTheLimit_Compile_OnASmallStack()
    {
        var result = SmallStack.Run(() => ScriptCompiler.Compile(NestFunctions(string.Empty, string.Empty, 0, GraphBuilder.MaxFunctionDepth), 0));

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Fact]
    public void FunctionsPastTheLimit_IsAnError()
    {
        var result = ScriptCompiler.Compile(NestFunctions(string.Empty, string.Empty, 0, GraphBuilder.MaxFunctionDepth + 1), 0);

        Assert.Contains($"Functions call each other more than {GraphBuilder.MaxFunctionDepth} deep", Assert.Single(result.Errors).Message);
    }

    /// <summary>
    /// A body is visited inside the expression that calls it, so the nesting adds up across the calls; at every level
    /// it either compiles or is an error, and never runs out of stack.
    /// </summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void FunctionsNestingInsideEachOther_AreCappedTogether_OnASmallStack(string open, string close)
    {
        for (var levels = 0; levels <= Deepest; levels++)
        {
            var text = NestFunctions(open, close, levels, GraphBuilder.MaxFunctionDepth);
            var result = SmallStack.Run(() => ScriptCompiler.Compile(text, 0));

            Assert.True(result.Succeeded || result.Errors.All(e => e.Message.Contains("counting the functions")), string.Join("\n", result.Errors));
            // Parentheses only group, so they don't add up
            if (levels == Deepest && open != "(")
                Assert.False(result.Succeeded);
        }
    }

    /// <summary>
    /// What each level adds before and after a 1: parentheses, calls, unary operators, an operator chain that nests
    /// to the left, and one that nests to the right.
    /// </summary>
    public static TheoryData<string, string> Shapes()
    {
        return new TheoryData<string, string>
        {
            { "(", ")" },
            { "passthrough(", ")" },
            { "!", string.Empty },
            { string.Empty, " & 1" },
            { "1 & (", ")" }
        };
    }

    private static string Nest(string open, string close, int depth)
    {
        return $"power_display({Repeat(open, depth)}1{Repeat(close, depth)})";
    }

    /// <summary>
    /// A chain of Functions, each calling the next <paramref name="levels"/> deep inside an expression.
    /// </summary>
    private static string NestFunctions(string open, string close, int levels, int depth)
    {
        var text = string.Concat(Enumerable.Range(1, depth - 1)
            .Select(i => $"function f{i}(x) -> y\n    y = {Repeat(open, levels)}f{i + 1}(x){Repeat(close, levels)}\nend\n"));
        return text + $"function f{depth}(x) -> y\n    y = x\nend\npower_display(f1(1))";
    }

    private static string Repeat(string text, int count)
    {
        return string.Concat(Enumerable.Repeat(text, count));
    }
}
