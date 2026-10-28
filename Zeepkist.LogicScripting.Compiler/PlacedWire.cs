namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// A Wire with at least one end on a block already in the level.
/// </summary>
public sealed class PlacedWire
{
    public PlacedWire(WireEnd from, int fromConnector, WireEnd to, int toConnector)
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
