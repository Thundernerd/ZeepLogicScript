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
            var turns = (int)Math.Round(yaw / 90) % 4;
            counts[(turns + 4) % 4]++;
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
    /// are taken from block pivots, in the group's rotation.
    /// </summary>
    /// <param name="oldPositions">World X and Z of each old group block's pivot; at least one.</param>
    /// <param name="cells">The new group's cells, laid out with <paramref name="quarterTurns"/>.</param>
    /// <param name="quarterTurns">The group's yaw in 90° steps.</param>
    public static (float X, float Z) GetOrigin(
        IReadOnlyCollection<(float X, float Z)> oldPositions,
        IReadOnlyCollection<GridCell> cells,
        int quarterTurns)
    {
        if (oldPositions.Count == 0)
            throw new ArgumentException("The old group has no blocks", nameof(oldPositions));

        var undo = 4 - (((quarterTurns % 4) + 4) % 4);
        var oldInGroup = oldPositions.Select(p => Rotate(p.X, p.Z, undo)).ToList();
        var newInGroup = cells.Select(c => Rotate(c.X * GridCell.Size, c.Z * GridCell.Size, undo)).ToList();

        var originX = oldInGroup.Min(p => p.X) - (newInGroup.Count == 0 ? 0 : newInGroup.Min(p => p.X));
        var originZ = oldInGroup.Min(p => p.Z) - (newInGroup.Count == 0 ? 0 : newInGroup.Min(p => p.Z));
        return Rotate(originX, originZ, quarterTurns);
    }

    /// <summary>
    /// Rotates a point about the Y axis the way a block's yaw does: +Z turns towards +X.
    /// </summary>
    private static (float X, float Z) Rotate(float x, float z, int quarterTurns)
    {
        return (((quarterTurns % 4) + 4) % 4) switch
        {
            1 => (z, -x),
            2 => (-x, -z),
            3 => (-z, x),
            _ => (x, z)
        };
    }
}
