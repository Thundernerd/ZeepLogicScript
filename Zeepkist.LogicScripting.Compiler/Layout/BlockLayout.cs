using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// Naive layout: a block's column is its longest Wire path from a Source Block, rows follow Script line order,
/// with one empty cell between columns and rows. Signals flow along the group's +Z, which the yaw turns to
/// point right as seen from the camera. The footprint is centred on cell (0, 0).
/// </summary>
internal static class BlockLayout
{
    private const int RowPitch = 2;

    public static GridCell[] Arrange(BlockGraph graph, int quarterTurns)
    {
        var columns = GetColumns(graph);
        var columnCount = graph.Blocks.Count == 0 ? 0 : columns.Max() + 1;

        var columnStarts = new int[columnCount];
        var rows = new int[graph.Blocks.Count];
        var rowCounts = new int[columnCount];
        var lengths = new int[columnCount];
        foreach (var block in graph.Blocks)
        {
            var column = columns[block.Index];
            rows[block.Index] = rowCounts[column]++;
            lengths[column] = Math.Max(lengths[column], block.Style.Length);
        }

        var depth = 0;
        for (var column = 0; column < columnCount; column++)
        {
            columnStarts[column] = depth;
            depth += lengths[column] + 1;
        }

        var width = columnCount == 0 ? 0 : (rowCounts.Max() - 1) * RowPitch + 1;
        var centreX = (width - 1) / 2;
        var centreZ = (depth - 2) / 2;

        var cells = new GridCell[graph.Blocks.Count];
        foreach (var block in graph.Blocks)
        {
            var x = rows[block.Index] * RowPitch - centreX;
            var z = columnStarts[columns[block.Index]] - centreZ;
            cells[block.Index] = Rotate(x, z, quarterTurns);
        }

        return cells;
    }

    /// <summary>
    /// The number of 90° turns that points the group's +Z closest to the given world direction (x, z).
    /// </summary>
    public static int GetQuarterTurnsFacing(float x, float z)
    {
        var degrees = Math.Atan2(x, z) * 180 / Math.PI;
        var turns = (int)Math.Round(degrees / 90);
        return ((turns % 4) + 4) % 4;
    }

    /// <summary>
    /// Rotates a cell about the Y axis the way a block's yaw does: +Z turns towards +X.
    /// </summary>
    public static GridCell Rotate(int x, int z, int quarterTurns)
    {
        return (((quarterTurns % 4) + 4) % 4) switch
        {
            1 => new GridCell(z, -x),
            2 => new GridCell(-x, -z),
            3 => new GridCell(-z, x),
            _ => new GridCell(x, z)
        };
    }

    private static int[] GetColumns(BlockGraph graph)
    {
        var inputs = new List<BlockNode>[graph.Blocks.Count];
        foreach (var wire in graph.Wires)
        {
            // A Delay sits in column 0. Every cycle a Script can describe runs into a Delay, so this breaks them all
            if (wire.To.Type == BlockManifest.Delay)
                continue;

            (inputs[wire.To.Index] ??= []).Add(wire.From);
        }

        var columns = new int[graph.Blocks.Count];
        var known = new bool[graph.Blocks.Count];

        int GetColumn(BlockNode block)
        {
            if (known[block.Index])
                return columns[block.Index];

            var column = 0;
            if (inputs[block.Index] != null)
            {
                foreach (var source in inputs[block.Index])
                {
                    column = Math.Max(column, GetColumn(source) + 1);
                }
            }

            known[block.Index] = true;
            return columns[block.Index] = column;
        }

        foreach (var block in graph.Blocks)
        {
            GetColumn(block);
        }

        return columns;
    }
}
