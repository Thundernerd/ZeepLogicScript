using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockGroupPlacementTests
{
    private const string Script = "a = 1 & 2\nb = a | 3\npower_display(b)";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AMovedGroup_RecompilesWhereItWasMoved(int quarterTurns)
    {
        var cells = Cells(ScriptCompiler.Compile(Script, quarterTurns));
        var moved = cells.Select(c => ((c.X * GridCell.Size) + 104, (c.Z * GridCell.Size) - 40)).ToList();

        Assert.Equal((104f, -40f), BlockGroupPlacement.GetOrigin(moved, cells, quarterTurns));
    }

    [Fact]
    public void TheNewStartCorner_GoesWhereTheOldOneIs()
    {
        // In yaw 0 rows run along +X and columns along +Z, so the start corner is the lowest X and Z
        var cells = Cells(ScriptCompiler.Compile(Script, 0));
        var origin = BlockGroupPlacement.GetOrigin([(50, 70), (82, 70), (50, 150)], cells, 0);

        Assert.Equal(50, origin.X + (cells.Min(c => c.X) * GridCell.Size));
        Assert.Equal(70, origin.Z + (cells.Min(c => c.Z) * GridCell.Size));
    }

    [Fact]
    public void TheStartCorner_TurnsWithTheGroup()
    {
        // In yaw 90 the group's +Z points along world +X and its +X along world -Z
        // Old corner: world (0, 0). New cells (0, 0) and (1, 1) put the new corner at the cell (1, 1), group (-16, 0)
        var origin = BlockGroupPlacement.GetOrigin([(0, 0), (32, 0), (0, -32)], [new GridCell(0, 0), new GridCell(1, 1)], 1);

        Assert.Equal((0f, -16f), origin);
    }

    [Fact]
    public void AnEmptyNewGroup_StartsAtTheOldCorner()
    {
        Assert.Equal((16f, 32f), BlockGroupPlacement.GetOrigin([(16, 32), (48, 64)], [], 0));
    }

    [Theory]
    [InlineData(new[] { 0f, 90f, 90f }, 1)]
    [InlineData(new[] { 270f, -90f, 0f }, 3)]
    [InlineData(new[] { 180f, 0f }, 0)]
    [InlineData(new[] { 89.9f, 359.9f, 0.1f }, 0)]
    public void TheYaw_IsTheMajorityRoundedToQuarterTurns(float[] yaws, int expected)
    {
        Assert.Equal(expected, BlockGroupPlacement.GetMajorityQuarterTurns(yaws));
    }

    private static List<GridCell> Cells(CompileResult result)
    {
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Blocks.Select(b => b.Cell).ToList();
    }
}
