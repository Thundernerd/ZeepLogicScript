using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The window itself: three regions side by side, Scripts on the left, the current Script in the centre and the
/// level on the right, whose sides collapse to a strip. The sides and the error panel under the editor are resized by
/// dragging their inner edge, and the error panel hides to its header. Open state, size, position, panel sizes and
/// collapsed panels are kept in the <see cref="WindowState"/>.
/// </summary>
internal sealed partial class ScriptWindow
{
    private const string Title = "Logic Script";
    private const string CompileLabel = "Compile (Ctrl+Enter)";
    private const float DefaultWidth = 1120;
    private const float DefaultHeight = 640;
    private const float DefaultScriptsWidth = 220;
    private const float MinScriptsWidth = 140;
    private const float MinLevelWidth = 300;
    private const string PlacedBlocksTab = "Placed blocks";
    private const string ReferenceTab = "Reference";

    /// <summary>
    /// Narrower than this, the sides draw collapsed whatever the state says, so the editor stays usable.
    /// </summary>
    private const float MinCentreWidth = 280;

    private const int DefaultErrorRows = 5;
    private const int MinErrorRows = 1;
    private const int MinFieldRows = 3;

    private WindowState _savedState;
    private bool _showReference;

    public void SetOpen(bool open)
    {
        _state.Open = open;
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
        var collapsedWidth = GetSideHeaderHeight(gui);
        var minLevelWidth = GetMinLevelWidth(gui);

        // The Level side gives way first; the Scripts side only when the centre still doesn't fit without both
        var levelFits = area.W - (_state.ScriptsCollapsed ? collapsedWidth : MinScriptsWidth) - minLevelWidth >= MinCentreWidth;
        var scriptsFits = area.W - MinScriptsWidth - collapsedWidth >= MinCentreWidth;
        var scriptsCollapsed = _state.ScriptsCollapsed || !scriptsFits;
        var levelCollapsed = _state.LevelCollapsed || !levelFits;

        // Before collapsing, the sides shrink to make room for the centre, the Level side first. The state keeps the
        // size they were given, so they grow back when the window does.
        var scriptsWidth = scriptsCollapsed
            ? collapsedWidth
            : ClampSize(
                GetSizeOrDefault(_state.ScriptsWidth, DefaultScriptsWidth),
                MinScriptsWidth,
                area.W - MinCentreWidth - (levelCollapsed ? collapsedWidth : minLevelWidth));
        var levelWidth = levelCollapsed
            ? collapsedWidth
            : ClampSize(GetSizeOrDefault(_state.LevelWidth, minLevelWidth), minLevelWidth, area.W - MinCentreWidth - scriptsWidth);

        var scripts = area.TakeLeft(scriptsWidth, out var rest);
        var level = rest.TakeRight(levelWidth, out var centre);
        var scriptsEdge = new ImRect(scripts.Right - 1, area.Y, 1, area.H);
        var levelEdge = new ImRect(level.X, area.Y, 1, area.H);
        DrawDivider(gui, scriptsEdge);
        DrawDivider(gui, levelEdge);

        // Imui numbers controls in drawing order, so each region gets its own scope: a new error row in the centre
        // must not change the ids, and so the focus and state, of the controls drawn after it
        gui.PushId("scripts");
        if (DrawSideHeader(gui, scripts, "Scripts", scriptsCollapsed, buttonOnRight: true, canToggle: scriptsFits))
            _state.ScriptsCollapsed = !_state.ScriptsCollapsed;

        if (!scriptsCollapsed)
        {
            WindowRegion.Begin(gui, GetSideBody(gui, scripts));
            DrawScriptList(gui);
            WindowRegion.End(gui);
        }

        gui.PopId();
        gui.PushId("centre");
        DrawCentre(gui, centre);
        gui.PopId();

        gui.PushId("level");
        if (DrawSideHeader(gui, level, "Level", levelCollapsed, buttonOnRight: false, canToggle: levelFits))
            _state.LevelCollapsed = !_state.LevelCollapsed;

        var hovered = levelCollapsed ? null : DrawLevelTabs(gui, GetSideBody(gui, level));
        gui.PopId();

        if (!scriptsCollapsed)
        {
            var dragged = ResizeHandle.Draw(gui, gui.GetControlId("scripts edge"), scriptsEdge, ImAxis.Horizontal);
            if (dragged != 0)
                _state.ScriptsWidth = Mathf.Max(MinScriptsWidth, scriptsWidth + dragged);
        }

        if (!levelCollapsed)
        {
            var dragged = ResizeHandle.Draw(gui, gui.GetControlId("level edge"), levelEdge, ImAxis.Horizontal);
            if (dragged != 0)
                _state.LevelWidth = Mathf.Max(minLevelWidth, levelWidth - dragged);
        }

        return hovered;
    }

    /// <summary>
    /// A drag never stores a zero size, which would read as never resized.
    /// </summary>
    private static float GetSizeOrDefault(float size, float defaultSize)
    {
        return size > 0 ? size : defaultSize;
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
    private static float GetMinLevelWidth(ImGui gui)
    {
        var settings = ImTabsPane.GetTextSettings(gui);
        var widestLabel = Mathf.Max(gui.MeasureTextSize(PlacedBlocksTab, settings).x, gui.MeasureTextSize(ReferenceTab, settings).x);
        var tabWidth = widestLabel + (gui.Style.Layout.Indent * 2);
        return Mathf.Max(MinLevelWidth, (tabWidth * 2) + (gui.Style.Layout.Spacing * 4));
    }

    private static void DrawDivider(ImGui gui, ImRect edge)
    {
        gui.Canvas.Rect(edge, gui.Style.Window.Box.BorderColor);
    }

    private static ImRect GetSideBody(ImGui gui, ImRect side)
    {
        side.TakeTop(GetSideHeaderHeight(gui), out var body);
        return body;
    }

    private static float GetSideHeaderHeight(ImGui gui)
    {
        return gui.GetRowHeight() + (gui.Style.Layout.Spacing * 2);
    }

    /// <summary>
    /// Returns whether the collapse button was clicked. A collapsed side is only that button, which is greyed out while
    /// the window is too narrow to show the side.
    /// </summary>
    private static bool DrawSideHeader(ImGui gui, ImRect side, string label, bool collapsed, bool buttonOnRight, bool canToggle)
    {
        var size = gui.GetRowHeight();
        var row = side.TakeTop(GetSideHeaderHeight(gui)).WithPadding(gui.Style.Layout.Spacing);
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
    private void DrawCentre(ImGui gui, ImRect rect)
    {
        gui.Layout.Push(ImAxis.Vertical, rect.WithPadding(gui.Style.Layout.Spacing));
        DrawStartupCheck(gui);
        if (_current == null)
        {
            gui.Text("No Script selected. Make one with New Script.", wrap: true);
            gui.Layout.Pop();
            return;
        }

        var compileClicked = DrawHeader(gui);
        DrawStorageWarning(gui);
        RefreshCheck();

        var spacing = gui.Style.Layout.Spacing;
        var available = gui.Layout.GetAvailableHeight() - (spacing * 2);
        var minFieldHeight = gui.GetRowsHeightWithSpacing(MinFieldRows);
        var headerHeight = GetSideHeaderHeight(gui);
        var minErrorsHeight = gui.GetRowsHeightWithSpacing(MinErrorRows);
        var errorsHeight = headerHeight;
        if (!_state.ErrorsHidden)
        {
            errorsHeight += ClampSize(
                GetSizeOrDefault(_state.ErrorsHeight, gui.GetRowsHeightWithSpacing(DefaultErrorRows)),
                minErrorsHeight,
                available - headerHeight - minFieldHeight);
        }

        var fieldHeight = Mathf.Max(minFieldHeight, available - errorsHeight);
        var compileRequested = DrawField(gui, gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), fieldHeight));

        if (compileClicked)
            Compile();
        else if (compileRequested)
            CompileFromKeyboard();

        var errors = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), Mathf.Max(0, gui.Layout.GetAvailableHeight() - spacing));
        gui.Layout.Pop();

        gui.Box(errors, in gui.Style.List.Box);
        var header = errors.TakeTop(headerHeight, out var body);
        if (DrawErrorsHeader(gui, header))
            _state.ErrorsHidden = !_state.ErrorsHidden;

        // A window too short for the editor's minimum leaves no room under the header
        if (_state.ErrorsHidden || body.H <= 0)
            return;

        WindowRegion.Begin(gui, body);
        DrawErrors(gui);
        if (_result.Length > 0)
            gui.Text(_result, wrap: true);

        WindowRegion.End(gui);

        // Halfway into the gap above the panel, so it doesn't take clicks from the editor's edge
        var edge = new ImRect(errors.X, errors.Top + (spacing / 2), errors.W, 0);
        var dragged = ResizeHandle.Draw(gui, gui.GetControlId("errors edge"), edge, ImAxis.Vertical);
        if (dragged != 0)
            _state.ErrorsHeight = Mathf.Max(minErrorsHeight, body.H + dragged);
    }

    /// <summary>
    /// Returns whether the hide button was clicked. A hidden panel is only this row, which shows the result line in
    /// place of the error count while there is one, as the list it's drawn under isn't shown.
    /// </summary>
    private bool DrawErrorsHeader(ImGui gui, ImRect header)
    {
        var row = header.WithPadding(gui.Style.Layout.Spacing);
        var buttonRect = row.TakeRight(gui.GetRowHeight(), gui.Style.Layout.Spacing, out var labelRect);

        var (text, color) = _state.ErrorsHidden && _result.Length > 0 ? (_result, gui.Style.Text.Color) : GetErrorsSummary();
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        gui.Text(text, settings, color, labelRect);
        if (labelRect.Contains(gui.Input.MousePosition) && gui.MeasureTextSize(text, settings).x > labelRect.W)
            gui.Tooltip(text, labelRect);

        var clicked = gui.Button(_state.ErrorsHidden ? "▲" : "▼", buttonRect);
        gui.TooltipAtLastControl(_state.ErrorsHidden ? "Show the error panel" : "Hide the error panel");
        return clicked;
    }

    /// <summary>
    /// The Script's name and marks, and Compile on the right. Returns whether Compile was clicked.
    /// </summary>
    private bool DrawHeader(ImGui gui)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var row = gui.AddSingleRowRect(default);
        var buttonWidth = gui.MeasureTextSize(CompileLabel).x + (gui.Style.Layout.Indent * 2);
        var buttonRect = row.TakeRight(buttonWidth, gui.Style.Layout.Spacing, out var nameRect);

        var (glyph, label, tooltip) = ScriptMarkText.Get(_current.Mark);
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        gui.Text(glyph == null ? _current.Name : $"{_current.Name}  {glyph} {label}", settings, nameRect);
        if (tooltip != null && nameRect.Contains(gui.Input.MousePosition))
            gui.Tooltip(tooltip, nameRect);

        return DrawCompileButton(gui, buttonRect);
    }

    /// <summary>
    /// Stays up while the level is missing the Script's text: leaving the editor can't be cancelled to save it.
    /// </summary>
    private void DrawStorageWarning(ImGui gui)
    {
        if (_current.Mark == ScriptMark.CarrierMissing)
        {
            gui.Text("The level has this Script's blocks but not its text, likely because a block holding it was deleted. Compile to store it again.", WindowColors.Error, wrap: true);
        }
        else if (_current.Mark == ScriptMark.NeverCompiled && _current.Text.Length > 0)
        {
            gui.Text("Not stored in the level yet: Compile to store it. Until then, loading a level or going back to the main menu loses it.", WindowColors.Error, wrap: true);
        }
    }

    private void DrawStartupCheck(ImGui gui)
    {
        if (!_startupCheck.CanPlace)
            gui.Text($"{StartupCheck.UpdateNeededMessage}. Scripts are still checked, but Compile places nothing.", WindowColors.Error, wrap: true);

        if (_startupCheck.DisabledBlocks.Count > 0)
            gui.Text($"Turned off for this game version: {string.Join(", ", _startupCheck.DisabledBlocks)}", WindowColors.Error, wrap: true);
    }

    /// <summary>
    /// The window keeps which tab is shown rather than Imui's tabs pane, whose choice is lost when the Level side is
    /// collapsed and doesn't draw it.
    /// </summary>
    private BlockProperties DrawLevelTabs(ImGui gui, ImRect rect)
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
            _referencePanel.DrawEntries(gui);
            WindowRegion.End(gui);
            gui.PopId();
            return null;
        }

        gui.PushId(PlacedBlocksTab);
        BlockProperties hovered = null;
        WindowRegion.Begin(gui, content);
        if (_current == null)
        {
            gui.Text("No Script selected", wrap: true);
        }
        else
        {
            hovered = _placedBlocksPanel.Draw(gui, _check, _editor, out var edited);
            if (edited)
                OnEdited();
        }

        WindowRegion.End(gui);
        gui.PopId();
        return hovered;
    }
}
