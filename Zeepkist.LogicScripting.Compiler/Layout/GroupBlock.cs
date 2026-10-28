using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Layout;

/// <summary>
/// A Block Group block as it stands in the level, for <see cref="BlockGroupPlacement"/>.
/// </summary>
public readonly struct GroupBlock
{
    private static readonly BlockStyle _oneCell = new(null, 0, 1);

    private readonly BlockStyle _style;

    /// <param name="x">World X of the block's pivot.</param>
    /// <param name="z">World Z of the block's pivot.</param>
    /// <param name="yaw">Rotation about Y in degrees, for the group's yaw.</param>
    /// <param name="style">The block's footprint; null for a block the Block Manifest doesn't know, taken as one cell.</param>
    /// <param name="isReshaped">See <see cref="IsReshaped"/>.</param>
    public GroupBlock(float x, float z, float yaw, BlockStyle style, bool isReshaped = false)
    {
        X = x;
        Z = z;
        Yaw = yaw;
        _style = style;
        IsReshaped = isReshaped;
    }

    public float X { get; }
    public float Z { get; }
    public float Yaw { get; }
    public BlockStyle Style => _style ?? _oneCell;

    /// <summary>
    /// Whether the player scaled or tilted the block by hand; the group's yaw leaves it out, since a tilted block's yaw
    /// is no quarter turn.
    /// </summary>
    public bool IsReshaped { get; }
}
