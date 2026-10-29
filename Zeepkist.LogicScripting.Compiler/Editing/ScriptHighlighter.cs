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
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text ?? string.Empty, [], comments);
        List<HighlightSpan> spans = [];
        var functions = GetFunctionNames(tokens);
        var nextComment = 0;

        void AddCommentsBefore(int index)
        {
            for (; nextComment < comments.Count && comments[nextComment].Start < index; nextComment++)
            {
                var comment = comments[nextComment];
                spans.Add(new HighlightSpan(comment.Start, comment.Length, HighlightKind.Comment));
            }
        }

        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.NewLine or TokenKind.End)
                continue;

            AddCommentsBefore(token.Span.Start);
            spans.Add(new HighlightSpan(token.Span.Start, token.Span.Length, GetKind(token, functions)));
        }

        AddCommentsBefore(int.MaxValue);
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
