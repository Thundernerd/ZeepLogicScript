using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    private BlockOutput? VisitUnary(UnaryExpression unary)
    {
        var operand = EvaluateFeed(unary.Operand);
        if (operand == null)
            return null;

        var op = unary.Operator;
        List<Func<InputFeed>> inputs = [];
        foreach (var literal in op.LeadingLiterals)
        {
            inputs.Add(() => InputFeed.Of(GetLiteral(literal)));
        }

        inputs.Add(() => operand.Value);
        return AddOperatorBlock(op.Block, op.Setting, inputs);
    }

    private BlockOutput? VisitBinary(BinaryExpression binary)
    {
        var left = EvaluateFeed(binary.Left);
        var right = EvaluateFeed(binary.Right);
        if (left == null || right == null)
            return null;

        var op = binary.Operator;
        return AddOperatorBlock(op.Block, op.Setting, [() => left.Value, () => right.Value]);
    }

    /// <summary>
    /// Places an operator's block and wires <paramref name="inputs"/> into its first inputs, in order. Each input is
    /// asked for after the block is placed, so a Generator it creates comes after the block.
    /// </summary>
    private BlockOutput AddOperatorBlock(BlockType type, Setting setting, IReadOnlyList<Func<InputFeed>> inputs)
    {
        var block = _graph.AddBlock(type, setting);
        for (var i = 0; i < inputs.Count; i++)
        {
            Connect(inputs[i](), block, type.Inputs[i].Index);
        }

        return new BlockOutput(block, type.Outputs[0].Index);
    }
}
