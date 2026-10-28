namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>default name: value</c>: a Setting for every block the Script places that doesn't give it itself.
/// </summary>
internal sealed class DefaultStatement : Statement
{
    public const string Keyword = "default";

    public DefaultStatement(Position position, NamedArgument setting)
        : base(position)
    {
        Setting = setting;
    }

    public NamedArgument Setting { get; }
}
