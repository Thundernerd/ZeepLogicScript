namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class IntegerLiteral : Expression
{
    public IntegerLiteral(Position position, long value)
        : base(position, [])
    {
        Value = value;
    }

    public long Value { get; }
}
