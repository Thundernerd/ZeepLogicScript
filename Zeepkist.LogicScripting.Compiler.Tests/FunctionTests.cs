using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class FunctionTests
{
    private const string Edge = "function edge(x) -> rising\n    prev = delay(x, ticks: 1)\n    rising = x & !prev\nend\n";

    private const string Latch = "function sr_latch(set, reset) -> q, not_q\n    q = delay(!reset & (set | q), ticks: 1)\n    not_q = !q\nend\n";

    [Fact]
    public void Expansion_EqualsTheHandWrittenWiring()
    {
        var expanded = Compile(Edge + "p = player_input()\npower_display(edge(p.horn))");
        var handWritten = Compile("p = player_input()\nprev = delay(p.horn, ticks: 1)\npower_display(p.horn & !prev)");

        Assert.Equal(BlockTree.RenderLast(handWritten), BlockTree.RenderLast(expanded));
        Assert.Equal(handWritten.Blocks.Select(b => b.Type), expanded.Blocks.Select(b => b.Type));
    }

    [Fact]
    public void Function_MayBeCalledBeforeItsDefinition()
    {
        var result = Compile("p = player_input()\npower_display(edge(p.horn))\n" + Edge);

        Assert.Equal("logic[xmo=0](player_input().horn, digitizer[xt1=1](delay[ldl=1](player_input().horn)))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void SeveralOutputs_ArePickedWithADot()
    {
        var result = Compile(Latch + "l = sr_latch(1, 0)\npower_display(l.not_q)\npower_display(sr_latch(0, 1).q)");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(2, result.Blocks.Count(b => b.Type == BlockManifest.Delay));
        var displays = result.Blocks.Select((b, i) => (b, i)).Where(x => x.b.Type == BlockManifest.PowerDisplay).Select(x => x.i).ToList();
        Assert.Equal([BlockManifest.Digitizer, BlockManifest.Delay], displays.Select(d => result.Blocks[result.Wires.Single(w => w.To.Block == d).From.Block].Type));
    }

    [Fact]
    public void NamedArguments_GiveInputsByName()
    {
        var result = Compile("function minus(a, b) -> d\n    d = a - b\nend\npower_display(minus(b: 1, a: 2))");

        Assert.Equal("math[xmo=1](2, 1)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void FunctionWithoutOutputs_IsCalledOnItsOwnLine()
    {
        var result = Compile("function show(x)\n    power_display(x, hide: true)\nend\nshow(3)");

        Assert.Equal("3", BlockTree.RenderLast(result));
        Assert.Equal(2, result.Blocks.Count);
    }

    [Fact]
    public void Argument_IsEvaluatedOnceAndShared()
    {
        var result = Compile("function twice(x) -> y\n    y = x + x\nend\npower_display(twice(1 & 2))");

        Assert.Single(result.Blocks, b => b.Type == BlockManifest.Logic);
        Assert.Equal("math[xmo=0](logic[xmo=0](1, 2), logic[xmo=0](1, 2))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Literals_AreSharedAcrossExpansions()
    {
        var result = Compile("function inc(x) -> y\n    y = x + 1\nend\npower_display(inc(1) + inc(5))");

        Assert.Equal(2, result.Blocks.Count(b => b.Type == BlockManifest.Generator));
        Assert.Equal("math[xmo=0](math[xmo=0](1, 1), math[xmo=0](5, 1))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void BodyNames_DontClashWithTheCallersOrEachOther()
    {
        var result = Compile("function inc(x) -> y\n    y = x + 1\nend\ny = inc(2)\nz = inc(y)\npower_display(z)");

        Assert.Equal("math[xmo=0](math[xmo=0](2, 1), 1)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Functions_CallOtherFunctions()
    {
        var result = Compile("function inc(x) -> y\n    y = x + 1\nend\nfunction inc2(x) -> y\n    y = inc(inc(x))\nend\npower_display(inc2(3))");

        Assert.Equal("math[xmo=0](math[xmo=0](3, 1), 1)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void DelayInsideABody_MayReferAheadInTheBody()
    {
        var result = Compile(Latch + "power_display(sr_latch(1, 0).q)");

        var delay = result.Blocks.ToList().FindIndex(b => b.Type == BlockManifest.Delay);
        Assert.Contains(result.Wires, w => w.From.Block == delay && result.Blocks[w.To.Block].Type == BlockManifest.Logic);
    }

    [Fact]
    public void DelayForwardReference_PassesThroughAFunction()
    {
        var result = Compile(Edge + "x = delay(edge(later), ticks: 1)\nlater = player_input().horn\npower_display(x)");

        Assert.Equal(
            "delay[ldl=1](logic[xmo=0](player_input().horn, digitizer[xt1=1](delay[ldl=1](player_input().horn))))",
            BlockTree.RenderLast(result));
    }

    [Fact]
    public void DelayForwardReference_ToItself_PassesThroughAFunction()
    {
        var result = Compile("function same(x) -> y\n    y = x\nend\nheld = delay(same(held) ^ 1, ticks: 1)\npower_display(held)");

        var delay = result.Blocks.ToList().FindIndex(b => b.Type == BlockManifest.Delay);
        var logic = result.Blocks.ToList().FindIndex(b => b.Type == BlockManifest.Logic);
        Assert.Contains(result.Wires, w => w.From.Block == delay && w.To.Block == logic);
    }

    [Fact]
    public void DefaultHide_AppliesToExpandedBlocks()
    {
        var result = Compile("default hide: true\n" + Edge + "power_display(edge(1))");

        Assert.All(result.Blocks, b => Assert.Contains(b.Settings, s => s.Key == BlockSetting.Hide.Key));
    }

    [Fact]
    public void PlacedReference_IsPassedInAndCalledInTheBody()
    {
        var result = Compile(
            "function light(lamp, x)\n    lamp(intensity: x)\nend\nlamp_1 = placed(\"L\")\nlight(lamp_1, 5)",
            Level());

        var wire = Assert.Single(result.PlacedWires);
        Assert.Equal("L", wire.To.Uid);
    }

    [Fact]
    public void PlacedInputWiredByTwoCalls_NamesTheFirstCallsLine()
    {
        var result = Compile(
            "function light(x)\n    lamp = placed(\"L\")\n    lamp(intensity: x)\nend\nlight(1)\nlight(2)",
            Level());

        Assert.Equal(["3:10: Input 'intensity' of 'lamp' is already wired on line 5"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void Placed_IsAllowedInABody()
    {
        var result = Compile(
            "function trigger() -> t\n    t = placed(\"T\")\nend\npower_display(trigger() & trigger())",
            Level());

        Assert.Equal(2, result.PlacedWires.Count);
        Assert.Single(result.PlacedBlockReferences);
    }

    [Fact]
    public void WiresIntoPlacedBlocksInABody_AreCheckedForDependencyLoops()
    {
        var result = Compile(
            "function feed(p, x)\n    p(x)\nend\np = placed(\"P\")\nfeed(p, p & 1)",
            new() { ["P"] = new PlacedBlock(BlockManifest.Passthrough.DefaultStyle.BlockId) });

        var error = Assert.Single(result.Errors);
        Assert.Equal((2, 7), (error.Position.Line, error.Position.Column));
        Assert.StartsWith("This Wire makes a Dependency Loop", error.Message);
    }

    [Fact]
    public void Recursion_IsAnError()
    {
        var result = Compile("function f(x) -> y\n    y = f(x)\nend\npower_display(f(1))");

        Assert.Equal(["2:9: 'f' calls itself, and a function can't"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void RecursionThroughAnotherFunction_IsAnError()
    {
        var result = Compile("function f(x) -> y\n    y = g(x)\nend\nfunction g(x) -> y\n    y = f(x)\nend\npower_display(f(1))");

        Assert.Equal(["5:9: 'f' calls itself through 'g', and a function can't"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void RecursionInAnUncalledFunction_IsAnError()
    {
        var result = Compile("function f(x) -> y\n    y = f(x)\nend");

        Assert.Equal(["2:9: 'f' calls itself, and a function can't"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void MissingOutput_IsAnError()
    {
        var result = Compile("function f(x) -> y, z\n    y = x\nend\npower_display(f(1).y)");

        Assert.Equal(["1:21: 'f' never defines its output 'z'"], result.Errors.Select(e => e.ToString()));
    }

    [Theory]
    [InlineData("power_display(f(1))", "6:15: 'f' needs input 'b'")]
    [InlineData("power_display(f())", "6:15: 'f' needs inputs 'a', 'b'")]
    [InlineData("power_display(f(1, 2, 3))", "6:15: 'f' takes 2 inputs, got 3")]
    [InlineData("power_display(f(1, c: 2))", "6:20: 'f' has no input 'c'. Use a, b")]
    [InlineData("power_display(f(1, a: 2))", "6:20: Input 'a' is already given by position")]
    [InlineData("power_display(f(b: 1, b: 2, a: 3))", "6:23: 'b' is given twice")]
    [InlineData("power_display(f(1, nope))", "6:20: 'nope' isn't defined")]
    [InlineData("power_display(f)", "6:15: 'f' is a function. Call it with ( )")]
    [InlineData("power_display(f(1, 2).x)", "6:23: 'f' has a single output. Use it without .x")]
    [InlineData("g(1)", "6:1: 'g' takes 0 inputs, got 1")]
    [InlineData("x = g()", "6:5: 'g' has no output to use as a value")]
    public void Arguments_Errors(string call, string error)
    {
        var result = Compile("function f(a, b) -> y\n    y = a & b\nend\nfunction g()\nend\n" + call);

        Assert.Equal([error], result.Errors.Select(e => e.ToString()));
    }

    [Theory]
    [InlineData("power_display(f(1, 2).z)", "'f' has no output 'z'. Use y, w")]
    [InlineData("power_display(f(1, 2))", "'f' has several outputs. Pick one, like f().y")]
    public void SeveralOutputs_Errors(string call, string message)
    {
        var result = Compile("function f(a, b) -> y, w\n    y = a & b\n    w = a | b\nend\n" + call);

        Assert.Equal(message, Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void BodyErrors_AreReportedOnceAtTheBody()
    {
        var result = Compile("function f(x) -> y\n    y = x & nope\nend\npower_display(f(1))\npower_display(f(2))");

        Assert.Equal(["2:13: 'nope' isn't defined"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void BodyErrors_InAnUncalledFunction_AreReported()
    {
        var result = Compile("function f(x) -> y\n    y = x & nope\n    z = logic(1, op: maybe)\nend");

        Assert.Equal(
            ["2:13: 'nope' isn't defined", "3:22: 'maybe' isn't an option of op. Use and, or, nand, nor, xor, xnor"],
            result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void UncalledFunction_PlacesNothing()
    {
        var result = Compile("function f(x) -> y\n    y = x & 7\nend\npower_display(1)");

        Assert.Equal(2, result.Blocks.Count);
        Assert.Equal("1", BlockTree.RenderLast(result));
    }

    [Theory]
    [InlineData("function f(x) -> y\n    y = top\nend\ntop = 1\npower_display(f(1))", "2:9: 'top' isn't defined in 'f'. A function only sees its inputs and its own names")]
    [InlineData("function f(x) -> y\n    y = x\n    x = 1\nend\npower_display(f(1))", "3:5: 'x' is an input of 'f' and can't be defined again")]
    [InlineData("function f(x) -> y\n    default hide: true\n    y = x\nend\npower_display(f(1))", "2:5: A default line goes outside functions")]
    [InlineData("function f(x) -> y\n    y = later\n    later = 1\nend\npower_display(f(1))", "2:9: 'later' is defined later, on line 3, so only delay(...) can refer to it")]
    [InlineData("function f(x) -> x\n    y = 1\nend", "1:18: 'x' is an input. Give the output a name of its own")]
    [InlineData("function f(x, x) -> y\n    y = 1\nend", "1:15: 'x' is already an input")]
    [InlineData("function f(x) -> y, y\n    y = 1\nend", "1:21: 'y' is already an output")]
    [InlineData("function logic(x)\nend", "1:10: 'logic' is a Block Function and can't be used as a name")]
    [InlineData("function f(true)\nend", "1:12: 'true' is a reserved word and can't be used as a name")]
    [InlineData("function f()\nend\nfunction f()\nend", "3:10: Function 'f' is already defined on line 1")]
    [InlineData("function f()\nend\nf = 1", "3:1: 'f' is a function and can't be used as a name")]
    [InlineData("function f()\nend\nfunction g(f)\nend", "3:12: 'f' is a function and can't be used as a name")]
    [InlineData("end = 1", "1:1: 'end' is a reserved word and can't be used as a name")]
    [InlineData("function = 1", "1:1: 'function' is a reserved word and can't be used as a name")]
    public void Names_Errors(string text, string error)
    {
        var result = Compile(text);

        Assert.Equal([error], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void DeferredArgumentMember_Error_UsesTheBodysName()
    {
        var result = Compile("function f(a) -> y\n    y = a.horn\nend\nx = delay(f(later), ticks: 1)\nlater = player_input().horn\npower_display(x)");

        Assert.Equal(["2:11: 'a' has a single output. Use it without .horn"], result.Errors.Select(e => e.ToString()));
    }

    [Theory]
    [InlineData("function f(x) -> y\n    y = x\n", "1:1: Function 'f' has no end")]
    [InlineData("function f(x) -> y\n    y = x\nfunction g()\nend", "3:1: Function 'f' has no end before this one. Functions can't be defined inside each other")]
    [InlineData("power_display(1)\nend", "2:1: 'end' has no function to close")]
    [InlineData("function f(x y)\nend", "1:14: Expected ',' or ')', found 'y'")]
    [InlineData("function (x)\nend", "1:10: Expected the function's name, found '('")]
    [InlineData("function f(x) -> \nend", "1:18: Expected an output name, found the end of the line")]
    [InlineData("function f(x) y\nend", "1:15: Expected '->' or the end of the line, found 'y'")]
    [InlineData("function f(x) -> y z\nend", "1:20: Expected ',' or the end of the line, found 'z'")]
    public void Syntax_Errors(string text, string error)
    {
        var result = Compile(text);

        Assert.Equal([error], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void SyntaxErrors_AreRecoveredFrom_SoLaterLinesAreStillChecked()
    {
        var result = Compile("function f(x y)\n    z = nope\nend\npower_display(f(1))\npower_display(missing)");

        Assert.Equal(["1:14: Expected ',' or ')', found 'y'", "5:15: 'missing' isn't defined"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void NestedFunction_IsReadAsTheNextOne()
    {
        var result = Compile("function f()\nfunction g() -> y\n    y = 1\nend\npower_display(g())");

        Assert.Equal(["2:1: Function 'f' has no end before this one. Functions can't be defined inside each other"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void FunctionChain_PastTheDepthLimit_IsAnError()
    {
        var text = string.Concat(Enumerable.Range(0, GraphBuilder.MaxFunctionDepth + 1)
            .Select(i => $"function f{i}(x) -> y\n    y = f{i + 1}(x)\nend\n"));
        text += $"function f{GraphBuilder.MaxFunctionDepth + 1}(x) -> y\n    y = x\nend\npower_display(f0(1))";

        var result = SmallStack.Run(() => Compile(text));

        Assert.Contains($"Functions call each other more than {GraphBuilder.MaxFunctionDepth} deep here", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void ManyExpansions_AreCapped()
    {
        // Each level calls the next four times, so the calls multiply far past the cap
        var text = string.Concat(Enumerable.Range(0, 7)
            .Select(i => $"function f{i}(x) -> y\n    y = f{i + 1}(x) + f{i + 1}(x) + f{i + 1}(x) + f{i + 1}(x)\nend\n"));
        text += "function f7(x) -> y\n    y = x\nend\npower_display(f0(1))";

        var result = Compile(text);

        Assert.All(result.Errors, e => Assert.Contains($"more than {GraphBuilder.MaxExpansions} times", e.Message));
        Assert.NotEmpty(result.Errors);
    }

    private static Dictionary<string, PlacedBlock> Level()
    {
        return new()
        {
            ["T"] = new PlacedBlock(BlockManifest.Trigger.DefaultStyle.BlockId),
            ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId)
        };
    }

    private static CompileResult Compile(string text, Dictionary<string, PlacedBlock> level = null)
    {
        return ScriptCompiler.Compile(text, 0, [], uid => level?.GetValueOrDefault(uid));
    }
}
