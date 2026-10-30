using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Every definition of a name that isn't reserved, and every input of a Function, in text order; a name defined
    /// twice is listed twice.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="findPlacedBlock">The block in the level with a UID, or null when there's none.</param>
    public static IReadOnlyList<DefinedName> Read(string text, Func<string, PlacedBlock> findPlacedBlock = null)
    {
        var tokens = Lexer.Tokenize(text ?? string.Empty, []);
        var functions = ReadFunctions(tokens);
        List<DefinedName> names = [];
        foreach (var (function, inputs) in functions)
        {
            names.AddRange(inputs
                .Where(i => !GraphBuilder.IsReserved(i.Text))
                .Select(i => new DefinedName(i.Text, i.Span, null, null, function: function, isInput: true)));
        }

        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            var isStatementStart = i == 0 || tokens[i - 1].Kind == TokenKind.NewLine;
            if (!isStatementStart || tokens[i].Kind != TokenKind.Name || tokens[i + 1].Kind != TokenKind.Equals)
                continue;

            var name = tokens[i].Text;
            if (GraphBuilder.IsReserved(name))
                continue;

            var type = ReadType(tokens, i + 2, findPlacedBlock, out var mode, out var isPlaced);
            var function = functions.Select(f => f.Function).FirstOrDefault(f => f.Contains(tokens[i].Span.Start));
            names.Add(new DefinedName(name, tokens[i].Span, type, mode, isPlaced, function));
        }

        return names.OrderBy(n => n.Span.Start).ToList();
    }

    /// <summary>
    /// Every Function with a name, in text order; a Function defined twice is listed twice.
    /// </summary>
    internal static IReadOnlyList<DefinedFunction> ReadFunctions(string text)
    {
        return ReadFunctions(Lexer.Tokenize(text ?? string.Empty, [])).Select(f => f.Function).Where(f => f.Name != null).ToList();
    }

    /// <summary>
    /// Reads them the way the parser does: a body ends at its first <c>end</c> line, or before the next Function's
    /// first line, since Functions can't be defined inside each other. A Function without a name still has a body,
    /// which is a scope of its own.
    /// </summary>
    internal static List<(DefinedFunction Function, List<Token> Inputs)> ReadFunctions(List<Token> tokens)
    {
        List<(DefinedFunction, List<Token>)> functions = [];
        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            if (!IsFunctionStart(tokens, i))
                continue;

            var name = tokens[i + 1].Kind == TokenKind.Name ? tokens[i + 1] : (Token?)null;
            List<Token> inputs = [];
            var j = name == null ? i + 1 : i + 2;
            if (name != null && tokens[j].Kind == TokenKind.OpenParen)
            {
                for (j++; tokens[j].Kind is not (TokenKind.CloseParen or TokenKind.NewLine or TokenKind.End); j++)
                {
                    if (tokens[j].Kind == TokenKind.Name && tokens[j - 1].Kind is TokenKind.OpenParen or TokenKind.Comma)
                        inputs.Add(tokens[j]);
                }
            }

            while (tokens[j].Kind is not (TokenKind.NewLine or TokenKind.End))
            {
                j++;
            }

            var bodyStart = tokens[j].Span.End;
            var bodyEnd = bodyStart;
            for (; j < tokens.Count; j++)
            {
                if (tokens[j].Kind == TokenKind.End)
                {
                    bodyEnd = tokens[j].Span.Start;
                    break;
                }

                if (IsFunctionStart(tokens, j))
                {
                    bodyEnd = tokens[j - 1].Span.Start;
                    break;
                }

                if (IsEndLine(tokens, j))
                {
                    bodyEnd = tokens[j].Span.End;
                    break;
                }
            }

            var body = new TextSpan(bodyStart, Math.Max(0, bodyEnd - bodyStart));
            var function = new DefinedFunction(name?.Text, name?.Span ?? tokens[i].Span, inputs.Select(t => t.Text).ToList(), body);
            functions.Add((function, inputs));
            i = j - 1;
        }

        return functions;
    }

    /// <summary>
    /// Like the parser, <c>function = ...</c> is a definition, and anything else after the keyword starts a Function.
    /// </summary>
    internal static bool IsFunctionStart(List<Token> tokens, int index)
    {
        return (index == 0 || tokens[index - 1].Kind == TokenKind.NewLine)
            && tokens[index].Kind == TokenKind.Name
            && tokens[index].Text == FunctionDefinition.Keyword
            && tokens[index + 1].Kind != TokenKind.Equals;
    }

    private static bool IsEndLine(List<Token> tokens, int index)
    {
        return tokens[index - 1].Kind == TokenKind.NewLine
            && tokens[index].Kind == TokenKind.Name
            && tokens[index].Text == FunctionDefinition.EndKeyword
            && tokens[index + 1].Kind is TokenKind.NewLine or TokenKind.End;
    }

    /// <summary>
    /// The type of the value starting at <paramref name="start"/> when the whole value is one call.
    /// </summary>
    private static BlockType ReadType(
        List<Token> tokens,
        int start,
        Func<string, PlacedBlock> findPlacedBlock,
        out string mode,
        out bool isPlaced)
    {
        mode = null;
        isPlaced = false;
        if (start + 1 >= tokens.Count || tokens[start].Kind != TokenKind.Name || tokens[start + 1].Kind != TokenKind.OpenParen)
            return null;

        var function = tokens[start].Text;
        BlockType type;
        string placedMode = null;
        if (function == GraphBuilder.PlacedFunction)
        {
            var uid = tokens[start + 2];
            var block = uid.Kind == TokenKind.String ? findPlacedBlock?.Invoke(uid.Text) : null;
            type = block == null ? null : BlockManifest.GetPlacedType(block.BlockId);
            isPlaced = type != null;
            placedMode = type?.Modes == null ? null : GetModeName(type, block.Settings);
        }
        else if (!BlockManifest.TryGetFunction(function, out type))
        {
            return null;
        }

        var end = FindCallEnd(tokens, start + 1, type, out mode);
        if (tokens[end].Kind is not (TokenKind.NewLine or TokenKind.End))
        {
            mode = null;
            isPlaced = false;
            return null;
        }

        if (isPlaced)
            mode = placedMode;

        return type;
    }

    /// <summary>
    /// Null when the level stores a mode the type doesn't have.
    /// </summary>
    private static string GetModeName(BlockType type, IReadOnlyList<Setting> settings)
    {
        var index = type.GetModeIndex(settings);
        var options = type.Modes.Setting.Options;
        return index >= 0 && index < options.Count ? options[index] : null;
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
