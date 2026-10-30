using System.Collections.Generic;
using System.Linq;
using Imui.Controls;
using Imui.Core;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using TNRD.Zeepkist.LogicScripting.Compiler.Manifest;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The Block Functions and placed-only types a Script can use, by category, then the Libraries' Functions, with a
/// filter.
/// </summary>
internal sealed class ReferencePanel
{
    private const string LibrariesHeader = "Libraries";
    private const string PlacedOnlyNote = "placed(\"UID\") only";
    private const string LibraryErrorsNote = "its Library has errors, so it can't be called";
    private const string InputsLabel = "inputs";
    private const string OutputsLabel = "outputs";
    private const string SettingsLabel = "settings";

    /// <summary>
    /// The most of an entry's width a name column takes, so the options beside it still have room.
    /// </summary>
    private const float MaxNameColumn = 0.45f;

    private readonly LibraryList _libraries;

    private string _filter = string.Empty;
    private IReadOnlyList<LibraryReferenceEntry> _libraryEntries = [];
    private int _libraryEntriesVersion = -1;

    public ReferencePanel(LibraryList libraries)
    {
        _libraries = libraries;
    }

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

    /// <summary>
    /// A foldout per category with an entry that matches the filter, and one for the Libraries' Functions. While a
    /// filter is typed every shown category is open, whatever <paramref name="collapsed"/> says, so a match is never
    /// hidden.
    /// </summary>
    public void DrawEntries(ImGui gui, ICollection<string> collapsed)
    {
        var filtering = _filter.Trim().Length > 0;
        var shown = 0;
        foreach (var category in BlockReference.Categories)
        {
            if (!category.Entries.Any(e => e.Matches(_filter)))
                continue;

            shown++;
            if (!DrawHeader(gui, category.Category.Name, collapsed, filtering))
                continue;

            foreach (var entry in category.Entries)
            {
                if (entry.Matches(_filter))
                    DrawEntry(gui, entry);
            }
        }

        var libraryEntries = GetLibraryEntries();
        if (libraryEntries.Any(e => e.Matches(_filter)))
        {
            shown++;
            if (DrawHeader(gui, LibrariesHeader, collapsed, filtering))
            {
                foreach (var entry in libraryEntries)
                {
                    if (entry.Matches(_filter))
                        DrawEntry(gui, entry);
                }
            }
        }

        if (shown == 0)
            gui.Text("Nothing matches", wrap: true);
    }

    /// <summary>
    /// Made again only when a Library changed since the last time.
    /// </summary>
    private IReadOnlyList<LibraryReferenceEntry> GetLibraryEntries()
    {
        if (_libraryEntriesVersion != _libraries.Version)
        {
            _libraryEntries = LibraryReference.Describe(_libraries);
            _libraryEntriesVersion = _libraries.Version;
        }

        return _libraryEntries;
    }

    /// <summary>
    /// Whether the category is open. Clicking it while filtering does nothing, as it can't close then.
    /// </summary>
    private static bool DrawHeader(ImGui gui, string name, ICollection<string> collapsed, bool filtering)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var id = gui.GetControlId(name);
        var rect = gui.AddSingleRowRect(default);
        var open = filtering || !collapsed.Contains(name);
        gui.BeginReadOnlyWithoutStyleChanges(filtering);
        ImFoldout.DrawFoldout(gui, id, ref open, name, rect);
        gui.EndReadOnlyWithoutStyleChanges();
        if (filtering)
            return true;

        if (open)
            collapsed.Remove(name);
        else if (!collapsed.Contains(name))
            collapsed.Add(name);

        return open;
    }

    /// <summary>
    /// The name in the editor's Block Function color, then a row per part with its label in a column of its own, and
    /// each Setting or mode on a row under its heading.
    /// </summary>
    private static void DrawEntry(ImGui gui, BlockReferenceEntry entry)
    {
        var palette = DrawName(gui, entry.Function);
        var text = gui.Style.Text.Color;
        var nameColor = palette.Get(HighlightKind.Keyword);
        var note = GetNote(entry);
        if (note.Length > 0)
            gui.Text(note, WindowColors.Muted, wrap: true);

        var labelWidth = GetLabelWidth(gui);
        DrawRow(gui, InputsLabel, WindowColors.Muted, entry.Inputs, text, labelWidth);

        if (entry.Outputs != null)
        {
            DrawRow(gui, OutputsLabel, WindowColors.Muted, entry.Outputs, text, labelWidth);
        }
        else
        {
            gui.Text(OutputsLabel, WindowColors.Muted);
            DrawNamedRows(gui, entry.ModeOutputs, nameColor, text);
        }

        if (entry.Settings.Count > 0)
        {
            gui.Text(SettingsLabel, WindowColors.Muted);
            DrawNamedRows(gui, entry.Settings, nameColor, text);
        }
    }

    /// <summary>
    /// Laid out like a Block Function's entry, with the inputs used as a Setting marked in the inputs' row, and a note
    /// while the Library has errors.
    /// </summary>
    private static void DrawEntry(ImGui gui, LibraryReferenceEntry entry)
    {
        DrawName(gui, entry.Function);
        if (entry.LibraryHasErrors)
            gui.Text(LibraryErrorsNote, WindowColors.Muted, wrap: true);

        var text = gui.Style.Text.Color;
        var labelWidth = GetLabelWidth(gui);
        DrawRow(gui, InputsLabel, WindowColors.Muted, entry.Inputs, text, labelWidth);
        DrawRow(gui, OutputsLabel, WindowColors.Muted, entry.Outputs, text, labelWidth);
    }

    /// <summary>
    /// The separator above an entry, and its name in the editor's Block Function color.
    /// </summary>
    private static SyntaxPalette DrawName(ImGui gui, string function)
    {
        gui.Separator();
        var palette = SyntaxPalette.For(gui.Style.Window.Box.BackColor, gui.Style.Text.Color);
        gui.Text(function, palette.Get(HighlightKind.Function), wrap: true);
        return palette;
    }

    private static float GetLabelWidth(ImGui gui)
    {
        return gui.MeasureTextSize(OutputsLabel).x + gui.Style.Layout.Indent;
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
        gui.BeginIndent();
        var indent = gui.Style.Layout.Indent;
        var maxNameWidth = gui.GetLayoutWidth() * MaxNameColumn;
        var widths = new float[rows.Count];
        var nameWidth = 0f;
        for (var i = 0; i < rows.Count; i++)
        {
            widths[i] = gui.MeasureTextSize(rows[i].Name).x + indent;
            if (widths[i] <= maxNameWidth)
                nameWidth = Mathf.Max(nameWidth, widths[i]);
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (widths[i] > maxNameWidth)
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
