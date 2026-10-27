namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class BinaryExpression : Expression
{
    public BinaryExpression(Position position, BinaryOperator op, Expression left, Expression right)
        : base(position)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public BinaryOperator Operator { get; }
    public Expression Left { get; }
    public Expression Right { get; }
}
