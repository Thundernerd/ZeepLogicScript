using System.Collections.Generic;
using BepInEx.Logging;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using TNRD.Zeepkist.LogicScripting.Blocks;
using TNRD.Zeepkist.LogicScripting.Compiler;
using TNRD.Zeepkist.LogicScripting.Compiler.Storage;
using TNRD.Zeepkist.LogicScripting.Storage;

namespace TNRD.Zeepkist.LogicScripting.UI;

internal sealed class EditedScript : EditedItem
{
    private const string CompileLabel = "Compile (Ctrl+Enter)";

    private readonly DraftSaver _draftSaver;
    private readonly StartupCheck _startupCheck;
    private readonly LibraryList _libraries;
    private readonly ManualLogSource _logger;
    private CompileResult _check;

    public EditedScript(
        ScriptEntry entry,
        ScriptEditor editor,
        DraftSaver draftSaver,
        StartupCheck startupCheck,
        LibraryList libraries,
        ManualLogSource logger)
        : base(editor)
    {
        Entry = entry;
        _draftSaver = draftSaver;
        _startupCheck = startupCheck;
        _libraries = libraries;
        _logger = logger;
    }

    public ScriptEntry Entry { get; }

    public override bool BelongsToLevel => true;

    /// <summary>
    /// The Script's name and marks, Compile on the right, and the storage warning under them.
    /// </summary>
    public override bool DrawHeader(ImGui gui)
    {
        gui.AddSpacingIfLayoutFrameNotEmpty();
        var row = gui.AddSingleRowRect(default);
        var buttonWidth = gui.MeasureTextSize(CompileLabel).x + (gui.Style.Layout.Indent * 2);
        var buttonRect = row.TakeRight(buttonWidth, gui.Style.Layout.Spacing, out var nameRect);

        var (glyph, label, tooltip) = ScriptMarkText.Get(Entry.Mark);
        var settings = new ImTextSettings(gui.Style.Layout.TextSize, 0, 0.5f, overflow: ImTextOverflow.Ellipsis);
        gui.Text(glyph == null ? Entry.Name : $"{Entry.Name}  {glyph} {label}", settings, nameRect);
        if (tooltip != null && nameRect.Contains(gui.Input.MousePosition))
            gui.Tooltip(tooltip, nameRect);

        var compileClicked = DrawCompileButton(gui, buttonRect);
        DrawStorageWarning(gui);
        return compileClicked;
    }

    /// <summary>
    /// Checks a Script edited since the last check first, so errors typed just now show in the list, not in the
    /// result line.
    /// </summary>
    public override string Compile()
    {
        if (!IsChecked)
        {
            CheckNow();
            if (HasCurrentErrors)
                return null;
        }

        var result = ScriptCompile.Run(Entry.ToStored(), _startupCheck, _libraries.FindText, _logger);

        // Placing changed the level, so the Placed blocks panel checks again now
        CheckSoon();
        return result;
    }

    public override string CompileFromKeyboard()
    {
        return GetCompileBlocker() ?? Compile();
    }

    public override BlockProperties DrawPlacedBlocks(ImGui gui, PlacedBlocksPanel panel, out bool edited)
    {
        return panel.Draw(gui, _check, Editor, out edited);
    }

    protected override void StoreText(string text)
    {
        Entry.Text = text;
        _draftSaver.Request();
    }

    protected override IReadOnlyList<Diagnostic> Check(string text)
    {
        _check = ScriptCompile.Check(text, Entry.Id, _startupCheck, _libraries.FindText);
        return _check?.Errors;
    }

    private bool DrawCompileButton(ImGui gui, ImRect rect)
    {
        var blocker = GetCompileBlocker();
        gui.BeginReadOnly(blocker != null);
        var clicked = gui.Button(CompileLabel, rect);
        gui.EndReadOnly();

        if (blocker != null)
            gui.TooltipAtLastControl(blocker);

        return clicked && blocker == null;
    }

    /// <summary>
    /// Why Compile can't run right now, or null when it can.
    /// </summary>
    private string GetCompileBlocker()
    {
        if (HasCurrentErrors)
            return "Fix the errors first";

        LevelEdit.GetCentral(out var blocker);
        return blocker;
    }

    /// <summary>
    /// Stays up while the level is missing the Script's text: leaving the editor can't be canceled to save it.
    /// </summary>
    private void DrawStorageWarning(ImGui gui)
    {
        if (Entry.Mark == ScriptMark.CarrierMissing)
        {
            gui.Text("The level has this Script's blocks but not its text, likely because a block holding it was deleted. Compile to store it again.", WindowColors.Error, wrap: true);
        }
        else if (Entry.Mark == ScriptMark.NeverCompiled && Entry.Text.Length > 0)
        {
            gui.Text("Not stored in the level yet: Compile to store it. Until then, loading a level or going back to the main menu loses it.", WindowColors.Error, wrap: true);
        }
    }
}
