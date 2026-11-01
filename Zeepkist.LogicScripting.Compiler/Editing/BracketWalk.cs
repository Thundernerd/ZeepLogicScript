using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Walks from a parenthesis or bracket to the one that matches it, over the ones nested in between. It doesn't tell a
/// parenthesis from a bracket when it matches them, so a half-typed or mistyped one still ends somewhere.
/// </summary>
internal static class BracketWalk
{
    /// <summary>
    /// The tokens directly inside the parenthesis or bracket at <paramref name="open"/>, where one nested in it shows
    /// as its opening token only. The last is the token the walk ends at: the one that closes it, or before that, the
    /// first token at any depth that <paramref name="stopsAt"/> holds for, or the end of the text.
    /// </summary>
    public static IEnumerable<int> Inside(List<Token> tokens, int open, Func<int, bool> stopsAt)
    {
        var depth = 0;
        for (var i = open + 1; ; i++)
        {
            var kind = tokens[i].Kind;
            if (kind == TokenKind.End || stopsAt(i) || (IsClose(kind) && depth == 0))
            {
                yield return i;
                yield break;
            }

            if (depth == 0)
                yield return i;

            if (IsOpen(kind))
                depth++;
            else if (IsClose(kind))
                depth--;
        }
    }

    /// <summary>
    /// The parenthesis or bracket that the one at <paramref name="close"/> closes; -1 when nothing opens it.
    /// </summary>
    public static int FindOpen(List<Token> tokens, int close)
    {
        var depth = 0;
        for (var i = close - 1; i >= 0; i--)
        {
            var kind = tokens[i].Kind;
            if (IsClose(kind))
            {
                depth++;
            }
            else if (IsOpen(kind))
            {
                if (depth == 0)
                    return i;

                depth--;
            }
        }

        return -1;
    }

    public static bool IsOpen(TokenKind kind)
    {
        return kind is TokenKind.OpenParen or TokenKind.OpenBracket;
    }

    public static bool IsClose(TokenKind kind)
    {
        return kind is TokenKind.CloseParen or TokenKind.CloseBracket;
    }
}
