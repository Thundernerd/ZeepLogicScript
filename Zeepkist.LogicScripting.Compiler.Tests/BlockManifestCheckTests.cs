using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using Xunit;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

public class BlockManifestCheckTests
{
    private static readonly GameConnector[] _logicConnectors =
    [
        new(0, ConnectorDirection.Input),
        new(1, ConnectorDirection.Input),
        new(2, ConnectorDirection.Input),
        new(3, ConnectorDirection.Output)
    ];

    [Fact]
    public void MatchingBlock_HasNoProblems()
    {
        Assert.Empty(BlockManifestCheck.Compare(BlockManifest.Logic, _logicConnectors));
    }

    [Fact]
    public void ConnectorsInAnyOrder_Match()
    {
        GameConnector[] connectors = [_logicConnectors[3], _logicConnectors[1], _logicConnectors[0], _logicConnectors[2]];

        Assert.Empty(BlockManifestCheck.Compare(BlockManifest.Logic, connectors));
    }

    [Fact]
    public void MissingConnector_IsReported()
    {
        GameConnector[] connectors = [_logicConnectors[0], _logicConnectors[1], _logicConnectors[3]];

        Assert.Equal(
            ["has 3 Connectors, expected 4", "has no Connector 2"],
            BlockManifestCheck.Compare(BlockManifest.Logic, connectors));
    }

    [Fact]
    public void ExtraConnector_IsReported()
    {
        GameConnector[] connectors = [.. _logicConnectors, new(4, ConnectorDirection.Input)];

        Assert.Equal(["has 5 Connectors, expected 4"], BlockManifestCheck.Compare(BlockManifest.Logic, connectors));
    }

    [Fact]
    public void SwappedDirection_IsReported()
    {
        GameConnector[] connectors = [.. _logicConnectors[..3], new(3, ConnectorDirection.Input)];

        Assert.Equal(
            ["Connector 3 is an input, expected an output"],
            BlockManifestCheck.Compare(BlockManifest.Logic, connectors));
    }

    [Fact]
    public void DuplicateIndex_IsReported()
    {
        GameConnector[] connectors = [.. _logicConnectors[..3], new(2, ConnectorDirection.Output)];

        Assert.Equal(
            ["has 2 Connectors numbered 2", "has no Connector 3"],
            BlockManifestCheck.Compare(BlockManifest.Logic, connectors));
    }
}
