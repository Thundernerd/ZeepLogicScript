using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// The blocks a Script describes and the Wires between them, before layout.
/// </summary>
internal sealed class BlockGraph
{
    public List<BlockNode> Blocks { get; } = [];
    public List<GraphWire> Wires { get; } = [];

    public BlockNode AddBlock(BlockType type, params Setting[] settings)
    {
        var block = new BlockNode(Blocks.Count, type, settings);
        Blocks.Add(block);
        return block;
    }

    public void AddWire(BlockOutput from, BlockNode to, int toConnector)
    {
        Wires.Add(new GraphWire(from.Block, from.Connector, to, toConnector));
    }
}

internal sealed class BlockNode
{
    public BlockNode(int index, BlockType type, IReadOnlyList<Setting> settings)
    {
        Index = index;
        Type = type;
        Settings = settings;
    }

    /// <summary>
    /// Position in the graph; blocks are added in Script line order.
    /// </summary>
    public int Index { get; }

    public BlockType Type { get; }
    public IReadOnlyList<Setting> Settings { get; }
}

/// <summary>
/// An output Connector of a block: what a name or an expression stands for.
/// </summary>
internal readonly struct BlockOutput
{
    public BlockOutput(BlockNode block, int connector)
    {
        Block = block;
        Connector = connector;
    }

    public BlockNode Block { get; }
    public int Connector { get; }
}

internal sealed class GraphWire
{
    public GraphWire(BlockNode from, int fromConnector, BlockNode to, int toConnector)
    {
        From = from;
        FromConnector = fromConnector;
        To = to;
        ToConnector = toConnector;
    }

    public BlockNode From { get; }
    public int FromConnector { get; }
    public BlockNode To { get; }
    public int ToConnector { get; }
}
