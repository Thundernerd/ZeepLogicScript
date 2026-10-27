namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>name: value</c> in a call.
/// </summary>
internal sealed class NamedArgument : SyntaxNode
{
    public NamedArgument(Position position, string name, Expression value, Position valuePosition)
        : base(position)
    {
        Name = name;
        Value = value;
        ValuePosition = valuePosition;
    }

    public string Name { get; }
    public Expression Value { get; }

    /// <summary>
    /// Where the value starts; an operator expression's own position is its operator.
    /// </summary>
    public Position ValuePosition { get; }
}
