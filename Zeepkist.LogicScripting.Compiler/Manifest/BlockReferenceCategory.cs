using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One <see cref="BlockCategory"/> as the window's Reference tab shows it.
/// </summary>
public sealed class BlockReferenceCategory
{
    public BlockReferenceCategory(BlockCategory category, IReadOnlyList<BlockReferenceEntry> entries)
    {
        Category = category;
        Entries = entries;
    }

    public BlockCategory Category { get; }

    /// <summary>
    /// Sorted by Block Function name.
    /// </summary>
    public IReadOnlyList<BlockReferenceEntry> Entries { get; }
}
