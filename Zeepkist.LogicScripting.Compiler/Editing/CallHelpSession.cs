using System;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Which call's help the editor shows, apart from how it's drawn and its input read.
/// </summary>
public sealed class CallHelpSession
{
    private readonly Func<string, PlacedBlock> _findPlacedBlock;
    private TextSnapshot? _snapshot;
    private int? _dismissedCall;

    /// <param name="findPlacedBlock">
    /// The block in the level with a UID, or null when there's none; it gives a Placed Block Reference its type.
    /// </param>
    public CallHelpSession(Func<string, PlacedBlock> findPlacedBlock = null)
    {
        _findPlacedBlock = findPlacedBlock;
    }

    /// <summary>
    /// The help to show; null while there's none.
    /// </summary>
    public CallHelp Help { get; private set; }

    /// <summary>
    /// Hides the help until the caret leaves its call: what Escape does.
    /// </summary>
    public void Dismiss()
    {
        _dismissedCall = Help?.OpenParen;
        Help = null;
    }

    /// <summary>
    /// Call after the editor handled its input, with the text and caret it ended with. Also call before handling an
    /// Escape, with the text and caret at that key. Shows nothing while the editor isn't focused.
    /// </summary>
    public void Update(TextSnapshot snapshot, bool focused)
    {
        if (!focused)
        {
            Help = null;
            _snapshot = null;
            _dismissedCall = null;
            return;
        }

        if (_snapshot is { } previous && previous.Text == snapshot.Text && previous.Caret == snapshot.Caret)
            return;

        _snapshot = snapshot;
        var help = CallHelp.At(snapshot.Text, snapshot.Caret, _findPlacedBlock);

        if (_dismissedCall is { } dismissed && (help == null || !help.EnclosingCalls.Contains(dismissed)))
            _dismissedCall = null;

        Help = _dismissedCall == null ? help : null;
    }
}
