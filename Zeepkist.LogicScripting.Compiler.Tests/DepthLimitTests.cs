using System;
using System.Linq;
using System.Threading;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class DepthLimitTests
{
    // Inside power_display(...), whose call and argument take the first two levels
    private const int Deepest = Parser.MaxDepth - 2;

    // Smaller than any thread the game compiles on
    private const int StackSize = 256 * 1024;

    [Theory]
    [MemberData(nameof(Shapes))]
    public void AtTheLimit_Compiles_OnASmallStack(string shape)
    {
        var result = CompileOnSmallStack(Nest(shape, Deepest));

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void PastTheLimit_IsAnError(string shape)
    {
        var result = CompileOnSmallStack(Nest(shape, Deepest + 1) + "\npower_display(nope)");

        Assert.Equal(2, result.Errors.Count);
        Assert.EndsWith($"This nests more than {Parser.MaxDepth} deep; split it up into named parts", result.Errors[0].ToString());
        Assert.Equal(2, result.Errors[1].Position.Line);
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void FarPastTheLimit_IsAnError(string shape)
    {
        var result = CompileOnSmallStack(Nest(shape, 100_000));

        Assert.Contains("nests more than", Assert.Single(result.Errors).Message);
    }

    public static TheoryData<string> Shapes()
    {
        return ["parentheses", "calls", "unary", "chain", "right"];
    }

    private static string Nest(string shape, int depth)
    {
        var inner = shape switch
        {
            "parentheses" => Repeat("(", depth) + "1" + Repeat(")", depth),
            "calls" => Repeat("passthrough(", depth) + "1" + Repeat(")", depth),
            "unary" => Repeat("!", depth) + "1",
            "chain" => "1" + Repeat(" & 1", depth),
            "right" => Repeat("1 & (", depth) + "1" + Repeat(")", depth),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        return $"power_display({inner})";
    }

    private static string Repeat(string text, int count)
    {
        return string.Concat(Enumerable.Repeat(text, count));
    }

    private static CompileResult CompileOnSmallStack(string text)
    {
        CompileResult result = null;
        Thread thread = new(() => result = ScriptCompiler.Compile(text, 0), StackSize);
        thread.Start();
        thread.Join();
        return result;
    }
}
