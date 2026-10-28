using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Block Functions and placed-only types a Script can use, with a filter.
/// </summary>
internal sealed class ReferencePanel
{
    private const string PlacedOnlyNote = "placed(\"UID\") only";
    private const string InputsLabel = "inputs";
    private const string OutputsLabel = "outputs";
    private const string SettingsLabel = "settings";

    private string _filter = string.Empty;

    public bool FilterFocused { get; private set; }

    public void ReleaseFocus()
    {
        FilterFocused = false;
    }

    /// <summary>
    /// Named, so its id can't match a scroll area's numbered one: a scroll area whose id is active takes it back on
    /// mouse up, which is how the field lost every click while it was the first control in a tab.
    /// </summary>
    public void DrawFilter(ImGui gui, ImRect rect)
    {
        var id = gui.GetControlId("reference-filter");
        ref var state = ref gui.Storage.Get<ImTextEditState>(id);
        gui.TextEdit(id, ref _filter, ref state, rect, false, hint: "Filter: name, input, output, option");
        FilterFocused = FieldFocus.Update(gui, id);
    }

    public void DrawEntries(ImGui gui)
    {
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

    /// <summary>
    /// The name in the editor's Block Function colour, then a row per part with its label in a column of its own, and
    /// each Setting or mode on a row under its heading.
    /// </summary>
    private static void DrawEntry(ImGui gui, BlockReferenceEntry entry)
    {
        gui.Separator();
        var text = gui.Style.Text.Color;
        var palette = SyntaxPalette.For(gui.Style.Window.Box.BackColor, text);
        gui.Text(entry.Function, palette.Get(HighlightKind.Function), wrap: true);
        var note = GetNote(entry);
        if (note.Length > 0)
            gui.Text(note, WindowColors.Muted, wrap: true);

        var indent = gui.Style.Layout.Indent;
        var labelWidth = gui.MeasureTextSize(OutputsLabel).x + indent;
        DrawRow(gui, InputsLabel, WindowColors.Muted, entry.Inputs, text, labelWidth);

        if (entry.Outputs.Count == 1 && entry.Outputs[0].Name == null)
        {
            DrawRow(gui, OutputsLabel, WindowColors.Muted, entry.Outputs[0].Value, text, labelWidth);
        }
        else
        {
            gui.Text(OutputsLabel, WindowColors.Muted);
            DrawNamedRows(gui, entry.Outputs, palette.Get(HighlightKind.Keyword), text);
        }

        if (entry.Settings.Count > 0)
        {
            gui.Text(SettingsLabel, WindowColors.Muted);
            DrawNamedRows(gui, entry.Settings, palette.Get(HighlightKind.Keyword), text);
        }
    }

    private static string GetNote(BlockReferenceEntry entry)
    {
        if (entry.IsPlacedOnly)
            return PlacedOnlyNote;

        return entry.Aliases.Length > 0 ? $"alias {entry.Aliases}" : string.Empty;
    }

    /// <summary>
    /// Indented, with the names in a column as wide as the longest. A name too long for that column gets a row of its
    /// own, with its value on the next.
    /// </summary>
    private static void DrawNamedRows(ImGui gui, IReadOnlyList<BlockReferenceRow> rows, Color32 nameColor, Color32 valueColor)
    {
        var indent = gui.Style.Layout.Indent;
        var maxNameWidth = gui.GetLayoutWidth() * 0.45f;
        var nameWidth = 0f;
        foreach (var row in rows)
        {
            var width = gui.MeasureTextSize(row.Name).x + indent;
            if (width <= maxNameWidth)
                nameWidth = Mathf.Max(nameWidth, width);
        }

        gui.BeginIndent();
        foreach (var row in rows)
        {
            if (gui.MeasureTextSize(row.Name).x + indent > maxNameWidth)
            {
                gui.Text(row.Name, nameColor);
                gui.BeginIndent();
                gui.Text(row.Value, valueColor, wrap: true);
                gui.EndIndent();
                continue;
            }

            DrawRow(gui, row.Name, nameColor, row.Value, valueColor, nameWidth);
        }

        gui.EndIndent();
    }

    /// <summary>
    /// The label in a column <paramref name="labelWidth"/> wide, the value wrapping in the rest.
    /// </summary>
    private static void DrawRow(ImGui gui, string label, Color32 labelColor, string value, Color32 valueColor, float labelWidth)
    {
        var line = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0);
        var wrapped = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0, wrap: true);
        var width = gui.GetLayoutWidth();
        var valueWidth = Mathf.Max(1, width - labelWidth);
        var height = Mathf.Max(gui.GetTextLineHeight(), gui.MeasureTextSize(value, wrapped, new Vector2(valueWidth, 0)).y);

        gui.AddSpacingIfLayoutFrameNotEmpty();
        var rect = gui.AddLayoutRect(width, height);
        var labelRect = rect.TakeLeft(labelWidth, out var valueRect);
        gui.Text(label, in line, labelColor, labelRect);
        gui.Text(value, in wrapped, valueColor, valueRect);
    }
}
