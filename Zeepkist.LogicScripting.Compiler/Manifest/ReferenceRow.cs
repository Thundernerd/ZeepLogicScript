namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// One labeled line of a Reference entry or the hover box: a part and its names, a Setting and what it accepts, or a
/// mode and its outputs.
/// </summary>
public sealed class ReferenceRow
{
    public ReferenceRow(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}
