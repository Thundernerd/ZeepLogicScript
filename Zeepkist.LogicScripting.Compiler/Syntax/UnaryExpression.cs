namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class UnaryExpression : Expression
{
    public UnaryExpression(Position position, UnaryOperator op, Expression operand)
        : base(position, [operand])
    {
        Operator = op;
        Operand = operand;
    }

    public UnaryOperator Operator { get; }
    public Expression Operand { get; }
}
