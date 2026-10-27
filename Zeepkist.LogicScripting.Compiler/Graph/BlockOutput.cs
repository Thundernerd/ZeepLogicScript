namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// An output Connector of a block: what a name or an expression stands for.
/// </summary>
internal readonly struct BlockOutput
{
    public BlockOutput(BlockNode block, int connector)
    {
        Block = block;
        Connector = connector;
    }

    public BlockNode Block { get; }
    public int Connector { get; }
}
