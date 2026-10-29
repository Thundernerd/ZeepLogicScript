using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private readonly List<DeferredWire> _deferredWires = [];

    /// <summary>
    /// What feeds an input. Inside <c>delay(...)</c> a name may not be defined yet; its Wire is then connected
    /// once every definition has been visited.
    /// </summary>
    private InputFeed? EvaluateFeed(Expression expression)
    {
        if (TryDefer(expression) is { } deferred)
            return InputFeed.Of(deferred);

        var value = Evaluate(expression);
        if (value?.Deferred != null)
            return InputFeed.Of(value.Value.Deferred);

        var output = ToOutput(expression, value);
        return output == null ? null : InputFeed.Of(output.Value);
    }

    /// <summary>
    /// Inside <c>delay(...)</c>, a name defined later in this scope, to look up once it is; otherwise null.
    /// </summary>
    private DeferredValue TryDefer(Expression expression)
    {
        return _scope.DelayDepth > 0 && IsForwardReference(expression) ? new DeferredValue(expression, _scope) : null;
    }

    /// <summary>
    /// Called around a call's arguments; inside <c>delay(...)</c> they may refer ahead.
    /// </summary>
    private void EnterArguments(BlockType type)
    {
        if (type == BlockManifest.Delay)
            _scope.DelayDepth++;
    }

    private void LeaveArguments(BlockType type)
    {
        if (type == BlockManifest.Delay)
            _scope.DelayDepth--;
    }

    private void Connect(InputFeed feed, BlockNode to, int toConnector)
    {
        if (feed.Output != null)
            _graph.AddWire(feed.Output.Value, to, toConnector);
        else
            _deferredWires.Add(new DeferredWire(feed.Deferred, to, toConnector));
    }

    /// <summary>
    /// Connects the deferred Wires from <paramref name="start"/> on, each looked up in the scope it was written in.
    /// </summary>
    private void ConnectDeferredWires(int start)
    {
        var scope = _scope;
        for (var i = start; i < _deferredWires.Count; i++)
        {
            var wire = _deferredWires[i];
            var output = Resolve(wire.From);
            if (output != null)
                _graph.AddWire(output.Value, wire.To, wire.ToConnector);
        }

        _scope = scope;
    }

    /// <summary>
    /// A value passed into a Function can itself be deferred in its caller's scope, so this follows it outward until
    /// it reaches a block.
    /// </summary>
    private BlockOutput? Resolve(DeferredValue deferred)
    {
        while (true)
        {
            _scope = deferred.Scope;
            var value = Evaluate(deferred.Expression);
            if (value?.Deferred == null)
                return ToOutput(deferred.Expression, value);

            deferred = value.Value.Deferred;
        }
    }
}
