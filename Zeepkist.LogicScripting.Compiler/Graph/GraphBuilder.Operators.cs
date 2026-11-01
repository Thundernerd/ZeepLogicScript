using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private InputFeed VisitUnary(UnaryExpression unary)
    {
        var operand = EvaluateFeed(unary.Operand);
        if (operand is FailedValue)
            return operand;

        var op = unary.Operator;
        List<Func<InputFeed>> inputs = [];
        foreach (var literal in op.LeadingLiterals)
        {
            inputs.Add(() => new OutputValue(GetLiteral(unary, literal)));
        }

        inputs.Add(() => operand);
        return AddOperatorBlock(unary, op.Block, inputs);
    }

    private InputFeed VisitBinary(BinaryExpression binary)
    {
        var left = EvaluateFeed(binary.Left);
        var right = EvaluateFeed(binary.Right);
        if (left is FailedValue || right is FailedValue)
            return FailedValue.Instance;

        var op = binary.Operator;
        return AddOperatorBlock(binary, op.Block, [() => left, () => right]);
    }

    /// <summary>
    /// Each input is asked for after the block is placed, so a Generator it creates comes after the block.
    /// </summary>
    private OutputValue AddOperatorBlock(SyntaxNode origin, OperatorBlock operatorBlock, IReadOnlyList<Func<InputFeed>> inputs)
    {
        var type = operatorBlock.Type;
        var block = AddBlock(origin, type, type.DefaultStyle, operatorBlock.Setting);
        for (var i = 0; i < inputs.Count; i++)
        {
            inputs[i]().Connect(this, block, type.Inputs[i].Index);
        }

        return new OutputValue(block, block.Outputs[0].Index);
    }
}
