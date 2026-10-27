namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class SyntaxNode
{
    protected SyntaxNode(Position position)
    {
        Position = position;
    }

    public Position Position { get; }
}
