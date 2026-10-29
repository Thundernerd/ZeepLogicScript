using System.Linq;
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
        Assert.EndsWith($"This nests more than {Parser.MaxDepth} deep; split it into definitions", result.Errors[0].ToString());
        Assert.Equal(2, result.Errors[1].Position.Line);
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void FarPastTheLimit_IsAnError(string open, string close)
    {
        var result = SmallStack.Run(() => ScriptCompiler.Compile(Nest(open, close, 100_000), 0));

        Assert.Contains("nests more than", Assert.Single(result.Errors).Message);
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

    private static string Repeat(string text, int count)
    {
        return string.Concat(Enumerable.Repeat(text, count));
    }
}
