using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockReferenceTests
{
    [Fact]
    public void EveryManifestType_HasACategory()
    {
        foreach (var type in BlockManifest.All)
            Assert.True(BlockCategory.All.Contains(type.Category), type.Function);
    }

    [Fact]
    public void Sections_ListEveryManifestTypeOnce()
    {
        var functions = BlockReference.Sections.SelectMany(s => s.Entries).Select(e => e.Function);

        Assert.Equal(BlockManifest.All.Select(t => t.Function).OrderBy(f => f), functions.OrderBy(f => f));
    }

    [Fact]
    public void Sections_FollowTheCategoryOrder()
    {
        Assert.Equal(BlockCategory.All, BlockReference.Sections.Select(s => s.Category));
    }

    [Fact]
    public void Section_SortsItsEntriesByName()
    {
        foreach (var section in BlockReference.Sections)
        {
            var functions = section.Entries.Select(e => e.Function).ToList();
            Assert.Equal(functions.OrderBy(f => f, StringComparer.Ordinal), functions);
        }
    }

    [Theory]
    [InlineData("generator", "Inputs")]
    [InlineData("trigger", "Inputs")]
    [InlineData("logic", "Operators")]
    [InlineData("delay", "Operators")]
    [InlineData("lamp", "Outputs")]
    [InlineData("toggler", "Outputs")]
    [InlineData("dynamic_mover", "Movers")]
    public void Entry_IsInTheBlockBrowsersFolder(string function, string category)
    {
        var section = BlockReference.Sections.Single(s => s.Entries.Any(e => e.Function == function));

        Assert.Equal(category, section.Category.Name);
    }

    [Fact]
    public void Entry_ShowsInputsOutputsAndOptions()
    {
        var logic = Find("logic");

        Assert.Equal("a, b, op_in", logic.Inputs);
        Assert.Equal("out", logic.Outputs);
        Assert.Empty(logic.ModeOutputs);
        Assert.Equal([("op", "and, or, nand, nor, xor, xnor"), ("hide", "true, false")], Rows(logic.Settings));
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
        Assert.Equal(("ticks", "number, at least 1 (required)"), Rows(Find("delay").Settings)[0]);
    }

    [Fact]
    public void Entry_ShowsStyles()
    {
        Assert.Contains(("style", "normal, compact"), Rows(Find("power_display").Settings));
    }

    [Fact]
    public void Entry_ShowsPlayersAndTargets()
    {
        Assert.Equal(("player", "all, 1, 2, 3, 4"), Rows(Find("death").Settings)[0]);
        Assert.Equal(("targets", "1–8 placed blocks"), Rows(Find("toggler").Settings)[0]);
    }

    [Fact]
    public void Entry_WithModes_ShowsOutputsPerMode()
    {
        var telemetry = Find("simple_telemetry");

        Assert.Equal(
            [("basic", "speed, yaw, up, state"), ("local_velocity", "yaw, x, y, z"), ("global_velocity", "x, y, z, heading")],
            Rows(telemetry.ModeOutputs));
        Assert.Null(telemetry.Outputs);
    }

    [Fact]
    public void PlacedOnlyEntry_HasNoHide()
    {
        var lamp = Find("lamp");

        Assert.True(lamp.IsPlacedOnly);
        Assert.DoesNotContain(lamp.Settings, r => r.Name == "hide");
    }

    [Theory]
    [InlineData("", "logic", true)]
    [InlineData("  ", "logic", true)]
    [InlineData("LOG", "logic", true)]
    [InlineData("mux", "multiplexer", true)]
    [InlineData("xnor", "logic", true)]
    [InlineData("heading", "simple_telemetry", true)]
    [InlineData("op_in", "math", true)]
    [InlineData("ticks", "delay", true)]
    [InlineData("local_velocity", "simple_telemetry", true)]
    [InlineData("xnor", "math", false)]
    public void Matches_LooksAtNameAliasesConnectorsAndSettings(string filter, string function, bool expected)
    {
        Assert.Equal(expected, Find(function).Matches(filter));
    }

    private static List<(string, string)> Rows(IReadOnlyList<BlockReferenceRow> rows)
    {
        return rows.Select(r => (r.Name, r.Value)).ToList();
    }

    private static BlockReferenceEntry Find(string function)
    {
        return BlockReference.Sections.SelectMany(s => s.Entries).Single(e => e.Function == function);
    }
}
