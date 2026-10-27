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
        var op = BinaryOperator.Subtract;
        var subtract = _graph.AddBlock(op.Block, op.Setting);
        _graph.AddWire(GetLiteral(0), subtract, op.Block.Inputs[0].Index);
        Connect(operand.Value, subtract, op.Block.Inputs[1].Index);
        return new BlockOutput(subtract, op.Block.Outputs[0].Index);
    }

    private BlockOutput? VisitBinary(BinaryExpression binary)
    {
        var left = EvaluateFeed(binary.Left);
        var right = EvaluateFeed(binary.Right);
        if (left == null || right == null)
            return null;

        var type = binary.Operator.Block;
        var block = _graph.AddBlock(type, binary.Operator.Setting);
        Connect(left.Value, block, type.Inputs[0].Index);
        Connect(right.Value, block, type.Inputs[1].Index);
        return new BlockOutput(block, type.Outputs[0].Index);
    }
}
