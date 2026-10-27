using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockLayoutTests
{
    [Fact]
    public void Columns_FollowTheLongestPathWithAGapCellBetween()
    {
        // Generator (1 cell), Logic (3 cells), Power Display (2 cells): 8 cells deep, centred on cell 3
        var result = ScriptCompiler.Compile("power_display(1 & 1)", 0);

        Assert.Equal(["(0, -3)", "(0, -1)", "(0, 3)"], result.Blocks.Select(b => b.Cell.ToString()));
    }

    [Fact]
    public void Rows_FollowLineOrderWithAGapCellBetween()
    {
        // Two Logic blocks in column 1 and two Generators in column 0: three cells wide, centred on cell 1
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
    public void Rotate_TurnsPlusZTowardsPlusX()
    {
        Assert.Equal("(1, 0)", BlockLayout.Rotate(0, 1, 1).ToString());
        Assert.Equal("(0, -1)", BlockLayout.Rotate(1, 0, 1).ToString());
    }
}
