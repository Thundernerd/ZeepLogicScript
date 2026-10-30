using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the caret with <c>|</c> and the selection's other end with <c>^</c>.
/// </summary>
public class EditCommandsTests
{
    [Theory]
    [InlineData("a\n    b|c", "a\n    |bc")]
    [InlineData("a\n    |bc", "a\n|    bc")]
    [InlineData("a\n|    bc", "a\n    |bc")]
    [InlineData("a\n  | bc", "a\n   |bc")]
    public void Home_GoesToTheFirstNonSpace_ThenToTheLineStart(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.Home(s, false)));
    }

    [Fact]
    public void ShiftHome_SelectsFromWhereTheSelectionStarted()
    {
        Assert.Equal("a\n  |bc^d", Run("a\n  bc^d|", s => EditCommands.Home(s, true)));
    }

    [Theory]
    [InlineData("a|b\ncd", false, "ab|\ncd")]
    [InlineData("ab\nc|d", false, "ab\ncd|")]
    [InlineData("a|b\ncd", true, "a^b|\ncd")]
    [InlineData("a^b|c\nd", false, "abc|\nd")]
    public void End_GoesToTheLineEnd(string text, bool select, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.End(s, select)));
    }

    [Theory]
    [InlineData("ab\nc|d", 0, false, "|ab\ncd")]
    [InlineData("ab\nc|d", 5, false, "ab\ncd|")]
    [InlineData("ab\nc|d", 0, true, "|ab\nc^d")]
    [InlineData("a^b|\ncd", 5, true, "a^b\ncd|")]
    public void MoveTo_GoesToTheScriptStartOrEnd(string text, int index, bool select, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.MoveTo(s, index, select)));
    }

    [Theory]
    [InlineData("foo bar|", 4, "foo |")]
    [InlineData("|foo bar", 3, "| bar")]
    [InlineData("f^oo b|ar", 0, "f|ar")]
    public void DeleteTo_DeletesToTheBoundary_OrOnlyTheSelection(string text, int boundary, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.DeleteTo(s, boundary)));
    }

    [Theory]
    [InlineData("a\nb|b\nc", -1, "b|b\na\nc")]
    [InlineData("a\nb|b\nc", 1, "a\nc\nb|b")]
    [InlineData("a|\nb", -1, "a|\nb")]
    [InlineData("a\nb|", 1, "a\nb|")]
    [InlineData("x\n^a\nb|\nc", -1, "^a\nb|\nx\nc")]
    [InlineData("^a\nb\n|c", 1, "c\n^a\nb|")]
    public void MoveLines_SwapsTheSelectedLinesWithTheNextOne(string text, int direction, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.MoveLines(s, direction)));
    }

    [Theory]
    [InlineData("a\nb|c\nd", "a\nbc\nb|c\nd")]
    [InlineData("a\nb|c", "a\nbc\nb|c")]
    [InlineData("a^bc|d", "abc^bc|d")]
    [InlineData("a|bc^d", "abc|bc^d")]
    public void Duplicate_CopiesTheLineBelow_OrTheSelectionAfterItself(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.Duplicate(s)));
    }

    [Theory]
    [InlineData("ab\ncd|e\nf", "ab\nf|")]
    [InlineData("ab\nc|d", "a|b")]
    [InlineData("a|b", "|")]
    [InlineData("^ab\ncd\n|ef", "|ef")]
    [InlineData("abc\nd^e\nf|g\nhi", "abc\nh|i")]
    public void DeleteLines_DeletesTheSelectedLines_KeepingTheCaretColumn(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.DeleteLines(s)));
    }

    [Theory]
    [InlineData("a|b", "a   |b")]
    [InlineData("|ab", "    |ab")]
    [InlineData("a^b|c", "a   |c")]
    [InlineData("^ab\n\ncd|\nef", "^    ab\n\n    cd|\nef")]
    [InlineData("^ab\n|cd", "^    ab\n|cd")]
    public void Indent_IndentsSelectedLines_OrInsertsSpacesToTheNextLevel(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.Indent(s)));
    }

    [Theory]
    [InlineData("      a|b", "  a|b")]
    [InlineData("  a|b", "a|b")]
    [InlineData("\t  a|b", "  a|b")]
    [InlineData("^    ab\n  cd|", "^ab\ncd|")]
    [InlineData("a|b", "a|b")]
    public void Outdent_TakesOneLevelOffEverySelectedLine(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.Outdent(s)));
    }

    [Theory]
    [InlineData("    ab|c", "    ab\n    |c")]
    [InlineData("  |  ab", "  \n  |  ab")]
    [InlineData("a^b|c", "a\n|c")]
    public void NewLine_KeepsTheLinesIndentation(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.NewLine(s)));
    }

    [Theory]
    [InlineData("function f(x) -> y|", "function f(x) -> y\n    |")]
    [InlineData("    function f(x)|", "    function f(x)\n        |")]
    [InlineData("function f(x)|  ", "function f(x)\n    |  ")]
    [InlineData("function f(^x)|", "function f(\n    |")]
    [InlineData("function| f(x)", "function\n| f(x)")]
    [InlineData("function = 1|", "function = 1\n|")]
    [InlineData("functions = 1|", "functions = 1\n|")]
    [InlineData("# function f(x)|", "# function f(x)\n|")]
    [InlineData("  |function f(x)", "  \n  |function f(x)")]
    public void NewLine_IndentsTheBody_AfterAFunctionHeader(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.NewLine(s)));
    }

    [Theory]
    [InlineData("function f(x) -> y\n    y = x\n    end|", "function f(x) -> y\n    y = x\nend|")]
    [InlineData("  function f(x)\n        end|", "  function f(x)\n  end|")]
    [InlineData("function f(x)\n    end|\nb = 1", "function f(x)\nend|\nb = 1")]
    [InlineData("function f(x)\nend\n    end|", "function f(x)\nend\n    end|")]
    [InlineData("    end|", "    end|")]
    [InlineData("function f(x)\n    end| # done", "function f(x)\n    end| # done")]
    [InlineData("function f(x)\n    en|d", "function f(x)\n    en|d")]
    [InlineData("function f(x)\n    blend|", "function f(x)\n    blend|")]
    [InlineData("function = 1\n    end|", "function = 1\n    end|")]
    public void OutdentEnd_MatchesTheHeaderItCloses(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.OutdentEnd(s)));
    }

    [Theory]
    [InlineData("a|b", "# a|b")]
    [InlineData("|ab", "# |ab")]
    [InlineData("# a|b", "a|b")]
    [InlineData("#a|b", "a|b")]
    [InlineData("^  a\n\n    b|", "^  # a\n\n  #   b|")]
    [InlineData("^  # a\n\n    #b|", "^  a\n\n    b|")]
    [InlineData("^# a\nb|", "^# # a\n# b|")]
    [InlineData("  |", "  |")]
    public void ToggleComment_CommentsTheLines_OrUncommentsThemWhenAllAre(string text, string expected)
    {
        Assert.Equal(expected, Run(text, s => EditCommands.ToggleComment(s)));
    }

    [Fact]
    public void ALineBreakWithItsIndentation_IsTypingThatTheNextCharacterJoins()
    {
        var history = new EditHistory();
        var typed = Parse("    a|");
        var broken = EditCommands.NewLine(typed);
        history.Record(typed, broken, 0);
        history.Record(broken, new TextSnapshot(broken.Text + "b", broken.Caret + 1), 0);

        history.TryUndo(out var undone);

        Assert.Equal(typed, undone);
    }

    private static string Run(string text, System.Func<TextSnapshot, TextSnapshot> command)
    {
        return Format(command(Parse(text)));
    }

    private static TextSnapshot Parse(string marked)
    {
        var caret = marked.IndexOf('|');
        var anchor = marked.IndexOf('^');
        if (anchor < 0)
            return new TextSnapshot(marked.Remove(caret, 1), caret);

        var text = marked.Replace("|", string.Empty).Replace("^", string.Empty);
        if (anchor < caret)
            caret--;
        else
            anchor--;

        return new TextSnapshot(text, caret, anchor - caret);
    }

    private static string Format(TextSnapshot snapshot)
    {
        var anchor = snapshot.Caret + snapshot.Selection;
        if (snapshot.Selection == 0)
            return snapshot.Text.Insert(snapshot.Caret, "|");

        return anchor < snapshot.Caret
            ? snapshot.Text.Insert(snapshot.Caret, "|").Insert(anchor, "^")
            : snapshot.Text.Insert(anchor, "^").Insert(snapshot.Caret, "|");
    }
}
