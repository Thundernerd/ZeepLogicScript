using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One Logic Block type in the Block Manifest: its Block Function name, block ID, Connectors and footprint.
/// </summary>
public sealed class BlockType
{
    public BlockType(
        string function,
        int blockId,
        int length,
        IReadOnlyList<BlockConnector> inputs,
        IReadOnlyList<BlockConnector> outputs,
        params BlockSetting[] settings)
    {
        Function = function;
        BlockId = blockId;
        Length = length;
        Inputs = inputs;
        Outputs = outputs;
        Settings = settings;
    }

    public string Function { get; }
    public int BlockId { get; }

    /// <summary>
    /// Footprint along the block's own +Z (the direction Signals flow), in 16-unit cells.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Input Connectors in the order positional arguments fill them.
    /// </summary>
    public IReadOnlyList<BlockConnector> Inputs { get; }

    /// <summary>
    /// Output Connectors; a block with more than one is used through dot access.
    /// </summary>
    public IReadOnlyList<BlockConnector> Outputs { get; }

    public IReadOnlyList<BlockSetting> Settings { get; }
}

/// <summary>
/// A named Connector and its zero-based index on the block.
/// </summary>
public sealed class BlockConnector
{
    public BlockConnector(string name, int index)
    {
        Name = name;
        Index = index;
    }

    public string Name { get; }
    public int Index { get; }
}
