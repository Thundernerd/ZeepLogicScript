using System;
using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// When the completion list is open and which item is selected, apart from how it's drawn and its input read.
/// </summary>
public sealed class CompletionSession
{
    /// <summary>
    /// The most items the list shows at once; the rest scroll into view as the selection moves.
    /// </summary>
    public const int MaxRows = 8;

    private readonly Func<string, PlacedBlock> _findPlacedBlock;
    private CaretContext _context;
    private IReadOnlyList<DefinedName> _definedNames = [];
    private bool _opensWithoutPrefix;
    private bool _openRequested;

    /// <param name="findPlacedBlock">
    /// The block in the level with a UID, or null when there's none; it gives a Placed Block Reference its type.
    /// </param>
    public CompletionSession(Func<string, PlacedBlock> findPlacedBlock = null)
    {
        _findPlacedBlock = findPlacedBlock;
    }

    public bool IsOpen { get; private set; }
    public IReadOnlyList<CompletionItem> Items { get; private set; } = [];
    public int Selected { get; private set; }

    /// <summary>
    /// The first item the list shows.
    /// </summary>
    public int FirstRow { get; private set; }

    public int VisibleRows => Math.Min(MaxRows, Items.Count);

    /// <summary>
    /// Where the word being completed starts while the list is open.
    /// </summary>
    public int? WordStart => IsOpen ? _context.Word.Start : null;

    /// <summary>
    /// Opens the list at the next <see cref="Update"/>, even before a name is typed: what Ctrl+Space does.
    /// </summary>
    public void Request()
    {
        _openRequested = true;
        _opensWithoutPrefix = true;
    }

    public void Close()
    {
        IsOpen = false;
        _opensWithoutPrefix = false;
    }

    /// <summary>
    /// Call after the editor handled its input, with the text and caret it ended with. When
    /// <paramref name="whileTyping"/> is on, typing a name opens the list, and so does typing the opening parenthesis
    /// of a call that takes named arguments, which lists them. It closes when the editor loses focus, text gets
    /// selected, the caret moves to another word, or nothing matches; an empty prefix keeps it open only when
    /// <see cref="Request"/> or a call's parenthesis opened it.
    /// </summary>
    public void Update(TextSnapshot snapshot, bool typedName, bool whileTyping, bool focused, bool typedOpenParen = false)
    {
        var openedByShortcut = _openRequested;
        var opensCall = typedOpenParen && whileTyping && !openedByShortcut;
        var requested = openedByShortcut || (typedName && whileTyping) || opensCall;
        _openRequested = false;
        if (!focused || snapshot.Selection != 0)
        {
            Close();
            return;
        }

        if (!IsOpen && !requested)
            return;

        // Opening reads the names again too: the level's blocks, which give a Placed Block Reference its type, can
        // change while the list is closed
        var opening = requested && !IsOpen;
        if (opening || _context == null || _context.Text != snapshot.Text || _context.Caret != snapshot.Caret)
        {
            var wordStart = _context?.Word.Start;
            if (opening || _context == null || _context.Text != snapshot.Text)
                _definedNames = DefinedNames.Read(snapshot.Text, _findPlacedBlock);

            _context = CaretContext.At(snapshot.Text, snapshot.Caret);
            Items = Completion.GetItems(_context, _definedNames);
            Selected = 0;
            FirstRow = 0;
            if (IsOpen && wordStart != _context.Word.Start)
                Close();
        }

        if (requested)
            IsOpen = true;

        // A parenthesis that isn't a call's, or one of a call without named arguments, would list every expression
        if (opensCall && !Items.Any(i => i.Kind == CompletionKind.Argument))
            Close();
        else if (opensCall)
            _opensWithoutPrefix = true;

        if (Items.Count == 0 || (_context.Prefix.Length == 0 && !_opensWithoutPrefix))
            Close();
    }

    /// <summary>
    /// Moves the selection, wrapping around the ends or stopping at them, and scrolls it into view.
    /// </summary>
    public void Select(int index, bool wrap)
    {
        var count = Items.Count;
        if (count == 0)
            return;

        Selected = wrap ? (((index % count) + count) % count) : Math.Max(0, Math.Min(index, count - 1));
        if (Selected < FirstRow)
            FirstRow = Selected;
        else if (Selected >= FirstRow + MaxRows)
            FirstRow = Selected - MaxRows + 1;
    }

    /// <summary>
    /// Closes the list and returns the text with the item written in; null when there's no such item.
    /// </summary>
    public TextSnapshot? Accept(int index)
    {
        if (!IsOpen || index < 0 || index >= Items.Count)
            return null;

        var item = Items[index];
        Close();
        return Completion.Accept(_context, item);
    }
}
