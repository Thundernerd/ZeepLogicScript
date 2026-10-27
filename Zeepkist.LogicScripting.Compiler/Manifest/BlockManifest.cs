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

    public static readonly BlockSetting MathOperator = BlockSetting.Option(
        "op",
        "xmo",
        "add", "subtract", "multiply", "divide", "modulo",
        "less", "greater", "less_equal", "greater_equal", "equal", "not_equal",
        "min", "max", "pow", "log", "abs", "sign", "repeat", "ping_pong",
        "sin", "cos", "tan", "asin", "acos", "atan", "atan2", "sqrt",
        "divide_scaled");

    public static readonly BlockSetting LogicOperator = BlockSetting.Option(
        "op",
        LogicOperatorKey,
        "and", "or", "nand", "nor", "xor", "xnor");

    public static readonly BlockSetting DigitizerInvert = BlockSetting.Bool("invert", "xt1");

    public static readonly BlockType Generator = new(
        "generator",
        2414,
        1,
        [],
        [new BlockConnector("out", 0)],
        BlockSetting.Number("power", GeneratorPowerKey));

    public static readonly BlockType Death = new(
        "death",
        2427,
        1,
        [new BlockConnector("trigger", 0)],
        []);

    public static readonly BlockType Delay = new(
        "delay",
        2428,
        1,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)],
        BlockSetting.Number("ticks", "ldl", isRequired: true));

    public static readonly BlockType Demultiplexer = new(
        "demultiplexer",
        2437,
        3,
        [new BlockConnector("select", 4), new BlockConnector("data", 5)],
        [new BlockConnector("ch1", 0), new BlockConnector("ch2", 1), new BlockConnector("ch3", 2), new BlockConnector("ch4", 3)]);

    public static readonly BlockType Digitizer = new(
        "digitizer",
        2418,
        1,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)],
        DigitizerInvert);

    public static readonly BlockType Logic = new(
        "logic",
        2420,
        3,
        [new BlockConnector("a", 0), new BlockConnector("b", 1), new BlockConnector("op_in", 2)],
        [new BlockConnector("out", 3)],
        LogicOperator);

    public static readonly BlockType Math = new(
        "math",
        2416,
        3,
        [new BlockConnector("a", 0), new BlockConnector("b", 1), new BlockConnector("op_in", 2)],
        [new BlockConnector("out", 3)],
        MathOperator);

    public static readonly BlockType PlayerInput = new(
        "player_input",
        2438,
        2,
        [],
        [
            new BlockConnector("arms_up", 0),
            new BlockConnector("brake", 1),
            new BlockConnector("left", 2),
            new BlockConnector("right", 3),
            new BlockConnector("analog_left", 4),
            new BlockConnector("analog_right", 5),
            new BlockConnector("horn", 6)
        ]);

    public static readonly BlockType PowerDisplay = new(
        "power_display",
        2415,
        2,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)]);

    private static readonly Dictionary<string, BlockType> _functions = new()
    {
        [Death.Function] = Death,
        [Delay.Function] = Delay,
        [Demultiplexer.Function] = Demultiplexer,
        [Digitizer.Function] = Digitizer,
        [Generator.Function] = Generator,
        [Logic.Function] = Logic,
        [Math.Function] = Math,
        [PlayerInput.Function] = PlayerInput,
        [PowerDisplay.Function] = PowerDisplay
    };

    public static bool TryGetFunction(string name, out BlockType type)
    {
        return _functions.TryGetValue(name, out type);
    }
}
