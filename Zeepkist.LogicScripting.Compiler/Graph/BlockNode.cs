using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

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
