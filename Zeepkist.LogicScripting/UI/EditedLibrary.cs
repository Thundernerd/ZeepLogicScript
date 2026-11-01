using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

/// <summary>
/// A Library isn't compiled itself, and lives on this machine rather than in the level, so it has no Compile button or
/// marks, and only gets errors from its check.
/// </summary>
internal sealed class EditedLibrary : EditedItem
{
    private readonly LibraryList _libraries;

    public EditedLibrary(Library library, ScriptEditor editor, LibraryList libraries)
        : base(editor)
    {
        Library = library;
        _libraries = libraries;
    }

    public Library Library { get; }

    public override bool BelongsToLevel => false;

    public override bool DrawHeader(ImGui gui)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var row = gui.AddSingleRowRect(default);
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        gui.Text($"Library {Library.Name}", settings, row);
        return false;
    }

    public override string Compile()
    {
        return CompileFromKeyboard();
    }

    public override string CompileFromKeyboard()
    {
        return $"A Library isn't compiled itself. Compile a Script that calls {Library.Name}.function(...)";
    }

    public override BlockProperties DrawPlacedBlocks(ImGui gui, PlacedBlocksPanel panel, out bool edited)
    {
        edited = false;
        gui.Text("A Library can't use placed(...), since a UID belongs to one level. A Script passes its placed blocks in as inputs.", wrap: true);
        return null;
    }

    protected override void StoreText(string text)
    {
        _libraries.SetText(Library, text);
    }

    protected override IReadOnlyList<Diagnostic> Check(string text)
    {
        return ScriptCompiler.CheckLibrary(text, _libraries.FindText, Library.Name);
    }
}
