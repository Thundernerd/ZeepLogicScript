namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>name: value</c> in a call.
/// </summary>
internal sealed class NamedArgument : SyntaxNode
{
    public NamedArgument(int line, int column, string name, Expression value, int valueLine, int valueColumn)
        : base(line, column)
    {
        Name = name;
        Value = value;
        ValueLine = valueLine;
        ValueColumn = valueColumn;
    }

    public string Name { get; }
    public Expression Value { get; }

    /// <summary>
    /// Where the value starts; an operator expression's own position is its operator.
    /// </summary>
    public int ValueLine { get; }

    public int ValueColumn { get; }
}
