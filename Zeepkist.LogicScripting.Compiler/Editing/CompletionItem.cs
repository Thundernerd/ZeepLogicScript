namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

public sealed class CompletionItem
{
    public CompletionItem(string label, CompletionKind kind, string detail)
    {
        Label = label;
        Kind = kind;
        Detail = detail;
    }

    /// <summary>
    /// What accepting the item writes.
    /// </summary>
    public string Label { get; }

    public CompletionKind Kind { get; }

    /// <summary>
    /// A short note shown beside the label, such as a function's inputs; empty when there is none.
    /// </summary>
    public string Detail { get; }
}
