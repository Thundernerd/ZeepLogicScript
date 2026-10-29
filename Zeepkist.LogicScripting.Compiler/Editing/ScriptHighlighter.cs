using System.Collections.Generic;
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
            spans.Add(new HighlightSpan(token.Span.Start, token.Span.Length, GetKind(token)));
        }

        AddCommentsBefore(int.MaxValue);
        return spans;
    }

    private static HighlightKind GetKind(Token token)
    {
        return token.Kind switch
        {
            TokenKind.Name when token.Text is "default" or "true" or "false" => HighlightKind.Keyword,
            TokenKind.Name when token.Text == "placed" || BlockManifest.TryGetFunction(token.Text, out _)
                => HighlightKind.Function,
            TokenKind.Name => HighlightKind.Name,
            TokenKind.Integer => HighlightKind.Number,
            TokenKind.String => HighlightKind.String,
            TokenKind.Operator or TokenKind.Equals => HighlightKind.Operator,
            _ => HighlightKind.Punctuation
        };
    }
}
