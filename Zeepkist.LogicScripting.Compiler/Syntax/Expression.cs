using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Expression : SyntaxNode
{
    protected Expression(Position position, IReadOnlyList<Expression> children)
        : base(position)
    {
        Children = children;
        Depth = 1 + children.Select(c => c.Depth).DefaultIfEmpty(0).Max();
    }

    /// <summary>
    /// The expressions this one is made of, in the order they're written. A walk that only looks for names can go
    /// through these without knowing each kind of expression.
    /// </summary>
    public IReadOnlyList<Expression> Children { get; }

    /// <summary>
    /// How many levels of expressions this one nests, itself included, so a leaf is 1. Compile walks expressions
    /// recursively, so the parser caps this to keep a deep one from overflowing the stack.
    /// </summary>
    public int Depth { get; }
}
