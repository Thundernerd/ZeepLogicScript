namespace TNRD.Zeepkist.LogicScripting.Compiler.Storage;

/// <summary>
/// A Block Group in the level whose Script the window doesn't have: the level lost its text, usually because the block
/// carrying it was deleted by hand.
/// </summary>
public sealed class OrphanedGroup
{
    public OrphanedGroup(ScriptId id, int blockCount, Draft draft)
    {
        Id = id;
        BlockCount = blockCount;
        Draft = draft;
    }

    public ScriptId Id { get; }
    public int BlockCount { get; }

    /// <summary>
    /// The Script's local Draft, or null when this machine has none.
    /// </summary>
    public Draft Draft { get; }
}
