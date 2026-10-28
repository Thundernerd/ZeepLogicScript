namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A block selected in the level, for <see cref="PlacedBlockReferenceInsert"/>.
/// </summary>
public sealed class SelectedBlock
{
    public SelectedBlock(string uid, int blockId, bool isInBlockGroup = false)
    {
        Uid = uid;
        BlockId = blockId;
        IsInBlockGroup = isInBlockGroup;
    }

    public string Uid { get; }
    public int BlockId { get; }

    /// <summary>
    /// Whether a Compile placed the block; a Script can't refer to one.
    /// </summary>
    public bool IsInBlockGroup { get; }
}
