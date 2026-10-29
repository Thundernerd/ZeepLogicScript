using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
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
    [InlineData("x = logic(op_in: lo|")]
    [InlineData("toggler(1, targets: [pl|")]
    [InlineData("x = 1\nlo|")]
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
    [InlineData("lo| = 1")]
    [InlineData("x = lo|: 1")]
    [InlineData("x = default hi|")]
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

    [Fact]
    public void Items_NamesDefinedAbove_WithTheirBlockType()
    {
        var a = Names("a = generator(power: 1)\nb = 1\n\nx = |").First(i => i.Label == "a");

        Assert.Equal(CompletionKind.Name, a.Kind);
        Assert.Equal("generator", a.Detail);
        Assert.Contains("b", NameLabels("a = generator(power: 1)\nb = 1\n\nx = |"));
    }

    [Fact]
    public void Items_AreInAlphabeticalOrder_WithTheNames()
    {
        Assert.Equal(["lo", "logic", "lower"], NameLabels("lower = 1\nlo = 1\nx = lo|").Take(3));
    }

    [Theory]
    [InlineData("x = |\na = 1")]
    [InlineData("a = |")]
    [InlineData("a = logic(1, |")]
    [InlineData("a = logic(\n    1,\n    |\n)")]
    [InlineData("x = logic(1, delay(2), |\na = 1")]
    [InlineData("a = logic(\n    1,\n    |")]
    [InlineData("a = logic(\n    |\nb = 1")]
    public void Items_NoNamesDefinedAtOrBelowTheCaret(string text)
    {
        Assert.DoesNotContain("a", NameLabels(text));
    }

    [Theory]
    [InlineData("x = delay(|\na = 1")]
    [InlineData("a = delay(|")]
    [InlineData("a = delay(1 & |)")]
    [InlineData("a = delay(logic(1, |))")]
    [InlineData("a = delay(\n    logic(1, b: |)\n)")]
    [InlineData("x = delay(\n    |\na = 1")]
    public void Items_AnyName_InsideDelay(string text)
    {
        Assert.Contains("a", NameLabels(text));
    }

    [Fact]
    public void Items_NamesAboveADefinition_AfterACallLeftOpen()
    {
        Assert.Contains("a", NameLabels("a = logic(\nb = |"));
    }

    [Fact]
    public void Items_ANameDefinedTwice_Once()
    {
        Assert.Single(NameLabels("a = 1\na = 2\nx = |"), l => l == "a");
    }

    [Fact]
    public void Items_NoNamesWhereNothingCompletes()
    {
        Assert.Empty(Names("a = 1\n# |"));
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

    [Fact]
    public void Accept_AName_WritesItAlone()
    {
        var context = At("abc = 1\nx = ab|");
        var item = Completion.GetItems(context, DefinedNames.Read(context.Text)).First(i => i.Label == "abc");

        Assert.Equal("abc = 1\nx = abc|", Format(Completion.Accept(context, item)));
    }

    [Theory]
    [InlineData("power_display(lo|")]
    [InlineData("power_display(lo|)")]
    [InlineData("x = logic(1, lo|")]
    [InlineData("x = logic(\n    1,\n    lo|\n)")]
    [InlineData("x = logic(\n    1,\n    lo|")]
    [InlineData("x = logic(1, logic(|), 2)")]
    public void Argument_WhereAnArgumentOfACallStarts(string text)
    {
        Assert.Equal(CaretContextKind.Argument, At(text).Kind);
    }

    [Theory]
    [InlineData("x = logic(1, o|p: and)")]
    [InlineData("x = logic(1, |: and)")]
    public void ArgumentName_BeforeANamedArgumentsColon(string text)
    {
        Assert.Equal(CaretContextKind.ArgumentName, At(text).Kind);
    }

    [Theory]
    [InlineData("default |")]
    [InlineData("default hi|")]
    [InlineData("x = 1\ndefault hi|: true")]
    public void DefaultSetting_AfterDefault(string text)
    {
        Assert.Equal(CaretContextKind.DefaultSetting, At(text).Kind);
    }

    [Fact]
    public void Items_InACall_NamedInputsAndSettings_BeforeTheExpressions()
    {
        var labels = Labels("logic(a, |");

        Assert.Equal(["b:", "hide:", "op:", "op_in:"], labels.Take(4));
        Assert.Contains("logic", labels);
    }

    [Fact]
    public void Items_InACall_LeaveOutTheNamedArgumentsItAlreadyHas()
    {
        Assert.DoesNotContain("op:", Labels("logic(a, op: and, |"));
        Assert.DoesNotContain("op:", Labels("logic(a, |, op: and)"));
        Assert.DoesNotContain("op:", Labels("logic(a, |\n    op: and)"));
        Assert.DoesNotContain("op:", Labels("x = logic(\n    a,\n    op: and,\n    |"));
        Assert.Contains("op:", Labels("logic(a, o|p: and)"));
    }

    [Theory]
    [InlineData("logic(|", "a:")]
    [InlineData("logic(1, |", "b:")]
    [InlineData("logic(op: and, 1, |", "b:")]
    [InlineData("logic(\n    1,\n    |", "b:")]
    [InlineData("logic(1, b|", "b:")]
    [InlineData("logic(f(1, 2), |", "b:")]
    public void Items_InACall_LeaveOutTheInputsItsPositionalArgumentsGive(string text, string first)
    {
        Assert.Equal(first, Labels(text).First(l => l.EndsWith(':')));
    }

    [Theory]
    [InlineData("logic(1, 2, |")]
    [InlineData("logic(1, |, 2)")]
    [InlineData("logic(1, | , 2)")]
    public void Items_InACall_NoInputsLeft_OnlyItsSettings(string text)
    {
        Assert.Equal(["hide:", "op:", "op_in:"], Labels(text).Where(l => l.EndsWith(':')));
    }

    [Fact]
    public void Items_InACall_OnlyItsOwnNamedArguments()
    {
        Assert.Contains("op:", Labels("logic(a, delay(1, ticks: 2, op: and), |"));
        Assert.Contains("ticks:", Labels("x = logic(op: and, delay(|"));
        Assert.DoesNotContain("op:", Labels("x = logic(op: and, delay(|"));
    }

    [Fact]
    public void Items_InACall_DescribeTheArguments()
    {
        var items = Completion.GetItems(At("logic(|"));

        Assert.Equal("input", items.First(i => i.Label == "a:").Detail);
        Assert.Equal("and, or, nand, nor, xor, xnor", items.First(i => i.Label == "op:").Detail);
        Assert.Equal("true, false", items.First(i => i.Label == "hide:").Detail);
        Assert.All(items.Where(i => i.Label.EndsWith(':')), i => Assert.Equal(CompletionKind.Argument, i.Kind));
    }

    [Fact]
    public void Items_InACallWithStyles_OfferStyle()
    {
        var style = Completion.GetItems(At("passthrough(|")).First(i => i.Label == "style:");

        Assert.Equal("normal, skinny_1h, skinny_2h, skinny_3h", style.Detail);
        Assert.DoesNotContain("style:", Labels("logic(|"));
    }

    [Fact]
    public void Items_BeforeAColon_OnlyNamedArguments()
    {
        Assert.Equal(["op:", "op_in:"], Labels("logic(a, o|: and)"));
    }

    [Fact]
    public void Items_InACallToAPlacedBlockReference_OnlyItsInputs()
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
        };
        var context = At("lamp_1 = placed(\"L\")\nlamp_1(1, |");
        var labels = Completion.GetItems(context, DefinedNames.Read(context.Text, uid => level.GetValueOrDefault(uid)))
            .Where(i => i.Kind == CompletionKind.Argument)
            .Select(i => i.Label);

        Assert.Equal(["angle:", "blue:", "green:", "range:", "red:"], labels);
    }

    [Theory]
    [InlineData("a = logic(1, 2)\na(|")]
    [InlineData("a = placed(\"UNKNOWN\")\na(|")]
    [InlineData("a(|\na = placed(\"L\")")]
    [InlineData("x = (|")]
    [InlineData("x = |")]
    [InlineData("toggler(targets: [|")]
    public void Items_NoNamedArguments_OutsideACallThatTakesThem(string text)
    {
        Assert.DoesNotContain(Names(text), i => i.Kind == CompletionKind.Argument);
    }

    [Fact]
    public void Items_AfterDefault_Hide()
    {
        Assert.Equal(["hide:"], Labels("default |"));
        Assert.Empty(Labels("default x|"));
    }

    [Theory]
    [InlineData("logic(a, o|", "op:", "logic(a, op: |")]
    [InlineData("logic(a, o|p", "op:", "logic(a, op: |")]
    [InlineData("logic(a, o|: and)", "op:", "logic(a, op|: and)")]
    [InlineData("logic(a, o| : and)", "op:", "logic(a, op| : and)")]
    [InlineData("default |", "hide:", "default hide: |")]
    public void Accept_ANamedArgument_WritesItsColon_UnlessOneFollows(string text, string label, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context).First(i => i.Label == label);

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }

    [Theory]
    [InlineData("logic(a, b, op: |")]
    [InlineData("logic(a, b, op:|")]
    [InlineData("logic(a, op: a|nd)")]
    [InlineData("x = logic(\n    a,\n    op: |")]
    [InlineData("logic(a, hide: |")]
    [InlineData("passthrough(1, style: |")]
    [InlineData("death(1, player: 2|")]
    [InlineData("delay(1, ticks: |")]
    [InlineData("default hide: |")]
    [InlineData("x = 1\ndefault hide: tr|")]
    public void SettingValue_AfterASettingsColon(string text)
    {
        Assert.Equal(CaretContextKind.SettingValue, At(text).Kind);
    }

    [Theory]
    [InlineData("logic(a, b: |")]
    [InlineData("toggler(1, targets: |")]
    [InlineData("logic(a, nope: |")]
    [InlineData("logic(style: |")]
    [InlineData("default op: |")]
    [InlineData("lamp_1 = placed(\"L\")\nlamp_1(red: |")]
    public void Expression_AfterTheColonOfAnInputOrPlacedBlocksOrAnythingElse(string text)
    {
        Assert.Equal(CaretContextKind.Expression, At(text).Kind);
    }

    [Fact]
    public void SettingValue_KnowsTheCallAndTheSetting()
    {
        var context = At("x = logic(a, op: |");

        Assert.Equal("logic", context.Callee);
        Assert.Equal("op", context.SettingName);
        Assert.Null(At("default hide: |").Callee);
        Assert.Null(At("x = logic(a, |").SettingName);
    }

    [Theory]
    [InlineData("logic(a, b, op: |", new[] { "and", "or", "nand", "nor", "xor", "xnor" })]
    [InlineData("logic(a, hide: |", new[] { "true", "false" })]
    [InlineData("default hide: |", new[] { "true", "false" })]
    [InlineData("death(1, player: |", new[] { "all", "1", "2", "3", "4" })]
    [InlineData("player_input(player: |", new[] { "1", "2", "3", "4" })]
    [InlineData("death(1, player: 2|", new[] { "2" })]
    [InlineData("passthrough(1, style: |", new[] { "normal", "skinny_1h", "skinny_2h", "skinny_3h" })]
    [InlineData("logic(a, op: n|", new[] { "nand", "nor", "and", "xnor" })]
    public void Items_ASettingsValues_InTheOrderItStoresThem(string text, string[] expected)
    {
        Assert.Equal(expected, Labels(text));
    }

    [Theory]
    [InlineData("delay(1, ticks: |")]
    [InlineData("generator(power: |")]
    public void Items_NoneForANumberSetting(string text)
    {
        Assert.Empty(Labels(text));
    }

    [Fact]
    public void Items_ASettingsValues_AreValues()
    {
        Assert.All(Completion.GetItems(At("logic(a, op: |")), i => Assert.Equal(CompletionKind.Value, i.Kind));
    }

    [Theory]
    [InlineData("logic(a, op: n|", "nor", "logic(a, op: nor|")]
    [InlineData("logic(a, op: a|nd)", "nand", "logic(a, op: nand|)")]
    [InlineData("default hide: |", "true", "default hide: true|")]
    public void Accept_AValue_ReplacesTheWord(string text, string label, string expected)
    {
        var context = At(text);
        var item = Completion.GetItems(context).First(i => i.Label == label);

        Assert.Equal(expected, Format(Completion.Accept(context, item)));
    }

    /// <summary>
    /// With the names the text itself defines.
    /// </summary>
    private static IReadOnlyList<CompletionItem> Names(string text)
    {
        var context = At(text);
        return Completion.GetItems(context, DefinedNames.Read(context.Text));
    }

    private static List<string> NameLabels(string text)
    {
        return Names(text).Select(i => i.Label).ToList();
    }

    private static List<string> Labels(string text)
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
