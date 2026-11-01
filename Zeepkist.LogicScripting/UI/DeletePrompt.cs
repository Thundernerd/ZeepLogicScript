using Imui.Controls;
using Imui.Core;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// Asks the player to confirm deleting a Script or Library, and for a Script with blocks, what happens to them.
/// </summary>
internal sealed class DeletePrompt<T>
    where T : class
{
    /// <summary>
    /// What's being deleted, or null when the prompt is closed.
    /// </summary>
    public T Deleted { get; private set; }

    public void Open(T deleted)
    {
        Deleted = deleted;
    }

    public void Close()
    {
        Deleted = null;
    }

    /// <summary>
    /// Call while <see cref="Deleted"/> is set. Returns the player's choice once they make one; Cancel closes the
    /// prompt. <paramref name="question"/> asks about <see cref="Deleted"/>. With <paramref name="keepBlocks"/>, the
    /// player picks whether its blocks go too.
    /// </summary>
    public DeleteChoice Draw(ImGui gui, string question, bool keepBlocks)
    {
        var choice = DeleteChoice.None;
        gui.Text(question, wrap: true);
        if (keepBlocks)
        {
            if (gui.Button("Delete blocks too"))
                choice = DeleteChoice.Delete;

            if (gui.Button("Keep blocks"))
                choice = DeleteChoice.KeepBlocks;
        }
        else if (gui.Button("Delete"))
        {
            choice = DeleteChoice.Delete;
        }

        // Stacked rather than side by side: the Scripts side is too narrow for the three in a row
        if (gui.Button("Cancel"))
            Close();

        return choice;
    }
}
