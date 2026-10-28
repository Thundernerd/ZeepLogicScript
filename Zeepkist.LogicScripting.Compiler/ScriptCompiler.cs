using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// Turns Script text into the blocks and Wires to place, as plain data. Placing them is up to the caller.
/// </summary>
public static class ScriptCompiler
{
    /// <param name="text">The Script.</param>
    /// <param name="quarterTurns">The group's yaw in 90° steps; see <see cref="GetQuarterTurnsFacing"/>.</param>
    /// <param name="disabledBlockIds">
    /// Blocks the startup check found don't match the game; a Script that places or refers to one gets an error.
    /// </param>
    /// <param name="findPlacedBlock">
    /// The block in the level with a UID, or null when there's none; without it, every Placed Block Reference is an
    /// error.
    /// </param>
    public static CompileResult Compile(
        string text,
        int quarterTurns,
        IReadOnlyCollection<int> disabledBlockIds = null,
        Func<string, PlacedBlock> findPlacedBlock = null)
    {
        List<Diagnostic> diagnostics = [];
        var tokens = Lexer.Tokenize(text ?? string.Empty, diagnostics);
        var statements = Parser.Parse(tokens, diagnostics);
        var graph = GraphBuilder.Build(statements, diagnostics, disabledBlockIds ?? [], findPlacedBlock ?? (_ => null));
        var references = graph.PlacedBlockReferences.OrderBy(r => r.Position).ToList();

        if (diagnostics.Count > 0)
        {
            var sorted = diagnostics.OrderBy(d => d.Position).ToList();
            return new CompileResult(sorted, [], [], [], references);
        }

        var cells = BlockLayout.Arrange(graph, quarterTurns);
        var yaw = (((quarterTurns % 4) + 4) % 4) * 90;

        List<CompiledBlock> blocks = [];
        foreach (var block in graph.Blocks)
        {
            blocks.Add(new CompiledBlock(block.Type, block.Style, block.Settings, cells[block.Index], yaw));
        }

        List<CompiledWire> wires = [];
        foreach (var wire in graph.Wires)
        {
            wires.Add(ToCompiledWire(wire));
        }

        List<CompiledWire> placedWires = [];
        foreach (var wire in graph.PlacedWires)
        {
            placedWires.Add(ToCompiledWire(wire));
        }

        return new CompileResult([], blocks, wires, placedWires, references);
    }

    private static CompiledWire ToCompiledWire(GraphWire wire)
    {
        return new CompiledWire(GetEnd(wire.From), wire.FromConnector, GetEnd(wire.To), wire.ToConnector);
    }

    private static WireEnd GetEnd(BlockNode block)
    {
        return block.IsPlaced ? WireEnd.Placed(block.Uid) : WireEnd.Compiled(block.Index);
    }

    /// <summary>
    /// The yaw that makes Signals flow towards the given world direction, usually the camera's right.
    /// </summary>
    public static int GetQuarterTurnsFacing(float x, float z)
    {
        return BlockLayout.GetQuarterTurnsFacing(x, z);
    }
}
