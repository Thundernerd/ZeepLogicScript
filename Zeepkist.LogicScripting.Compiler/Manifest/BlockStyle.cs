namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One look of a block type: the block it places and its footprint. A <c>style:</c> argument picks it by name.
/// </summary>
public sealed class BlockStyle
{
    public BlockStyle(string name, int blockId, int length, int minX = 0, int maxX = 0)
    {
        Name = name;
        BlockId = blockId;
        Length = length;
        MinX = minX;
        MaxX = maxX;
    }

    public string Name { get; }
    public int BlockId { get; }

    /// <summary>
    /// Footprint along the block's own +Z (the direction Signals flow), in 16-unit cells.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// With <see cref="MaxX"/>, the footprint's first and last cell along the block's own X, relative to its pivot
    /// cell: -1 for a block that is a cell wider toward -X.
    /// </summary>
    public int MinX { get; }

    public int MaxX { get; }

    public int Width => MaxX - MinX + 1;
}
