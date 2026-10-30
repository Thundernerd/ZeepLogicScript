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
    /// <param name="text">The Script or Library.</param>
    /// <param name="libraries">
    /// The Function names of each Library on this machine, by the Library's name; a qualified call to one of them,
    /// <c>timing.edge</c>, gets a Block Function's color.
    /// </param>
    public static IReadOnlyList<HighlightSpan> Highlight(
        string text,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> libraries = null)
    {
        List<TextSpan> comments = [];
        var tokens = Lexer.Tokenize(text ?? string.Empty, [], comments);
        List<HighlightSpan> spans = [];
        var functions = GetFunctionNames(tokens);
        var libraryCalls = GetLibraryCallTokens(tokens, libraries);
        var nextComment = 0;

        void AddCommentsBefore(int index)
        {
            for (; nextComment < comments.Count && comments[nextComment].Start < index; nextComment++)
            {
                var comment = comments[nextComment];
                spans.Add(new HighlightSpan(comment.Start, comment.Length, HighlightKind.Comment));
            }
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind is TokenKind.NewLine or TokenKind.End)
                continue;

            AddCommentsBefore(token.Span.Start);
            var kind = libraryCalls.Contains(i) ? HighlightKind.Function : GetKind(token, functions);
            spans.Add(new HighlightSpan(token.Span.Start, token.Span.Length, kind));
        }

        AddCommentsBefore(int.MaxValue);
        return spans;
    }

    /// <summary>
    /// The names a Script or Library defines its own Functions with, such as a Library's, to pass to
    /// <see cref="Highlight"/>.
    /// </summary>
    public static IReadOnlyCollection<string> GetFunctionNames(string text)
    {
        return GetFunctionNames(Lexer.Tokenize(text ?? string.Empty, []));
    }

    /// <summary>
    /// The names the Script's own Functions are defined with, so their calls get the same color as a Block Function's.
    /// </summary>
    private static HashSet<string> GetFunctionNames(List<Token> tokens)
    {
        return new HashSet<string>(DefinedNames.ReadFunctions(tokens).Select(f => f.Function.Name).Where(n => n != null));
    }

    /// <summary>
    /// The indexes of both names in each qualified call, <c>library.function(</c>, to a Function a Library on this
    /// machine has. A name after another dot is an output, like the compiler reads it.
    /// </summary>
    private static HashSet<int> GetLibraryCallTokens(
        List<Token> tokens,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> libraries)
    {
        HashSet<int> indexes = [];
        if (libraries == null)
            return indexes;

        for (var i = 0; i + 3 < tokens.Count; i++)
        {
            if (tokens[i].Kind == TokenKind.Name
                && (i == 0 || tokens[i - 1].Kind != TokenKind.Dot)
                && tokens[i + 1].Kind == TokenKind.Dot
                && tokens[i + 2].Kind == TokenKind.Name
                && tokens[i + 3].Kind == TokenKind.OpenParen
                && libraries.TryGetValue(tokens[i].Text, out var functions)
                && functions.Contains(tokens[i + 2].Text))
            {
                indexes.Add(i);
                indexes.Add(i + 2);
            }
        }

        return indexes;
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
