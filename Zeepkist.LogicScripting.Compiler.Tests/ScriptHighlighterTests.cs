using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptHighlighterTests
{
    [Fact]
    public void EachToken_GetsTheColourOfItsKind()
    {
        const string Text = "x = logic(a, 12) & placed(\"U\")";

        Assert.Equal(
            [
                "x:Name", "=:Operator", "logic:Function", "(:Punctuation", "a:Name", ",:Punctuation",
                "12:Number", "):Punctuation", "&:Operator", "placed:Function", "(:Punctuation", "\"U\":String",
                "):Punctuation"
            ],
            Render(Text));
    }

    [Fact]
    public void Comments_RunToTheEndOfTheLine_ButNotInsideText()
    {
        const string Text = "a = placed(\"#1\") # note\ndefault hide: true";

        Assert.Equal(
            [
                "a:Name", "=:Operator", "placed:Function", "(:Punctuation", "\"#1\":String", "):Punctuation",
                "# note:Comment", "default:Keyword", "hide:Name", "::Punctuation", "true:Keyword"
            ],
            Render(Text));
    }

    [Fact]
    public void CommentLinesInARow_EachGetASpan()
    {
        Assert.Equal(["# a:Comment", "# b:Comment", "x:Name"], Render("# a\n\n# b\nx"));
    }

    [Fact]
    public void UnclosedText_RunsToTheEndOfItsLine()
    {
        Assert.Equal(["a:Name", "=:Operator", "\"bc:String", "d:Name"], Render("a = \"bc\nd"));
    }

    [Fact]
    public void Spans_AreFoundAcrossLinesJoinedByBrackets()
    {
        Assert.Equal(
            ["x:Name", "=:Operator", "logic:Function", "(:Punctuation", "1:Number", "):Punctuation"],
            Render("x = logic(\n  1\n)"));
    }

    private static string[] Render(string text)
    {
        return ScriptHighlighter.Highlight(text).Select(s => $"{text.Substring(s.Start, s.Length)}:{s.Kind}").ToArray();
    }
}
