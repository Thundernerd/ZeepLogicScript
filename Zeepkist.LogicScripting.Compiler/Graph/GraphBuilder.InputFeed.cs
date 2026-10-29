namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What feeds an input: an output, or a name inside <c>delay(...)</c> that isn't defined yet.
    /// </summary>
    private readonly struct InputFeed
    {
        private InputFeed(BlockOutput? output, DeferredValue deferred)
        {
            Output = output;
            Deferred = deferred;
        }

        public BlockOutput? Output { get; }
        public DeferredValue Deferred { get; }

        public static InputFeed Of(BlockOutput output)
        {
            return new InputFeed(output, null);
        }

        public static InputFeed Of(DeferredValue deferred)
        {
            return new InputFeed(null, deferred);
        }
    }
}
