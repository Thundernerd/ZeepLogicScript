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

        var zero = GetLiteral(0);
        var math = BlockManifest.Math;
        var subtract = _graph.AddBlock(math, BlockManifest.MathOperator.Choose("subtract"));
        _graph.AddWire(zero, subtract, math.Inputs[0].Index);
        Connect(operand.Value, subtract, math.Inputs[1].Index);
        return new BlockOutput(subtract, math.Outputs[0].Index);
    }

    private BlockOutput? VisitBinary(BinaryExpression binary)
    {
        var left = EvaluateFeed(binary.Left);
        var right = EvaluateFeed(binary.Right);
        if (left == null || right == null)
            return null;

        var (type, setting) = GetOperatorBlock(binary.Operator);
        var block = _graph.AddBlock(type, setting);
        Connect(left.Value, block, type.Inputs[0].Index);
        Connect(right.Value, block, type.Inputs[1].Index);
        return new BlockOutput(block, type.Outputs[0].Index);
    }

    /// <summary>
    /// The Block Function an operator is shorthand for, and the Setting that picks the operation.
    /// </summary>
    private static (BlockType Type, Setting Setting) GetOperatorBlock(BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Or => (BlockManifest.Logic, BlockManifest.LogicOperator.Choose("or")),
            BinaryOperator.And => (BlockManifest.Logic, BlockManifest.LogicOperator.Choose("and")),
            BinaryOperator.Xor => (BlockManifest.Logic, BlockManifest.LogicOperator.Choose("xor")),
            BinaryOperator.Add => (BlockManifest.Math, BlockManifest.MathOperator.Choose("add")),
            BinaryOperator.Subtract => (BlockManifest.Math, BlockManifest.MathOperator.Choose("subtract")),
            BinaryOperator.Multiply => (BlockManifest.Math, BlockManifest.MathOperator.Choose("multiply")),
            BinaryOperator.Divide => (BlockManifest.Math, BlockManifest.MathOperator.Choose("divide")),
            BinaryOperator.Modulo => (BlockManifest.Math, BlockManifest.MathOperator.Choose("modulo")),
            BinaryOperator.Less => (BlockManifest.Math, BlockManifest.MathOperator.Choose("less")),
            BinaryOperator.Greater => (BlockManifest.Math, BlockManifest.MathOperator.Choose("greater")),
            BinaryOperator.LessEqual => (BlockManifest.Math, BlockManifest.MathOperator.Choose("less_equal")),
            BinaryOperator.GreaterEqual => (BlockManifest.Math, BlockManifest.MathOperator.Choose("greater_equal")),
            BinaryOperator.Equal => (BlockManifest.Math, BlockManifest.MathOperator.Choose("equal")),
            BinaryOperator.NotEqual => (BlockManifest.Math, BlockManifest.MathOperator.Choose("not_equal")),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }
}
