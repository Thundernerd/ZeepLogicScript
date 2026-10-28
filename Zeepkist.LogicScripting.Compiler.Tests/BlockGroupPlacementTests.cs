using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
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
        var blocks = Blocks(ScriptCompiler.Compile(Script, quarterTurns));
        var moved = blocks.Select(b => InLevel(b, 104, -40)).ToList();

        Assert.Equal((104f, -40f), BlockGroupPlacement.GetOrigin(moved, blocks, quarterTurns));
    }

    [Fact]
    public void TheNewStartCorner_GoesWhereTheOldOneIs()
    {
        // In yaw 0 rows run along +X and columns along +Z, so the start corner is the lowest X and Z
        var blocks = Blocks(ScriptCompiler.Compile(Script, 0));
        var origin = BlockGroupPlacement.GetOrigin([OneCell(50, 70), OneCell(82, 70), OneCell(50, 150)], blocks, 0);

        Assert.Equal(50, origin.X + (blocks.Min(b => b.Cell.X) * GridCell.Size));
        Assert.Equal(70, origin.Z + (blocks.Min(b => b.Cell.Z) * GridCell.Size));
    }

    [Fact]
    public void TheStartCorner_TurnsWithTheGroup()
    {
        // In yaw 90 the group's +Z points along world +X and its +X along world -Z
        // Old corner: world (0, 0). New cells (0, 0) and (1, 1) put the new corner at the cell (1, 1), group (-16, 0)
        var origin = BlockGroupPlacement.GetOrigin(
            [OneCell(0, 0, 90), OneCell(32, 0, 90), OneCell(0, -32, 90)],
            [Compiled(0, 0, 90), Compiled(1, 1, 90)],
            1);

        Assert.Equal((0f, -16f), origin);
    }

    [Fact]
    public void ABlockReachingPastItsPivot_SetsTheOldCorner()
    {
        // A Player Input is a cell wider toward -X, so the old corner is a cell left of its pivot
        var playerInput = new GroupBlock(0, 0, 0, BlockManifest.PlayerInput.DefaultStyle);

        Assert.Equal((-16f, 0f), BlockGroupPlacement.GetOrigin([playerInput], [Compiled(0, 0, 0)], 0));
    }

    [Fact]
    public void ABlockReachingPastItsPivot_SetsTheNewCorner()
    {
        var playerInput = new CompiledBlock(BlockManifest.PlayerInput, BlockManifest.PlayerInput.DefaultStyle, [], new GridCell(0, 0), 0);

        Assert.Equal((16f, 0f), BlockGroupPlacement.GetOrigin([OneCell(0, 0)], [playerInput], 0));
    }

    [Fact]
    public void AWideBlock_TurnsWithTheGroup()
    {
        // In yaw 90 the Player Input's extra cell, at group (-16, 0), lies at world (0, 16)
        var playerInput = new GroupBlock(0, 0, 90, BlockManifest.PlayerInput.DefaultStyle);

        Assert.Equal((0f, 16f), BlockGroupPlacement.GetOrigin([playerInput], [Compiled(0, 0, 90)], 1));
    }

    [Fact]
    public void ABlockTurnedOnItsOwn_UsesItsOwnFootprint()
    {
        // A Date Timer reaches a cell toward +X and two along +Z; turned 180° that is -X and -Z
        var dateTimer = new GroupBlock(0, 0, 180, BlockManifest.DateTimer.DefaultStyle);

        Assert.Equal((-16f, -32f), BlockGroupPlacement.GetOrigin([dateTimer], [Compiled(0, 0, 0)], 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AGroupWithWideBlocks_RecompilesWithoutThemWhereItWas(int quarterTurns)
    {
        // The wide blocks stand on the corner's edges in the first Compile and are gone in the second
        var first = Blocks(ScriptCompiler.Compile("player_input()\ndate_timer()\nwheel_telemetry()\n" + Script, quarterTurns));
        var second = Blocks(ScriptCompiler.Compile(Script, quarterTurns));
        var oldBlocks = first.Select(b => InLevel(b, 0, 0)).ToList();
        var origin = BlockGroupPlacement.GetOrigin(oldBlocks, second, quarterTurns);

        var newBlocks = second.Select(b => InLevel(b, origin.X, origin.Z)).ToList();
        Assert.Equal(GetFootprintCorner(oldBlocks, quarterTurns), GetFootprintCorner(newBlocks, quarterTurns));
    }

    [Fact]
    public void AnEmptyNewGroup_StartsAtTheOldCorner()
    {
        Assert.Equal((16f, 32f), BlockGroupPlacement.GetOrigin([OneCell(16, 32), OneCell(48, 64)], [], 0));
    }

    [Fact]
    public void ABlockTheManifestDoesntKnow_TakesOneCell()
    {
        Assert.Equal((16f, 32f), BlockGroupPlacement.GetOrigin([new GroupBlock(16, 32, 0, null)], [], 0));
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

    private static IReadOnlyList<CompiledBlock> Blocks(CompileResult result)
    {
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Blocks;
    }

    private static GroupBlock InLevel(CompiledBlock block, float originX, float originZ)
    {
        return new GroupBlock(
            (block.Cell.X * GridCell.Size) + originX,
            (block.Cell.Z * GridCell.Size) + originZ,
            block.Yaw,
            block.Style);
    }

    /// <summary>
    /// The lowest X and Z of every cell the blocks cover, in the group's axes.
    /// </summary>
    private static (int X, int Z) GetFootprintCorner(IEnumerable<GroupBlock> blocks, int quarterTurns)
    {
        var cells = (
            from block in blocks
            from x in Enumerable.Range(block.Style.MinX, block.Style.Width)
            from z in Enumerable.Range(0, block.Style.Length)
            let offset = BlockLayout.Rotate(x, z, (int)(block.Yaw / 90))
            select BlockLayout.Rotate(
                (int)(block.X / GridCell.Size) + offset.X,
                (int)(block.Z / GridCell.Size) + offset.Z,
                -quarterTurns)).ToList();

        return (cells.Min(c => c.X), cells.Min(c => c.Z));
    }

    private static GroupBlock OneCell(float x, float z, float yaw = 0)
    {
        return new GroupBlock(x, z, yaw, BlockManifest.Delay.DefaultStyle);
    }

    private static CompiledBlock Compiled(int x, int z, int yaw)
    {
        return new CompiledBlock(BlockManifest.Delay, BlockManifest.Delay.DefaultStyle, [], new GridCell(x, z), yaw);
    }
}
