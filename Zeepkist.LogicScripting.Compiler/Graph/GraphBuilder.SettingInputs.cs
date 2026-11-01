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

    /// <summary>
    /// The tables whose Setting inputs are being found together, or null when there's no search going on.
    /// </summary>
    private List<Dictionary<string, FunctionDefinition>> _settingInputSearch;

    /// <summary>
    /// Libraries loaded during the search, whose Setting inputs it's still finding.
    /// </summary>
    private readonly Queue<LoadedLibrary> _uncheckedLibraries = [];

    /// <param name="function">One of <paramref name="functions"/>.</param>
    /// <param name="functions">The Functions the body calls without a prefix.</param>
    private Dictionary<string, SyntaxNode> GetSettingInputs(FunctionDefinition function, Dictionary<string, FunctionDefinition> functions)
    {
        if (_settingInputTables.Add(functions))
        {
            foreach (var tableFunction in functions.Values)
            {
                _settingInputs[tableFunction] = [];
            }

            // Libraries that call each other need each other's answers, so a table reached mid-search joins it
            if (_settingInputSearch != null)
                _settingInputSearch.Add(functions);
            else
                FindSettingInputs(functions);
        }

        return _settingInputs.TryGetValue(function, out var settingInputs) ? settingInputs : [];
    }

    /// <summary>
    /// Finds the Setting inputs of every Function in a table, and in the tables its calls reach, together, going over
    /// them until none gains one: passing an input on to another Function's Setting input makes it a Setting input too.
    /// Following each call into the Function it names instead would recurse once per Function in a chain of calls.
    /// </summary>
    private void FindSettingInputs(Dictionary<string, FunctionDefinition> functions)
    {
        _settingInputSearch = [functions];
        bool isGrowing;
        do
        {
            isGrowing = false;
            for (var i = 0; i < _settingInputSearch.Count; i++)
            {
                var table = _settingInputSearch[i];
                foreach (var function in table.Values)
                {
                    var settingInputs = _settingInputs[function];
                    VisitInputUses(function, table, (name, node, isSetting) =>
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
        }
        while (isGrowing);

        _settingInputSearch = null;
        while (_uncheckedLibraries.Count > 0)
        {
            CheckSettingInputUses(_uncheckedLibraries.Dequeue());
        }
    }

    /// <summary>
    /// Checks a loaded Library's Setting inputs once they're all found, keeping what's wrong at its own lines.
    /// </summary>
    private void CheckSettingInputUses(LoadedLibrary library)
    {
        if (_settingInputSearch != null)
        {
            _uncheckedLibraries.Enqueue(library);
            return;
        }

        InLibrary(library, () =>
        {
            foreach (var function in library.Functions.Values)
            {
                CheckSettingInputUses(function);
            }
        });

        library.Errors.Sort((a, b) => a.Position.CompareTo(b.Position));
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

        // Only a call's arguments can be Settings, so every other kind of expression is just its children
        void VisitExpression(Expression expression)
        {
            switch (expression)
            {
                case NameExpression name when inputs.Contains(name.Name):
                    use(name.Name, name, false);
                    break;
                case CallExpression call:
                    VisitCall(call);
                    break;
                default:
                    foreach (var child in expression.Children)
                    {
                        VisitExpression(child);
                    }

                    break;
            }
        }

        void VisitCall(CallExpression call)
        {
            if (call.Library == null && inputs.Contains(call.Function))
                use(call.Function, call, false);

            if (IsFunctionCall(call, functions))
            {
                var callee = FindFunctionIgnoringErrors(call, functions);
                var calleeSettings = callee == null ? null : GetSettingInputs(callee, FunctionsOf(callee));
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
    /// The Function a call names, or null when there's none, even in a Library with an error. Setting inputs come from
    /// the text alone, so they don't depend on which Library was checked first.
    /// </summary>
    private FunctionDefinition FindFunctionIgnoringErrors(CallExpression call, Dictionary<string, FunctionDefinition> functions)
    {
        if (call.Library == null)
            return functions[call.Function];

        FunctionDefinition function = null;
        LoadLibrary(call.Library)?.Functions.TryGetValue(call.Function, out function);
        return function;
    }

    /// <summary>
    /// Whether a named argument of a Block Function gives a constant: a Setting or the style. A Toggler's targets are
    /// placed blocks, so they're names to look up.
    /// </summary>
    private static bool GivesConstant(BlockType type, string name)
    {
        return GetNamedArgumentKind(type, name) is NamedArgumentKind.Style or NamedArgumentKind.Setting;
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

        _scope.SettingConstants.TryGetValue(name.Name, out constant);
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
