namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// Wording that several errors and window texts share, so that rewording a rule changes every copy.
/// </summary>
public static class MessageFragments
{
    /// <summary>
    /// Follows a clause about a name defined later, which "it" refers back to.
    /// </summary>
    public const string OnlyDelayRefersAhead = "only delay(...) can refer to it";

    /// <summary>
    /// Why a Script can't refer to a block a Compile placed.
    /// </summary>
    public const string InBlockGroup = "in a Block Group, which a recompile replaces";
}
