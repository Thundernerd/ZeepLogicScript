using System.Collections.Generic;
using System.Linq;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Colors a Script with the compiler's own lexer, so the editor and Compile read the text the same way.
/// </summary>
public static class ScriptHighlighter
{
    /// <summary>
    /// One span per token and comment, in text order. Whitespace and characters the lexer rejects get none.
    /// </summary>
    public static IReadOnlyList<HighlightSpan> Highlight(string text)
    {
        text ??= string.Empty;
        var lineStarts = ScriptText.GetLineStarts(text);
        List<HighlightSpan> spans = [];
        var end = 0;
        var tokens = Lexer.Tokenize(text, []);
        var functions = GetFunctionNames(tokens);
        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.NewLine or TokenKind.End)
                continue;

            var start = ScriptText.GetIndex(lineStarts, text.Length, token.Position);
            AddComments(text, end, start, spans);
            var length = GetLength(text, token, start);
            spans.Add(new HighlightSpan(start, length, GetKind(token, functions)));
            end = start + length;
        }

        AddComments(text, end, text.Length, spans);
        return spans;
    }

    /// <summary>
    /// The names the Script's own Functions are defined with, so their calls get the same color as a Block Function's.
    /// </summary>
    private static HashSet<string> GetFunctionNames(List<Token> tokens)
    {
        return new HashSet<string>(tokens
            .Where((t, i) => t.Kind == TokenKind.Name && i > 0 && tokens[i - 1].Kind == TokenKind.Name && tokens[i - 1].Text == FunctionDefinition.Keyword)
            .Select(t => t.Text));
    }

    /// <summary>
    /// The comments in the text between two tokens, each from its <c>#</c> to the end of its line.
    /// </summary>
    private static void AddComments(string text, int from, int to, List<HighlightSpan> spans)
    {
        var start = text.IndexOf('#', from, to - from);
        while (start >= 0)
        {
            var lineEnd = text.IndexOf('\n', start, to - start);
            var end = lineEnd < 0 ? to : lineEnd;
            spans.Add(new HighlightSpan(start, end - start, HighlightKind.Comment));
            start = end < to ? text.IndexOf('#', end, to - end) : -1;
        }
    }

    /// <summary>
    /// A string token's text leaves out its quotes; the span keeps them.
    /// </summary>
    private static int GetLength(string text, Token token, int start)
    {
        if (token.Kind != TokenKind.String)
            return token.Text.Length;

        var closing = start + 1 + token.Text.Length;
        return closing < text.Length && text[closing] == '"' ? token.Text.Length + 2 : token.Text.Length + 1;
    }

    private static HighlightKind GetKind(Token token, HashSet<string> functions)
    {
        return token.Kind switch
        {
            TokenKind.Name when token.Text is "default" or "true" or "false" or FunctionDefinition.Keyword or FunctionDefinition.EndKeyword
                => HighlightKind.Keyword,
            TokenKind.Name when token.Text == "placed" || BlockManifest.TryGetFunction(token.Text, out _) || functions.Contains(token.Text)
                => HighlightKind.Function,
            TokenKind.Name => HighlightKind.Name,
            TokenKind.Integer => HighlightKind.Number,
            TokenKind.String => HighlightKind.String,
            TokenKind.Operator or TokenKind.Equals or TokenKind.Arrow => HighlightKind.Operator,
            _ => HighlightKind.Punctuation
        };
    }
}
