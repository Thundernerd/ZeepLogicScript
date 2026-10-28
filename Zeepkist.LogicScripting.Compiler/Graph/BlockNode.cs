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
    /// Position in the graph; blocks are added in Script line order.
    /// </summary>
    public int Index { get; }

    public BlockType Type { get; }
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
