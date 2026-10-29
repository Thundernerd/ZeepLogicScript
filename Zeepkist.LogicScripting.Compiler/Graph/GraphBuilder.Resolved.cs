namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a name or expression stands for: one output, a multi-output block or Function call to pick an output from,
    /// or a name inside <c>delay(...)</c> that isn't defined yet, passed into a Function.
    /// </summary>
    private readonly struct Resolved
    {
        private Resolved(BlockNode block, BlockOutput? output, FunctionOutputs functionOutputs, DeferredValue deferred)
        {
            Block = block;
            Output = output;
            FunctionOutputs = functionOutputs;
            Deferred = deferred;
        }

        /// <summary>
        /// Null for a Function call or a deferred value.
        /// </summary>
        public BlockNode Block { get; }

        /// <summary>
        /// Null for a multi-output block, a Function call with several outputs, or a deferred value.
        /// </summary>
        public BlockOutput? Output { get; }

        public FunctionOutputs FunctionOutputs { get; }
        public DeferredValue Deferred { get; }

        public static Resolved? Of(BlockOutput? output)
        {
            return output == null ? null : new Resolved(output.Value.Block, output, null, null);
        }

        public static Resolved Of(BlockNode block)
        {
            return new Resolved(block, null, null, null);
        }

        public static Resolved Of(FunctionOutputs functionOutputs)
        {
            return new Resolved(null, null, functionOutputs, null);
        }

        public static Resolved Of(DeferredValue deferred)
        {
            return new Resolved(null, null, null, deferred);
        }
    }
}
