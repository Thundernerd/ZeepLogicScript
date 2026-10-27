using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// The block an operator places: its block type and the Setting that picks the operation on it.
/// </summary>
internal sealed class OperatorBlock
{
    public OperatorBlock(BlockType type, Setting setting)
    {
        Type = type;
        Setting = setting;
    }

    public BlockType Type { get; }

    /// <summary>
    /// The Setting that picks the operation on <see cref="Type"/>.
    /// </summary>
    public Setting Setting { get; }
}
