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
internal sealed class GraphBuilder
{
    private readonly List<Diagnostic> _diagnostics;
    private readonly BlockGraph _graph = new();
    private readonly Dictionary<string, Definition> _definitions = [];
    private readonly Dictionary<string, Value> _names = [];
    private readonly HashSet<string> _failedNames = [];
    private readonly Dictionary<long, BlockOutput> _literals = [];
    private readonly List<DeferredWire> _deferredWires = [];
    private Definition _current;
    private int _delayDepth;

    private GraphBuilder(List<Diagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public static BlockGraph Build(List<Statement> statements, List<Diagnostic> diagnostics)
    {
        var builder = new GraphBuilder(diagnostics);
        foreach (var statement in statements)
        {
            if (statement is Definition definition
                && !BlockManifest.TryGetFunction(definition.Name, out _)
                && !builder._definitions.ContainsKey(definition.Name))
                builder._definitions.Add(definition.Name, definition);
        }

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

    private void VisitDefinition(Definition definition)
    {
        if (BlockManifest.TryGetFunction(definition.Name, out _))
        {
            Error(definition, $"'{definition.Name}' is a Block Function and can't be used as a name");
            Evaluate(definition.Value);
            return;
        }

        if (_definitions[definition.Name] != definition)
        {
            Error(definition, $"'{definition.Name}' is already defined");
            Evaluate(definition.Value);
            return;
        }

        _current = definition;
        var value = Evaluate(definition.Value);
        _current = null;

        if (value == null)
            _failedNames.Add(definition.Name);
        else
            _names.Add(definition.Name, value.Value);
    }

    /// <summary>
    /// Returns what the expression stands for, or null after reporting why it stands for nothing.
    /// </summary>
    private Value? Evaluate(Expression expression)
    {
        switch (expression)
        {
            case IntegerLiteral literal:
                return Value.Of(GetLiteral(literal.Value));

            case NameExpression name:
                return LookUp(name);

            case MemberExpression member:
                return VisitMember(member);

            case UnaryExpression unary:
                return Value.Of(VisitUnary(unary));

            case BinaryExpression binary:
                return Value.Of(VisitBinary(binary));

            case CallExpression call:
                var block = VisitCall(call);
                if (block == null)
                    return null;

                return block.Type.Outputs.Count switch
                {
                    0 => Fail(call, $"'{call.Function}' has no output to use as a value"),
                    1 => Value.Of(new BlockOutput(block, block.Type.Outputs[0].Index)),
                    _ => Value.Of(block)
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

    /// <summary>
    /// What feeds an input. Inside <c>delay(...)</c> a name may not be defined yet; its Wire is then connected
    /// once every definition has been visited.
    /// </summary>
    private Source? EvaluateSource(Expression expression)
    {
        if (_delayDepth > 0 && IsForwardReference(expression))
            return Source.Deferred(expression);

        var output = EvaluateOutput(expression);
        return output == null ? null : Source.Of(output.Value);
    }

    private bool IsForwardReference(Expression expression)
    {
        var name = expression switch
        {
            NameExpression n => n,
            MemberExpression { Target: NameExpression n } => n,
            _ => null
        };

        return name != null
            && _definitions.ContainsKey(name.Name)
            && !_names.ContainsKey(name.Name)
            && !_failedNames.Contains(name.Name);
    }

    private Value? LookUp(NameExpression name)
    {
        if (_names.TryGetValue(name.Name, out var value))
            return value;

        // Its definition already reported why it failed
        if (_failedNames.Contains(name.Name))
            return null;

        if (_current != null && _current.Name == name.Name)
            return Fail(name, $"'{name.Name}' can't refer to itself outside delay(...)");

        if (_definitions.TryGetValue(name.Name, out var later))
            return Fail(name, $"'{name.Name}' is defined later, on line {later.Line}; only delay(...) can refer ahead");

        return Fail(name, BlockManifest.TryGetFunction(name.Name, out _)
            ? $"'{name.Name}' is a Block Function; call it with ( )"
            : $"'{name.Name}' isn't defined");
    }

    private Value? VisitMember(MemberExpression member)
    {
        var target = Evaluate(member.Target);
        if (target == null)
            return null;

        var type = target.Value.Block.Type;
        if (target.Value.Output != null)
            return Fail(member, $"'{Describe(member.Target)}' has a single output; use it without .{member.Output}");

        foreach (var output in type.Outputs)
        {
            if (output.Name == member.Output)
                return Value.Of(new BlockOutput(target.Value.Block, output.Index));
        }

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

    private BlockOutput? VisitUnary(UnaryExpression unary)
    {
        var operand = EvaluateSource(unary.Operand);
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
        var left = EvaluateSource(binary.Left);
        var right = EvaluateSource(binary.Right);
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
            _ => (BlockManifest.Math, BlockManifest.MathOperator.Choose("not_equal"))
        };
    }

    private BlockNode VisitCall(CallExpression call)
    {
        if (!BlockManifest.TryGetFunction(call.Function, out var type))
        {
            foreach (var argument in call.Arguments)
            {
                Evaluate(argument);
            }

            Error(call, $"'{call.Function}' isn't a Block Function");
            return null;
        }

        var isDelay = type == BlockManifest.Delay;
        if (isDelay)
            _delayDepth++;

        var failed = false;
        var inputs = new Source?[type.Inputs.Count];
        var wiredByPosition = new bool[type.Inputs.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var output = EvaluateSource(call.Arguments[i]);
            failed |= output == null;
            if (i < inputs.Length)
            {
                inputs[i] = output;
                wiredByPosition[i] = true;
            }
        }

        if (call.Arguments.Count > type.Inputs.Count)
        {
            Error(call, $"'{call.Function}' takes at most {type.Inputs.Count} inputs, got {call.Arguments.Count}");
            failed = true;
        }

        Dictionary<BlockSetting, Setting> settings = [];
        HashSet<string> named = [];
        foreach (var argument in call.NamedArguments)
        {
            if (!named.Add(argument.Name))
            {
                Error(argument, $"'{argument.Name}' is given twice");
                failed = true;
                continue;
            }

            var input = IndexOf(type.Inputs, argument.Name);
            if (input >= 0)
            {
                var output = EvaluateSource(argument.Value);
                failed |= output == null;
                if (wiredByPosition[input])
                {
                    Error(argument, $"Input '{argument.Name}' is already wired by position");
                    failed = true;
                }

                inputs[input] = output;
                continue;
            }

            var blockSetting = FindSetting(type, argument.Name);
            if (blockSetting == null)
            {
                Error(argument, $"'{call.Function}' has no input or Setting '{argument.Name}'");
                failed = true;
                continue;
            }

            var setting = GetSettingValue(blockSetting, argument);
            if (setting == null)
                failed = true;
            else
                settings.Add(blockSetting, setting);
        }

        if (isDelay)
            _delayDepth--;

        foreach (var blockSetting in type.Settings)
        {
            if (blockSetting.IsRequired && !settings.ContainsKey(blockSetting))
            {
                Error(call, $"'{call.Function}' needs {blockSetting.Name}:, like {blockSetting.Name}: 1");
                failed = true;
            }
        }

        if (failed)
            return null;

        List<Setting> ordered = [];
        foreach (var blockSetting in type.Settings)
        {
            if (settings.TryGetValue(blockSetting, out var setting))
                ordered.Add(setting);
        }

        var block = _graph.AddBlock(type, ordered.ToArray());
        for (var i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] != null)
                Connect(inputs[i].Value, block, type.Inputs[i].Index);
        }

        return block;
    }

    /// <summary>
    /// A Setting's value is written out in the call itself: a number, true / false, or an option word.
    /// </summary>
    private Setting GetSettingValue(BlockSetting setting, NamedArgument argument)
    {
        switch (setting.Type)
        {
            case SettingType.Number:
                if (argument.Value is IntegerLiteral literal)
                    return Setting.Text(setting.Key, literal.Value.ToString(CultureInfo.InvariantCulture));

                ValueError(argument, $"{setting.Name} takes a whole number, like {setting.Name}: 5");
                return null;

            case SettingType.Bool:
                if (argument.Value is NameExpression { Name: "true" or "false" } word)
                    return Setting.Int(setting.Key, word.Name == "true" ? 1 : 0);

                ValueError(argument, $"{setting.Name} takes true or false");
                return null;

            default:
                var options = string.Join(", ", setting.Options);
                if (argument.Value is not NameExpression option)
                {
                    ValueError(argument, $"{setting.Name} takes a word: {options}");
                    return null;
                }

                if (setting.IndexOf(option.Name) < 0)
                {
                    ValueError(argument, $"'{option.Name}' isn't an option of {setting.Name}; use {options}");
                    return null;
                }

                return setting.Choose(option.Name);
        }
    }

    private static int IndexOf(IReadOnlyList<BlockConnector> connectors, string name)
    {
        for (var i = 0; i < connectors.Count; i++)
        {
            if (connectors[i].Name == name)
                return i;
        }

        return -1;
    }

    private static BlockSetting FindSetting(BlockType type, string name)
    {
        foreach (var setting in type.Settings)
        {
            if (setting.Name == name)
                return setting;
        }

        return null;
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

    private void Connect(Source source, BlockNode to, int toConnector)
    {
        if (source.Output != null)
            _graph.AddWire(source.Output.Value, to, toConnector);
        else
            _deferredWires.Add(new DeferredWire(source.Expression, to, toConnector));
    }

    private void ConnectDeferredWires()
    {
        foreach (var wire in _deferredWires)
        {
            var output = EvaluateOutput(wire.Source);
            if (output != null)
                _graph.AddWire(output.Value, wire.To, wire.ToConnector);
        }
    }

    private Value? Fail(SyntaxNode node, string message)
    {
        Error(node, message);
        return null;
    }

    private void Error(SyntaxNode node, string message)
    {
        _diagnostics.Add(new Diagnostic(node.Line, node.Column, message));
    }

    private void ValueError(NamedArgument argument, string message)
    {
        _diagnostics.Add(new Diagnostic(argument.ValueLine, argument.ValueColumn, message));
    }

    /// <summary>
    /// What a name or expression stands for: one output, or a multi-output block to pick an output from.
    /// </summary>
    private readonly struct Value
    {
        private Value(BlockNode block, BlockOutput? output)
        {
            Block = block;
            Output = output;
        }

        public BlockNode Block { get; }

        /// <summary>
        /// Null for a multi-output block.
        /// </summary>
        public BlockOutput? Output { get; }

        public static Value? Of(BlockOutput? output)
        {
            return output == null ? null : new Value(output.Value.Block, output);
        }

        public static Value Of(BlockNode block)
        {
            return new Value(block, null);
        }
    }

    /// <summary>
    /// What feeds an input: an output, or a name inside <c>delay(...)</c> that isn't defined yet.
    /// </summary>
    private readonly struct Source
    {
        private Source(BlockOutput? output, Expression expression)
        {
            Output = output;
            Expression = expression;
        }

        public BlockOutput? Output { get; }
        public Expression Expression { get; }

        public static Source Of(BlockOutput output)
        {
            return new Source(output, null);
        }

        public static Source Deferred(Expression expression)
        {
            return new Source(null, expression);
        }
    }

    private sealed class DeferredWire
    {
        public DeferredWire(Expression source, BlockNode to, int toConnector)
        {
            Source = source;
            To = to;
            ToConnector = toConnector;
        }

        public Expression Source { get; }
        public BlockNode To { get; }
        public int ToConnector { get; }
    }
}
