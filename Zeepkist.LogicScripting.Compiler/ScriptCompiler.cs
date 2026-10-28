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
    /// Blocks the startup check found don't match the game; a Script that places one gets an error.
    /// </param>
    public static CompileResult Compile(string text, int quarterTurns, IReadOnlyCollection<int> disabledBlockIds = null)
    {
        List<Diagnostic> diagnostics = [];
        var tokens = Lexer.Tokenize(text ?? string.Empty, diagnostics);
        var statements = Parser.Parse(tokens, diagnostics);
        var graph = GraphBuilder.Build(statements, diagnostics, disabledBlockIds ?? []);

        if (diagnostics.Count > 0)
        {
            var sorted = diagnostics.OrderBy(d => d.Position).ToList();
            return new CompileResult(sorted, [], []);
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
            wires.Add(new CompiledWire(wire.From.Index, wire.FromConnector, wire.To.Index, wire.ToConnector));
        }

        return new CompileResult([], blocks, wires);
    }

    /// <summary>
    /// The yaw that makes Signals flow towards the given world direction, usually the camera's right.
    /// </summary>
    public static int GetQuarterTurnsFacing(float x, float z)
    {
        return BlockLayout.GetQuarterTurnsFacing(x, z);
    }
}
