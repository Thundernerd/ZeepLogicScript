namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// The Script after <see cref="PlacedReferenceInsert.Insert"/>.
/// </summary>
public sealed class InsertResult
{
    public InsertResult(string text, int caret, int inserted, int skipped)
    {
        Text = text;
        Caret = caret;
        Inserted = inserted;
        Skipped = skipped;
    }

    public string Text { get; }

    /// <summary>
    /// Where the caret goes: right after the inserted lines.
    /// </summary>
    public int Caret { get; }

    public int Inserted { get; }

    /// <summary>
    /// Blocks the Script already refers to.
    /// </summary>
    public int Skipped { get; }
}
