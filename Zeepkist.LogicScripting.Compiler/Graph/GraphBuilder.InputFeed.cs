using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What feeds an input: an <see cref="OutputValue"/>, a <see cref="DeferredValue"/> to wire once it's defined, or a
    /// <see cref="FailedValue"/> that wires nothing.
    /// </summary>
    private abstract class InputFeed : Resolved
    {
        public sealed override InputFeed ToFeed(GraphBuilder builder, Expression origin)
        {
            return this;
        }

        public abstract void Connect(GraphBuilder builder, BlockNode to, int toConnector);
    }
}
