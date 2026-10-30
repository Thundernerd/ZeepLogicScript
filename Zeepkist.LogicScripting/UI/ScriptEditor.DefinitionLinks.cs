using Imui.Controls;
using Imui.Core;
using Imui.IO.Events;
using TNRD.Zeepkist.LogicScripting.Compiler.Editing;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed partial class ScriptEditor
{
    private const float LinkThickness = 1;

    private (string Text, int? Index) _linkFound;
    private DefinitionLink _link;

    /// <summary>
    /// The link of the name under the mouse while Ctrl (Cmd on macOS) is held; null otherwise. Read again only when
    /// the text or the character change.
    /// </summary>
    private DefinitionLink FindLink(ImGui gui, ImRect textRect, ImRect textBounds, in ImTextEditBuffer buffer, bool hovered)
    {
        if (!hovered || !IsLinkModifierHeld())
            return null;

        var index = FindCharacterAt(gui, gui.Input.MousePosition, textRect, textBounds, in buffer);
        if (ReferenceEquals(_linkFound.Text, Text) && _linkFound.Index == index)
            return _link;

        _linkFound = (Text, index);
        _link = index is { } at ? DefinitionLink.At(Text, at, _assist.FindPlacedBlock) : null;
        return _link;
    }

    private void DrawLink(ImGui gui, DefinitionLink link, ImRect textRect, SyntaxPalette palette)
    {
        var span = link.Name;
        var line = ImTextEdit.FindLineAtCaretPosition(span.Start, in _layout, out var offset);
        var from = ImTextEdit.LineOffsetToViewPosition(line, offset, Text, textRect, gui.TextDrawer, in _layout);
        var to = ImTextEdit.LineOffsetToViewPosition(line, offset + span.Length, Text, textRect, gui.TextDrawer, in _layout);
        var y = from.y - _layout.LineHeight + LinkThickness;
        gui.Canvas.Line(new Vector2(from.x, y), new Vector2(to.x, y), palette.Get(null), LinkThickness);
    }

    /// <summary>
    /// Ctrl+click on a linked name goes to its definition instead of placing the caret, focusing the field if it
    /// wasn't; returns whether it did.
    /// </summary>
    private bool HandleLinkClick(ImGui gui, DefinitionLink link)
    {
        ref readonly var evt = ref gui.Input.MouseEvent;
        if (link == null || evt.Type != ImMouseEventType.Down || !evt.LeftButton)
            return false;

        JumpTo(link.Target.Start);
        gui.Input.UseMouseEvent();
        return true;
    }

    /// <summary>
    /// Read from the keyboard rather than the mouse event, which only carries Ctrl, never Cmd. Not with Alt, like
    /// <see cref="IsCommand"/>, because AltGr arrives as Ctrl+Alt.
    /// </summary>
    private static bool IsLinkModifierHeld()
    {
        var control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        var command = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        return (control || command) && !alt;
    }
}
