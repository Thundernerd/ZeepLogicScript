using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private readonly List<DeferredWire> _deferredWires = [];
    private int _delayDepth;

    /// <summary>
    /// What feeds an input. Inside <c>delay(...)</c> a name may not be defined yet; its Wire is then connected
    /// once every definition has been visited.
    /// </summary>
    private InputFeed? EvaluateFeed(Expression expression)
    {
        if (_delayDepth > 0 && IsForwardReference(expression))
            return InputFeed.Deferred(expression);

        var output = EvaluateOutput(expression);
        return output == null ? null : InputFeed.Of(output.Value);
    }

    /// <summary>
    /// Called around a call's arguments; inside <c>delay(...)</c> they may refer ahead.
    /// </summary>
    private void EnterArguments(BlockType type)
    {
        if (type == BlockManifest.Delay)
            _delayDepth++;
    }

    private void LeaveArguments(BlockType type)
    {
        if (type == BlockManifest.Delay)
            _delayDepth--;
    }

    private void Connect(InputFeed feed, BlockNode to, int toConnector)
    {
        if (feed.Output != null)
            _graph.AddWire(feed.Output.Value, to, toConnector);
        else
            _deferredWires.Add(new DeferredWire(feed.Expression, to, toConnector));
    }

    private void ConnectDeferredWires()
    {
        foreach (var wire in _deferredWires)
        {
            var output = EvaluateOutput(wire.From);
            if (output != null)
                _graph.AddWire(output.Value, wire.To, wire.ToConnector);
        }
    }
}
