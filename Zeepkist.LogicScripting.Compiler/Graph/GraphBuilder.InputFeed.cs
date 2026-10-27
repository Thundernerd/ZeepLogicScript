using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What feeds an input: an output, or a name inside <c>delay(...)</c> that isn't defined yet.
    /// </summary>
    private readonly struct InputFeed
    {
        private InputFeed(BlockOutput? output, Expression expression)
        {
            Output = output;
            Expression = expression;
        }

        public BlockOutput? Output { get; }
        public Expression Expression { get; }

        public static InputFeed Of(BlockOutput output)
        {
            return new InputFeed(output, null);
        }

        public static InputFeed Deferred(Expression expression)
        {
            return new InputFeed(null, expression);
        }
    }
}
