using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// Each call to a Function expands its body inline, in a scope of its own, so the level only holds ordinary Logic
/// Blocks (ADR 0008).
/// </summary>
internal sealed partial class GraphBuilder
{
    public const int MaxFunctionDepth = 8;

    /// <summary>
    /// Visiting a body takes about as much stack as two levels of an operator expression, so an expansion counts as
    /// two levels against <see cref="Parser.MaxDepth"/>.
    /// </summary>
    private const int ExpansionDepth = 2;

    /// <summary>
    /// How many calls to Functions one Compile expands, counting the calls inside bodies. Nested calls multiply, and
    /// the live check compiles on every key press.
    /// </summary>
    public const int MaxExpansions = 1000;

    private readonly Dictionary<string, FunctionDefinition> _functions = [];

    /// <summary>
    /// Functions with an error on their first line, already reported. Calls to them only check their arguments.
    /// </summary>
    private readonly HashSet<FunctionDefinition> _brokenFunctions = [];

    private readonly HashSet<FunctionDefinition> _expandedFunctions = [];
    private readonly List<FunctionDefinition> _callStack = [];
    private int _expansions;

    /// <summary>
    /// Notes every Function up front, since a call may come before the definition, and checks each one's first line.
    /// </summary>
    private void CollectFunctions(List<Statement> statements)
    {
        var functions = statements.OfType<FunctionDefinition>().ToList();
        foreach (var function in functions)
        {
            var name = function.Name;
            if (CheckName(name) && !_functions.ContainsKey(name.Name))
                _functions.Add(name.Name, function);
            else if (_functions.TryGetValue(name.Name, out var first))
                Error(name, $"Function '{name.Name}' is already defined on line {first.Position.Line}");
        }

        foreach (var function in functions)
        {
            if (_functions.TryGetValue(function.Name.Name, out var registered) && registered == function && !CheckHeader(function))
                _brokenFunctions.Add(function);
        }
    }

    /// <summary>
    /// Whether the inputs and outputs have usable names, and the Function was read to its <c>end</c>. An output its
    /// body never defines is reported here too, but doesn't stop calls.
    /// </summary>
    private bool CheckHeader(FunctionDefinition function)
    {
        var isValid = function.IsComplete;
        HashSet<string> inputs = [];
        foreach (var input in function.Inputs)
        {
            if (!CheckParameterName(input))
            {
                isValid = false;
            }
            else if (!inputs.Add(input.Name))
            {
                Error(input, $"'{input.Name}' is already an input");
                isValid = false;
            }
        }

        HashSet<string> outputs = [];
        foreach (var output in function.Outputs)
        {
            if (!CheckParameterName(output))
            {
                isValid = false;
            }
            else if (inputs.Contains(output.Name))
            {
                Error(output, $"'{output.Name}' is an input; give the output a name of its own");
                isValid = false;
            }
            else if (!outputs.Add(output.Name))
            {
                Error(output, $"'{output.Name}' is already an output");
                isValid = false;
            }
            else if (function.IsComplete && !function.Body.OfType<Definition>().Any(d => d.Name == output.Name))
            {
                Error(output, $"'{function.Name.Name}' never defines its output '{output.Name}'");
            }
        }

        return isValid;
    }

    private bool CheckParameterName(FunctionParameter parameter)
    {
        var message = GetNameError(parameter.Name);
        if (message != null)
            Error(parameter, message);

        return message == null;
    }

    /// <summary>
    /// A Function's own name is checked before every Function is known, so another Function's name doesn't count.
    /// </summary>
    private bool CheckName(FunctionParameter parameter)
    {
        var message = GetReservedNameError(parameter.Name);
        if (message != null)
            Error(parameter, message);

        return message == null;
    }

    private bool IsFunctionCall(CallExpression call)
    {
        return _functions.ContainsKey(call.Function);
    }

    /// <summary>
    /// A call used as a value: its one output, or all of them to pick from with a dot.
    /// </summary>
    private Resolved? VisitFunctionCall(CallExpression call)
    {
        var function = _functions[call.Function];
        var outputs = ExpandCall(call);
        if (outputs == null)
            return null;

        return function.Outputs.Count switch
        {
            0 => Fail(call, $"'{call.Function}' has no output to use as a value"),
            1 => outputs[function.Outputs[0].Name],
            _ => Resolved.Of(new FunctionOutputs(function, outputs))
        };
    }

    /// <summary>
    /// The value of each output by name, or null after reporting why the call places nothing.
    /// </summary>
    private Dictionary<string, Resolved?> ExpandCall(CallExpression call)
    {
        var function = _functions[call.Function];
        if (_brokenFunctions.Contains(function) || !CanExpand(call, function))
        {
            foreach (var argument in call.Arguments.Concat(call.NamedArguments.Select(a => a.Value)))
            {
                EvaluateArgumentValue(argument);
            }

            return null;
        }

        var arguments = BindArguments(call, function);
        if (arguments == null)
            return null;

        return Expand(function, scope =>
        {
            foreach (var argument in arguments)
            {
                scope.Names.Add(argument.Key, argument.Value);
            }
        });
    }

    /// <summary>
    /// Whether expanding the call stays within the limits, reporting why when it doesn't.
    /// </summary>
    private bool CanExpand(CallExpression call, FunctionDefinition function)
    {
        var calling = _callStack.IndexOf(function);
        if (calling >= 0)
        {
            var through = _callStack.Skip(calling + 1).Select(f => $"'{f.Name.Name}'").ToList();
            Error(call, through.Count == 0
                ? $"'{call.Function}' calls itself, and a function can't"
                : $"'{call.Function}' calls itself through {string.Join(", ", through)}, and a function can't");
            return false;
        }

        if (_callStack.Count >= MaxFunctionDepth)
        {
            Error(call, $"Functions call each other more than {MaxFunctionDepth} deep here");
            return false;
        }

        if (++_expansions > MaxExpansions)
        {
            Error(call, $"This Script calls functions more than {MaxExpansions} times, counting the calls inside them");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Every input's value by name, each evaluated once in the caller's scope, so a body that uses an input twice
    /// shares its blocks. Null after reporting what's wrong with the arguments.
    /// </summary>
    private Dictionary<string, Resolved> BindArguments(CallExpression call, FunctionDefinition function)
    {
        var inputs = function.Inputs;
        var values = new Resolved?[inputs.Count];
        var isGiven = new bool[inputs.Count];
        var failed = false;
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var value = EvaluateArgumentValue(call.Arguments[i]);
            failed |= value == null;
            if (i < inputs.Count)
            {
                values[i] = value;
                isGiven[i] = true;
            }
        }

        if (call.Arguments.Count > inputs.Count)
        {
            Error(call, $"'{call.Function}' takes {Count(inputs.Count, "input")}, got {call.Arguments.Count}");
            failed = true;
        }

        HashSet<string> givenNames = [];
        var namesFailed = false;
        foreach (var argument in call.NamedArguments)
        {
            var value = EvaluateArgumentValue(argument.Value);
            failed |= value == null;
            var input = inputs.FindIndex(i => i.Name == argument.Name);
            if (!givenNames.Add(argument.Name))
            {
                Error(argument, $"'{argument.Name}' is given twice");
                namesFailed = true;
            }
            else if (input < 0)
            {
                Error(argument, inputs.Count == 0
                    ? $"'{call.Function}' has no inputs"
                    : $"'{call.Function}' has no input '{argument.Name}'; use {string.Join(", ", inputs.Select(i => i.Name))}");
                namesFailed = true;
            }
            else if (isGiven[input])
            {
                Error(argument, $"Input '{argument.Name}' is already given by position");
                namesFailed = true;
            }
            else
            {
                values[input] = value;
                isGiven[input] = true;
            }
        }

        // A wrong name was most likely meant as the missing input, and is already reported
        var missing = inputs.Where((_, i) => !isGiven[i]).Select(i => $"'{i.Name}'").ToList();
        if (missing.Count > 0 && !namesFailed)
        {
            Error(call, missing.Count == 1
                ? $"'{call.Function}' needs input {missing[0]}"
                : $"'{call.Function}' needs inputs {string.Join(", ", missing)}");
            failed = true;
        }

        if (failed || namesFailed)
            return null;

        Dictionary<string, Resolved> arguments = [];
        for (var i = 0; i < inputs.Count; i++)
        {
            arguments.Add(inputs[i].Name, values[i].Value);
        }

        return arguments;
    }

    /// <summary>
    /// An argument's value; inside <c>delay(...)</c> it may be a name defined later, passed on to wire once it is.
    /// </summary>
    private Resolved? EvaluateArgumentValue(Expression expression)
    {
        var deferred = TryDefer(expression);
        return deferred != null ? Resolved.Of(deferred) : Evaluate(expression);
    }

    /// <summary>
    /// Visits the body in a new scope whose inputs <paramref name="bindInputs"/> fills, and gives each output's value.
    /// </summary>
    private Dictionary<string, Resolved?> Expand(FunctionDefinition function, Action<Scope> bindInputs)
    {
        var scope = new Scope(function);
        CollectDefinitions(scope, function.Body);
        bindInputs(scope);

        var caller = _scope;
        _scope = scope;
        _callStack.Add(function);
        _expandedFunctions.Add(function);
        _depth += ExpansionDepth;
        foreach (var statement in function.Body)
        {
            Visit(statement);
        }

        _depth -= ExpansionDepth;
        _callStack.RemoveAt(_callStack.Count - 1);
        _scope = caller;

        Dictionary<string, Resolved?> outputs = [];
        foreach (var output in function.Outputs)
        {
            outputs.Add(output.Name, scope.Names.TryGetValue(output.Name, out var value) ? value : null);
        }

        return outputs;
    }

    /// <summary>
    /// Checks the body of each Function no call expanded, so its errors show while it's being written. Its inputs
    /// stand for nothing, like failed definitions, and what it placed is taken out again.
    /// </summary>
    private void CheckUncalledFunctions()
    {
        foreach (var function in _functions.Values.ToList())
        {
            if (_brokenFunctions.Contains(function) || _expandedFunctions.Contains(function))
                continue;

            var blockCount = _graph.Blocks.Count;
            var wireCount = _graph.Wires.Count;
            var placedWireCount = _graph.PlacedWires.Count;
            var wiresIntoPlacedCount = _wiresIntoPlaced.Count;
            var deferredWireCount = _deferredWires.Count;
            var expansions = _expansions;
            var placedInputLines = new Dictionary<(BlockNode Block, int Connector), int>(_placedInputLines);

            Expand(function, scope => scope.FailedNames.UnionWith(function.Inputs.Select(i => i.Name)));
            ConnectDeferredWires(deferredWireCount);

            _graph.RemoveFrom(blockCount, wireCount, placedWireCount);
            _wiresIntoPlaced.RemoveRange(wiresIntoPlacedCount, _wiresIntoPlaced.Count - wiresIntoPlacedCount);
            _deferredWires.RemoveRange(deferredWireCount, _deferredWires.Count - deferredWireCount);
            _expansions = expansions;
            foreach (var literal in _literals.Where(l => l.Value.Block.Index >= blockCount).ToList())
            {
                _literals.Remove(literal.Key);
            }

            _placedInputLines.Clear();
            foreach (var line in placedInputLines)
            {
                _placedInputLines.Add(line.Key, line.Value);
            }
        }
    }

    private static string Count(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
