using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class LibraryTests
{
    private const string Timing = "function edge(x) -> rising\n    prev = delay(x, ticks: 1)\n    rising = x & !prev\nend\n";

    private const string Latches = "function sr(set, reset) -> q, not_q\n    q = delay(!reset & (set | q), ticks: 1)\n    not_q = !q\nend\n";

    [Fact]
    public void QualifiedCall_ExpandsLikeALocalFunction()
    {
        var qualified = Compile("p = player_input()\npower_display(timing.edge(p.horn))", new() { ["timing"] = Timing });
        var local = Compile(Timing + "p = player_input()\npower_display(edge(p.horn))");

        Assert.True(qualified.Succeeded, string.Join("\n", qualified.Errors));
        Assert.Equal(BlockTree.RenderLast(local), BlockTree.RenderLast(qualified));
        Assert.Equal(local.Blocks.Select(b => b.Type), qualified.Blocks.Select(b => b.Type));
    }

    [Fact]
    public void QualifiedCall_PicksAnOutputWithADot()
    {
        var qualified = Compile("power_display(latches.sr(1, 0).not_q)", new() { ["latches"] = Latches });
        var local = Compile(Latches + "power_display(sr(1, 0).not_q)");

        Assert.True(qualified.Succeeded, string.Join("\n", qualified.Errors));
        Assert.Equal(local.Blocks.Select(b => b.Type), qualified.Blocks.Select(b => b.Type));
        Assert.Equal(local.Wires.Select(Describe), qualified.Wires.Select(Describe));
    }

    [Fact]
    public void QualifiedCall_StandsOnItsOwnLine()
    {
        var result = Compile("show.it(3)", new() { ["show"] = "function it(x)\n    power_display(x)\nend" });

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal("3", BlockTree.RenderLast(result));
    }

    [Fact]
    public void EachLibrary_IsAskedForOnce()
    {
        List<string> asked = [];
        var result = ScriptCompiler.Compile(
            "power_display(timing.edge(1))\npower_display(timing.edge(2))\npower_display(nope.f(3))\npower_display(nope.f(4))",
            0,
            findLibrary: name =>
            {
                asked.Add(name);
                return name == "timing" ? Timing : null;
            });

        Assert.Equal(["timing", "nope"], asked);
        Assert.Equal(2, result.Errors.Count);
    }

    [Theory]
    [InlineData("power_display(timeing.edge(1))", "1:15: No Library 'timeing' on this machine")]
    [InlineData("power_display(timing.edgee(1))", "1:15: 'timing' has no function 'edgee'")]
    [InlineData("timing.nope(1)", "1:1: 'timing' has no function 'nope'")]
    [InlineData("power_display(timing.edge())", "1:15: 'timing.edge' needs input 'x'")]
    [InlineData("power_display(timing.edge(1, 2))", "1:15: 'timing.edge' takes 1 input, got 2")]
    [InlineData("power_display(timing.edge(1).r)", "1:30: 'timing.edge' has a single output. Use it without .r")]
    [InlineData("power_display(timing .edge(1))", "1:22: Write the dot right after 'timing', without a space")]
    [InlineData("power_display(timing. edge(1))", "1:21: Write 'edge' right after the dot, without a space")]
    public void MissingLibraryOrFunction_AndArgumentErrors(string text, string error)
    {
        var result = Compile(text, new() { ["timing"] = Timing });

        Assert.Equal([error], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void MissingLibrary_StillChecksTheArguments()
    {
        var result = Compile("power_display(timing.edge(nope))");

        Assert.Equal(["1:15: No Library 'timing' on this machine", "1:27: 'nope' isn't defined"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void Library_CallsItsOwnFunctionsWithoutAPrefix()
    {
        var library = "function inc(x) -> y\n    y = x + 1\nend\nfunction inc2(x) -> y\n    y = inc(inc(x))\nend";

        var result = Compile("power_display(math_lib.inc2(3))", new() { ["math_lib"] = library });

        Assert.Equal("math[xmo=0](math[xmo=0](3, 1), 1)", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Library_CallsAnotherWithItsPrefix()
    {
        var buttons = "function press(x) -> y\n    y = timing.edge(x)\nend";

        var result = Compile("p = player_input()\npower_display(buttons.press(p.horn))", new() { ["timing"] = Timing, ["buttons"] = buttons });

        Assert.Equal("logic[xmo=0](player_input().horn, digitizer[xt1=1](delay[ldl=1](player_input().horn)))", BlockTree.RenderLast(result));
    }

    [Fact]
    public void Library_DoesntSeeTheScriptsFunctions()
    {
        var result = Compile(
            "function edge(x) -> y\n    y = x\nend\npower_display(buttons.press(1))",
            new() { ["buttons"] = "function press(x) -> y\n    y = edge(x)\nend" });

        Assert.Equal(["4:15: In buttons.press, line 2: 'edge' isn't a Block Function"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void RecursionAcrossLibraries_IsAnError()
    {
        var libraries = new Dictionary<string, string>
        {
            ["a"] = "function f(x) -> y\n    y = b.g(x)\nend",
            ["b"] = "function g(x) -> y\n    y = a.f(x)\nend"
        };

        var result = Compile("power_display(a.f(1))", libraries);

        Assert.Equal(["1:15: In b.g, line 2: 'a.f' calls itself through 'b.g', and a function can't"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void Placed_IsAnErrorInALibrary()
    {
        var result = Compile(
            "power_display(lamps.t())",
            new() { ["lamps"] = "function t() -> y\n    y = placed(\"T\")\nend" },
            new() { ["T"] = new PlacedBlock(BlockManifest.Trigger.DefaultStyle.BlockId) });

        Assert.Equal(
            ["1:15: In lamps.t, line 2: A Library can't use placed(...), since a UID belongs to one level. Pass the block in as an input"],
            result.Errors.Select(e => e.ToString()));
        Assert.Empty(result.PlacedBlockReferences);
    }

    [Fact]
    public void PlacedReference_IsPassedIntoALibrary()
    {
        var result = Compile(
            "lamp_1 = placed(\"L\")\nlamps.light(lamp_1, 5)",
            new() { ["lamps"] = "function light(lamp, x)\n    lamp(intensity: x)\nend" },
            new() { ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId) });

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal("L", Assert.Single(result.PlacedWires).To.Uid);
    }

    [Fact]
    public void ErrorThatDependsOnTheCall_IsReportedAtTheCallSite()
    {
        var result = Compile(
            "p = player_input()\nx = 1\npower_display(pick.horn(x))\npower_display(pick.horn(p))",
            new() { ["pick"] = "# Reads the horn\nfunction horn(p) -> h\n    h = p.horn\nend" });

        Assert.Equal(["3:15: In pick.horn, line 3: 'p' has a single output. Use it without .horn"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void ErrorInANestedLibrary_IsReportedAtTheOutermostCall()
    {
        var libraries = new Dictionary<string, string>
        {
            ["outer"] = "function f(x) -> y\n    y = inner.g(x)\nend",
            ["inner"] = "\nfunction g(x) -> y\n    y = x & nope\nend"
        };

        var result = Compile("\npower_display(outer.f(1))", libraries);

        Assert.Equal(["2:15: In inner.g, line 3: 'nope' isn't defined"], result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void ErrorInALibrary_KeepsWhereItIsApartFromTheDetail()
    {
        var result = Compile(
            "power_display(lamps.t(nope))",
            new() { ["lamps"] = "function t(x) -> y\n    y = x & missing\nend" });

        var errors = result.Errors.Select(e => (e.Origin, e.Detail)).ToList();
        Assert.Equal([("In lamps.t, line 2", "'missing' isn't defined"), (null, "'nope' isn't defined")], errors);
    }

    [Fact]
    public void DependencyLoopThroughALibrary_IsReportedAtTheCallSite()
    {
        var result = Compile(
            "p = placed(\"P\")\nwire.feed(p, p & 1)",
            new() { ["wire"] = "function feed(p, x)\n    p(x)\nend" },
            new() { ["P"] = new PlacedBlock(BlockManifest.Passthrough.DefaultStyle.BlockId) });

        var error = Assert.Single(result.Errors);
        Assert.Equal((2, 1), (error.Position.Line, error.Position.Column));
        Assert.StartsWith("In wire.feed, line 2: This Wire makes a Dependency Loop", error.Message);
    }

    [Fact]
    public void BrokenLibrary_ReportsItsErrorsAtTheCall()
    {
        var result = Compile("power_display(timing.edge(1))", new() { ["timing"] = Timing + "x = 1\nfunction f(a b)\nend" });

        Assert.Equal(
            [
                "1:15: Library 'timing' has an error on line 5: A Library holds only functions. Move this line into one",
                "1:15: Library 'timing' has an error on line 6: Expected ',' or ')', found 'b'"
            ],
            result.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void CheckLibrary_FindsNothingInAGoodLibrary()
    {
        Assert.Empty(ScriptCompiler.CheckLibrary("# Edges\n" + Timing + Latches));
    }

    [Fact]
    public void CheckLibrary_InputsAreAcceptedAnywhere()
    {
        var text = "function f(a, b) -> y, z\n    y = a.horn & b\n    z = delay(a, ticks: 1)\n    lamp = b\nend";

        Assert.Empty(ScriptCompiler.CheckLibrary(text));
    }

    [Theory]
    [InlineData("function f(n) -> y\n    y = delay(1, ticks: n)\nend")]
    [InlineData("function f(x, b) -> y\n    y = digitizer(x, invert: b)\nend")]
    [InlineData("function f(x, s) -> y\n    y = passthrough(x, style: s)\nend")]
    [InlineData("function f(x, n) -> y\n    y = inner(x, n)\nend\nfunction inner(x, n) -> y\n    y = delay(x, ticks: n)\nend")]
    [InlineData("function f(x, h) -> y\n    y = passthrough(x, hide: h)\nend")]
    [InlineData("function f(x, n) -> y\n    y = timing.pulse_after(x, n)\nend")]
    public void CheckLibrary_AnInputUsedAsASetting_IsAccepted(string text)
    {
        const string timing = "function pulse_after(x, ticks) -> out\n    out = pulser(delay(x, ticks: ticks))\nend";

        Assert.Empty(ScriptCompiler.CheckLibrary(text, name => name == "timing" ? timing : null));
    }

    [Theory]
    [InlineData("x = 1", "1:1: A Library holds only functions. Move this line into one")]
    [InlineData("power_display(1)", "1:1: A Library holds only functions. Move this line into one")]
    [InlineData("default hide: true", "1:1: A Library holds only functions. Move this line into one")]
    [InlineData("function f(x) -> y\n    y = x & nope\nend", "2:13: 'nope' isn't defined")]
    [InlineData("function f() -> y\n    y = placed(\"A\")\nend", "2:9: A Library can't use placed(...), since a UID belongs to one level. Pass the block in as an input")]
    [InlineData("function f(x) -> y\n    y = f(x)\nend", "2:9: 'f' calls itself, and a function can't")]
    [InlineData("function f(x) -> y\n    y = other.g(x)\nend", "2:9: No Library 'other' on this machine")]
    [InlineData("function f(x) -> y\n    y = timing.edgee(x)\nend", "2:9: 'timing' has no function 'edgee'")]
    [InlineData("function f(x) -> y\nend", "1:18: 'f' never defines its output 'y'")]
    public void CheckLibrary_Findings(string text, string error)
    {
        var diagnostics = ScriptCompiler.CheckLibrary(text, name => name == "timing" ? Timing : null);

        Assert.Equal([error], diagnostics.Select(e => e.ToString()));
    }

    [Fact]
    public void CheckLibrary_ReportsAnotherLibrarysErrorsAtTheCall()
    {
        var diagnostics = ScriptCompiler.CheckLibrary(
            "function f(x) -> y\n    y = inner.g(1) & x\nend",
            _ => "function g(x) -> y\n    y = x.horn\nend");

        Assert.Equal(["2:9: In inner.g, line 2: 'x' has a single output. Use it without .horn"], diagnostics.Select(e => e.ToString()));
    }

    [Fact]
    public void CheckLibrary_QualifiedCallIntoItself_ReadsTheTextBeingChecked()
    {
        var text = "function f(x) -> y\n    y = me.g(1) & x\nend\nfunction g(x) -> y\n    y = x\nend";

        Assert.Empty(ScriptCompiler.CheckLibrary(text, _ => "function old()\nend", "me"));
    }

    [Theory]
    [InlineData("timing")]
    [InlineData("_t2")]
    [InlineData("Latches")]
    public void LibraryName_AnIdentifier_IsFine(string name)
    {
        Assert.Null(LibraryName.GetError(name, ["other"]));
    }

    [Theory]
    [InlineData("", "Give the Library a name")]
    [InlineData("2timing", "Use only letters, digits and _, and start with a letter or _")]
    [InlineData("my timing", "Use only letters, digits and _, and start with a letter or _")]
    [InlineData("tímíng", "Use only letters, digits and _, and start with a letter or _")]
    [InlineData("logic", "'logic' is a Block Function")]
    [InlineData("mux", "'mux' is a Block Function")]
    [InlineData("placed", "'placed' is a reserved word")]
    [InlineData("end", "'end' is a reserved word")]
    [InlineData("TIMING", "Another Library is already called \"TIMING\"")]
    public void LibraryName_Errors(string name, string error)
    {
        Assert.Equal(error, LibraryName.GetError(name, ["timing"]));
    }

    [Fact]
    public void LibraryName_PastTheMaxLength_IsAnError()
    {
        Assert.NotNull(LibraryName.GetError(new string('a', LibraryName.MaxLength + 1), []));
        Assert.Null(LibraryName.GetError(new string('a', LibraryName.MaxLength), []));
    }

    private static string Describe(CompiledWire wire)
    {
        return $"{wire.From}:{wire.FromConnector} -> {wire.To}:{wire.ToConnector}";
    }

    private static CompileResult Compile(
        string text,
        Dictionary<string, string> libraries = null,
        Dictionary<string, PlacedBlock> level = null)
    {
        return ScriptCompiler.Compile(text, 0, [], uid => level?.GetValueOrDefault(uid), name => libraries?.GetValueOrDefault(name));
    }
}
