namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed class GraphWire
{
    public GraphWire(BlockNode from, int fromConnector, BlockNode to, int toConnector)
    {
        From = from;
        FromConnector = fromConnector;
        To = to;
        ToConnector = toConnector;
    }

    public BlockNode From { get; }
    public int FromConnector { get; }
    public BlockNode To { get; }
    public int ToConnector { get; }
}
