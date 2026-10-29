using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// A Block Function call. Positional arguments are inputs in Connector order; named ones are Settings or inputs.
/// </summary>
internal sealed class CallExpression : Expression
{
    public CallExpression(Position position, string function, List<Expression> arguments, List<NamedArgument> namedArguments)
        : base(position, 1 + GetDepth(arguments.Concat(namedArguments.Select(a => a.Value))))
    {
        Function = function;
        Arguments = arguments;
        NamedArguments = namedArguments;
    }

    public string Function { get; }
    public List<Expression> Arguments { get; }
    public List<NamedArgument> NamedArguments { get; }
}
