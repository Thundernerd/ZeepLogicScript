using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Layout;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

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

public sealed class CompiledBlock
{
    public CompiledBlock(BlockType type, IReadOnlyList<Setting> settings, GridCell cell, int yaw)
    {
        Type = type;
        Settings = settings;
        Cell = cell;
        Yaw = yaw;
    }

    public BlockType Type { get; }
    public IReadOnlyList<Setting> Settings { get; }

    /// <summary>
    /// The block's pivot cell, in world axes, relative to the group's centre.
    /// </summary>
    public GridCell Cell { get; }

    /// <summary>
    /// Rotation about Y in degrees.
    /// </summary>
    public int Yaw { get; }
}

/// <summary>
/// A Wire from an output Connector to an input Connector, with blocks given by their index in the result.
/// </summary>
public sealed class CompiledWire
{
    public CompiledWire(int from, int fromConnector, int to, int toConnector)
    {
        From = from;
        FromConnector = fromConnector;
        To = to;
        ToConnector = toConnector;
    }

    public int From { get; }
    public int FromConnector { get; }
    public int To { get; }
    public int ToConnector { get; }
}
