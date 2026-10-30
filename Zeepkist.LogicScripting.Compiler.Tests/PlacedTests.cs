using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class PlacedTests
{
    private const int DoorBlockId = 1;

    private static readonly Dictionary<string, PlacedBlock> _level = new()
    {
        ["T"] = new PlacedBlock(BlockManifest.Trigger.DefaultStyle.BlockId),
        ["L"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId),
        ["L2"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId),
        ["WIRED"] = new PlacedBlock(BlockManifest.Lamp.DefaultStyle.BlockId, wiredInputs: [0]),
        ["D"] = new PlacedBlock(BlockManifest.DistanceSensor.DefaultStyle.BlockId),
        ["P"] = new PlacedBlock(BlockManifest.Passthrough.DefaultStyle.BlockId),
        ["Q"] = new PlacedBlock(BlockManifest.Passthrough.DefaultStyle.BlockId, downstream: ["P"]),
        ["DELAY"] = new PlacedBlock(BlockManifest.Delay.DefaultStyle.BlockId),
        ["S"] = new PlacedBlock(
            BlockManifest.SimpleTelemetry.DefaultStyle.BlockId,
            [BlockManifest.SimpleTelemetryMode.Choose("local_velocity")]),
        ["DOOR"] = new PlacedBlock(DoorBlockId),
        ["DOOR2"] = new PlacedBlock(DoorBlockId),
        ["GROUP"] = new PlacedBlock(BlockManifest.Passthrough.DefaultStyle.BlockId, isInBlockGroup: true)
    };

    [Fact]
    public void CallingAReference_WiresIntoItsInputs()
    {
        var result = Compile("t = placed(\"T\")\nlamp_1 = placed(\"L\")\nlamp_1(intensity: t)\nlamp_1(red: 1, green: 2)");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(["1", "2"], result.Blocks.Select(b => b.Settings[0].TextValue));
        Assert.Equal(["T.0 -> L.0", "0.0 -> L.1", "1.0 -> L.2"], result.PlacedWires.Select(Describe));
    }

    [Fact]
    public void APlacedOutput_FeedsNewBlocks()
    {
        var result = Compile("t = placed(\"T\")\nd = placed(\"D\")\npower_display(t & d.x)");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal([BlockManifest.Logic, BlockManifest.PowerDisplay], result.Blocks.Select(b => b.Type));
        Assert.Equal(["T.0 -> 0.0", "D.1 -> 0.1"], result.PlacedWires.Select(Describe));
        Assert.Equal(["0.3 -> 1.0"], result.Wires.Select(w => $"{w.From}.{w.FromConnector} -> {w.To}.{w.ToConnector}"));
    }

    [Fact]
    public void APlacedBlocksMode_NamesItsOutputs()
    {
        var result = Compile("s = placed(\"S\")\npower_display(s.x)\npower_display(s.speed)");

        Assert.Equal(["3:17 'simple_telemetry' has no output 'speed' in mode local_velocity. Use yaw, x, y, z"], Errors(result));
    }

    [Fact]
    public void References_AreListed_WithTheirNameAndType()
    {
        var result = Compile("lamp_1 = placed(\"L\")\ntoggler(1, targets: placed(\"DOOR\"))\nx = placed(\"NOPE\")");

        Assert.Equal(
            ["1:10 lamp_1 L lamp", "2:21 - DOOR block", "3:5 x NOPE -"],
            result.PlacedBlockReferences.Select(r => $"{r.Position} {r.Name ?? "-"} {r.Uid} {r.Type?.Function ?? "-"}"));
    }

    [Fact]
    public void ABlockGroupBlock_CantBeReferenced()
    {
        var result = Compile("p = placed(\"GROUP\")\npower_display(p)");

        Assert.Equal(
            ["1:12 Block \"GROUP\" is in a Block Group, which a recompile replaces. Refer to a hand-placed block, such as a Passthrough between two Scripts"],
            Errors(result));
    }

    [Fact]
    public void ReferencesToOneUid_ShareOneBlock()
    {
        var result = Compile("a = placed(\"L\")\nb = placed(\"L\")\na(1)\nb(intensity: 2)");

        Assert.Equal(["4:3 Input 'intensity' of 'b' is already wired on line 3"], Errors(result));
    }

    [Theory]
    [InlineData("x = placed(\"NOPE\")", "1:12 No block in this level has UID \"NOPE\"")]
    [InlineData("x = placed(1)", "1:5 placed(...) takes one UID in quotes, like placed(\"AbC12\")")]
    [InlineData("x = placed(\"L\", \"T\")", "1:5 placed(...) takes one UID in quotes, like placed(\"AbC12\")")]
    [InlineData("x = placed(uid: \"L\")", "1:5 placed(...) takes one UID in quotes, like placed(\"AbC12\")")]
    [InlineData("x = placed", "1:5 placed takes a UID. Call it like placed(\"AbC12\")")]
    [InlineData("placed = 1", "1:1 'placed' is a reserved word and can't be used as a name")]
    [InlineData("placed(\"L\")", "1:1 A value on its own line does nothing. Give it a name or pass it to a call")]
    [InlineData("x = \"L\"", "1:5 Text in quotes is only for the UID in placed(...)")]
    [InlineData("x = [1, 2]", "1:5 A list in [ ] is only for a Toggler's targets:")]
    public void BadReferences_AreErrors(string script, string error)
    {
        Assert.Equal([error], Errors(Compile(script)));
    }

    [Fact]
    public void UnclosedQuotes_EndAtTheLineEnd()
    {
        var result = Compile("x = placed(\"L\nl = placed(\"L\")\nl(1)");

        Assert.Equal(["1:11 '(' isn't closed", "1:12 Text in quotes isn't closed on its line"], Errors(result));
    }

    [Theory]
    [InlineData("l = placed(\"L\")\nl(hide: true)", "2:3 A Script never changes a placed block's Settings. Set hide in the editor")]
    [InlineData("p = placed(\"P\")\np(1, style: skinny_1h)", "2:6 A Script never changes a placed block's Settings. Set style in the editor")]
    [InlineData("l = placed(\"L\")\nl(brightness: 1)", "2:3 'l' has no input 'brightness'. Use intensity, red, green, blue, range, angle")]
    [InlineData("l = placed(\"L\")\nl(1, 2, 3, 4, 5, 6, 7)", "2:1 'l' takes at most 6 inputs, got 7")]
    [InlineData("l = placed(\"L\")\nl(1)\nl(2)", "3:3 Input 'intensity' of 'l' is already wired on line 2")]
    [InlineData("l = placed(\"WIRED\")\nl(1)", "2:3 Input 'intensity' of 'l' already has a Wire in the level, and an input takes one Wire")]
    [InlineData("d = placed(\"DOOR\")\nd(1)", "2:1 'd' has no inputs to wire")]
    [InlineData("t = placed(\"T\")\nt(1)", "2:1 't' has no inputs to wire")]
    [InlineData("d = placed(\"DOOR\")\nd(open: yes)", "2:1 'd' has no inputs to wire")]
    [InlineData("l = placed(\"L\")\nx = l(1)", "2:5 Wiring into 'l' gives no value. Put it on its own line")]
    [InlineData("x = 1 & 1\nx(1)", "2:1 'x' isn't a placed block, so it can't be called. Only names defined with placed(...) can")]
    [InlineData("l(1)\nl = placed(\"L\")", "1:1 'l' is defined later, on line 2")]
    [InlineData("l = placed(\"L\")\npower_display(l)", "2:15 'l' has no output to use as a value")]
    [InlineData("l = placed(\"L\")\npower_display(l.x)", "2:17 'l' has no outputs")]
    [InlineData("d = placed(\"D\")\npower_display(d)", "2:15 'd' has several outputs. Pick one, like d.distance")]
    public void BadReferenceCalls_AreErrors(string script, string error)
    {
        Assert.Equal([error], Errors(Compile(script)));
    }

    [Fact]
    public void ANameDefinedLater_CantBeWiredIntoAPlacedBlock()
    {
        var result = Compile("l = placed(\"L\")\ndelay(l(y), ticks: 1)\ny = 1");

        Assert.Equal(
            [
                "2:7 Wiring into 'l' gives no value. Put it on its own line",
                "2:9 A name defined later can't be wired into a placed block, since only delay(...) can refer to it"
            ],
            Errors(result));
    }

    [Fact]
    public void ACallToABlockWithNoInputs_StillChecksItsNamedValues()
    {
        var result = Compile("d = placed(\"DOOR\")\nd(open: nope & 1)");

        Assert.Equal(["2:1 'd' has no inputs to wire", "2:9 'nope' isn't defined"], Errors(result));
    }

    [Theory]
    [InlineData("l = placed(\"L\")", "1:5 'lamp' doesn't match this game version and is turned off")]
    [InlineData("toggler(1, targets: placed(\"L\"))", "1:21 'lamp' doesn't match this game version and is turned off")]
    public void AReferenceToATurnedOffBlock_IsAnError(string script, string error)
    {
        var result = ScriptCompiler.Compile(
            script,
            0,
            [BlockManifest.Lamp.DefaultStyle.BlockId],
            uid => _level.GetValueOrDefault(uid));

        Assert.Equal([error], Errors(result));
    }

    [Fact]
    public void AFailedReference_IsReportedOnce()
    {
        var result = Compile("l = placed(\"NOPE\")\nl(1)\npower_display(l)");

        Assert.Equal(["1:12 No block in this level has UID \"NOPE\""], Errors(result));
    }

    [Theory]
    [InlineData("p = placed(\"P\")\np(p)", "2:3")]
    [InlineData("p = placed(\"P\")\nx = p & 1\np(x)", "3:3")]
    [InlineData("p = placed(\"P\")\nq = placed(\"Q\")\nq(p)", "3:3")]
    public void WiresIntoPlacedBlocks_CantCloseADependencyLoop(string script, string position)
    {
        Assert.Equal(
            [$"{position} This Wire makes a Dependency Loop, which stops all logic in play. Put a delay(...) in the loop"],
            Errors(Compile(script)));
    }

    [Theory]
    [InlineData("p = placed(\"P\")\np(delay(p, ticks: 1))")]
    [InlineData("p = placed(\"P\")\nq = placed(\"Q\")\np(q)")]
    [InlineData("p = placed(\"P\")\nd = placed(\"DELAY\")\nd(p)\np(d)")]
    public void LoopsThroughADelay_AreFine(string script)
    {
        var result = Compile(script);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
    }

    [Theory]
    [InlineData("toggler(1, targets: [d1, d2])", "xuid1=DOOR xuid2=L")]
    [InlineData("toggler(1, targets: d1)", "xuid1=DOOR")]
    [InlineData("toggler(1, targets: [placed(\"DOOR2\"), t])", "xuid1=DOOR2 xuid2=T")]
    public void TogglerTargets_AreStoredByUid(string call, string settings)
    {
        var result = Compile($"d1 = placed(\"DOOR\")\nd2 = placed(\"L\")\nt = placed(\"T\")\n{call}");

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        var toggler = Assert.Single(result.Blocks, b => b.Type == BlockManifest.Toggler);
        Assert.Equal(settings, string.Join(" ", toggler.Settings.Select(s => $"{s.Key}={s.TextValue}")));
    }

    [Theory]
    [InlineData("toggler(1, targets: [])", "2:21 targets takes 1 to 8 placed blocks, like targets: [door_1, door_2]")]
    [InlineData("toggler(1, targets: [d, d])", "2:25 'd' is already a target")]
    [InlineData("toggler(1, targets: [d, placed(\"DOOR\")])", "2:25 'placed' is already a target")]
    [InlineData("toggler(1, targets: [d, 1 & 1])", "2:27 'value' isn't a placed block, and targets takes only names defined with placed(...)")]
    [InlineData("toggler(1, targets: [d, nope])", "2:25 'nope' isn't defined")]
    [InlineData("toggler(1, targets: [d, d, d, d, d, d, d, d, d])", "2:46 A Toggler switches at most 8 blocks")]
    [InlineData("toggler(1)", "2:1 'toggler' needs targets:, 1 to 8 placed blocks to switch, like targets: [door_1, door_2]")]
    public void BadTogglerTargets_AreErrors(string call, string error)
    {
        Assert.Contains(error, Errors(Compile($"d = placed(\"DOOR\")\n{call}")));
    }

    [Fact]
    public void MoreThanEightTargets_IsAnError()
    {
        var uids = Enumerable.Range(1, 9).Select(i => $"placed(\"X{i}\")");
        var result = ScriptCompiler.Compile(
            $"toggler(1, targets: [{string.Join(", ", uids)}])",
            0,
            findPlacedBlock: _ => new PlacedBlock(DoorBlockId));

        Assert.Equal(["1:134 A Toggler switches at most 8 blocks"], Errors(result));
    }

    private static CompileResult Compile(string script)
    {
        return ScriptCompiler.Compile(script, 0, findPlacedBlock: uid => _level.GetValueOrDefault(uid));
    }

    private static IEnumerable<string> Errors(CompileResult result)
    {
        return result.Errors.Select(e => $"{e.Position} {e.Message}");
    }

    private static string Describe(CompiledWire wire)
    {
        return $"{wire.From}.{wire.FromConnector} -> {wire.To}.{wire.ToConnector}";
    }
}
