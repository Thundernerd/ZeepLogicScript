namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// Text in quotes, like the UID in <c>placed("UID")</c>.
/// </summary>
internal sealed class StringLiteral : Expression
{
    public StringLiteral(Position position, string value)
        : base(position, [])
    {
        Value = value;
    }

    public string Value { get; }
}
