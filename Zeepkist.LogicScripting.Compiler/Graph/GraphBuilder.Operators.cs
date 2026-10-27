using System;
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

        if (unary.Operator == UnaryOperator.Not)
        {
            var digitizer = BlockManifest.Digitizer;
            var block = _graph.AddBlock(digitizer, Setting.Int(BlockManifest.DigitizerInvert.Key, 1));
            Connect(operand.Value, block, digitizer.Inputs[0].Index);
            return new BlockOutput(block, digitizer.Outputs[0].Index);
        }

        // -x is 0 - x
        return AddOperatorBlock(BinaryOperator.Subtract, () => InputFeed.Of(GetLiteral(0)), operand.Value);
    }

    private BlockOutput? VisitBinary(BinaryExpression binary)
    {
        var left = EvaluateFeed(binary.Left);
        var right = EvaluateFeed(binary.Right);
        if (left == null || right == null)
            return null;

        return AddOperatorBlock(binary.Operator, () => left.Value, right.Value);
    }

    /// <summary>
    /// Places the operator's block and wires its operands. <paramref name="left"/> is asked for after the block is
    /// placed, so a Generator it creates comes after the block.
    /// </summary>
    private BlockOutput AddOperatorBlock(BinaryOperator op, Func<InputFeed> left, InputFeed right)
    {
        var block = _graph.AddBlock(op.Block, op.Setting);
        Connect(left(), block, op.Block.Inputs[0].Index);
        Connect(right, block, op.Block.Inputs[1].Index);
        return new BlockOutput(block, op.Block.Outputs[0].Index);
    }
}
