namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal sealed class NameExpression : Expression
{
    public NameExpression(Position position, string name)
        : base(position, [])
    {
        Name = name;
    }

    public string Name { get; }
}
