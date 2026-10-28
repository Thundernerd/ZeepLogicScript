using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// Layered layout: a block's column is its longest Wire path from a Source Block, and a few barycenter passes order
/// the blocks within each column, ties by Script line order. One empty cell separates columns and rows, and each
/// column is centred across the group. Signals flow along the group's +Z, which the yaw turns to point right as seen
/// from the camera. The footprint is centred on cell (0, 0).
/// </summary>
internal static class BlockLayout
{
    private const int BarycenterPasses = 4;

    public static GridCell[] Arrange(BlockGraph graph, int quarterTurns)
    {
        if (graph.Blocks.Count == 0)
            return [];

        var columns = GetColumns(graph);
        var columnCount = columns.Max() + 1;

        var order = new List<BlockNode>[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            order[column] = [];
        }

        foreach (var block in graph.Blocks)
        {
            order[columns[block.Index]].Add(block);
        }

        var columnStarts = new int[columnCount];
        var columnWidths = new int[columnCount];
        var nextStart = 0;
        for (var column = 0; column < columnCount; column++)
        {
            columnStarts[column] = nextStart;
            nextStart += order[column].Max(b => b.Style.Length) + 1;
            columnWidths[column] = order[column].Sum(b => b.Style.Width + 1) - 1;
        }

        var depth = nextStart - 1;

        var width = columnWidths.Max();
        var rowStarts = new int[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            rowStarts[column] = (width - columnWidths[column]) / 2;
        }

        var starts = new int[graph.Blocks.Count];
        for (var column = 0; column < columnCount; column++)
        {
            Stack(order[column], rowStarts[column], starts);
        }

        Order(graph, columns, order, rowStarts, starts);

        var centreX = (width - 1) / 2;
        var centreZ = (depth - 1) / 2;
        var cells = new GridCell[graph.Blocks.Count];
        foreach (var block in graph.Blocks)
        {
            var x = starts[block.Index] - block.Style.MinX - centreX;
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

    /// <summary>
    /// Sweeps the columns alternately forwards and backwards, sorting each by the mean centre of the blocks it is wired
    /// to in the columns already swept. A block with no such Wires keeps its place as its key.
    /// </summary>
    private static void Order(
        BlockGraph graph,
        int[] columns,
        List<BlockNode>[] order,
        int[] rowStarts,
        int[] starts)
    {
        var neighbours = new List<BlockNode>[graph.Blocks.Count];
        foreach (var wire in graph.Wires)
        {
            (neighbours[wire.From.Index] ??= []).Add(wire.To);
            (neighbours[wire.To.Index] ??= []).Add(wire.From);
        }

        double GetCentre(BlockNode block)
        {
            return starts[block.Index] + (block.Style.Width - 1) / 2.0;
        }

        double GetKey(BlockNode block, bool isForward)
        {
            var column = columns[block.Index];
            var swept = neighbours[block.Index]?
                .Where(n => isForward ? columns[n.Index] < column : columns[n.Index] > column)
                .ToList();

            return swept is { Count: > 0 } ? swept.Average(GetCentre) : GetCentre(block);
        }

        for (var pass = 0; pass < BarycenterPasses; pass++)
        {
            var isForward = pass % 2 == 0;
            for (var step = 1; step < order.Length; step++)
            {
                var column = isForward ? step : order.Length - 1 - step;
                order[column] = [.. order[column].OrderBy(b => GetKey(b, isForward)).ThenBy(b => b.Index)];
                Stack(order[column], rowStarts[column], starts);
            }
        }
    }

    /// <summary>
    /// Sets each block's first cell along X, one empty cell apart, in order.
    /// </summary>
    private static void Stack(List<BlockNode> blocks, int rowStart, int[] starts)
    {
        var start = rowStart;
        foreach (var block in blocks)
        {
            starts[block.Index] = start;
            start += block.Style.Width + 1;
        }
    }
}
