using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// A unary operator: its symbol and the block it becomes. Adding an operator to <see cref="All"/> is all the lexer,
/// parser and graph builder need.
/// </summary>
internal sealed class UnaryOperator
{
    public static readonly UnaryOperator Not = new("!", BlockManifest.Digitizer, Setting.Int(BlockManifest.DigitizerInvert.Key, 1));

    // -x is 0 - x
    public static readonly UnaryOperator Negate = new("-", BinaryOperator.Subtract.Block, BinaryOperator.Subtract.Setting, 0);

    public static readonly IReadOnlyList<UnaryOperator> All = [Not, Negate];

    private UnaryOperator(string symbol, BlockType block, Setting setting, params long[] leadingLiterals)
    {
        Symbol = symbol;
        Block = block;
        Setting = setting;
        LeadingLiterals = leadingLiterals;
    }

    public string Symbol { get; }

    /// <summary>
    /// The block type the operator places; its first inputs take <see cref="LeadingLiterals"/>, and the input after
    /// them takes the operand.
    /// </summary>
    public BlockType Block { get; }

    /// <summary>
    /// The Setting that makes <see cref="Block"/> do the operation.
    /// </summary>
    public Setting Setting { get; }

    /// <summary>
    /// The literals fed into <see cref="Block"/>'s first inputs, in order, before the operand.
    /// </summary>
    public IReadOnlyList<long> LeadingLiterals { get; }

    /// <summary>
    /// The operator whose symbol starts at <paramref name="index"/>, preferring the longest.
    /// </summary>
    public static UnaryOperator Match(string text, int index)
    {
        UnaryOperator match = null;
        foreach (var op in All)
        {
            if (index + op.Symbol.Length <= text.Length
                && string.CompareOrdinal(text, index, op.Symbol, 0, op.Symbol.Length) == 0
                && (match == null || op.Symbol.Length > match.Symbol.Length))
            {
                match = op;
            }
        }

        return match;
    }
}
