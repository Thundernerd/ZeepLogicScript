namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// One input or Setting of the call <see cref="CallHelp"/> describes.
/// </summary>
public sealed class CallHelpItem
{
    public CallHelpItem(string name, bool isInput, string detail)
    {
        Name = name;
        IsInput = isInput;
        Detail = detail;
    }

    public string Name { get; }

    /// <summary>
    /// Whether it is an input, which a positional argument can give; a Setting is only given by name.
    /// </summary>
    public bool IsInput { get; }

    /// <summary>
    /// What it accepts, as the Reference tab describes it.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// A Setting with its colon.
    /// </summary>
    public string Label => IsInput ? Name : $"{Name}:";
}
