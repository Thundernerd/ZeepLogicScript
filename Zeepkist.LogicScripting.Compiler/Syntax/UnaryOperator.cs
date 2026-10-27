using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// A unary operator: its symbol and the Block Function call it's shorthand for. Adding an operator to
/// <see cref="All"/> is all the lexer, parser and graph builder need.
/// </summary>
internal sealed class UnaryOperator
{
    public static readonly UnaryOperator Not = new(
        "!",
        signsInteger: false,
        BlockManifest.Digitizer,
        Setting.Int(BlockManifest.DigitizerInvert.Key, 1));

    // -x is 0 - x, but -5 is the literal itself
    public static readonly UnaryOperator Negate = new(
        "-",
        signsInteger: true,
        BinaryOperator.Subtract.Block,
        BinaryOperator.Subtract.Setting,
        0);

    public static readonly IReadOnlyList<UnaryOperator> All = [Not, Negate];

    private UnaryOperator(string symbol, bool signsInteger, BlockType block, Setting setting, params long[] leadingLiterals)
    {
        Symbol = symbol;
        SignsInteger = signsInteger;
        Block = block;
        Setting = setting;
        LeadingLiterals = leadingLiterals;
    }

    public string Symbol { get; }

    /// <summary>
    /// Whether the symbol right before a number is part of the literal, so <c>-5</c> is one Generator, not 0 - 5.
    /// </summary>
    public bool SignsInteger { get; }

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
}
