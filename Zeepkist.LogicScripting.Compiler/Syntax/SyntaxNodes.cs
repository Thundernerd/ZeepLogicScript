using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class SyntaxNode
{
    protected SyntaxNode(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }
    public int Column { get; }
}

internal abstract class Statement : SyntaxNode
{
    protected Statement(int line, int column)
        : base(line, column)
    {
    }
}

/// <summary>
/// <c>name = expression</c>
/// </summary>
internal sealed class Definition : Statement
{
    public Definition(int line, int column, string name, Expression value)
        : base(line, column)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public Expression Value { get; }
}

/// <summary>
/// An expression on a line of its own, such as a stand-alone call.
/// </summary>
internal sealed class ExpressionStatement : Statement
{
    public ExpressionStatement(Expression expression)
        : base(expression.Line, expression.Column)
    {
        Expression = expression;
    }

    public Expression Expression { get; }
}

internal abstract class Expression : SyntaxNode
{
    protected Expression(int line, int column)
        : base(line, column)
    {
    }
}

internal sealed class IntegerLiteral : Expression
{
    public IntegerLiteral(int line, int column, long value)
        : base(line, column)
    {
        Value = value;
    }

    public long Value { get; }
}

internal sealed class NameExpression : Expression
{
    public NameExpression(int line, int column, string name)
        : base(line, column)
    {
        Name = name;
    }

    public string Name { get; }
}

internal enum BinaryOperator
{
    And
}

internal sealed class BinaryExpression : Expression
{
    public BinaryExpression(int line, int column, BinaryOperator op, Expression left, Expression right)
        : base(line, column)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public BinaryOperator Operator { get; }
    public Expression Left { get; }
    public Expression Right { get; }
}

/// <summary>
/// A Block Function call; its arguments are positional, in Connector order.
/// </summary>
internal sealed class CallExpression : Expression
{
    public CallExpression(int line, int column, string function, List<Expression> arguments)
        : base(line, column)
    {
        Function = function;
        Arguments = arguments;
    }

    public string Function { get; }
    public List<Expression> Arguments { get; }
}
