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

    public BlockNode AddBlock(BlockType type, BlockStyle style, params Setting[] settings)
    {
        var block = new BlockNode(Blocks.Count, type, style, settings);
        Blocks.Add(block);
        return block;
    }

    public void AddWire(BlockOutput from, BlockNode to, int toConnector)
    {
        Wires.Add(new GraphWire(from.Block, from.Connector, to, toConnector));
    }
}
