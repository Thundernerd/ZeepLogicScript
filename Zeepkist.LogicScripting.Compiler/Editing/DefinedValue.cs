using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What a defined name's value is, when the whole value is one call the editor knows: a Block Function call, a Placed
/// Block Reference to a block in the level, or a call to the Script's own Function.
/// </summary>
public sealed class DefinedValue
{
    private DefinedValue(BlockType type, string mode, bool isPlaced, DefinedFunction callee)
    {
        Type = type;
        Mode = mode;
        IsPlaced = isPlaced;
        Callee = callee;
    }

    /// <summary>
    /// Null for a call to the Script's own Function.
    /// </summary>
    public BlockType Type { get; }

    /// <summary>
    /// The option written for the call's mode Setting (<c>mode: local_velocity</c>), or for a Placed Block Reference, the
    /// one the block has in the level; null when none is written.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Whether the value is a Placed Block Reference, which the Script can call to wire its inputs.
    /// </summary>
    public bool IsPlaced { get; }

    /// <summary>
    /// The Script's own Function the value calls; null for a block.
    /// </summary>
    public DefinedFunction Callee { get; }

    public static DefinedValue BlockFunctionCall(BlockType type, string mode)
    {
        return new DefinedValue(type, mode, false, null);
    }

    public static DefinedValue PlacedBlockReference(BlockType type, string mode)
    {
        return new DefinedValue(type, mode, true, null);
    }

    public static DefinedValue FunctionCall(DefinedFunction callee)
    {
        return new DefinedValue(null, null, false, callee);
    }
}
