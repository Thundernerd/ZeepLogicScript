namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A Wire from an output Connector to an input Connector, with blocks given by their index in the result.
/// </summary>
public sealed class CompiledWire
{
    public CompiledWire(int from, int fromConnector, int to, int toConnector)
    {
        From = from;
        FromConnector = fromConnector;
        To = to;
        ToConnector = toConnector;
    }

    public int From { get; }
    public int FromConnector { get; }
    public int To { get; }
    public int ToConnector { get; }
}
