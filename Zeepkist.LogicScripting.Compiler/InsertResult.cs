namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// The Script after <see cref="PlacedBlockReferenceInsert.Insert"/>.
/// </summary>
public sealed class InsertResult
{
    public InsertResult(string text, int caret, int insertedCount, int skippedCount, int skippedGroupBlockCount)
    {
        Text = text;
        Caret = caret;
        InsertedCount = insertedCount;
        SkippedCount = skippedCount;
        SkippedGroupBlockCount = skippedGroupBlockCount;
    }

    public string Text { get; }

    /// <summary>
    /// Where the caret goes: right after the inserted lines.
    /// </summary>
    public int Caret { get; }

    public int InsertedCount { get; }

    /// <summary>
    /// Blocks the Script already refers to.
    /// </summary>
    public int SkippedCount { get; }

    /// <summary>
    /// Blocks in a Block Group, which a Script can't refer to.
    /// </summary>
    public int SkippedGroupBlockCount { get; }
}
