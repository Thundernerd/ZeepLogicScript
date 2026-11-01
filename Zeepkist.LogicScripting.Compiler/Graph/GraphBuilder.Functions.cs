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

    /// <summary>
    /// The Functions the text being compiled defines; a Library's own are in its <see cref="LoadedLibrary"/>.
    /// </summary>
    private readonly Dictionary<string, FunctionDefinition> _scriptFunctions = [];

    /// <summary>
    /// Functions with an error on their first line, already reported. Calls to them only check their arguments.
    /// </summary>
    private readonly HashSet<FunctionDefinition> _brokenFunctions = [];

    private readonly HashSet<FunctionDefinition> _expandedFunctions = [];
    private readonly List<FunctionDefinition> _callStack = [];
    private int _expansions;

    /// <summary>
    /// Notes every Function up front, since a call may come before the definition, and checks each one's first line.
    /// They go to the current scope's Functions: the text being compiled, or a Library being loaded.
    /// </summary>
    private void CollectFunctions(List<Statement> statements)
    {
        var known = Functions;
        var functions = statements.OfType<FunctionDefinition>().ToList();
        foreach (var function in functions)
        {
            var name = function.Name;
            if (CheckName(name) && !known.ContainsKey(name.Name))
            {
                known.Add(name.Name, function);

                // Before any Setting inputs are found, since finding them follows calls within this Library
                if (_scope.Library != null)
                    _libraryFunctions.Add(function, _scope.Library);
            }
            else if (known.TryGetValue(name.Name, out var first))
                Error(name, $"Function '{name.Name}' is already defined on line {first.Position.Line}");
        }

        foreach (var function in functions)
        {
            if (known.TryGetValue(function.Name.Name, out var registered) && registered == function && !CheckHeader(function))
                _brokenFunctions.Add(function);
        }

        // A Library's Functions are checked for Setting input uses once it's loaded, which may be during a search for
        // its Setting inputs
        if (_scope.Library != null)
            return;

        foreach (var function in known.Values)
        {
            CheckSettingInputUses(function);
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
                Error(output, $"'{output.Name}' is an input. Give the output a name of its own");
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

    /// <summary>
    /// A qualified call is always to a Function; whether its Library has one is checked when it's expanded.
    /// </summary>
    private bool IsFunctionCall(CallExpression call)
    {
        return IsFunctionCall(call, Functions);
    }

    /// <summary>
    /// <paramref name="functions"/> are the ones the caller calls without a prefix.
    /// </summary>
    private static bool IsFunctionCall(CallExpression call, Dictionary<string, FunctionDefinition> functions)
    {
        return call.Library != null || functions.ContainsKey(call.Function);
    }

    /// <summary>
    /// A call used as a value: its one output, or all of them to pick from with a dot.
    /// </summary>
    private Resolved VisitFunctionCall(CallExpression call)
    {
        var function = FindFunction(call);
        var outputs = ExpandCall(call, function);
        if (outputs == null)
            return FailedValue.Instance;

        return function.Outputs.Count switch
        {
            0 => Fail(call, $"'{call.QualifiedName}' has no output to use as a value"),
            1 => outputs[function.Outputs[0].Name],
            _ => new FunctionOutputs(function, outputs)
        };
    }

    /// <summary>
    /// The value of each output by name, or null after reporting why the call places nothing. A null
    /// <paramref name="function"/> means the call names none, already reported.
    /// </summary>
    private Dictionary<string, Resolved> ExpandCall(CallExpression call, FunctionDefinition function)
    {
        if (function == null || _brokenFunctions.Contains(function) || !CanExpand(call, function))
        {
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                EvaluateInput(call, function, i, call.Arguments[i], call.Arguments[i].Position);
            }

            foreach (var argument in call.NamedArguments)
            {
                var input = function?.Inputs.FindIndex(i => i.Name == argument.Name) ?? -1;
                EvaluateInput(call, function, input, argument.Value, argument.ValuePosition);
            }

            return null;
        }

        var arguments = BindArguments(call, function);
        if (arguments == null)
            return null;

        var callSite = _libraryFunctions.ContainsKey(function) ? _scope.CallSite ?? call.Position : (Position?)null;
        return Expand(function, callSite, scope =>
        {
            foreach (var argument in arguments)
            {
                argument.Value.BindTo(scope, argument.Key);
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
            var through = _callStack.Skip(calling + 1).Select(f => $"'{GetQualifiedName(f)}'").ToList();
            var name = GetQualifiedName(function);
            Error(call, through.Count == 0
                ? $"'{name}' calls itself, and a function can't"
                : $"'{name}' calls itself through {string.Join(", ", through)}, and a function can't");
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
    /// shares its blocks. A value that failed, already reported, stands for nothing in the body, which is
    /// still checked with the others, such as the constants passed to its Settings. Null after reporting that the
    /// arguments don't match the inputs.
    /// </summary>
    private Dictionary<string, ArgumentValue> BindArguments(CallExpression call, FunctionDefinition function)
    {
        var inputs = function.Inputs;
        var values = new ArgumentValue[inputs.Count];
        var isGiven = new bool[inputs.Count];
        var failed = false;
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var value = EvaluateInput(call, function, i, call.Arguments[i], call.Arguments[i].Position);
            if (i < inputs.Count)
            {
                values[i] = value;
                isGiven[i] = true;
            }
        }

        if (call.Arguments.Count > inputs.Count)
        {
            Error(call, $"'{call.QualifiedName}' takes {Count(inputs.Count, "input")}, got {call.Arguments.Count}");
            failed = true;
        }

        HashSet<string> givenNames = [];
        var namesFailed = false;
        foreach (var argument in call.NamedArguments)
        {
            var input = inputs.FindIndex(i => i.Name == argument.Name);
            var value = EvaluateInput(call, function, input, argument.Value, argument.ValuePosition);
            if (!givenNames.Add(argument.Name))
            {
                Error(argument, $"'{argument.Name}' is given twice");
                namesFailed = true;
            }
            else if (input < 0)
            {
                Error(argument, inputs.Count == 0
                    ? $"'{call.QualifiedName}' has no inputs"
                    : $"'{call.QualifiedName}' has no input '{argument.Name}'. Use {string.Join(", ", inputs.Select(i => i.Name))}");
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
                ? $"'{call.QualifiedName}' needs input {missing[0]}"
                : $"'{call.QualifiedName}' needs inputs {string.Join(", ", missing)}");
            failed = true;
        }

        if (failed || namesFailed)
            return null;

        Dictionary<string, ArgumentValue> arguments = [];
        for (var i = 0; i < inputs.Count; i++)
        {
            arguments.Add(inputs[i].Name, values[i]);
        }

        return arguments;
    }

    /// <summary>
    /// The value of an argument for the input at <paramref name="inputIndex"/>, which is -1 when the call names none: a
    /// constant for a Setting input, or else what <see cref="EvaluateArgumentValue"/> gives. A
    /// <see cref="FailedValue"/> after reporting why there's neither.
    /// </summary>
    private ArgumentValue EvaluateInput(CallExpression call, FunctionDefinition function, int inputIndex, Expression value, Position valuePosition)
    {
        if (function == null || inputIndex < 0 || inputIndex >= function.Inputs.Count)
            return new ArgumentValue(EvaluateArgumentValue(value));

        var input = function.Inputs[inputIndex];
        if (!GetSettingInputs(function, FunctionsOf(function)).ContainsKey(input.Name))
            return new ArgumentValue(EvaluateArgumentValue(value));

        var constant = BindSetting(call, input, value, valuePosition);
        return constant == null ? new ArgumentValue(FailedValue.Instance) : new ArgumentValue(constant);
    }

    /// <summary>
    /// An argument's value; inside <c>delay(...)</c> it may be a name defined later, passed on to wire once it is.
    /// </summary>
    private Resolved EvaluateArgumentValue(Expression expression)
    {
        return (Resolved)TryDefer(expression) ?? Evaluate(expression);
    }

    /// <summary>
    /// Visits the body in a new scope whose inputs <paramref name="bindInputs"/> fills, and gives each output's value.
    /// <paramref name="callSite"/> is the scope's <see cref="Scope.CallSite"/>.
    /// </summary>
    private Dictionary<string, Resolved> Expand(FunctionDefinition function, Position? callSite, Action<Scope> bindInputs)
    {
        _libraryFunctions.TryGetValue(function, out var library);
        var scope = new Scope(function, library, callSite);
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

        Dictionary<string, Resolved> outputs = [];
        foreach (var output in function.Outputs)
        {
            outputs.Add(output.Name, scope.Names.TryGetValue(output.Name, out var value) ? value : FailedValue.Instance);
        }

        return outputs;
    }

    /// <summary>
    /// Checks the body of each Function no call expanded, so its errors show while it's being written. Its inputs
    /// stand for nothing, like failed definitions, and what it placed is taken out again.
    /// </summary>
    private void CheckUncalledFunctions()
    {
        foreach (var function in _scriptFunctions.Values.ToList())
        {
            if (_brokenFunctions.Contains(function) || _expandedFunctions.Contains(function))
                continue;

            var snapshot = new Snapshot(this);
            Expand(function, null, scope => scope.FailedNames.UnionWith(function.Inputs.Select(i => i.Name)));
            ConnectDeferredWires(snapshot.DeferredWireCount);
            snapshot.Restore();
        }
    }

    private static string Count(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
