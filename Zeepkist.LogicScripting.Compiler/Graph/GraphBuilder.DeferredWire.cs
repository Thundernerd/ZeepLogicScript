using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private sealed class DeferredWire
    {
        public DeferredWire(Expression from, BlockNode to, int toConnector)
        {
            From = from;
            To = to;
            ToConnector = toConnector;
        }

        public Expression From { get; }
        public BlockNode To { get; }
        public int ToConnector { get; }
    }
}
