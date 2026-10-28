using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// The blocks a Script describes and the Wires between them, before layout.
/// </summary>
internal sealed class BlockGraph
{
    /// <summary>
    /// The blocks the Compile places.
    /// </summary>
    public List<BlockNode> Blocks { get; } = [];

    public List<GraphWire> Wires { get; } = [];

    /// <summary>
    /// Wires with at least one end on a placed block. Layout leaves them out, since placed blocks never move.
    /// </summary>
    public List<GraphWire> PlacedWires { get; } = [];

    /// <summary>
    /// Every <c>placed("UID")</c> the Script was read far enough to find.
    /// </summary>
    public List<PlacedBlockReference> PlacedBlockReferences { get; } = [];

    public BlockNode AddBlock(BlockType type, BlockStyle style, params Setting[] settings)
    {
        var block = new BlockNode(Blocks.Count, type, style, settings);
        Blocks.Add(block);
        return block;
    }

    public GraphWire AddWire(BlockOutput from, BlockNode to, int toConnector)
    {
        var wire = new GraphWire(from.Block, from.Connector, to, toConnector);
        if (from.Block.IsPlaced || to.IsPlaced)
            PlacedWires.Add(wire);
        else
            Wires.Add(wire);

        return wire;
    }
}
