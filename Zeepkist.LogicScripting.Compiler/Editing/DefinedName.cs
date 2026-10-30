using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// A name a Script defines, as the editor reads it.
/// </summary>
public sealed class DefinedName
{
    public DefinedName(
        string name,
        TextSpan span,
        BlockType type,
        string mode,
        bool isPlaced = false,
        DefinedFunction function = null,
        bool isInput = false)
    {
        Name = name;
        Span = span;
        Type = type;
        Mode = mode;
        IsPlaced = isPlaced;
        Function = function;
        IsInput = isInput;
    }

    public string Name { get; }

    /// <summary>
    /// Where the name is written in its definition.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// The type of block the name stands for when its value is a Block Function call or a Placed Block Reference to a
    /// block in the level; null otherwise.
    /// </summary>
    public BlockType Type { get; }

    /// <summary>
    /// The option written for the call's mode Setting (<c>mode: local_velocity</c>); null when none is written.
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// Whether the value is a Placed Block Reference, which the Script can call to wire its inputs.
    /// </summary>
    public bool IsPlaced { get; }

    /// <summary>
    /// The Function whose body defines the name, or whose input it is; null at the top level. Only that body sees it.
    /// </summary>
    public DefinedFunction Function { get; }

    /// <summary>
    /// Whether the name is one of the <see cref="Function"/>'s inputs, written on its first line.
    /// </summary>
    public bool IsInput { get; }
}
