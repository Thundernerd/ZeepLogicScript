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

internal enum UnaryOperator
{
    Not,
    Negate
}

internal sealed class UnaryExpression : Expression
{
    public UnaryExpression(int line, int column, UnaryOperator op, Expression operand)
        : base(line, column)
    {
        Operator = op;
        Operand = operand;
    }

    public UnaryOperator Operator { get; }
    public Expression Operand { get; }
}

internal enum BinaryOperator
{
    And,
    Or,
    Xor,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,
    Equal,
    NotEqual
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
/// A Block Function call. Positional arguments are inputs in Connector order; named ones are Settings or inputs.
/// </summary>
internal sealed class CallExpression : Expression
{
    public CallExpression(int line, int column, string function, List<Expression> arguments, List<NamedArgument> namedArguments)
        : base(line, column)
    {
        Function = function;
        Arguments = arguments;
        NamedArguments = namedArguments;
    }

    public string Function { get; }
    public List<Expression> Arguments { get; }
    public List<NamedArgument> NamedArguments { get; }
}

/// <summary>
/// <c>name: value</c> in a call.
/// </summary>
internal sealed class NamedArgument : SyntaxNode
{
    public NamedArgument(int line, int column, string name, Expression value, int valueLine, int valueColumn)
        : base(line, column)
    {
        Name = name;
        Value = value;
        ValueLine = valueLine;
        ValueColumn = valueColumn;
    }

    public string Name { get; }
    public Expression Value { get; }

    /// <summary>
    /// Where the value starts; an operator expression's own position is its operator.
    /// </summary>
    public int ValueLine { get; }

    public int ValueColumn { get; }
}

/// <summary>
/// <c>target.output</c>: one output of a multi-output block. Its position is the output name's.
/// </summary>
internal sealed class MemberExpression : Expression
{
    public MemberExpression(int line, int column, Expression target, string output)
        : base(line, column)
    {
        Target = target;
        Output = output;
    }

    public Expression Target { get; }
    public string Output { get; }
}
