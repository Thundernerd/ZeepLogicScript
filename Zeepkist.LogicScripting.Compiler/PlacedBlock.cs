using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A block already in the level, as a Placed Block Reference sees it.
/// </summary>
public sealed class PlacedBlock
{
    public PlacedBlock(
        int blockId,
        IReadOnlyList<Setting> settings = null,
        IReadOnlyCollection<int> wiredInputs = null,
        IReadOnlyCollection<string> downstream = null,
        bool isInBlockGroup = false)
    {
        BlockId = blockId;
        Settings = settings ?? [];
        WiredInputs = wiredInputs ?? [];
        Downstream = downstream ?? [];
        IsInBlockGroup = isInBlockGroup;
    }

    public int BlockId { get; }

    /// <summary>
    /// The block's Settings; only the one that picks a mode matters, for its output names.
    /// </summary>
    public IReadOnlyList<Setting> Settings { get; }

    /// <summary>
    /// Indices of the input Connectors that already have a Wire, not counting Wires from the compiling Script's own
    /// Block Group, which a recompile replaces.
    /// </summary>
    public IReadOnlyCollection<int> WiredInputs { get; }

    /// <summary>
    /// UIDs of the blocks this block's Signal reaches in the same Tick through the level's Wires, so not past a Delay.
    /// </summary>
    public IReadOnlyCollection<string> Downstream { get; }

    /// <summary>
    /// Whether a Compile placed the block, as a member of some Script's Block Group.
    /// </summary>
    public bool IsInBlockGroup { get; }
}
