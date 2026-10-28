namespace TNRD.Zeepkist.LogicScripting.Blocks;

internal static partial class BlockBatch
{
    private sealed class ChangedBlock
    {
        public ChangedBlock(BlockProperties block, string before)
        {
            Block = block;
            Before = before;
        }

        public BlockProperties Block { get; }
        public string Before { get; }
    }
}
