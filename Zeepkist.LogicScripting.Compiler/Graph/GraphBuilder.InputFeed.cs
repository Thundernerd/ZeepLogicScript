namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What feeds an input: an <see cref="OutputValue"/>, a <see cref="DeferredValue"/> to wire once it's defined, or a
    /// <see cref="FailedValue"/> that wires nothing.
    /// </summary>
    private abstract class InputFeed : Resolved
    {
    }
}
