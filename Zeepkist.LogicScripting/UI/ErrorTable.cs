using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Compiler;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The error panel's errors, a row each, with the line and column in columns as wide as their widest value so the
/// message keeps as much of the width as it can. Its column names are drawn apart from the rows, so they stay put while
/// the rows scroll.
/// </summary>
internal sealed class ErrorTable
{
    private const string LineHeader = "Line";
    private const string ColumnHeader = "Col";
    private const string MessageHeader = "Message";

    private float _lineWidth;
    private float _columnWidth;

    /// <summary>
    /// Sizes the number columns to <paramref name="errors"/>, for this frame's <see cref="DrawHeader"/> and
    /// <see cref="DrawRows"/>.
    /// </summary>
    public void Measure(ImGui gui, IReadOnlyList<Diagnostic> errors)
    {
        var line = gui.MeasureTextSize(LineHeader).x;
        var column = gui.MeasureTextSize(ColumnHeader).x;
        foreach (var error in errors)
        {
            line = Mathf.Max(line, gui.MeasureTextSize(error.Position.Line.ToString()).x);
            column = Mathf.Max(column, gui.MeasureTextSize(error.Position.Column.ToString()).x);
        }

        var padding = gui.Style.Layout.Spacing * 2;
        _lineWidth = line + padding;
        _columnWidth = column + padding;
    }

    public static float GetHeaderHeight(ImGui gui)
    {
        return gui.GetTextLineHeight() + gui.Style.Layout.ExtraRowHeight;
    }

    /// <summary>
    /// The column names in the muted color, over a line that sets them apart from the rows.
    /// </summary>
    public void DrawHeader(ImGui gui, ImRect rect)
    {
        var settings = GetLineSettings(gui, 0);
        var numberSettings = GetLineSettings(gui, 1);
        SplitCells(gui, rect, out var lineRect, out var columnRect, out var messageRect);
        gui.Text(LineHeader, in numberSettings, WindowColors.Muted, lineRect);
        gui.Text(ColumnHeader, in numberSettings, WindowColors.Muted, columnRect);
        gui.Text(MessageHeader, in settings, WindowColors.Muted, messageRect);

        gui.Canvas.Rect(new ImRect(rect.X, rect.Y, rect.W, 1), gui.Style.Window.Box.BorderColor);
    }

    /// <summary>
    /// Returns the position of the error whose row was clicked, or null. An error from inside a Library Function shows
    /// where in the Library it is in the muted color, on its own line above the message.
    /// </summary>
    public Position? DrawRows(ImGui gui, IReadOnlyList<Diagnostic> errors)
    {
        var numberSettings = GetLineSettings(gui, 1);
        var wrapped = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0, wrap: true);
        var spacing = gui.Style.Layout.Spacing;
        var width = gui.GetLayoutWidth();
        var messageBounds = new Vector2(Mathf.Max(1, width - _lineWidth - _columnWidth - (spacing * 2)), 0);
        var hover = gui.Style.Menu.ItemNormal.Hovered.BackColor;

        Position? clicked = null;
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            var originHeight = error.Origin == null ? 0 : gui.MeasureTextSize(error.Origin, in wrapped, messageBounds).y;
            var detailHeight = gui.MeasureTextSize(error.Detail, in wrapped, messageBounds).y;
            var height = Mathf.Max(gui.GetRowHeight(), originHeight + detailHeight + gui.Style.Layout.ExtraRowHeight);

            // No spacing between rows, so the alternating backgrounds meet
            var rect = gui.Layout.AddRect(width, height);
            if (gui.InvisibleButton(gui.GetNextControlId(), rect, out var state))
                clicked = error.Position;

            if (state != ImButtonState.Normal)
                gui.Canvas.Rect(rect, hover);
            else if (i % 2 == 1)
                gui.Canvas.Rect(rect, WindowColors.RowStripe);

            SplitCells(gui, rect, out var lineRect, out var columnRect, out var messageRect);
            gui.Text(error.Position.Line.ToString(), in numberSettings, gui.Style.Text.Color, lineRect);
            gui.Text(error.Position.Column.ToString(), in numberSettings, gui.Style.Text.Color, columnRect);
            if (error.Origin != null)
            {
                var originRect = messageRect.TakeTop(originHeight, out messageRect);
                gui.Text(error.Origin, in wrapped, WindowColors.Muted, originRect);
            }

            gui.Text(error.Detail, in wrapped, gui.Style.Text.Color, messageRect);
        }

        return clicked;
    }

    private static ImTextSettings GetLineSettings(ImGui gui, float alignX)
    {
        return new ImTextSettings(gui.Style.Layout.TextSize, alignX, 0, overflow: ImTextOverflow.Ellipsis);
    }

    /// <summary>
    /// Splits a row into its cells, each padded so the numbers and the message don't touch.
    /// </summary>
    private void SplitCells(ImGui gui, ImRect row, out ImRect line, out ImRect column, out ImRect message)
    {
        var spacing = gui.Style.Layout.Spacing;
        var content = row.WithPadding(top: gui.Style.Layout.ExtraRowHeight / 2, bottom: gui.Style.Layout.ExtraRowHeight / 2);
        line = content.TakeLeft(_lineWidth, out var rest).WithPadding(left: spacing, right: spacing);
        column = rest.TakeLeft(_columnWidth, out message).WithPadding(left: spacing, right: spacing);
        message = message.WithPadding(left: spacing, right: spacing);
    }
}
