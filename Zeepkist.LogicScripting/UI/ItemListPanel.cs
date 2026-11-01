using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A list of Scripts or Libraries in a scroll region, and per entry Rename and Delete. Under the list is a footer pinned
/// to the bottom of the tab, which holds whichever prompt is open, the result line, and New as its bottom row.
/// </summary>
internal abstract class ItemListPanel<T>
    where T : class
{
    private const int MinListRows = 2;

    private readonly string _noun;
    private readonly NamePrompt _namePrompt;
    private readonly DeletePrompt<T> _deletePrompt = new();
    private T _renamed;
    private ImRect _prompts;
    private ImRect _newRow;

    // Measured as the prompts are drawn, as Imui lays out top-down and the list above them needs to know the height first
    private float _promptsHeight;

    /// <summary>
    /// <paramref name="noun"/> is what's listed, such as "Script".
    /// </summary>
    protected ItemListPanel(string noun)
    {
        _noun = noun;
        _namePrompt = new NamePrompt(noun);
    }

    public bool NameFocused => _namePrompt.Focused;

    protected string Message { get; set; } = string.Empty;

    protected abstract IReadOnlyList<T> Entries { get; }

    protected abstract string EmptyText { get; }

    /// <summary>
    /// Null for none.
    /// </summary>
    protected virtual string NewTooltip => null;

    public void ReleaseFocus()
    {
        _namePrompt.Focused = false;
    }

    public void Clear()
    {
        _namePrompt.Close();
        _deletePrompt.Close();
        Message = string.Empty;
    }

    /// <summary>
    /// Begins the list's scroll region, for <see cref="DrawEntries"/> and what's listed with them. The prompts and result
    /// line take the height they took last frame, so while they grow they're cut short for one frame.
    /// </summary>
    public void BeginList(ImGui gui, ImRect rect)
    {
        var spacing = gui.Style.Layout.Spacing;
        var minListHeight = gui.GetRowsHeightWithSpacing(MinListRows) + (spacing * 2);
        _newRow = rect.TakeBottom(gui.GetRowHeight() + (spacing * 2), out var rest);
        _prompts = rest.TakeBottom(FooterSplit.GetFooterHeight(rest.H, _promptsHeight, minListHeight), out var list);

        // A scope each, so an entry coming or going doesn't change the id, and with it the focus and text, of the name
        // field in the footer
        gui.PushId("list");
        WindowRegion.Begin(gui, list);
    }

    /// <summary>
    /// Ends the list's scroll region and draws the footer under it. Returns the entry the player made, or null.
    /// </summary>
    public T EndList(ImGui gui)
    {
        WindowRegion.End(gui);
        gui.PopId();

        var spacing = gui.Style.Layout.Spacing;
        gui.Canvas.Rect(new ImRect(_prompts.X, _prompts.Top - 1, _prompts.W, 1), gui.Style.Window.Box.BorderColor);
        gui.PushId("footer");

        // Only the prompts scroll when there's no room for them, so New stays in view
        WindowRegion.Begin(gui, _prompts);
        var made = DrawPrompts(gui);
        WindowRegion.End(gui, out var contentHeight);
        _promptsHeight = contentHeight > 0 ? contentHeight + (spacing * 2) : 0;

        DrawNew(gui, _newRow.WithPadding(spacing));
        gui.PopId();
        return made;
    }

    /// <summary>
    /// Returns the entry the player clicked, or null.
    /// </summary>
    public T DrawEntries(ImGui gui, T current)
    {
        T selected = null;
        foreach (var entry in Entries)
        {
            gui.AddSpacingIfLayoutFrameNotEmpty();
            var rect = gui.AddSingleRowRect(default);
            ref readonly var style = ref entry == current ? ref gui.Style.List.ItemSelected : ref gui.Style.List.ItemNormal;
            if (gui.Button(gui.GetNextControlId(), GetLabel(entry), rect, in style, out _) && entry != current)
                selected = entry;

            if (GetTooltip(entry) is { } tooltip)
                gui.TooltipAtLastControl(tooltip);

            if (gui.BeginContextMenu(rect))
            {
                if (gui.Menu("Rename"))
                {
                    _deletePrompt.Close();
                    _renamed = entry;
                    _namePrompt.OpenRename(GetName(entry));
                }

                DrawMenuItems(gui, entry);

                if (gui.Menu("Delete"))
                    OpenDelete(entry);

                gui.EndContextMenu();
            }
        }

        if (Entries.Count == 0)
            gui.Text(EmptyText, WindowColors.Muted, wrap: true);

        return selected;
    }

    /// <summary>
    /// Returns the entry the player made, or null.
    /// </summary>
    private T DrawPrompts(ImGui gui)
    {
        T made = null;
        var named = _namePrompt.Draw(gui, name => GetNameError(name, _renamed));
        if (named != null)
        {
            Message = string.Empty;
            if (_renamed == null)
                made = Add(named);
            else
                Rename(_renamed, named);

            _namePrompt.Close();
        }

        if (_deletePrompt.Deleted is { } deleted)
        {
            var choice = _deletePrompt.Draw(gui, GetDeleteQuestion(deleted), AsksAboutBlocks(deleted));
            if (choice != DeleteChoice.None)
            {
                Delete(deleted, choice);
                _deletePrompt.Close();
            }
        }

        if (Message.Length > 0)
            gui.Text(Message, wrap: true);

        return made;
    }

    private void DrawNew(ImGui gui, ImRect rect)
    {
        if (gui.Button($"New {_noun}", rect))
        {
            _deletePrompt.Close();
            _renamed = null;
            _namePrompt.OpenNew();
        }

        if (NewTooltip != null)
            gui.TooltipAtLastControl(NewTooltip);
    }

    public void OpenDelete(T entry)
    {
        _namePrompt.Close();
        _deletePrompt.Open(entry);
    }

    /// <summary>
    /// What naming an entry in New's prompt does. Returns null, with why in <paramref name="error"/>, for a name the
    /// prompt doesn't take.
    /// </summary>
    public T New(string name, out string error)
    {
        name = name.Trim();
        error = GetNameError(name, null);
        if (error != null)
            return null;

        _renamed = null;
        _namePrompt.Close();
        _deletePrompt.Close();
        Message = string.Empty;
        return Add(name);
    }

    protected abstract string GetName(T entry);

    protected virtual string GetLabel(T entry)
    {
        return GetName(entry);
    }

    protected virtual string GetTooltip(T entry)
    {
        return null;
    }

    /// <summary>
    /// The context menu items between Rename and Delete.
    /// </summary>
    protected virtual void DrawMenuItems(ImGui gui, T entry)
    {
    }

    /// <summary>
    /// <paramref name="renamed"/> is the entry being renamed, or null for a new one.
    /// </summary>
    protected abstract string GetNameError(string name, T renamed);

    protected abstract T Add(string name);

    protected abstract void Rename(T entry, string name);

    protected abstract string GetDeleteQuestion(T entry);

    protected virtual bool AsksAboutBlocks(T entry)
    {
        return false;
    }

    /// <summary>
    /// Sets the <see cref="Message"/>.
    /// </summary>
    protected abstract void Delete(T entry, DeleteChoice choice);
}
