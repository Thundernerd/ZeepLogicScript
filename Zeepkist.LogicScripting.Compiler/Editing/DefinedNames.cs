using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Graph;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Editing;

/// <summary>
/// Reads a Script's definitions from its tokens, so a half-typed line, which the parser drops, doesn't hide the ones
/// around it.
/// </summary>
public static class DefinedNames
{
    /// <summary>
    /// Every definition of a name that isn't reserved, in text order; a name defined twice is listed twice.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="findPlacedBlock">The block in the level with a UID, or null when there's none.</param>
    public static IReadOnlyList<DefinedName> Read(string text, Func<string, PlacedBlock> findPlacedBlock = null)
    {
        var tokens = Lexer.Tokenize(text ?? string.Empty, []);
        List<DefinedName> names = [];
        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            var isStatementStart = i == 0 || tokens[i - 1].Kind == TokenKind.NewLine;
            if (!isStatementStart || tokens[i].Kind != TokenKind.Name || tokens[i + 1].Kind != TokenKind.Equals)
                continue;

            var name = tokens[i].Text;
            if (GraphBuilder.IsReserved(name))
                continue;

            var type = ReadType(tokens, i + 2, findPlacedBlock, out var mode);
            var isPlaced = type != null && tokens[i + 2].Text == GraphBuilder.PlacedFunction;
            names.Add(new DefinedName(name, tokens[i].Span, type, mode, isPlaced));
        }

        return names;
    }

    /// <summary>
    /// The type of the value starting at <paramref name="start"/> when the whole value is one call.
    /// </summary>
    private static BlockType ReadType(List<Token> tokens, int start, Func<string, PlacedBlock> findPlacedBlock, out string mode)
    {
        mode = null;
        if (start + 1 >= tokens.Count || tokens[start].Kind != TokenKind.Name || tokens[start + 1].Kind != TokenKind.OpenParen)
            return null;

        var function = tokens[start].Text;
        BlockType type;
        if (function == GraphBuilder.PlacedFunction)
        {
            var uid = tokens[start + 2];
            var block = uid.Kind == TokenKind.String ? findPlacedBlock?.Invoke(uid.Text) : null;
            type = block == null ? null : BlockManifest.GetPlacedType(block.BlockId);
        }
        else if (!BlockManifest.TryGetFunction(function, out type))
        {
            return null;
        }

        var end = FindCallEnd(tokens, start + 1, type, out mode);
        if (tokens[end].Kind is not (TokenKind.NewLine or TokenKind.End))
        {
            mode = null;
            return null;
        }

        return type;
    }

    /// <summary>
    /// The token after the call's closing parenthesis, or the end of its statement when it has none, while typing.
    /// Notes the option of a named argument that sets the type's mode Setting on the way.
    /// </summary>
    private static int FindCallEnd(List<Token> tokens, int openParen, BlockType type, out string mode)
    {
        mode = null;
        var modeSetting = type?.Modes?.Setting.Name;
        var depth = 0;
        var i = openParen;
        for (; tokens[i].Kind is not (TokenKind.NewLine or TokenKind.End); i++)
        {
            switch (tokens[i].Kind)
            {
                case TokenKind.OpenParen or TokenKind.OpenBracket:
                    depth++;
                    break;

                case TokenKind.CloseParen or TokenKind.CloseBracket:
                    depth--;
                    if (depth == 0)
                        return i + 1;

                    break;

                case TokenKind.Name when depth == 1
                    && tokens[i].Text == modeSetting
                    && tokens[i - 1].Kind is TokenKind.OpenParen or TokenKind.Comma
                    && tokens[i + 1].Kind == TokenKind.Colon
                    && tokens[i + 2].Kind == TokenKind.Name:
                    mode = tokens[i + 2].Text;
                    break;
            }
        }

        return i;
    }
}
