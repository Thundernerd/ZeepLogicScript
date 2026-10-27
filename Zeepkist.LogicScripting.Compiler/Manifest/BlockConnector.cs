namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// A named Connector and its zero-based index on the block.
/// </summary>
public sealed class BlockConnector
{
    public BlockConnector(string name, int index)
    {
        Name = name;
        Index = index;
    }

    public string Name { get; }
    public int Index { get; }
}
