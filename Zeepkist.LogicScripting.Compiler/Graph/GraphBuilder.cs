using System.Collections.Generic;
using System.Globalization;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// Checks the parsed Script and turns it into a block graph: one block per Block Function call or operator,
/// one shared Generator per distinct literal value.
/// </summary>
internal sealed class GraphBuilder
{
    private readonly List<Diagnostic> _diagnostics;
    private readonly BlockGraph _graph = new();
    private readonly Dictionary<string, BlockOutput> _names = [];
    private readonly Dictionary<long, BlockOutput> _literals = [];

    private GraphBuilder(List<Diagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public static BlockGraph Build(List<Statement> statements, List<Diagnostic> diagnostics)
    {
        var builder = new GraphBuilder(diagnostics);
        foreach (var statement in statements)
        {
            builder.Visit(statement);
        }

        return builder._graph;
    }

    private void Visit(Statement statement)
    {
        switch (statement)
        {
            case Definition definition:
                VisitDefinition(definition);
                break;
            case ExpressionStatement { Expression: CallExpression call }:
                VisitCall(call);
                break;
            case ExpressionStatement expression:
                Evaluate(expression.Expression);
                Error(expression, "A value on its own line does nothing; give it a name or pass it to a call");
                break;
        }
    }

    private void VisitDefinition(Definition definition)
    {
        if (BlockManifest.TryGetFunction(definition.Name, out _))
        {
            Error(definition, $"'{definition.Name}' is a Block Function and can't be used as a name");
            Evaluate(definition.Value);
            return;
        }

        if (_names.ContainsKey(definition.Name))
        {
            Error(definition, $"'{definition.Name}' is already defined");
            Evaluate(definition.Value);
            return;
        }

        var value = Evaluate(definition.Value);
        if (value != null)
            _names.Add(definition.Name, value.Value);
    }

    /// <summary>
    /// Returns the output the expression stands for, or null after reporting why it has none.
    /// </summary>
    private BlockOutput? Evaluate(Expression expression)
    {
        switch (expression)
        {
            case IntegerLiteral literal:
                return GetLiteral(literal.Value);

            case NameExpression name:
                if (_names.TryGetValue(name.Name, out var output))
                    return output;

                Error(name, BlockManifest.TryGetFunction(name.Name, out _)
                    ? $"'{name.Name}' is a Block Function; call it with ( )"
                    : $"'{name.Name}' isn't defined");
                return null;

            case BinaryExpression binary:
                return VisitAnd(binary);

            case CallExpression call:
                var block = VisitCall(call);
                if (block == null)
                    return null;

                if (block.Type.Outputs.Count == 1)
                    return new BlockOutput(block, block.Type.Outputs[0].Index);

                Error(call, $"'{call.Function}' has no single output to use as a value");
                return null;

            default:
                return null;
        }
    }

    private BlockOutput? VisitAnd(BinaryExpression binary)
    {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);
        if (left == null || right == null)
            return null;

        var type = BlockManifest.Logic;
        var block = _graph.AddBlock(type, Setting.Int(BlockManifest.LogicOperatorKey, BlockManifest.LogicOperatorAnd));
        _graph.AddWire(left.Value, block, type.Inputs[0].Index);
        _graph.AddWire(right.Value, block, type.Inputs[1].Index);
        return new BlockOutput(block, type.Outputs[0].Index);
    }

    private BlockNode VisitCall(CallExpression call)
    {
        List<BlockOutput?> arguments = [];
        foreach (var argument in call.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (!BlockManifest.TryGetFunction(call.Function, out var type))
        {
            Error(call, $"'{call.Function}' isn't a Block Function");
            return null;
        }

        if (arguments.Count > type.Inputs.Count)
        {
            Error(call, $"'{call.Function}' takes at most {type.Inputs.Count} inputs, got {arguments.Count}");
            return null;
        }

        if (arguments.Contains(null))
            return null;

        var block = _graph.AddBlock(type);
        for (var i = 0; i < arguments.Count; i++)
        {
            _graph.AddWire(arguments[i].Value, block, type.Inputs[i].Index);
        }

        return block;
    }

    private BlockOutput GetLiteral(long value)
    {
        if (_literals.TryGetValue(value, out var output))
            return output;

        var type = BlockManifest.Generator;
        var power = Setting.Text(BlockManifest.GeneratorPowerKey, value.ToString(CultureInfo.InvariantCulture));
        output = new BlockOutput(_graph.AddBlock(type, power), type.Outputs[0].Index);
        _literals.Add(value, output);
        return output;
    }

    private void Error(SyntaxNode node, string message)
    {
        _diagnostics.Add(new Diagnostic(node.Line, node.Column, message));
    }
}
