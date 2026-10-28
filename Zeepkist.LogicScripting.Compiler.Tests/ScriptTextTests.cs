using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptTextTests
{
    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 3, 2)]
    [InlineData(2, 1, 4)]
    [InlineData(2, 2, 5)]
    [InlineData(2, 99, 6)]
    [InlineData(1, 99, 3)]
    [InlineData(99, 1, 6)]
    public void Index_IsTheCharacterAtTheLineAndColumn_KeptOnItsLine(int line, int column, int expected)
    {
        Assert.Equal(expected, ScriptText.GetIndex("abc\nde", new Position(line, column)));
    }

    [Theory]
    [InlineData(1, 5, 4, 5)] // the name "logic"
    [InlineData(1, 11, 10, 3)] // the text "\"U\"" with its quotes
    [InlineData(1, 4, 3, 1)] // a space: one character
    [InlineData(1, 15, 14, 0)] // the end of the line: nothing to underline
    public void ErrorSpan_CoversTheTokenAtTheError(int line, int column, int start, int length)
    {
        var spans = ScriptText.GetErrorSpans("x = logic(\"U\")\n", [new Position(line, column)]);

        Assert.Equal([(start, length)], spans.Select(s => (s.Start, s.Length)));
    }

    [Fact]
    public void ErrorSpans_KeepTheErrorsOrder()
    {
        var spans = ScriptText.GetErrorSpans("ab = cd\nef", [new Position(2, 1), new Position(1, 6)]);

        Assert.Equal([(8, 2), (5, 2)], spans.Select(s => (s.Start, s.Length)));
    }
}
