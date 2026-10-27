namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

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
