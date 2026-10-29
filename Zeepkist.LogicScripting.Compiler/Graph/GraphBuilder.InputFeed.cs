namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What feeds an input: an <see cref="OutputValue"/>, or a <see cref="DeferredValue"/> to wire once it's defined.
    /// </summary>
    private abstract class InputFeed : Resolved
    {
    }
}
