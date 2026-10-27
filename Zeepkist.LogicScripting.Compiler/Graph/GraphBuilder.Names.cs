using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private readonly Dictionary<string, Definition> _definitions = [];
    private readonly Dictionary<string, Resolved> _names = [];
    private readonly HashSet<string> _failedNames = [];
    private Definition _current;

    /// <summary>
    /// Notes the first definition of each name up front, so a use can tell a later definition from no definition.
    /// </summary>
    private void CollectDefinitions(List<Statement> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is Definition definition
                && !IsReserved(definition.Name)
                && !_definitions.ContainsKey(definition.Name))
                _definitions.Add(definition.Name, definition);
        }
    }

    private void VisitDefinition(Definition definition)
    {
        if (BlockManifest.TryGetFunction(definition.Name, out _))
        {
            Error(definition, $"'{definition.Name}' is a Block Function and can't be used as a name");
            Evaluate(definition.Value);
            return;
        }

        if (IsReserved(definition.Name))
        {
            Error(definition, $"'{definition.Name}' is a reserved word and can't be used as a name");
            Evaluate(definition.Value);
            return;
        }

        if (_definitions[definition.Name] != definition)
        {
            Error(definition, $"'{definition.Name}' is already defined");
            Evaluate(definition.Value);
            return;
        }

        _current = definition;
        var value = Evaluate(definition.Value);
        _current = null;

        if (value == null)
            _failedNames.Add(definition.Name);
        else
            _names.Add(definition.Name, value.Value);
    }

    /// <summary>
    /// Block Functions, and the words Settings use for true and false.
    /// </summary>
    private static bool IsReserved(string name)
    {
        return name is "true" or "false" || BlockManifest.TryGetFunction(name, out _);
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
            && _definitions.ContainsKey(name.Name)
            && !_names.ContainsKey(name.Name)
            && !_failedNames.Contains(name.Name);
    }

    private Resolved? LookUp(NameExpression name)
    {
        if (_names.TryGetValue(name.Name, out var value))
            return value;

        // Its definition already reported why it failed
        if (_failedNames.Contains(name.Name))
            return null;

        if (_current != null && _current.Name == name.Name)
            return Fail(name, $"'{name.Name}' can't refer to itself outside delay(...)");

        if (_definitions.TryGetValue(name.Name, out var later))
            return Fail(name, $"'{name.Name}' is defined later, on line {later.Position.Line}; only delay(...) can refer ahead");

        return Fail(name, BlockManifest.TryGetFunction(name.Name, out _)
            ? $"'{name.Name}' is a Block Function; call it with ( )"
            : $"'{name.Name}' isn't defined");
    }
}
