namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class Expression : SyntaxNode
{
    protected Expression(int line, int column)
        : base(line, column)
    {
    }
}
