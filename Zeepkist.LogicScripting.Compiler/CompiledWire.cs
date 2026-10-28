namespace TNRD.Zeepkist.LogicScripting.Compiler;

public sealed class CompiledWire
{
    public CompiledWire(WireEnd from, int fromConnector, WireEnd to, int toConnector)
    {
        From = from;
        FromConnector = fromConnector;
        To = to;
        ToConnector = toConnector;
    }

    public WireEnd From { get; }
    public int FromConnector { get; }
    public WireEnd To { get; }
    public int ToConnector { get; }
}
