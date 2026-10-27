namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a call's argument wires an input to, and whether it does so by position or by name.
    /// </summary>
    private readonly struct WiredInput
    {
        private WiredInput(InputFeed? feed, bool isByPosition)
        {
            Feed = feed;
            IsByPosition = isByPosition;
        }

        /// <summary>
        /// Null when the argument failed to evaluate.
        /// </summary>
        public InputFeed? Feed { get; }
        public bool IsByPosition { get; }

        public static WiredInput ByPosition(InputFeed? feed)
        {
            return new WiredInput(feed, true);
        }

        public static WiredInput ByName(InputFeed? feed)
        {
            return new WiredInput(feed, false);
        }
    }
}
