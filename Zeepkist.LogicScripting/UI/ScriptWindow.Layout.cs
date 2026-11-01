using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The window itself: three regions side by side, Scripts on the left, the current Script in the center and Blocks
/// on the right (those in the level and those a Script can use), whose sides collapse to a strip. The sides and the
/// error panel under the editor are resized by dragging their inner edge, and the error panel hides to its header.
/// Open state, size, position, panel sizes, collapsed panels, collapsed Reference categories, the Scripts side's shown
/// tab and whether Orphaned Groups is collapsed are kept in the <see cref="WindowState"/>.
/// </summary>
internal sealed partial class ScriptWindow
{
    private const string Title = "Logic Script";
    private const float DefaultWidth = 1120;
    private const float DefaultHeight = 640;
    private const float DefaultScriptsWidth = 220;
    private const float MinScriptsWidth = 140;
    private const float MinBlocksWidth = 300;
    private const string PlacedBlocksTab = "Placed blocks";
    private const string ReferenceTab = "Reference";
    private const string ScriptsTab = "Scripts";
    private const string LibrariesTab = "Libraries";

    /// <summary>
    /// Narrower than this, the sides draw collapsed whatever the state says, so the editor stays usable.
    /// </summary>
    private const float MinCenterWidth = 280;

    private const int DefaultErrorRows = 5;
    private const int MinErrorRows = 1;
    private const int MinFieldRows = 3;

    private readonly ResizeHandle _scriptsEdge = new(ImAxis.Horizontal, 1);
    private readonly ResizeHandle _blocksEdge = new(ImAxis.Horizontal, -1);
    private readonly ResizeHandle _errorsEdge = new(ImAxis.Vertical, 1);

    private WindowState _savedState;
    private bool _showReference;

    /// <summary>
    /// Opening tries again after a failure, and logs the next one. A failure in the work before the toggle key is
    /// read, such as saving Drafts, leaves only ZeepSDK's menu bar to reopen the window.
    /// </summary>
    public void SetOpen(bool open)
    {
        _state.Open = open;
        if (open)
            _failed = false;
    }

    /// <summary>
    /// Call when the mod unloads.
    /// </summary>
    public void SaveState()
    {
        if (_state.IsSameAs(_savedState))
            return;

        _stateStore.Save(_state);
        _savedState = _state.Copy();
    }

    /// <summary>
    /// Waits for the mouse to be up, so dragging or resizing the window writes the file once, at the end.
    /// </summary>
    private void SaveStateWhenSettled()
    {
        if (!Input.GetMouseButton(0))
            SaveState();
    }

    /// <summary>
    /// Returns the block under the hovered row of the Placed blocks tab, or null.
    /// </summary>
    private BlockProperties DrawWindow(ImGui gui)
    {
        BlockProperties hovered = null;
        var open = true;

        // A field sets whether it has focus as it's drawn, so one in a hidden tab or a collapsed side has none
        ReleasePanelFocus();
        if (gui.BeginWindow(
                Title,
                ref open,
                ref _mouseOver,
                GetInitialRect(gui),
                mouseEntered: () => SetMouseBlocked(true),
                mouseExited: () => SetMouseBlocked(false)))
        {
            StoreRect(gui.WindowManager.GetWindowState(gui.PeekId()).Rect);
            RefreshScripts();
            hovered = DrawRegions(gui);
        }

        gui.EndWindow();
        if (!open)
            SetOpen(false);

        return hovered;
    }

    /// <summary>
    /// Imui only takes this rect when it first makes the window and keeps its own rect after that, through test mode
    /// too. So it's where the window was in an earlier session, kept on screen in case the resolution changed since.
    /// </summary>
    private ImRect GetInitialRect(ImGui gui)
    {
        var screen = gui.Canvas.SafeScreenRect;
        if (_state.Width > 0 && _state.Height > 0)
            return ImRectUtility.Clamp(screen, new ImRect(_state.X, _state.Y, _state.Width, _state.Height));

        var width = Mathf.Min(DefaultWidth, screen.W);
        var height = Mathf.Min(DefaultHeight, screen.H);
        return new ImRect(screen.X + ((screen.W - width) / 2), screen.Y + ((screen.H - height) / 2), width, height);
    }

    private void StoreRect(ImRect rect)
    {
        _state.X = rect.X;
        _state.Y = rect.Y;
        _state.Width = rect.W;
        _state.Height = rect.H;
    }

    private BlockProperties DrawRegions(ImGui gui)
    {
        var area = gui.Layout.AddRect(gui.GetLayoutWidth(), gui.GetLayoutHeight());
        var collapsedWidth = GetPanelHeaderHeight(gui);
        var minScriptsWidth = GetMinScriptsWidth(gui);
        var minBlocksWidth = GetMinBlocksWidth(gui);

        // The Blocks side gives way first; the Scripts side only when the center still doesn't fit without both
        var blocksFits = area.W - (_state.Scripts.Collapsed ? collapsedWidth : minScriptsWidth) - minBlocksWidth >= MinCenterWidth;
        var scriptsFits = area.W - minScriptsWidth - collapsedWidth >= MinCenterWidth;
        var scriptsCollapsed = _state.Scripts.Collapsed || !scriptsFits;
        var blocksCollapsed = _state.Blocks.Collapsed || !blocksFits;

        // Before collapsing, the sides shrink to make room for the center, the Blocks side first. The state keeps the
        // size they were given, so they grow back when the window does.
        var maxScriptsWidth = area.W - MinCenterWidth - (blocksCollapsed ? collapsedWidth : minBlocksWidth);
        var scriptsWidth = scriptsCollapsed
            ? collapsedWidth
            : GetPanelSize(_state.Scripts.Size, DefaultScriptsWidth, minScriptsWidth, maxScriptsWidth);
        var maxBlocksWidth = area.W - MinCenterWidth - scriptsWidth;
        var blocksWidth = blocksCollapsed
            ? collapsedWidth
            : GetPanelSize(_state.Blocks.Size, minBlocksWidth, minBlocksWidth, maxBlocksWidth);

        var scriptsEdgeId = gui.GetControlId("scripts edge");
        var blocksEdgeId = gui.GetControlId("blocks edge");
        if (!scriptsCollapsed && _scriptsEdge.Drag(gui, scriptsEdgeId, scriptsWidth) is { } draggedScriptsWidth)
            _state.Scripts.Size = ClampSize(draggedScriptsWidth, minScriptsWidth, maxScriptsWidth);

        if (!blocksCollapsed && _blocksEdge.Drag(gui, blocksEdgeId, blocksWidth) is { } draggedBlocksWidth)
            _state.Blocks.Size = ClampSize(draggedBlocksWidth, minBlocksWidth, maxBlocksWidth);

        var scripts = area.TakeLeft(scriptsWidth, out var rest);
        var blocks = rest.TakeRight(blocksWidth, out var center);
        var scriptsEdge = new ImRect(scripts.Right - 1, area.Y, 1, area.H);
        var blocksEdge = new ImRect(blocks.X, area.Y, 1, area.H);
        DrawDivider(gui, scriptsEdge);
        DrawDivider(gui, blocksEdge);

        // Imui numbers controls in drawing order, so each region gets its own scope: a new error row in the center
        // must not change the ids, and so the focus and state, of the controls drawn after it
        gui.PushId("scripts");
        if (DrawSideHeader(gui, scripts, "Scripts", scriptsCollapsed, buttonOnRight: true, canToggle: scriptsFits))
            _state.Scripts.ToggleCollapsed();

        if (!scriptsCollapsed)
            DrawScriptsTabs(gui, GetSideBody(gui, scripts));

        gui.PopId();
        gui.PushId("center");
        DrawCenter(gui, center);
        gui.PopId();

        gui.PushId("blocks");
        if (DrawSideHeader(gui, blocks, "Blocks", blocksCollapsed, buttonOnRight: false, canToggle: blocksFits))
            _state.Blocks.ToggleCollapsed();

        var hovered = blocksCollapsed ? null : DrawBlocksTabs(gui, GetSideBody(gui, blocks));
        gui.PopId();

        // Each grab strip lies right of its edge, off the scrollbar of the region on the left
        if (!scriptsCollapsed)
            _scriptsEdge.Draw(gui, scriptsEdgeId, scriptsEdge, ResizeHandle.GrabRightOf(scriptsEdge));

        if (!blocksCollapsed)
            _blocksEdge.Draw(gui, blocksEdgeId, blocksEdge, ResizeHandle.GrabRightOf(blocksEdge));

        return hovered;
    }

    /// <summary>
    /// A stored size of zero means the panel was never resized. A drag never stores one: it stores the size it shows,
    /// clamped with <see cref="ClampSize"/>.
    /// </summary>
    private static float GetPanelSize(float stored, float defaultSize, float min, float max)
    {
        return ClampSize(stored > 0 ? stored : defaultSize, min, max);
    }

    /// <summary>
    /// Unlike <see cref="Mathf.Clamp(float, float, float)"/>, the minimum wins when there's less room than that.
    /// </summary>
    private static float ClampSize(float size, float min, float max)
    {
        return Mathf.Max(min, Mathf.Min(size, max));
    }

    /// <summary>
    /// Wide enough for both tabs, each half the strip, to show their whole label in the theme's font.
    /// </summary>
    private static float GetMinBlocksWidth(ImGui gui)
    {
        var settings = ImTabsPane.GetTextSettings(gui);
        var widestLabel = Mathf.Max(gui.MeasureTextSize(PlacedBlocksTab, settings).x, gui.MeasureTextSize(ReferenceTab, settings).x);
        return GetMinTabsWidth(gui, widestLabel, MinBlocksWidth);
    }

    /// <summary>
    /// Like <see cref="GetMinBlocksWidth"/>, with room in each tab for both its marks.
    /// </summary>
    private static float GetMinScriptsWidth(ImGui gui)
    {
        var widestLabel = Mathf.Max(SideTab.GetLabelWidth(gui, ScriptsTab), SideTab.GetLabelWidth(gui, LibrariesTab));
        return GetMinTabsWidth(gui, widestLabel, MinScriptsWidth);
    }

    /// <summary>
    /// A side with two tabs, each half its width, and at least <paramref name="min"/>.
    /// </summary>
    private static float GetMinTabsWidth(ImGui gui, float widestLabel, float min)
    {
        var tabWidth = widestLabel + (gui.Style.Layout.Indent * 2);
        return Mathf.Max(min, (tabWidth * 2) + (gui.Style.Layout.Spacing * 4));
    }

    private static void DrawDivider(ImGui gui, ImRect edge)
    {
        gui.Canvas.Rect(edge, gui.Style.Window.Box.BorderColor);
    }

    private static ImRect GetSideBody(ImGui gui, ImRect side)
    {
        side.TakeTop(GetPanelHeaderHeight(gui), out var body);
        return body;
    }

    private static float GetPanelHeaderHeight(ImGui gui)
    {
        return gui.GetRowHeight() + (gui.Style.Layout.Spacing * 2);
    }

    /// <summary>
    /// Returns whether the collapse button was clicked. A collapsed side is only that button, which is grayed out while
    /// the window is too narrow to show the side.
    /// </summary>
    private static bool DrawSideHeader(ImGui gui, ImRect side, string label, bool collapsed, bool buttonOnRight, bool canToggle)
    {
        var size = gui.GetRowHeight();
        var row = side.TakeTop(GetPanelHeaderHeight(gui)).WithPadding(gui.Style.Layout.Spacing);
        ImRect labelRect;
        var buttonRect = buttonOnRight ? row.TakeRight(size, out labelRect) : row.TakeLeft(size, out labelRect);
        if (!collapsed)
            gui.Text(label, new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f), labelRect);

        gui.BeginReadOnly(!canToggle);
        var clicked = gui.Button(collapsed == buttonOnRight ? "»" : "«", buttonRect) && canToggle;
        gui.EndReadOnly();
        if (!canToggle)
            gui.TooltipAtLastControl($"Make the window wider to show the {label} panel");
        else
            gui.TooltipAtLastControl(collapsed ? $"Show the {label} panel" : $"Hide the {label} panel");

        return clicked;
    }

    /// <summary>
    /// The header with Compile, the warnings, the editor, then the error panel: a header with the error count, and the
    /// error list and result line in a region of their own, so a long list scrolls instead of pushing the editor away.
    /// The editor takes the height the panel doesn't.
    /// </summary>
    private void DrawCenter(ImGui gui, ImRect rect)
    {
        gui.Layout.Push(ImAxis.Vertical, rect.WithPadding(gui.Style.Layout.Spacing));
        DrawStartupCheck(gui);
        if (_edited == null)
        {
            gui.Text("No Script selected. Make one with New Script.", wrap: true);
            gui.Layout.Pop();
            return;
        }

        var compileClicked = _edited.DrawHeader(gui);
        _edited.RefreshCheck();

        var spacing = gui.Style.Layout.Spacing;
        var available = gui.Layout.GetAvailableHeight() - (spacing * 2);
        var minFieldHeight = gui.GetRowsHeightWithSpacing(MinFieldRows);
        var headerHeight = GetPanelHeaderHeight(gui);
        var defaultErrorsHeight = gui.GetRowsHeightWithSpacing(DefaultErrorRows);
        var minErrorsHeight = gui.GetRowsHeightWithSpacing(MinErrorRows);
        var maxErrorsHeight = available - headerHeight - minFieldHeight;
        var errorsBodyHeight = GetPanelSize(_state.Errors.Size, defaultErrorsHeight, minErrorsHeight, maxErrorsHeight);
        var errorsHeight = headerHeight + (_state.Errors.Collapsed ? 0 : errorsBodyHeight);

        var errorsEdgeId = gui.GetControlId("errors edge");
        if (!_state.Errors.Collapsed && _errorsEdge.Drag(gui, errorsEdgeId, errorsBodyHeight) is { } draggedErrorsHeight)
            _state.Errors.Size = ClampSize(draggedErrorsHeight, minErrorsHeight, maxErrorsHeight);

        var fieldHeight = Mathf.Max(minFieldHeight, available - errorsHeight);
        var compileRequested = DrawField(gui, gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), fieldHeight));

        var result = compileClicked ? _edited.Compile() : compileRequested ? _edited.CompileFromKeyboard() : null;
        if (result != null)
            _result = result;

        var errors = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), Mathf.Max(0, gui.Layout.GetAvailableHeight() - spacing));
        gui.Layout.Pop();

        gui.Box(errors, in gui.Style.List.Box);
        var header = errors.TakeTop(headerHeight, out var body);
        if (DrawErrorsHeader(gui, header))
            _state.Errors.ToggleCollapsed();

        // A window too short for the editor's minimum leaves no room under the header
        if (_state.Errors.Collapsed || body.H <= 0)
            return;

        var hasErrors = _edited.HasCurrentErrors;
        var columns = default(ErrorColumns);
        if (hasErrors)
        {
            columns = ErrorTable.Measure(gui, _edited.Errors);
            var columnsHeader = body.TakeTop(ErrorTable.GetHeaderHeight(gui), out body);
            ErrorTable.DrawHeader(gui, columnsHeader.WithPadding(left: spacing, right: spacing), columns);
            if (body.H <= 0)
                return;
        }

        WindowRegion.Begin(gui, body);
        if (hasErrors && ErrorTable.DrawRows(gui, _edited.Errors, columns) is { } clicked)
            _edited.Editor.JumpTo(clicked);

        if (_result.Length > 0)
            gui.Text(_result, wrap: true);

        WindowRegion.End(gui);

        // Centered in the gap above the panel, so the grab strip takes as little as it can of the editor's edge
        var edge = new ImRect(errors.X, errors.Top + (spacing / 2), errors.W, 0);
        _errorsEdge.Draw(gui, errorsEdgeId, edge, _errorsEdge.GrabAround(edge));
    }

    /// <summary>
    /// Returns whether the hide button was clicked. A hidden panel is only this row, which shows the result line in
    /// place of the error count while there is one, as the list it's drawn under isn't shown.
    /// </summary>
    private bool DrawErrorsHeader(ImGui gui, ImRect header)
    {
        var row = header.WithPadding(gui.Style.Layout.Spacing);
        var buttonRect = row.TakeRight(gui.GetRowHeight(), gui.Style.Layout.Spacing, out var labelRect);

        var (text, color) = _state.Errors.Collapsed && _result.Length > 0 ? (_result, gui.Style.Text.Color) : GetErrorsSummary();
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        gui.Text(text, settings, color, labelRect);
        if (labelRect.Contains(gui.Input.MousePosition) && gui.MeasureTextSize(text, settings).x > labelRect.W)
            gui.Tooltip(text, labelRect);

        var clicked = gui.Button(_state.Errors.Collapsed ? "▲" : "▼", buttonRect);
        gui.TooltipAtLastControl(_state.Errors.Collapsed ? "Show the error panel" : "Hide the error panel");
        return clicked;
    }

    private (string Text, Color32 Color) GetErrorsSummary()
    {
        if (_edited.HasCurrentErrors)
        {
            var count = _edited.Errors.Count;
            return (count == 1 ? "1 error" : $"{count} errors", WindowColors.Error);
        }

        // A pending check hides the old errors, so the panel doesn't claim the text is fine
        return (_edited.IsChecked ? "No errors found" : "Checking...", WindowColors.Muted);
    }

    private void DrawStartupCheck(ImGui gui)
    {
        if (!_startupCheck.CanPlace)
            gui.Text($"{StartupCheck.UpdateNeededMessage}. Scripts are still checked, but Compile places nothing.", WindowColors.Error, wrap: true);

        if (_startupCheck.DisabledBlocks.Count > 0)
            gui.Text($"Turned off for this game version: {string.Join(", ", _startupCheck.DisabledBlocks)}", WindowColors.Error, wrap: true);
    }

    /// <summary>
    /// The window keeps which tab is shown rather than Imui's tabs pane, whose choice is lost when the Blocks side is
    /// collapsed and doesn't draw it.
    /// </summary>
    private BlockProperties DrawBlocksTabs(ImGui gui, ImRect rect)
    {
        var bar = rect.WithPadding(gui.Style.Layout.Spacing).TakeTop(ImTabsPane.GetTabBarHeight(gui), out var content);
        var placedRect = bar.TakeLeft(bar.W / 2, out var referenceRect);
        if (ImTabsPane.TabBarButton(gui, gui.GetNextControlId(), !_showReference, PlacedBlocksTab, placedRect))
            _showReference = false;

        if (ImTabsPane.TabBarButton(gui, gui.GetNextControlId(), _showReference, ReferenceTab, referenceRect))
            _showReference = true;

        gui.Box(content, in gui.Style.Tabs.ContainerBox);

        // A scope per tab, so their scroll areas don't share one id, and with it one scroll position
        if (_showReference)
        {
            gui.PushId(ReferenceTab);
            var spacing = gui.Style.Layout.Spacing;
            var filter = content.WithPadding(spacing).TakeTop(gui.GetRowHeight(), out _);
            content.TakeTop(gui.GetRowHeight() + spacing, out var entries);
            _referencePanel.DrawFilter(gui, filter);
            WindowRegion.Begin(gui, entries);
            _referencePanel.DrawEntries(gui, _state.CollapsedReferenceCategories);
            WindowRegion.End(gui);
            gui.PopId();
            return null;
        }

        gui.PushId(PlacedBlocksTab);
        BlockProperties hovered = null;
        WindowRegion.Begin(gui, content);
        if (_edited == null)
        {
            gui.Text("No Script selected", wrap: true);
        }
        else
        {
            hovered = _edited.DrawPlacedBlocks(gui, _placedBlocksPanel, out var edited);
            if (edited)
                OnEdited();
        }

        WindowRegion.End(gui);
        gui.PopId();
        return hovered;
    }

    /// <summary>
    /// Like the Blocks side's tabs. Switching tabs leaves the editor as it is, so a dot marks the tab that holds what
    /// it shows, and a warning mark the Scripts tab while the level has Orphaned Groups.
    /// </summary>
    private void DrawScriptsTabs(ImGui gui, ImRect rect)
    {
        var bar = rect.WithPadding(gui.Style.Layout.Spacing).TakeTop(ImTabsPane.GetTabBarHeight(gui), out var content);
        var scriptsRect = bar.TakeLeft(bar.W / 2, out var librariesRect);
        var hasOrphans = _scripts.OrphanedGroups.Count > 0;
        if (SideTab.Draw(gui, !_state.ShowLibraries, ScriptsTab, scriptsRect, CurrentScript != null, hasOrphans))
            _state.ShowLibraries = false;

        gui.TooltipAtLastControl(hasOrphans ? $"{ScriptListPanel.Help}. The level has Orphaned Groups, listed under its Scripts" : ScriptListPanel.Help);

        if (SideTab.Draw(gui, _state.ShowLibraries, LibrariesTab, librariesRect, CurrentLibrary != null, false))
            _state.ShowLibraries = true;

        gui.TooltipAtLastControl(LibraryListPanel.Help);

        gui.Box(content, in gui.Style.Tabs.ContainerBox);

        // A scope per tab, so their scroll areas don't share one id, and with it one scroll position
        gui.PushId(_state.ShowLibraries ? LibrariesTab : ScriptsTab);
        WindowRegion.Begin(gui, content);
        if (_state.ShowLibraries)
            DrawLibrariesTab(gui);
        else
            DrawScriptsTab(gui);

        WindowRegion.End(gui);
        gui.PopId();
    }
}
