using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// Where a recompile puts the new Block Group: in the old group's yaw, with the new start corner (column 0, row 0)
/// where the old group's start corner is now. Moving the whole group sticks; nudging single blocks doesn't.
/// </summary>
public static class BlockGroupPlacement
{
    /// <summary>
    /// The yaw most of the blocks have, in 90° steps; a tie goes to the fewest turns, so block order doesn't matter.
    /// </summary>
    /// <param name="yaws">Each block's rotation about Y in degrees.</param>
    public static int GetMajorityQuarterTurns(IEnumerable<float> yaws)
    {
        var counts = new int[4];
        foreach (var yaw in yaws)
        {
            counts[QuarterTurns.FromDegrees(yaw)]++;
        }

        var majority = 0;
        for (var turns = 1; turns < counts.Length; turns++)
        {
            if (counts[turns] > counts[majority])
                majority = turns;
        }

        return majority;
    }

    /// <summary>
    /// The world position for the new group's cell (0, 0), so its start corner lands on the old group's. Both corners
    /// are taken from the blocks' footprints, in the group's rotation, so a block that reaches past its pivot doesn't
    /// shift the group when it moves on or off the corner's edge.
    /// </summary>
    /// <param name="oldBlocks">The old group's blocks; at least one.</param>
    /// <param name="newBlocks">The new group's blocks, laid out with <paramref name="quarterTurns"/>.</param>
    /// <param name="quarterTurns">The group's yaw in 90° steps.</param>
    public static (float X, float Z) GetOrigin(
        IReadOnlyCollection<GroupBlock> oldBlocks,
        IReadOnlyCollection<CompiledBlock> newBlocks,
        int quarterTurns)
    {
        if (oldBlocks.Count == 0)
            throw new ArgumentException("The old group has no blocks", nameof(oldBlocks));

        var oldCorners = oldBlocks.Select(b => GetStartCorner(b, quarterTurns)).ToList();
        var newCorners = newBlocks
            .Select(b => new GroupBlock(b.Cell.X * GridCell.Size, b.Cell.Z * GridCell.Size, b.Yaw, b.Style))
            .Select(b => GetStartCorner(b, quarterTurns))
            .ToList();

        var originX = oldCorners.Min(c => c.X) - (newCorners.Count == 0 ? 0 : newCorners.Min(c => c.X));
        var originZ = oldCorners.Min(c => c.Z) - (newCorners.Count == 0 ? 0 : newCorners.Min(c => c.Z));
        return QuarterTurns.Rotate(originX, originZ, quarterTurns);
    }

    /// <summary>
    /// The lowest X and Z of a block's footprint, in the axes of a group turned by <paramref name="quarterTurns"/>.
    /// </summary>
    private static (float X, float Z) GetStartCorner(GroupBlock block, int quarterTurns)
    {
        var minX = (block.Style?.MinX ?? 0) * GridCell.Size;
        var maxX = (block.Style?.MaxX ?? 0) * GridCell.Size;
        var maxZ = ((block.Style?.Length ?? 1) - 1) * GridCell.Size;

        var turns = QuarterTurns.FromDegrees(block.Yaw) - quarterTurns;
        var first = QuarterTurns.Rotate(minX, 0, turns);
        var last = QuarterTurns.Rotate(maxX, maxZ, turns);
        var pivot = QuarterTurns.Rotate(block.X, block.Z, -quarterTurns);
        return (pivot.X + Math.Min(first.X, last.X), pivot.Z + Math.Min(first.Z, last.Z));
    }
}
