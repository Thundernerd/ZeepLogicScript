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
    public void Items_FunctionsDescribeTheirInputs_ThenTheirSettings()
    {
        var logic = Completion.GetItems(At("lo|")).First(i => i.Label == "logic");

        Assert.Equal("(a, b, op_in, op:)", logic.Detail);
    }

    [Fact]
    public void Items_FunctionsWithoutSettings_DescribeOnlyTheirInputs()
    {
        var multiplexer = Completion.GetItems(At("multi|")).First(i => i.Label == "multiplexer");

        Assert.Equal("(in1, in2, in3, in4, select)", multiplexer.Detail);
    }

    [Fact]
    public void Items_FunctionsWithStyles_DescribeTheirStyle()
    {
        var powerDisplay = Completion.GetItems(At("pow|")).First(i => i.Label == "power_display");

        Assert.Equal("(x, style:)", powerDisplay.Detail);
    }

    [Fact]
    public void Items_AliasesDescribeTheirFunction()
    {
        var mux = Completion.GetItems(At("mu|")).First(i => i.Label == "mux");
        var lerp = Completion.GetItems(At("ler|")).First(i => i.Label == "lerp");

        Assert.Equal("multiplexer(in1, in2, in3, in4, select)", mux.Detail);
        Assert.Equal("math_3_pins(start, end, t, op_in, op:)", lerp.Detail);
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
    [InlineData("function edge(x, n) -> y\nend\nz = |")]
    [InlineData("z = |\nfunction edge(x, n) -> y\nend")]
    [InlineData("function edge(x, n) -> y\n    a = |\nend")]
    public void Items_TheScriptsFunctions_WithTheirInputs(string text)
    {
        var edge = Completion.GetItems(At(text)).First(i => i.Label == "edge");

        Assert.Equal(CompletionKind.Function, edge.Kind);
        Assert.Equal("(x, n)", edge.Detail);
    }

    [Fact]
    public void Items_AFunctionDefinedTwice_Once()
    {
        Assert.Single(Labels("function f()\nend\nfunction f(a)\nend\nx = |"), l => l == "f");
    }

    [Theory]
    [InlineData("|", "function")]
    [InlineData("x = 1\nfu|", "function")]
    [InlineData("function f()\nend\n|", "function")]
    [InlineData("function f()\n    |\nend", "end")]
    [InlineData("function f()\n    x = 1\n    en|", "end")]
    [InlineData("function f()\n    x = 1\nend|", "end")]
    public void Items_FunctionAndEnd_AtTheStartOfAStatement(string text, string keyword)
    {
        var item = Completion.GetItems(At(text)).First(i => i.Label == keyword);

        Assert.Equal(CompletionKind.Keyword, item.Kind);
    }

    [Theory]
    [InlineData("x = |")]
    [InlineData("x = 1 & |")]
    [InlineData("logic(|")]
    [InlineData("function f()\n    x = |\nend")]
    public void Items_NoFunctionOrEnd_InsideAStatement(string text)
    {
        var labels = Labels(text);

        Assert.DoesNotContain("function", labels);
        Assert.DoesNotContain("end", labels);
    }

    [Fact]
    public void Items_NoEnd_InACallLeftOpen()
    {
        Assert.DoesNotContain("end", Labels("function f(x)\n    a = logic(x,\n    |"));
    }

    [Fact]
    public void Items_ANameAFunctionHas_OnlyAsTheFunction()
    {
        var items = Names("f = 1\nfunction f(x)\nend\nb = |").Where(i => i.Label == "f").ToList();

        Assert.Equal(CompletionKind.Function, Assert.Single(items).Kind);
    }

    [Fact]
    public void Items_InsideAFunctionWithoutAName_OnlyItsOwnNames()
    {
        var labels = NameLabels("a = 1\nfunction (x)\n    q = 1\n    r = |\nend");

        Assert.Contains("q", labels);
        Assert.DoesNotContain("a", labels);
    }

    [Fact]
    public void Items_NoEnd_OutsideAFunction_NorFunction_InsideOne()
    {
        Assert.DoesNotContain("end", Labels("x = 1\n|"));
        Assert.DoesNotContain("function", Labels("function f()\n    |\nend"));
    }

    [Theory]
    [InlineData("function f(|")]
    [InlineData("function f(a, |")]
    [InlineData("function f(a) -> |")]
    [InlineData("function f(a) -> b, |\nend")]
    [InlineData("function fo|")]
    public void None_OnAFunctionsFirstLine(string text)
    {
        Assert.Equal(CaretContextKind.None, At(text).Kind);
    }

    [Fact]
    public void Items_InsideAFunction_ItsInputsAndDefinitionsAbove_NotTheTopLevels()
    {
        var labels = NameLabels("a = 1\nfunction f(x)\n    b = 1\n    c = |\n    d = 1\nend");

        Assert.Contains("x", labels);
        Assert.Contains("b", labels);
        Assert.DoesNotContain("a", labels);
        Assert.DoesNotContain("c", labels);
        Assert.DoesNotContain("d", labels);
    }

    [Fact]
    public void Items_AFunctionsInputs_DescribedAsInputs()
    {
        Assert.Equal("input", Names("function f(x)\n    c = |\nend").First(i => i.Label == "x").Detail);
    }

    [Fact]
    public void Items_InsideDelayInAFunction_OnlyItsOwnNames()
    {
        var labels = NameLabels("a = 1\nfunction f(x)\n    c = delay(|)\n    d = 1\nend\nb = 1");

        Assert.Contains("x", labels);
        Assert.Contains("d", labels);
        Assert.DoesNotContain("a", labels);
        Assert.DoesNotContain("b", labels);
    }

    [Theory]
    [InlineData("function f(x)\n    b = 1\nend\nc = |")]
    [InlineData("c = delay(|)\nfunction f(x)\n    b = 1\nend")]
    public void Items_OutsideAFunction_NoneOfItsNames(string text)
    {
        var labels = NameLabels(text);

        Assert.DoesNotContain("x", labels);
        Assert.DoesNotContain("b", labels);
    }

    [Theory]
    [InlineData("function f(a, b)\nend\nf(|", new[] { "a:", "b:" })]
    [InlineData("function f(a, b)\nend\nf(1, |", new[] { "b:" })]
    [InlineData("function f(a, b)\nend\nf(b: 1, |", new[] { "a:" })]
    [InlineData("f(|\nfunction f(a, b)\nend", new[] { "a:", "b:" })]
    public void Items_InACallToAFunction_ItsInputsItDoesntHaveYet(string text, string[] arguments)
    {
        var items = Completion.GetItems(At(text)).Where(i => i.Kind == CompletionKind.Argument).ToList();

        Assert.Equal(arguments, items.Select(i => i.Label));
        Assert.All(items, i => Assert.Equal("input", i.Detail));
    }

    [Fact]
    public void Accept_AFunction_OpensItsCall()
    {
        var context = At("function edge(x)\nend\nz = ed|");
        var item = Completion.GetItems(context).First(i => i.Label == "edge");

        Assert.Equal("function edge(x)\nend\nz = edge(|", Format(Completion.Accept(context, item)));
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

    [Fact]
    public void Items_InACallToANameDefinedTwice_TheFirstDefinitionsInputs()
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
        };
        var context = At("lamp_1 = placed(\"L\")\nlamp_1 = logic(1, 2)\nlamp_1(|");
        var labels = Completion.GetItems(context, DefinedNames.Read(context.Text, uid => level.GetValueOrDefault(uid)))
            .Where(i => i.Kind == CompletionKind.Argument)
            .Select(i => i.Label);

        Assert.Equal(["angle:", "blue:", "green:", "intensity:", "range:", "red:"], labels);
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

    [Theory]
    [InlineData("x = p.ho|", "p")]
    [InlineData("x = p.|", "p")]
    [InlineData("x = p.|ho", "p")]
    [InlineData("x = 1 & p.|", "p")]
    [InlineData("p.|", "p")]
    [InlineData("x = logic(p.|", "p")]
    public void Output_AfterANameAndADot(string text, string name)
    {
        var context = At(text);

        Assert.Equal(CaretContextKind.Output, context.Kind);
        Assert.Equal(name, context.DottedName);
    }

    [Theory]
    [InlineData("x = f().|", "f", null)]
    [InlineData("x = f().ho|", "f", null)]
    [InlineData("x = f(g(1), [a]).|", "f", null)]
    [InlineData("x = f([a, (b)], g([c])).|", "f", null)]
    [InlineData("x = logic(simple_telemetry(mode: local_velocity).|", "simple_telemetry", "local_velocity")]
    [InlineData("x = simple_telemetry(a: logic(mode: b), mode: local_velocity).|", "simple_telemetry", "local_velocity")]
    public void Output_AfterACallAndADot(string text, string callee, string mode)
    {
        var context = At(text);

        Assert.Equal(CaretContextKind.Output, context.Kind);
        Assert.Equal(callee, context.DottedCallee);
        Assert.Equal(mode, context.DottedMode);
        Assert.Null(context.DottedName);
    }

    [Theory]
    [InlineData("x = 1.|")]
    [InlineData("x = p .|")]
    [InlineData("# p.|")]
    [InlineData("x = f() .|")]
    [InlineData("x = (a).|")]
    [InlineData("x = f()(1).|")]
    [InlineData("x = t.f().|")]
    [InlineData("x = [a].|")]
    public void None_AfterADotThatDoesntFollowANameOrACall(string text)
    {
        Assert.Equal(CaretContextKind.None, At(text).Kind);
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfTheBlockTheNameIsDefinedAs_InConnectorOrder()
    {
        Assert.Equal(["speed", "yaw", "up", "state"], NameLabels("t = simple_telemetry()\nx = t.|"));
    }

    [Theory]
    [InlineData("t = simple_telemetry(mode: local_velocity)\nx = t.|", new[] { "yaw", "x", "y", "z" })]
    [InlineData("t = simple_telemetry(1, mode: global_velocity)\nx = t.|", new[] { "x", "y", "z", "heading" })]
    [InlineData("t = simple_telemetry(mode: basic)\nx = t.|", new[] { "speed", "yaw", "up", "state" })]
    public void Items_AfterADot_FollowTheWrittenMode(string text, string[] expected)
    {
        Assert.Equal(expected, NameLabels(text));
    }

    [Fact]
    public void Items_AfterADot_MatchThePrefix()
    {
        Assert.Equal(["yaw", "y"], NameLabels("t = simple_telemetry(mode: local_velocity)\nx = t.y|"));
    }

    [Fact]
    public void Items_AfterADot_AreOutputs()
    {
        Assert.All(Names("t = simple_telemetry()\nx = t.|"), i => Assert.Equal(CompletionKind.Output, i.Kind));
    }

    [Theory]
    [InlineData(0, new[] { "speed", "yaw", "up", "state" })]
    [InlineData(2, new[] { "x", "y", "z", "heading" })]
    public void Items_AfterAPlacedBlockReferencesDot_ItsOutputsInTheLevel(int mode, string[] expected)
    {
        var level = new Dictionary<string, PlacedBlock>
        {
            ["T"] = new PlacedBlock(
                BlockManifest.SimpleTelemetry.DefaultStyle.BlockId,
                [Setting.Int(BlockManifest.SimpleTelemetryMode.Key, mode)]),
            ["D"] = new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId)
        };
        var context = At("t = placed(\"T\")\nd = placed(\"D\")\nx = t.|");
        var items = Completion.GetItems(context, DefinedNames.Read(context.Text, uid => level.GetValueOrDefault(uid)));

        Assert.Equal(expected, items.Select(i => i.Label));

        context = At("t = placed(\"T\")\nd = placed(\"D\")\nx = d.|");
        items = Completion.GetItems(context, DefinedNames.Read(context.Text, uid => level.GetValueOrDefault(uid)));
        Assert.Equal(["distance", "x", "y", "z"], items.Select(i => i.Label));
    }

    [Theory]
    [InlineData("x = a & b\ny = x.|")]
    [InlineData("x = 1\ny = x.|")]
    [InlineData("x = logic(1, 2)\ny = x.|")]
    [InlineData("x = simple_telemetry() & 1\ny = x.|")]
    [InlineData("x = placed(\"UNKNOWN\")\ny = x.|")]
    [InlineData("t = simple_telemetry(mode: sideways)\nx = t.|")]
    [InlineData("y = t.|\nt = simple_telemetry()")]
    [InlineData("y = nothing.|")]
    [InlineData("function f(t)\n  y = t.|\nend")]
    [InlineData("t = simple_telemetry()\nfunction f()\n  y = t.|\nend")]
    [InlineData("function f(a) -> hi\nend\nr = f(1)\nx = r.|")]
    [InlineData("function f(a)\nend\nr = f(1)\nx = r.|")]
    [InlineData("function f(a) -> hi, lo\nend\nr = f(1) & 1\nx = r.|")]
    [InlineData("r = g(1)\nx = r.|")]
    [InlineData("function f(a) -> hi, lo\nend\nfunction g()\n  r = f(1)\nend\nx = r.|")]
    public void Items_AfterADot_NothingWhenTheNameIsntAVisibleBlockOrFunctionCallWithSeveralOutputs(string text)
    {
        Assert.Empty(Names(text));
    }

    [Fact]
    public void Items_AfterADot_TheFirstDefinitionOfANameDefinedTwice()
    {
        const string Text = "t = simple_telemetry(mode: global_velocity)\nt = simple_telemetry()\nx = t.|";

        Assert.Equal(["x", "y", "z", "heading"], NameLabels(Text));
    }

    [Fact]
    public void Items_AfterADot_InsideDelay_ALaterDefinition()
    {
        Assert.Equal(["speed", "yaw", "up", "state"], NameLabels("x = delay(t.|, ticks: 1)\nt = simple_telemetry()"));
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfTheFunctionTheNameCalls_InTheOrderItWritesThem()
    {
        Assert.Equal(["lo", "hi"], NameLabels("function f(a) -> lo, hi\nend\nr = f(1)\nx = r.|"));
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfTheFirstDefinitionOfAFunctionDefinedTwice()
    {
        const string Text = "function f(a) -> lo, hi\nend\nfunction f(a) -> x, y, z\nend\nr = f(1)\nx = r.|";

        Assert.Equal(["lo", "hi"], NameLabels(Text));
    }

    [Fact]
    public void Items_AfterADot_TheOutputsOfAFunctionCallInsideAFunction()
    {
        Assert.Equal(["lo", "hi"], NameLabels("function f(a) -> lo, hi\nend\nfunction g()\n  r = f(1)\n  x = r.|\nend"));
    }

    [Theory]
    [InlineData("x = simple_telemetry().|", new[] { "speed", "yaw", "up", "state" })]
    [InlineData("x = simple_telemetry(mode: global_velocity).|", new[] { "x", "y", "z", "heading" })]
    [InlineData("x = simple_telemetry(hide: true, mode: local_velocity).|", new[] { "yaw", "x", "y", "z" })]
    public void Items_AfterACallsDot_TheOutputsOfTheBlockFunction_InItsWrittenMode(string text, string[] expected)
    {
        Assert.Equal(expected, Labels(text));
    }

    [Theory]
    [InlineData("x = simple_telemetry(mode: global_velocity, mode: local_velocity).|")]
    [InlineData("t = simple_telemetry(mode: global_velocity, mode: local_velocity)\nx = t.|")]
    public void Items_AfterADot_TheFirstModeOfACallThatWritesItTwice(string text)
    {
        Assert.Equal(["x", "y", "z", "heading"], NameLabels(text));
    }

    [Theory]
    [InlineData("function f(a) -> lo, hi\nend\nx = f(1).|")]
    [InlineData("x = f(1).|\nfunction f(a) -> lo, hi\nend")]
    [InlineData("function f(a) -> lo, hi\nend\nfunction f(a) -> x, y, z\nend\nx = f(1).|")]
    [InlineData("function f(a) -> lo, hi\nend\nfunction g()\n  x = f(1).|\nend")]
    public void Items_AfterACallsDot_TheOutputsOfTheFirstDefinitionOfTheFunction_InTheOrderItWritesThem(string text)
    {
        Assert.Equal(["lo", "hi"], Labels(text));
    }

    [Theory]
    [InlineData("x = logic(1, 2).|")]
    [InlineData("x = simple_telemetry(mode: sideways).|")]
    [InlineData("function f(a) -> hi\nend\nx = f(1).|")]
    [InlineData("x = g(1).|")]
    [InlineData("x = placed(\"T\").|")]
    public void Items_AfterACallsDot_NothingWhenItsNotToABlockOrFunctionWithSeveralOutputs(string text)
    {
        Assert.Empty(Labels(text));
    }

    [Fact]
    public void Accept_AnOutput_WritesItsName()
    {
        var context = At("t = simple_telemetry()\nx = t.sp|");
        var item = Completion.GetItems(context, DefinedNames.Read(context.Text)).First();

        Assert.Equal("t = simple_telemetry()\nx = t.speed|", Format(Completion.Accept(context, item)));
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
    [InlineData("x = logic(\n    a,\n    op:\n    |")]
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
    [InlineData("default hide:\n|")]
    [InlineData("x = logic(a)\nop:\n|")]
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
