using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// What the editor tells about the character the mouse rests on. Read from the tokens, like
/// <see cref="CaretContext"/>, so half-typed text has it too.
/// </summary>
public sealed class HoverInfo
{
    private HoverInfo(
        IReadOnlyList<string> errors,
        BlockReferenceEntry function,
        ReferenceRow argument,
        int definitionLine,
        string definition)
    {
        Errors = errors;
        Function = function;
        Argument = argument;
        DefinitionLine = definitionLine;
        Definition = definition;
    }

    /// <summary>
    /// The name the mouse is on, or else the error underline.
    /// </summary>
    public TextSpan Span { get; private set; }

    /// <summary>
    /// The messages of the error underlines at the character, in the errors' order.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// The Reference entry of the Block Function the name is, or of the one its alias stands for; null otherwise.
    /// </summary>
    public BlockReferenceEntry Function { get; }

    /// <summary>
    /// The input or Setting a named argument's name gives, with what it accepts as the Reference tab describes it;
    /// null when the name isn't one.
    /// </summary>
    public ReferenceRow Argument { get; }

    /// <summary>
    /// The one-based line <see cref="Definition"/> is on; 0 when there's none.
    /// </summary>
    public int DefinitionLine { get; }

    /// <summary>
    /// The trimmed first line of the definition of the name or of the Script's own Function it is; null when it's
    /// neither.
    /// </summary>
    public string Definition { get; }

    /// <summary>
    /// Whether there's something to tell about the name, apart from <see cref="Errors"/>.
    /// </summary>
    public bool HasName => Function != null || Argument != null || Definition != null;

    /// <summary>
    /// What there is to tell about the character at <paramref name="index"/>; null when there's nothing. A name tells
    /// one thing: the Block Function it is, the input or Setting it names in an argument, or else its definition. The
    /// latest definition above it is the one used, or the first below it when there's none above, as only
    /// <c>delay(...)</c> may refer to a name defined later.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="index"></param>
    /// <param name="errors">The editor's error underlines for the same text.</param>
    /// <param name="findPlacedBlock">
    /// The block in the level with a UID, or null when there's none; it gives a Placed Block Reference its type, so
    /// its inputs can be named.
    /// </param>
    public static HoverInfo At(
        string text,
        int index,
        IReadOnlyList<ErrorUnderline> errors = null,
        Func<string, PlacedBlock> findPlacedBlock = null)
    {
        text ??= string.Empty;
        var underlines = (errors ?? []).Where(e => e.Covers(index)).ToList();
        var messages = underlines.Select(e => e.Message).ToList();

        var tokens = Lexer.Tokenize(text, []);
        var name = DefinitionJump.FindName(tokens, index);
        BlockReferenceEntry function = null;
        ReferenceRow argument = null;
        var definitionLine = 0;
        string definition = null;
        if (name >= 0)
        {
            var token = tokens[name];
            if (BlockManifest.TryGetFunction(token.Text, out var type))
                function = BlockReference.GetEntry(type);
            else if (tokens[name + 1].Kind == TokenKind.Colon)
                argument = FindArgument(text, token, findPlacedBlock);
            else
                definitionLine = FindDefinition(text, tokens, token, findPlacedBlock, out definition);
        }

        var info = new HoverInfo(messages, function, argument, definitionLine, definition);
        if (!info.HasName && messages.Count == 0)
            return null;

        info.Span = info.HasName ? tokens[name].Span : underlines[0].Span;
        return info;
    }

    /// <summary>
    /// Only the innermost call around the name is asked, so a name in a call without help isn't taken for an
    /// argument of the call outside it.
    /// </summary>
    private static ReferenceRow FindArgument(string text, Token token, Func<string, PlacedBlock> findPlacedBlock)
    {
        var help = CallHelp.At(text, token.Span.Start, findPlacedBlock);
        if (help == null || help.EnclosingCalls[0] != help.OpenParen || help.Active < 0)
            return null;

        var item = help.Items[help.Active];
        return item.Name == token.Text ? new ReferenceRow(item.Name, item.Detail) : null;
    }

    private static int FindDefinition(
        string text,
        List<Token> tokens,
        Token token,
        Func<string, PlacedBlock> findPlacedBlock,
        out string definition)
    {
        definition = null;
        if (DefinitionJump.Find(text, tokens, token, findPlacedBlock) is not { } jump)
            return 0;

        var lineStarts = ScriptText.GetLineStarts(text);
        var line = lineStarts.FindLastIndex(s => s <= jump.Target.Start);
        var lineEnd = ScriptText.GetLineEnd(lineStarts, text.Length, line);
        definition = text.Substring(lineStarts[line], lineEnd - lineStarts[line]).Trim();
        return line + 1;
    }
}
