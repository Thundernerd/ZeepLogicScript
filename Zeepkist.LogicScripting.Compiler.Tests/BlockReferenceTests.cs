using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockReferenceTests
{
    [Fact]
    public void Entries_ListEveryManifestType_InManifestOrder()
    {
        var functions = BlockReference.Entries.Select(e => e.Function).ToList();

        Assert.Equal(BlockManifest.All.Select(t => t.Function), functions);
    }

    [Fact]
    public void Entry_ShowsInputsOutputsAndOptions()
    {
        var logic = Find("logic");

        Assert.Equal("a, b, op_in", logic.Inputs);
        Assert.Equal("out", logic.Outputs);
        Assert.Equal("op: and, or, nand, nor, xor, xnor; hide: true, false", logic.Settings);
    }

    [Fact]
    public void Entry_WithoutConnectors_ShowsADash()
    {
        var ticker = Find("ticker");

        Assert.Equal("–", ticker.Inputs);
        Assert.Equal("–", Find("toggler").Outputs);
    }

    [Fact]
    public void Entry_ShowsAliases()
    {
        Assert.Equal("mux", Find("multiplexer").Aliases);
        Assert.Equal("door", Find("toggler").Aliases);
        Assert.Equal(string.Empty, Find("logic").Aliases);
    }

    [Fact]
    public void Entry_MarksRequiredSettings_AndShowsLimits()
    {
        Assert.StartsWith("ticks: number, at least 1 (required)", Find("delay").Settings);
    }

    [Fact]
    public void Entry_ShowsStyles()
    {
        Assert.Contains("style: normal, compact", Find("power_display").Settings);
    }

    [Fact]
    public void Entry_ShowsPlayersAndTargets()
    {
        Assert.StartsWith("player: all, 1, 2, 3, 4", Find("death").Settings);
        Assert.StartsWith("targets: 1–8 placed blocks", Find("toggler").Settings);
    }

    [Fact]
    public void Entry_WithModes_ShowsOutputsPerMode()
    {
        var telemetry = Find("simple_telemetry");

        Assert.Equal(
            "basic: speed, yaw, up, state; local_velocity: yaw, x, y, z; global_velocity: x, y, z, heading",
            telemetry.Outputs);
    }

    [Fact]
    public void PlacedOnlyEntry_HasNoHide()
    {
        var lamp = Find("lamp");

        Assert.True(lamp.IsPlacedOnly);
        Assert.DoesNotContain("hide", lamp.Settings);
    }

    [Theory]
    [InlineData("", "logic", true)]
    [InlineData("  ", "logic", true)]
    [InlineData("LOG", "logic", true)]
    [InlineData("mux", "multiplexer", true)]
    [InlineData("xnor", "logic", true)]
    [InlineData("heading", "simple_telemetry", true)]
    [InlineData("op_in", "math", true)]
    [InlineData("xnor", "math", false)]
    public void Matches_LooksAtNameAliasesConnectorsAndSettings(string filter, string function, bool expected)
    {
        Assert.Equal(expected, Find(function).Matches(filter));
    }

    private static BlockReferenceEntry Find(string function)
    {
        return BlockReference.Entries.Single(e => e.Function == function);
    }
}
