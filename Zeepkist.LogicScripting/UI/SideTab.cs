using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A tab drawn like <see cref="ImTabsPane.TabBarButton"/>, whose label can carry a dot before it, for the tab that
/// holds what the editor shows, and a warning mark after it.
/// </summary>
internal static class SideTab
{
    private const string WarningMark = "!";
    private const float DotScale = 0.4f;

    /// <summary>
    /// Returns whether the tab was clicked.
    /// </summary>
    public static bool Draw(ImGui gui, bool selected, string label, ImRect rect, bool dot, bool warning)
    {
        ref readonly var style = ref selected ? ref gui.Style.Tabs.Selected : ref gui.Style.Tabs.Normal;
        var clicked = gui.Button(gui.GetNextControlId(), rect, in style, out var state);
        var color = ImButton.GetStateFrontColor(in style, state);

        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f);
        var spacing = gui.Style.Layout.InnerSpacing;
        var dotSize = GetDotSize(gui);
        var labelWidth = gui.MeasureTextSize(label, settings).x;
        var markWidth = gui.MeasureTextSize(WarningMark, settings).x;
        var width = labelWidth + (dot ? dotSize + spacing : 0) + (warning ? spacing + markWidth : 0);

        var x = rect.Center.x - (width / 2);
        if (dot)
        {
            gui.Canvas.Circle(new Vector2(x + (dotSize / 2), rect.Center.y), dotSize / 2, color);
            x += dotSize + spacing;
        }

        gui.Text(label, settings, color, new ImRect(x, rect.Y, labelWidth, rect.H));
        if (warning)
            gui.Text(WarningMark, settings, WindowColors.Warning, new ImRect(x + labelWidth + spacing, rect.Y, markWidth, rect.H));

        return clicked;
    }

    /// <summary>
    /// The width of the label with both marks, so a tab's label fits whichever it shows.
    /// </summary>
    public static float GetLabelWidth(ImGui gui, string label)
    {
        var settings = ImTabsPane.GetTextSettings(gui);
        var spacing = gui.Style.Layout.InnerSpacing;
        return GetDotSize(gui) + spacing + gui.MeasureTextSize(label, settings).x + spacing + gui.MeasureTextSize(WarningMark, settings).x;
    }

    private static float GetDotSize(ImGui gui)
    {
        return gui.Style.Layout.TextSize * DotScale;
    }
}
