using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class CompileResult
{
    public CompileResult(
        IReadOnlyList<Diagnostic> errors,
        IReadOnlyList<CompiledBlock> blocks,
        IReadOnlyList<CompiledWire> wires,
        IReadOnlyList<PlacedWire> placedWires,
        IReadOnlyList<PlacedBlockReference> placedBlockReferences)
    {
        Errors = errors;
        Blocks = blocks;
        Wires = wires;
        PlacedWires = placedWires;
        PlacedBlockReferences = placedBlockReferences;
    }

    /// <summary>
    /// Every error, sorted by position. When there are any, there are no blocks.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors { get; }

    public IReadOnlyList<CompiledBlock> Blocks { get; }

    /// <summary>
    /// Wires between <see cref="Blocks"/>, by index.
    /// </summary>
    public IReadOnlyList<CompiledWire> Wires { get; }

    /// <summary>
    /// Wires into or out of blocks already in the level. When there are errors, there are none.
    /// </summary>
    public IReadOnlyList<PlacedWire> PlacedWires { get; }

    /// <summary>
    /// Every <c>placed("UID")</c> the Script could be read far enough to find, errors or not, in Script order.
    /// </summary>
    public IReadOnlyList<PlacedBlockReference> PlacedBlockReferences { get; }

    public bool Succeeded => Errors.Count == 0;

    /// <summary>
    /// How many blocks already in the level <see cref="PlacedWires"/> wire into or out of.
    /// </summary>
    public int WiredPlacedBlockCount => PlacedWires
        .SelectMany(w => new[] { w.From.Uid, w.To.Uid })
        .Where(uid => uid != null)
        .Distinct()
        .Count();
}
