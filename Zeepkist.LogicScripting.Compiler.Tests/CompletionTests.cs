using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Texts mark the caret with <c>|</c>.
/// </summary>
public class CompletionTests
{
    [Theory]
    [InlineData("lo|")]
    [InlineData("|")]
    [InlineData("x = lo|")]
    [InlineData("x = 1 & lo|")]
    [InlineData("x = !lo|")]
    [InlineData("power_display(lo|")]
    [InlineData("power_display(lo|)")]
    [InlineData("x = logic(1, lo|")]
    [InlineData("x = logic(op_in: lo|")]
    [InlineData("toggler(1, targets: [pl|")]
    [InlineData("x = 1\nlo|")]
    [InlineData("x = logic(\n    1,\n    lo|\n)")]
    [InlineData("x = (lo|")]
    [InlineData("x = lo|gic")]
    [InlineData("# a comment\nlo|")]
    [InlineData("x = \"a\" & lo|")]
    [InlineData("x = \"#\" & lo|")]
    public void Expression_WhereAnExpressionCanStart(string text)
    {
        Assert.Equal(CaretContextKind.Expression, At(text).Kind);
    }

    [Theory]
    [InlineData("# lo|")]
    [InlineData("x = 1 # lo|")]
    [InlineData("x = placed(\"lo|")]
    [InlineData("x = placed(\"lo|\")")]
    [InlineData("x = 12|")]
    [InlineData("x = 1|2")]
    [InlineData("x = 1lo|")]
    [InlineData("x = p.ho|")]
    [InlineData("x = p ho|")]
    [InlineData("x = f() lo|")]
    [InlineData("default hi|")]
    [InlineData("lo| = 1")]
    [InlineData("x = logic(1, o|p: and)")]
    public void None_InCommentsStringsNumbersAndWhereNoExpressionGoes(string text)
    {
        Assert.Equal(CaretContextKind.None, At(text).Kind);
    }

    [Fact]
    public void Word_IsTheWholeNameAroundTheCaret_AndThePrefixThePartBeforeIt()
    {
        var context = At("x = lo|gic(1)");

        Assert.Equal(4, context.Word.Start);
        Assert.Equal(5, context.Word.Length);
        Assert.Equal("lo", context.Prefix);
    }

    [Fact]
    public void Items_BlockFunctionsAliasesPlacedAndBooleans()
    {
        var labels = Labels("x = |");

        Assert.Contains("logic", labels);
        Assert.Contains("power_display", labels);
        Assert.Contains("mux", labels);
        Assert.Contains("placed", labels);
        Assert.Contains("true", labels);
        Assert.Contains("false", labels);
        Assert.DoesNotContain("lamp", labels);
        Assert.DoesNotContain("block", labels);
    }

    [Fact]
    public void Items_PrefixMatchesFirst_ThenSubstringMatches()
    {
        var labels = Labels("x = mul|");

        Assert.Equal(["multiplexer", "demultiplexer", "property_multiplier"], labels);
    }

    [Fact]
    public void Items_IgnoreCase()
    {
        Assert.Equal("logic", Labels("x = LO|")[0]);
    }

    [Fact]
    public void Items_FunctionsDescribeTheirInputs()
    {
        var logic = Completion.GetItems(At("lo|")).First(i => i.Label == "logic");
        var mux = Completion.GetItems(At("mu|")).First(i => i.Label == "mux");

        Assert.Equal("(a, b, op_in)", logic.Detail);
        Assert.Equal("multiplexer(in1, in2, in3, in4, select)", mux.Detail);
    }

    [Fact]
    public void Items_NoneWhereNothingCompletes()
    {
        Assert.Empty(Completion.GetItems(At("# lo|")));
    }

    [Theory]
    [InlineData("x = lo|", "logic", "x = logic(|")]
    [InlineData("x = lo|gic", "logic", "x = logic(|")]
    [InlineData("x = lo|(1, 2)", "logic", "x = logic(|1, 2)")]
    [InlineData("lo|\ny = 1", "logic", "logic(|\ny = 1")]
    [InlineData("x = tr|", "true", "x = true|")]
    [InlineData("x = |", "placed", "x = placed(|")]
    public void Accept_ReplacesTheWord_AndOpensAFunctionsCall(string text, string label, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context).First(i => i.Label == label);

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }

    private static System.Collections.Generic.List<string> Labels(string text)
    {
        return Completion.GetItems(At(text)).Select(i => i.Label).ToList();
    }

    private static CaretContext At(string marked)
    {
        var caret = marked.IndexOf('|');
        return CaretContext.At(marked.Remove(caret, 1), caret);
    }

    private static string Format(TextSnapshot snapshot)
    {
        return snapshot.Text.Insert(snapshot.Caret, "|");
    }
}
