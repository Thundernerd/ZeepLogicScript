using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// A Function's input may give a Setting in its body, <c>delay(x, ticks: ticks)</c>. A call then passes it a constant
/// instead of a Wire, which stands in for the input wherever the body writes it as a Setting's value, or passes it on to
/// another Function's Setting input.
/// </summary>
internal sealed partial class GraphBuilder
{
    /// <summary>
    /// Each Function's Setting inputs, with the first place its body uses each one as a Setting.
    /// </summary>
    private readonly Dictionary<FunctionDefinition, Dictionary<string, SyntaxNode>> _settingInputs = [];

    /// <summary>
    /// The Function tables whose Setting inputs are found, or being found.
    /// </summary>
    private readonly HashSet<Dictionary<string, FunctionDefinition>> _settingInputTables = [];

    /// <param name="function">One of <paramref name="functions"/>.</param>
    /// <param name="functions">The Functions the body calls without a prefix.</param>
    private Dictionary<string, SyntaxNode> GetSettingInputs(FunctionDefinition function, Dictionary<string, FunctionDefinition> functions)
    {
        FindSettingInputs(functions);
        return _settingInputs.TryGetValue(function, out var settingInputs) ? settingInputs : [];
    }

    /// <summary>
    /// Finds the Setting inputs of every Function in a table together, going over them until none gains one: passing
    /// an input on to another Function's Setting input makes it a Setting input too. Following each call into the
    /// Function it names instead would recurse once per Function in a chain of calls.
    /// </summary>
    private void FindSettingInputs(Dictionary<string, FunctionDefinition> functions)
    {
        if (!_settingInputTables.Add(functions))
            return;

        foreach (var function in functions.Values)
        {
            _settingInputs[function] = [];
        }

        bool isGrowing;
        do
        {
            isGrowing = false;
            foreach (var function in functions.Values)
            {
                var settingInputs = _settingInputs[function];
                VisitInputUses(function, functions, (name, node, isSetting) =>
                {
                    if (!isSetting)
                        return;

                    if (!settingInputs.TryGetValue(name, out var first))
                    {
                        settingInputs.Add(name, node);
                        isGrowing = true;
                    }
                    else if (node.Position.CompareTo(first.Position) < 0)
                    {
                        settingInputs[name] = node;
                    }
                });
            }
        }
        while (isGrowing);
    }

    /// <summary>
    /// Reports each use of a Setting input as a Signal: a constant has nothing to wire.
    /// </summary>
    private void CheckSettingInputUses(FunctionDefinition function)
    {
        var settingInputs = GetSettingInputs(function, Functions);
        if (settingInputs.Count == 0)
            return;

        VisitInputUses(function, Functions, (name, node, isSetting) =>
        {
            if (!isSetting && settingInputs.TryGetValue(name, out var settingUse))
                Error(node, node is CallExpression
                    ? $"'{name}' is used as a Setting on line {settingUse.Position.Line}, so it isn't a placed block to call"
                    : $"'{name}' is used as a Setting on line {settingUse.Position.Line}, so it can't also be used as a Signal");
        });
    }

    /// <summary>
    /// Calls <paramref name="use"/> with each place the body uses an input, and whether it's as a Setting's value.
    /// It follows the calls the way visiting the body does, without reporting anything.
    /// </summary>
    private void VisitInputUses(
        FunctionDefinition function,
        Dictionary<string, FunctionDefinition> functions,
        Action<string, SyntaxNode, bool> use)
    {
        var inputs = new HashSet<string>(function.Inputs.Select(i => i.Name));
        foreach (var statement in function.Body)
        {
            switch (statement)
            {
                case Definition definition:
                    VisitExpression(definition.Value);
                    break;
                case ExpressionStatement expression:
                    VisitExpression(expression.Expression);
                    break;
            }
        }

        void VisitExpression(Expression expression)
        {
            switch (expression)
            {
                case NameExpression name when inputs.Contains(name.Name):
                    use(name.Name, name, false);
                    break;
                case MemberExpression member:
                    VisitExpression(member.Target);
                    break;
                case UnaryExpression unary:
                    VisitExpression(unary.Operand);
                    break;
                case BinaryExpression binary:
                    VisitExpression(binary.Left);
                    VisitExpression(binary.Right);
                    break;
                case ListExpression list:
                    list.Items.ForEach(VisitExpression);
                    break;
                case CallExpression call:
                    VisitCall(call);
                    break;
            }
        }

        void VisitCall(CallExpression call)
        {
            if (call.Library == null && inputs.Contains(call.Function))
                use(call.Function, call, false);

            if (call.Library != null || functions.ContainsKey(call.Function))
            {
                var callee = FindFunctionQuietly(call, functions, out var calleeFunctions);
                var calleeSettings = callee == null ? null : GetSettingInputs(callee, calleeFunctions);
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    VisitArgument(call.Arguments[i], i < callee?.Inputs.Count && calleeSettings.ContainsKey(callee.Inputs[i].Name));
                }

                foreach (var argument in call.NamedArguments)
                {
                    VisitArgument(argument.Value, calleeSettings?.ContainsKey(argument.Name) == true);
                }

                return;
            }

            call.Arguments.ForEach(VisitExpression);
            BlockManifest.TryGetFunction(call.Function, out var type);
            foreach (var argument in call.NamedArguments)
            {
                VisitArgument(argument.Value, type != null && GivesConstant(type, argument.Name));
            }
        }

        // A Setting's value that isn't a name is never evaluated, only reported
        void VisitArgument(Expression value, bool isSetting)
        {
            if (!isSetting)
                VisitExpression(value);
            else if (value is NameExpression name && inputs.Contains(name.Name))
                use(name.Name, name, true);
        }
    }

    /// <summary>
    /// Whether a named argument of a Block Function gives a constant: a Setting or the style. A Toggler's targets are
    /// placed blocks, so they're names to look up.
    /// </summary>
    private static bool GivesConstant(BlockType type, string name)
    {
        if (type.IndexOfInput(name) >= 0)
            return false;

        return (name == BlockType.StyleName && type.HasStyles) || type.FindSetting(name) is { Type: not SettingType.Targets };
    }

    /// <summary>
    /// The Function a call names and the Functions its body calls without a prefix, or null when there's none to
    /// expand; <see cref="FindFunction"/> reports why.
    /// </summary>
    private FunctionDefinition FindFunctionQuietly(
        CallExpression call,
        Dictionary<string, FunctionDefinition> functions,
        out Dictionary<string, FunctionDefinition> calleeFunctions)
    {
        calleeFunctions = functions;
        if (call.Library != null)
        {
            var library = LoadLibrary(call.Library);
            if (library == null || library.Errors.Count > 0)
                return null;

            calleeFunctions = library.Functions;
        }

        return calleeFunctions.TryGetValue(call.Function, out var function) ? function : null;
    }

    /// <summary>
    /// Whether <paramref name="value"/> names one of the current Function's Setting inputs. The constant its call passed
    /// is null when the body is checked without a call.
    /// </summary>
    private bool TryGetSettingInput(Expression value, out SettingConstant constant)
    {
        constant = null;
        if (value is not NameExpression name || _scope.Function == null || !GetSettingInputs(_scope.Function, Functions).ContainsKey(name.Name))
            return false;

        constant = _scope.Names.TryGetValue(name.Name, out var bound) ? bound as SettingConstant : null;
        return true;
    }

    /// <summary>
    /// What a call passes to a Setting input, or null after reporting why it isn't a constant. A name the caller
    /// defines is a Wire; any other word is left for the Setting to check.
    /// </summary>
    private SettingConstant BindSetting(CallExpression call, FunctionParameter input, Expression value, Position valuePosition)
    {
        if (TryGetSettingInput(value, out var forwarded))
            return forwarded;

        switch (value)
        {
            case IntegerLiteral:
            case NameExpression name when !_scope.Names.ContainsKey(name.Name) && !_scope.Definitions.ContainsKey(name.Name):
                return new SettingConstant(value, valuePosition, _scope);
            default:
                Report(valuePosition, $"'{input.Name}' is used as a Setting in '{call.QualifiedName}'. Pass a number, true or false, or a word, not a Wire");
                return null;
        }
    }

    /// <summary>
    /// <paramref name="getValue"/> of the argument, or of the constant its call passed when the argument's value is a
    /// Setting input, reporting a wrong constant where the call wrote it. Null when the value is wrong, already
    /// reported, or the body is checked without a call.
    /// </summary>
    private T WithSettingInput<T>(NamedArgument argument, Func<NamedArgument, T> getValue)
        where T : class
    {
        if (!TryGetSettingInput(argument.Value, out var constant))
            return getValue(argument);

        if (constant == null)
            return null;

        var scope = _scope;
        _scope = constant.Scope;
        var value = getValue(new NamedArgument(argument.Position, argument.Name, constant.Value, constant.ValuePosition));
        _scope = scope;
        return value;
    }
}
