using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    internal const string PlacedFunction = "placed";

    private readonly Dictionary<string, BlockNode> _placedNodes = [];

    private readonly Dictionary<(BlockNode Block, int Connector), int> _placedInputLines = [];

    /// <summary>
    /// The only Wires that can close a Dependency Loop.
    /// </summary>
    private readonly List<(GraphWire Wire, SyntaxNode Origin)> _wiresIntoPlaced = [];

    /// <summary>
    /// Null after reporting why the reference reaches no block. Several references to one UID share one block.
    /// </summary>
    private Resolved VisitPlaced(CallExpression call)
    {
        if (call.Arguments.Count != 1 || call.NamedArguments.Count > 0 || call.Arguments[0] is not StringLiteral uid)
            return Fail(call, $"{PlacedFunction}(...) takes one UID in quotes, like {PlacedFunction}(\"AbC12\")");

        var name = _scope.Current?.Value == call ? _scope.Current.Name : null;
        var placed = _findPlacedBlock(uid.Value);
        if (placed == null)
        {
            AddPlacedBlockReference(new PlacedBlockReference(call.Position, name, uid.Value, null));
            return Fail(uid, $"No block in this level has UID \"{uid.Value}\"");
        }

        var type = BlockManifest.GetPlacedType(placed.BlockId);
        var style = type.Styles.FirstOrDefault(s => s.BlockId == placed.BlockId);

        AddPlacedBlockReference(new PlacedBlockReference(call.Position, name, uid.Value, type));
        if (style != null && _disabledBlockIds.Contains(style.BlockId))
            return Fail(call, $"'{type.Describe(style)}' doesn't match this game version and is turned off");

        if (placed.IsInBlockGroup)
            return Fail(uid, $"Block \"{uid.Value}\" is in a Block Group, which a recompile replaces; refer to a hand-placed block, such as a Passthrough between two Scripts");

        if (!_placedNodes.TryGetValue(uid.Value, out var block))
        {
            block = new BlockNode(uid.Value, type, style, placed);
            _placedNodes.Add(uid.Value, block);
        }

        return ValueOf(block);
    }

    /// <summary>
    /// Once per <c>placed(...)</c> in the text, though a Function's body may be expanded several times.
    /// </summary>
    private void AddPlacedBlockReference(PlacedBlockReference reference)
    {
        if (!_graph.PlacedBlockReferences.Exists(r => r.Position.CompareTo(reference.Position) == 0))
            _graph.PlacedBlockReferences.Add(reference);
    }

    private bool IsReferenceCall(CallExpression call)
    {
        return _scope.Names.TryGetValue(call.Function, out var value) && GetPlacedBlock(value) != null;
    }

    /// <summary>
    /// The placed block a value comes from, or null when it doesn't come from one.
    /// </summary>
    private static BlockNode GetPlacedBlock(Resolved value)
    {
        var block = value switch
        {
            OutputValue output => output.Output.Block,
            BlockValue blockValue => blockValue.Block,
            _ => null
        };

        return block is { IsPlaced: true } ? block : null;
    }

    /// <summary>
    /// <c>lamp_1(intensity: x)</c>: wires into a placed block's inputs, each at most once, and never changes its
    /// Settings.
    /// </summary>
    private void VisitReferenceCall(CallExpression call)
    {
        var block = GetPlacedBlock(_scope.Names[call.Function]);
        var type = block.Type;
        if (type.Inputs.Count == 0)
        {
            foreach (var argument in call.Arguments)
            {
                Evaluate(argument);
            }

            foreach (var argument in call.NamedArguments)
            {
                CheckUntypedValue(argument.Value);
            }

            Error(call, $"'{call.Function}' has no inputs to wire");
            return;
        }

        var check = new ArgumentCheck(call, type, Error);
        VisitPositionalArguments(check);
        foreach (var argument in call.NamedArguments)
        {
            if (!check.TryGive(argument))
                continue;

            var input = type.IndexOfInput(argument.Name);
            if (input >= 0)
            {
                check.WireByName(argument, input, EvaluateArgument(check, argument.Value));
                continue;
            }

            check.ReportAndFail(argument, IsSettingName(type, argument.Name)
                ? $"A Script never changes a placed block's Settings; set {argument.Name} in the editor"
                : $"'{call.Function}' has no input '{argument.Name}'; use {string.Join(", ", type.Inputs.Select(i => i.Name))}");
        }

        if (check.Failed)
            return;

        for (var i = 0; i < check.Inputs.Length; i++)
        {
            if (check.Inputs[i] is not { Feed: { } feed } wired)
                continue;

            if (feed is OutputValue output)
                WireIntoPlaced(call, wired.IsByPosition, i, output.Output, block);
            else
                Error(GetInputOrigin(call, wired.IsByPosition, i, block.Type), "A name defined later can't be wired into a placed block; only delay(...) can refer ahead");
        }
    }

    /// <summary>
    /// Whether a named argument would give a Setting, or the style, if the type were called as a Block Function.
    /// </summary>
    private static bool IsSettingName(BlockType type, string name)
    {
        return type.FindSetting(name) != null || name == BlockSetting.Hide.Name || name == BlockType.StyleName;
    }

    /// <summary>
    /// <paramref name="input"/> is the input's position in the block type's inputs, not its Connector index.
    /// </summary>
    private void WireIntoPlaced(CallExpression call, bool isByPosition, int input, BlockOutput from, BlockNode block)
    {
        var connector = block.Type.Inputs[input];
        var origin = GetInputOrigin(call, isByPosition, input, block.Type);

        if (block.Placed.WiredInputs.Contains(connector.Index))
        {
            Error(origin, $"Input '{connector.Name}' of '{call.Function}' already has a Wire in the level, and an input takes one Wire");
            return;
        }

        if (_placedInputLines.TryGetValue((block, connector.Index), out var line))
        {
            Error(origin, $"Input '{connector.Name}' of '{call.Function}' is already wired on line {line}");
            return;
        }

        // A body may be expanded several times, so name the line it was called from
        _placedInputLines.Add((block, connector.Index), _scope.Function == null ? call.Position.Line : _statementLine);
        _wiresIntoPlaced.Add((_graph.AddWire(from, block, connector.Index), origin));
    }

    private static SyntaxNode GetInputOrigin(CallExpression call, bool isByPosition, int input, BlockType type)
    {
        return isByPosition ? call.Arguments[input] : call.NamedArguments.First(a => a.Name == type.Inputs[input].Name);
    }

    /// <summary>
    /// Reports each Wire into a placed block whose Signal comes back to where it starts within one Tick, through the
    /// Script's Wires or the level's. Only these Wires can close a loop: inside the Script, only <c>delay(...)</c>
    /// refers ahead.
    /// </summary>
    private void CheckDependencyLoops()
    {
        foreach (var (wire, origin) in _wiresIntoPlaced)
        {
            if (wire.To.Type != BlockManifest.Delay && Reaches(wire.To, wire.From))
                Error(origin, "This Wire makes a Dependency Loop, which stops all logic in play; put a delay(...) in the loop");
        }
    }

    /// <summary>
    /// Whether a Signal from <paramref name="start"/> reaches <paramref name="target"/> in the same Tick. A Delay
    /// passes its input on only in the next Tick, so the search stops there.
    /// </summary>
    private bool Reaches(BlockNode start, BlockNode target)
    {
        HashSet<BlockNode> seen = [start];
        Stack<BlockNode> open = new([start]);
        while (open.Count > 0)
        {
            var block = open.Pop();
            if (block == target)
                return true;

            foreach (var next in GetFedBlocks(block))
            {
                if (next.Type != BlockManifest.Delay && seen.Add(next))
                    open.Push(next);
            }
        }

        return false;
    }

    /// <summary>
    /// The blocks a block's outputs feed, through the Script's Wires or, between placed blocks, the level's.
    /// </summary>
    private IEnumerable<BlockNode> GetFedBlocks(BlockNode block)
    {
        foreach (var wire in _graph.Wires.Concat(_graph.PlacedWires))
        {
            if (wire.From == block)
                yield return wire.To;
        }

        if (!block.IsPlaced)
            yield break;

        foreach (var uid in block.Placed.Downstream)
        {
            if (_placedNodes.TryGetValue(uid, out var fed))
                yield return fed;
        }
    }
}
