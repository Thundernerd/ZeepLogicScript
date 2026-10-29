using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>[a, b]</c>, like a Toggler's targets.
/// </summary>
internal sealed class ListExpression : Expression
{
    public ListExpression(Position position, List<Expression> items)
        : base(position, 1 + GetDepth(items))
    {
        Items = items;
    }

    public List<Expression> Items { get; }
}
