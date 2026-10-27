namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Statement : SyntaxNode
{
    protected Statement(Position position)
        : base(position)
    {
    }
}
