using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Block Functions and placed-only types a Script can use, with a filter.
/// </summary>
internal sealed class ReferencePanel
{
    private const string PlacedOnlyNote = "placed(\"UID\") only";

    private string _filter = string.Empty;

    public bool FilterFocused { get; private set; }

    public void ReleaseFocus()
    {
        FilterFocused = false;
    }

    public void Draw(ImGui gui)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var rect = gui.AddSingleRowRect(default);
        var id = gui.GetNextControlId();
        ref var state = ref gui.Storage.Get<ImTextEditState>(id);
        gui.TextEdit(id, ref _filter, ref state, rect, false);
        FilterFocused = FieldFocus.Update(gui, id);

        if (_filter.Length == 0)
            gui.TooltipAtLastControl("Filter by name, input, output or option");

        var shown = 0;
        foreach (var entry in BlockReference.Entries)
        {
            if (!entry.Matches(_filter))
                continue;

            DrawEntry(gui, entry);
            shown++;
        }

        if (shown == 0)
            gui.Text("Nothing matches", wrap: true);
    }

    private static void DrawEntry(ImGui gui, BlockReferenceEntry entry)
    {
        gui.Separator();
        if (entry.IsPlacedOnly)
            gui.Text($"{entry.Function}: {PlacedOnlyNote}", wrap: true);
        else if (entry.Aliases.Length > 0)
            gui.Text($"{entry.Function} (alias {entry.Aliases})", wrap: true);
        else
            gui.Text(entry.Function, wrap: true);

        gui.Text($"inputs: {entry.Inputs}", WindowColors.Muted, wrap: true);
        gui.Text($"outputs: {entry.Outputs}", WindowColors.Muted, wrap: true);
        gui.Text($"settings: {entry.Settings}", WindowColors.Muted, wrap: true);
    }
}
