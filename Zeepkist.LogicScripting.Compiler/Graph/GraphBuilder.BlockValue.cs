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
    }
}
