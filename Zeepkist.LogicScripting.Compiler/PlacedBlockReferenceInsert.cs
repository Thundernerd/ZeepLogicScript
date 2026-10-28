using System;
using System.Collections.Generic;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Syntax;

namespace TNRD.Zeepkist.LogicScripting.Compiler;

/// <summary>
/// Writes a Placed Block Reference for each selected block into a Script: <c>&lt;type&gt;_&lt;n&gt; = placed("UID")</c>,
/// with <c>block_&lt;n&gt;</c> for a block that can only be a Toggler target.
/// </summary>
public static class PlacedBlockReferenceInsert
{
    /// <summary>
    /// Inserts one line per selected block the Script doesn't refer to yet and that isn't in a Block Group, as whole
    /// lines at the caret: where the caret's line starts, or else after that line. Each name takes the lowest number not yet defined.
    /// </summary>
    public static InsertResult Insert(string text, int caret, IReadOnlyList<SelectedBlock> blocks)
    {
        text ??= string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));

        var tokens = Lexer.Tokenize(text, []);
        HashSet<string> names = [];
        HashSet<string> uids = [];
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind == TokenKind.String)
                uids.Add(tokens[i].Text);
            else if (tokens[i].Kind == TokenKind.Name && i + 1 < tokens.Count && tokens[i + 1].Kind == TokenKind.Equals)
                names.Add(tokens[i].Text);
        }

        List<string> lines = [];
        var alreadyReferenced = 0;
        var skippedGroupBlocks = 0;
        foreach (var block in blocks)
        {
            if (block.IsInBlockGroup)
            {
                skippedGroupBlocks++;
                continue;
            }

            if (!uids.Add(block.Uid))
            {
                alreadyReferenced++;
                continue;
            }

            var prefix = BlockManifest.GetPlacedType(block.BlockId).Function;
            var number = 1;
            while (names.Contains($"{prefix}_{number}"))
            {
                number++;
            }

            var name = $"{prefix}_{number}";
            names.Add(name);
            lines.Add($"{name} = placed(\"{block.Uid}\")");
        }

        if (lines.Count == 0)
            return new InsertResult(text, caret, 0, alreadyReferenced, skippedGroupBlocks);

        var inserted = string.Join("\n", lines);
        int at;
        if (caret == 0 || text[caret - 1] == '\n')
        {
            at = caret;
            if (at < text.Length)
                inserted += "\n";
        }
        else
        {
            var lineEnd = text.IndexOf('\n', caret);
            at = lineEnd < 0 ? text.Length : lineEnd;
            inserted = "\n" + inserted;
        }

        return new InsertResult(text.Insert(at, inserted), at + inserted.Length, lines.Count, alreadyReferenced, skippedGroupBlocks);
    }
}
