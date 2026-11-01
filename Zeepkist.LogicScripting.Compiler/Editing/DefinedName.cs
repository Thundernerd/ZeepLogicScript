using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// A name a Script defines, as the editor reads it.
/// </summary>
public sealed class DefinedName
{
    public DefinedName(
        string name,
        TextSpan span,
        DefinedValue value,
        DefinedFunction function = null,
        bool isInput = false)
    {
        Name = name;
        Span = span;
        Value = value;
        Function = function;
        IsInput = isInput;
    }

    public string Name { get; }

    /// <summary>
    /// Where the name is written in its definition.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// Null when the value is no call the editor knows, and for an input.
    /// </summary>
    public DefinedValue Value { get; }

    /// <summary>
    /// The Function whose body defines the name, or whose input it is; null at the top level. Only that body sees it.
    /// </summary>
    public DefinedFunction Function { get; }

    /// <summary>
    /// Whether the name is one of the <see cref="Function"/>'s inputs, written on its first line.
    /// </summary>
    public bool IsInput { get; }

    /// <summary>
    /// Whether the text at <paramref name="index"/> sees the name: it's in the same Function's body, or both are at the
    /// top level, outside every one of the <paramref name="functions"/>.
    /// </summary>
    internal bool IsInScopeAt(int index, IEnumerable<DefinedFunction> functions)
    {
        return Function == null ? !functions.Any(f => f.Contains(index)) : Function.Contains(index);
    }
}
