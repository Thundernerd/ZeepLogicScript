namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>target.output</c>: one output of a multi-output block. Its position is the output name's.
/// </summary>
internal sealed class MemberExpression : Expression
{
    public MemberExpression(int line, int column, Expression target, string output)
        : base(line, column)
    {
        Target = target;
        Output = output;
    }

    public Expression Target { get; }
    public string Output { get; }
}
