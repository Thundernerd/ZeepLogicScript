namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// A Connector as the game's block has it, for <see cref="BlockManifestCheck"/>.
/// </summary>
public readonly struct GameConnector
{
    public GameConnector(int index, ConnectorDirection direction)
    {
        Index = index;
        Direction = direction;
    }

    public int Index { get; }
    public ConnectorDirection Direction { get; }
}
