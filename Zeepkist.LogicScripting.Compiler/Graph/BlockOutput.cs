namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

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
