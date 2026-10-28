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
            if (gui.Button("Delete blocks too"))
                choice = DeleteChoice.DeleteBlocks;

            if (gui.Button("Keep blocks"))
                choice = DeleteChoice.KeepBlocks;
        }
        else
        {
            gui.Text($"Delete \"{Deleted.Name}\"? It has no blocks in the level.", wrap: true);
            if (gui.Button("Delete"))
                choice = DeleteChoice.DeleteBlocks;
        }

        // Stacked rather than side by side: the Scripts side is too narrow for the three in a row
        if (gui.Button("Cancel"))
            Close();

        return choice;
    }
}
