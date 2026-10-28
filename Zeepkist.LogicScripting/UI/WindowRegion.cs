using Imui.Controls;
using Imui.Core;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A fixed part of the window that scrolls on its own and clips what it draws, so each region keeps its size whatever
/// its content.
/// </summary>
internal static class WindowRegion
{
    public static void Begin(ImGui gui, ImRect rect)
    {
        gui.Layout.Push(ImAxis.Vertical, rect.WithPadding(gui.Style.Layout.Spacing));

        // Without the clip rect, controls scrolled out of the region would still take clicks
        gui.Canvas.PushRectMask(rect, 0);
        gui.Canvas.PushClipRect(rect);
        gui.BeginScrollable();
    }

    public static void End(ImGui gui)
    {
        gui.EndScrollable();
        gui.Canvas.PopClipRect();
        gui.Canvas.PopRectMask();
        gui.Layout.Pop();
    }
}
