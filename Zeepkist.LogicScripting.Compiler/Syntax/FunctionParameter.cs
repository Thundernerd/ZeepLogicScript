namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// A name on a Function's first line: the Function's own, or one of its inputs or outputs.
/// </summary>
internal sealed class FunctionParameter : SyntaxNode
{
    public FunctionParameter(Position position, string name)
        : base(position)
    {
        Name = name;
    }

    public string Name { get; }
}
