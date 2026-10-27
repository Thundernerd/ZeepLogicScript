namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

internal abstract class SyntaxNode
{
    protected SyntaxNode(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }
    public int Column { get; }
}
