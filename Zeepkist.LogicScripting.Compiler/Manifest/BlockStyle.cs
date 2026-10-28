namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One look of a block type: the block it places and its footprint. A <c>style:</c> argument picks it by name.
/// </summary>
public sealed class BlockStyle
{
    public BlockStyle(string name, int blockId, int length)
    {
        Name = name;
        BlockId = blockId;
        Length = length;
    }

    public string Name { get; }
    public int BlockId { get; }

    /// <summary>
    /// Footprint along the block's own +Z (the direction Signals flow), in 16-unit cells.
    /// </summary>
    public int Length { get; }
}
