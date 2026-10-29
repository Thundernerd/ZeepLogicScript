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
    }
}
