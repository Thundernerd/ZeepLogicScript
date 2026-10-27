namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a name or expression stands for: one output, or a multi-output block to pick an output from.
    /// </summary>
    private readonly struct Resolved
    {
        private Resolved(BlockNode block, BlockOutput? output)
        {
            Block = block;
            Output = output;
        }

        public BlockNode Block { get; }

        /// <summary>
        /// Null for a multi-output block.
        /// </summary>
        public BlockOutput? Output { get; }

        public static Resolved? Of(BlockOutput? output)
        {
            return output == null ? null : new Resolved(output.Value.Block, output);
        }

        public static Resolved Of(BlockNode block)
        {
            return new Resolved(block, null);
        }
    }
}
