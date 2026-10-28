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
/// level on the right, whose sides collapse to a strip. Open state, size, position and collapsed sides are kept in
/// the <see cref="WindowState"/>.
/// </summary>
internal sealed partial class ScriptWindow
{
    private const string Title = "Logic Script";
    private const string CompileLabel = "Compile (Ctrl+Enter)";
    private const float DefaultWidth = 1120;
    private const float DefaultHeight = 640;
    private const float ScriptsWidth = 220;
    private const float MinLevelWidth = 300;
    private const string PlacedBlocksTab = "Placed blocks";
    private const string ReferenceTab = "Reference";

    /// <summary>
    /// Narrower than this, the sides draw collapsed whatever the state says, so the editor stays usable.
    /// </summary>
    private const float MinCentreWidth = 280;

    private const int ProblemRows = 5;

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
        var levelWidth = GetLevelWidth(gui);

        // The Level side gives way first; the Scripts side only when the centre still doesn't fit without both
        var levelFits = area.W - ScriptsWidth - levelWidth >= MinCentreWidth;
        var scriptsFits = area.W - ScriptsWidth - collapsedWidth >= MinCentreWidth;
        var scriptsCollapsed = _state.ScriptsCollapsed || !scriptsFits;
        var levelCollapsed = _state.LevelCollapsed || !levelFits;

        var scripts = area.TakeLeft(scriptsCollapsed ? collapsedWidth : ScriptsWidth, out var rest);
        var level = rest.TakeRight(levelCollapsed ? collapsedWidth : levelWidth, out var centre);
        DrawDivider(gui, scripts.Right - 1, area);
        DrawDivider(gui, level.X, area);

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
        return hovered;
    }

    /// <summary>
    /// Wide enough for both tabs, each half the strip, to show their whole label in the theme's font.
    /// </summary>
    private static float GetLevelWidth(ImGui gui)
    {
        var settings = ImTabsPane.GetTextSettings(gui);
        var widestLabel = Mathf.Max(gui.MeasureTextSize(PlacedBlocksTab, settings).x, gui.MeasureTextSize(ReferenceTab, settings).x);
        var tabWidth = widestLabel + (gui.Style.Layout.Indent * 2);
        return Mathf.Max(MinLevelWidth, (tabWidth * 2) + (gui.Style.Layout.Spacing * 4));
    }

    private static void DrawDivider(ImGui gui, float x, ImRect area)
    {
        gui.Canvas.Rect(new ImRect(x, area.Y, 1, area.H), gui.Style.Window.Box.BorderColor);
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
    /// The header with Compile, the warnings, the editor, then the error list and result line in a region of their
    /// own, so a long list scrolls instead of pushing the editor away.
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
        var problemsHeight = gui.GetRowsHeightWithSpacing(ProblemRows);
        var fieldHeight = Mathf.Max(gui.GetRowsHeightWithSpacing(3), gui.Layout.GetAvailableHeight() - problemsHeight - (spacing * 2));
        var compileRequested = DrawField(gui, gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), fieldHeight));

        if (compileClicked)
            Compile();
        else if (compileRequested)
            CompileFromKeyboard();

        var problems = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), Mathf.Max(0, gui.Layout.GetAvailableHeight() - spacing));
        gui.Layout.Pop();

        gui.Box(problems, in gui.Style.List.Box);
        WindowRegion.Begin(gui, problems);
        DrawErrors(gui);
        if (_result.Length > 0)
            gui.Text(_result, wrap: true);

        WindowRegion.End(gui);
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
        BlockProperties hovered = null;
        WindowRegion.Begin(gui, content);
        if (_showReference)
        {
            _referencePanel.Draw(gui);
        }
        else if (_current == null)
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
        return hovered;
    }
}
