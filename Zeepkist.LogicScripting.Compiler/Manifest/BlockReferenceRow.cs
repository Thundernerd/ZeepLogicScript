namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One line of a <see cref="BlockReferenceEntry"/>: a Setting and what it accepts, or a mode and its outputs.
/// </summary>
public sealed class BlockReferenceRow
{
    public BlockReferenceRow(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Null for the outputs of a block without modes, which are one row.
    /// </summary>
    public string Name { get; }

    public string Value { get; }
}
