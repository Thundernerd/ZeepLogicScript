namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A block selected in the level, for <see cref="PlacedBlockReferenceInsert"/>.
/// </summary>
public sealed class SelectedBlock
{
    public SelectedBlock(string uid, int blockId)
    {
        Uid = uid;
        BlockId = blockId;
    }

    public string Uid { get; }
    public int BlockId { get; }
}
