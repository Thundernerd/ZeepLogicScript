using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed class BlockNode
{
    private readonly List<Setting> _settings;

    public BlockNode(int index, BlockType type, BlockStyle style, IEnumerable<Setting> settings)
    {
        Index = index;
        Type = type;
        Style = style;
        _settings = [.. settings];
        Outputs = type.GetOutputs(_settings);
    }

    /// <summary>
    /// A block already in the level, reached through a Placed Block Reference.
    /// </summary>
    public BlockNode(string uid, BlockType type, BlockStyle style, PlacedBlock placed)
        : this(-1, type, style, placed.Settings)
    {
        Uid = uid;
        Placed = placed;
    }

    /// <summary>
    /// Position in the graph; blocks are added in Script line order. -1 for a placed block, which isn't laid out.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The placed block's UID, or null for a block the Compile places.
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// What the level says about the placed block, or null for a block the Compile places.
    /// </summary>
    public PlacedBlock Placed { get; }

    public bool IsPlaced => Placed != null;

    public BlockType Type { get; }
    /// <summary>
    /// Null for a placed block the Block Manifest doesn't describe.
    /// </summary>
    public BlockStyle Style { get; }
    public IReadOnlyList<Setting> Settings => _settings;

    /// <summary>
    /// The outputs in this block's mode.
    /// </summary>
    public IReadOnlyList<BlockConnector> Outputs { get; }

    public bool HasSetting(string key)
    {
        return _settings.Exists(s => s.Key == key);
    }

    /// <summary>
    /// Adds a Setting the call didn't give, such as one from a <c>default</c> line.
    /// </summary>
    public void AddSetting(Setting setting)
    {
        _settings.Add(setting);
    }
}
