namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a Block Function's named argument gives, as <see cref="GetNamedArgumentKind"/> decides.
    /// </summary>
    private enum NamedArgumentKind
    {
        Input,
        Style,
        Setting,
        Targets,
        Unknown
    }
}
