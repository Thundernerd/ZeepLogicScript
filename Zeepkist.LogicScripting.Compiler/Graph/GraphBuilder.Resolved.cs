using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a name or expression stands for: an <see cref="OutputValue"/>, a <see cref="BlockValue"/> or
    /// <see cref="FunctionOutputs"/> to pick an output from, a <see cref="DeferredValue"/>, or a <see cref="FailedValue"/>.
    /// </summary>
    private abstract class Resolved
    {
        /// <summary>
        /// The placed block the value comes from, or null when it doesn't come from one.
        /// </summary>
        public virtual BlockNode PlacedBlock => null;

        /// <summary>
        /// The value as what feeds an input, or a <see cref="FailedValue"/> once it's reported, at
        /// <paramref name="origin"/> or before, why it isn't one output.
        /// </summary>
        public abstract InputFeed ToFeed(GraphBuilder builder, Expression origin);

        /// <summary>
        /// The output <paramref name="member"/> picks from the value, or a <see cref="FailedValue"/> after reporting
        /// why there's none.
        /// </summary>
        public abstract Resolved SelectMember(GraphBuilder builder, MemberExpression member);
    }
}
