using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Expression : SyntaxNode
{
    protected Expression(Position position, int depth = 1)
        : base(position)
    {
        Depth = depth;
    }

    /// <summary>
    /// How many levels of expressions this one nests, itself included, so a leaf is 1. Compile walks expressions
    /// recursively, so the parser caps this to keep a deep one from overflowing the stack.
    /// </summary>
    public int Depth { get; }

    protected static int GetDepth(IEnumerable<Expression> children)
    {
        return children.Select(c => c.Depth).DefaultIfEmpty(0).Max();
    }
}
