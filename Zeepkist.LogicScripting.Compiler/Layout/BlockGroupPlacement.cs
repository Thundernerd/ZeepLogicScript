using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// Where a recompile puts the new Block Group: in the old group's yaw, with the new start corner (column 0, row 0)
/// where the old group's start corner is now. Moving the whole group sticks; nudging single blocks doesn't.
/// </summary>
public static class BlockGroupPlacement
{
    /// <summary>
    /// The yaw most of the blocks have, in 90° steps; a tie goes to the fewest turns, so block order doesn't matter.
    /// Reshaped blocks don't count unless all of them are.
    /// </summary>
    /// <param name="blocks">The group's blocks; at least one.</param>
    public static int GetMajorityQuarterTurns(IReadOnlyCollection<GroupBlock> blocks)
    {
        var notReshaped = blocks.Where(b => !b.IsReshaped).ToList();
        var counts = new int[4];
        foreach (var block in notReshaped.Count > 0 ? notReshaped : blocks)
        {
            counts[QuarterTurns.FromDegrees(block.Yaw)]++;
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
    /// shift the group when it moves on or off the corner's edge. Each old block is measured as Compile placed it, in the
    /// group's yaw and unscaled, since the recompile puts it back that way; turning, tilting or scaling a block by hand
    /// doesn't shift the group.
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

        var oldCorners = oldBlocks.Select(b => GetStartCorner(b.X, b.Z, b.Style, quarterTurns)).ToList();
        var newCorners = newBlocks
            .Select(b => GetStartCorner(b.Cell.X * GridCell.Size, b.Cell.Z * GridCell.Size, b.Style, quarterTurns))
            .ToList();

        var originX = oldCorners.Min(c => c.X) - (newCorners.Count == 0 ? 0 : newCorners.Min(c => c.X));
        var originZ = oldCorners.Min(c => c.Z) - (newCorners.Count == 0 ? 0 : newCorners.Min(c => c.Z));
        return QuarterTurns.Rotate(originX, originZ, quarterTurns);
    }

    /// <summary>
    /// The lowest X and Z of the footprint of a block at world (<paramref name="x"/>, <paramref name="z"/>) in the
    /// group's yaw, in the axes of a group turned by <paramref name="quarterTurns"/>.
    /// </summary>
    private static (float X, float Z) GetStartCorner(float x, float z, BlockStyle style, int quarterTurns)
    {
        var pivot = QuarterTurns.Rotate(x, z, -quarterTurns);
        return (pivot.X + (style.MinX * GridCell.Size), pivot.Z);
    }
}
