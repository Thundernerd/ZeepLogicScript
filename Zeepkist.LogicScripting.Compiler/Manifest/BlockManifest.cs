using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// The hand-written table of Logic Block types the compiler can place.
/// </summary>
public static class BlockManifest
{
    public const string GeneratorPowerKey = "lbp";
    public const string LogicOperatorKey = "xmo";
    public const int LogicOperatorAnd = 0;

    public static readonly BlockType Generator = new(
        "generator",
        2414,
        1,
        [],
        [new BlockConnector("out", 0)]);

    public static readonly BlockType Logic = new(
        "logic",
        2420,
        3,
        [new BlockConnector("a", 0), new BlockConnector("b", 1), new BlockConnector("op_in", 2)],
        [new BlockConnector("out", 3)]);

    public static readonly BlockType PowerDisplay = new(
        "power_display",
        2415,
        2,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)]);

    private static readonly Dictionary<string, BlockType> _functions = new()
    {
        [Generator.Function] = Generator,
        [Logic.Function] = Logic,
        [PowerDisplay.Function] = PowerDisplay
    };

    public static bool TryGetFunction(string name, out BlockType type)
    {
        return _functions.TryGetValue(name, out type);
    }
}
