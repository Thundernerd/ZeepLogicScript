using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// One <c>placed("UID")</c> in a Script.
/// </summary>
public sealed class PlacedBlockReference
{
    public PlacedBlockReference(Position position, string name, string uid, BlockType type)
    {
        Position = position;
        Name = name;
        Uid = uid;
        Type = type;
    }

    public Position Position { get; }

    /// <summary>
    /// The name it's defined as, or null when it's written inside another expression.
    /// </summary>
    public string Name { get; }

    public string Uid { get; }

    /// <summary>
    /// The block's type, <see cref="BlockManifest.OtherBlock"/> for a block the Block Manifest doesn't describe, or
    /// null when no block in the level has the UID.
    /// </summary>
    public BlockType Type { get; }
}
