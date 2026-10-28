using System.Collections.Generic;
using System.Linq;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using ZeepSDK.Utilities;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Script's Placed Block References from its last check, and Insert selected.
/// </summary>
internal sealed class PlacedBlocksPanel
{
    private string _insertMessage = string.Empty;

    /// <summary>
    /// A block referred to more than once is one row.
    /// </summary>
    public static int CountRows(CompileResult check)
    {
        return check == null ? 0 : GetRows(check).Count;
    }

    public void ClearMessage()
    {
        _insertMessage = string.Empty;
    }

    /// <summary>
    /// Returns the block under the hovered row, or null. <paramref name="edited"/> is whether Insert selected changed
    /// the editor's text.
    /// </summary>
    public BlockProperties Draw(ImGui gui, CompileResult check, ScriptEditor editor, out bool edited)
    {
        edited = false;
        var central = ComponentCache.Get<LEV_LevelEditorCentral>();
        var selectedCount = central == null ? 0 : central.selection.list.Count;
        if (gui.Button($"Insert selected ({selectedCount})"))
            edited = InsertSelected(gui, central, editor);

        gui.TooltipAtLastControl("Select blocks in the level, then insert them at the caret");
        if (_insertMessage.Length > 0)
            gui.Text(_insertMessage, wrap: true);

        if (central == null || check == null || check.PlacedBlockReferences.Count == 0)
        {
            gui.Text("No placed(...) references in this Script yet", wrap: true);
            return null;
        }

        BlockProperties hovered = null;
        foreach (var reference in GetRows(check))
        {
            var name = reference.Name ?? "(unnamed)";
            var block = reference.Type == null ? null : LevelBlocks.Find(central, reference.Uid);
            if (block == null)
            {
                gui.Text($"{name}: no block with UID \"{reference.Uid}\"", WindowColors.Error, wrap: true);
                continue;
            }

            gui.AddSpacingIfLayoutFrameNotEmpty();
            var id = gui.GetNextControlId();
            var rect = gui.AddSingleRowRect(default);
            if (gui.Button(id, $"{name}: {reference.Type.Function}", rect, out var state))
                PlacedBlockHighlight.Frame(central, block);

            gui.TooltipAtLastControl($"UID \"{reference.Uid}\"; click to look at it");
            if (state != ImButtonState.Normal)
                hovered = block;
        }

        return hovered;
    }

    private static List<PlacedBlockReference> GetRows(CompileResult check)
    {
        HashSet<string> shown = [];
        return check.PlacedBlockReferences.Where(r => shown.Add(r.Uid)).ToList();
    }

    private bool InsertSelected(ImGui gui, LEV_LevelEditorCentral central, ScriptEditor editor)
    {
        if (central == null || central.selection.list.Count == 0)
        {
            _insertMessage = "Select blocks in the level first";
            return false;
        }

        List<SelectedBlock> selected = [];
        foreach (var block in central.selection.list)
        {
            selected.Add(new SelectedBlock(block.UID, block.blockID, BlockGroups.IsInAnyGroup(block)));
        }

        // Clicking the button took focus from the field, but the editor still holds the caret
        var insert = PlacedBlockReferenceInsert.Insert(editor.Text, editor.Caret, selected);
        if (insert.InsertedCount > 0)
            editor.Replace(insert.Text, insert.Caret, gui.Input.Time);

        _insertMessage = $"Inserted {insert.InsertedCount} placed blocks";
        if (insert.AlreadyReferencedCount > 0)
            _insertMessage += $"; skipped {insert.AlreadyReferencedCount} the Script already refers to";

        if (insert.SkippedGroupBlockCount > 0)
            _insertMessage += $"; skipped {insert.SkippedGroupBlockCount} in a Block Group, which a recompile replaces";

        return insert.InsertedCount > 0;
    }
}
