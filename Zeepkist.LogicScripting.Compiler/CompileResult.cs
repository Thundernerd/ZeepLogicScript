using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class CompileResult
{
    public CompileResult(IReadOnlyList<Diagnostic> errors, IReadOnlyList<CompiledBlock> blocks, IReadOnlyList<CompiledWire> wires)
    {
        Errors = errors;
        Blocks = blocks;
        Wires = wires;
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

    public bool Succeeded => Errors.Count == 0;
}
