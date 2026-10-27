using System.Collections.Generic;
using System.Globalization;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// Checks the parsed Script and turns it into a block graph: one block per Block Function call or operator,
/// one shared Generator per distinct literal value. Names are defined before use, once each; only inside
/// <c>delay(...)</c> can a name refer to a later definition or to itself, so every cycle passes through a Delay.
/// </summary>
/// <remarks>
/// Split by responsibility: names and definitions in <c>GraphBuilder.Names.cs</c>, Wires and the ones
/// <c>delay(...)</c> defers in <c>GraphBuilder.Wiring.cs</c>, operators in <c>GraphBuilder.Operators.cs</c>,
/// Block Function calls in <c>GraphBuilder.Calls.cs</c> and their Settings in <c>GraphBuilder.Settings.cs</c>.
/// </remarks>
internal sealed partial class GraphBuilder
{
    private readonly List<Diagnostic> _diagnostics;
    private readonly BlockGraph _graph = new();
    private readonly Dictionary<long, BlockOutput> _literals = [];

    private GraphBuilder(List<Diagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public static BlockGraph Build(List<Statement> statements, List<Diagnostic> diagnostics)
    {
        var builder = new GraphBuilder(diagnostics);
        builder.CollectDefinitions(statements);

        foreach (var statement in statements)
        {
            builder.Visit(statement);
        }

        builder.ConnectDeferredWires();
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

    /// <summary>
    /// Returns what the expression stands for, or null after reporting why it stands for nothing.
    /// </summary>
    private Resolved? Evaluate(Expression expression)
    {
        switch (expression)
        {
            case IntegerLiteral literal:
                return Resolved.Of(GetLiteral(literal.Value));

            case NameExpression name:
                return LookUp(name);

            case MemberExpression member:
                return VisitMember(member);

            case UnaryExpression unary:
                return Resolved.Of(VisitUnary(unary));

            case BinaryExpression binary:
                return Resolved.Of(VisitBinary(binary));

            case CallExpression call:
                var block = VisitCall(call);
                if (block == null)
                    return null;

                return block.Type.Outputs.Count switch
                {
                    0 => Fail(call, $"'{call.Function}' has no output to use as a value"),
                    1 => Resolved.Of(new BlockOutput(block, block.Type.Outputs[0].Index)),
                    _ => Resolved.Of(block)
                };

            default:
                return null;
        }
    }

    /// <summary>
    /// Like <see cref="Evaluate"/>, but the expression has to stand for one output.
    /// </summary>
    private BlockOutput? EvaluateOutput(Expression expression)
    {
        var value = Evaluate(expression);
        if (value == null)
            return null;

        if (value.Value.Output != null)
            return value.Value.Output;

        var first = value.Value.Block.Type.Outputs[0].Name;
        var example = expression is NameExpression name ? $"{name.Name}.{first}" : $"{Describe(expression)}().{first}";
        Error(expression, $"'{Describe(expression)}' has several outputs; pick one, like {example}");
        return null;
    }

    private Resolved? VisitMember(MemberExpression member)
    {
        var target = Evaluate(member.Target);
        if (target == null)
            return null;

        var type = target.Value.Block.Type;
        if (target.Value.Output != null)
            return Fail(member, $"'{Describe(member.Target)}' has a single output; use it without .{member.Output}");

        var picked = type.FindOutput(member.Output);
        if (picked != null)
            return Resolved.Of(new BlockOutput(target.Value.Block, picked.Index));

        List<string> names = [];
        foreach (var output in type.Outputs)
        {
            names.Add(output.Name);
        }

        return Fail(member, $"'{type.Function}' has no output '{member.Output}'; use {string.Join(", ", names)}");
    }

    private static string Describe(Expression expression)
    {
        return expression switch
        {
            NameExpression name => name.Name,
            CallExpression call => call.Function,
            _ => "value"
        };
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

    private Resolved? Fail(SyntaxNode node, string message)
    {
        Error(node, message);
        return null;
    }

    private void Error(SyntaxNode node, string message)
    {
        _diagnostics.Add(new Diagnostic(node.Position, message));
    }

    private void ValueError(NamedArgument argument, string message)
    {
        _diagnostics.Add(new Diagnostic(argument.ValuePosition, message));
    }
}
