using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class ScriptHighlighterTests
{
    private static readonly Dictionary<string, IReadOnlyCollection<string>> _timing = new() { ["timing"] = ["edge"] };

    [Fact]
    public void EachToken_GetsTheColorOfItsKind()
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

    [Fact]
    public void FunctionWords_AreKeywords_AndCallsToLocalFunctions_AreFunctions()
    {
        const string Text = "y = f(1)\nfunction f(x) -> r\nend";

        Assert.Equal(
            [
                "y:Name", "=:Operator", "f:Function", "(:Punctuation", "1:Number", "):Punctuation",
                "function:Keyword", "f:Function", "(:Punctuation", "x:Name", "):Punctuation", "->:Operator", "r:Name",
                "end:Keyword"
            ],
            Render(Text));
    }

    [Fact]
    public void QualifiedCalls_ToKnownLibraryFunctions_AreFunctions()
    {
        const string Text = "y = timing.edge(p.horn) | timing.nope(1) | other.edge(2)";

        Assert.Equal(
            [
                "y:Name", "=:Operator", "timing:Function", ".:Punctuation", "edge:Function", "(:Punctuation", "p:Name",
                ".:Punctuation", "horn:Name", "):Punctuation", "|:Operator", "timing:Name", ".:Punctuation", "nope:Name",
                "(:Punctuation", "1:Number", "):Punctuation", "|:Operator", "other:Name", ".:Punctuation", "edge:Name",
                "(:Punctuation", "2:Number", "):Punctuation"
            ],
            Render(Text, _timing));
    }

    [Theory]
    [InlineData("y = timing.edge")]
    [InlineData("y = a.timing.edge(1)")]
    public void LibraryAndFunctionNames_OutsideAQualifiedCall_AreNames(string text)
    {
        Assert.DoesNotContain(Render(text, _timing), s => s is "timing:Function" or "edge:Function");
    }

    [Fact]
    public void FunctionNames_AreTheNamesALibraryDefines()
    {
        var names = ScriptHighlighter.GetFunctionNames("# edges\nfunction edge(x) -> y\n    y = x\nend\nfunction pulse()\nend");

        Assert.Equal(["edge", "pulse"], names.OrderBy(n => n));
    }

    private static string[] Render(string text, IReadOnlyDictionary<string, IReadOnlyCollection<string>> libraries = null)
    {
        return ScriptHighlighter.Highlight(text, libraries).Select(s => $"{text.Substring(s.Start, s.Length)}:{s.Kind}").ToArray();
    }
}
