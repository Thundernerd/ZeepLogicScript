using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

/// <summary>
/// Checks the parsed Script and turns it into a block graph: one block per Block Function call or operator,
/// one shared Generator per distinct literal value. Names are defined before use, once each; only inside
/// <c>delay(...)</c> can a name refer to a later definition or to itself, so every cycle within the Script passes
/// through a Delay (ADR 0002). Each call to a Function expands its body. Placed Block References reach blocks already
/// in the level; Wires into them are checked for loops.
/// </summary>
internal sealed partial class GraphBuilder
{
    private readonly List<Diagnostic> _diagnostics;
    private readonly IReadOnlyCollection<int> _disabledBlockIds;
    private readonly Func<string, PlacedBlock> _findPlacedBlock;
    private readonly BlockGraph _graph = new();
    private readonly Dictionary<long, BlockOutput> _literals = [];

    /// <summary>
    /// Every error so far, so a Function's body reports each of its errors once however often it's expanded.
    /// </summary>
    private readonly HashSet<(int Line, int Column, string Message)> _reported = [];

    /// <summary>
    /// How deep <see cref="Evaluate"/> recurses. The parser caps each expression, but a Function's body is visited
    /// inside the expression that calls it, so the depths add up.
    /// </summary>
    private int _depth;

    /// <summary>
    /// The line of the top-level statement being visited, where a Function's body is expanded from.
    /// </summary>
    private int _statementLine;

    private GraphBuilder(
        List<Diagnostic> diagnostics,
        IReadOnlyCollection<int> disabledBlockIds,
        Func<string, PlacedBlock> findPlacedBlock)
    {
        _diagnostics = diagnostics;
        _disabledBlockIds = disabledBlockIds;
        _findPlacedBlock = findPlacedBlock;
        _scope = _topScope;
    }

    /// <param name="statements">The parsed Script.</param>
    /// <param name="diagnostics">Where to add the errors found.</param>
    /// <param name="disabledBlockIds">Blocks that don't match the game; placing one is an error.</param>
    /// <param name="findPlacedBlock">The block in the level with a UID, or null when there's none.</param>
    public static BlockGraph Build(
        List<Statement> statements,
        List<Diagnostic> diagnostics,
        IReadOnlyCollection<int> disabledBlockIds,
        Func<string, PlacedBlock> findPlacedBlock)
    {
        var builder = new GraphBuilder(diagnostics, disabledBlockIds, findPlacedBlock);
        builder.CollectFunctions(statements);
        builder.CollectDefinitions(builder._topScope, statements);

        foreach (var statement in statements)
        {
            builder._statementLine = statement.Position.Line;
            builder.Visit(statement);
        }

        builder.CheckUncalledFunctions();
        builder.ConnectDeferredWires(0);
        builder.CheckDependencyLoops();
        builder.ApplyDefaults();
        return builder._graph;
    }

    private void Visit(Statement statement)
    {
        switch (statement)
        {
            case Definition definition:
                VisitDefinition(definition);
                break;
            case DefaultStatement defaultStatement when _scope.Function != null:
                Error(defaultStatement, "A default line goes outside functions");
                break;
            case DefaultStatement defaultStatement:
                VisitDefault(defaultStatement);
                break;
            case ExpressionStatement { Expression: CallExpression call } when IsFunctionCall(call):
                ExpandCall(call);
                break;
            case ExpressionStatement { Expression: CallExpression call } when call.Function != PlacedFunction:
                VisitCall(call);
                break;
            case ExpressionStatement expression:
                Evaluate(expression.Expression);
                Error(expression, "A value on its own line does nothing; give it a name or pass it to a call");
                break;
        }
    }

    /// <summary>
    /// Null after reporting why the expression stands for nothing.
    /// </summary>
    private Resolved? Evaluate(Expression expression)
    {
        if (_depth >= Parser.MaxDepth)
            return Fail(expression, $"This nests more than {Parser.MaxDepth} deep, counting the functions it's called from; split it into definitions");

        _depth++;
        try
        {
            return EvaluateUnchecked(expression);
        }
        finally
        {
            _depth--;
        }
    }

    private Resolved? EvaluateUnchecked(Expression expression)
    {
        switch (expression)
        {
            case IntegerLiteral literal:
                return Resolved.Of(GetLiteral(literal, literal.Value));

            case StringLiteral text:
                return Fail(text, $"Text in quotes is only for the UID in {PlacedFunction}(...)");

            case ListExpression list:
                foreach (var item in list.Items)
                {
                    Evaluate(item);
                }

                return Fail(list, "A list in [ ] is only for a Toggler's targets:");

            case NameExpression name:
                return LookUp(name);

            case MemberExpression member:
                return VisitMember(member);

            case UnaryExpression unary:
                return Resolved.Of(VisitUnary(unary));

            case BinaryExpression binary:
                return Resolved.Of(VisitBinary(binary));

            case CallExpression { Function: PlacedFunction } call:
                return VisitPlaced(call);

            case CallExpression call when IsReferenceCall(call):
                VisitReferenceCall(call);
                return Fail(call, $"Wiring into '{call.Function}' gives no value; put it on its own line");

            case CallExpression call when IsFunctionCall(call):
                return VisitFunctionCall(call);

            case CallExpression call:
                var block = VisitCall(call);
                if (block == null)
                    return null;

                return block.Outputs.Count switch
                {
                    0 => Fail(call, $"'{call.Function}' has no output to use as a value"),
                    1 => Resolved.Of(new BlockOutput(block, block.Outputs[0].Index)),
                    _ => Resolved.Of(block)
                };

            default:
                return null;
        }
    }

    /// <summary>
    /// The one output an expression's value stands for, or null after reporting why there isn't one.
    /// </summary>
    private BlockOutput? ToOutput(Expression expression, Resolved? value)
    {
        if (value == null)
            return null;

        if (value.Value.Output != null)
            return value.Value.Output;

        if (value.Value.Block?.Outputs.Count == 0)
        {
            Error(expression, $"'{Describe(expression)}' has no output to use as a value");
            return null;
        }

        var first = value.Value.FunctionOutputs?.Function.Outputs[0].Name ?? value.Value.Block.Outputs[0].Name;
        var example = expression is NameExpression name ? $"{name.Name}.{first}" : $"{Describe(expression)}().{first}";
        Error(expression, $"'{Describe(expression)}' has several outputs; pick one, like {example}");
        return null;
    }

    private Resolved? VisitMember(MemberExpression member)
    {
        var target = Evaluate(member.Target);
        if (target == null)
            return null;

        if (target.Value.Deferred is { } deferred)
        {
            var deferredMember = new MemberExpression(member.Position, deferred.Expression, member.Output);
            return Resolved.Of(new DeferredValue(deferredMember, deferred.Scope));
        }

        if (target.Value.FunctionOutputs is { } outputs)
        {
            if (outputs.Values.TryGetValue(member.Output, out var value))
                return value;

            var function = outputs.Function;
            return Fail(member, $"'{function.Name.Name}' has no output '{member.Output}'; use {string.Join(", ", function.Outputs.Select(o => o.Name))}");
        }

        var block = target.Value.Block;
        if (target.Value.Output != null)
            return Fail(member, $"'{Describe(member.Target)}' has a single output; use it without .{member.Output}");

        if (block.Outputs.Count == 0)
            return Fail(member, $"'{Describe(member.Target)}' has no outputs");

        List<string> names = [];
        foreach (var output in block.Outputs)
        {
            if (output.Name == member.Output)
                return Resolved.Of(new BlockOutput(block, output.Index));

            names.Add(output.Name);
        }

        var type = block.Type;
        var mode = type.Modes == null
            ? string.Empty
            : $" in {type.Modes.Setting.Name} {type.GetModeName(block.Settings)}";
        return Fail(member, $"'{type.Function}' has no output '{member.Output}'{mode}; use {string.Join(", ", names)}");
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

    /// <summary>
    /// One Generator per value, placed by the first <paramref name="origin"/> that writes it.
    /// </summary>
    private BlockOutput GetLiteral(SyntaxNode origin, long value)
    {
        if (_literals.TryGetValue(value, out var output))
            return output;

        var type = BlockManifest.Generator;
        var power = Setting.Text(BlockManifest.GeneratorPowerKey, value.ToString(CultureInfo.InvariantCulture));
        var block = AddBlock(origin, type, type.DefaultStyle, power);
        output = new BlockOutput(block, block.Outputs[0].Index);
        _literals.Add(value, output);
        return output;
    }

    /// <summary>
    /// Reports at <paramref name="origin"/> when the startup check turned the block off.
    /// </summary>
    private BlockNode AddBlock(SyntaxNode origin, BlockType type, BlockStyle style, params Setting[] settings)
    {
        if (_disabledBlockIds.Contains(style.BlockId))
        {
            Error(origin, $"'{type.Describe(style)}' doesn't match this game version and is turned off");
        }

        return _graph.AddBlock(type, style, settings);
    }

    private Resolved? Fail(SyntaxNode node, string message)
    {
        Error(node, message);
        return null;
    }

    private void Error(SyntaxNode node, string message)
    {
        Report(node.Position, message);
    }

    private void ValueError(NamedArgument argument, string message)
    {
        Report(argument.ValuePosition, message);
    }

    private void Report(Position position, string message)
    {
        if (_reported.Add((position.Line, position.Column, message)))
            _diagnostics.Add(new Diagnostic(position, message));
    }
}
