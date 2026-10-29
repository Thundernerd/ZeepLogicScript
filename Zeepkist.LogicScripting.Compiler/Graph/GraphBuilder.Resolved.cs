namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a name or expression stands for: an <see cref="OutputValue"/>, a <see cref="BlockValue"/> or
    /// <see cref="FunctionOutputs"/> to pick an output from, or a <see cref="DeferredValue"/>.
    /// </summary>
    private abstract class Resolved
    {
    }
}
