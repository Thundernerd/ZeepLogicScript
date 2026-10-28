using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockLayoutTests
{
    [Fact]
    public void Columns_FollowTheLongestPathWithAGapCellBetween()
    {
        // Generator (1 cell), Logic (3 cells), Power Display (2 cells): 8 cells deep, centered on cell 3
        var result = ScriptCompiler.Compile("power_display(1 & 1)", 0);

        Assert.Equal(["(0, -3)", "(0, -1)", "(0, 3)"], result.Blocks.Select(b => b.Cell.ToString()));
    }

    [Fact]
    public void Rows_FollowLineOrderWithAGapCellBetween()
    {
        // Two Logic blocks in column 1 and two Generators in column 0: three cells wide, centered on cell 1
        var result = ScriptCompiler.Compile("a = 1 & 1\nb = 2 & 2", 0);

        Assert.Equal(
            ["(-1, -2)", "(-1, 0)", "(1, -2)", "(1, 0)"],
            result.Blocks.Select(b => b.Cell.ToString()));
    }

    [Fact]
    public void LongestPath_PushesAShortcutInputToTheLaterColumn()
    {
        var result = ScriptCompiler.Compile("a = 1 & 1\npower_display(a & 1)", 0);

        // Generator, first Logic, second Logic (fed by both), Power Display
        var depths = result.Blocks.Select(b => b.Cell.Z).ToArray();
        Assert.True(depths[0] < depths[1]);
        Assert.True(depths[1] < depths[2]);
        Assert.True(depths[2] < depths[3]);
    }

    [Fact]
    public void Barycenter_MovesABlockLevelWithWhatFeedsIt()
    {
        // Line order would put b, fed by the second ticker, level with the first one: their Wires would cross
        var result = ScriptCompiler.Compile("x = ticker()\ny = ticker()\nb = digitizer(y)\na = digitizer(x)", 0);

        var xs = result.Blocks.Select(b => b.Cell.X).ToArray();
        Assert.Equal((xs[0], xs[1]), (xs[3], xs[2]));
    }

    [Fact]
    public void Columns_AreCenteredAcrossTheGroup()
    {
        var result = ScriptCompiler.Compile("a = ticker()\nb = ticker()\nc = ticker()\nlogic(a, b, c)", 0);

        Assert.Equal([-2, 0, 2, 0], result.Blocks.Select(b => b.Cell.X));
    }

    [Fact]
    public void WideBlocks_TakeTheirFullCells()
    {
        // Player Input reaches one cell toward -X from its pivot, Date Timer one toward +X: seven cells wide with the
        // Ticker and the gaps, centered on cell 3
        var result = ScriptCompiler.Compile("player_input()\ndate_timer()\nticker()", 0);

        Assert.Equal([-2, 0, 3], result.Blocks.Select(b => b.Cell.X));
    }

    [Fact]
    public void Delay_SitsInColumnZeroEvenWhenFedFromLater()
    {
        var result = ScriptCompiler.Compile("d = delay(n, ticks: 1)\nn = !d", 0);

        Assert.True(result.Blocks[0].Cell.Z < result.Blocks[1].Cell.Z);
    }

    [Fact]
    public void Layout_IsTheSameEveryCompile()
    {
        const string script = "x = ticker()\ny = ticker()\nb = digitizer(y)\na = digitizer(x)\npower_display(a & b)";

        var first = ScriptCompiler.Compile(script, 0).Blocks.Select(b => b.Cell.ToString());
        var second = ScriptCompiler.Compile(script, 0).Blocks.Select(b => b.Cell.ToString());

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Footprints_NeverOverlapAndAreCentered(int quarterTurns)
    {
        const string script = """
            p = player_input()
            t = date_timer()
            w = wheel_telemetry()
            count = counter(p.brake, p.horn)
            on = (p.left | p.right) & !w.any
            blink = delay(!blink, ticks: 10)
            level = (t.second % 2) * count + blink
            force_giver(on, level, 0, 1)
            power_display(mux(level, count, 3, 4, p.arms_up))
            death(level > 100)
            """;
        var result = ScriptCompiler.Compile(script, quarterTurns);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));

        List<(int X, int Z)> occupied = [];
        foreach (var block in result.Blocks)
        {
            for (var x = block.Style.MinX; x <= block.Style.MaxX; x++)
            {
                for (var z = 0; z < block.Style.Length; z++)
                {
                    var offset = BlockLayout.Rotate(x, z, quarterTurns);
                    occupied.Add((block.Cell.X + offset.X, block.Cell.Z + offset.Z));
                }
            }
        }

        Assert.Equal(occupied.Count, occupied.Distinct().Count());
        Assert.InRange(occupied.Min(c => c.X) + occupied.Max(c => c.X), -1, 1);
        Assert.InRange(occupied.Min(c => c.Z) + occupied.Max(c => c.Z), -1, 1);
    }

    [Theory]
    [InlineData(0, 0, -3, 0)]
    [InlineData(1, -3, 0, 90)]
    [InlineData(2, 0, 3, 180)]
    [InlineData(3, 3, 0, 270)]
    public void QuarterTurns_RotateTheGroupAndEachBlock(int quarterTurns, int x, int z, int yaw)
    {
        var result = ScriptCompiler.Compile("power_display(1 & 1)", quarterTurns);

        var generator = result.Blocks[0];
        Assert.Equal((x, z, yaw), (generator.Cell.X, generator.Cell.Z, generator.Yaw));
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(0, -1, 2)]
    [InlineData(-1, 0, 3)]
    [InlineData(0.9f, 0.3f, 1)]
    [InlineData(-0.8f, 0.6f, 3)]
    public void QuarterTurnsFacing_SnapsToTheClosestAxis(float x, float z, int expected)
    {
        Assert.Equal(expected, ScriptCompiler.GetQuarterTurnsFacing(x, z));
    }

    [Fact]
    public void Rotate_TurnsPlusZTowardPlusX()
    {
        Assert.Equal("(1, 0)", BlockLayout.Rotate(0, 1, 1).ToString());
        Assert.Equal("(0, -1)", BlockLayout.Rotate(1, 0, 1).ToString());
    }
}
