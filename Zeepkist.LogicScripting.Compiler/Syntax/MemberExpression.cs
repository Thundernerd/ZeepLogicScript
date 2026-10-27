namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>target.output</c>: one output of a multi-output block. Its position is the output name's.
/// </summary>
internal sealed class MemberExpression : Expression
{
    public MemberExpression(Position position, Expression target, string output)
        : base(position)
    {
        Target = target;
        Output = output;
    }

    public Expression Target { get; }
    public string Output { get; }
}
