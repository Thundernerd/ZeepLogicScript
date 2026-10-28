namespace TNRD.Zeepkist.LogicScripting.Blocks;

/// <summary>
/// A Wire for <see cref="BlockBatch"/> to add, from an output Connector to an input Connector, with blocks by UID.
/// Separate from the compiler's wire, whose ends can be blocks not created yet, so the batch needs no Script types.
/// </summary>
internal sealed class BlockWire
{
    public BlockWire(string fromUid, int fromConnector, string toUid, int toConnector)
    {
        FromUid = fromUid;
        FromConnector = fromConnector;
        ToUid = toUid;
        ToConnector = toConnector;
    }

    public string FromUid { get; }
    public int FromConnector { get; }
    public string ToUid { get; }
    public int ToConnector { get; }
}
