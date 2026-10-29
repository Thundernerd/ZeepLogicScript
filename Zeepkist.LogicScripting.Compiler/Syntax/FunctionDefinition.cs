using System.Collections.Generic;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// <c>function name(inputs) -> outputs</c>, its body, and <c>end</c>: a wiring pattern each call expands inline.
/// </summary>
internal sealed class FunctionDefinition : Statement
{
    public const string Keyword = "function";
    public const string EndKeyword = "end";

    public FunctionDefinition(
        Position position,
        FunctionParameter name,
        List<FunctionParameter> inputs,
        List<FunctionParameter> outputs,
        List<Statement> body,
        bool isComplete)
        : base(position)
    {
        Name = name;
        Inputs = inputs;
        Outputs = outputs;
        Body = body;
        IsComplete = isComplete;
    }

    public FunctionParameter Name { get; }
    public List<FunctionParameter> Inputs { get; }
    public List<FunctionParameter> Outputs { get; }
    public List<Statement> Body { get; }

    /// <summary>
    /// False when its first line or its <c>end</c> has a syntax error, already reported; calls to it are then left
    /// unchecked.
    /// </summary>
    public bool IsComplete { get; }
}
