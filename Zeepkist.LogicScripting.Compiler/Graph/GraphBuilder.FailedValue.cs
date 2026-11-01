using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// A name or expression that stands for nothing, after reporting why. It passes through wherever a value goes
    /// without reporting again, so one mistake gives one error.
    /// </summary>
    private sealed class FailedValue : InputFeed
    {
        public static readonly FailedValue Instance = new();

        private FailedValue()
        {
        }

        public override Resolved SelectMember(GraphBuilder builder, MemberExpression member)
        {
            return this;
        }

        public override void Connect(GraphBuilder builder, BlockNode to, int toConnector)
        {
        }
    }
}
