namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

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
