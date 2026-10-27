namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Statement : SyntaxNode
{
    protected Statement(int line, int column)
        : base(line, column)
    {
    }
}
