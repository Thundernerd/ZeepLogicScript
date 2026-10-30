using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

/// <summary>
/// The hand-written table of Logic Block types: the Block Functions a Script can call, and the placed-only types it
/// can only refer to. Option words follow the editor's list labels, in the order the editor stores them.
/// </summary>
public static class BlockManifest
{
    public const string GeneratorPowerKey = "lbp";
    public const string LogicOperatorKey = "xmo";

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

    /// <summary>
    /// <c>player:</c> on blocks that read one player's car.
    /// </summary>
    public static readonly BlockSetting OnePlayer = BlockSetting.Player("xpr", hasEveryone: false);

    /// <summary>
    /// <c>player:</c> on blocks that act on everyone, or one player.
    /// </summary>
    public static readonly BlockSetting PlayerOrEveryone = BlockSetting.Player("xpr", hasEveryone: true);

    public static readonly BlockSetting DigitizerInvert = BlockSetting.Bool("invert", "xt1");

    public static readonly BlockSetting SimpleTelemetryMode = BlockSetting.Option(
        "mode",
        "xbs",
        "basic", "local_velocity", "global_velocity");

    public static readonly BlockSetting RandomMinimum = BlockSetting.Number("min", "xv1");
    public static readonly BlockSetting RandomMaximum = BlockSetting.Number("max", "xv2");

    public static readonly BlockType Generator = new(
        "generator",
        BlockCategory.Inputs,
        2414,
        1,
        [],
        [new BlockConnector("out", 0)],
        BlockSetting.Number("power", GeneratorPowerKey));

    public static readonly BlockType Logic = new(
        "logic",
        BlockCategory.Operators,
        2420,
        3,
        Numbered(0, "a", "b", "op_in"),
        [new BlockConnector("out", 3)],
        LogicOperator);

    public static readonly BlockType Math = new(
        "math",
        BlockCategory.Operators,
        2416,
        3,
        Numbered(0, "a", "b", "op_in"),
        [new BlockConnector("out", 3)],
        MathOperator);

    public static readonly BlockType Math3Pins = new(
        "math_3_pins",
        BlockCategory.Operators,
        2453,
        4,
        Numbered(0, "start", "end", "t", "op_in"),
        [new BlockConnector("out", 4)],
        BlockSetting.Option("op", "xmo", "lerp", "smooth_lerp"));

    public static readonly BlockType Digitizer = new(
        "digitizer",
        BlockCategory.Operators,
        2418,
        1,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)],
        DigitizerInvert);

    public static readonly BlockType Transistor = new(
        "transistor",
        BlockCategory.Operators,
        2419,
        2,
        Numbered(0, "data", "gate"),
        [new BlockConnector("out", 2)]);

    public static readonly BlockType Multiplexer = new(
        "multiplexer",
        BlockCategory.Operators,
        2436,
        3,
        Numbered(0, "in1", "in2", "in3", "in4", "select"),
        [new BlockConnector("out", 5)]);

    public static readonly BlockType Demultiplexer = new(
        "demultiplexer",
        BlockCategory.Operators,
        2437,
        3,
        Numbered(4, "select", "data"),
        Numbered(0, "ch1", "ch2", "ch3", "ch4"));

    public static readonly BlockType Delay = new(
        "delay",
        BlockCategory.Operators,
        2428,
        1,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)],
        // 0 ticks, the untouched value, throws every tick in play mode; the block reads ticks as an int, and one
        // too big for it reads as 0
        BlockSetting.Number("ticks", "ldl", isRequired: true, minimum: 1, maximum: int.MaxValue));

    public static readonly BlockType Pulser = new(
        "pulser",
        BlockCategory.Operators,
        2439,
        1,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)],
        BlockSetting.Option("mode", "xpl", "rise", "fall", "any_rise", "any_fall", "increased", "decreased"));

    // The block rolls from min to max, both included; with min above max + 1 it swaps them, so min: 10, max: 1 rolls
    // 2 to 9
    public static readonly BlockType Random = new(
        "random",
        BlockCategory.Operators,
        [new BlockStyle(null, 2425, 1)],
        Numbered(0, "trigger", "max_in"),
        [new BlockConnector("out", 2)],
        [RandomMinimum, RandomMaximum],
        range: new BlockRange(RandomMinimum, RandomMaximum, "max_in"));

    public static readonly BlockType Ticker = new(
        "ticker",
        BlockCategory.Inputs,
        2429,
        1,
        [],
        [new BlockConnector("out", 0)]);

    public static readonly BlockType ResettableTicker = new(
        "resettable_ticker",
        BlockCategory.Operators,
        2433,
        1,
        [new BlockConnector("reset", 0)],
        [new BlockConnector("out", 1)]);

    public static readonly BlockType Timer = new(
        "timer",
        BlockCategory.Operators,
        2444,
        2,
        Numbered(0, "duration", "start"),
        [new BlockConnector("out", 2)]);

    public static readonly BlockType SimpleMemory = new(
        "simple_memory",
        BlockCategory.Operators,
        2431,
        2,
        Numbered(0, "data", "write"),
        [new BlockConnector("out", 2)],
        BlockSetting.Number("initial", "oi2"));

    public static readonly BlockType CheeseMemory = new(
        "cheese_memory",
        BlockCategory.Operators,
        2432,
        2,
        Numbered(0, "data", "write"),
        [new BlockConnector("out", 2)],
        BlockSetting.Number("initial", "oi2"));

    public static readonly BlockType Counter = new(
        "counter",
        BlockCategory.Operators,
        2434,
        2,
        Numbered(0, "increment", "decrement", "reset"),
        [new BlockConnector("out", 3)],
        // The block clamps both on load; 0 turns a reset off
        BlockSetting.Number("max_reset", "ld1", minimum: 0, maximum: int.MaxValue),
        BlockSetting.Number("min_reset", "ld2", minimum: -long.MaxValue, maximum: 0));

    public static readonly BlockType ToggleLatch = new(
        "toggle_latch",
        BlockCategory.Operators,
        2435,
        1,
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)],
        BlockSetting.Bool("initial", "x5"));

    public static readonly BlockType DateTimer = new(
        "date_timer",
        BlockCategory.Inputs,
        [new BlockStyle(null, 2430, 3, maxX: 1)],
        [],
        Numbered(0, "year", "month", "day", "hour", "minute", "second"),
        [BlockSetting.Bool("utc", "utc")]);

    public static readonly BlockType PlayerInput = new(
        "player_input",
        BlockCategory.Inputs,
        [new BlockStyle(null, 2438, 2, minX: -1)],
        [],
        Numbered(0, "arms_up", "brake", "left", "right", "analog_left", "analog_right", "horn"),
        [OnePlayer]);

    public static readonly BlockType SimpleTelemetry = new(
        "simple_telemetry",
        BlockCategory.Inputs,
        [new BlockStyle(null, 2440, 3)],
        [],
        Numbered(0, "speed", "yaw", "up", "state"),
        [OnePlayer, SimpleTelemetryMode],
        new BlockModes(
            SimpleTelemetryMode,
            Numbered(0, "speed", "yaw", "up", "state"),
            Numbered(0, "yaw", "x", "y", "z"),
            Numbered(0, "x", "y", "z", "heading")));

    public static readonly BlockType WheelTelemetry = new(
        "wheel_telemetry",
        BlockCategory.Inputs,
        [new BlockStyle(null, 2441, 3, minX: -1)],
        [],
        Numbered(0, "all", "any", "lf", "rf", "lr", "rr"),
        [OnePlayer, BlockSetting.Option("mode", "xbs", "grounded", "slipping", "surface", "physics_material")]);

    public static readonly BlockType Passthrough = new(
        "passthrough",
        BlockCategory.Operators,
        [
            new BlockStyle("normal", 2426, 1),
            new BlockStyle("skinny_1h", 2505, 1),
            new BlockStyle("skinny_2h", 2506, 1),
            new BlockStyle("skinny_3h", 2507, 1)
        ],
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)]);

    public static readonly BlockType PowerDisplay = new(
        "power_display",
        BlockCategory.Operators,
        [new BlockStyle("normal", 2415, 2), new BlockStyle("compact", 2513, 1)],
        [new BlockConnector("x", 0)],
        [new BlockConnector("out", 1)]);

    public static readonly BlockType Toggler = new(
        "toggler",
        BlockCategory.Outputs,
        2417,
        1,
        [new BlockConnector("x", 0)],
        [],
        BlockSetting.Targets("targets", "xuid"));

    public static readonly BlockType Death = new(
        "death",
        BlockCategory.Outputs,
        2427,
        1,
        [new BlockConnector("trigger", 0)],
        [],
        PlayerOrEveryone);

    public static readonly BlockType Parking = new(
        "parking",
        BlockCategory.Outputs,
        2443,
        1,
        [new BlockConnector("x", 0)],
        [],
        PlayerOrEveryone);

    public static readonly BlockType StateChanger = new(
        "state_changer",
        BlockCategory.Outputs,
        2450,
        1,
        Numbered(0, "trigger", "state"),
        [],
        PlayerOrEveryone);

    // Slightly over two cells wide, centered on its pivot, so it reaches into the cells on both sides
    public static readonly BlockType ForceGiver = new(
        "force_giver",
        BlockCategory.Outputs,
        [new BlockStyle(null, 2446, 3, minX: -1, maxX: 1)],
        Numbered(0, "enable", "x", "y", "z"),
        [],
        [
            PlayerOrEveryone,
            BlockSetting.Option("force_mode", "xfm", "acceleration", "force", "impulse", "velocity_change"),
            BlockSetting.Option("space", "xco", "local", "global", "follow_velocity", "local_rotation", "global_rotation"),
            BlockSetting.Option("particles", "xpt", "none", "booster", "slowster", "feather", "flying", "fan")
        ]);

    // Its ~59 multipliers come later
    public static readonly BlockType PropertyMultiplier = new(
        "property_multiplier",
        BlockCategory.Outputs,
        2449,
        1,
        Numbered(0, "apply", "reset"),
        [],
        PlayerOrEveryone);

    public static readonly BlockType Trigger = PlacedOnly(
        "trigger",
        BlockCategory.Inputs,
        [new BlockStyle(null, 2413, 1)],
        [],
        [new BlockConnector("out", 0)]);

    public static readonly BlockType DistanceSensor = PlacedOnly(
        "distance_sensor",
        BlockCategory.Inputs,
        [new BlockStyle(null, 2442, 3)],
        [],
        Numbered(0, "distance", "x", "y", "z"));

    public static readonly BlockType Lamp = PlacedOnly(
        "lamp",
        BlockCategory.Outputs,
        [new BlockStyle(null, 2448, 1)],
        Numbered(0, "intensity", "red", "green", "blue", "range", "angle"),
        []);

    public static readonly BlockType Cannon = PlacedOnly(
        "cannon",
        BlockCategory.Outputs,
        [new BlockStyle(null, 2451, 1)],
        Numbered(0, "fire", "object"),
        []);

    public static readonly BlockType DynamicMover = PlacedOnly(
        "dynamic_mover",
        BlockCategory.Movers,
        [new BlockStyle("normal", 2452, 1), new BlockStyle("primitives", 2457, 1)],
        [new BlockConnector("go", 0), new BlockConnector("return", 1), new BlockConnector("reverse", 5)],
        Numbered(2, "arrived_target", "arrived_start", "player_on"));

    public static readonly BlockType DynamicRotator = PlacedOnly(
        "dynamic_rotator",
        BlockCategory.Movers,
        [new BlockStyle(null, 2455, 1)],
        Numbered(0, "speed", "stop_at_target", "target_angle"),
        Numbered(3, "arrived", "angle"));

    public static readonly BlockType AdvancedMover = PlacedOnly(
        "advanced_mover",
        BlockCategory.Movers,
        [new BlockStyle("normal", 2456, 1), new BlockStyle("primitives", 2458, 1)],
        Numbered(0, "x", "y", "z", "rot_x", "rot_y", "rot_z"),
        []);

    /// <summary>
    /// Any block the Block Manifest doesn't describe, reached through a Placed Block Reference. It has no Connectors,
    /// so it can only be a Toggler target.
    /// </summary>
    public static readonly BlockType OtherBlock = new("block", null, [], [], [], isPlacedOnly: true);

    /// <summary>
    /// Every type, Block Functions first. The decorative Simple Rotator has no Connectors and isn't one of them.
    /// </summary>
    public static readonly IReadOnlyList<BlockType> All =
    [
        Generator, Logic, Math, Math3Pins, Digitizer, Transistor, Multiplexer, Demultiplexer,
        Delay, Pulser, Random, Ticker, ResettableTicker, Timer,
        SimpleMemory, CheeseMemory, Counter, ToggleLatch,
        DateTimer, PlayerInput, SimpleTelemetry, WheelTelemetry,
        Passthrough, PowerDisplay, Toggler,
        Death, Parking, StateChanger, ForceGiver, PropertyMultiplier,
        Trigger, DistanceSensor, Lamp, Cannon, DynamicMover, DynamicRotator, AdvancedMover
    ];

    private static readonly Dictionary<string, BlockType> _functions = ByFunction(isPlacedOnly: false);
    private static readonly Dictionary<string, BlockType> _placedOnly = ByFunction(isPlacedOnly: true);
    private static readonly Dictionary<int, (BlockType Type, BlockStyle Style)> _blocks = ByBlockId();

    public static IReadOnlyDictionary<string, BlockType> Aliases { get; } = new Dictionary<string, BlockType>
    {
        ["mux"] = Multiplexer,
        ["demux"] = Demultiplexer,
        ["lerp"] = Math3Pins,
        ["door"] = Toggler
    };

    /// <summary>
    /// The Block Function with this name or alias.
    /// </summary>
    public static bool TryGetFunction(string name, out BlockType type)
    {
        return _functions.TryGetValue(name, out type) || Aliases.TryGetValue(name, out type);
    }

    public static bool TryGetPlacedOnly(string name, out BlockType type)
    {
        return _placedOnly.TryGetValue(name, out type);
    }

    public static bool TryGetBlock(int blockId, out BlockType type, out BlockStyle style)
    {
        var found = _blocks.TryGetValue(blockId, out var block);
        type = block.Type;
        style = block.Style;
        return found;
    }

    /// <summary>
    /// The type a Placed Block Reference to a block ID has: its manifest type, or <see cref="OtherBlock"/>.
    /// </summary>
    public static BlockType GetPlacedType(int blockId)
    {
        return TryGetBlock(blockId, out var type, out _) ? type : OtherBlock;
    }

    private static Dictionary<int, (BlockType Type, BlockStyle Style)> ByBlockId()
    {
        Dictionary<int, (BlockType Type, BlockStyle Style)> blocks = [];
        foreach (var type in All)
        {
            foreach (var style in type.Styles)
            {
                blocks.Add(style.BlockId, (type, style));
            }
        }

        return blocks;
    }

    private static Dictionary<string, BlockType> ByFunction(bool isPlacedOnly)
    {
        Dictionary<string, BlockType> types = [];
        foreach (var type in All)
        {
            if (type.IsPlacedOnly == isPlacedOnly)
                types.Add(type.Function, type);
        }

        return types;
    }

    private static BlockType PlacedOnly(
        string function,
        BlockCategory category,
        IReadOnlyList<BlockStyle> styles,
        IReadOnlyList<BlockConnector> inputs,
        IReadOnlyList<BlockConnector> outputs)
    {
        return new BlockType(function, category, styles, inputs, outputs, isPlacedOnly: true);
    }

    private static BlockConnector[] Numbered(int firstIndex, params string[] names)
    {
        var connectors = new BlockConnector[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            connectors[i] = new BlockConnector(names[i], firstIndex + i);
        }

        return connectors;
    }
}
