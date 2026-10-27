namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Expression : SyntaxNode
{
    protected Expression(Position position)
        : base(position)
    {
    }
}
