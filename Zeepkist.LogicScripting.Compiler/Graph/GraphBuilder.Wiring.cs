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
    private InputFeed EvaluateFeed(Expression expression)
    {
        return (InputFeed)TryDefer(expression) ?? Evaluate(expression).ToFeed(this, expression);
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

    /// <summary>
    /// Connects the deferred Wires from <paramref name="start"/> on, each looked up in the scope it was written in.
    /// </summary>
    private void ConnectDeferredWires(int start)
    {
        var scope = _scope;
        for (var i = start; i < _deferredWires.Count; i++)
        {
            var wire = _deferredWires[i];
            Resolve(wire.From).Connect(this, wire.To, wire.ToConnector);
        }

        _scope = scope;
    }

    /// <summary>
    /// A value passed into a Function can itself be deferred in its caller's scope, so this follows it outward until
    /// it reaches a block or fails.
    /// </summary>
    private InputFeed Resolve(DeferredValue deferred)
    {
        InputFeed feed = deferred;
        while (feed is DeferredValue next)
        {
            _scope = next.Scope;
            var value = Evaluate(next.Expression);
            foreach (var member in next.Members)
            {
                value = value.SelectMember(this, member);
            }

            feed = value.ToFeed(this, next.Origin);
        }

        return feed;
    }
}
