using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// Layered layout: a block's column is its longest Wire path from a Source Block, and a few barycenter passes order
/// the blocks within each column, ties by Script line order. One empty cell separates columns and rows, and each
/// column is centered across the group. Signals flow along the group's +Z, which the yaw turns to point right as seen
/// from the camera. The footprint is centered on cell (0, 0).
/// </summary>
internal static class BlockLayout
{
    private const int BarycenterPasses = 4;
    private const int Gap = 1;

    public static GridCell[] Arrange(BlockGraph graph, int quarterTurns)
    {
        if (graph.Blocks.Count == 0)
            return [];

        var columns = GetColumns(graph);
        var columnCount = columns.Max() + 1;

        var columnBlocks = new List<BlockNode>[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            columnBlocks[column] = [];
        }

        foreach (var block in graph.Blocks)
        {
            columnBlocks[columns[block.Index]].Add(block);
        }

        var columnStarts = new int[columnCount];
        var columnSpans = new int[columnCount];
        var nextStart = 0;
        for (var column = 0; column < columnCount; column++)
        {
            columnStarts[column] = nextStart;
            nextStart += columnBlocks[column].Max(b => b.Style.Length) + Gap;
            columnSpans[column] = columnBlocks[column].Sum(b => b.Style.Width + Gap) - Gap;
        }

        var depth = nextStart - Gap;

        var width = columnSpans.Max();
        var columnXStarts = new int[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            columnXStarts[column] = (width - columnSpans[column]) / 2;
        }

        var xStarts = new int[graph.Blocks.Count];
        for (var column = 0; column < columnCount; column++)
        {
            Stack(columnBlocks[column], columnXStarts[column], xStarts);
        }

        Order(graph, columns, columnBlocks, columnXStarts, xStarts);

        var centerX = (width - 1) / 2;
        var centerZ = (depth - 1) / 2;
        var cells = new GridCell[graph.Blocks.Count];
        foreach (var block in graph.Blocks)
        {
            var x = xStarts[block.Index] - block.Style.MinX - centerX;
            var z = columnStarts[columns[block.Index]] - centerZ;
            cells[block.Index] = Rotate(x, z, quarterTurns);
        }

        return cells;
    }

    /// <summary>
    /// The number of 90° turns that points the group's +Z closest to the given world direction (x, z).
    /// </summary>
    public static int GetQuarterTurnsFacing(float x, float z)
    {
        return QuarterTurns.FromDegrees(Math.Atan2(x, z) * 180 / Math.PI);
    }

    /// <summary>
    /// Rotates a cell about the Y axis the way a block's yaw does: +Z turns toward +X.
    /// </summary>
    public static GridCell Rotate(int x, int z, int quarterTurns)
    {
        var (rotatedX, rotatedZ) = QuarterTurns.Rotate(x, z, quarterTurns);
        return new GridCell((int)rotatedX, (int)rotatedZ);
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

        // Recursing stays shallow however long the Script: blocks are built after the ones that feed them, and only a
        // Delay's input can come from a later one, so a block's sources are known by the time it's reached
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
    /// Sweeps the columns alternately forward and backward, sorting each by the mean center of the blocks it is wired
    /// to in the columns already swept. A block with no such Wires keeps its place as its key.
    /// </summary>
    private static void Order(
        BlockGraph graph,
        int[] columns,
        List<BlockNode>[] columnBlocks,
        int[] columnXStarts,
        int[] xStarts)
    {
        var neighbors = new List<BlockNode>[graph.Blocks.Count];
        foreach (var wire in graph.Wires)
        {
            (neighbors[wire.From.Index] ??= []).Add(wire.To);
            (neighbors[wire.To.Index] ??= []).Add(wire.From);
        }

        double GetCenter(BlockNode block)
        {
            return xStarts[block.Index] + (block.Style.Width - 1) / 2.0;
        }

        double GetKey(BlockNode block, bool isForward)
        {
            var column = columns[block.Index];
            var swept = neighbors[block.Index]?
                .Where(n => isForward ? columns[n.Index] < column : columns[n.Index] > column)
                .ToList();

            return swept is { Count: > 0 } ? swept.Average(GetCenter) : GetCenter(block);
        }

        for (var pass = 0; pass < BarycenterPasses; pass++)
        {
            var isForward = pass % 2 == 0;
            for (var offset = 1; offset < columnBlocks.Length; offset++)
            {
                var column = isForward ? offset : columnBlocks.Length - 1 - offset;
                columnBlocks[column] = [.. columnBlocks[column].OrderBy(b => GetKey(b, isForward)).ThenBy(b => b.Index)];
                Stack(columnBlocks[column], columnXStarts[column], xStarts);
            }
        }
    }

    private static void Stack(List<BlockNode> blocks, int xStart, int[] xStarts)
    {
        var start = xStart;
        foreach (var block in blocks)
        {
            xStarts[block.Index] = start;
            start += block.Style.Width + Gap;
        }
    }
}
