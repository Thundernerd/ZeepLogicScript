namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One named line of a <see cref="BlockReferenceEntry"/>: a Setting and what it accepts, or a mode and its outputs.
/// </summary>
public sealed class BlockReferenceRow
{
    public BlockReferenceRow(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}
