using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// A Library's Functions are called by their qualified name, <c>timing.edge(x)</c>, and expand like the Script's own.
/// Its text lives on the player's machine, not in the text being compiled, so what goes wrong in it is reported where
/// that text calls into it.
/// </summary>
internal sealed partial class GraphBuilder
{
    private readonly Func<string, string> _findLibrary;

    /// <summary>
    /// Whether the text being compiled is itself a Library, checked on its own.
    /// </summary>
    private readonly bool _isLibrary;

    /// <summary>
    /// Each Library asked for so far, or null when there's none by that name.
    /// </summary>
    private readonly Dictionary<string, LoadedLibrary> _libraries = [];

    private readonly Dictionary<FunctionDefinition, LoadedLibrary> _libraryFunctions = [];

    /// <summary>
    /// The Functions the current scope calls without a prefix.
    /// </summary>
    private Dictionary<string, FunctionDefinition> Functions => FunctionsOf(_scope);

    private Dictionary<string, FunctionDefinition> FunctionsOf(Scope scope)
    {
        return scope.Library?.Functions ?? _scriptFunctions;
    }

    /// <summary>
    /// The Functions <paramref name="function"/>'s body calls without a prefix.
    /// </summary>
    private Dictionary<string, FunctionDefinition> FunctionsOf(FunctionDefinition function)
    {
        return _libraryFunctions.TryGetValue(function, out var library) ? library.Functions : _scriptFunctions;
    }

    /// <summary>
    /// The Functions of the Library by <paramref name="name"/>, each with the inputs its body uses as a Setting; empty
    /// when there's none by that name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="findLibrary">A Library's text by its name, or null when there's none.</param>
    /// <param name="hasErrors">Whether its text has an error, which stops every call into it.</param>
    public static List<(FunctionDefinition Function, IReadOnlyCollection<string> SettingInputs)> DescribeLibrary(
        string name,
        Func<string, string> findLibrary,
        out bool hasErrors)
    {
        var builder = new GraphBuilder([], [], _ => null, findLibrary, isLibrary: true);
        var library = builder.LoadLibrary(name);
        hasErrors = library?.Errors.Count > 0;
        if (library == null)
            return [];

        return library.Functions.Values
            .Select(f => (f, (IReadOnlyCollection<string>)builder.GetSettingInputs(f, library.Functions).Keys))
            .ToList();
    }

    /// <summary>
    /// The Function a call names, or null after reporting why there's none.
    /// </summary>
    private FunctionDefinition FindFunction(CallExpression call)
    {
        return FindFunction(call, Functions, message => Error(call, message));
    }

    /// <summary>
    /// The Function a call names, or null when its Library has none to expand, telling <paramref name="report"/> why.
    /// <paramref name="functions"/> are the ones the caller calls without a prefix, and must hold an unprefixed call's,
    /// as <see cref="IsFunctionCall(CallExpression, Dictionary{string, FunctionDefinition})"/> checks.
    /// </summary>
    private FunctionDefinition FindFunction(
        CallExpression call,
        Dictionary<string, FunctionDefinition> functions,
        Action<string> report)
    {
        if (call.Library == null)
            return functions[call.Function];

        var library = LoadLibrary(call.Library);
        if (library == null)
        {
            report($"No Library '{call.Library}' on this machine");
            return null;
        }

        if (library.Errors.Count > 0)
        {
            foreach (var error in library.Errors)
            {
                report($"Library '{library.Name}' has an error on line {error.Position.Line}: {error.Message}");
            }

            return null;
        }

        if (library.Functions.TryGetValue(call.Function, out var function))
            return function;

        report($"'{call.Library}' has no function '{call.Function}'");
        return null;
    }

    /// <summary>
    /// Parses a Library the first time it's asked for. Null when there's none by that name.
    /// </summary>
    private LoadedLibrary LoadLibrary(string name)
    {
        if (_libraries.TryGetValue(name, out var library))
            return library;

        var text = _findLibrary(name);
        library = text == null ? null : new LoadedLibrary(name);
        _libraries.Add(name, library);
        if (library == null)
            return null;

        var tokens = Lexer.Tokenize(text, library.Errors);
        var statements = Parser.Parse(tokens, library.Errors);

        // Its own errors are kept at its own lines, to report at every call into it
        var (diagnostics, reported, scope) = (_diagnostics, _reported, _scope);
        (_diagnostics, _reported, _scope) = (library.Errors, [], new Scope(null, library, null));
        CollectFunctions(statements);
        ReportStatementsOutsideFunctions(statements);
        (_diagnostics, _reported, _scope) = (diagnostics, reported, scope);

        library.Errors.Sort((a, b) => a.Position.CompareTo(b.Position));
        return library;
    }

    private void ReportStatementsOutsideFunctions(List<Statement> statements)
    {
        foreach (var statement in statements)
        {
            if (statement is not FunctionDefinition)
                Error(statement, "A Library holds only functions. Move this line into one");
        }
    }

    /// <summary>
    /// A Library's Function with its Library's name, <c>timing.edge</c>; any other just by its own.
    /// </summary>
    private string GetQualifiedName(FunctionDefinition function)
    {
        return _libraryFunctions.TryGetValue(function, out var library)
            ? $"{library.Name}.{function.Name.Name}"
            : function.Name.Name;
    }
}
