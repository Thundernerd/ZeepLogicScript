using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What one call to a Function with several outputs gives, to pick an output from with a dot.
    /// </summary>
    private sealed class FunctionOutputs : Resolved
    {
        public FunctionOutputs(FunctionDefinition function, Dictionary<string, Resolved> values)
        {
            Function = function;
            Values = values;
        }

        public FunctionDefinition Function { get; }

        /// <summary>
        /// A <see cref="FailedValue"/> for an output whose definition failed, which already reported why.
        /// </summary>
        public Dictionary<string, Resolved> Values { get; }

        public override InputFeed ToFeed(GraphBuilder builder, Expression origin)
        {
            builder.ReportSeveralOutputs(origin, Function.Outputs[0].Name);
            return FailedValue.Instance;
        }

        public override Resolved SelectMember(GraphBuilder builder, MemberExpression member)
        {
            if (Values.TryGetValue(member.Output, out var value))
                return value;

            return builder.Fail(member, $"'{builder.GetQualifiedName(Function)}' has no output '{member.Output}'. Use {string.Join(", ", Function.Outputs.Select(o => o.Name))}");
        }
    }
}
