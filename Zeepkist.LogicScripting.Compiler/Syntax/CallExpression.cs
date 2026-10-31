using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// A Block Function call. Positional arguments are inputs in Connector order; named ones are Settings or inputs.
/// </summary>
internal sealed class CallExpression : Expression
{
    public CallExpression(
        Position position,
        string library,
        string function,
        List<Expression> arguments,
        List<NamedArgument> namedArguments)
        : base(position, [.. arguments, .. namedArguments.Select(a => a.Value)])
    {
        Library = library;
        Function = function;
        Arguments = arguments;
        NamedArguments = namedArguments;
    }

    /// <summary>
    /// The Library a qualified call, <c>timing.edge(x)</c>, names; null for any other call.
    /// </summary>
    public string Library { get; }

    public string Function { get; }

    /// <summary>
    /// The name as written, with its Library.
    /// </summary>
    public string QualifiedName => Library == null ? Function : $"{Library}.{Function}";
    public List<Expression> Arguments { get; }
    public List<NamedArgument> NamedArguments { get; }
}
