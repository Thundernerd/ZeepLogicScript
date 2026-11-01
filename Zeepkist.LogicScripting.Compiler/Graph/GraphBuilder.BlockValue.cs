using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// A block without a single output to stand for: one with several, to pick from with a dot, or a placed block
    /// with none, to wire into.
    /// </summary>
    private sealed class BlockValue : Resolved
    {
        public BlockValue(BlockNode block)
        {
            Block = block;
        }

        public BlockNode Block { get; }

        public override BlockNode PlacedBlock => Block.IsPlaced ? Block : null;

        public override InputFeed ToFeed(GraphBuilder builder, Expression origin)
        {
            if (Block.Outputs.Count == 0)
                builder.Error(origin, $"'{Describe(origin)}' has no output to use as a value");
            else
                builder.ReportSeveralOutputs(origin, Block.Outputs[0].Name);

            return FailedValue.Instance;
        }

        public override Resolved SelectMember(GraphBuilder builder, MemberExpression member)
        {
            if (Block.Outputs.Count == 0)
                return builder.Fail(member, $"'{Describe(member.Target)}' has no outputs");

            List<string> names = [];
            foreach (var output in Block.Outputs)
            {
                if (output.Name == member.Output)
                    return new OutputValue(Block, output.Index);

                names.Add(output.Name);
            }

            var type = Block.Type;
            var mode = type.Modes == null
                ? string.Empty
                : $" in {type.Modes.Setting.Name} {type.GetModeName(Block.Settings)}";
            return builder.Fail(member, $"'{type.Function}' has no output '{member.Output}'{mode}. Use {string.Join(", ", names)}");
        }
    }
}
