using Imui.Core;
using Imui.IO.Events;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A strip over the edge between two regions that moves the edge when dragged. Draw it after both regions: Imui gives
/// the hover to the last control under the cursor.
/// </summary>
internal static class ResizeHandle
{
    /// <summary>
    /// Wider than the 1px edge it lies over, so it's easy to grab.
    /// </summary>
    private const float GrabSize = 6;

    private const float LineSize = 2;

    /// <summary>
    /// Takes the edge's line, as thin as it's drawn, and the axis it moves along. Returns how far it was dragged this
    /// frame: along x for a vertical edge, along y (up is positive) for a horizontal one.
    /// </summary>
    public static float Draw(ImGui gui, uint id, ImRect edge, ImAxis axis)
    {
        var hovered = gui.IsControlHovered(id);
        var active = gui.IsControlActive(id);
        var dragged = 0f;

        ref readonly var evt = ref gui.Input.MouseEvent;
        switch (evt.Type)
        {
            // The first frame's movement only comes with BeginDrag: Imui sends no Drag for it
            case ImMouseEventType.Down or ImMouseEventType.BeginDrag when evt.LeftButton && hovered:
                gui.SetActiveControl(id, ImControlFlag.Draggable);
                dragged = GetDelta(evt, axis);
                gui.Input.UseMouseEvent();
                break;

            case ImMouseEventType.Drag when active:
                dragged = GetDelta(evt, axis);
                gui.Input.UseMouseEvent();
                break;

            case ImMouseEventType.Up when active:
                gui.ResetActiveControl();
                break;
        }

        gui.RegisterControl(id, Widen(edge, axis, GrabSize));
        if (hovered || active)
            gui.Canvas.Rect(Widen(edge, axis, LineSize), gui.Style.Window.ResizeHandleActiveColor);

        return dragged;
    }

    private static float GetDelta(in ImMouseEvent evt, ImAxis axis)
    {
        return axis == ImAxis.Horizontal ? evt.Delta.x : evt.Delta.y;
    }

    private static ImRect Widen(ImRect edge, ImAxis axis, float size)
    {
        var center = edge.Center;
        return axis == ImAxis.Horizontal
            ? new ImRect(center.x - (size / 2), edge.Y, size, edge.H)
            : new ImRect(edge.X, center.y - (size / 2), edge.W, size);
    }
}
