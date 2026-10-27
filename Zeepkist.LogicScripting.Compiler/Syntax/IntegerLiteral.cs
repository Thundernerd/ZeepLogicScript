namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class IntegerLiteral : Expression
{
    public IntegerLiteral(int line, int column, long value)
        : base(line, column)
    {
        Value = value;
    }

    public long Value { get; }
}
