namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class NameExpression : Expression
{
    public NameExpression(int line, int column, string name)
        : base(line, column)
    {
        Name = name;
    }

    public string Name { get; }
}
