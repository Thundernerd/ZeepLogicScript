using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// Compares a Block Manifest entry with the game's block, so a game update that changes a block turns that block
/// off instead of letting Compile wire it wrongly.
/// </summary>
public static class BlockManifestCheck
{
    /// <summary>
    /// How the game's block differs from <paramref name="type"/>'s Connectors; empty when it matches.
    /// </summary>
    public static List<string> Compare(BlockType type, IReadOnlyList<GameConnector> connectors)
    {
        List<string> problems = [];
        var expected = type.Inputs.Select(c => (c.Index, Direction: ConnectorDirection.Input))
            .Concat(type.Outputs.Select(c => (c.Index, Direction: ConnectorDirection.Output)))
            .ToList();

        if (connectors.Count != expected.Count)
            problems.Add($"has {connectors.Count} Connectors, expected {expected.Count}");

        foreach (var (index, direction) in expected)
        {
            var found = connectors.Where(c => c.Index == index).ToList();
            if (found.Count == 0)
                problems.Add($"has no Connector {index}");
            else if (found.Count > 1)
                problems.Add($"has {found.Count} Connectors numbered {index}");
            else if (found[0].Direction != direction)
                problems.Add($"Connector {index} is an {Describe(found[0].Direction)}, expected an {Describe(direction)}");
        }

        return problems;
    }

    private static string Describe(ConnectorDirection direction)
    {
        return direction == ConnectorDirection.Input ? "input" : "output";
    }
}
