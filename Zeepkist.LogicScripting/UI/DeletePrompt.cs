using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Asks what happens to a Script's blocks when the Script is deleted.
/// </summary>
internal sealed class DeletePrompt
{
    /// <summary>
    /// The Script being deleted, or null when the prompt is closed.
    /// </summary>
    public ScriptEntry Deleted { get; private set; }

    public void Open(ScriptEntry entry)
    {
        Deleted = entry;
    }

    public void Close()
    {
        Deleted = null;
    }

    /// <summary>
    /// Returns the player's choice once they make one; Cancel closes the prompt.
    /// </summary>
    public DeleteChoice Draw(ImGui gui)
    {
        if (Deleted == null)
            return DeleteChoice.None;

        var choice = DeleteChoice.None;
        if (Deleted.HasGroup)
        {
            gui.Text($"Delete \"{Deleted.Name}\"? Its blocks can go too, or stay as plain Logic Blocks.", wrap: true);
            gui.AddSpacingIfLayoutFrameNotEmpty();
            gui.BeginHorizontal();
            if (gui.Button("Delete blocks too", ImSizeMode.Fit))
                choice = DeleteChoice.DeleteBlocks;

            if (gui.Button("Keep blocks", ImSizeMode.Fit))
                choice = DeleteChoice.KeepBlocks;
        }
        else
        {
            gui.Text($"Delete \"{Deleted.Name}\"? It has no blocks in the level.", wrap: true);
            gui.AddSpacingIfLayoutFrameNotEmpty();
            gui.BeginHorizontal();
            if (gui.Button("Delete", ImSizeMode.Fit))
                choice = DeleteChoice.DeleteBlocks;
        }

        if (gui.Button("Cancel", ImSizeMode.Fit))
            Close();

        gui.EndHorizontal();
        return choice;
    }
}
