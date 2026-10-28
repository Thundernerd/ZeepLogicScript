using Imui.Core;
using Imui.IO.Events;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A strip over the edge of a panel that resizes the panel when dragged. Call <see cref="Drag"/> before drawing the
/// controls under the strip, so they never see the drag's mouse events and take the drag over, and
/// <see cref="Draw"/> after them, as Imui gives the hover to the last control under the cursor.
/// </summary>
internal sealed class ResizeHandle
{
    /// <summary>
    /// Wider than the 1px edge it lies over, so it's easy to grab.
    /// </summary>
    private const float GrabSize = 6;

    private const float LineSize = 2;

    private readonly ImAxis _axis;
    private readonly float _growDirection;
    private float _startSize;
    private float _startMouse;

    /// <param name="axis">The axis the edge moves along.</param>
    /// <param name="growDirection">1 when moving the edge right or up makes the panel bigger, -1 when smaller.</param>
    public ResizeHandle(ImAxis axis, float growDirection)
    {
        _axis = axis;
        _growDirection = growDirection;
    }

    /// <summary>
    /// Returns the size the drag gives the panel, or null while it isn't being dragged. The size follows the mouse from
    /// where the drag began: Imui's drag deltas don't add up to the mouse's movement in game.
    /// </summary>
    public float? Drag(ImGui gui, uint id, float size)
    {
        ref readonly var evt = ref gui.Input.MouseEvent;
        if (!gui.IsControlActive(id))
        {
            if (evt.Type is not (ImMouseEventType.Down or ImMouseEventType.BeginDrag) || !evt.LeftButton || !gui.IsControlHovered(id))
                return null;

            gui.SetActiveControl(id, ImControlFlag.Draggable);
            gui.Input.UseMouseEvent();
            _startSize = size;
            _startMouse = GetMouse(gui);
            return null;
        }

        // Unity's button state too, in case the Up event never reaches the handle
        if (evt.Type == ImMouseEventType.Up || !Input.GetMouseButton(0))
        {
            gui.ResetActiveControl();
            return null;
        }

        if (evt.Type is ImMouseEventType.BeginDrag or ImMouseEventType.Drag)
            gui.Input.UseMouseEvent();

        return _startSize + ((GetMouse(gui) - _startMouse) * _growDirection);
    }

    public void Draw(ImGui gui, uint id, ImRect edge, ImRect grab)
    {
        gui.RegisterControl(id, grab);
        if (gui.IsControlHovered(id) || gui.IsControlActive(id))
            gui.Canvas.Rect(Widen(edge, LineSize), gui.Style.Window.ResizeHandleActiveColor);
    }

    public ImRect GrabAround(ImRect edge)
    {
        return Widen(edge, GrabSize);
    }

    /// <summary>
    /// For a vertical edge with something on its left, like a scrollbar, that must keep its clicks.
    /// </summary>
    public static ImRect GrabRightOf(ImRect edge)
    {
        return new ImRect(edge.X, edge.Y, GrabSize, edge.H);
    }

    private float GetMouse(ImGui gui)
    {
        var mouse = gui.Input.MousePosition;
        return _axis == ImAxis.Horizontal ? mouse.x : mouse.y;
    }

    private ImRect Widen(ImRect edge, float size)
    {
        var center = edge.Center;
        return _axis == ImAxis.Horizontal
            ? new ImRect(center.x - (size / 2), edge.Y, size, edge.H)
            : new ImRect(edge.X, center.y - (size / 2), edge.W, size);
    }
}
