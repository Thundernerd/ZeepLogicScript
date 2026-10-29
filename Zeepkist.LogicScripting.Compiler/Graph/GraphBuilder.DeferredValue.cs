using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// A name inside <c>delay(...)</c> that isn't defined yet, to look up in its own scope once every definition has
    /// been visited.
    /// </summary>
    private sealed class DeferredValue : InputFeed
    {
        public DeferredValue(Expression expression, Scope scope)
        {
            Expression = expression;
            Scope = scope;
        }

        public Expression Expression { get; }
        public Scope Scope { get; }
    }
}
