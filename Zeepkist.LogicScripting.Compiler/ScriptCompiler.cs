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
    /// <param name="findLibrary">
    /// A Library's text by its name, or null when there's none on this machine; without it, every qualified call is an
    /// error. Each Library is asked for once.
    /// </param>
    public static CompileResult Compile(
        string text,
        int quarterTurns,
        IReadOnlyCollection<int> disabledBlockIds = null,
        Func<string, PlacedBlock> findPlacedBlock = null,
        Func<string, string> findLibrary = null)
    {
        List<Diagnostic> diagnostics = [];
        var tokens = Lexer.Tokenize(text ?? string.Empty, diagnostics);
        var statements = Parser.Parse(tokens, diagnostics);
        var graph = GraphBuilder.Build(
            statements,
            diagnostics,
            disabledBlockIds ?? [],
            findPlacedBlock ?? (_ => null),
            findLibrary ?? (_ => null));
        var references = graph.PlacedBlockReferences.OrderBy(r => r.Position).ToList();

        if (diagnostics.Count > 0)
        {
            var sorted = diagnostics.OrderBy(d => d.Position).ToList();
            return new CompileResult(sorted, [], [], [], references);
        }

        var cells = BlockLayout.Arrange(graph, quarterTurns);
        var yaw = QuarterTurns.Normalize(quarterTurns) * 90;

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

    /// <summary>
    /// The errors in a Library on its own, without a Script calling it. It may hold only Functions, and each body is
    /// checked with inputs that stand for whatever a caller could pass.
    /// </summary>
    /// <param name="text">The Library.</param>
    /// <param name="findLibrary">Another Library's text by its name, or null when there's none on this machine.</param>
    public static IReadOnlyList<Diagnostic> CheckLibrary(string text, Func<string, string> findLibrary = null)
    {
        List<Diagnostic> diagnostics = [];
        var tokens = Lexer.Tokenize(text ?? string.Empty, diagnostics);
        var statements = Parser.Parse(tokens, diagnostics);
        GraphBuilder.Build(statements, diagnostics, [], _ => null, findLibrary ?? (_ => null), isLibrary: true);
        return diagnostics.OrderBy(d => d.Position).ToList();
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
    /// The yaw that makes Signals flow toward the given world direction, usually the camera's right.
    /// </summary>
    public static int GetQuarterTurnsFacing(float x, float z)
    {
        return BlockLayout.GetQuarterTurnsFacing(x, z);
    }
}
