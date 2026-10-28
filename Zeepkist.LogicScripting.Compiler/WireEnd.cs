namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// One end of a <see cref="PlacedWire"/>: a block the Compile places, or one already in the level.
/// </summary>
public sealed class WireEnd
{
    private WireEnd(int block, string uid)
    {
        Block = block;
        Uid = uid;
    }

    /// <summary>
    /// The block's index in <see cref="CompileResult.Blocks"/>, or -1 for a placed block.
    /// </summary>
    public int Block { get; }

    /// <summary>
    /// The placed block's UID, or null for a block the Compile places.
    /// </summary>
    public string Uid { get; }

    public bool IsPlaced => Uid != null;

    public static WireEnd Compiled(int block)
    {
        return new WireEnd(block, null);
    }

    public static WireEnd Placed(string uid)
    {
        return new WireEnd(-1, uid);
    }
}
