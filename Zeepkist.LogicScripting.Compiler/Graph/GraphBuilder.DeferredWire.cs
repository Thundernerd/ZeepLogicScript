namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private sealed class DeferredWire
    {
        public DeferredWire(DeferredValue from, BlockNode to, int toConnector)
        {
            From = from;
            To = to;
            ToConnector = toConnector;
        }

        public DeferredValue From { get; }
        public BlockNode To { get; }
        public int ToConnector { get; }
    }
}
