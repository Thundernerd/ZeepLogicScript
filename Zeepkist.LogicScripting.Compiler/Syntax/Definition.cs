namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

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
