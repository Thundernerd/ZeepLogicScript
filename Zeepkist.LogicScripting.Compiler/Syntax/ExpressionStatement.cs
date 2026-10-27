namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// An expression on a line of its own, such as a stand-alone call.
/// </summary>
internal sealed class ExpressionStatement : Statement
{
    public ExpressionStatement(Expression expression)
        : base(expression.Position)
    {
        Expression = expression;
    }

    public Expression Expression { get; }
}
