using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// The box that shows the inputs and Settings of the call the caret is in, with the one being written highlighted. It
/// takes no clicks or keys apart from Escape, which hides it until the caret leaves the call.
/// </summary>
internal sealed class CallHelpPopup
{
    private const float MaxWidth = 480;
    private const string Separator = ", ";

    private readonly CallHelpSession _session;
    private readonly List<(string Text, Color32 Color, ImRect Rect)> _pieces = [];
    private CallHelp _laidOut;
    private float _laidOutWidth;
    private Vector2 _size;

    public CallHelpPopup(EditorAssist assist)
    {
        _session = new CallHelpSession(assist.FindPlacedBlock);
    }

    /// <summary>
    /// Whether Escape hid the help this frame, so it doesn't also take the editor's focus.
    /// </summary>
    public bool TookEscape { get; private set; }

    /// <summary>
    /// Above the caret's line, whose top left is <paramref name="linePosition"/>, or under it when there's no room
    /// above, as <see cref="PopupPlacement"/> picks; past the completion list when that's open on the same side.
    /// Hidden when neither fits inside <paramref name="window"/>.
    /// </summary>
    public void Draw(ImGui gui, Vector2 linePosition, float lineHeight, ImRect editor, ImRect window, ImRect completion)
    {
        if (_session.Help is not { } help)
            return;

        var padding = gui.Style.Menu.Padding;
        var maxTextWidth = Mathf.Min(MaxWidth, window.W) - padding.Horizontal;
        if (!ReferenceEquals(_laidOut, help) || _laidOutWidth != maxTextWidth)
        {
            _laidOut = help;
            _laidOutWidth = maxTextWidth;
            _size = Layout(gui, help, maxTextWidth, lineHeight);
        }

        // A separator never wraps, so a line can run past the most it may take; its text ends in an ellipsis there
        var width = Mathf.Min(_size.x, maxTextWidth) + padding.Horizontal;
        var height = _size.y + padding.Vertical;
        var x = Mathf.Max(window.X, Mathf.Min(linePosition.x - padding.Left, window.Right - width));

        var hasCompletion = completion.W > 0;
        var above = new ImRect(x, linePosition.y, width, height);
        if (hasCompletion && above.Overlaps(completion))
            above.Y = completion.Top;

        var below = new ImRect(x, linePosition.y - lineHeight - height, width, height);
        if (hasCompletion && below.Overlaps(completion))
            below.Y = completion.Y - height;

        if (PopupPlacement.Choose(above.Y, below.Y, height, (editor.Y, editor.Top), (window.Y, window.Top)) is not { } y)
            return;

        var rect = new ImRect(x, y, width, height);

        gui.BeginPopup();
        gui.Box(rect, in gui.Style.Menu.Box);
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0, overflow: ImTextOverflow.Ellipsis);
        var origin = new Vector2(rect.X + padding.Left, rect.Top - padding.Top);
        foreach (var piece in _pieces)
        {
            var pieceRect = piece.Rect;
            pieceRect.Position += origin;
            pieceRect.W = Mathf.Min(pieceRect.W, rect.Right - padding.Right - pieceRect.X);
            if (pieceRect.W > 0)
                gui.Text(piece.Text, in settings, piece.Color, pieceRect);
        }

        gui.EndPopup();
    }

    /// <summary>
    /// Call after the completion list handled the keyboard: Escape that didn't close the list hides the help.
    /// </summary>
    public void HandleKeyboard(bool escapeUsed)
    {
        TookEscape = false;
        if (escapeUsed || _session.Help == null || !Input.GetKeyDown(KeyCode.Escape))
            return;

        _session.Dismiss();
        TookEscape = true;
    }

    /// <inheritdoc cref="CallHelpSession.Update"/>
    public void Update(TextSnapshot snapshot, bool focused)
    {
        TookEscape &= focused;
        _session.Update(snapshot, focused);
    }

    /// <summary>
    /// The call as it's written, <c>logic(a, b, op_in, op:, hide:)</c>, wrapping between its parts, then what the
    /// highlighted Setting accepts on a line of its own. Fills <see cref="_pieces"/> with rects relative to the top left
    /// and returns the size they take.
    /// </summary>
    private Vector2 Layout(ImGui gui, CallHelp help, float maxWidth, float lineHeight)
    {
        _pieces.Clear();
        var palette = SyntaxPalette.For(gui.Style.Menu.Box.BackColor, gui.Style.Text.Color);
        var textColor = gui.Style.Text.Color;
        var x = 0f;
        var y = 0f;
        var width = 0f;

        Add($"{help.Callee}(", palette.Get(HighlightKind.Function));
        for (var i = 0; i < help.Items.Count; i++)
        {
            var last = i == help.Items.Count - 1;
            var color = i == help.Active ? palette.Get(HighlightKind.Keyword) : WindowColors.Muted;
            Add(help.Items[i].Label, color);
            Add(last ? ")" : Separator, WindowColors.Muted, canWrap: false);
        }

        // An input accepts any expression, so only a Setting has more to say
        if (help.Active >= 0 && !help.Items[help.Active].IsInput)
        {
            var active = help.Items[help.Active];
            x = 0;
            y -= lineHeight;
            Add($"{active.Label} ", palette.Get(HighlightKind.Keyword));

            // Wraps between the options a Setting lists
            var values = active.Detail.Split([Separator], StringSplitOptions.None);
            for (var i = 0; i < values.Length; i++)
            {
                Add(i < values.Length - 1 ? values[i] + Separator : values[i], textColor);
            }
        }

        return new Vector2(width, -y + lineHeight);

        void Add(string piece, Color32 color, bool canWrap = true)
        {
            var pieceWidth = Mathf.Min(gui.MeasureTextSize(piece).x, maxWidth);
            if (canWrap && x > 0 && x + pieceWidth > maxWidth)
            {
                x = 0;
                y -= lineHeight;
            }

            _pieces.Add((piece, color, new ImRect(x, y - lineHeight, pieceWidth, lineHeight)));
            x += pieceWidth;
            width = Mathf.Max(width, x);
        }
    }
}
