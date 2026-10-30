using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private readonly Scope _topScope = new(null, null, null);
    private Scope _scope;

    /// <summary>
    /// Notes the first definition of each name up front, so a use can tell a later definition from no definition.
    /// </summary>
    private void CollectDefinitions(Scope scope, List<Statement> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is Definition definition
                && !IsReserved(definition.Name)
                && !FunctionsOf(scope).ContainsKey(definition.Name)
                && !scope.Definitions.ContainsKey(definition.Name))
                scope.Definitions.Add(definition.Name, definition);
        }
    }

    private void VisitDefinition(Definition definition)
    {
        if (GetNameError(definition.Name) is { } nameError)
        {
            Error(definition, nameError);
            Evaluate(definition.Value);
            return;
        }

        if (_scope.Function != null && _scope.Function.Inputs.Exists(i => i.Name == definition.Name))
        {
            Error(definition, $"'{definition.Name}' is an input of '{_scope.Function.Name.Name}' and can't be defined again");
            Evaluate(definition.Value);
            return;
        }

        if (_scope.Definitions[definition.Name] != definition)
        {
            Error(definition, $"'{definition.Name}' is already defined");
            Evaluate(definition.Value);
            return;
        }

        _scope.Current = definition;
        var value = Evaluate(definition.Value);
        _scope.Current = null;

        if (value == null)
            _scope.FailedNames.Add(definition.Name);
        else
            _scope.Names.Add(definition.Name, value);
    }

    /// <summary>
    /// Why a name can't be defined, or null when it can.
    /// </summary>
    private string GetNameError(string name)
    {
        return GetReservedNameError(name)
            ?? (Functions.ContainsKey(name) ? $"'{name}' is a function and can't be used as a name" : null);
    }

    private static string GetReservedNameError(string name)
    {
        if (BlockManifest.TryGetFunction(name, out _))
            return $"'{name}' is a Block Function and can't be used as a name";

        return IsReserved(name) ? $"'{name}' is a reserved word and can't be used as a name" : null;
    }

    /// <summary>
    /// Block Functions, <c>placed</c>, the words around a Function, and the words Settings use for true and false.
    /// </summary>
    internal static bool IsReserved(string name)
    {
        return name is "true" or "false" or PlacedFunction or FunctionDefinition.Keyword or FunctionDefinition.EndKeyword
            || BlockManifest.TryGetFunction(name, out _);
    }

    private bool IsForwardReference(Expression expression)
    {
        var name = expression switch
        {
            NameExpression n => n,
            MemberExpression { Target: NameExpression n } => n,
            _ => null
        };

        return name != null
            && _scope.Definitions.ContainsKey(name.Name)
            && !_scope.Names.ContainsKey(name.Name)
            && !_scope.FailedNames.Contains(name.Name);
    }

    private Resolved LookUp(NameExpression name)
    {
        // A Setting input used as a Signal is reported with its Function
        if (_scope.Names.TryGetValue(name.Name, out var value))
            return value is SettingConstant ? null : value;

        // Its definition already reported why it failed
        if (_scope.FailedNames.Contains(name.Name))
            return null;

        if (_scope.Current != null && _scope.Current.Name == name.Name)
            return Fail(name, $"'{name.Name}' can't refer to itself outside delay(...)");

        if (_scope.Definitions.TryGetValue(name.Name, out var later))
            return Fail(name, $"'{name.Name}' is defined later, on line {later.Position.Line}, so only delay(...) can refer to it");

        if (name.Name == PlacedFunction)
            return Fail(name, $"{PlacedFunction} takes a UID. Call it like {PlacedFunction}(\"AbC12\")");

        if (Functions.ContainsKey(name.Name))
            return Fail(name, $"'{name.Name}' is a function. Call it with ( )");

        if (GetOutOfScopeMessage(name.Name) is { } outOfScope)
            return Fail(name, outOfScope);

        return Fail(name, BlockManifest.TryGetFunction(name.Name, out _)
            ? $"'{name.Name}' is a Block Function. Call it with ( )"
            : $"'{name.Name}' isn't defined");
    }

    /// <summary>
    /// Why a Function's body can't use a name the top level defines, or null when it doesn't. A Library's body
    /// comes from other text, so the top level's names are no hint there.
    /// </summary>
    private string GetOutOfScopeMessage(string name)
    {
        if (_scope.Function == null || _scope.Library != null || !_topScope.Definitions.ContainsKey(name))
            return null;

        return $"'{name}' isn't defined in '{_scope.Function.Name.Name}'. A function only sees its inputs and its own names";
    }
}
