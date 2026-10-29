using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Expression : SyntaxNode
{
    protected Expression(Position position, int height = 1)
        : base(position)
    {
        Height = height;
    }

    /// <summary>
    /// The levels of expressions in this one, itself included, so a leaf is 1. Compile walks expressions recursively,
    /// so the parser caps this to keep a deep one from overflowing the stack.
    /// </summary>
    public int Height { get; }

    protected static int GetHeight(IEnumerable<Expression> children)
    {
        return children.Select(c => c.Height).DefaultIfEmpty(0).Max();
    }
}
