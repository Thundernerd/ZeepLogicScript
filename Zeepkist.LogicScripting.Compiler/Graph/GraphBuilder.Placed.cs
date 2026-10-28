using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// <c>placed("UID")</c>: a Placed Block Reference.
    /// </summary>
    private const string PlacedFunction = "placed";

    private readonly Dictionary<string, BlockNode> _placedNodes = [];

    /// <summary>
    /// The line that wired each input of a placed block, by block and input Connector.
    /// </summary>
    private readonly Dictionary<(BlockNode Block, int Connector), int> _placedInputs = [];

    /// <summary>
    /// Wires into placed blocks and the argument that wired each; the only Wires that can close a Dependency Loop.
    /// </summary>
    private readonly List<(GraphWire Wire, SyntaxNode Origin)> _wiresIntoPlaced = [];

    /// <summary>
    /// The block a <c>placed("UID")</c> refers to, or null after reporting why it refers to none. Several references
    /// to one UID share one block.
    /// </summary>
    private Resolved? VisitPlaced(CallExpression call)
    {
        if (call.Arguments.Count != 1 || call.NamedArguments.Count > 0 || call.Arguments[0] is not StringLiteral uid)
            return Fail(call, $"{PlacedFunction}(...) takes one UID in quotes, like {PlacedFunction}(\"AbC12\")");

        var name = _current?.Value == call ? _current.Name : null;
        var placed = _findPlacedBlock(uid.Value);
        if (placed == null)
        {
            _graph.PlacedReferences.Add(new PlacedReference(call.Position, name, uid.Value, null));
            return Fail(uid, $"No block in this level has UID \"{uid.Value}\"");
        }

        if (!BlockManifest.TryGetBlock(placed.BlockId, out var type, out var style))
            type = BlockManifest.OtherBlock;

        _graph.PlacedReferences.Add(new PlacedReference(call.Position, name, uid.Value, type));
        if (style != null && _disabledBlockIds.Contains(style.BlockId))
            return Fail(call, $"'{type.Describe(style)}' doesn't match this game version and is turned off");

        if (!_placedNodes.TryGetValue(uid.Value, out var block))
        {
            block = new BlockNode(uid.Value, type, style, placed);
            _placedNodes.Add(uid.Value, block);
        }

        return block.Outputs.Count == 1 ? Resolved.Of(new BlockOutput(block, block.Outputs[0].Index)) : Resolved.Of(block);
    }

    /// <summary>
    /// Whether a call is to a name defined with <c>placed(...)</c>, which wires into that block's inputs.
    /// </summary>
    private bool IsReferenceCall(CallExpression call)
    {
        return _names.TryGetValue(call.Function, out var value) && value.Block.IsPlaced;
    }

    /// <summary>
    /// <c>lamp_1(intensity: x)</c>: wires into a placed block's inputs, each at most once, and never changes its
    /// Settings.
    /// </summary>
    private void VisitReferenceCall(CallExpression call)
    {
        var block = _names[call.Function].Block;
        var type = block.Type;
        if (type.Inputs.Count == 0)
        {
            foreach (var argument in call.Arguments.Concat(call.NamedArguments.Select(a => a.Value)))
            {
                Evaluate(argument);
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
            var output = check.Inputs[i]?.Feed?.Output;
            if (output != null)
                WireIntoPlaced(call, check.Inputs[i].Value.IsByPosition, i, output.Value, block);
        }
    }

    /// <summary>
    /// Whether a named argument would give a Setting, or the style, if the type were called as a Block Function.
    /// </summary>
    private static bool IsSettingName(BlockType type, string name)
    {
        return type.FindSetting(name) != null || name == BlockSetting.Hide.Name || name == BlockType.StyleName;
    }

    /// <param name="call">The call that wires the input.</param>
    /// <param name="isByPosition">Whether the call wires it by position rather than by name.</param>
    /// <param name="input">The input's position in the block type's inputs.</param>
    /// <param name="from">The output to wire from.</param>
    /// <param name="block">The placed block.</param>
    private void WireIntoPlaced(CallExpression call, bool isByPosition, int input, BlockOutput from, BlockNode block)
    {
        var connector = block.Type.Inputs[input];
        SyntaxNode origin = isByPosition
            ? call.Arguments[input]
            : call.NamedArguments.First(a => a.Name == connector.Name);

        if (block.Placed.WiredInputs.Contains(connector.Index))
        {
            Error(origin, $"Input '{connector.Name}' of '{call.Function}' already has a Wire in the level, and an input takes one Wire");
            return;
        }

        if (_placedInputs.TryGetValue((block, connector.Index), out var line))
        {
            Error(origin, $"Input '{connector.Name}' of '{call.Function}' is already wired on line {line}");
            return;
        }

        _placedInputs.Add((block, connector.Index), call.Position.Line);
        _wiresIntoPlaced.Add((_graph.AddWire(from, block, connector.Index), origin));
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
