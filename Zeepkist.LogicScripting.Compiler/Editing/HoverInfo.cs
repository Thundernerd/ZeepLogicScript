using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
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
        TextSpan span,
        IReadOnlyList<string> errors,
        BlockReferenceEntry function,
        BlockReferenceRow argument,
        int definitionLine,
        string definition)
    {
        Span = span;
        Errors = errors;
        Function = function;
        Argument = argument;
        DefinitionLine = definitionLine;
        Definition = definition;
    }

    /// <summary>
    /// The name the mouse is on, or else the error underline.
    /// </summary>
    public TextSpan Span { get; }

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
    public BlockReferenceRow Argument { get; }

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
        var name = tokens.FindIndex(t => t.Kind == TokenKind.Name && t.Span.Start <= index && index < t.Span.End);
        BlockReferenceEntry function = null;
        BlockReferenceRow argument = null;
        var definitionLine = 0;
        string definition = null;
        // A name after a dot is an output or a Library's Function, and one after a colon a Setting's option word,
        // which can be spelled like an alias (op: lerp)
        if (name >= 0 && (name == 0 || tokens[name - 1].Kind is not (TokenKind.Dot or TokenKind.Colon)))
        {
            var token = tokens[name];
            if (BlockManifest.TryGetFunction(token.Text, out var type))
                function = BlockReference.GetEntry(type);
            else if (tokens[name + 1].Kind == TokenKind.Colon)
                argument = FindArgument(text, token, findPlacedBlock);
            else
                definitionLine = FindDefinition(text, tokens, token, findPlacedBlock, out definition);
        }

        var hasName = function != null || argument != null || definition != null;
        if (!hasName && messages.Count == 0)
            return null;

        var span = hasName ? tokens[name].Span : underlines[0].Span;
        return new HoverInfo(span, messages, function, argument, definitionLine, definition);
    }

    /// <summary>
    /// Only the innermost call around the name is asked, so a name in a call without help isn't taken for an
    /// argument of the call outside it.
    /// </summary>
    private static BlockReferenceRow FindArgument(string text, Token token, Func<string, PlacedBlock> findPlacedBlock)
    {
        var help = CallHelp.At(text, token.Span.Start, findPlacedBlock);
        if (help == null || help.EnclosingCalls[0] != help.OpenParen || help.Active < 0)
            return null;

        var item = help.Items[help.Active];
        return item.Name == token.Text ? new BlockReferenceRow(item.Name, item.Detail) : null;
    }

    private static int FindDefinition(
        string text,
        List<Token> tokens,
        Token token,
        Func<string, PlacedBlock> findPlacedBlock,
        out string definition)
    {
        var starts = DefinedNames.Read(text, findPlacedBlock)
            .Where(n => n.Name == token.Text)
            .Select(n => n.Span.Start)
            .Concat(FindFunctionDefinitions(tokens, token.Text))
            .OrderBy(s => s)
            .ToList();

        definition = null;
        if (starts.Count == 0)
            return 0;

        var above = starts.Where(s => s <= token.Span.Start).ToList();
        var start = above.Count > 0 ? above[above.Count - 1] : starts[0];
        var lineStarts = ScriptText.GetLineStarts(text);
        var line = lineStarts.FindLastIndex(s => s <= start);
        var lineEnd = ScriptText.GetLineEnd(lineStarts, text.Length, line);
        definition = text.Substring(lineStarts[line], lineEnd - lineStarts[line]).Trim();
        return line + 1;
    }

    /// <summary>
    /// Where the Script's own Functions with the name are defined: the name after the keyword.
    /// </summary>
    private static IEnumerable<int> FindFunctionDefinitions(List<Token> tokens, string name)
    {
        for (var i = 1; i < tokens.Count; i++)
        {
            var isAfterKeyword = tokens[i - 1].Kind == TokenKind.Name && tokens[i - 1].Text == FunctionDefinition.Keyword;
            if (isAfterKeyword && tokens[i].Kind == TokenKind.Name && tokens[i].Text == name && !GraphBuilder.IsReserved(name))
                yield return tokens[i].Span.Start;
        }
    }
}
