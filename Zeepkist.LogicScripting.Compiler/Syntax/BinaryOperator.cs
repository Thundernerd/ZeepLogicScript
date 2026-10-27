using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

/// <summary>
/// A binary operator: its symbol, its precedence, and the Block Function call it's shorthand for. Adding an operator
/// to <see cref="All"/> is all the lexer, parser and graph builder need.
/// </summary>
internal sealed class BinaryOperator
{
    // Precedence follows C's binary levels; higher binds tighter
    public static readonly BinaryOperator Or = new("|", 0, BlockManifest.Logic, BlockManifest.LogicOperator, "or");
    public static readonly BinaryOperator Xor = new("^", 1, BlockManifest.Logic, BlockManifest.LogicOperator, "xor");
    public static readonly BinaryOperator And = new("&", 2, BlockManifest.Logic, BlockManifest.LogicOperator, "and");
    public static readonly BinaryOperator Equal = new("==", 3, BlockManifest.Math, BlockManifest.MathOperator, "equal");
    public static readonly BinaryOperator NotEqual = new("!=", 3, BlockManifest.Math, BlockManifest.MathOperator, "not_equal");
    public static readonly BinaryOperator Less = new("<", 4, BlockManifest.Math, BlockManifest.MathOperator, "less");
    public static readonly BinaryOperator Greater = new(">", 4, BlockManifest.Math, BlockManifest.MathOperator, "greater");
    public static readonly BinaryOperator LessEqual = new("<=", 4, BlockManifest.Math, BlockManifest.MathOperator, "less_equal");
    public static readonly BinaryOperator GreaterEqual = new(">=", 4, BlockManifest.Math, BlockManifest.MathOperator, "greater_equal");
    public static readonly BinaryOperator Add = new("+", 5, BlockManifest.Math, BlockManifest.MathOperator, "add");
    public static readonly BinaryOperator Subtract = new("-", 5, BlockManifest.Math, BlockManifest.MathOperator, "subtract");
    public static readonly BinaryOperator Multiply = new("*", 6, BlockManifest.Math, BlockManifest.MathOperator, "multiply");
    public static readonly BinaryOperator Divide = new("/", 6, BlockManifest.Math, BlockManifest.MathOperator, "divide");
    public static readonly BinaryOperator Modulo = new("%", 6, BlockManifest.Math, BlockManifest.MathOperator, "modulo");

    public static readonly IReadOnlyList<BinaryOperator> All =
    [
        Or, Xor, And,
        Equal, NotEqual,
        Less, Greater, LessEqual, GreaterEqual,
        Add, Subtract,
        Multiply, Divide, Modulo
    ];

    private BinaryOperator(string symbol, int precedence, BlockType block, BlockSetting setting, string option)
    {
        Symbol = symbol;
        Precedence = precedence;
        Block = new OperatorBlock(block, setting.Choose(option));
    }

    public string Symbol { get; }
    public int Precedence { get; }

    /// <summary>
    /// The block of the Block Function the operator calls; its first two inputs take the operands.
    /// </summary>
    public OperatorBlock Block { get; }
}
