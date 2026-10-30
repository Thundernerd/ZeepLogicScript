using System;
using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// A name the Script defines, used somewhere in its text, and where Ctrl+click on it goes. Read from the tokens, like
/// <see cref="HoverInfo"/>, so half-typed text has it too.
/// </summary>
public sealed class DefinitionJump
{
    private DefinitionJump(TextSpan name, TextSpan target)
    {
        Name = name;
        Target = target;
    }

    /// <summary>
    /// The name the character is in.
    /// </summary>
    public TextSpan Name { get; }

    /// <summary>
    /// The name in the definition it uses: before the <c>=</c>, or after the keyword of the Script's own Function.
    /// </summary>
    public TextSpan Target { get; }

    /// <summary>
    /// The jump of the name the character at <paramref name="index"/> is in; null when it isn't in a name the Script
    /// defines. The latest definition above the name is the one used, or the first below it when there's none above,
    /// as only <c>delay(...)</c> may refer to a name defined later.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="index"></param>
    /// <param name="findPlacedBlock">The block in the level with a UID, or null when there's none.</param>
    public static DefinitionJump At(string text, int index, Func<string, PlacedBlock> findPlacedBlock = null)
    {
        text ??= string.Empty;
        var tokens = Lexer.Tokenize(text, []);
        var name = FindName(tokens, index);
        if (name < 0 || BlockManifest.TryGetFunction(tokens[name].Text, out _) || tokens[name + 1].Kind == TokenKind.Colon)
            return null;

        return Find(text, tokens, tokens[name], findPlacedBlock);
    }

    /// <summary>
    /// The token of the name the character is in, unless it comes after a dot or a colon: then it's an output or a
    /// Library's Function, or a Setting's option word, which can be spelled like an alias (<c>op: lerp</c>). -1 when
    /// there's none.
    /// </summary>
    internal static int FindName(List<Token> tokens, int index)
    {
        var name = tokens.FindIndex(t => t.Kind == TokenKind.Name && t.Span.Start <= index && index < t.Span.End);
        return name > 0 && tokens[name - 1].Kind is TokenKind.Dot or TokenKind.Colon ? -1 : name;
    }

    /// <summary>
    /// The jump of <paramref name="token"/>, a name that isn't a Block Function or a named argument's name.
    /// </summary>
    internal static DefinitionJump Find(string text, List<Token> tokens, Token token, Func<string, PlacedBlock> findPlacedBlock)
    {
        var targets = DefinedNames.Read(text, findPlacedBlock)
            .Where(n => n.Name == token.Text)
            .Select(n => n.Span)
            .Concat(FindFunctionDefinitions(tokens, token.Text))
            .OrderBy(s => s.Start)
            .ToList();

        if (targets.Count == 0)
            return null;

        var above = targets.Where(s => s.Start <= token.Span.Start).ToList();
        return new DefinitionJump(token.Span, above.Count > 0 ? above[above.Count - 1] : targets[0]);
    }

    /// <summary>
    /// Where the Script's own Functions with the name are defined: the name after the keyword.
    /// </summary>
    private static IEnumerable<TextSpan> FindFunctionDefinitions(List<Token> tokens, string name)
    {
        if (GraphBuilder.IsReserved(name))
            yield break;

        for (var i = 1; i < tokens.Count; i++)
        {
            var isAfterKeyword = tokens[i - 1].Kind == TokenKind.Name && tokens[i - 1].Text == FunctionDefinition.Keyword;
            if (isAfterKeyword && tokens[i].Kind == TokenKind.Name && tokens[i].Text == name)
                yield return tokens[i].Span;
        }
    }
}
