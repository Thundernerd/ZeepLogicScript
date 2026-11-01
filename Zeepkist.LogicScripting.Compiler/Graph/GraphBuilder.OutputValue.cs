using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// One output, to use as a value.
    /// </summary>
    private sealed class OutputValue : InputFeed
    {
        public OutputValue(BlockOutput output)
        {
            Output = output;
        }

        public OutputValue(BlockNode block, int connector)
            : this(new BlockOutput(block, connector))
        {
        }

        public BlockOutput Output { get; }

        public override BlockNode PlacedBlock => Output.Block.IsPlaced ? Output.Block : null;

        public override Resolved SelectMember(GraphBuilder builder, MemberExpression member)
        {
            return builder.Fail(member, $"'{Describe(member.Target)}' has a single output. Use it without .{member.Output}");
        }

        public override void Connect(GraphBuilder builder, BlockNode to, int toConnector)
        {
            builder._graph.AddWire(Output, to, toConnector);
        }
    }
}
